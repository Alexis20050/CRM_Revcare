using crm.domain.entities;
using CRM.infrastructure.data;
using CRM_Revcare_;
using Microsoft.EntityFrameworkCore;
using System.Data;

internal static class LegacySeed
{
    private const string Batch = "legacy-fictional-20261123-v1";
    private const string EmailSuffix = "@revcare-seed.example";

    public static async Task RunAsync(bool apply)
    {
        var localOptions = new DbContextOptionsBuilder<MasterCrmDbContext>().UseSqlServer(
            @"Server=(localdb)\MSSQLLocalDB;Database=DB_MasterCrm;Trusted_Connection=True;TrustServerCertificate=True",
            sql => sql.EnableRetryOnFailure()).Options;
        Func<MasterCrmDbContext>[] factories = [() => new(localOptions), CrmDatabase.CreateContext];
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var factory in factories)
        {
            using var check = factory();
            var connection = check.Database.GetDbConnection();
            string target = connection.DataSource + "/" + connection.Database;
            if (!visited.Add(target)) continue;
            if (connection.Database is not ("DB_MasterCrm" or "db80735"))
                throw new InvalidOperationException("Refusing to seed a non-legacy database: " + target);
            Console.WriteLine("Target: " + target);
            if ((await check.Database.GetPendingMigrationsAsync()).Any() || check.Database.HasPendingModelChanges())
                throw new InvalidOperationException("Review pending schema changes before seeding.");
            Console.WriteLine("Before: " + Counts(check));
            if (!apply) continue;
            await check.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                using var db = factory();
                db.AuditActor = Batch;
                db.Database.SetCommandTimeout(120);
                using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
                // Serialize repeat invocations and make retries safe even after an ambiguous commit.
                db.Database.ExecuteSqlRaw("DECLARE @r int; EXEC @r = sp_getapplock @Resource = 'RevCareLegacySeed20261123', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000; IF @r < 0 THROW 51000, 'Another seed is running.', 1;");
                if (db.AuditEntries.Any(a => a.Actor == Batch && a.Action == "SeedBatchCompleted"))
                {
                    Verify(db);
                    Console.WriteLine("Batch already present; no duplicate records added.");
                    tx.Commit();
                    return;
                }
                if (db.Customers.Any(c => c.Email.EndsWith(EmailSuffix)))
                    throw new InvalidOperationException("Seed marker collision. Review existing records before proceeding.");
                Seed(db);
                Verify(db);
                db.AuditEntries.Add(new AuditEntry { Actor = Batch, EntityType = "SeedBatch", Action = "SeedBatchCompleted",
                    Changes = "200 fictional records per legacy module; existing records preserved.", OccurredAtUtc = DateTime.UtcNow });
                db.SaveChanges();
                tx.Commit();
                Console.WriteLine("Committed 200 records per module.");
                await Task.CompletedTask;
            });
            using var verify = factory();
            Verify(verify);
            Console.WriteLine("After: " + Counts(verify));
        }
    }

    private static string Counts(MasterCrmDbContext db) =>
        $"Customers={db.Customers.Count()}, Inquiries={db.Inquiries.Count()}, JobOrders={db.JobOrders.Count()}, JobTasks={db.JobTasks.Count()}, Payments={db.Payments.Count()}, Complaints={db.Complaints.Count()}, Feedback={db.Feedbacks.Count()}";

    private static void Seed(MasterCrmDbContext db)
    {
        string[] first = ["Adrian", "Bianca", "Carlo", "Diana", "Enzo", "Faith", "Gabriel", "Hannah", "Ivan", "Julia", "Kevin", "Lara", "Marco", "Nina", "Oliver", "Patricia", "Rafael", "Sofia", "Tristan", "Vanessa"];
        string[] last = ["Mendoza", "Reyes", "Villanueva", "Santos", "Bautista", "Navarro", "Ramos", "Castillo", "Mercado", "Fernandez"];
        string[] vehicles = ["Toyota Vios 2021", "Honda City 2022", "Mitsubishi Mirage 2020", "Toyota Wigo 2023", "Suzuki Ertiga 2022", "Honda Brio 2021", "Toyota Innova 2020", "Nissan Almera 2023", "Mitsubishi Xpander 2022", "Hyundai Accent 2019"];
        string[] services = ["Oil and filter change", "Brake inspection", "Wheel alignment", "Exterior detailing", "Air-conditioning diagnosis"];
        decimal[] prices = [3500m, 2200m, 1800m, 4500m, 2800m];
        string[] concerns = ["Scheduled maintenance and engine oil replacement", "Brake noise during low-speed stops", "Vehicle pulls slightly to the left", "Paint surface contamination and water spots", "Cabin cooling is weak during afternoon traffic"];
        var now = DateTime.UtcNow;
        var staff = Enumerable.Range(0, 5).Select(i => new WorkshopStaff
        {
            StaffId = Batch + "-mechanic-" + i, DisplayName = first[i + 10] + " " + last[i + 3],
            Role = "Automotive Mechanic", IsActive = true, CreatedAtUtc = now.AddDays(-100)
        }).ToArray();
        db.Staff.AddRange(staff);
        var customers = Enumerable.Range(0, 200).Select(i => new Customer
        {
            FullName = first[i % 20] + " " + last[i / 20],
            // Deliberately non-dialable contacts and reserved email domain for fictional people.
            ContactNumber = $"09XX-{i / 100:D3}-{i % 100:D4}",
            Email = $"{first[i % 20].ToLowerInvariant()}.{last[i / 20].ToLowerInvariant()}{EmailSuffix}",
            CreatedAt = now.AddDays(-100 - i % 30), IsActive = true
        }).ToArray();
        db.Customers.AddRange(customers);
        db.SaveChanges();
        Console.WriteLine("Customers and supporting workshop staff saved (transaction pending).");
        var jobs = Enumerable.Range(0, 200).Select(i => new JobOrder
        {
            CustomerId = customers[i].CustomerId, VehicleDetails = vehicles[i % 10],
            TotalEstimatedCost = prices[i % 5], InquiryNotes = concerns[i % 5],
            AssignedMechanicUserId = staff[i % 5].StaffId, AssignedMechanic = staff[i % 5].DisplayName,
            Status = "In Queue", StartedAt = now.AddDays(-1 - i % 45), CreatedAt = now.AddDays(-2 - i % 45)
        }).ToArray();
        db.JobOrders.AddRange(jobs);
        db.SaveChanges();
        var tasks = jobs.Select((j, i) => new JobTask { JobOrderId = j.JobOrderId, ServiceId = services[i % 5],
            Remarks = "Customer requested: " + concerns[i % 5] + ". Confirm findings before additional work.",
            Status = "Pending", CreatedAt = j.CreatedAt }).ToArray();
        db.JobTasks.AddRange(tasks);
        db.Inquiries.AddRange(jobs.Select((j, i) => new Inquiry { CustomerId = customers[i].CustomerId,
            CustomerName = customers[i].FullName, ContactNumber = customers[i].ContactNumber,
            VehicleDetails = j.VehicleDetails, InquiryPlatform = new[] { "Walk-in", "Messenger", "Call" }[i % 3],
            MessageDetails = concerns[i % 5] + ". Requested an estimate and workshop availability.",
            Status = "Converted", ConvertedJobOrderId = j.JobOrderId, CreatedAt = j.CreatedAt.AddDays(-1) }));
        db.SaveChanges();
        db.Payments.AddRange(jobs.Select((j, i) => new Payment { JobOrderId = j.JobOrderId,
            AmountPaid = j.TotalEstimatedCost!.Value * (i % 4 < 2 ? 0.5m : 1m),
            PaymentType = i % 4 < 2 ? "Downpayment" : "Retail Full",
            PaymentMethod = new[] { "Cash", "Card", "GCash", "Transfer" }[i % 4], CreatedAt = j.CreatedAt.AddHours(1) }));
        db.SaveChanges();
        Console.WriteLine("Jobs, tasks, inquiries and payments saved (transaction pending).");
        for (int i = 0; i < 200; i++) if (i % 4 != 0) jobs[i].Status = "In Progress";
        db.SaveChanges();
        for (int i = 0; i < 200; i++)
        {
            if (i % 4 == 0) continue;
            tasks[i].Status = i % 4 == 1 ? "Ongoing" : "Done";
            tasks[i].Remarks = i % 4 == 1 ? "Inspection underway; technician documenting findings and checking service specifications."
                : services[i % 5] + " completed. Operation checked and final inspection passed.";
        }
        db.SaveChanges();
        db.FinalInspectionConfirmed = true;
        for (int i = 0; i < 200; i++) if (i % 4 >= 2) jobs[i].Status = "Completed";
        db.SaveChanges();
        for (int i = 0; i < 200; i++) if (i % 4 == 3) jobs[i].Status = "Released";
        db.SaveChanges();
        var finished = jobs.Where((j, i) => i % 4 >= 2).ToArray();
        // Two post-service responses per completed visit, with different follow-up context.
        var feedback = Enumerable.Range(0, 200).Select(i => new Feedback { JobOrderId = finished[i % 100].JobOrderId,
            RatingScore = i % 10 == 0 ? 2 : 3 + i % 3,
            PositiveRemarks = i < 100 ? "Service advisor explained the work performed and maintenance recommendations."
                : "Follow-up: vehicle operation is satisfactory after the workshop visit.",
            AreasForImprovement = i % 10 == 0 ? "Waiting time exceeded the promised collection time; please follow up."
                : "Please provide an earlier progress update before vehicle collection.", CreatedAt = now }).ToArray();
        db.Feedbacks.AddRange(feedback);
        db.SaveChanges(); // 20 low ratings generate 20 urgent complaints through normal rules.
        db.Complaints.AddRange(Enumerable.Range(0, 180).Select(i => new Complaint {
            JobOrderId = jobs[i].JobOrderId,
            ComplaintType = new[] { "Communication", "Waiting Time", "Pricing" }[i % 3],
            Description = new[] { "Customer requested more frequent repair progress updates.", "Customer reported a longer reception wait than expected.", "Customer requested clarification of the quoted service charges." }[i % 3],
            ResolutionStatus = new[] { "Open", "Investigating", "Resolved" }[i % 3],
            ActionTaken = i % 3 == 2 ? "Service advisor explained the itemized quotation; customer acknowledged the explanation." : "",
            CreatedAt = now }));
        db.SaveChanges();
        Console.WriteLine("Workflow, feedback and complaints saved; checking relationships.");
    }

    private static void Verify(MasterCrmDbContext db)
    {
        var customers = db.Customers.Where(c => c.Email.EndsWith(EmailSuffix)).Select(c => c.CustomerId);
        var jobs = db.JobOrders.Where(j => customers.Contains(j.CustomerId!.Value));
        var ids = jobs.Select(j => j.JobOrderId);
        var counts = new[] { customers.Count(), db.Inquiries.Count(i => customers.Contains(i.CustomerId!.Value)), jobs.Count(),
            db.JobTasks.Count(t => ids.Contains(t.JobOrderId)), db.Payments.Count(p => ids.Contains(p.JobOrderId)),
            db.Complaints.Count(c => ids.Contains(c.JobOrderId!.Value)), db.Feedbacks.Count(f => ids.Contains(f.JobOrderId)) };
        if (counts.Any(c => c != 200)) throw new InvalidOperationException("Batch counts do not match: " + string.Join(",", counts));
        if (jobs.Any(j => j.Status == "Released" && db.Payments.Where(p => p.JobOrderId == j.JobOrderId && !p.IsVoided).Sum(p => p.AmountPaid) < j.TotalEstimatedCost)
            || jobs.Any(j => (j.Status == "Completed" || j.Status == "Released") && db.JobTasks.Any(t => t.JobOrderId == j.JobOrderId && t.Status != "Done")))
            throw new InvalidOperationException("Seeded job financial/task integrity check failed.");
        Console.WriteLine("Verified: 200 each; released balances settled; completed tasks done.");
    }
}
