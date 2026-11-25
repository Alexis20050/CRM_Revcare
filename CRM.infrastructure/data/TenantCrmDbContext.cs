using crm.domain.entities;
using Microsoft.EntityFrameworkCore;
namespace CRM.infrastructure.data;

public partial class TenantCrmDbContext : DbContext
{
    public string AuditActor { get; set; } = "Unattributed application";
    public bool IsReviewOperation { get; set; }
    public Func<string, (bool Exists, bool IsActive, string Role)?>? StaffValidator { get; set; }
    public DbSet<TaskWork> TaskWork => Set<TaskWork>();
    public DbSet<TaskReview> TaskReviews => Set<TaskReview>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<JobOrder> JobOrders => Set<JobOrder>();
    public DbSet<JobTask> JobTasks => Set<JobTask>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<TenantDatabaseIdentity> DatabaseIdentity => Set<TenantDatabaseIdentity>();
    public DbSet<TenantStaff> Staff => Set<TenantStaff>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
    public DbSet<AssignmentHistory> AssignmentHistory => Set<AssignmentHistory>();
    public DbSet<ServiceOffering> Services => Set<ServiceOffering>();
    public DbSet<TenantBranch> Branches => Set<TenantBranch>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Estimate> Estimates => Set<Estimate>();
    public DbSet<EstimateLine> EstimateLines => Set<EstimateLine>();
    public DbSet<JobCancellationRequest> JobCancellationRequests => Set<JobCancellationRequest>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<AppointmentHistory> AppointmentHistories => Set<AppointmentHistory>();

    private readonly string? _targetDatabaseName;
    public bool IsLegacyPrototypeTenantA => string.Equals(_targetDatabaseName, "RevCare_TenantA", StringComparison.OrdinalIgnoreCase);
    public bool UseLegacyCatalogTables { get; }

