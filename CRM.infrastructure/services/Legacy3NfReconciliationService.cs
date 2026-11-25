#nullable enable
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public sealed class ReconciliationReport
{
    public bool IsDryRun { get; set; }
    public int TotalJobsEvaluated { get; set; }
    public int AlreadyLinkedCount { get; set; }
    public int NewlyLinkedCount { get; set; }
    public int ExistingEstimateReusedCount { get; set; }
    public int PaymentsCorrectedCount { get; set; }
    public int PaymentsRequiringReviewCount { get; set; }
    public List<UnresolvedJobRecord> UnresolvedJobs { get; set; } = new();
    public List<string> ValidationMessages { get; set; } = new();
}

public sealed record UnresolvedJobRecord(int JobOrderId, string Status,
    decimal? TotalEstimatedCost, decimal TaskSum, decimal? Difference, string Reason);

public static class Legacy3NfReconciliationService
{
    public static Task<ReconciliationReport> Backfill3NfAsync(MasterCrmDbContext db, bool dryRun = false, CancellationToken cancellationToken = default)
        => Reconcile3NfAsync(db, dryRun, cancellationToken);

    // Legacy schema has no verified agreement provenance. Both entry points remain
    // read-only until reviewed evidence and separate production authorization exist.
    public static async Task<ReconciliationReport> Reconcile3NfAsync(MasterCrmDbContext db, bool dryRun = false, CancellationToken cancellationToken = default)
    {
        var report = new ReconciliationReport { IsDryRun = dryRun };
        var jobs = await db.JobOrders.AsNoTracking().Include(j => j.Tasks).OrderBy(j => j.JobOrderId).ToListAsync(cancellationToken);
        report.TotalJobsEvaluated = jobs.Count;
        report.AlreadyLinkedCount = jobs.Count(j => j.ApprovedEstimateId != null);
        foreach (var job in jobs.Where(j => j.ApprovedEstimateId == null))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sum = job.Tasks.Sum(t => t.AgreedPrice ?? 0m);
            report.UnresolvedJobs.Add(new(job.JobOrderId, job.Status, job.TotalEstimatedCost, sum,
                job.TotalEstimatedCost.HasValue ? job.TotalEstimatedCost.Value - sum : null,
                "Historical agreement and approval provenance unverified. Preserve amount; obtain source documents and business review. Null prices remain unknown; sum includes recorded prices only. Do not infer additional charges."));
        }
        report.PaymentsRequiringReviewCount = await db.Payments.AsNoTracking()
            .CountAsync(p => p.PaymentType == "Insurer Share" || p.PaymentType == "Final Balance", cancellationToken);
        report.ValidationMessages.Add("Automatic historical writes disabled, including catalog price backfill, estimate creation/reuse, approval timestamps and payment reclassification. Existing links also require provenance review.");
        return report;
    }
}
