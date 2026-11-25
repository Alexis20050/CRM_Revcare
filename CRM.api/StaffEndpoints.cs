using System.Data;
using System.Security.Claims;
using CRM.infrastructure.data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.api;

public static class StaffEndpoints
{
    public static void MapStaffEndpoints(this WebApplication app)
    {
        var crm = app.MapGroup("/crm/staff").RequireAuthorization("TenantOperations");

        crm.MapGet("", async (PlatformMasterDbContext master, TenantRequestContext tenantContext, ClaimsPrincipal actor) =>
        {
            string tenantId = actor.FindFirstValue("tenant_id") ?? "";
            var members = await (from m in master.Memberships.AsNoTracking()
                                 join u in master.Users on m.UserId equals u.Id
                                 where m.TenantId == tenantId
                                 select new
                                 {
                                     m.UserId,
                                     u.UserName,
                                     m.Role,
                                     m.DisplayName,
                                     m.ProfileProvisioningStatus,
                                     m.ProvisioningFailureReason,
                                     m.IsActive,
                                     m.RowVersion
                                 }).ToListAsync();

            List<TenantStaff>? profiles = null;
            if (tenantContext.Database != null)
            {
                try { profiles = await tenantContext.Database.Staff.AsNoTracking().ToListAsync(); } catch { }
            }

            return Results.Ok(members.Select(m => new
            {
                m.UserId,
                m.UserName,
                m.Role,
                m.IsActive,
                m.RowVersion,
                DisplayName = profiles?.FirstOrDefault(p => p.UserId == m.UserId)?.DisplayName
                    ?? (string.IsNullOrWhiteSpace(m.DisplayName) ? m.UserName : m.DisplayName),
                ProfileProvisioningStatus = m.ProfileProvisioningStatus,
                ProvisioningFailureReason = m.ProvisioningFailureReason
            }));
        });

        crm.MapPost("", async (CreateStaff request, PlatformMasterDbContext master, TenantRequestContext tenantContext,
            UserManager<IdentityUser> users, ClaimsPrincipal actor) =>
        {
            string tenantId = actor.FindFirstValue("tenant_id") ?? "";
            string actorName = actor.FindFirstValue(ClaimTypes.Name) ?? actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";

            if (string.Equals(request.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "Tenant administrator cannot create a SuperAdmin account." });

            if (!TenantPermissions.Roles.Contains(request.Role) || string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 150)
                return Results.BadRequest(new { error = "Choose a valid tenant role and enter a staff name (up to 150 characters)." });

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrEmpty(request.Password))
                return Results.BadRequest(new { error = "Username and initial password are required." });

            // Safeguard: restrict OwnerManager grants to explicit authorization
            if (request.Role == "OwnerManager" && !request.AllowOwnerGrant)
                return Results.BadRequest(new { error = "Assigning OwnerManager role requires explicit owner-grant confirmation." });

            var existingUser = await users.FindByNameAsync(request.Username.Trim());
            IdentityUser user;
            if (existingUser != null)
            {
                // Check if user is already a member of this tenant
                if (await master.Memberships.AnyAsync(m => m.TenantId == tenantId && m.UserId == existingUser.Id))
                    return Results.Conflict(new { error = "This user is already a member of this tenant." });
                user = existingUser;
            }
            else
            {
                user = new IdentityUser(request.Username.Trim()) { LockoutEnabled = true };
                var createResult = await users.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                    return Results.BadRequest(new { error = string.Join(" ", createResult.Errors.Select(e => e.Description)) });
            }

            var now = DateTime.UtcNow;
            var db = tenantContext.Database;
            string profileStatus = db != null ? "Synced" : "Pending";
            string? failureReason = db != null ? null : "Tenant database is pending or unreachable.";

            var membership = new TenantMembership
            {
                UserId = user.Id,
                TenantId = tenantId,
                Role = request.Role,
                DisplayName = request.DisplayName.Trim(),
                ProfileProvisioningStatus = profileStatus,
                ProvisioningFailureReason = failureReason,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await master.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var tx = await master.Database.BeginTransactionAsync();
                master.Memberships.Add(membership);
                master.StaffAccessAudits.Add(new StaffAccessAudit
                {
                    TenantId = tenantId,
                    UserId = user.Id,
                    Actor = actorName,
                    Change = $"Created staff {request.DisplayName} ({request.Role}) - Profile: {profileStatus}",
                    OccurredAtUtc = now
                });
                await master.SaveChangesAsync();
                await tx.CommitAsync();
            });

            if (db != null)
            {
                try
                {
                    if (!await db.Staff.AnyAsync(s => s.UserId == user.Id))
                    {
                        db.Staff.Add(new TenantStaff { UserId = user.Id, DisplayName = request.DisplayName.Trim() });
                        await db.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Update master membership to Pending if tenant DB write failed
                    membership.ProfileProvisioningStatus = "Pending";
                    membership.ProvisioningFailureReason = "Database write failed: " + ex.Message;
                    await master.SaveChangesAsync();
                }
            }

            return Results.Created("/crm/staff/" + user.Id, new
            {
                user.Id,
                user.UserName,
                request.Role,
                request.DisplayName,
                ProfileProvisioningStatus = membership.ProfileProvisioningStatus,
                ProvisioningFailureReason = membership.ProvisioningFailureReason
            });
        });

