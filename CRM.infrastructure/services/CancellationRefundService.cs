#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public sealed record CancellationEligibility(
    bool CanDirectCancel,
    bool RequiresReview,
    bool IsBlocked,
    string StatusExplanation,
    decimal TotalPaid,
    decimal TotalRefunded,
    decimal RemainingRefundable,
    JobCancellationRequest? ExistingActiveRequest
);

public static class CancellationRefundService
{
    public static readonly string[] CancellationReasonCategories =
    [
        "Customer requested cancellation",
        "Customer unavailable",
        "Scheduling conflict",
        "Required parts unavailable",
        "Duplicate job order",
        "Customer declined service",
        "Workshop unable to perform service",
        "Insurance approval declined",
        "Other"
    ];

    public static readonly string[] RefundMethods =
    [
        "Cash",
        "GCash",
        "Card",
        "Bank Transfer",
        "Check"
    ];

    public static readonly string[] ActiveRequestStatuses =
    [
        "Pending Review",
        "Requires Information",
        "Approved",
        "Pending Refund"
    ];

    public static decimal GetTotalPaid(MasterCrmDbContext db, int jobOrderId)
    {
        return db.Payments
            .Where(p => p.JobOrderId == jobOrderId && !p.IsVoided)
            .Sum(p => (decimal?)p.AmountPaid) ?? 0m;
    }

    public static decimal GetTotalRefunded(MasterCrmDbContext db, int jobOrderId)
    {
        return db.Refunds
            .Where(r => r.JobOrderId == jobOrderId)
            .Sum(r => (decimal?)r.Amount) ?? 0m;
    }

    public static decimal GetRemainingRefundable(MasterCrmDbContext db, int jobOrderId)
    {
        decimal paid = GetTotalPaid(db, jobOrderId);
        decimal refunded = GetTotalRefunded(db, jobOrderId);
        return Math.Max(0m, paid - refunded);
    }

    public static decimal GetPaymentRemainingRefundable(MasterCrmDbContext db, int paymentId)
    {
        var payment = db.Payments.AsNoTracking().FirstOrDefault(p => p.PaymentId == paymentId);
        if (payment == null || payment.IsVoided) return 0m;

        decimal refundedAgainstThisPayment = db.Refunds
            .Where(r => r.PaymentId == paymentId)
            .Sum(r => (decimal?)r.Amount) ?? 0m;

        return Math.Max(0m, payment.AmountPaid - refundedAgainstThisPayment);
    }

    public static JobCancellationRequest? GetActiveRequest(MasterCrmDbContext db, int jobOrderId)
    {
        return db.JobCancellationRequests
            .Include(r => r.Refunds)
            .FirstOrDefault(r => r.JobOrderId == jobOrderId &&
                (r.Status == "Pending Review" || r.Status == "Requires Information" || r.Status == "Approved" || r.Status == "Pending Refund"));
    }

