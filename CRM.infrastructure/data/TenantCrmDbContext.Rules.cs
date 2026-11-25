using System.Data;
using crm.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data;

public partial class TenantCrmDbContext
{
    private static DateTime WorkshopNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"));
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        if (Database.CurrentTransaction != null) return SaveWithRules(acceptAllChangesOnSuccess);
        return Database.CreateExecutionStrategy().Execute(() =>
        {
            using var tx = Database.BeginTransaction(IsolationLevel.Serializable);
            int count = SaveWithRules(acceptAllChangesOnSuccess);
            tx.Commit();
            return count;
        });
    }
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Database.CurrentTransaction != null)
        {
            await Task.Run(ValidateRules, cancellationToken);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        return await Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            await Task.Run(ValidateRules, cancellationToken);
            int count = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return count;
        });
    }
    private int SaveWithRules(bool accept)
    {
        ValidateRules();
        return base.SaveChanges(accept);
    }
    private void ValidateRules()
    {
        new WorkshopRules(this, AuditActor).Validate();
        if (!IsLegacyPrototypeTenantA)
        {
            foreach (var entry in ChangeTracker.Entries<Payment>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList())
            {
                var payment = entry.Entity;
                if (payment.JobOrderId > 0)
                {
                    var parentJob = JobOrders.Local.FirstOrDefault(j => j.JobOrderId == payment.JobOrderId)
                        ?? JobOrders.AsNoTracking().FirstOrDefault(j => j.JobOrderId == payment.JobOrderId);
                    if (parentJob != null)
                    {
                        if (payment.BranchId.HasValue && parentJob.BranchId.HasValue && payment.BranchId != parentJob.BranchId)
                        {
                            throw new InvalidOperationException($"Payment branch (#{payment.BranchId}) must match parent Job Order branch (#{parentJob.BranchId}).");
                        }
                        if (!payment.BranchId.HasValue && parentJob.BranchId.HasValue)
                        {
                            payment.BranchId = parentJob.BranchId;
                        }
                    }
                }
            }

            foreach (var entry in ChangeTracker.Entries<Appointment>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList())
            {
                var appt = entry.Entity;
                if (appt.LinkedJobOrderId.HasValue && appt.LinkedJobOrderId.Value > 0)
                {
                    var linkedJob = JobOrders.Local.FirstOrDefault(j => j.JobOrderId == appt.LinkedJobOrderId.Value)
                        ?? JobOrders.AsNoTracking().FirstOrDefault(j => j.JobOrderId == appt.LinkedJobOrderId.Value);
                    if (linkedJob != null)
                    {
                        if (appt.BranchId.HasValue && linkedJob.BranchId.HasValue && appt.BranchId != linkedJob.BranchId)
                        {
                            throw new InvalidOperationException($"Appointment branch (#{appt.BranchId}) must match linked Job Order branch (#{linkedJob.BranchId}).");
                        }
                        if (!appt.BranchId.HasValue && linkedJob.BranchId.HasValue)
                        {
                            appt.BranchId = linkedJob.BranchId;
                        }
                    }
                }
            }
        }
        foreach (var entry in ChangeTracker.Entries<JobTask>().Where(e => e.State == EntityState.Modified).ToList())
        {
            var task = entry.Entity;
            var old = JobTasks.AsNoTracking().Single(t => t.JobTaskId == task.JobTaskId);
            if (old.Status == task.Status && old.Remarks == task.Remarks) continue;
            var work = TaskWork.Find(task.JobTaskId);
            if (work == null)
            {
                work = new TaskWork { JobTaskId = task.JobTaskId, ServiceNameAtApproval = task.ServiceId };
                TaskWork.Add(work); // Historical prices are unknown; never invent them.
            }
            if (!IsReviewOperation)
            {
                if (work.ReviewStatus is "Submitted" or "Approved")
                    throw new InvalidOperationException("This task is awaiting review or already approved. Ask the Lead Mechanic to return it for rework.");
                if (task.Status != old.Status && !((old.Status == "Pending" && task.Status == "Ongoing") || (old.Status == "Ongoing" && task.Status == "Done")))
                    throw new InvalidOperationException("Start the task before marking it done. Only QA can return finished work for rework.");
                work.SubmittedAtUtc = null;
                work.SubmittedBy = null;
                work.ReviewStatus = "Not Submitted";
            }
            if (task.Status == "Ongoing") { work.StartedAtUtc ??= DateTime.UtcNow; work.FinishedAtUtc = null; }
            if (task.Status == "Done" && old.Status != "Done") work.FinishedAtUtc = DateTime.UtcNow;
        }
        foreach (var entry in ChangeTracker.Entries<JobOrder>().Where(e => e.State == EntityState.Modified).ToList())
        {
            var job = entry.Entity;
            string oldStatus = JobOrders.AsNoTracking().Where(j => j.JobOrderId == job.JobOrderId).Select(j => j.Status).Single();
            if (oldStatus == job.Status) continue;
            var tasks = JobTasks.AsNoTracking().Where(t => t.JobOrderId == job.JobOrderId).ToList();
            if (job.Status == "Completed")
            {
                var reviews = TaskWork.AsNoTracking().Where(w => w.Task.JobOrderId == job.JobOrderId).ToList();
                bool valid = tasks.Count > 0 && tasks.All(t => t.Status == "Done" && reviews.Any(w => w.JobTaskId == t.JobTaskId && w.ReviewStatus == "Approved"));
                if (!valid) throw new InvalidOperationException("Approve the final inspection of every task before completing the job.");
            }
        }
    }
}