    public TenantCrmDbContext(DbContextOptions<TenantCrmDbContext> options) : base(options)
    {
        UseLegacyCatalogTables = options.FindExtension<LegacyCatalogTablesOptionsExtension>() != null;
        var relational = options.Extensions
            .OfType<Microsoft.EntityFrameworkCore.Infrastructure.RelationalOptionsExtension>()
            .FirstOrDefault();
        var connStr = relational?.ConnectionString;
        if (!string.IsNullOrWhiteSpace(connStr))
        {
            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr);
                _targetDatabaseName = builder.InitialCatalog;
            }
            catch
            {
            }
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantCrmModelCacheKeyFactory>();
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        WorkshopModel.Configure(builder);
        builder.Entity<TenantBranch>(e =>
        {
            e.HasKey(x => x.BranchId);
            e.Property(x => x.BranchCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.BranchName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.ContactNumber).HasMaxLength(50);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAtUtc).IsRequired();
            e.HasIndex(x => x.BranchCode).IsUnique();
        });
        if (!IsLegacyPrototypeTenantA)
        {
            builder.Entity<JobOrder>(entity =>
            {
                entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.ApprovedEstimate).WithMany().HasForeignKey(x => x.ApprovedEstimateId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<TenantBranch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(x => x.CancellationReason).HasMaxLength(500);
                entity.Property(x => x.CancelledBy).HasMaxLength(100);
            });

            builder.Entity<Payment>(entity =>
            {
                entity.HasOne<TenantBranch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(x => x.PaymentPurpose).HasMaxLength(50).HasDefaultValue("Unknown");
                entity.Property(x => x.PayerType).HasMaxLength(50).HasDefaultValue("Unknown");
            });

            builder.Entity<JobTask>(entity =>
            {
                entity.Ignore(x => x.ServiceCatalog);
                entity.Property(x => x.AgreedPrice).HasPrecision(18, 2);
                entity.Property(x => x.ServiceNameSnapshot).HasMaxLength(200);
            });
        }
        else
        {
            builder.Entity<JobOrder>(entity =>
            {
                entity.Ignore(x => x.Vehicle);
                entity.Ignore(x => x.ApprovedEstimate);
                entity.Ignore(x => x.VehicleId);
                entity.Ignore(x => x.ApprovedEstimateId);
                entity.Ignore(x => x.BranchId);
                entity.Ignore(x => x.CancelledAtUtc);
                entity.Ignore(x => x.CancellationReason);
                entity.Ignore(x => x.CancelledBy);
                entity.Ignore(x => x.IsArchived);
                entity.Ignore(x => x.ArchivedAtUtc);
                entity.Ignore(x => x.ArchivedBy);
                entity.Ignore(x => x.ArchiveReason);
            });

            builder.Entity<Payment>(entity =>
            {
                entity.Ignore(x => x.BranchId);
                entity.Ignore(x => x.PaymentPurpose);
                entity.Ignore(x => x.PayerType);
            });

            builder.Entity<JobTask>(entity =>
            {
                entity.Ignore(x => x.ServiceCatalog);
                entity.Ignore(x => x.AgreedPrice);
                entity.Ignore(x => x.ServiceNameSnapshot);
                entity.Ignore(x => x.IsArchived);
                entity.Ignore(x => x.ArchivedAtUtc);
                entity.Ignore(x => x.ArchivedBy);
                entity.Ignore(x => x.ArchiveReason);
            });

            builder.Ignore<Vehicle>();
            builder.Ignore<Appointment>();
            builder.Ignore<AppointmentHistory>();
            builder.Ignore<Estimate>();
            builder.Ignore<EstimateLine>();
            builder.Ignore<JobCancellationRequest>();
            builder.Ignore<Refund>();
        }

        builder.Entity<Inquiry>(entity =>
        {
            entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(x => x.VehicleId);
            entity.Property(x => x.VehicleId).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.Property(x => x.PlateNumber).HasMaxLength(50);
            entity.Property(x => x.Make).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Model).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Color).HasMaxLength(50);
            entity.Property(x => x.Vin).HasMaxLength(50);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.PlateNumber);
            entity.HasIndex(x => x.CustomerId);
        });

        builder.Entity<Appointment>(entity =>
        {
            entity.HasKey(x => x.AppointmentId);
            entity.Property(x => x.AppointmentId).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired().HasDefaultValue("Scheduled");
            entity.Property(x => x.RequestedServices).HasMaxLength(1000);
            entity.Property(x => x.ServiceAdvisorUserId).HasMaxLength(450);
            entity.Property(x => x.CancellationReason).HasMaxLength(500);
            entity.Property(x => x.CancelledBy).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Inquiry).WithMany().HasForeignKey(x => x.InquiryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LinkedJobOrder).WithMany().HasForeignKey(x => x.LinkedJobOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TenantBranch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.LinkedJobOrderId).IsUnique().HasFilter("[LinkedJobOrderId] IS NOT NULL");
        });

        builder.Entity<Estimate>(entity =>
        {
            entity.HasKey(x => x.EstimateId);
            entity.Property(x => x.EstimateId).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.Property(x => x.EstimateNumber).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired().HasDefaultValue("Draft");
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.EstimateNumber).IsUnique();
        });

        builder.Entity<EstimateLine>(entity =>
        {
            entity.HasKey(x => x.EstimateLineId);
            entity.Property(x => x.EstimateLineId).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.Property(x => x.Description).HasMaxLength(250).IsRequired();
            entity.Property(x => x.Quantity).HasPrecision(18, 2);
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.Property(x => x.LineTotal).HasPrecision(18, 2);
            entity.Ignore(x => x.ServiceCatalog);
            entity.HasOne(x => x.Estimate).WithMany(e => e.Lines).HasForeignKey(x => x.EstimateId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<JobCancellationRequest>(entity =>
        {
            entity.HasKey(x => x.RequestId);
            entity.Property(x => x.RequestId).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.Property(x => x.ReasonCategory).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ReasonExplanation).HasMaxLength(2000);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Pending Review");
            entity.Property(x => x.RequestedBy).HasMaxLength(150).IsRequired();
            entity.Property(x => x.TotalPaidAtRequest).HasPrecision(18, 2);
            entity.Property(x => x.IncurredCharges).HasPrecision(18, 2);
            entity.Property(x => x.ProposedRefund).HasPrecision(18, 2);
            entity.Property(x => x.ApprovedRefund).HasPrecision(18, 2);
            entity.Property(x => x.ReviewJustification).HasMaxLength(2000);
            entity.Property(x => x.ReviewedBy).HasMaxLength(150);
            entity.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => x.JobOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.JobOrderId);
            entity.HasIndex(x => x.Status);
        });

        builder.Entity<Refund>(entity =>
        {
            entity.HasKey(x => x.RefundId);
            entity.Property(x => x.RefundId).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.RefundMethod).HasMaxLength(50).IsRequired();
            entity.Property(x => x.RecipientName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.AuthorizedBy).HasMaxLength(150).IsRequired();
            entity.HasIndex(x => x.SubmissionId).IsUnique();
            entity.HasOne(x => x.JobOrder).WithMany().HasForeignKey(x => x.JobOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CancellationRequest).WithMany(x => x.Refunds).HasForeignKey(x => x.CancellationRequestId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.JobOrderId);
        });

        builder.Entity<AppointmentHistory>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd().UseIdentityColumn();
            entity.Property(x => x.Action).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Actor).HasMaxLength(150).IsRequired();
            entity.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.AppointmentId);
        });
        builder.Entity<TaskWork>().HasKey(x => x.JobTaskId);
        builder.Entity<TaskWork>().HasOne(x => x.Task).WithOne().HasForeignKey<TaskWork>(x => x.JobTaskId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TaskWork>().Property(x => x.ReferencePriceAtApproval).HasPrecision(18, 2);
        builder.Entity<TaskReview>().HasOne<JobTask>().WithMany().HasForeignKey(x => x.JobTaskId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TenantStaff>(e =>
        {
            e.HasKey(x => x.UserId);
            if (UseLegacyCatalogTables) e.ToTable("TenantStaff");
        });
        builder.Entity<TaskAssignment>().HasKey(x => x.JobTaskId);
        builder.Entity<TaskAssignment>().HasOne(x => x.Task).WithMany().HasForeignKey(x => x.JobTaskId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TaskAssignment>().HasOne<TenantStaff>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AssignmentHistory>().HasOne<JobTask>().WithMany().HasForeignKey(x => x.JobTaskId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ServiceOffering>(e =>
        {
            if (UseLegacyCatalogTables) e.ToTable("TenantServices");
            e.Property(x => x.Price).HasPrecision(18, 2);
            e.HasIndex(x => x.Name).IsUnique();
        });
        builder.Entity<TenantDatabaseIdentity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.TenantId).HasMaxLength(50);
            e.ToTable("TenantDatabaseIdentity", t => t.HasCheckConstraint("CK_TenantIdentity_Singleton", "[Id] = 1"));
        });
    }
}

