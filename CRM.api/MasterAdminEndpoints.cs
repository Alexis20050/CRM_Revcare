using System.Security.Claims;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CRM.api;

public static class MasterAdminEndpoints
{
    public static void MapMasterAdminEndpoints(this WebApplication app)
    {
        var master = app.MapGroup("/master").RequireAuthorization("MasterAdmin");

        // 1. List tenants
        master.MapGet("/tenants", async (PlatformMasterDbContext db) =>
        {
            var tenants = await db.Tenants.AsNoTracking()
                .OrderBy(t => t.TenantId)
                .Select(t => new
                {
                    t.TenantId,
                    t.DisplayName,
                    t.CompanyCode,
                    t.ConnectionKey,
                    t.ServerName,
                    t.DatabaseName,
                    t.CurrentPlanId,
                    t.Status,
                    t.IsActive,
                    t.CreatedAtUtc,
                    t.UpdatedAtUtc
                })
                .ToListAsync();
            return Results.Ok(tenants);
        });

        // 2. Register new tenant
        master.MapPost("/tenants", async (RegisterTenantRequest request, PlatformMasterDbContext db, ClaimsPrincipal actor) =>
        {
            if (string.IsNullOrWhiteSpace(request.TenantId) || request.TenantId.Length > 50)
                return Results.BadRequest(new { error = "TenantId is required (up to 50 characters, URL-safe)." });
            if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 200)
                return Results.BadRequest(new { error = "DisplayName is required (up to 200 characters)." });
            if (string.IsNullOrWhiteSpace(request.ConnectionKey) || request.ConnectionKey.Length > 100)
                return Results.BadRequest(new { error = "ConnectionKey is required (up to 100 characters)." });

            string tenantId = request.TenantId.Trim().ToLowerInvariant();
            if (await db.Tenants.AnyAsync(t => t.TenantId == tenantId))
                return Results.Conflict(new { error = $"Tenant '{tenantId}' already exists." });
            if (await db.Tenants.AnyAsync(t => t.ConnectionKey == request.ConnectionKey.Trim()))
                return Results.Conflict(new { error = $"ConnectionKey '{request.ConnectionKey}' is already registered to another tenant." });

            var now = DateTime.UtcNow;
            var tenant = new TenantRegistration
            {
                TenantId = tenantId,
                DisplayName = request.DisplayName.Trim(),
                ConnectionKey = request.ConnectionKey.Trim(),
                Status = "Provisioning", // Default provisioning status
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            return Results.Created($"/master/tenants/{tenant.TenantId}", tenant);
        });

        // 3. Update tenant status / activation
        master.MapPut("/tenants/{tenantId}/status", async (string tenantId, UpdateTenantStatusRequest request,
            PlatformMasterDbContext db, ClaimsPrincipal actor) =>
        {
            var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant == null) return Results.NotFound();

            var allowedStatuses = new[] { "Ready", "Provisioning", "Suspended" };
            if (!string.IsNullOrWhiteSpace(request.Status) && !allowedStatuses.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "Status must be 'Ready', 'Provisioning', or 'Suspended'." });

            if (!string.IsNullOrWhiteSpace(request.Status))
                tenant.Status = request.Status;

            if (request.IsActive.HasValue)
                tenant.IsActive = request.IsActive.Value;

            tenant.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(tenant);
        });

        // 4. Tenant configuration health check (never exposing passwords)
        master.MapGet("/tenants/{tenantId}/health", async (string tenantId, PlatformMasterDbContext db,
            IConfiguration config) =>
        {
            var tenant = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant == null) return Results.NotFound();

            string connectionKey = tenant.ConnectionKey;
            string? connStr = config.GetConnectionString(connectionKey);
            bool isConfigured = !string.IsNullOrWhiteSpace(connStr);
            bool isReachable = false;
            bool identityMatches = false;
            string failureReason = "";

            if (isConfigured)
            {
                try
                {
                    await using var tenantDb = new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>()
                        .UseSqlServer(connStr, sql => sql.EnableRetryOnFailure(1)).Options);

                    if (await tenantDb.Database.CanConnectAsync())
                    {
                        isReachable = true;
                        var identity = await tenantDb.DatabaseIdentity.AsNoTracking().FirstOrDefaultAsync();
                        if (identity != null && identity.TenantId == tenantId)
                            identityMatches = true;
                        else
                            failureReason = "Database identity marker does not match tenant ID.";
                    }
                    else
                    {
                        failureReason = "Database host is unreachable or database does not exist.";
                    }
                }
                catch (Exception ex)
                {
                    failureReason = ex.Message;
                }
            }
            else
            {
                failureReason = $"Connection string key '{connectionKey}' is not configured.";
            }

            return Results.Ok(new
            {
                tenant.TenantId,
                tenant.DisplayName,
                tenant.Status,
                tenant.IsActive,
                ConnectionKey = tenant.ConnectionKey,
                IsConfigured = isConfigured,
                IsReachable = isReachable,
                IdentityMatches = identityMatches,
                Diagnostics = failureReason
            });
        });

        // 5. Provision or invite initial tenant owner
        master.MapPost("/tenants/{tenantId}/owner", async (string tenantId, ProvisionOwnerRequest request,
            PlatformMasterDbContext db, UserManager<IdentityUser> users, IConfiguration config, ClaimsPrincipal actor) =>
        {
            var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant == null) return Results.NotFound();

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
                return Results.BadRequest(new { error = "Username and password are required." });

            var existingUser = await users.FindByNameAsync(request.Username.Trim());
            IdentityUser user;
            if (existingUser != null)
            {
                user = existingUser;
            }
            else
            {
                user = new IdentityUser(request.Username.Trim()) { LockoutEnabled = true };
                var createResult = await users.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                    return Results.BadRequest(new { error = string.Join("; ", createResult.Errors.Select(e => e.Description)) });
            }

            var membership = await db.Memberships.SingleOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == user.Id);
            if (membership == null)
            {
                membership = new TenantMembership
                {
                    TenantId = tenantId,
                    UserId = user.Id,
                    Role = "OwnerManager",
                    DisplayName = request.DisplayName?.Trim() ?? request.Username.Trim(),
                    ProfileProvisioningStatus = "Pending",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                db.Memberships.Add(membership);
            }
            else
            {
                membership.Role = "OwnerManager";
                membership.IsActive = true;
                membership.DisplayName = request.DisplayName?.Trim() ?? membership.DisplayName;
            }

            // Attempt to write profile into tenant database if configured and reachable
            string? connStr = config.GetConnectionString(tenant.ConnectionKey);
            if (!string.IsNullOrWhiteSpace(connStr))
            {
                try
                {
                    await using var tenantDb = new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>()
                        .UseSqlServer(connStr, sql => sql.EnableRetryOnFailure(1)).Options);
                    if (await tenantDb.Database.CanConnectAsync())
                    {
                        var staff = await tenantDb.Staff.FindAsync(user.Id);
                        if (staff == null)
                            tenantDb.Staff.Add(new TenantStaff { UserId = user.Id, DisplayName = membership.DisplayName });
                        else
                            staff.DisplayName = membership.DisplayName;

                        await tenantDb.SaveChangesAsync();
                        membership.ProfileProvisioningStatus = "Synced";
                        membership.ProvisioningFailureReason = null;
                    }
                }
                catch (Exception ex)
                {
                    membership.ProfileProvisioningStatus = "Pending";
                    membership.ProvisioningFailureReason = "Tenant database write failed: " + ex.Message;
                }
            }
            else
            {
                membership.ProfileProvisioningStatus = "Pending";
                membership.ProvisioningFailureReason = "Tenant database connection is not configured.";
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                user.Id,
                user.UserName,
                tenantId,
                Role = "OwnerManager",
                DisplayName = membership.DisplayName,
                ProfileProvisioningStatus = membership.ProfileProvisioningStatus,
                membership.ProvisioningFailureReason
            });
        });

        // 6. View module catalog
        master.MapGet("/modules", async (PlatformMasterDbContext db) =>
        {
            var modules = await db.Modules.AsNoTracking().ToListAsync();
            return Results.Ok(modules);
        });

        // 7. View tenant's effective module access
        master.MapGet("/tenants/{tenantId}/modules", async (string tenantId, PlatformMasterDbContext db) =>
        {
            var tenant = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant == null) return Results.NotFound();

            var entitlements = await (from m in db.Modules.AsNoTracking()
                                      join e in db.ModuleEntitlements.AsNoTracking().Where(x => x.TenantId == tenantId)
                                      on m.ModuleId equals e.ModuleId into ents
                                      from ent in ents.DefaultIfEmpty()
                                      select new
                                      {
                                          m.ModuleId,
                                          m.DisplayName,
                                          m.Description,
                                          m.IsImplemented,
                                          m.RequiredDependencies,
                                          IsEnabled = ent != null && ent.IsEnabled,
                                          Reason = ent != null ? ent.Reason : "",
                                          UpdatedAtUtc = ent != null ? (DateTime?)ent.UpdatedAtUtc : null,
                                          UpdatedBy = ent != null ? ent.UpdatedBy : "",
                                          RowVersion = ent != null ? ent.RowVersion : null
                                      }).ToListAsync();

            return Results.Ok(entitlements);
        });

        // 8. Enable or disable module for a tenant (with audit and dependency verification)
        master.MapPut("/tenants/{tenantId}/modules/{moduleId}", async (string tenantId, string moduleId,
            UpdateModuleEntitlementRequest request, PlatformMasterDbContext db, ClaimsPrincipal actor) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Results.BadRequest(new { error = "A mandatory reason must be provided for module entitlement changes." });

            var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant == null) return Results.NotFound(new { error = $"Tenant '{tenantId}' not found." });

            var module = await db.Modules.SingleOrDefaultAsync(m => m.ModuleId == moduleId);
            if (module == null) return Results.NotFound(new { error = $"Module '{moduleId}' does not exist in catalog." });

            string actorName = actor.FindFirstValue(ClaimTypes.Name) ?? actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SuperAdmin";
            var now = DateTime.UtcNow;

            if (request.IsEnabled)
            {
                // Verify module is implemented
                if (!module.IsImplemented)
                    return Results.BadRequest(new { error = $"Module '{module.DisplayName}' is not implemented yet and cannot be enabled." });

                // Verify dependencies
                if (!string.IsNullOrWhiteSpace(module.RequiredDependencies))
                {
                    var deps = module.RequiredDependencies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var activeEnts = await db.ModuleEntitlements.AsNoTracking()
                        .Where(e => e.TenantId == tenantId && e.IsEnabled)
                        .Select(e => e.ModuleId)
                        .ToListAsync();

                    var missingDeps = deps.Where(d => !activeEnts.Contains(d)).ToList();
                    if (missingDeps.Count > 0)
                        return Results.BadRequest(new { error = $"Cannot enable '{module.DisplayName}'. Required dependencies not enabled: {string.Join(", ", missingDeps)}." });
                }
            }
            else
            {
                // Verify no dependent modules remain enabled
                var allModules = await db.Modules.AsNoTracking().ToListAsync();
                var activeEnts = await db.ModuleEntitlements.AsNoTracking()
                    .Where(e => e.TenantId == tenantId && e.IsEnabled && e.ModuleId != moduleId)
                    .Select(e => e.ModuleId)
                    .ToListAsync();

                var dependentActive = allModules
                    .Where(m => activeEnts.Contains(m.ModuleId) &&
                                m.RequiredDependencies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(moduleId))
                    .Select(m => m.DisplayName)
                    .ToList();

                if (dependentActive.Count > 0)
                {
                    return Results.Conflict(new
                    {
                        error = $"Cannot disable '{module.DisplayName}' because the following enabled modules depend on it: {string.Join(", ", dependentActive)}. Disable dependent modules first."
                    });
                }
            }

            var entitlement = await db.ModuleEntitlements.SingleOrDefaultAsync(e => e.TenantId == tenantId && e.ModuleId == moduleId);
            if (entitlement == null)
            {
                entitlement = new TenantModuleEntitlement
                {
                    TenantId = tenantId,
                    ModuleId = moduleId,
                    IsEnabled = request.IsEnabled,
                    Reason = request.Reason.Trim(),
                    UpdatedBy = actorName,
                    UpdatedAtUtc = now
                };
                db.ModuleEntitlements.Add(entitlement);
            }
            else
            {
                entitlement.IsEnabled = request.IsEnabled;
                entitlement.Reason = request.Reason.Trim();
                entitlement.UpdatedBy = actorName;
                entitlement.UpdatedAtUtc = now;
            }

            // Record immutable audit history
            db.ModuleAudits.Add(new ModuleAuditEntry
            {
                TenantId = tenantId,
                ModuleId = moduleId,
                Actor = actorName,
                Action = request.IsEnabled ? "Enabled" : "Disabled",
                Reason = request.Reason.Trim(),
                OccurredAtUtc = now
            });

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                entitlement.TenantId,
                entitlement.ModuleId,
                entitlement.IsEnabled,
                entitlement.Reason,
                entitlement.UpdatedBy,
                entitlement.UpdatedAtUtc,
                entitlement.RowVersion
            });
        });

        // 9. View module management and platform audit log
        master.MapGet("/audit", async (string? tenantId, PlatformMasterDbContext db) =>
        {
            var query = db.ModuleAudits.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(tenantId))
                query = query.Where(a => a.TenantId == tenantId);

            var audits = await query.OrderByDescending(a => a.OccurredAtUtc).Take(500).ToListAsync();
            return Results.Ok(audits);
        });

        // 10. List subscription plans
        master.MapGet("/plans", async (PlatformMasterDbContext db) =>
        {
            var plans = await db.SubscriptionPlans.AsNoTracking().OrderBy(p => p.PlanId).ToListAsync();
            return Results.Ok(plans);
        });

        // 11. Get tenant subscription
        master.MapGet("/tenants/{tenantId}/subscription", async (string tenantId, PlatformMasterDbContext db) =>
        {
            var tenant = await db.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant == null) return Results.NotFound();

            var subscription = await db.Subscriptions.AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.Status == "Active")
                .OrderByDescending(s => s.StartDateUtc)
                .FirstOrDefaultAsync();

            var plan = subscription != null ? await db.SubscriptionPlans.AsNoTracking().SingleOrDefaultAsync(p => p.PlanId == subscription.PlanId) : null;
            var audits = await db.SubscriptionAudits.AsNoTracking().Where(a => a.TenantId == tenantId).OrderByDescending(a => a.OccurredAtUtc).Take(50).ToListAsync();

            return Results.Ok(new
            {
                tenant.TenantId,
                tenant.DisplayName,
                tenant.CurrentPlanId,
                Subscription = subscription,
                Plan = plan,
                AuditHistory = audits
            });
        });

        // 12. Upgrade or change tenant subscription plan
        master.MapPut("/tenants/{tenantId}/subscription", async (string tenantId, ChangeSubscriptionRequest request, PlatformMasterDbContext db, ClaimsPrincipal actor) =>
        {
            if (string.IsNullOrWhiteSpace(request.PlanId))
                return Results.BadRequest(new { error = "PlanId is required." });
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Results.BadRequest(new { error = "A mandatory reason must be provided for subscription plan changes." });

            var tenant = await db.Tenants.SingleOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant == null) return Results.NotFound(new { error = $"Tenant '{tenantId}' not found." });

            var newPlan = await db.SubscriptionPlans.SingleOrDefaultAsync(p => p.PlanId == request.PlanId);
            if (newPlan == null) return Results.NotFound(new { error = $"Subscription plan '{request.PlanId}' does not exist." });

            string actorName = actor.FindFirstValue(ClaimTypes.Name) ?? actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SuperAdmin";
            var now = DateTime.UtcNow;

            var existingSub = await db.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == "Active");
            string action = existingSub == null ? "Created" : "Upgraded";

            if (existingSub == null)
            {
                existingSub = new TenantSubscription
                {
                    TenantId = tenantId,
                    PlanId = request.PlanId,
                    Status = "Active",
                    StartDateUtc = now,
                    AutoRenew = true,
                    UpdatedBy = actorName,
                    UpdatedAtUtc = now
                };
                db.Subscriptions.Add(existingSub);
            }
            else
            {
                existingSub.PlanId = request.PlanId;
                existingSub.UpdatedBy = actorName;
                existingSub.UpdatedAtUtc = now;
            }

            tenant.CurrentPlanId = request.PlanId;
            tenant.UpdatedAtUtc = now;

            db.SubscriptionAudits.Add(new SubscriptionAuditEntry
            {
                TenantId = tenantId,
                PlanId = request.PlanId,
                Actor = actorName,
                Action = action,
                Reason = request.Reason.Trim(),
                OccurredAtUtc = now
            });

            // Reconcile module entitlements according to the new plan
            var modules = newPlan.IncludedModules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var modId in modules)
            {
                var ent = await db.ModuleEntitlements.SingleOrDefaultAsync(e => e.TenantId == tenantId && e.ModuleId == modId);
                if (ent == null)
                {
                    db.ModuleEntitlements.Add(new TenantModuleEntitlement
                    {
                        TenantId = tenantId,
                        ModuleId = modId,
                        IsEnabled = true,
                        Reason = $"Entitled per upgrade to {newPlan.PlanName}",
                        UpdatedBy = actorName,
                        UpdatedAtUtc = now
                    });
                }
                else
                {
                    ent.IsEnabled = true;
                    ent.UpdatedAtUtc = now;
                }
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                tenant.TenantId,
                tenant.CurrentPlanId,
                PlanName = newPlan.PlanName,
                existingSub.Status,
                existingSub.StartDateUtc,
                existingSub.UpdatedAtUtc
            });
        });
    }
}

public sealed record RegisterTenantRequest(string TenantId, string DisplayName, string ConnectionKey);
public sealed record UpdateTenantStatusRequest(string? Status, bool? IsActive);
public sealed record ProvisionOwnerRequest(string Username, string Password, string? DisplayName);
public sealed record UpdateModuleEntitlementRequest(bool IsEnabled, string Reason);
public sealed record ChangeSubscriptionRequest(string PlanId, string Reason);
