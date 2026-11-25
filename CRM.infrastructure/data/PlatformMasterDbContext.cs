using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data;

// MasterCrmDbContext remains the legacy desktop context until its explicit cutover.
public sealed class PlatformMasterDbContext(DbContextOptions<PlatformMasterDbContext> options) : IdentityDbContext(options)
{
    public DbSet<TenantRegistration> Tenants => Set<TenantRegistration>();
    public DbSet<TenantMembership> Memberships => Set<TenantMembership>();
    public DbSet<StaffAccessAudit> StaffAccessAudits => Set<StaffAccessAudit>();
    public DbSet<ModuleDefinition> Modules => Set<ModuleDefinition>();
    public DbSet<TenantModuleEntitlement> ModuleEntitlements => Set<TenantModuleEntitlement>();
    public DbSet<ModuleAuditEntry> ModuleAudits => Set<ModuleAuditEntry>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TenantSubscription> Subscriptions => Set<TenantSubscription>();
    public DbSet<SubscriptionAuditEntry> SubscriptionAudits => Set<SubscriptionAuditEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TenantRegistration>(e =>
        {
            e.HasKey(x => x.TenantId);
            e.Property(x => x.TenantId).HasMaxLength(50);
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.Property(x => x.CompanyCode).HasMaxLength(50).IsRequired().HasDefaultValue("");
            e.Property(x => x.ConnectionKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.ServerName).HasMaxLength(200).IsRequired().HasDefaultValue(@"(localdb)\MSSQLLocalDB");
            e.Property(x => x.DatabaseName).HasMaxLength(200).IsRequired().HasDefaultValue("");
            e.Property(x => x.CurrentPlanId).HasMaxLength(50).IsRequired().HasDefaultValue("PlanA");
            e.Property(x => x.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Provisioning");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAtUtc).IsRequired();
            e.Property(x => x.UpdatedAtUtc).IsRequired();
            e.HasIndex(x => x.ConnectionKey).IsUnique();
        });

        builder.Entity<TenantMembership>(e =>
        {
            e.HasKey(x => new { x.TenantId, x.UserId });
            e.Property(x => x.TenantId).HasMaxLength(50);
            e.Property(x => x.UserId).HasMaxLength(450);
            e.Property(x => x.Role).HasMaxLength(50).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired().HasDefaultValue("");
            e.Property(x => x.ProfileProvisioningStatus).HasMaxLength(50).IsRequired().HasDefaultValue("Synced");
            e.Property(x => x.ProvisioningFailureReason).HasMaxLength(500);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAtUtc).IsRequired();
            e.Property(x => x.UpdatedAtUtc).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<TenantRegistration>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ModuleDefinition>(e =>
        {
            e.HasKey(x => x.ModuleId);
            e.Property(x => x.ModuleId).HasMaxLength(50);
            e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.RequiredDependencies).HasMaxLength(200).IsRequired().HasDefaultValue("");
        });

        builder.Entity<TenantModuleEntitlement>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
            e.Property(x => x.ModuleId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired().HasDefaultValue("");
            e.Property(x => x.UpdatedBy).HasMaxLength(100).IsRequired().HasDefaultValue("");
            e.Property(x => x.UpdatedAtUtc).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasIndex(x => new { x.TenantId, x.ModuleId }).IsUnique();
            e.HasOne<TenantRegistration>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ModuleDefinition>().WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ModuleAuditEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
            e.Property(x => x.ModuleId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Actor).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired().HasDefaultValue("");
            e.Property(x => x.OccurredAtUtc).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.OccurredAtUtc });
        });

        builder.Entity<SubscriptionPlan>(e =>
        {
            e.HasKey(x => x.PlanId);
            e.Property(x => x.PlanId).HasMaxLength(50);
            e.Property(x => x.PlanName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.IncludedModules).HasMaxLength(500).IsRequired().HasDefaultValue("");
            e.Property(x => x.AllowsBI).HasDefaultValue(false);
            e.Property(x => x.AllowsBranching).HasDefaultValue(false);
            e.Property(x => x.MaxBranches).HasDefaultValue(1);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAtUtc).IsRequired();
        });

        builder.Entity<TenantSubscription>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
            e.Property(x => x.PlanId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Active");
            e.Property(x => x.AutoRenew).HasDefaultValue(true);
            e.Property(x => x.UpdatedBy).HasMaxLength(100).IsRequired().HasDefaultValue("");
            e.Property(x => x.StartDateUtc).IsRequired();
            e.Property(x => x.UpdatedAtUtc).IsRequired();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne<TenantRegistration>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<SubscriptionPlan>().WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.TenantId, x.Status });
        });

        builder.Entity<SubscriptionAuditEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasMaxLength(50).IsRequired();
            e.Property(x => x.PlanId).HasMaxLength(50).IsRequired();
            e.Property(x => x.Actor).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired().HasDefaultValue("");
            e.Property(x => x.OccurredAtUtc).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.OccurredAtUtc });
        });
    }
}