public class TenantCrmModelCacheKeyFactory : Microsoft.EntityFrameworkCore.Infrastructure.ModelCacheKeyFactory
{
    public TenantCrmModelCacheKeyFactory(Microsoft.EntityFrameworkCore.Infrastructure.ModelCacheKeyFactoryDependencies dependencies)
        : base(dependencies)
    {
    }

    public override object Create(DbContext context, bool designTime)
    {
        if (context is TenantCrmDbContext tenant)
        {
            return (context.GetType(), tenant.IsLegacyPrototypeTenantA, tenant.UseLegacyCatalogTables, designTime);
        }
        return base.Create(context, designTime);
    }
}

public sealed class LegacyCatalogTablesOptionsExtension : Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsExtension
{
    public Microsoft.EntityFrameworkCore.Infrastructure.DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { }

    public void Validate(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptions options) { }

    private sealed class ExtensionInfo(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsExtension extension)
        : Microsoft.EntityFrameworkCore.Infrastructure.DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "UseLegacyCatalogTables ";
        public override int GetServiceProviderHashCode() => 0;
        public override bool ShouldUseSameServiceProvider(Microsoft.EntityFrameworkCore.Infrastructure.DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo;
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            => debugInfo["RevCare:UseLegacyCatalogTables"] = "1";
    }
}

public static class TenantCrmDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder<TenantCrmDbContext> UseLegacyCatalogTables(
        this DbContextOptionsBuilder<TenantCrmDbContext> builder)
    {
        ((Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsBuilderInfrastructure)builder)
            .AddOrUpdateExtension(new LegacyCatalogTablesOptionsExtension());
        return builder;
    }

    public static async Task<bool> ShouldUseLegacyCatalogBridgeAsync(string connectionString, bool allowMissingDatabase = false, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT CASE WHEN OBJECT_ID('dbo.TenantStaff','U') IS NOT NULL AND OBJECT_ID('dbo.TenantServices','U') IS NOT NULL THEN 1 ELSE 0 END";
            return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct)) == 1;
        }
        catch (Microsoft.Data.SqlClient.SqlException) when (allowMissingDatabase)
        {
            return false;
        }
    }
}
public sealed class TenantDatabaseIdentity
{
    public int Id { get; set; } = 1;
    public string TenantId { get; set; } = "tenant-a";
}

public sealed class TenantBranch
{
    public int BranchId { get; set; }
    public string BranchCode { get; set; } = "MAIN";
    public string BranchName { get; set; } = "Main Branch";
    public string? Address { get; set; }
    public string? ContactNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
