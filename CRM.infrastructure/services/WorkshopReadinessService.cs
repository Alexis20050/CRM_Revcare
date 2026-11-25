#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public sealed record ReadinessResult(
    bool IsPermitted,
    IReadOnlyList<string> SatisfiedRequirements,
    IReadOnlyList<string> MissingRequirements,
    string Message
)
{
    public static ReadinessResult Allowed(string message, IEnumerable<string>? satisfied = null)
        => new(true, (satisfied ?? Array.Empty<string>()).ToList(), Array.Empty<string>(), message);

    public static ReadinessResult Denied(string message, IEnumerable<string>? missing = null, IEnumerable<string>? satisfied = null)
        => new(false, (satisfied ?? Array.Empty<string>()).ToList(), (missing ?? new[] { message }).ToList(), message);
}

public sealed record TaskProgressSummary(
    int TotalTasks,
    int CompletedTasks,
    int OngoingTasks,
    int PendingTasks,
    int AttentionTasks,
    decimal CompletionPercentage,
    string DisplayText
);

public static class WorkshopReadinessService
{
    public static TaskProgressSummary CalculateTaskProgress(IEnumerable<JobTask>? tasks)
    {
        if (tasks == null)
            return new TaskProgressSummary(0, 0, 0, 0, 0, 0m, "0/0 tasks completed (0%)");

        var taskList = tasks.Where(t => t.Status != "Cancelled" && t.Status != "Withdrawn").ToList();
        int total = taskList.Count;
        int completed = taskList.Count(t => t.Status is "Done" or "Completed");
        int ongoing = taskList.Count(t => t.Status is "Ongoing" or "In Progress");
        int pending = taskList.Count(t => t.Status is "Pending");
        int attention = taskList.Count(t => t.Status is "Rework" or "Failed" or "Rejected");

        decimal pct = total == 0 ? 0m : Math.Round((decimal)completed / total * 100m, 1, MidpointRounding.AwayFromZero);
        string text = total == 0
            ? "No service tasks assigned"
            : $"{completed}/{total} tasks done ({pct:0.#}%)";

        return new TaskProgressSummary(total, completed, ongoing, pending, attention, pct, text);
    }

    public static ReadinessResult EvaluateStartWork(DbContext db, JobOrder job)
    {
        var satisfied = new List<string>();
        var missing = new List<string>();

        if (job.Status != "In Queue")
            missing.Add($"Current stage is '{job.Status}'. Only 'In Queue' jobs can be started.");
        else
            satisfied.Add("Stage is In Queue");

        if (IsJobPendingCancellation(db, job.JobOrderId))
            missing.Add("Job has an active cancellation request pending review.");
        else
            satisfied.Add("No pending cancellation request");

        if (!job.CustomerId.HasValue || !db.Set<Customer>().Any(c => c.CustomerId == job.CustomerId.Value && c.IsActive))
            missing.Add("Valid active customer is required.");
        else
            satisfied.Add("Customer is active");

        decimal cost = job.TotalEstimatedCost ?? 0m;
        if (cost <= 0)
            missing.Add("Total estimated cost must be greater than zero before starting work.");
        else
            satisfied.Add($"Estimated cost is ₱{cost:N2}");

        // 50% deposit requirement
        decimal requiredDeposit = decimal.Round(cost * 0.5m, 2, MidpointRounding.AwayFromZero);
        decimal validPaid = db.Set<Payment>()
            .Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided)
            .Sum(p => (decimal?)p.AmountPaid) ?? 0m;

        decimal refunded = 0m;
        if (db is MasterCrmDbContext mDb)
        {
            refunded = mDb.Refunds
                .Where(r => r.JobOrderId == job.JobOrderId)
                .Sum(r => (decimal?)r.Amount) ?? 0m;
        }
        decimal netPaid = Math.Max(0, validPaid - refunded);

        if (netPaid < requiredDeposit)
            missing.Add($"A 50% deposit of ₱{requiredDeposit:N2} is required before work can start (Net paid: ₱{netPaid:N2}). Remaining deposit: ₱{Math.Max(0, requiredDeposit - netPaid):N2}.");
        else
            satisfied.Add($"Deposit satisfied (Paid: ₱{netPaid:N2} >= ₱{requiredDeposit:N2})");

