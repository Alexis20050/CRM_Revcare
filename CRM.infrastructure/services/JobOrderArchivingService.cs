#nullable enable
using System;
using System.Text.Json;
using System.Threading.Tasks;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public static class JobOrderArchivingService
{
    public static async Task<ReadinessResult> ArchiveJobOrderAsync(Func<MasterCrmDbContext> dbFactory, int jobOrderId, string reason, string actor)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ReadinessResult.Denied("A mandatory archive reason is required to archive a Job Order.");

        if (string.IsNullOrWhiteSpace(actor))
            actor = "System Administrator";

        using var preDb = dbFactory();
        var preJob = await preDb.JobOrders.AsNoTracking().FirstOrDefaultAsync(j => j.JobOrderId == jobOrderId);
        if (preJob == null)
            return ReadinessResult.Denied($"Job Order #{jobOrderId} does not exist.");

        if (preJob.IsArchived)
            return ReadinessResult.Denied($"Job Order #{jobOrderId} is already archived.");

        var eligibility = WorkshopReadinessService.EvaluateArchive(preDb, preJob, reason);
        if (!eligibility.IsPermitted)
            return eligibility;

        using var db = dbFactory();
        var strategy = db.Database.CreateExecutionStrategy();
        ReadinessResult result = ReadinessResult.Denied("Unknown failure");

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var job = await db.JobOrders
                .FromSqlInterpolated($"SELECT * FROM JobOrders WITH (UPDLOCK, HOLDLOCK) WHERE JobOrderId = {jobOrderId}")
                .SingleOrDefaultAsync();

            if (job == null)
                throw new InvalidOperationException($"Job Order #{jobOrderId} not found.");

            if (job.IsArchived)
                throw new InvalidOperationException($"Job Order #{jobOrderId} is already archived.");

            var currentEligibility = WorkshopReadinessService.EvaluateArchive(db, job, reason);
            if (!currentEligibility.IsPermitted)
                throw new InvalidOperationException(currentEligibility.Message);

            job.IsArchived = true;
            job.ArchivedAtUtc = DateTime.UtcNow;
            job.ArchivedBy = actor;
            job.ArchiveReason = reason.Trim();

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actor,
                EntityType = "JobOrder",
                Action = "Archived",
                Changes = JsonSerializer.Serialize(new
                {
                    job.JobOrderId,
                    job.Status,
                    Reason = job.ArchiveReason,
                    ArchivedBy = job.ArchivedBy,
                    ArchivedAt = job.ArchivedAtUtc
                })
            });

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            result = ReadinessResult.Allowed($"Job Order #{jobOrderId} was archived successfully.");
        });

        return result;
    }

    public static async Task<ReadinessResult> RestoreJobOrderAsync(Func<MasterCrmDbContext> dbFactory, int jobOrderId, string reason, string actor)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ReadinessResult.Denied("A mandatory restoration reason is required to restore an archived Job Order.");

        if (string.IsNullOrWhiteSpace(actor))
            actor = "System Administrator";

        using var db = dbFactory();
        var strategy = db.Database.CreateExecutionStrategy();
        ReadinessResult result = ReadinessResult.Denied("Unknown failure");

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var job = await db.JobOrders
                .FromSqlInterpolated($"SELECT * FROM JobOrders WITH (UPDLOCK, HOLDLOCK) WHERE JobOrderId = {jobOrderId}")
                .SingleOrDefaultAsync();

            if (job == null)
                throw new InvalidOperationException($"Job Order #{jobOrderId} not found.");

            if (!job.IsArchived)
                throw new InvalidOperationException($"Job Order #{jobOrderId} is not currently archived.");

            job.IsArchived = false;

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actor,
                EntityType = "JobOrder",
                Action = "Restored",
                Changes = JsonSerializer.Serialize(new
                {
                    job.JobOrderId,
                    job.Status,
                    RestorationReason = reason.Trim(),
                    RestoredBy = actor,
                    PreviousArchivedAt = job.ArchivedAtUtc,
                    PreviousArchiveReason = job.ArchiveReason
                })
            });

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            result = ReadinessResult.Allowed($"Job Order #{jobOrderId} was restored successfully. Operational status remains {job.Status}.");
        });

        return result;
    }

    public static async Task<ReadinessResult> ArchiveJobTaskAsync(Func<MasterCrmDbContext> dbFactory, int jobTaskId, string reason, string actor)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ReadinessResult.Denied("A mandatory withdrawal reason is required to archive/withdraw a task.");

        if (string.IsNullOrWhiteSpace(actor))
            actor = "System Administrator";

        using var db = dbFactory();
        var strategy = db.Database.CreateExecutionStrategy();
        ReadinessResult result = ReadinessResult.Denied("Unknown failure");

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var task = await db.JobTasks
                .FromSqlInterpolated($"SELECT * FROM JobTasks WITH (UPDLOCK, HOLDLOCK) WHERE JobTaskId = {jobTaskId}")
                .SingleOrDefaultAsync();

            if (task == null)
                throw new InvalidOperationException($"Job Task #{jobTaskId} not found.");

            if (task.IsArchived)
                throw new InvalidOperationException($"Job Task #{jobTaskId} is already archived/withdrawn.");

            var job = await db.JobOrders.AsNoTracking().FirstOrDefaultAsync(j => j.JobOrderId == task.JobOrderId);
            if (job == null)
                throw new InvalidOperationException("Parent job order not found.");

            if (job.Status is "Completed" or "Released")
                throw new InvalidOperationException($"Parent Job Order is {job.Status}. Tasks cannot be modified or withdrawn on closed jobs.");

            if (task.Status is "Ongoing" or "In Progress")
                throw new InvalidOperationException("In-progress tasks cannot be archived. Complete, cancel, or stop the task first.");

            if (task.Status is "Done" or "Completed")
                throw new InvalidOperationException("Completed service tasks cannot be withdrawn. Completed work must be preserved.");

            task.IsArchived = true;
            task.ArchivedAtUtc = DateTime.UtcNow;
            task.ArchivedBy = actor;
            task.ArchiveReason = reason.Trim();
            task.Status = "Withdrawn";

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actor,
                EntityType = "JobTask",
                Action = "WithdrawnAndArchived",
                Changes = JsonSerializer.Serialize(new
                {
                    task.JobTaskId,
                    task.JobOrderId,
                    task.ServiceId,
                    Reason = task.ArchiveReason,
                    ArchivedBy = task.ArchivedBy
                })
            });

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            result = ReadinessResult.Allowed($"Job Task #{jobTaskId} was withdrawn and archived successfully.");
        });

        return result;
    }

    public static async Task<ReadinessResult> RestoreJobTaskAsync(Func<MasterCrmDbContext> dbFactory, int jobTaskId, string reason, string actor)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return ReadinessResult.Denied("A mandatory restoration reason is required to restore an archived task.");

        if (string.IsNullOrWhiteSpace(actor))
            actor = "System Administrator";

        using var db = dbFactory();
        var strategy = db.Database.CreateExecutionStrategy();
        ReadinessResult result = ReadinessResult.Denied("Unknown failure");

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var task = await db.JobTasks
                .FromSqlInterpolated($"SELECT * FROM JobTasks WITH (UPDLOCK, HOLDLOCK) WHERE JobTaskId = {jobTaskId}")
                .SingleOrDefaultAsync();

            if (task == null)
                throw new InvalidOperationException($"Job Task #{jobTaskId} not found.");

            if (!task.IsArchived)
                throw new InvalidOperationException($"Job Task #{jobTaskId} is not currently archived.");

            task.IsArchived = false;
            task.Status = "Pending";

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actor,
                EntityType = "JobTask",
                Action = "Restored",
                Changes = JsonSerializer.Serialize(new
                {
                    task.JobTaskId,
                    task.JobOrderId,
                    RestorationReason = reason.Trim(),
                    RestoredBy = actor
                })
            });

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            result = ReadinessResult.Allowed($"Job Task #{jobTaskId} was restored to Pending status.");
        });

        return result;
    }
}