public sealed class TenantRegistration
{
    public string TenantId { get; set; } = "tenant-a";
    public string DisplayName { get; set; } = "RevCare Auto Hub";
    public string CompanyCode { get; set; } = "REVC-A";
    public string ConnectionKey { get; set; } = "TenantA";
    public string ServerName { get; set; } = @"(localdb)\MSSQLLocalDB";
    public string DatabaseName { get; set; } = "RevCare_TenantA";
    public string CurrentPlanId { get; set; } = "PlanA";
    public string Status { get; set; } = "Ready"; // "Ready", "Provisioning", "Suspended"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class TenantMembership
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = [];
    public string TenantId { get; set; } = "tenant-a";
    public string UserId { get; set; } = "";
    public string Role { get; set; } = "OwnerManager";
    public string DisplayName { get; set; } = "";
    public string ProfileProvisioningStatus { get; set; } = "Synced"; // "Synced", "Pending", "Failed"
    public string? ProvisioningFailureReason { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class StaffAccessAudit
{
    public long Id { get; set; }
    public string TenantId { get; set; } = "tenant-a";
    public string UserId { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Change { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ModuleDefinition
{
    public string ModuleId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsImplemented { get; set; }
    public string RequiredDependencies { get; set; } = ""; // Comma-delimited list of parent ModuleIds
}

public sealed class TenantModuleEntitlement
{
    public int Id { get; set; }
    public string TenantId { get; set; } = "";
    public string ModuleId { get; set; } = "";
    public bool IsEnabled { get; set; }
    public string Reason { get; set; } = "";
    public string UpdatedBy { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = [];
}

public sealed class ModuleAuditEntry
{
    public long Id { get; set; }
    public string TenantId { get; set; } = "";
    public string ModuleId { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Action { get; set; } = ""; // "Enabled", "Disabled"
    public string Reason { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class SubscriptionPlan
{
    public string PlanId { get; set; } = "";
    public string PlanName { get; set; } = "";
    public string Description { get; set; } = "";
    public string IncludedModules { get; set; } = "";
    public bool AllowsBI { get; set; }
    public bool AllowsBranching { get; set; }
    public int MaxBranches { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class TenantSubscription
{
    public int Id { get; set; }
    public string TenantId { get; set; } = "";
    public string PlanId { get; set; } = "";
    public string Status { get; set; } = "Active"; // "Active", "Suspended", "Expired", "Cancelled"
    public DateTime StartDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndDateUtc { get; set; }
    public bool AutoRenew { get; set; } = true;
    public string UpdatedBy { get; set; } = "";
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = [];
}

public sealed class SubscriptionAuditEntry
{
    public long Id { get; set; }
    public string TenantId { get; set; } = "";
    public string PlanId { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Action { get; set; } = ""; // "Created", "Upgraded", "Downgraded", "Suspended", "Reactivated"
    public string Reason { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