    public static CancellationEligibility EvaluateEligibility(MasterCrmDbContext db, JobOrder job)
    {
        string normStatus = JobOrderWorkflow.Normalize(job.Status);
        decimal totalPaid = GetTotalPaid(db, job.JobOrderId);
        decimal totalRefunded = GetTotalRefunded(db, job.JobOrderId);
        decimal remainingRefundable = Math.Max(0m, totalPaid - totalRefunded);
        var activeReq = GetActiveRequest(db, job.JobOrderId);

        if (normStatus is "Completed" or "Released")
        {
            return new CancellationEligibility(
                CanDirectCancel: false,
                RequiresReview: false,
                IsBlocked: true,
                StatusExplanation: $"Completed and released jobs cannot be cancelled through ordinary cancellation workflows ({RecordReference.JobOrder(job.JobOrderId)} is {normStatus}). Use warranty or correction workflows.",
                TotalPaid: totalPaid,
                TotalRefunded: totalRefunded,
                RemainingRefundable: remainingRefundable,
                ExistingActiveRequest: activeReq
            );
        }

        if (normStatus == "Cancelled")
        {
            return new CancellationEligibility(
                CanDirectCancel: false,
                RequiresReview: false,
                IsBlocked: true,
                StatusExplanation: $"Job order {RecordReference.JobOrder(job.JobOrderId)} is already in Cancelled terminal state.",
                TotalPaid: totalPaid,
                TotalRefunded: totalRefunded,
                RemainingRefundable: remainingRefundable,
                ExistingActiveRequest: activeReq
            );
        }

        if (activeReq != null)
        {
            return new CancellationEligibility(
                CanDirectCancel: false,
                RequiresReview: true,
                IsBlocked: false,
                StatusExplanation: $"A cancellation request ({RecordReference.CancellationRequest(activeReq.RequestId)}) is currently active ({activeReq.Status}). Management review in progress.",
                TotalPaid: totalPaid,
                TotalRefunded: totalRefunded,
                RemainingRefundable: remainingRefundable,
                ExistingActiveRequest: activeReq
            );
        }

        if (normStatus == "In Queue")
        {
            // Check for completed work or payments
            bool hasStartedWork = db.JobTasks.Any(t => t.JobOrderId == job.JobOrderId && (t.Status == "Ongoing" || t.Status == "Done" || t.Status == "Completed"));

            if (totalPaid == 0 && !hasStartedWork)
            {
                return new CancellationEligibility(
                    CanDirectCancel: true,
                    RequiresReview: false,
                    IsBlocked: false,
                    StatusExplanation: "In Queue with zero payments and no completed work. Eligible for direct cancellation with a required reason.",
                    TotalPaid: totalPaid,
                    TotalRefunded: totalRefunded,
                    RemainingRefundable: remainingRefundable,
                    ExistingActiveRequest: null
                );
            }

            return new CancellationEligibility(
                CanDirectCancel: false,
                RequiresReview: true,
                IsBlocked: false,
                StatusExplanation: totalPaid > 0
                    ? $"Recorded payments of ₱{totalPaid:N2} detected. In-queue jobs with recorded payments require management refund review before cancellation."
                    : "Work or financial obligations have been documented. Management cancellation review required.",
                TotalPaid: totalPaid,
                TotalRefunded: totalRefunded,
                RemainingRefundable: remainingRefundable,
                ExistingActiveRequest: null
            );
        }

        if (normStatus == "In Progress")
        {
            return new CancellationEligibility(
                CanDirectCancel: false,
                RequiresReview: true,
                IsBlocked: false,
                StatusExplanation: "In Progress jobs require management review of completed work, incurred charges, and refund obligations before cancellation.",
                TotalPaid: totalPaid,
                TotalRefunded: totalRefunded,
                RemainingRefundable: remainingRefundable,
                ExistingActiveRequest: null
            );
        }

        return new CancellationEligibility(
            CanDirectCancel: false,
            RequiresReview: true,
            IsBlocked: false,
            StatusExplanation: $"Job status '{normStatus}' requires review.",
            TotalPaid: totalPaid,
            TotalRefunded: totalRefunded,
            RemainingRefundable: remainingRefundable,
            ExistingActiveRequest: null
        );
    }

