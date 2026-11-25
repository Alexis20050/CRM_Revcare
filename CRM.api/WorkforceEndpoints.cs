using System.Security.Claims;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api;

public static class WorkforceEndpoints
{
    public static bool CanTransition(ClaimsPrincipal user, string status) => status switch
    {
        "In Progress" => TenantPermissions.Has(user, "Jobs.Start"),
        "QA Checking" or "Completed" => TenantPermissions.Has(user, "Jobs.QA"),
        "Released" => TenantPermissions.Has(user, "Jobs.Release"),
        _ => false
    };
    public static void MapWorkforceEndpoints(this WebApplication app)
    {
        var crm = app.MapGroup("/crm").RequireAuthorization("TenantOperations");
        crm.MapGet("/workshop/jobs/{id:int}/work-order", async (int id, TenantCrmDbContext db, PlatformMasterDbContext master, ClaimsPrincipal user, IConfiguration config) =>
        {
            var job = await db.JobOrders.AsNoTracking().FirstOrDefaultAsync(j => j.JobOrderId == id);
            if (job == null) return Results.NotFound();
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            if (!db.IsLegacyPrototypeTenantA && userBranchId.HasValue && job.BranchId.HasValue && job.BranchId.Value != userBranchId.Value)
            {
                return Results.NotFound();
            }

            string tenantId = user.FindFirst("tenant_id")?.Value
                ?? (await db.DatabaseIdentity.Select(d => d.TenantId).FirstOrDefaultAsync())
                ?? "tenant-a";

            var tenantReg = await master.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.TenantId == tenantId);
            var tenantSection = config.GetSection($"Tenants:{tenantId}");

            string bName = tenantSection["Name"] ?? tenantReg?.DisplayName ?? "RevCare Auto Hub";
            string? bTagline = tenantSection["Tagline"];
            string? bAddress = tenantSection["Address"];
            string? bContact = tenantSection["Contact"];

            var businessDetails = new
            {
                name = bName,
                tagline = bTagline,
                address = bAddress,
                contact = bContact
            };

            string userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            bool isTech = TenantPermissions.Technician(user);

            if (isTech)
            {
                if (job.AssignedMechanicUserId != userId) return Results.Forbid();
            }

            var customerName = await db.Customers.AsNoTracking()
                .Where(c => c.CustomerId == job.CustomerId)
                .Select(c => c.FullName)
                .FirstOrDefaultAsync() ?? "Customer";

            var taskQuery = from task in db.JobTasks.AsNoTracking()
                join profile in db.Staff on job.AssignedMechanicUserId equals profile.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                join work in db.TaskWork on task.JobTaskId equals work.JobTaskId into workRows
                from work in workRows.DefaultIfEmpty()
                where task.JobOrderId == id
                select new
                {
                    task.JobTaskId,
                    task.ServiceId,
                    task.Remarks,
                    task.Status,
                    AssignedUserId = job.AssignedMechanicUserId,
                    AssignedTo = profile == null ? (job.AssignedMechanic ?? "Unassigned") : profile.DisplayName,
                    ReviewStatus = work == null ? "Not Submitted" : work.ReviewStatus
                };

            var tasks = await taskQuery.OrderBy(t => t.JobTaskId).ToListAsync();

            return Results.Ok(new
            {
                jobOrderId = job.JobOrderId,
                estimateReference = job.EstimateNumber,
                customerName,
                vehicleDetails = job.VehicleDisplay,
                intakeNotes = job.InquiryNotes ?? "",
                jobStatus = job.Status,
                businessDetails,
                tasks = tasks.Select(t => new
                {
                    taskId = t.JobTaskId,
                    serviceName = t.ServiceId,
                    assignedTechnician = t.AssignedTo,
                    status = t.Status,
                    reviewStatus = t.ReviewStatus,
                    notes = t.Remarks
                })
            });
        });
        crm.MapGet("/workshop/assignment-history", async (TenantCrmDbContext db) => Results.Ok(await db.AssignmentHistory.AsNoTracking().OrderByDescending(h => h.Id).Take(500).ToListAsync()));
        crm.MapGet("/workshop", async (TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            string id = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            bool assignedOnly = TenantPermissions.Technician(user);
            if (assignedOnly)
            {
                var techQuery = from task in db.JobTasks.AsNoTracking()
                    join job in db.JobOrders on task.JobOrderId equals job.JobOrderId
                    join profile in db.Staff on job.AssignedMechanicUserId equals profile.UserId into profiles
                    from profile in profiles.DefaultIfEmpty()
                    join work in db.TaskWork on task.JobTaskId equals work.JobTaskId into workRows
                    from work in workRows.DefaultIfEmpty()
                    where job.AssignedMechanicUserId == id
                    select new { task, job, profile, work };

                if (!db.IsLegacyPrototypeTenantA && userBranchId.HasValue)
                {
                    techQuery = techQuery.Where(x => x.job.BranchId == userBranchId.Value);
                }

                return Results.Ok(await techQuery
                    .OrderByDescending(x => x.job.JobOrderId).ThenBy(x => x.task.JobTaskId)
                    .Select(x => new {
                        x.task.JobTaskId, x.task.JobOrderId, VehicleDetails = x.job.VehicleDisplay, JobStatus = x.job.Status,
                        Customer = db.Customers.Where(c => c.CustomerId == x.job.CustomerId).Select(c => c.FullName).FirstOrDefault(),
                        x.task.ServiceId, x.task.Remarks, x.task.Status, x.task.RowVersion,
                        AssignedUserId = x.job.AssignedMechanicUserId,
                        AssignedTo = x.profile == null ? (x.job.AssignedMechanic ?? "Unassigned") : x.profile.DisplayName,
                        ReviewStatus = x.work == null ? "Not Submitted" : x.work.ReviewStatus,
                        StartedAtUtc = x.work == null ? null : x.work.StartedAtUtc,
                        FinishedAtUtc = x.work == null ? null : x.work.FinishedAtUtc,
                        SubmittedAtUtc = x.work == null ? null : x.work.SubmittedAtUtc,
                        ReviewNotes = x.work == null ? "" : x.work.ReviewNotes,
                        IntakeNotes = x.job.InquiryNotes,
                        JobTaskCount = db.JobTasks.Count(t => t.JobOrderId == x.job.JobOrderId),
                        JobDoneCount = db.JobTasks.Count(t => t.JobOrderId == x.job.JobOrderId && t.Status == "Done"),
                        JobSubmittedCount = db.TaskWork.Count(w => w.Task.JobOrderId == x.job.JobOrderId && (w.ReviewStatus == "Submitted" || w.ReviewStatus == "Approved")),
                        JobRowVersion = x.job.RowVersion
                    }).ToListAsync());
            }

            var generalQuery = from task in db.JobTasks.AsNoTracking()
                join job in db.JobOrders on task.JobOrderId equals job.JobOrderId
                join profile in db.Staff on job.AssignedMechanicUserId equals profile.UserId into profiles
                from profile in profiles.DefaultIfEmpty()
                join work in db.TaskWork on task.JobTaskId equals work.JobTaskId into workRows
                from work in workRows.DefaultIfEmpty()
                select new { task, job, profile, work };

            if (!db.IsLegacyPrototypeTenantA && userBranchId.HasValue)
            {
                generalQuery = generalQuery.Where(x => x.job.BranchId == userBranchId.Value);
            }

            var rawTasks = await generalQuery
                .OrderByDescending(x => x.job.JobOrderId).ThenBy(x => x.task.JobTaskId)
                .Select(x => new {
                    x.task.JobTaskId, x.task.JobOrderId, VehicleDetails = x.job.VehicleDisplay, JobStatus = x.job.Status,
                    Customer = db.Customers.Where(c => c.CustomerId == x.job.CustomerId).Select(c => c.FullName).FirstOrDefault(),
                    x.task.ServiceId, x.task.Remarks, x.task.Status, x.task.RowVersion,
                    AssignedUserId = x.job.AssignedMechanicUserId,
                    AssignedTo = x.profile == null ? (x.job.AssignedMechanic ?? "Unassigned") : x.profile.DisplayName,
                    ReviewStatus = x.work == null ? "Not Submitted" : x.work.ReviewStatus,
                    StartedAtUtc = x.work == null ? null : x.work.StartedAtUtc,
                    FinishedAtUtc = x.work == null ? null : x.work.FinishedAtUtc,
                    SubmittedAtUtc = x.work == null ? null : x.work.SubmittedAtUtc,
                    ReviewNotes = x.work == null ? "" : x.work.ReviewNotes,
                    IntakeNotes = x.job.InquiryNotes,
                    JobTaskCount = db.JobTasks.Count(t => t.JobOrderId == x.job.JobOrderId),
                    JobDoneCount = db.JobTasks.Count(t => t.JobOrderId == x.job.JobOrderId && t.Status == "Done"),
                    JobSubmittedCount = db.TaskWork.Count(w => w.Task.JobOrderId == x.job.JobOrderId && (w.ReviewStatus == "Submitted" || w.ReviewStatus == "Approved")),
                    JobRowVersion = x.job.RowVersion
                }).ToListAsync();

            var emptyJobs = await (from job in db.JobOrders.AsNoTracking()
                where !db.JobTasks.Any(t => t.JobOrderId == job.JobOrderId)
                orderby job.JobOrderId descending
                select new {
                    JobTaskId = 0,
                    JobOrderId = job.JobOrderId,
                    VehicleDetails = job.VehicleDisplay,
                    JobStatus = job.Status,
                    Customer = db.Customers.Where(c => c.CustomerId == job.CustomerId).Select(c => c.FullName).FirstOrDefault(),
                    ServiceId = "(No services requested)",
                    Remarks = "",
                    Status = "Pending",
                    RowVersion = job.RowVersion,
                    AssignedUserId = (string?)null,
                    AssignedTo = "Unassigned",
                    ReviewStatus = "Not Submitted",
                    StartedAtUtc = (DateTime?)null,
                    FinishedAtUtc = (DateTime?)null,
                    SubmittedAtUtc = (DateTime?)null,
                    ReviewNotes = "",
                    IntakeNotes = job.InquiryNotes,
                    JobTaskCount = 0,
                    JobDoneCount = 0,
                    JobSubmittedCount = 0,
                    JobRowVersion = job.RowVersion
                }).ToListAsync();

            var combined = rawTasks.Concat(emptyJobs).OrderByDescending(r => r.JobOrderId).ThenBy(r => r.JobTaskId).ToList();
            return Results.Ok(combined);
        });
        crm.MapPost("/job-orders/{id:int}/status", async (int id, JobStatusChange request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (!CanTransition(user, request.Status)) return Results.Forbid();
            var job = await db.JobOrders.FindAsync(id);
            if (job == null) return Results.NotFound();
            if (request.RowVersion is not { Length: > 0 }) return Results.BadRequest(new { error = "Reload this job before changing its status." });
            db.Entry(job).Property(x => x.RowVersion).OriginalValue = request.RowVersion;
            job.Status = request.Status;
            await db.SaveChangesAsync();
            // Workshop responses deliberately exclude financial and customer contact data.
            return Results.Ok(new { job.JobOrderId, job.Status, job.CompletedAt, job.RowVersion });
        });
        crm.MapGet("/assignees", async (PlatformMasterDbContext master, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            string tenantId = user.FindFirst("tenant_id")?.Value
                ?? (await db.DatabaseIdentity.Select(d => d.TenantId).FirstOrDefaultAsync())
                ?? "tenant-a";
            var members = await master.Memberships.AsNoTracking().Where(m => m.TenantId == tenantId && m.IsActive &&
                (m.Role == "LeadMechanic" || m.Role == "AutomotiveMechanic" || m.Role == "DetailingTechnician")).ToListAsync();
            var profiles = await db.Staff.AsNoTracking().ToListAsync();
            return Results.Ok(members.Select(m => new { m.UserId, m.Role, DisplayName = profiles.FirstOrDefault(p => p.UserId == m.UserId)?.DisplayName ?? "Staff profile required" }));
        });
        crm.MapPut("/job-tasks/{id:int}/assignment", (int id) =>
            Results.Conflict(new { error = "Individual task assignment is retired. One mechanic is responsible for the entire Job Order. Use PUT /crm/job-orders/{jobId}/assignment.", code = "WHOLE_JOB_ASSIGNMENT_REQUIRED" }));
        crm.MapPut("/job-orders/{id:int}/assignment", async (int id, AssignJob request, TenantCrmDbContext db, PlatformMasterDbContext master, ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 500) return Results.BadRequest(new { error = "Enter an assignment reason (up to 500 characters)." });
            string tenantId = user.FindFirst("tenant_id")?.Value
                ?? (await db.DatabaseIdentity.Select(d => d.TenantId).FirstOrDefaultAsync())
                ?? "tenant-a";

            return await master.Database.CreateExecutionStrategy().ExecuteAsync<IResult>(async Task<IResult> () =>
            {
                await using var membershipLock = await master.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                var member = await master.Memberships.AsNoTracking().SingleOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == request.UserId && m.IsActive);
                if (member == null || member.Role is not ("LeadMechanic" or "AutomotiveMechanic" or "DetailingTechnician"))
                    return Results.BadRequest(new { error = "Choose an active workshop staff member." });
                if (!await db.Staff.AnyAsync(s => s.UserId == request.UserId)) return Results.BadRequest(new { error = "Create the staff profile first." });
                var job = await db.JobOrders.FindAsync(id);
                if (job == null) return Results.NotFound();
                if (job.Status is "Completed" or "Released") return Results.Conflict(new { error = "Completed or released jobs cannot be reassigned." });
                if (request.RowVersion is not { Length: > 0 }) return Results.BadRequest(new { error = "Reload this job before assigning it." });

                var staffProfile = await db.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == request.UserId);
                string mechanicName = staffProfile?.DisplayName ?? request.UserId;
                string? previousMechanicId = job.AssignedMechanicUserId;
                string? previousMechanicName = job.AssignedMechanic;
                bool isReassign = !string.IsNullOrWhiteSpace(previousMechanicId) && previousMechanicId != request.UserId;

                db.Entry(job).Property(j => j.RowVersion).OriginalValue = request.RowVersion;
                job.AssignedMechanicUserId = request.UserId;
                job.AssignedMechanic = mechanicName;

                var tasks = await db.JobTasks.Where(t => t.JobOrderId == id).ToListAsync();
                foreach (var task in tasks)
                {
                    var assignment = await db.TaskAssignments.FindAsync(task.JobTaskId);
                    db.AssignmentHistory.Add(new AssignmentHistory { JobTaskId = task.JobTaskId, PreviousUserId = assignment?.UserId,
                        UserId = request.UserId, Actor = db.AuditActor, Reason = request.Reason.Trim(), ChangedAt = DateTime.UtcNow });
                    if (assignment == null) db.TaskAssignments.Add(new TaskAssignment { JobTaskId = task.JobTaskId, UserId = request.UserId });
                    else assignment.UserId = request.UserId;

                    db.Entry(task).Property(t => t.Remarks).IsModified = true;
                }

                db.AuditEntries.Add(new AuditEntry
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    Actor = db.AuditActor,
                    EntityType = "JobOrder",
                    Action = isReassign ? "ReassignMechanic" : "AssignMechanic",
                    Changes = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        job.JobOrderId,
                        PreviousMechanicId = previousMechanicId,
                        PreviousMechanicName = previousMechanicName,
                        NewMechanicId = request.UserId,
                        NewMechanicName = mechanicName,
                        Reason = request.Reason.Trim()
                    })
                });

                await db.SaveChangesAsync();
                await membershipLock.CommitAsync();
                return Results.Ok(new { job.JobOrderId, job.AssignedMechanicUserId, AssignedMechanic = job.AssignedMechanic, job.RowVersion });
            });
        });
        crm.MapGet("/audit", async (TenantCrmDbContext db) => Results.Ok(await db.AuditEntries.AsNoTracking().OrderByDescending(a => a.AuditEntryId).Take(500).ToListAsync()));
        crm.MapGet("/services", async (TenantCrmDbContext db) => Results.Ok(await db.Services.AsNoTracking().OrderBy(s => s.Name).ToListAsync()));
        crm.MapPost("/services", async (ServiceWrite request, TenantCrmDbContext db) =>
        {
            if (!ValidService(request)) return Results.BadRequest(new { error = "Enter a service name and a nonnegative price with at most two decimal places." });
            var service = new ServiceOffering { Name = request.Name.Trim(), Price = request.Price, IsActive = request.IsActive };
            db.Services.Add(service); await db.SaveChangesAsync(); return Results.Ok(service);
        });
        crm.MapPut("/services/{id:int}", async (int id, ServiceWrite request, TenantCrmDbContext db) =>
        {
            if (!ValidService(request) || request.RowVersion is not { Length: > 0 }) return Results.BadRequest(new { error = "Check the service name, price and record version." });
            var service = await db.Services.FindAsync(id);
            if (service == null) return Results.NotFound();
            db.Entry(service).Property(s => s.RowVersion).OriginalValue = request.RowVersion;
            service.Name = request.Name.Trim(); service.Price = request.Price; service.IsActive = request.IsActive;
            await db.SaveChangesAsync(); return Results.Ok(service);
        });
    }
    private static bool ValidService(ServiceWrite r) => !string.IsNullOrWhiteSpace(r.Name) && r.Name.Trim().Length <= 150 && r.Price >= 0 && decimal.Round(r.Price, 2) == r.Price;
}
public sealed record JobStatusChange(string Status, byte[] RowVersion);
public sealed record AssignJob(string UserId, string Reason, byte[] RowVersion);
public sealed record AssignTask(string UserId, string Reason, byte[] RowVersion);
public sealed record ServiceWrite(string Name, decimal Price, bool IsActive, byte[] RowVersion);
