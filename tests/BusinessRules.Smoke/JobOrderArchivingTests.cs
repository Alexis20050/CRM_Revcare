#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace BusinessRules.Smoke
{
    public static class JobOrderArchivingTests
    {
        public static async Task RunAsync(MasterCrmDbContext db, Func<MasterCrmDbContext> factory, Action<bool, string> check, Action<Action, string> reject)
        {
            Console.WriteLine("\n[STAGE 6 & 10] Running Job Order & Job Task Archiving and Readiness Tests...");

            // 1. Physical deletion prohibition
            var testCust = new Customer { FullName = "Deletion Protection Test", ContactNumber = "09170001111", IsActive = true };
            db.Add(testCust);
            db.SaveChanges();

            var protectJob = new JobOrder
            {
                CustomerId = testCust.CustomerId,
                EstimateId = "Protection Test",
                TotalEstimatedCost = 2500m,
                StartedAt = DateTime.Now
            };
            db.Add(protectJob);
            db.SaveChanges();

            var protectTask = new JobTask
            {
                JobOrderId = protectJob.JobOrderId,
                ServiceId = "Brake inspection",
                Status = "Pending"
            };
            db.Add(protectTask);
            db.SaveChanges();

            reject(() =>
            {
                using var c = factory();
                c.Remove(c.JobOrders.First(j => j.JobOrderId == protectJob.JobOrderId));
                c.SaveChanges();
            }, "Archive: Physical deletion of Job Order is permanently rejected");

            reject(() =>
            {
                using var c = factory();
                c.Remove(c.JobTasks.First(t => t.JobTaskId == protectTask.JobTaskId));
                c.SaveChanges();
            }, "Archive: Physical deletion of Job Task is permanently rejected");

            // 2. Centralized Task Progress calculation
            var sampleTasks = new List<JobTask>
            {
                new() { JobTaskId = 1, ServiceId = "S1", Status = "Done" },
                new() { JobTaskId = 2, ServiceId = "S2", Status = "Done" },
                new() { JobTaskId = 3, ServiceId = "S3", Status = "Ongoing" },
                new() { JobTaskId = 4, ServiceId = "S4", Status = "Pending" },
                new() { JobTaskId = 5, ServiceId = "S5", Status = "Cancelled" } // Excluded from required denominator
            };
            var progress = WorkshopReadinessService.CalculateTaskProgress(sampleTasks);
            check(progress.TotalTasks == 4, "Progress: Cancelled tasks excluded from required total");
            check(progress.CompletedTasks == 2, "Progress: Completed tasks counted accurately");
            check(progress.OngoingTasks == 1, "Progress: Ongoing tasks counted accurately");
            check(progress.PendingTasks == 1, "Progress: Pending tasks counted accurately");
            check(progress.CompletionPercentage == 50.0m, "Progress: 2/4 completed equals 50.0%");

            var emptyProgress = WorkshopReadinessService.CalculateTaskProgress(new List<JobTask>());
            check(emptyProgress.CompletionPercentage == 0m && emptyProgress.TotalTasks == 0, "Progress: Zero tasks handled safely without division by zero");

            // 3. Centralized Readiness Evaluator & Archiving Eligibility
            var queuedJob = new JobOrder
            {
                CustomerId = testCust.CustomerId,
                EstimateId = "Readiness Queued",
                TotalEstimatedCost = 3000m,
                Status = "In Queue",
                StartedAt = DateTime.Now
            };
            var checkQueueArchive = WorkshopReadinessService.EvaluateArchive(db, queuedJob, "Testing queue archive");
            check(!checkQueueArchive.IsPermitted, "Readiness: In Queue job rejected from archiving");

            var inProgMem = new JobOrder { CustomerId = testCust.CustomerId, EstimateId = "Readiness In Prog", TotalEstimatedCost = 3000m, Status = "In Progress", StartedAt = DateTime.Now };
            var checkProgArchive = WorkshopReadinessService.EvaluateArchive(db, inProgMem, "Testing in progress archive");
            check(!checkProgArchive.IsPermitted, "Readiness: In Progress job rejected from archiving");

            // 4. Proper lifecycle progression through rules to Released status
            var realJob = new JobOrder
            {
                CustomerId = testCust.CustomerId,
                EstimateId = "Archiving Full Lifecycle",
                StartedAt = DateTime.Now.AddHours(-6)
            };
            var mech = db.Staff.First(s => s.IsActive && s.Role == "Lead Mechanic");
            realJob.AssignedMechanicUserId = mech.StaffId;
            realJob.AssignedMechanic = mech.DisplayName;
            JobServices.Assign(db, realJob, new[] { "Oil and filter change" });
            realJob.TotalEstimatedCost = 1500m;
            db.Add(realJob);
            db.SaveChanges();
            int rId = realJob.JobOrderId;

            // Pay 50% deposit
            using (var c = factory()) { c.Add(new Payment { JobOrderId = rId, AmountPaid = 750m, PaymentMethod = "Cash", PaymentType = "Initial" }); c.SaveChanges(); }

            // Move to In Progress
            using (var c = factory()) { var j = c.JobOrders.Find(rId)!; j.Status = "In Progress"; c.SaveChanges(); }

            // Complete task
            using (var c = factory()) { var t = c.JobTasks.First(x => x.JobOrderId == rId); t.Status = "Done"; c.SaveChanges(); }

            // Move to Completed with final inspection confirmed
            using (var c = factory()) { c.FinalInspectionConfirmed = true; var j = c.JobOrders.Find(rId)!; j.Status = "Completed"; j.CompletedAt = DateTime.Now; c.SaveChanges(); }

            // Verify Completed but unreleased job rejected from archiving
            using (var c = factory())
            {
                var j = c.JobOrders.Include(x => x.Payments).First(x => x.JobOrderId == rId);
                var checkCompArchive = WorkshopReadinessService.EvaluateArchive(c, j, "Testing completed unreleased archive");
                check(!checkCompArchive.IsPermitted, "Readiness: Completed but unreleased job rejected from archiving");
            }

            // Pay remaining balance to settle
            using (var c = factory()) { c.Add(new Payment { JobOrderId = rId, AmountPaid = 750m, PaymentMethod = "Cash", PaymentType = "Final" }); c.SaveChanges(); }

            // Move to Released
            using (var c = factory()) { var j = c.JobOrders.Find(rId)!; j.Status = "Released"; c.SaveChanges(); }

            // 5. Released job fully settled is eligible for archiving
            using (var c = factory())
            {
                var j = c.JobOrders.Include(x => x.Payments).First(x => x.JobOrderId == rId);
                var checkSettledArchive = WorkshopReadinessService.EvaluateArchive(c, j, "Settled closeout");
                check(checkSettledArchive.IsPermitted, "Readiness: Released job fully settled is eligible for archiving");
            }

            // 6. Execute Archiving Transaction
            var archResult = await JobOrderArchivingService.ArchiveJobOrderAsync(factory, rId, "Completed closeout and archival", "Lead Admin");
            check(archResult.IsPermitted, "Archive: Job Order archived successfully");

            using (var c = factory())
            {
                var j = c.JobOrders.Find(rId)!;
                check(j.IsArchived, "Archive: IsArchived flag set to true");
                check(j.ArchivedAtUtc.HasValue, "Archive: ArchivedAtUtc timestamp recorded");
                check(j.ArchivedBy == "Lead Admin", "Archive: ArchivedBy actor preserved");
                check(j.ArchiveReason == "Completed closeout and archival", "Archive: ArchiveReason preserved");
                check(j.Status == "Released", "Archive: Operational status remains Released (archiving does not alter operational status)");

                var audit = c.AuditEntries.FirstOrDefault(a => a.EntityType == "JobOrder" && a.Action == "Archived");
                check(audit != null, "Archive: AuditEntry recorded atomically");
            }

            // Duplicate archive rejected
            var dupeArch = await JobOrderArchivingService.ArchiveJobOrderAsync(factory, rId, "Archiving again", "Lead Admin");
            check(!dupeArch.IsPermitted, "Archive: Duplicate archival rejected");

            // 7. Execute Restoration Transaction
            var restoreResult = await JobOrderArchivingService.RestoreJobOrderAsync(factory, rId, "Reopened for warranty inspection", "Lead Admin");
            check(restoreResult.IsPermitted, "Restore: Job Order restored successfully");

            using (var c = factory())
            {
                var j = c.JobOrders.Find(rId)!;
                check(!j.IsArchived, "Restore: IsArchived flag set to false");
                check(j.Status == "Released", "Restore: Operational status preserved as Released");

                var audit = c.AuditEntries.FirstOrDefault(a => a.EntityType == "JobOrder" && a.Action == "Restored");
                check(audit != null, "Restore: AuditEntry recorded for restoration");
            }

            // 8. Job Task Archival / Withdrawal
            var taskJob = new JobOrder
            {
                CustomerId = testCust.CustomerId,
                EstimateId = "Task Archive Job",
                TotalEstimatedCost = 1500m,
                Status = "In Queue",
                StartedAt = DateTime.Now
            };
            db.Add(taskJob);
            db.SaveChanges();

            var taskToArchive = new JobTask
            {
                JobOrderId = taskJob.JobOrderId,
                ServiceId = "Exterior detailing",
                Status = "Pending"
            };
            db.Add(taskToArchive);
            db.SaveChanges();

            var taskArchResult = await JobOrderArchivingService.ArchiveJobTaskAsync(factory, taskToArchive.JobTaskId, "Customer declined detailing", "Service Advisor");
            check(taskArchResult.IsPermitted, "Task Archive: Pending task withdrawn and archived successfully");

            using (var c = factory())
            {
                var t = c.JobTasks.Find(taskToArchive.JobTaskId)!;
                check(t.IsArchived, "Task Archive: Task IsArchived flag set to true");
                check(t.Status == "Withdrawn", "Task Archive: Task Status updated to Withdrawn");
            }

            var taskRestoreResult = await JobOrderArchivingService.RestoreJobTaskAsync(factory, taskToArchive.JobTaskId, "Customer requested detailing re-addition", "Service Advisor");
            check(taskRestoreResult.IsPermitted, "Task Restore: Task restored successfully");

            using (var c = factory())
            {
                var t = c.JobTasks.Find(taskToArchive.JobTaskId)!;
                check(!t.IsArchived, "Task Restore: Task IsArchived flag set to false");
                check(t.Status == "Pending", "Task Restore: Task status reset to Pending");
            }

            // 9. Cancelled Job Archival
            var cancelledJob = new JobOrder
            {
                CustomerId = testCust.CustomerId,
                EstimateId = "Cancelled Job",
                TotalEstimatedCost = 1000m,
                StartedAt = DateTime.Now
            };
            db.Add(cancelledJob);
            db.SaveChanges();

            using (var c = factory())
            {
                var j = c.JobOrders.Find(cancelledJob.JobOrderId)!;
                j.Status = "Cancelled";
                j.CancellationReason = "Customer request";
                c.SaveChanges();
            }

            var cancelArchive = await JobOrderArchivingService.ArchiveJobOrderAsync(factory, cancelledJob.JobOrderId, "Resolved cancellation archiving", "Lead Admin");
            check(cancelArchive.IsPermitted, "Archive: Resolved cancelled job archived successfully");
        }
    }
}
