using System.Security.Claims;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api;

public static class TaskReviewEndpoints
{
    public static void MapTaskReviewEndpoints(this WebApplication app)
    {
        var crm = app.MapGroup("/crm/job-tasks").RequireAuthorization("TenantOperations");
        crm.MapPost("/{id:int}/submit", async (int id, ReviewRequest request, TenantCrmDbContext db, ClaimsPrincipal user) =>
            await ChangeReview(id, request, db, user, "Submitted"));
        crm.MapPost("/{id:int}/review", async (int id, ReviewRequest request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (request.Result is not ("Approved" or "Rework")) return Results.BadRequest(new { error = "Choose Approve or Return for rework." });
            return await ChangeReview(id, request, db, user, request.Result);
        });
        crm.MapGet("/{id:int}/reviews", async (int id, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var task = await db.JobTasks.FindAsync(id);
            if (task == null) return Results.NotFound();
            if (TenantPermissions.Technician(user))
            {
                string userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var job = await db.JobOrders.AsNoTracking().FirstOrDefaultAsync(j => j.JobOrderId == task.JobOrderId);
                if (job == null || job.AssignedMechanicUserId != userId) return Results.NotFound();
            }
            return Results.Ok(await db.TaskReviews.AsNoTracking().Where(r => r.JobTaskId == id).OrderByDescending(r => r.Id).ToListAsync());
        });
    }
    private static async Task<IResult> ChangeReview(int id, ReviewRequest request, TenantCrmDbContext db, ClaimsPrincipal user, string result)
    {
        if (request.RowVersion is not { Length: > 0 }) return Results.BadRequest(new { error = "Reload the task before saving." });
        if ((request.Notes?.Length ?? 0) > 2000 || result == "Rework" && string.IsNullOrWhiteSpace(request.Notes))
            return Results.BadRequest(new { error = "A rework reason is required; notes must be 2000 characters or fewer." });
        var task = await db.JobTasks.FindAsync(id);
        if (task == null) return Results.NotFound();
        var job = await db.JobOrders.FindAsync(task.JobOrderId);
        if (job == null) return Results.NotFound();
        if (TenantPermissions.Technician(user))
        {
            string userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (job.AssignedMechanicUserId != userId)
                return Results.NotFound(new { error = "You are not authorized to submit tasks for this job. The job is assigned to another mechanic.", code = "STALE_ASSIGNMENT" });
        }
        if (job.Status is "In Queue" or "Completed" or "Released") return Results.Conflict(new { error = "Only active workshop jobs can submit or review tasks." });
        var work = await db.TaskWork.FindAsync(id);
        if (work == null) { work = new TaskWork { JobTaskId = id, ServiceNameAtApproval = task.ServiceId }; db.TaskWork.Add(work); }
        if (result == "Submitted")
        {
            if (task.Status != "Done" || work.ReviewStatus is "Submitted" or "Approved") return Results.Conflict(new { error = "Finish this task before submitting it. Submitted or approved work cannot be submitted twice." });
            work.SubmittedAtUtc = DateTime.UtcNow; work.SubmittedBy = db.AuditActor;
        }
        else
        {
            if (crm.domain.entities.JobOrderWorkflow.Normalize(job.Status) != "In Progress") return Results.Conflict(new { error = "Only jobs in progress can have tasks inspected." });
            if (work.ReviewStatus != "Submitted" && !(result == "Rework" && work.ReviewStatus == "Approved"))
                return Results.Conflict(new { error = "Only submitted tasks can be approved. Submitted or approved tasks can be returned for rework." });
            work.ReviewedAtUtc = DateTime.UtcNow; work.ReviewedBy = db.AuditActor;
            if (result == "Rework") task.Status = "Ongoing";
        }
        db.IsReviewOperation = true;
        work.ReviewStatus = result; work.ReviewNotes = request.Notes?.Trim() ?? "";
        db.TaskReviews.Add(new TaskReview { JobTaskId = id, Actor = db.AuditActor, Result = result, Notes = work.ReviewNotes });
        db.Entry(task).Property(t => t.RowVersion).OriginalValue = request.RowVersion;
        db.Entry(task).Property(t => t.Remarks).IsModified = true;
        await db.SaveChangesAsync();
        return Results.Ok(new { task.JobTaskId, task.Status, task.RowVersion, work.ReviewStatus });
    }
}
public sealed record ReviewRequest(string? Result, string? Notes, byte[] RowVersion);
