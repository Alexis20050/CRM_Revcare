using System.Security.Claims;
using CRM.infrastructure.data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.api;

public sealed record BackendAccess(string Scope) : IAuthorizationRequirement;

public sealed class BackendAccessHandler(PlatformMasterDbContext master, UserManager<IdentityUser> users)
    : AuthorizationHandler<BackendAccess>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BackendAccess requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return;

        var user = await users.FindByIdAsync(userId);
        if (user == null || user.SecurityStamp != context.User.FindFirstValue("security_stamp") ||
            (user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow)) return;

        if (requirement.Scope == "master")
        {
            if (context.User.FindFirstValue("tenant_id") == null && await users.IsInRoleAsync(user, "SuperAdmin"))
                context.Succeed(requirement);
            return;
        }

        string tenantId = context.User.FindFirstValue("tenant_id") ?? "";
        if (string.IsNullOrWhiteSpace(tenantId)) return;

        var tenant = await master.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.TenantId == tenantId && t.IsActive);
        if (tenant == null || string.Equals(tenant.Status, "Suspended", StringComparison.OrdinalIgnoreCase)) return;

        var activeSub = await master.Subscriptions.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Status == "Active")
            .FirstOrDefaultAsync();
        if (activeSub == null && await master.Subscriptions.AsNoTracking().AnyAsync(s => s.TenantId == tenantId))
            return;

        var member = await master.Memberships.AsNoTracking().SingleOrDefaultAsync(m =>
            m.UserId == user.Id && m.TenantId == tenantId && m.IsActive);
        if (member == null) return;
        var baseRole = TenantPermissions.ExtractBaseRole(member.Role);
        if (baseRole != context.User.FindFirstValue("tenant_role")) return;
        if (!TenantPermissions.Roles.Contains(baseRole)) return;

        if (context.Resource is HttpContext http)
        {
            if (!TenantPermissions.Allows(http, baseRole)) return;

            string? requiredModule = TenantPermissions.RequiredModule(http);
            if (requiredModule != null)
            {
                if (!string.Equals(tenant.Status, "Ready", StringComparison.OrdinalIgnoreCase)) return;

                var entitlement = await master.ModuleEntitlements.AsNoTracking()
                    .SingleOrDefaultAsync(e => e.TenantId == tenantId && e.ModuleId == requiredModule);
                if (entitlement == null || !entitlement.IsEnabled) return;
            }
        }

        context.Succeed(requirement);
    }
}

public sealed class TenantRequestContext
{
    public TenantCrmDbContext? Database { get; set; }
}