    public static void DirectCancelUnpaidJob(MasterCrmDbContext db, int jobOrderId, string reasonCategory, string explanation, string actor)
    {
        var job = db.JobOrders.Include(j => j.Tasks).FirstOrDefault(j => j.JobOrderId == jobOrderId)
            ?? throw new InvalidOperationException($"Job order #{jobOrderId} not found.");

        var eligibility = EvaluateEligibility(db, job);
        if (!eligibility.CanDirectCancel)
        {
            throw new InvalidOperationException($"Direct cancellation is not permitted: {eligibility.StatusExplanation}");
        }

        string fullReason = string.IsNullOrWhiteSpace(explanation)
            ? reasonCategory
            : $"{reasonCategory}: {explanation.Trim()}";

        if (fullReason.Trim().Length < 4)
            throw new InvalidOperationException("A valid cancellation reason of at least 4 characters is required.");

        job.Status = "Cancelled";
        job.CancellationReason = fullReason;
        job.CancelledBy = string.IsNullOrWhiteSpace(actor) ? db.AuditActor : actor;
        job.CancelledAtUtc = DateTime.UtcNow;

        // Make unfinished tasks non-actionable
        var unfinishedTasks = db.JobTasks.Where(t => t.JobOrderId == job.JobOrderId && t.Status != "Done" && t.Status != "Completed").ToList();
        foreach (var t in unfinishedTasks)
        {
            t.Status = "Cancelled";
        }

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = job.CancelledBy,
            EntityType = "JobOrder",
            Action = "JobDirectlyCancelled",
            Changes = System.Text.Json.JsonSerializer.Serialize(new
            {
                job.JobOrderId,
                ReasonCategory = reasonCategory,
                Explanation = explanation,
                CancelledBy = job.CancelledBy
            })
        });

        db.SaveChanges();
    }

    public static JobCancellationRequest SubmitCancellationRequest(
        MasterCrmDbContext db,
        int jobOrderId,
        string reasonCategory,
        string explanation,
        string requestedBy,
        decimal incurredCharges = 0m,
        decimal proposedRefund = 0m)
    {
        var job = db.JobOrders.Find(jobOrderId)
            ?? throw new InvalidOperationException($"Job order #{jobOrderId} not found.");

        var eligibility = EvaluateEligibility(db, job);
        if (eligibility.IsBlocked)
        {
            throw new InvalidOperationException($"Cannot request cancellation: {eligibility.StatusExplanation}");
        }

        if (eligibility.ExistingActiveRequest != null)
        {
            throw new InvalidOperationException($"An active cancellation request ({RecordReference.CancellationRequest(eligibility.ExistingActiveRequest.RequestId)}) already exists for this job.");
        }

        if (string.IsNullOrWhiteSpace(reasonCategory))
            throw new InvalidOperationException("Select a cancellation reason category.");

        if (reasonCategory == "Other" && string.IsNullOrWhiteSpace(explanation))
            throw new InvalidOperationException("An explanation is required when selecting 'Other' as cancellation reason.");

        decimal totalPaid = eligibility.TotalPaid;
        if (proposedRefund < 0 || proposedRefund > eligibility.RemainingRefundable)
            throw new InvalidOperationException($"Proposed refund must be between ₱0.00 and remaining refundable balance (₱{eligibility.RemainingRefundable:N2}).");

        var request = new JobCancellationRequest
        {
            JobOrderId = jobOrderId,
            RequestedAtUtc = DateTime.UtcNow,
            RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? db.AuditActor : requestedBy,
            ReasonCategory = reasonCategory,
            ReasonExplanation = explanation?.Trim() ?? "",
            Status = "Pending Review",
            TotalPaidAtRequest = totalPaid,
            IncurredCharges = Math.Max(0m, incurredCharges),
            ProposedRefund = proposedRefund
        };

        db.JobCancellationRequests.Add(request);

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = request.RequestedBy,
            EntityType = "JobCancellationRequest",
            Action = "Submitted",
            Changes = System.Text.Json.JsonSerializer.Serialize(new
            {
                JobOrderId = jobOrderId,
                request.ReasonCategory,
                request.ReasonExplanation,
                TotalPaidAtRequest = totalPaid,
                request.ProposedRefund
            })
        });

        db.SaveChanges();
        return request;
    }

    public static bool IsAuthorizedManager(string? roleOrActor, MasterCrmDbContext db)
    {
        if (string.IsNullOrWhiteSpace(roleOrActor)) return false;
        string actor = roleOrActor.Trim();

        // Check if matching staff member in directory has an authorized management role
        var staff = db.Staff.AsNoTracking().FirstOrDefault(s =>
            (s.StaffId == actor || s.DisplayName == actor) && s.IsActive);

        if (staff != null)
        {
            return staff.Role is "Owner" or "Manager" or "Super Admin" or "SuperAdmin" or "Owner / Manager" or "OwnerManager" or "Lead Mechanic";
        }

        // Check actor string for management keywords
        return actor.Contains("Manager", StringComparison.OrdinalIgnoreCase) ||
               actor.Contains("Owner", StringComparison.OrdinalIgnoreCase) ||
               actor.Contains("Admin", StringComparison.OrdinalIgnoreCase);
    }

    public static void ReviewCancellationRequest(
        MasterCrmDbContext db,
        int requestId,
        string decisionAction, // "Approve", "Reject", "Requires Information", "Withdraw"
        string justification,
        string authorizedReviewer,
        decimal? approvedRefundAmount = null)
    {
        var request = db.JobCancellationRequests.Include(r => r.Refunds).FirstOrDefault(r => r.RequestId == requestId)
            ?? throw new InvalidOperationException($"Cancellation request #{requestId} not found.");

        if (string.IsNullOrWhiteSpace(justification) || justification.Trim().Length < 4)
            throw new InvalidOperationException("A decision justification of at least 4 characters is required.");

        if (string.IsNullOrWhiteSpace(authorizedReviewer))
            throw new InvalidOperationException("Authorized reviewer identity is required.");

        // Fail-closed authorization check for approvals
        if (decisionAction == "Approve" && !IsAuthorizedManager(authorizedReviewer, db))
        {
            throw new InvalidOperationException(
                $"Management authorization required: '{authorizedReviewer}' is not an authorized workshop manager or administrator.");
        }

        var job = db.JobOrders.Find(request.JobOrderId)
            ?? throw new InvalidOperationException($"Job order #{request.JobOrderId} not found.");

        switch (decisionAction)
        {
            case "Approve":
                decimal approvedRefund = approvedRefundAmount ?? request.ProposedRefund;
                decimal remainingRefundable = GetRemainingRefundable(db, request.JobOrderId);

                if (approvedRefund < 0 || approvedRefund > remainingRefundable)
                    throw new InvalidOperationException($"Approved refund amount cannot exceed remaining refundable balance (₱{remainingRefundable:N2}).");

                request.ApprovedRefund = approvedRefund;
                request.ReviewJustification = justification.Trim();
                request.ReviewedBy = authorizedReviewer;
                request.ReviewedAtUtc = DateTime.UtcNow;

                if (approvedRefund > 0)
                {
                    request.Status = "Pending Refund";
                }
                else
                {
                    // No refund required: complete review and finalize cancellation directly
                    request.Status = "Completed";
                    request.CompletedAtUtc = DateTime.UtcNow;
                    FinalizeJobCancellation(db, job, request.ReasonCategory + ": " + justification, authorizedReviewer);
                }
                break;

            case "Reject":
                request.Status = "Rejected";
                request.ReviewJustification = justification.Trim();
                request.ReviewedBy = authorizedReviewer;
                request.ReviewedAtUtc = DateTime.UtcNow;
                break;

            case "Requires Information":
                request.Status = "Requires Information";
                request.ReviewJustification = justification.Trim();
                request.ReviewedBy = authorizedReviewer;
                request.ReviewedAtUtc = DateTime.UtcNow;
                break;

            case "Withdraw":
                request.Status = "Withdrawn";
                request.ReviewJustification = justification.Trim();
                request.ReviewedBy = authorizedReviewer;
                request.ReviewedAtUtc = DateTime.UtcNow;
                break;

            default:
                throw new InvalidOperationException($"Unknown decision action '{decisionAction}'.");
        }

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = authorizedReviewer,
            EntityType = "JobCancellationRequest",
            Action = $"Reviewed_{decisionAction}",
            Changes = System.Text.Json.JsonSerializer.Serialize(new
            {
                RequestId = requestId,
                Action = decisionAction,
                NewStatus = request.Status,
                Justification = justification,
                ApprovedRefund = request.ApprovedRefund,
                ReviewedBy = authorizedReviewer
            })
        });

        db.SaveChanges();
    }

    public static Refund RecordRefund(
        MasterCrmDbContext db,
        int jobOrderId,
        int? cancellationRequestId,
        int? paymentId,
        decimal amount,
        string refundMethod,
        string recipientName,
        string reason,
        string authorizedBy,
        Guid? submissionId = null)
    {
        if (amount <= 0 || decimal.Round(amount, 2) != amount)
            throw new InvalidOperationException("Refund amount must be positive with at most two decimal places.");

        if (string.IsNullOrWhiteSpace(recipientName))
            throw new InvalidOperationException("Recipient name is required to record a refund.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 4)
            throw new InvalidOperationException("A refund reason of at least 4 characters is required.");

        if (string.IsNullOrWhiteSpace(authorizedBy))
            throw new InvalidOperationException("Authorized manager name is required.");

        // Fail-closed authorization check
        if (!IsAuthorizedManager(authorizedBy, db))
        {
            throw new InvalidOperationException(
                $"Management authorization required: '{authorizedBy}' is not authorized to execute refunds.");
        }

        if (!RefundMethods.Contains(refundMethod))
            throw new InvalidOperationException($"Unsupported refund method '{refundMethod}'. Supported methods: {string.Join(", ", RefundMethods)}.");

        var job = db.JobOrders.FromSqlInterpolated($"SELECT * FROM JobOrders WITH (UPDLOCK,HOLDLOCK) WHERE JobOrderId = {jobOrderId}").FirstOrDefault()
            ?? db.JobOrders.Find(jobOrderId)
            ?? throw new InvalidOperationException($"Job order #{jobOrderId} not found.");

        if (job.Status is "Completed" or "Released")
            throw new InvalidOperationException("Refunds cannot be issued on completed or released jobs without formal warranty review.");

        decimal remainingRefundable = GetRemainingRefundable(db, jobOrderId);
        if (amount > remainingRefundable)
            throw new InvalidOperationException($"Refund amount (₱{amount:N2}) exceeds remaining refundable balance (₱{remainingRefundable:N2}).");

        if (paymentId.HasValue && paymentId.Value > 0)
        {
            var payment = db.Payments.Find(paymentId.Value)
                ?? throw new InvalidOperationException($"Original payment #{paymentId.Value} not found.");

            if (payment.IsVoided)
                throw new InvalidOperationException("Cannot refund against a voided payment.");

            decimal paymentRemaining = GetPaymentRemainingRefundable(db, paymentId.Value);
            if (amount > paymentRemaining)
                throw new InvalidOperationException($"Refund amount (₱{amount:N2}) exceeds remaining refundable balance on receipt #{paymentId.Value} (₱{paymentRemaining:N2}).");
        }

        // Validate cancellation request status and approved limit to prevent stale approvals
        JobCancellationRequest? req = null;
        if (cancellationRequestId.HasValue && cancellationRequestId.Value > 0)
        {
            req = db.JobCancellationRequests.Include(r => r.Refunds).FirstOrDefault(r => r.RequestId == cancellationRequestId.Value)
                ?? throw new InvalidOperationException($"Cancellation request #{cancellationRequestId.Value} not found.");

            if (req.Status is not ("Approved" or "Pending Refund"))
                throw new InvalidOperationException($"Cannot record refund against cancellation request #{req.RequestId} with status '{req.Status}'. Only Approved or Pending Refund requests can be refunded.");

            decimal approvedTarget = req.ApprovedRefund ?? req.ProposedRefund;
            decimal alreadyRefundedOnReq = db.Refunds.Where(r => r.CancellationRequestId == req.RequestId).Sum(r => (decimal?)r.Amount) ?? 0m;
            decimal remainingApproved = Math.Max(0m, approvedTarget - alreadyRefundedOnReq);
            if (amount > remainingApproved)
                throw new InvalidOperationException($"Refund amount (₱{amount:N2}) exceeds remaining approved refund limit for cancellation request #{req.RequestId} (₱{remainingApproved:N2}).");
        }

        Guid subId = submissionId ?? Guid.NewGuid();
        if (db.Refunds.Any(r => r.SubmissionId == subId))
            throw new InvalidOperationException("Duplicate refund submission detected. This transaction was already processed.");

        var refund = new Refund
        {
            CancellationRequestId = cancellationRequestId,
            JobOrderId = jobOrderId,
            PaymentId = paymentId,
            Amount = amount,
            RefundMethod = refundMethod,
            RefundDateUtc = DateTime.UtcNow,
            RecipientName = recipientName.Trim(),
            Reason = reason.Trim(),
            AuthorizedBy = authorizedBy.Trim(),
            SubmissionId = subId,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Refunds.Add(refund);

        // Check if this refund fulfills active cancellation request
        if (req != null)
        {
            decimal totalRefundedAfter = GetTotalRefunded(db, jobOrderId) + amount;
            decimal approvedTarget = req.ApprovedRefund ?? req.ProposedRefund;

            if (totalRefundedAfter >= approvedTarget || totalRefundedAfter >= GetTotalPaid(db, jobOrderId))
            {
                req.Status = "Completed";
                req.CompletedAtUtc = DateTime.UtcNow;
                FinalizeJobCancellation(db, job, "Refund completed: " + reason, authorizedBy);
            }
        }

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = authorizedBy,
            EntityType = "Refund",
            Action = "Recorded",
            Changes = System.Text.Json.JsonSerializer.Serialize(new
            {
                JobOrderId = jobOrderId,
                PaymentId = paymentId,
                Amount = amount,
                RefundMethod = refundMethod,
                RecipientName = recipientName,
                Reason = reason,
                AuthorizedBy = authorizedBy,
                SubmissionId = subId
            })
        });

        db.SaveChanges();
        return refund;
    }

    private static void FinalizeJobCancellation(MasterCrmDbContext db, JobOrder job, string cancellationReason, string actor)
    {
        job.Status = "Cancelled";
        job.CancellationReason = cancellationReason;
        job.CancelledBy = actor;
        job.CancelledAtUtc = DateTime.UtcNow;

        var unfinished = db.JobTasks.Where(t => t.JobOrderId == job.JobOrderId && t.Status != "Done" && t.Status != "Completed").ToList();
        foreach (var t in unfinished)
        {
            t.Status = "Cancelled";
        }

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = actor,
            EntityType = "JobOrder",
            Action = "JobCancelledAfterReview",
            Changes = System.Text.Json.JsonSerializer.Serialize(new
            {
                job.JobOrderId,
                Reason = cancellationReason,
                CancelledBy = actor
            })
        });
    }
}
