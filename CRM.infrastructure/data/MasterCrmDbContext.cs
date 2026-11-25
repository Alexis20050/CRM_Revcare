using crm.domain.entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data
{
    public partial class MasterCrmDbContext : IdentityDbContext
    {
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
        public string AuditActor { get; set; } = "Unattributed application";
        public bool IsBackfillOperation { get; set; }
        public DbSet<JobOrder> JobOrders => Set<JobOrder>();
        public DbSet<JobTask> JobTasks => Set<JobTask>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<WorkshopStaff> Staff => Set<WorkshopStaff>();
        // --- MASTER / TENANT ENTITIES ---
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyDatabase> CompanyDatabases => Set<CompanyDatabase>();

        // --- CRM MAIN TRANSACTIONS (DATA COLLECTIONS) ---
        public DbSet<Inquiry> Inquiries => Set<Inquiry>();
        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();

        // --- 3NF NORMALIZED RELATIONAL ENTITIES ---
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<WorkshopService> Services => Set<WorkshopService>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<Estimate> Estimates => Set<Estimate>();
        public DbSet<EstimateLine> EstimateLines => Set<EstimateLine>();

        // --- CANCELLATION, REFUND & APPOINTMENT LIFECYCLE ENTITIES ---
        public DbSet<JobCancellationRequest> JobCancellationRequests => Set<JobCancellationRequest>();
        public DbSet<Refund> Refunds => Set<Refund>();
        public DbSet<AppointmentHistory> AppointmentHistories => Set<AppointmentHistory>();

        // --- MULTI-COMPANY TENANT IDENTITY & BRANCHES ---
        public DbSet<TenantDatabaseIdentity> DatabaseIdentity => Set<TenantDatabaseIdentity>();
        public DbSet<TenantBranch> Branches => Set<TenantBranch>();

        // --- ALIGNED TENANT WORKFORCE & SERVICE BRIDGE ---
        public DbSet<TaskWork> TaskWork => Set<TaskWork>();
        public DbSet<TaskReview> TaskReviews => Set<TaskReview>();
        public DbSet<TenantStaff> TenantStaff => Set<TenantStaff>();
        public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();
        public DbSet<AssignmentHistory> AssignmentHistory => Set<AssignmentHistory>();
        public DbSet<ServiceOffering> TenantServices => Set<ServiceOffering>();

        private readonly string? _targetDatabaseName;

        public bool IsLegacyDbMasterCrm => string.Equals(_targetDatabaseName, "DB_MasterCrm", StringComparison.OrdinalIgnoreCase);
        public bool IsCloudDb80735 => string.Equals(_targetDatabaseName, "db80735", StringComparison.OrdinalIgnoreCase);

        public MasterCrmDbContext(DbContextOptions<MasterCrmDbContext> options) : base(options)
        {
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
            optionsBuilder.ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, MasterCrmModelCacheKeyFactory>();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            WorkshopModel.Configure(builder);
            builder.Entity<Company>(entity =>
            {
                entity.HasKey(x => x.CompanyId);
                entity.Property(x => x.CompanyCode).HasMaxLength(50).IsRequired();
                entity.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
                entity.HasIndex(x => x.CompanyCode).IsUnique();
            });

            builder.Entity<CompanyDatabase>(entity =>
            {
                entity.HasKey(x => x.CompanyDatabaseId);
                entity.Property(x => x.ServerName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.DatabaseName).HasMaxLength(200).IsRequired();
                entity.HasOne(x => x.Company)
                      .WithMany(x => x.Databases)
                      .HasForeignKey(x => x.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<WorkshopStaff>(entity =>
            {
                entity.HasKey(x => x.StaffId);
                entity.Property(x => x.StaffId).HasMaxLength(450);
                entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Role).HasMaxLength(100).IsRequired();
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.HasIndex(x => x.DisplayName);
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

            builder.Entity<WorkshopService>(entity =>
            {
                entity.ToTable("Services");
                entity.HasKey(x => x.ServiceId);
                entity.Property(x => x.ServiceId).ValueGeneratedOnAdd().UseIdentityColumn();
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(1000);
                entity.Property(x => x.CurrentPrice).HasPrecision(18, 2);
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.HasIndex(x => x.Name).IsUnique();
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
                entity.HasOne(x => x.Estimate).WithMany(e => e.Lines).HasForeignKey(x => x.EstimateId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.ServiceCatalog).WithMany().HasForeignKey(x => x.ServiceCatalogId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<JobOrder>(entity =>
            {
                entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.ApprovedEstimate).WithMany().HasForeignKey(x => x.ApprovedEstimateId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(x => x.CancellationReason).HasMaxLength(500);
                entity.Property(x => x.CancelledBy).HasMaxLength(100);
            });

            builder.Entity<JobTask>(entity =>
            {
                entity.HasOne(x => x.ServiceCatalog).WithMany().HasForeignKey(x => x.ServiceCatalogId).OnDelete(DeleteBehavior.Restrict);
                entity.Property(x => x.AgreedPrice).HasPrecision(18, 2);
                entity.Property(x => x.ServiceNameSnapshot).HasMaxLength(200);
            });

            builder.Entity<Inquiry>(entity =>
            {
                entity.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Payment>(entity =>
            {
                entity.Property(x => x.PaymentPurpose).HasMaxLength(50).HasDefaultValue("Deposit");
                entity.Property(x => x.PayerType).HasMaxLength(50).HasDefaultValue("Customer");
            });

            if (IsCloudDb80735)
            {
                builder.Entity<JobOrder>().Ignore(x => x.BranchId);
                builder.Entity<JobOrder>().Ignore(x => x.IsArchived);
                builder.Entity<JobOrder>().Ignore(x => x.ArchivedAtUtc);
                builder.Entity<JobOrder>().Ignore(x => x.ArchivedBy);
                builder.Entity<JobOrder>().Ignore(x => x.ArchiveReason);

                builder.Entity<JobTask>().Ignore(x => x.IsArchived);
                builder.Entity<JobTask>().Ignore(x => x.ArchivedAtUtc);
                builder.Entity<JobTask>().Ignore(x => x.ArchivedBy);
                builder.Entity<JobTask>().Ignore(x => x.ArchiveReason);

                builder.Entity<Appointment>().Ignore(x => x.BranchId);
                builder.Entity<Payment>().Ignore(x => x.BranchId);

                builder.Ignore<TenantBranch>();
                builder.Ignore<TenantDatabaseIdentity>();
                builder.Ignore<JobCancellationRequest>();
                builder.Ignore<Refund>();
                builder.Ignore<AppointmentHistory>();
                builder.Ignore<TaskWork>();
                builder.Ignore<TaskReview>();
                builder.Ignore<TenantStaff>();
                builder.Ignore<TaskAssignment>();
                builder.Ignore<AssignmentHistory>();
                builder.Ignore<ServiceOffering>();
            }
            else if (IsLegacyDbMasterCrm)
            {
                builder.Entity<JobOrder>().Ignore(x => x.BranchId);
                builder.Entity<Appointment>().Ignore(x => x.BranchId);
                builder.Entity<Payment>().Ignore(x => x.BranchId);
                builder.Ignore<TenantBranch>();
                builder.Ignore<TenantDatabaseIdentity>();
                builder.Ignore<TaskWork>();
                builder.Ignore<TaskReview>();
                builder.Ignore<TenantStaff>();
                builder.Ignore<TaskAssignment>();
                builder.Ignore<AssignmentHistory>();
                builder.Ignore<ServiceOffering>();
            }
            else
            {
                builder.Entity<Appointment>().HasOne<TenantBranch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
                builder.Entity<JobOrder>().HasOne<TenantBranch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
                builder.Entity<Payment>().HasOne<TenantBranch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);

                builder.Entity<TenantBranch>(entity =>
                {
                    entity.ToTable("Branches");
                    entity.HasKey(x => x.BranchId);
                    entity.Property(x => x.BranchCode).HasMaxLength(50).IsRequired();
                    entity.Property(x => x.BranchName).HasMaxLength(200).IsRequired();
                    entity.Property(x => x.Address).HasMaxLength(500);
                    entity.Property(x => x.ContactNumber).HasMaxLength(50);
                    entity.Property(x => x.IsActive).HasDefaultValue(true);
                    entity.Property(x => x.CreatedAtUtc).IsRequired();
                    entity.HasIndex(x => x.BranchCode).IsUnique();
                });

                builder.Entity<TenantDatabaseIdentity>(entity =>
                {
                    entity.HasKey(x => x.Id);
                    entity.Property(x => x.Id).ValueGeneratedNever();
                    entity.Property(x => x.TenantId).HasMaxLength(50);
                    entity.ToTable("TenantDatabaseIdentity", t => t.HasCheckConstraint("CK_TenantIdentity_Singleton", "[Id] = 1"));
                });

                builder.Entity<TaskWork>(entity =>
                {
                    entity.HasKey(x => x.JobTaskId);
                    entity.HasOne(x => x.Task).WithOne().HasForeignKey<TaskWork>(x => x.JobTaskId).OnDelete(DeleteBehavior.Restrict);
                    entity.Property(x => x.ReferencePriceAtApproval).HasPrecision(18, 2);
                });
                builder.Entity<TaskReview>().HasOne<JobTask>().WithMany().HasForeignKey(x => x.JobTaskId).OnDelete(DeleteBehavior.Restrict);
                builder.Entity<TenantStaff>(entity =>
                {
                    entity.ToTable("TenantStaff");
                    entity.HasKey(x => x.UserId);
                });
                builder.Entity<TaskAssignment>(entity =>
                {
                    entity.HasKey(x => x.JobTaskId);
                    entity.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.JobTaskId).OnDelete(DeleteBehavior.Restrict);
                    entity.HasOne<TenantStaff>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
                });
                builder.Entity<AssignmentHistory>().HasOne<JobTask>().WithMany().HasForeignKey(x => x.JobTaskId).OnDelete(DeleteBehavior.Restrict);
                builder.Entity<ServiceOffering>(entity =>
                {
                    entity.ToTable("TenantServices");
                    entity.Property(x => x.Price).HasPrecision(18, 2);
                    entity.HasIndex(x => x.Name).IsUnique();
                });
            }

            if (!IsCloudDb80735)
            {
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
            }
        }
    }

    public class MasterCrmModelCacheKeyFactory : Microsoft.EntityFrameworkCore.Infrastructure.ModelCacheKeyFactory
    {
        public MasterCrmModelCacheKeyFactory(Microsoft.EntityFrameworkCore.Infrastructure.ModelCacheKeyFactoryDependencies dependencies)
            : base(dependencies)
        {
        }

        public override object Create(DbContext context, bool designTime)
        {
            if (context is MasterCrmDbContext master)
            {
                int variant = master.IsCloudDb80735 ? 1 : (master.IsLegacyDbMasterCrm ? 2 : 3);
                return (context.GetType(), variant, designTime);
            }
            return base.Create(context, designTime);
        }
    }
}

