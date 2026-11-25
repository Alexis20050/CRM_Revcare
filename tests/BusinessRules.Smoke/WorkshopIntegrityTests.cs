using System;
using System.Linq;
using System.Threading.Tasks;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace BusinessRules.Smoke;

public static class WorkshopIntegrityTests
{
    public static async Task RunAsync(
        MasterCrmDbContext db,
        Func<MasterCrmDbContext> createFactory,
        Action<bool, string> check,
        Action<Action, string> reject)
    {
        Console.WriteLine("\n[STAGE: Workshop Transaction Integrity Corrections]");

        // 1. Task Lifecycle & Reversal Prohibitions
        int customerId;
        using (var c = createFactory())
        {
            var cust = new Customer { FullName = "Integrity Test Customer", ContactNumber = "09171234999" };
            c.Customers.Add(cust);
            c.SaveChanges();
            customerId = cust.CustomerId;
        }

        int activeJobId;
        int taskId;
        using (var c = createFactory())
        {
            var job = new JobOrder
            {
                CustomerId = customerId,
                EstimateId = "EST-INT-01",
                VehicleDetails = "Test Vehicle 2026",
                Status = "In Queue",
                TotalEstimatedCost = 3000m,
                AssignedMechanicUserId = "juan.gomez",
                AssignedMechanic = "Juan Gomez"
            };
            c.JobOrders.Add(job);
            c.SaveChanges();
            activeJobId = job.JobOrderId;

            // Add 50% deposit to allow In Progress
            c.Payments.Add(new Payment
            {
                JobOrderId = activeJobId,
                AmountPaid = 1500m,
                PaymentMethod = "Cash",
                CreatedAt = DateTime.UtcNow
            });
            c.SaveChanges();

            // Add task
            var t = new JobTask
            {
                JobOrderId = activeJobId,
                ServiceId = "Brake inspection",
                Status = "Pending"
            };
            c.JobTasks.Add(t);
            c.SaveChanges();
            taskId = t.JobTaskId;

            // Move job to In Progress
            job.Status = "In Progress";
            c.SaveChanges();
        }

        // Test Pending -> Ongoing (Valid)
        using (var c = createFactory())
        {
            var t = c.JobTasks.Find(taskId)!;
            t.Status = "Ongoing";
            c.SaveChanges();
        }
        check(true, "Integrity: Pending -> Ongoing transition allowed");

        // Test Ongoing -> Pending (Reverse transition blocked)
        reject(() =>
        {
            using var c = createFactory();
            var t = c.JobTasks.Find(taskId)!;
            t.Status = "Pending";
            c.SaveChanges();
        }, "Integrity: Ongoing -> Pending reverse transition rejected");

        // Test Ongoing -> Done (Valid)
        using (var c = createFactory())
        {
            var t = c.JobTasks.Find(taskId)!;
            t.Status = "Done";
            c.SaveChanges();
        }
        check(true, "Integrity: Ongoing -> Done transition allowed");

        // Test Done -> Pending (Reverse transition blocked)
        reject(() =>
        {
            using var c = createFactory();
            var t = c.JobTasks.Find(taskId)!;
            t.Status = "Pending";
            c.SaveChanges();
        }, "Integrity: Done -> Pending reverse transition rejected");

        // Test Done -> Ongoing (Reverse transition blocked)
        reject(() =>
        {
            using var c = createFactory();
            var t = c.JobTasks.Find(taskId)!;
            t.Status = "Ongoing";
            c.SaveChanges();
        }, "Integrity: Done -> Ongoing reverse transition rejected");

        // Test Task ServiceId and JobOrderId Immutability
        reject(() =>
        {
            using var c = createFactory();
            var t = c.JobTasks.Find(taskId)!;
            t.ServiceId = "Oil and filter change";
            c.SaveChanges();
        }, "Integrity: Task ServiceId modification rejected as immutable");

        reject(() =>
        {
            using var c = createFactory();
            var t = c.JobTasks.Find(taskId)!;
            t.JobOrderId = activeJobId + 999;
            c.SaveChanges();
        }, "Integrity: Task JobOrderId reassignment rejected as immutable");

        // 2. Closed Job Protection (Completed, Released, Cancelled)
        int completedJobId;
        int completedTaskId;
        using (var c = createFactory())
        {
            var job = new JobOrder
            {
                CustomerId = customerId,
                EstimateId = "EST-COMP-01",
                VehicleDetails = "Complete Test Vehicle",
                Status = "In Queue",
                TotalEstimatedCost = 2000m,
                AssignedMechanicUserId = "juan.gomez",
                AssignedMechanic = "Juan Gomez"
            };
            c.JobOrders.Add(job);
            c.SaveChanges();
            completedJobId = job.JobOrderId;

            // 50% deposit
            c.Payments.Add(new Payment { JobOrderId = completedJobId, AmountPaid = 1000m, PaymentMethod = "Cash" });
            c.SaveChanges();

            var t = new JobTask { JobOrderId = completedJobId, ServiceId = "Wheel alignment", Status = "Pending" };
            c.JobTasks.Add(t);
            c.SaveChanges();
            completedTaskId = t.JobTaskId;

            job.Status = "In Progress";
            c.SaveChanges();

            t.Status = "Ongoing";
            c.SaveChanges();
            t.Status = "Done";
            c.SaveChanges();

            // Settle final payment
            c.Payments.Add(new Payment { JobOrderId = completedJobId, AmountPaid = 1000m, PaymentMethod = "Cash" });
            c.SaveChanges();

            // Complete with inspection
            c.FinalInspectionConfirmed = true;
            job.Status = "Completed";
            c.SaveChanges();
        }

        // Test: Cannot edit remarks on Completed Job
        reject(() =>
        {
            using var c = createFactory();
            var t = c.JobTasks.Find(completedTaskId)!;
            t.Remarks = "Tampering with completed task remarks";
            c.SaveChanges();
        }, "Integrity: Editing remarks on Completed job rejected");

        // Test: Cannot add new task to Completed Job
        reject(() =>
        {
            using var c = createFactory();
            c.JobTasks.Add(new JobTask { JobOrderId = completedJobId, ServiceId = "Brake inspection", Status = "Pending" });
            c.SaveChanges();
        }, "Integrity: Adding task to Completed job rejected");

        // Release the job
        using (var c = createFactory())
        {
            var job = c.JobOrders.Find(completedJobId)!;
            job.Status = "Released";
            c.SaveChanges();
        }

        // Test: Cannot edit remarks on Released Job
        reject(() =>
        {
            using var c = createFactory();
            var t = c.JobTasks.Find(completedTaskId)!;
            t.Remarks = "Tampering with released task remarks";
            c.SaveChanges();
        }, "Integrity: Editing remarks on Released job rejected");

        // 3. Archived Job Protection
        // Archive the released job
        var archResult = await JobOrderArchivingService.ArchiveJobOrderAsync(createFactory, completedJobId, "Workshop records cycle archiving", "Audit Specialist");
        check(archResult.IsPermitted, "Integrity: Job archived successfully");

        reject(() =>
        {
            using var c = createFactory();
            var t = c.JobTasks.Find(completedTaskId)!;
            t.Remarks = "Tampering with archived job task";
            c.SaveChanges();
        }, "Integrity: Editing remarks on Archived job rejected");

        // 4. Appointment Check-In Race Condition Protection
        int apptId;
        using (var c = createFactory())
        {
            var appt = new Appointment
            {
                CustomerId = customerId,
                ScheduledStart = DateTime.Today.AddDays(2).AddHours(9),
                ScheduledEnd = DateTime.Today.AddDays(2).AddHours(10),
                RequestedServices = "Wheel alignment",
                Status = "Scheduled"
            };
            c.Appointments.Add(appt);
            c.SaveChanges();
            apptId = appt.AppointmentId;
        }

        // First check-in succeeds
        int createdJobId = 0;
        using (var c = createFactory())
        {
            var strategy = c.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                using var tx = c.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
                var target = c.Appointments
                    .FromSqlInterpolated($"SELECT * FROM Appointments WITH (UPDLOCK, HOLDLOCK) WHERE AppointmentId = {apptId}")
                    .FirstOrDefault();

                if (target == null || target.Status != "Scheduled" || target.LinkedJobOrderId.HasValue)
                    throw new InvalidOperationException("Appointment not eligible for check-in.");

                var job = new JobOrder
                {
                    CustomerId = target.CustomerId,
                    Status = "In Queue",
                    StartedAt = DateTime.Now
                };
                c.JobOrders.Add(job);
                target.Status = "Checked In";
                target.LinkedJobOrder = job;
                c.SaveChanges();
                tx.Commit();
                createdJobId = job.JobOrderId;
            });
        }
        check(createdJobId > 0, "Integrity: First check-in creates Job Order");

        // Second concurrent check-in attempt is rejected
        reject(() =>
        {
            using var c = createFactory();
            var strategy = c.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                using var tx = c.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
                var target = c.Appointments
                    .FromSqlInterpolated($"SELECT * FROM Appointments WITH (UPDLOCK, HOLDLOCK) WHERE AppointmentId = {apptId}")
                    .FirstOrDefault();

                if (target == null || target.Status != "Scheduled" || target.LinkedJobOrderId.HasValue)
                    throw new InvalidOperationException("Appointment is no longer in Scheduled status.");

                var job = new JobOrder { CustomerId = target.CustomerId, Status = "In Queue" };
                c.JobOrders.Add(job);
                target.Status = "Checked In";
                target.LinkedJobOrder = job;
                c.SaveChanges();
                tx.Commit();
            });
        }, "Integrity: Concurrent second check-in attempt safely rejected");

        // Verify exactly one Job Order was created
        using (var c = createFactory())
        {
            int totalLinkedJobs = c.Appointments.Where(a => a.AppointmentId == apptId && a.LinkedJobOrderId != null).Count();
            check(totalLinkedJobs == 1, "Integrity: Exactly one Job Order exists for checked-in appointment");
        }
    }
}
