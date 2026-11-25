using CRM.api;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

DatabaseTargetRouting.ResolvePlatformMasterAndConfigure(builder.Configuration, args);

builder.Services.AddDbContext<PlatformMasterDbContext>((services, options) =>
    options.UseSqlServer(services.GetRequiredService<IConfiguration>().GetConnectionString("PlatformMaster"),
        sql => sql.EnableRetryOnFailure(3)));
builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.Password.RequiredLength = 12;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<PlatformMasterDbContext>().AddSignInManager().AddDefaultTokenProviders();
builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken(options => options.BearerTokenExpiration = TimeSpan.FromMinutes(30));
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TenantOperations", policy => policy.RequireAuthenticatedUser().AddRequirements(new BackendAccess("tenant")));
    options.AddPolicy("MasterAdmin", policy => policy.RequireAuthenticatedUser().AddRequirements(new BackendAccess("master")));
});
builder.Services.AddScoped<IAuthorizationHandler, BackendAccessHandler>();
builder.Services.AddScoped<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
builder.Services.AddScoped<TenantRequestContext>();
builder.Services.AddScoped(sp => sp.GetRequiredService<TenantRequestContext>().Database
    ?? throw new InvalidOperationException("No authorized tenant database is available."));
builder.Services.AddOpenApi();

var app = builder.Build();
if (args.Contains("--initialize", StringComparer.Ordinal))
{
    await BackendInitialization.RunAsync(app.Services, app.Configuration);
    return;
}
if (args.Contains("--provision-all-tenants", StringComparer.Ordinal) || args.Contains("--provision-local", StringComparer.Ordinal))
{
    await BackendInitialization.ProvisionAllLocalDatabasesAsync(app.Services, app.Configuration);
    return;
}
if (args.Contains("--provision-staff", StringComparer.Ordinal) || args.Contains("--provision-tenant-a-staff", StringComparer.Ordinal))
{
    var reports = await BackendInitialization.ProvisionTenantAStaffAsync(app.Services, app.Configuration);
    foreach (var r in reports)
    {
        Console.WriteLine($"[STAFF] {r.Username} ({r.Role}) -> Created: {r.WasCreated}, Profile: {r.ProfileProvisioningStatus}, Active: {r.IsActive}");
    }
    return;
}
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (DbUpdateConcurrencyException) when (!context.Response.HasStarted)
    {
        await Results.Conflict(new { error = "This record changed. Reload it before saving." }).ExecuteAsync(context);
    }
    catch (DbUpdateException) when (!context.Response.HasStarted)
    {
        await Results.Conflict(new { error = "The change could not be saved. Check related records and duplicate references." }).ExecuteAsync(context);
    }
    catch (InvalidOperationException ex) when (!context.Response.HasStarted)
    {
        if (ex.InnerException != null) app.Logger.LogError(ex, "Tenant operation failed.");
        // Business-rule errors are safe messages; provider retry failures are not.
        if (ex.InnerException != null)
            await Results.Problem("Database unavailable. Retry shortly.", statusCode: 503).ExecuteAsync(context);
        else await Results.Conflict(new { error = ex.Message }).ExecuteAsync(context);
    }
    catch (Microsoft.Data.SqlClient.SqlException) when (!context.Response.HasStarted)
    {
        await Results.Problem("Database unavailable. Retry shortly.", statusCode: 503).ExecuteAsync(context);
    }
    catch (Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException) when (!context.Response.HasStarted)
    {
        await Results.Problem("Database unavailable after retrying. Retry shortly.", statusCode: 503).ExecuteAsync(context);
    }
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/crm") && context.User.Identity?.IsAuthenticated == true)
    {
        var tenantId = context.User.FindFirst("tenant_id")?.Value ?? "";
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            var factory = context.RequestServices.GetRequiredService<ITenantDbContextFactory>();
            try
            {
                var db = await factory.CreateAsync(tenantId);
                var auditUser = context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                    ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? "Unknown";
                db.AuditActor = auditUser;
                var masterDb = context.RequestServices.GetRequiredService<PlatformMasterDbContext>();
                db.StaffValidator = (userId) =>
                {
                    var m = masterDb.Memberships.AsNoTracking().SingleOrDefault(x => x.TenantId == tenantId && x.UserId == userId);
                    if (m == null) return (false, false, "");
                    return (true, m.IsActive, m.Role);
                };
                context.RequestServices.GetRequiredService<TenantRequestContext>().Database = db;
            }
            catch (Exception)
            {
                var path = context.Request.Path.Value ?? "";
                if (!path.StartsWith("/crm/staff", StringComparison.OrdinalIgnoreCase) &&
                    !path.StartsWith("/crm/modules", StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }
            }
            await next(context);
            return;
        }
        await next(context);
    }
    else await next(context);
});
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "running" }));
app.MapBackendAuthentication();
app.MapCrmEndpoints();
app.MapAppointmentEndpoints();
app.MapEstimateEndpoints();
app.MapInsuranceEndpoints();
app.MapCustomerFollowUpEndpoints();
app.MapOperationalReportEndpoints();
app.MapWorkshopEndpoints();
app.MapWorkforceEndpoints();
app.MapTaskReviewEndpoints();
app.MapStaffEndpoints();
app.MapMasterAdminEndpoints();
app.Run();

public partial class Program { }