public static class BackendAuthentication
{
    public static void MapBackendAuthentication(this WebApplication app)
    {
        app.MapPost("/auth/resolve-tenants", async (ResolveTenantsRequest request, UserManager<IdentityUser> users,
            SignInManager<IdentityUser> signIn, PlatformMasterDbContext master) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
                return Results.Unauthorized();

            var user = await users.FindByNameAsync(request.Username.Trim());
            if (user == null) return Results.Unauthorized();

            var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded || user.TwoFactorEnabled) return Results.Unauthorized();

            bool isSuperAdmin = await users.IsInRoleAsync(user, "SuperAdmin");
            var userMemberships = await (from m in master.Memberships.AsNoTracking()
                join t in master.Tenants.AsNoTracking() on m.TenantId equals t.TenantId
                where m.UserId == user.Id && m.IsActive
                select new
                {
                    t.TenantId,
                    t.DisplayName,
                    t.CompanyCode,
                    t.CurrentPlanId,
                    m.Role,
                    BaseRole = TenantPermissions.ExtractBaseRole(m.Role),
                    BranchCode = TenantPermissions.ExtractBranchCode(m.Role),
                    t.Status,
                    t.IsActive
                }).ToListAsync();

            return Results.Ok(new
            {
                userId = user.Id,
                username = user.UserName,
                isSuperAdmin,
                tenants = userMemberships
            });
        }).AllowAnonymous();

        app.MapPost("/auth/login", async (LoginRequest request, UserManager<IdentityUser> users,
            SignInManager<IdentityUser> signIn, PlatformMasterDbContext master, CRM.infrastructure.services.ITenantDbContextFactory tenantDbFactory) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password)) return Results.Unauthorized();
            var user = await users.FindByNameAsync(request.Username.Trim());
            if (user == null) return Results.Unauthorized();
            var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded || user.TwoFactorEnabled) return Results.Unauthorized();
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName ?? user.Id),
                new("security_stamp", user.SecurityStamp ?? "")
            };
            if (request.TenantId == null)
            {
                if (!await users.IsInRoleAsync(user, "SuperAdmin")) return Results.Forbid();
                claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
            }
            else
            {
                var tenant = await master.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.TenantId == request.TenantId && t.IsActive);
                if (tenant == null) return Results.Forbid();
                if (string.Equals(tenant.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
                    return Results.Problem("Tenant access is suspended.", statusCode: 403);

                var member = await master.Memberships.AsNoTracking().SingleOrDefaultAsync(m =>
                    m.UserId == user.Id && m.TenantId == request.TenantId && m.IsActive);
                if (member == null) return Results.Forbid();

                var baseRole = TenantPermissions.ExtractBaseRole(member.Role);
                var branchCode = TenantPermissions.ExtractBranchCode(member.Role);
                if (!TenantPermissions.Roles.Contains(baseRole)) return Results.Forbid();

                var sub = await master.Subscriptions.AsNoTracking()
                    .Where(s => s.TenantId == request.TenantId && s.Status == "Active")
                    .OrderByDescending(s => s.StartDateUtc)
                    .FirstOrDefaultAsync();
                if (sub == null && await master.Subscriptions.AsNoTracking().AnyAsync(s => s.TenantId == request.TenantId))
                    return Results.Problem("Tenant subscription is inactive or suspended.", statusCode: 403);

                if (request.TenantId == "tenant-a")
                {
                    try { await using var verified = await tenantDbFactory.CreateAsync(request.TenantId); }
                    catch { return Results.Problem("Tenant A database is unavailable or its schema is incompatible. Workspace access is blocked.", statusCode: 503); }
                }

                claims.Add(new Claim("tenant_id", member.TenantId));
                claims.Add(new Claim("tenant_role", baseRole));
                claims.Add(new Claim("raw_role", member.Role));
                claims.Add(new Claim("tenant_status", tenant.Status));
                claims.Add(new Claim("subscription_plan", sub?.PlanId ?? tenant.CurrentPlanId));

                if (!string.IsNullOrWhiteSpace(branchCode))
                {
                    claims.Add(new Claim("branch_code", branchCode));
                    try
                    {
                        using var tenantDb = await tenantDbFactory.CreateAsync(request.TenantId);
                        var branch = await tenantDb.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.BranchCode == branchCode);
                        if (branch != null)
                        {
                            claims.Add(new Claim("branch_id", branch.BranchId.ToString()));
                        }
                    }
                    catch
                    {
                        // Fallback when branch catalog is uninitialized
                    }
                }
            }
            return Results.SignIn(new ClaimsPrincipal(new ClaimsIdentity(claims,
                Microsoft.AspNetCore.Authentication.BearerToken.BearerTokenDefaults.AuthenticationScheme)),
                authenticationScheme: Microsoft.AspNetCore.Authentication.BearerToken.BearerTokenDefaults.AuthenticationScheme);
        }).AllowAnonymous();

        app.MapGet("/auth/me", async (ClaimsPrincipal user, PlatformMasterDbContext master) =>
        {
            string? tenantId = user.FindFirstValue("tenant_id");
            string? role = user.FindFirstValue("tenant_role") ?? user.FindFirstValue(ClaimTypes.Role);
            string? rawRole = user.FindFirstValue("raw_role");
            string? branchCode = user.FindFirstValue("branch_code");
            string? branchId = user.FindFirstValue("branch_id");
            string? status = user.FindFirstValue("tenant_status");
            string? plan = user.FindFirstValue("subscription_plan");

            List<string> enabledModules = [];
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                enabledModules = await master.ModuleEntitlements.AsNoTracking()
                    .Where(e => e.TenantId == tenantId && e.IsEnabled)
                    .Select(e => e.ModuleId)
                    .ToListAsync();
            }

            return Results.Ok(new
            {
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
                TenantId = tenantId,
                Role = role,
                RawRole = rawRole,
                BranchCode = branchCode,
                BranchId = branchId,
                Status = status,
                SubscriptionPlan = plan,
                Permissions = TenantPermissions.ForRole(user.FindFirstValue("tenant_role") ?? ""),
                EnabledModules = enabledModules
            });
        }).RequireAuthorization();

        app.MapGet("/crm/modules", async (ClaimsPrincipal user, PlatformMasterDbContext master) =>
        {
            string tenantId = user.FindFirstValue("tenant_id") ?? "";
            var modules = await (from m in master.Modules.AsNoTracking()
                join e in master.ModuleEntitlements.AsNoTracking().Where(x => x.TenantId == tenantId) on m.ModuleId equals e.ModuleId into entitlements
                from ent in entitlements.DefaultIfEmpty()
                select new
                {
                    m.ModuleId,
                    m.DisplayName,
                    m.Description,
                    m.IsImplemented,
                    m.RequiredDependencies,
                    IsEnabled = ent != null && ent.IsEnabled
                }).ToListAsync();
            return Results.Ok(modules);
        }).RequireAuthorization("TenantOperations");
    }
}

public sealed record LoginRequest(string Username, string Password, string? TenantId);
public sealed record ResolveTenantsRequest(string Username, string Password);