        // Mechanic requirement
        if (string.IsNullOrWhiteSpace(job.AssignedMechanicUserId))
        {
            missing.Add("An active eligible mechanic must be assigned before work can start.");
        }
        else
        {
            var mechValidation = MechanicEligibilityService.ValidateAssignment(db, job, job.AssignedMechanicUserId);
            if (!mechValidation.IsValid)
                missing.Add(mechValidation.ErrorMessage ?? "Assigned mechanic is not eligible for this job.");
            else
                satisfied.Add($"Eligible mechanic assigned: {job.AssignedMechanicDisplay}");
        }

        // Services/tasks requirement
        int taskCount = db.Set<JobTask>().Count(t => t.JobOrderId == job.JobOrderId && t.Status != "Cancelled" && t.Status != "Withdrawn");
        if (taskCount == 0)
            missing.Add("Job must have at least one authorized service task.");
        else
            satisfied.Add($"{taskCount} service task(s) assigned");

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        return ReadinessResult.Allowed("All start work requirements are satisfied. Work can begin.", satisfied);
    }

    public static ReadinessResult EvaluateAddService(DbContext db, JobOrder job)
    {
        var satisfied = new List<string>();
        var missing = new List<string>();

        if (job.Status is "Completed" or "Released")
            missing.Add($"Job is {job.Status}. Completed and released jobs cannot accept new services.");
        else if (job.Status == "Cancelled")
            missing.Add("Job is Cancelled. Cancelled jobs cannot accept new services.");
        else
            satisfied.Add($"Job stage '{job.Status}' allows adding services");

        if (IsJobPendingCancellation(db, job.JobOrderId))
            missing.Add("Job has a pending cancellation request. Ordinary processing is suspended.");
        else
            satisfied.Add("No pending cancellation");

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        return ReadinessResult.Allowed("New services may be added with customer authorization.", satisfied);
    }

    public static ReadinessResult EvaluateAssignMechanic(DbContext db, JobOrder job, string? mechanicUserId = null)
    {
        var satisfied = new List<string>();
        var missing = new List<string>();

        if (job.Status is "Completed" or "Released")
            missing.Add($"Job is {job.Status}. Completed and released jobs cannot be assigned or reassigned.");
        else if (job.Status == "Cancelled")
            missing.Add("Job is Cancelled. Cancelled jobs cannot be reassigned.");
        else
            satisfied.Add($"Job status '{job.Status}' allows mechanic assignment");

        if (IsJobPendingCancellation(db, job.JobOrderId))
            missing.Add("Job has an active cancellation request pending review.");
        else
            satisfied.Add("No pending cancellation");

        if (!string.IsNullOrWhiteSpace(mechanicUserId))
        {
            var validation = MechanicEligibilityService.ValidateAssignment(db, job, mechanicUserId);
            if (!validation.IsValid)
                missing.Add(validation.ErrorMessage ?? "Selected staff member is not eligible.");
            else
                satisfied.Add("Selected mechanic is active, eligible, and specialty-compatible");
        }

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        return ReadinessResult.Allowed("Mechanic assignment permitted.", satisfied);
    }

    public static ReadinessResult EvaluateReassignMechanic(DbContext db, JobOrder job, string? newMechanicUserId, string? reason)
    {
        var assignCheck = EvaluateAssignMechanic(db, job, newMechanicUserId);
        if (!assignCheck.IsPermitted)
            return assignCheck;

        var satisfied = new List<string>(assignCheck.SatisfiedRequirements);
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(reason))
            missing.Add("A mandatory reassignment reason is required to reassign a job order.");
        else
            satisfied.Add("Reassignment reason provided");

        if (string.IsNullOrWhiteSpace(newMechanicUserId))
            missing.Add("Select a replacement mechanic.");
        else if (newMechanicUserId == job.AssignedMechanicUserId)
            missing.Add("The selected mechanic is already assigned to this job.");

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        return ReadinessResult.Allowed("Mechanic reassignment permitted.", satisfied);
    }

    public static ReadinessResult EvaluateFinalInspection(DbContext db, JobOrder job)
    {
        var satisfied = new List<string>();
        var missing = new List<string>();

        if (job.Status != "In Progress")
            missing.Add($"Job status is '{job.Status}'. Final inspection is conducted only during 'In Progress'.");
        else
            satisfied.Add("Job is In Progress");

        int unfinished = db.Set<JobTask>()
            .Count(t => t.JobOrderId == job.JobOrderId && t.Status != "Done" && t.Status != "Completed" && t.Status != "Cancelled" && t.Status != "Withdrawn");

        if (unfinished > 0)
            missing.Add($"{unfinished} service task(s) still unfinished. Complete all tasks before final inspection.");
        else
            satisfied.Add("All required service tasks are completed");

        if (IsJobPendingCancellation(db, job.JobOrderId))
            missing.Add("Job has a pending cancellation request.");
        else
            satisfied.Add("No pending cancellation");

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        return ReadinessResult.Allowed("Final quality inspection can be performed and recorded.", satisfied);
    }

    public static ReadinessResult EvaluateCompleteJob(DbContext db, JobOrder job, bool inspectionConfirmed = false)
    {
        var satisfied = new List<string>();
        var missing = new List<string>();

        if (job.Status != "In Progress")
            missing.Add($"Job status is '{job.Status}'. Only 'In Progress' jobs can be completed.");
        else
            satisfied.Add("Job is In Progress");

        int unfinished = db.Set<JobTask>()
            .Count(t => t.JobOrderId == job.JobOrderId && t.Status != "Done" && t.Status != "Completed" && t.Status != "Cancelled" && t.Status != "Withdrawn");

        if (unfinished > 0)
            missing.Add($"{unfinished} service task(s) still unfinished. All tasks must be completed first.");
        else
            satisfied.Add("All required service tasks are completed");

        if (!inspectionConfirmed)
            missing.Add("Final inspection must pass before completing the job order.");
        else
            satisfied.Add("Final inspection confirmed passed");

        if (IsJobPendingCancellation(db, job.JobOrderId))
            missing.Add("Job has an active cancellation request pending review.");
        else
            satisfied.Add("No pending cancellation");

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        return ReadinessResult.Allowed("Job is ready for completion.", satisfied);
    }

    public static ReadinessResult EvaluateReleaseVehicle(DbContext db, JobOrder job)
    {
        var satisfied = new List<string>();
        var missing = new List<string>();

        if (job.Status != "Completed")
            missing.Add($"Job status is '{job.Status}'. Vehicles can only be released after the job is 'Completed'.");
        else
            satisfied.Add("Job status is Completed");

        decimal cost = job.TotalEstimatedCost ?? 0m;
        if (cost <= 0)
            missing.Add("A valid total estimated cost must be recorded before releasing the vehicle.");
        else
            satisfied.Add($"Estimated total cost is ₱{cost:N2}");

        decimal validPaid = db.Set<Payment>()
            .Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided)
            .Sum(p => (decimal?)p.AmountPaid) ?? 0m;

        decimal refunded = 0m;
        if (db is MasterCrmDbContext mDb)
        {
            refunded = mDb.Refunds
                .Where(r => r.JobOrderId == job.JobOrderId)
                .Sum(r => (decimal?)r.Amount) ?? 0m;
        }
        decimal netPaid = Math.Max(0, validPaid - refunded);
        decimal balance = Math.Max(0, cost - netPaid);

        if (balance > 0)
            missing.Add($"Release blocked: Outstanding balance of ₱{balance:N2} must be fully settled before vehicle release (Net paid: ₱{netPaid:N2} of ₱{cost:N2}).");
        else
            satisfied.Add($"Balance fully settled (Net paid: ₱{netPaid:N2} of ₱{cost:N2})");

        if (IsJobPendingCancellation(db, job.JobOrderId))
            missing.Add("Job has an active cancellation request.");
        else
            satisfied.Add("No pending cancellation");

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        return ReadinessResult.Allowed("Vehicle is fully paid and ready for release to customer.", satisfied);
    }

    public static ReadinessResult EvaluateReceivePayment(DbContext db, JobOrder job, decimal amountToPay = 0m)
    {
        var satisfied = new List<string>();
        var missing = new List<string>();

        if (job.Status == "Cancelled")
            missing.Add("Job is Cancelled. Cancelled jobs cannot accept ordinary payments.");
        else
            satisfied.Add($"Job status '{job.Status}' accepts payments");

        if (IsJobPendingCancellation(db, job.JobOrderId))
            missing.Add("Job has a pending cancellation request. Payment collection is suspended.");
        else
            satisfied.Add("No pending cancellation");

        decimal cost = job.TotalEstimatedCost ?? 0m;
        if (cost <= 0)
            missing.Add("Estimated cost must be set on the job order before recording payments.");
        else
            satisfied.Add($"Estimated cost is ₱{cost:N2}");

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        return ReadinessResult.Allowed("Payment can be recorded.", satisfied);
    }

    public static ReadinessResult EvaluateCancelJob(DbContext db, JobOrder job)
    {
        var satisfied = new List<string>();
        var missing = new List<string>();

        if (job.Status is "Completed" or "Released")
            missing.Add($"Job is already {job.Status}. Completed or released jobs cannot be cancelled.");
        else if (job.Status == "Cancelled")
            missing.Add("Job is already Cancelled.");
        else
            satisfied.Add($"Job status '{job.Status}' permits cancellation");

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        decimal paid = db.Set<Payment>()
            .Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided)
            .Sum(p => (decimal?)p.AmountPaid) ?? 0m;

        if (job.Status == "In Queue" && paid == 0)
            return ReadinessResult.Allowed("Eligible for direct cancellation (Unpaid In-Queue job).", satisfied);

        return ReadinessResult.Allowed($"Requires cancellation and refund review (Status: {job.Status}, Paid: ₱{paid:N2}).", satisfied);
    }

    public static ReadinessResult EvaluateArchive(DbContext db, JobOrder job, string? reason = null)
    {
        var satisfied = new List<string>();
        var missing = new List<string>();

        // Archive eligibility:
        // Eligible: Released (with zero balance) OR Cancelled (with completed financial resolution)
        // Prohibited: In Queue, In Progress, Completed (unreleased), pending cancellation, pending refund, unresolved balance
        if (job.Status is "In Queue" or "In Progress")
        {
            missing.Add($"Job is {job.Status}. Active workshop transactions cannot be archived. They must complete or cancel through normal workflows.");
        }
        else if (job.Status == "Completed")
        {
            missing.Add("Job is Completed but unreleased. Settle the balance and release the vehicle before archiving.");
        }
        else if (job.Status == "Released")
        {
            decimal cost = job.TotalEstimatedCost ?? 0m;
            decimal validPaid = db.Set<Payment>()
                .Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided)
                .Sum(p => (decimal?)p.AmountPaid) ?? 0m;
            decimal refunded = 0m;
            if (db is MasterCrmDbContext mDb)
            {
                refunded = mDb.Refunds.Where(r => r.JobOrderId == job.JobOrderId).Sum(r => (decimal?)r.Amount) ?? 0m;
            }
            decimal netPaid = Math.Max(0, validPaid - refunded);
            if (cost > 0 && netPaid < cost)
            {
                missing.Add($"Released job has an unsettled balance of ₱{cost - netPaid:N2}. Unresolved financial obligations block archival.");
            }
            else
            {
                satisfied.Add("Job is Released with settled balance");
            }
        }
        else if (job.Status == "Cancelled")
        {
            if (IsJobPendingCancellation(db, job.JobOrderId))
            {
                missing.Add("Job has an unresolved cancellation review or pending refund. Complete financial resolution before archiving.");
            }
            else
            {
                satisfied.Add("Job is Cancelled with resolved financial obligations");
            }
        }
        else
        {
            missing.Add($"Operational status '{job.Status}' is not eligible for archiving.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            missing.Add("A mandatory archive reason is required.");
        }
        else
        {
            satisfied.Add("Archive reason provided");
        }

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        return ReadinessResult.Allowed("Job Order is eligible for archiving.", satisfied);
    }

    public static ReadinessResult EvaluateRestore(DbContext db, JobOrder job, string? reason = null)
    {
        var satisfied = new List<string>();
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(reason))
            missing.Add("A mandatory restoration reason is required.");
        else
            satisfied.Add("Restoration reason provided");

        if (missing.Count > 0)
            return ReadinessResult.Denied(missing[0], missing, satisfied);

        return ReadinessResult.Allowed("Job Order is eligible for restoration to active view.", satisfied);
    }

    public static bool IsJobPendingCancellation(DbContext db, int jobOrderId)
    {
        if (db is not MasterCrmDbContext mDb) return false;

        return mDb.JobCancellationRequests.Local.Any(r => r.JobOrderId == jobOrderId && (r.Status == "Pending Review" || r.Status == "Requires Information" || r.Status == "Approved" || r.Status == "Pending Refund"))
            || (!mDb.JobCancellationRequests.Local.Any(r => r.JobOrderId == jobOrderId && (r.Status == "Completed" || r.Status == "Rejected" || r.Status == "Withdrawn"))
                && mDb.JobCancellationRequests.Any(r => r.JobOrderId == jobOrderId && (r.Status == "Pending Review" || r.Status == "Requires Information" || r.Status == "Approved" || r.Status == "Pending Refund")));
    }
}
