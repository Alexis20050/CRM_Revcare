using crm.domain.entities;
using Microsoft.EntityFrameworkCore;
namespace CRM.infrastructure.data;

internal static class WorkshopModel
{
    public static void Configure(ModelBuilder builder)
    {
            builder.Entity<Payment>().HasIndex(p => p.SubmissionId).IsUnique();
            builder.Entity<JobOrder>().Property(x => x.TotalEstimatedCost).HasPrecision(18, 2);
            builder.Entity<JobOrder>().Navigation(x => x.Payments).AutoInclude();
            builder.Entity<JobOrder>().ToTable(t => t.HasCheckConstraint("CK_JobOrders_Estimate", "[TotalEstimatedCost] IS NULL OR [TotalEstimatedCost] >= 0"));
            builder.Entity<Payment>().ToTable(t => t.HasCheckConstraint("CK_Payments_Positive", "[AmountPaid] > 0"));
            builder.Entity<Inquiry>().HasOne<JobOrder>().WithMany().HasForeignKey(x => x.ConvertedJobOrderId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Inquiry>().HasIndex(x => x.ConvertedJobOrderId).IsUnique().HasFilter("[ConvertedJobOrderId] IS NOT NULL");
            builder.Entity<Complaint>().HasOne(x => x.SourceFeedback).WithMany().HasForeignKey(x => x.SourceFeedbackId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Complaint>().HasIndex(x => x.SourceFeedbackId).IsUnique().HasFilter("[SourceFeedbackId] IS NOT NULL");
            builder.Entity<Customer>(entity =>
            {
                entity.HasKey(x => x.CustomerId);
                entity.Property(x => x.CustomerId).ValueGeneratedOnAdd().UseIdentityColumn();
                entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.ContactNumber).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
                entity.HasIndex(x => x.FullName);
                entity.HasIndex(x => x.ContactNumber);
            });
            builder.Entity<JobOrder>().HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Inquiry>().HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Complaint>().HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Complaint>().HasOne<JobOrder>().WithMany().HasForeignKey(x => x.JobOrderId).OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Feedback>().HasOne<JobOrder>().WithMany().HasForeignKey(x => x.JobOrderId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<JobOrder>(entity =>
            {
                entity.HasKey(x => x.JobOrderId);
                entity.Property(x => x.JobOrderId).ValueGeneratedOnAdd().UseIdentityColumn();
                entity.Property(x => x.EstimateId).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
                entity.Property(x => x.AssignedMechanicUserId).HasMaxLength(450);
                entity.Property(x => x.AssignedMechanic).HasMaxLength(150);
                entity.Property(x => x.IsArchived).HasDefaultValue(false);
                entity.Property(x => x.ArchivedBy).HasMaxLength(150);
                entity.Property(x => x.ArchiveReason).HasMaxLength(500);
                entity.HasIndex(x => x.IsArchived);
            });
            builder.Entity<JobTask>(entity =>
            {
                entity.HasKey(x => x.JobTaskId);
                entity.Property(x => x.JobTaskId).ValueGeneratedOnAdd().UseIdentityColumn();
                entity.Property(x => x.ServiceId).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Remarks).HasMaxLength(2000).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
                entity.Property(x => x.IsArchived).HasDefaultValue(false);
                entity.Property(x => x.ArchivedBy).HasMaxLength(150);
                entity.Property(x => x.ArchiveReason).HasMaxLength(500);
                entity.HasIndex(x => x.IsArchived);
                entity.HasOne<JobOrder>().WithMany(j => j.Tasks).HasForeignKey(x => x.JobOrderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            builder.Entity<Payment>(entity =>
            {
                entity.HasKey(x => x.PaymentId);
                entity.Property(x => x.PaymentId).ValueGeneratedOnAdd().UseIdentityColumn();
                entity.Property(x => x.PaymentType).HasMaxLength(30).IsRequired();
                entity.Property(x => x.PaymentMethod).HasMaxLength(30).IsRequired();
                entity.Property(x => x.AmountPaid).HasPrecision(18, 2);
                entity.HasOne<JobOrder>().WithMany(j => j.Payments).HasForeignKey(x => x.JobOrderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // 1. TENANT ENTITIES CONFIGURATION
            // ==========================================
            // ==========================================
            // 2. MAIN TRANSACTIONS CONFIGURATION
            // ==========================================

            // Inquiry â€” stores CustomerName as plain string, auto-incrementing PK
            builder.Entity<Inquiry>(entity =>
            {
                entity.HasKey(x => x.InquiryId);
                entity.Property(x => x.InquiryId).ValueGeneratedOnAdd().UseIdentityColumn();
            });

            // Complaint â€” CustomerId and JobOrderId are nullable (int?), auto-incrementing PK
            builder.Entity<Complaint>(entity =>
            {
                entity.HasKey(x => x.ComplaintId);
                entity.Property(x => x.ComplaintId).ValueGeneratedOnAdd().UseIdentityColumn();
                entity.Property(x => x.ComplaintType).HasMaxLength(100).IsRequired();
                entity.Property(x => x.ResolutionStatus).HasMaxLength(50).HasDefaultValue("Open");
            });

            // Feedback â€” CustomerId and JobOrderId are required (int), auto-incrementing PK
            builder.Entity<Feedback>(entity =>
            {
                entity.HasKey(x => x.FeedbackId);
                entity.Property(x => x.FeedbackId).ValueGeneratedOnAdd().UseIdentityColumn();
                entity.Property(x => x.RatingScore).IsRequired();
            });
    }
}

