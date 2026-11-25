using crm.domain.entities;
using Microsoft.EntityFrameworkCore;
namespace CRM.infrastructure.data;

public sealed class WorkshopRules(DbContext context, string auditActor)
{
    public static bool RequireAssignedMechanic { get; set; } = false;
    private bool IsMechanicRequired => context is TenantCrmDbContext || RequireAssignedMechanic;
    private Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker ChangeTracker => context.ChangeTracker;
    private DbSet<Customer> Customers => context.Set<Customer>();
    private DbSet<JobOrder> JobOrders => context.Set<JobOrder>();
    private DbSet<JobTask> JobTasks => context.Set<JobTask>();
    private DbSet<Payment> Payments => context.Set<Payment>();
    private DbSet<Inquiry> Inquiries => context.Set<Inquiry>();
    private DbSet<Complaint> Complaints => context.Set<Complaint>();
    private DbSet<Feedback> Feedbacks => context.Set<Feedback>();
    private DbSet<AuditEntry> AuditEntries => context.Set<AuditEntry>();
    private string AuditActor => auditActor;
    private static DateTime WorkshopNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time"));
    public void Validate()
    {
        if (context is MasterCrmDbContext historicalDb)
            CRM.infrastructure.services.HistoricalCompatibility.Validate(historicalDb);
        if (context is MasterCrmDbContext { IsBackfillOperation: true })
        {
            return;
        }

        ChangeTracker.DetectChanges();
        var changes = ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        foreach (var entry in changes)
        {
            if (entry.Entity is Appointment appointment)
            {
                if (entry.State == EntityState.Deleted)
                    throw new InvalidOperationException("Preserve appointment history instead of deleting it.");
                if (string.IsNullOrWhiteSpace(appointment.Status))
                    appointment.Status = "Scheduled";
                if (!new[] { "Scheduled", "Checked In", "Cancelled", "No Show" }.Contains(appointment.Status))
                    throw new InvalidOperationException("Appointment status must be Scheduled, Checked In, Cancelled, or No Show.");
                var oldAppt = entry.State == EntityState.Added ? null : context.Set<Appointment>().AsNoTracking().FirstOrDefault(a => a.AppointmentId == appointment.AppointmentId);
                if (oldAppt != null)
                {
                    if (oldAppt.Status != "Scheduled" && appointment.Status != oldAppt.Status)
                        throw new InvalidOperationException($"Cannot change status of a {oldAppt.Status} appointment.");
                    if (oldAppt.LinkedJobOrderId.HasValue && appointment.LinkedJobOrderId != oldAppt.LinkedJobOrderId)
                        throw new InvalidOperationException("Cannot alter the linked job order of a checked-in appointment.");
                    if (oldAppt.Status == "Scheduled" && appointment.ScheduledStart != oldAppt.ScheduledStart)
                    {
                        AuditEntries.Add(new AuditEntry
                        {
                            OccurredAtUtc = DateTime.UtcNow,
                            Actor = AuditActor,
                            EntityType = "Appointment",
                            Action = "Rescheduled",
                            Changes = System.Text.Json.JsonSerializer.Serialize(new { appointment.AppointmentId, PreviousStart = oldAppt.ScheduledStart, NewStart = appointment.ScheduledStart })
                        });
                    }
                }
                if (appointment.Status == "Cancelled")
                {
                    if (string.IsNullOrWhiteSpace(appointment.CancellationReason))
                        throw new InvalidOperationException("A cancellation reason is required to cancel an appointment.");
                    appointment.CancelledAt = DateTime.UtcNow;
                    appointment.CancelledBy = string.IsNullOrWhiteSpace(appointment.CancelledBy) ? AuditActor : appointment.CancelledBy;
                }
                if (appointment.Status == "Checked In")
                {
                    if (appointment.LinkedJobOrderId == null && !ChangeTracker.Entries<JobOrder>().Any(j => j.State == EntityState.Added))
                        throw new InvalidOperationException("Checked In appointments must link to a created job order.");
                }
            }
            if (entry.Entity is Inquiry inquiry && entry.State != EntityState.Deleted && inquiry.Status == "Converted" && inquiry.ConvertedJobOrderId == null)
            {
                var priorStatus = entry.State == EntityState.Added ? null : Inquiries.AsNoTracking().Where(i => i.InquiryId == inquiry.InquiryId).Select(i => i.Status).FirstOrDefault();
                if (priorStatus != "Converted") throw new InvalidOperationException("Use Convert to Job Order to mark an inquiry as Converted.");
            }
            if (entry.Entity is Payment payment)
            {
                if (entry.State == EntityState.Deleted) throw new InvalidOperationException("Void the payment with a reason instead of deleting it.");
                var original = entry.State == EntityState.Added ? null : Payments.AsNoTracking().SingleOrDefault(p => p.PaymentId == payment.PaymentId);
                if (original?.IsVoided == true) throw new InvalidOperationException("Voided payments cannot be edited.");
                if (original != null && (payment.AmountPaid != original.AmountPaid || payment.JobOrderId != original.JobOrderId
                    || payment.PaymentMethod != original.PaymentMethod || payment.PaymentType != original.PaymentType
                    || payment.CreatedAt != original.CreatedAt || payment.SubmissionId != original.SubmissionId
                    || payment.AllowCustomerCredit != original.AllowCustomerCredit
                    || (context is MasterCrmDbContext && (payment.PayerType != original.PayerType || payment.PaymentPurpose != original.PaymentPurpose))))
                    throw new InvalidOperationException("Saved receipts cannot be changed. Void the incorrect receipt with a reason, then record a replacement payment.");
                if (original == null && payment.IsVoided)
                    throw new InvalidOperationException("Record a payment before voiding it.");
                if (original == null && !new[] { "Cash", "Card", "GCash", "Transfer", "Credit Card", "Debit Card", "Bank Transfer", "Check" }.Contains(payment.PaymentMethod))
                    throw new InvalidOperationException("Choose a supported payment method.");
                if (original == null && string.IsNullOrWhiteSpace(payment.PaymentType))
                    throw new InvalidOperationException("Choose a payment category.");
                if (original == null && context is MasterCrmDbContext mDbPay && mDbPay.JobCancellationRequests.Any(r => r.JobOrderId == payment.JobOrderId && (r.Status == "Pending Review" || r.Status == "Pending Refund" || r.Status == "Requires Information")))
                    throw new InvalidOperationException("Cannot record payments while a cancellation request is pending review.");
                if (payment.IsVoided)
                {
                    if (string.IsNullOrWhiteSpace(payment.VoidReason)) throw new InvalidOperationException("A reason is required to void a payment.");
                    if (context is MasterCrmDbContext mDbVoid && mDbVoid.Refunds.Any(r => r.PaymentId == payment.PaymentId || r.JobOrderId == payment.JobOrderId))
                        throw new InvalidOperationException("This receipt has recorded refunds linked to it. Payments with associated refunds cannot be voided.");
                    payment.VoidedAtUtc = DateTime.UtcNow;
                    var paymentJob = JobOrders.AsNoTracking().SingleOrDefault(j => j.JobOrderId == payment.JobOrderId);
                    if (paymentJob?.Status == "Released")
                    {
                        decimal remainingPaid = Payments.Where(p => p.JobOrderId == paymentJob.JobOrderId && !p.IsVoided && p.PaymentId != payment.PaymentId).Sum(p => (decimal?)p.AmountPaid) ?? 0;
                        if (paymentJob.TotalEstimatedCost.HasValue && remainingPaid < paymentJob.TotalEstimatedCost.Value)
                            throw new InvalidOperationException("Released jobs cannot become unpaid through an ordinary payment void.");
                    }
                }
            }
            if (entry.Entity is Customer && entry.State == EntityState.Deleted)
                throw new InvalidOperationException("Deactivate the customer instead of deleting their history.");
            if (entry.Entity is JobOrder job)
            {
                var old = entry.State == EntityState.Added ? null : JobOrders.FromSqlInterpolated($"SELECT * FROM JobOrders WITH (UPDLOCK,HOLDLOCK) WHERE JobOrderId = {job.JobOrderId}").IgnoreAutoIncludes().AsNoTracking().SingleOrDefault();
                if (entry.State == EntityState.Deleted)
                {
                    throw new InvalidOperationException($"Physical deletion of Job Order #{job.JobOrderId} is permanently prohibited. Use Archive Record instead.");
                }
                if (old == null || old.CustomerId != job.CustomerId)
                    if (job.CustomerId == null || !Customers.Any(c => c.CustomerId == job.CustomerId && c.IsActive))
                        throw new InvalidOperationException("Select an active customer before creating or reassigning a job.");
                if (job.TotalEstimatedCost is decimal cost && (cost < 0 || decimal.Round(cost, 2) != cost))
                    throw new InvalidOperationException("Estimated cost must be nonnegative with at most two decimal places.");
                if (job.Status == "QA Checking" && old?.Status != "QA Checking")
                    throw new InvalidOperationException("Final inspection is part of In Progress. Complete the job after inspection approval.");
                if (old != null && (old.Status is "Completed" or "Released") &&
                    (old.AssignedMechanicUserId != job.AssignedMechanicUserId || old.AssignedMechanic != job.AssignedMechanic))
                    throw new InvalidOperationException("Completed or released jobs cannot be reassigned.");

                bool mechanicChanged = old == null
                    ? (context is MasterCrmDbContext && !string.IsNullOrWhiteSpace(job.AssignedMechanicUserId))
                    : (old.AssignedMechanicUserId != job.AssignedMechanicUserId && !string.IsNullOrWhiteSpace(job.AssignedMechanicUserId));
                if (mechanicChanged)
                {
                    ValidateMechanicEligibility(job, job.AssignedMechanicUserId!);
                }

                job.Status = JobOrderWorkflow.Normalize(job.Status);
                decimal paid = Payments.Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided).Sum(p => (decimal?)p.AmountPaid) ?? 0;

                if (job.Status == "Cancelled")
                {
                    if (old == null)
                        throw new InvalidOperationException("New jobs start in queue. Cannot create a job directly as Cancelled.");
                    if (old.Status is "Completed" or "Released")
                        throw new InvalidOperationException("Completed or released jobs cannot be cancelled. Use warranty or correction workflows.");
                    if (paid > 0)
                    {
                        bool hasCompletedReview = context is MasterCrmDbContext mDb &&
                            (mDb.JobCancellationRequests.Local.Any(r => r.JobOrderId == job.JobOrderId && (r.Status == "Completed" || r.Status == "Approved" || r.Status == "Pending Refund")) ||
                             mDb.JobCancellationRequests.Any(r => r.JobOrderId == job.JobOrderId && (r.Status == "Completed" || r.Status == "Approved" || r.Status == "Pending Refund")));
                        if (!hasCompletedReview)
                        {
                            throw new InvalidOperationException($"This job has recorded payments of PHP {paid:N2}. Cancellation with existing payments requires financial refund review.");
                        }
                    }
                    if (string.IsNullOrWhiteSpace(job.CancellationReason))
                        throw new InvalidOperationException("A cancellation reason is required to cancel this job.");

                    job.CancelledAtUtc = DateTime.UtcNow;
                    job.CancelledBy = string.IsNullOrWhiteSpace(job.CancelledBy) ? AuditActor : job.CancelledBy;

                    // Make unfinished tasks non-actionable
                    var unfinishedTasks = JobTasks.Where(t => t.JobOrderId == job.JobOrderId && t.Status != "Done" && t.Status != "Completed").ToList();
                    foreach (var t in unfinishedTasks)
                    {
                        t.Status = "Cancelled";
                    }

                    AuditEntries.Add(new AuditEntry
                    {
                        OccurredAtUtc = DateTime.UtcNow,
                        Actor = AuditActor,
                        EntityType = "JobOrder",
                        Action = "JobCancelled",
                        Changes = System.Text.Json.JsonSerializer.Serialize(new { job.JobOrderId, Reason = job.CancellationReason, CancelledBy = job.CancelledBy })
                    });
                    continue;
                }

                if (old?.Status == "Cancelled")
                    throw new InvalidOperationException("Cancelled jobs are in a terminal state and cannot continue ordinary workshop processing.");

                bool isCancellationReviewActiveForJob = context is MasterCrmDbContext mDbStage &&
                    (mDbStage.JobCancellationRequests.Local.Any(r => r.JobOrderId == job.JobOrderId && (r.Status == "Pending Review" || r.Status == "Requires Information")) ||
                     (!mDbStage.JobCancellationRequests.Local.Any(r => r.JobOrderId == job.JobOrderId && (r.Status == "Completed" || r.Status == "Rejected" || r.Status == "Withdrawn")) &&
                      mDbStage.JobCancellationRequests.Any(r => r.JobOrderId == job.JobOrderId && (r.Status == "Pending Review" || r.Status == "Pending Refund" || r.Status == "Requires Information"))));

                if (isCancellationReviewActiveForJob && job.Status != "Cancelled")
                {
                    if (old != null && old.Status != job.Status)
                        throw new InvalidOperationException("This job order has a pending cancellation request under review. Ordinary workshop processing is suspended.");
                }

                int next = JobOrderWorkflow.Stage(job.Status);
                int previous = old == null ? 0 : JobOrderWorkflow.Stage(old.Status);
                if (next < 0 || (old == null && next != 0) || (old != null && next != previous && next != previous + 1))
                    throw new InvalidOperationException("Move jobs one step at a time: Waiting, In Progress, Completed, Released.");
                if (previous != next && next > 0)
                {
                    if (job.TotalEstimatedCost == null) throw new InvalidOperationException("Set the estimated cost before progressing this job.");
                    if (next == 1)
                    {
                        if (paid < decimal.Round(job.TotalEstimatedCost.Value * 0.5m, 2, MidpointRounding.AwayFromZero))
                            throw new InvalidOperationException("A 50% downpayment is required before work starts.");

                        if (IsMechanicRequired)
                        {
                            if (string.IsNullOrWhiteSpace(job.AssignedMechanicUserId))
                            {
                                if (!string.IsNullOrWhiteSpace(job.AssignedMechanic) && !job.AssignedMechanic.Trim().Equals("Unassigned", StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new InvalidOperationException($"This job has an unresolved legacy mechanic name '{job.AssignedMechanic}'. Use Assign Mechanic to select an active, eligible workshop staff member before starting work.");
                                }
                                throw new InvalidOperationException("Assign an active eligible mechanic before starting work.");
                            }

                            ValidateMechanicEligibility(job, job.AssignedMechanicUserId);
                        }
                        else if (!string.IsNullOrWhiteSpace(job.AssignedMechanicUserId))
                        {
                            ValidateMechanicEligibility(job, job.AssignedMechanicUserId);
                        }
                    }
                    if (next == 1 && old != null && old.AssignedMechanicUserId != job.AssignedMechanicUserId && !string.IsNullOrWhiteSpace(job.AssignedMechanicUserId))
                    {
                        ValidateMechanicEligibility(job, job.AssignedMechanicUserId);
                    }
                    if (next >= 2 && (JobTasks.Any(t => t.JobOrderId == job.JobOrderId && t.Status != "Done" && t.Status != "Completed") ||
                        changes.Any(e => e.Entity is JobTask t && t.JobOrderId == job.JobOrderId && t.Status != "Done" && t.Status != "Completed" && e.State != EntityState.Deleted)))
                        throw new InvalidOperationException("Finish all job tasks before final inspection or completion.");
                }
                if (next == 3 && (job.TotalEstimatedCost == null || paid < job.TotalEstimatedCost))
                    throw new InvalidOperationException("Pay the outstanding balance before releasing the vehicle.");
                if (old?.CompletedAt != null && job.StartedAt != old.StartedAt) throw new InvalidOperationException("Start time cannot change after completion.");
                job.CompletedAt = old?.CompletedAt;
                if (next == 2 && previous != next)
                {
                    if (context is MasterCrmDbContext legacy && !legacy.FinalInspectionConfirmed)
                        throw new InvalidOperationException("Confirm that the final inspection passed before completing this job.");
                    if (job.StartedAt > WorkshopNow) throw new InvalidOperationException("Start time cannot be after completion.");
                    job.CompletedAt = WorkshopNow;
                    if (context is MasterCrmDbContext)
                        AuditEntries.Add(new AuditEntry { OccurredAtUtc = DateTime.UtcNow, Actor = AuditActor,
                            EntityType = "JobOrder", Action = "FinalInspectionApproved",
                            Changes = System.Text.Json.JsonSerializer.Serialize(new { job.JobOrderId, Approved = true }) });
                }
            }
            if (entry.Entity is JobTask task)
            {
                if (entry.State == EntityState.Deleted)
                {
                    throw new InvalidOperationException($"Physical deletion of Job Task #{task.JobTaskId} is permanently prohibited. Use Archive Record or service withdrawal instead.");
                }
                var taskJobEntry = ChangeTracker.Entries<JobOrder>().FirstOrDefault(e => (task.JobOrderId > 0 && e.Entity.JobOrderId == task.JobOrderId) || (e.State == EntityState.Added && e.Entity.Tasks.Contains(task)));
                var taskJob = taskJobEntry?.Entity;
                if (taskJobEntry != null && taskJobEntry.State == EntityState.Unchanged && task.JobOrderId > 0)
                {
                    taskJobEntry.Reload();
                    taskJob = taskJobEntry.State == EntityState.Detached ? null : taskJobEntry.Entity;
                }
                if (taskJob == null && task.JobOrderId > 0)
                    taskJob = JobOrders.FromSqlInterpolated($"SELECT * FROM JobOrders WITH (UPDLOCK,HOLDLOCK) WHERE JobOrderId = {task.JobOrderId}").IgnoreAutoIncludes().AsNoTracking().SingleOrDefault();
                if (!new[] { "Pending", "Ongoing", "Done", "Completed", "Cancelled", "Withdrawn", "In Progress", "In Queue" }.Contains(task.Status))
                    throw new InvalidOperationException("Choose Pending, Ongoing, or Done for the task.");
                if (taskJob == null) throw new InvalidOperationException("Select an existing job for this task.");

                if (taskJob.IsArchived)
                    throw new InvalidOperationException($"Parent Job Order #{taskJob.JobOrderId} is archived. Tasks and remarks cannot be modified on archived jobs.");

                bool isCancellationReviewActiveForTask = context is MasterCrmDbContext mDbTask &&
                    (mDbTask.JobCancellationRequests.Local.Any(r => r.JobOrderId == taskJob.JobOrderId && (r.Status == "Pending Review" || r.Status == "Requires Information")) ||
                     (!mDbTask.JobCancellationRequests.Local.Any(r => r.JobOrderId == taskJob.JobOrderId && (r.Status == "Completed" || r.Status == "Rejected" || r.Status == "Withdrawn")) &&
                      mDbTask.JobCancellationRequests.Any(r => r.JobOrderId == taskJob.JobOrderId && (r.Status == "Pending Review" || r.Status == "Pending Refund" || r.Status == "Requires Information"))));

                if (isCancellationReviewActiveForTask && task.Status != "Cancelled" && task.Status != "Withdrawn")
                {
                    if (entry.State == EntityState.Added || (entry.State == EntityState.Modified && entry.Properties.Any(p => p.IsModified && p.Metadata.Name is "Status" or "Remarks" or "ServiceId" or "JobOrderId")))
                        throw new InvalidOperationException("Cannot modify tasks while a cancellation request is pending review.");
                }

                if (taskJob.Status is "Completed" or "Released")
                {
                    if (entry.State == EntityState.Added)
                        throw new InvalidOperationException($"Cannot add tasks to a {taskJob.Status} job.");
                    if (entry.State == EntityState.Modified && entry.Properties.Any(p => p.IsModified && p.Metadata.Name is "Status" or "Remarks" or "JobOrderId" or "ServiceId"))
                        throw new InvalidOperationException($"Parent Job Order #{taskJob.JobOrderId} is {taskJob.Status}. Tasks and remarks cannot be modified on closed jobs.");
                }
                if (taskJob.Status == "Cancelled")
                {
                    if (entry.State == EntityState.Added)
                        throw new InvalidOperationException("Cannot add tasks to a Cancelled job.");
                    if (entry.State == EntityState.Modified && (task.Status != "Cancelled" || entry.Properties.Any(p => p.IsModified && p.Metadata.Name == "Remarks")))
                        throw new InvalidOperationException($"Parent Job Order #{taskJob.JobOrderId} is Cancelled. Tasks and remarks cannot be modified on cancelled jobs.");
                }

                var previousTask = entry.State == EntityState.Added ? null : JobTasks.AsNoTracking().SingleOrDefault(t => t.JobTaskId == task.JobTaskId);

                if (previousTask != null)
                {
                    if (previousTask.ServiceId != task.ServiceId || previousTask.JobOrderId != task.JobOrderId)
                        throw new InvalidOperationException("Task service identity and job order linkage are immutable.");

                    if (previousTask.IsArchived && !task.IsArchived && task.Status != "Pending")
                        throw new InvalidOperationException($"Archived task #{task.JobTaskId} cannot be modified.");

                    if ((previousTask.Status is "Done" or "Completed") && (task.Status is "Pending" or "Ongoing"))
                    {
                        bool isAuthorizedRework = context is TenantCrmDbContext { IsReviewOperation: true };
                        if (!isAuthorizedRework)
                            throw new InvalidOperationException($"Completed task #{task.JobTaskId} cannot be moved back to {task.Status}. Reversing completed task status is prohibited.");
                    }

                    if (previousTask.Status == "Ongoing" && task.Status == "Pending")
                        throw new InvalidOperationException($"Started task #{task.JobTaskId} cannot be reverted to Pending.");
                }

                if (taskJob.Status == "In Queue" && task.Status != "Pending" && task.Status != "Cancelled" && task.Status != "Withdrawn" &&
                    (previousTask == null || previousTask.Status != task.Status || previousTask.JobOrderId != task.JobOrderId))
                    throw new InvalidOperationException("Move the job to In Progress after recording its required 50% downpayment before starting or completing tasks.");
            }
            if (entry.Entity is Feedback feedback && entry.State != EntityState.Deleted)
            {
                if (feedback.RatingScore < 1 || feedback.RatingScore > 5) throw new InvalidOperationException("Feedback rating must be between 1 and 5.");
                if (feedback.IsActive && feedback.RatingScore <= 2 &&
                    (feedback.FeedbackId <= 0 || !Complaints.Any(c => c.SourceFeedbackId == feedback.FeedbackId)) &&
                    !ChangeTracker.Entries<Complaint>().Any(c => c.Entity.SourceFeedback == feedback))
                    Complaints.Add(new Complaint { SourceFeedback = feedback, JobOrderId = feedback.JobOrderId,
                        ComplaintType = "Low-rating follow-up", Description = "Urgent follow-up for feedback rated " + feedback.RatingScore + "/5. " + feedback.AreasForImprovement,
                        IsUrgent = true, ResolutionStatus = "Open" });
            }
        }
        // Validate aggregate amounts including this save's payment changes.
        var paymentChanges = ChangeTracker.Entries<Payment>().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        var affected = new HashSet<int>();
        foreach (var e in paymentChanges)
        {
            affected.Add(e.Entity.JobOrderId);
            var old = Payments.AsNoTracking().SingleOrDefault(p => p.PaymentId == e.Entity.PaymentId);
            if (old != null) affected.Add(old.JobOrderId);
            if (e.State != EntityState.Deleted && (e.Entity.AmountPaid <= 0 || decimal.Round(e.Entity.AmountPaid, 2) != e.Entity.AmountPaid))
                throw new InvalidOperationException("Payment must be positive with at most two decimal places.");
        }
        foreach (int id in affected.Where(x => x > 0).Order())
        {
            // Serialize payment writers and release decisions on the same parent.
            var job = JobOrders.FromSqlInterpolated($"SELECT * FROM JobOrders WITH (UPDLOCK, HOLDLOCK) WHERE JobOrderId = {id}").IgnoreAutoIncludes().AsNoTracking().SingleOrDefault();
            if (job == null) continue;
            if (job.TotalEstimatedCost == null) throw new InvalidOperationException("Set the job's estimated cost before recording payments.");
            var rows = Payments.AsNoTracking().Where(p => p.JobOrderId == id).ToList();
            decimal before = rows.Where(p => !p.IsVoided).Sum(p => p.AmountPaid);
            foreach (var e in paymentChanges)
            {
                rows.RemoveAll(p => p.PaymentId == e.Entity.PaymentId && e.Entity.PaymentId != 0);
                if (e.State != EntityState.Deleted && e.Entity.JobOrderId == id) rows.Add(e.Entity);
            }
            decimal after = rows.Where(p => !p.IsVoided).Sum(p => p.AmountPaid);
            if (after > job.TotalEstimatedCost && after > before &&
                paymentChanges.Any(e => e.Entity.JobOrderId == id && e.State != EntityState.Deleted && !e.Entity.AllowCustomerCredit))
                throw new InvalidOperationException("Payment exceeds the outstanding balance. Explicitly allow customer credit to continue.");
            var pendingJob = ChangeTracker.Entries<JobOrder>().FirstOrDefault(e => e.Entity.JobOrderId == id && e.State == EntityState.Modified)?.Entity ?? job;
            if (pendingJob.Status == "Released" && after < pendingJob.TotalEstimatedCost)
                throw new InvalidOperationException("This change would leave a released job with an unpaid balance.");
        }
        var complaintEntries = ChangeTracker.Entries<Complaint>().Where(e => e.State == EntityState.Added).ToList();
        if (complaintEntries.Count > 0)
        {
            var jobIds = complaintEntries.Select(e => e.Entity.JobOrderId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var completedMap = JobOrders.Where(j => jobIds.Contains(j.JobOrderId)).Select(j => new { j.JobOrderId, j.CompletedAt }).ToDictionary(x => x.JobOrderId, x => x.CompletedAt);
            foreach (var e in complaintEntries)
            {
                DateTime? completed = e.Entity.JobOrderId.HasValue && completedMap.TryGetValue(e.Entity.JobOrderId.Value, out var cAt) ? cAt : null;
                e.Entity.UnderWarrantyService = completed.HasValue && completed <= WorkshopNow && completed >= WorkshopNow.AddDays(-30);
            }
        }
        foreach (var e in ChangeTracker.Entries().Where(e => e.Entity is not AuditEntry && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList())
        {
            var values = e.Properties.Where(p => p.Metadata.Name != "RowVersion" && (e.State != EntityState.Modified || p.IsModified))
                .ToDictionary(p => p.Metadata.Name, p => new { Before = e.State == EntityState.Added ? null : p.OriginalValue, After = p.CurrentValue });
            if (e.Metadata.ClrType.Namespace != "crm.domain.entities") continue;
            AuditEntries.Add(new AuditEntry { OccurredAtUtc = DateTime.UtcNow, Actor = AuditActor,
                EntityType = e.Metadata.ClrType.Name, Action = e.State.ToString(), Changes = System.Text.Json.JsonSerializer.Serialize(values) });
        }
    }

    private void ValidateMechanicEligibility(JobOrder job, string mechanicUserId)
    {
        var validation = CRM.infrastructure.services.MechanicEligibilityService.ValidateAssignment(context, job, mechanicUserId);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.ErrorMessage);
    }
}