        crm.MapPut("/{userId}", async (string userId, UpdateStaff request, PlatformMasterDbContext master,
            UserManager<IdentityUser> users, TenantRequestContext tenantContext, ClaimsPrincipal actor) =>
        {
            string tenantId = actor.FindFirstValue("tenant_id") ?? "";
            string actorId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            string actorName = actor.FindFirstValue(ClaimTypes.Name) ?? actorId;

            if (!TenantPermissions.Roles.Contains(request.Role) || string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 150 || request.RowVersion is not { Length: > 0 })
                return Results.BadRequest(new { error = "Choose a tenant role, enter a staff name and reload the current record version." });

            if (userId == actorId)
                return Results.Conflict(new { error = "Ask another owner to change your access. You cannot change your own role or activation." });

            var db = tenantContext.Database;

            return await master.Database.CreateExecutionStrategy().ExecuteAsync<IResult>(async () =>
            {
                await using var tx = await master.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var member = await master.Memberships.SingleOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId);
                if (member == null) return Results.NotFound();

                // Safeguard: promoting to OwnerManager via regular update requires explicit owner-grant confirmation
                if (request.Role == "OwnerManager" && member.Role != "OwnerManager" && !request.AllowOwnerGrant)
                    return Results.BadRequest(new { error = "Promoting a staff member to OwnerManager requires an explicit owner-grant confirmation." });

                // Prevent removal of the last active owner
                if (member.IsActive && member.Role == "OwnerManager" && (!request.IsActive || request.Role != "OwnerManager") &&
                    await master.Memberships.CountAsync(m => m.TenantId == tenantId && m.Role == "OwnerManager" && m.IsActive) <= 1)
                    return Results.Conflict(new { error = "Keep at least one active Owner/Manager." });

                // Check unfinished tasks
                if (db != null && (!request.IsActive || request.Role != member.Role))
                {
                    bool hasUnfinished = await (from a in db.TaskAssignments
                                                join t in db.JobTasks on a.JobTaskId equals t.JobTaskId
                                                where a.UserId == userId && t.Status != "Done"
                                                select a).AnyAsync();
                    if (hasUnfinished)
                        return Results.Conflict(new { error = "Reassign this staff member's unfinished tasks before changing their access." });
                }

                master.Entry(member).Property(m => m.RowVersion).OriginalValue = request.RowVersion;
                master.StaffAccessAudits.Add(new StaffAccessAudit
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Actor = actorName,
                    Change = $"Role {member.Role} -> {request.Role}; active {member.IsActive} -> {request.IsActive}; name -> {request.DisplayName}",
                    OccurredAtUtc = DateTime.UtcNow
                });

                bool accessChanged = member.IsActive != request.IsActive || member.Role != request.Role;
                member.Role = request.Role;
                member.IsActive = request.IsActive;
                member.DisplayName = request.DisplayName.Trim();
                member.UpdatedAtUtc = DateTime.UtcNow;

                await master.SaveChangesAsync();
                await tx.CommitAsync();

                if (db != null)
                {
                    try
                    {
                        var profile = await db.Staff.FindAsync(userId);
                        if (profile == null) db.Staff.Add(new TenantStaff { UserId = userId, DisplayName = request.DisplayName.Trim() });
                        else profile.DisplayName = request.DisplayName.Trim();
                        await db.SaveChangesAsync();
                        member.ProfileProvisioningStatus = "Synced";
                        member.ProvisioningFailureReason = null;
                        await master.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        member.ProfileProvisioningStatus = "Pending";
                        member.ProvisioningFailureReason = "Profile sync failed: " + ex.Message;
                        await master.SaveChangesAsync();
                    }
                }

                // Invalidate existing sessions on role change or deactivation
                if (accessChanged)
                {
                    var user = await users.FindByIdAsync(userId);
                    if (user != null) await users.UpdateSecurityStampAsync(user);
                }

                return Results.Ok(new
                {
                    member.UserId,
                    member.Role,
                    member.IsActive,
                    member.DisplayName,
                    ProfileProvisioningStatus = member.ProfileProvisioningStatus,
                    member.RowVersion
                });
            });
        });

        crm.MapPost("/{userId}/retry-profile", async (string userId, PlatformMasterDbContext master,
            TenantRequestContext tenantContext, ClaimsPrincipal actor) =>
        {
            string tenantId = actor.FindFirstValue("tenant_id") ?? "";
            var member = await master.Memberships.SingleOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId);
            if (member == null) return Results.NotFound();

            var db = tenantContext.Database;
            if (db == null)
                return Results.Problem("Tenant database is currently unavailable or provisioning.", statusCode: 503);

            try
            {
                var profile = await db.Staff.FindAsync(userId);
                if (profile == null)
                    db.Staff.Add(new TenantStaff { UserId = userId, DisplayName = member.DisplayName });
                else
                    profile.DisplayName = member.DisplayName;

                await db.SaveChangesAsync();

                member.ProfileProvisioningStatus = "Synced";
                member.ProvisioningFailureReason = null;
                member.UpdatedAtUtc = DateTime.UtcNow;
                await master.SaveChangesAsync();

                return Results.Ok(new { member.UserId, ProfileProvisioningStatus = "Synced", Message = "Staff profile synced successfully." });
            }
            catch (Exception ex)
            {
                member.ProfileProvisioningStatus = "Failed";
                member.ProvisioningFailureReason = ex.Message;
                await master.SaveChangesAsync();
                return Results.Problem("Sync failed: " + ex.Message, statusCode: 500);
            }
        });

        crm.MapPost("/{userId}/promote-owner", async (string userId, PromoteOwnerRequest request, PlatformMasterDbContext master,
            TenantRequestContext tenantContext, ClaimsPrincipal actor) =>
        {
            string tenantId = actor.FindFirstValue("tenant_id") ?? "";
            string actorId = actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            string actorName = actor.FindFirstValue(ClaimTypes.Name) ?? actorId;

            if (userId == actorId)
                return Results.Conflict(new { error = "You cannot promote yourself." });

            if (string.IsNullOrWhiteSpace(request.Reason) || request.RowVersion is not { Length: > 0 })
                return Results.BadRequest(new { error = "A promotion reason and current record version are required." });

            return await master.Database.CreateExecutionStrategy().ExecuteAsync<IResult>(async () =>
            {
                await using var tx = await master.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var member = await master.Memberships.SingleOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId);
                if (member == null) return Results.NotFound();

                if (!member.IsActive)
                    return Results.BadRequest(new { error = "Cannot promote an inactive staff member. Activate them first." });

                if (member.Role == "OwnerManager")
                    return Results.Conflict(new { error = "This staff member is already an Owner/Manager." });

                master.Entry(member).Property(m => m.RowVersion).OriginalValue = request.RowVersion;
                master.StaffAccessAudits.Add(new StaffAccessAudit
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Actor = actorName,
                    Change = $"Explicit Owner Promotion: {member.Role} -> OwnerManager. Reason: {request.Reason.Trim()}",
                    OccurredAtUtc = DateTime.UtcNow
                });

                member.Role = "OwnerManager";
                member.UpdatedAtUtc = DateTime.UtcNow;

                await master.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new
                {
                    member.UserId,
                    member.Role,
                    member.IsActive,
                    member.DisplayName,
                    member.ProfileProvisioningStatus,
                    member.RowVersion,
                    Message = $"Successfully promoted {member.DisplayName} to Owner/Manager."
                });
            });
        });

        crm.MapPost("/{userId}/reset-password", async (string userId, ResetPasswordRequest request,
            PlatformMasterDbContext master, UserManager<IdentityUser> users, ClaimsPrincipal actor) =>
        {
            string tenantId = actor.FindFirstValue("tenant_id") ?? "";
            string actorName = actor.FindFirstValue(ClaimTypes.Name) ?? actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 12)
                return Results.BadRequest(new { error = "New password must be at least 12 characters." });

            var user = await users.FindByIdAsync(userId);
            if (user == null) return Results.NotFound();

            // Privilege protection: Tenant owners cannot reset Super Admin credentials
            if (await users.IsInRoleAsync(user, "SuperAdmin"))
                return Results.Forbid();

            var member = await master.Memberships.SingleOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId);
            if (member == null) return Results.NotFound();

            // Multi-company isolation: An owner of one company cannot compromise a multi-company identity
            bool belongsToOtherCompany = await master.Memberships.AsNoTracking()
                .AnyAsync(m => m.UserId == userId && m.TenantId != tenantId && m.IsActive);
            if (belongsToOtherCompany)
                return Results.Conflict(new { error = "Cannot administratively reset password for a user identity associated with multiple companies. Multi-company users must reset their own credentials or contact platform administration." });

            var token = await users.GeneratePasswordResetTokenAsync(user);
            var result = await users.ResetPasswordAsync(user, token, request.NewPassword);
            if (!result.Succeeded)
            {
                // Fallback for direct administrative reset if token validation has any issue
                var removeResult = await users.RemovePasswordAsync(user);
                if (removeResult.Succeeded)
                {
                    result = await users.AddPasswordAsync(user, request.NewPassword);
                }
            }

            if (!result.Succeeded)
                return Results.BadRequest(new { error = string.Join(" ", result.Errors.Select(e => e.Description)) });

            await users.UpdateSecurityStampAsync(user);

            member.UpdatedAtUtc = DateTime.UtcNow;
            master.StaffAccessAudits.Add(new StaffAccessAudit
            {
                TenantId = tenantId,
                UserId = userId,
                Actor = actorName,
                Change = $"Password reset for staff {member.DisplayName} ({member.Role})",
                OccurredAtUtc = DateTime.UtcNow
            });
            await master.SaveChangesAsync();

            return Results.Ok(new { message = $"Password successfully reset for {member.DisplayName}." });
        });

        crm.MapGet("/access-history", async (PlatformMasterDbContext master, ClaimsPrincipal actor) =>
        {
            string tenantId = actor.FindFirstValue("tenant_id") ?? "";
            return Results.Ok(await master.StaffAccessAudits.AsNoTracking()
                .Where(a => a.TenantId == tenantId)
                .OrderByDescending(a => a.Id)
                .Take(500)
                .ToListAsync());
        });
    }
}

public sealed record CreateStaff(string Username, string Password, string DisplayName, string Role, bool AllowOwnerGrant = false);
public sealed record UpdateStaff(string DisplayName, string Role, bool IsActive, byte[] RowVersion, bool AllowOwnerGrant = false);
public sealed record PromoteOwnerRequest(string Reason, byte[] RowVersion);
public sealed record ResetPasswordRequest(string NewPassword, byte[]? RowVersion = null);
