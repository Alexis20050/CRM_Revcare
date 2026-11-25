using System.Data;
using crm.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data;

public partial class MasterCrmDbContext
{
    private static DateTime WorkshopNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"));
    public bool FinalInspectionConfirmed { get; set; }
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureWritableModel();
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
        EnsureWritableModel();
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
    private void EnsureWritableModel()
    {
        if (IsCloudDb80735)
            throw new InvalidOperationException("Cloud compatibility mode is read-only. Approved migration and authenticated workspace validation are required before operational writes.");
    }
    private void ValidateRules() => new WorkshopRules(this, AuditActor).Validate();
}
