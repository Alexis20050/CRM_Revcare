using System.Text.Json;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

// Temporary, reviewed compatibility inventory: exact persisted keys + creation times,
// not an ID range or date cutoff. No price or approval evidence is inferred here.
public static class HistoricalCompatibility
{
    private sealed record Identity(int Id, DateTime CreatedAt);
    private sealed record Inventory(Identity[] Jobs, Identity[] Estimates);
    private static readonly Inventory Baseline = Load();
    private static readonly HashSet<Identity> Jobs = Baseline.Jobs.ToHashSet();
    private static readonly HashSet<Identity> Estimates = Baseline.Estimates.ToHashSet();
    private static Inventory Load()
    {
        using var stream = typeof(HistoricalCompatibility).Assembly.GetManifestResourceStream("CRM.infrastructure.data.HistoricalCompatibilityInventory.json")
            ?? throw new InvalidOperationException("Historical protection inventory missing; cannot safely edit legacy data.");
        return JsonSerializer.Deserialize<Inventory>(stream) ?? throw new InvalidOperationException("Invalid historical protection inventory.");
    }
    public static bool IsProtected(JobOrder? job) => job != null && Jobs.Contains(new(job.JobOrderId, job.CreatedAt));
    public static bool IsProtected(Estimate? estimate) => estimate != null &&
        (Estimates.Contains(new(estimate.EstimateId, estimate.CreatedAt)) || estimate.Notes.Contains("backfilled", StringComparison.OrdinalIgnoreCase) || estimate.Notes.Contains("reconstructed", StringComparison.OrdinalIgnoreCase));
    public static string Qualification(JobOrder? job) => !IsProtected(job) ? "Quotation approval not verified by this view."
        : job!.ApprovedEstimateId.HasValue ? "Legacy record with reconstructed estimate; customer approval unverified. Original job total retained."
        : "Legacy record; verified itemized quotation unavailable. Original job total retained.";
    public static string Reference(JobOrder job) => string.IsNullOrWhiteSpace(job.EstimateId) ? "Not recorded" : job.EstimateId;

    internal static void Validate(MasterCrmDbContext db)
    {
        db.ChangeTracker.DetectChanges();
        JobOrder? Job(int id) => db.JobOrders.FromSqlInterpolated($"SELECT * FROM JobOrders WITH(UPDLOCK,HOLDLOCK) WHERE JobOrderId={id}").IgnoreAutoIncludes().AsNoTracking().SingleOrDefault();
        Estimate? Estimate(int id) => db.Estimates.FromSqlInterpolated($"SELECT * FROM Estimates WITH(UPDLOCK,HOLDLOCK) WHERE EstimateId={id}").IgnoreAutoIncludes().AsNoTracking().SingleOrDefault();
        void Reject() => throw new InvalidOperationException("Historical financial record is protected. Preserve the original total, receipts and itemization; use a separately reviewed correction process. Service notes and payment history remain available.");
        foreach(var entry in db.ChangeTracker.Entries().Where(e=>e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
        {
            if(entry.Entity is JobOrder job && entry.State!=EntityState.Added)
            {
                var old=Job(job.JobOrderId);
                if(IsProtected(old) && (entry.State==EntityState.Deleted || old!.TotalEstimatedCost!=job.TotalEstimatedCost || old.ApprovedEstimateId!=job.ApprovedEstimateId || old.EstimateId!=job.EstimateId || old.CustomerId!=job.CustomerId || old.CreatedAt!=job.CreatedAt)) Reject();
            }
            if(entry.Entity is JobTask task)
            {
                var old=entry.State==EntityState.Added?null:db.JobTasks.AsNoTracking().SingleOrDefault(t=>t.JobTaskId==task.JobTaskId);
                if(IsProtected(Job(task.JobOrderId)) || (old!=null && IsProtected(Job(old.JobOrderId))))
                    if(old==null || entry.State==EntityState.Deleted || old.JobOrderId!=task.JobOrderId || old.ServiceId!=task.ServiceId || old.AgreedPrice!=task.AgreedPrice || old.ServiceCatalogId!=task.ServiceCatalogId || old.ServiceNameSnapshot!=task.ServiceNameSnapshot || old.CreatedAt!=task.CreatedAt) Reject();
            }
            if(entry.Entity is Estimate estimate && entry.State!=EntityState.Added && IsProtected(Estimate(estimate.EstimateId))) Reject();
            if(entry.Entity is EstimateLine line)
            {
                var old=entry.State==EntityState.Added?null:db.EstimateLines.AsNoTracking().SingleOrDefault(l=>l.EstimateLineId==line.EstimateLineId);
                if(IsProtected(Estimate(line.EstimateId)) || (old!=null && IsProtected(Estimate(old.EstimateId)))) Reject();
            }
        }
    }
}
