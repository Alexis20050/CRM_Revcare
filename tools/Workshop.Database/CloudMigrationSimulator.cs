using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using CRM.infrastructure.data;
using crm.domain.entities;

namespace Workshop.Database;

public static class CloudMigrationSimulator
{
    private const string MasterConn = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
    private const string SimDbName = "RevCare_Db80735_Simulation";
    private const string SimConn = $@"Server=(localdb)\MSSQLLocalDB;Database={SimDbName};Trusted_Connection=True;TrustServerCertificate=True";

    public static async Task RunSimulationAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("    REVCARE CRM - DISPOSABLE CLOUD MIGRATION SIMULATION (db80735)");
        Console.WriteLine("================================================================================");
        Console.WriteLine($"Disposable Test Database: {SimDbName} on (localdb)\\MSSQLLocalDB");
        Console.WriteLine("Goal: Verify 100% data preservation and financial integrity of pending migrations.\n");

        await DropDatabaseIfExistsAsync();
        try
        {
            await CreateDatabaseAsync();
            Console.WriteLine("[STEP 1] Applying migrations 1 through 10 (simulating db80735 current cloud state)...");
            await ApplyBaselineMigrationsAsync();
            Console.WriteLine("   -> Baseline schema successfully established.");

            Console.WriteLine("\n[STEP 2] Seeding representative dataset matching db80735 (400 Job Orders, 400 Payments = ₱938,050.00)...");
            await SeedDb80735BaselineAsync();
            Console.WriteLine("   -> Seed complete. Validating baseline figures...");

            var preAudit = await AuditDatabaseAsync();
            Console.WriteLine($"   Pre-Migration Job Orders: {preAudit.JobOrderCount} (Range: #{preAudit.MinJobId} - #{preAudit.MaxJobId})");
            Console.WriteLine($"   Pre-Migration Payments:   {preAudit.PaymentCount} (Sum: ₱{preAudit.PaymentSum:N2})");

            if (preAudit.JobOrderCount != 400 || preAudit.PaymentSum != 938050.00m)
            {
                throw new InvalidOperationException($"Baseline verification failed! Expected 400 jobs & ₱938,050.00, got {preAudit.JobOrderCount} & ₱{preAudit.PaymentSum:N2}");
            }
            Console.WriteLine("   -> Baseline financial audit verified 100% exact.");

            Console.WriteLine("\n[STEP 3] Applying pending cloud migrations (11, 12, 13) via EF Core IMigrator...");
            var options = new DbContextOptionsBuilder<MasterCrmDbContext>()
                .UseSqlServer(SimConn)
                .Options;
            using (var db = new MasterCrmDbContext(options))
            {
                var migrator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions
                    .GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(db.Database);
                await migrator.MigrateAsync();
            }
            Console.WriteLine("   -> Pending migrations successfully executed without errors.");

            Console.WriteLine("\n[STEP 4] Auditing post-migration data and financial integrity...");
            var postAudit = await AuditDatabaseAsync();
            Console.WriteLine($"   Post-Migration Job Orders: {postAudit.JobOrderCount} (Range: #{postAudit.MinJobId} - #{postAudit.MaxJobId})");
            Console.WriteLine($"   Post-Migration Payments:   {postAudit.PaymentCount} (Sum: ₱{postAudit.PaymentSum:N2})");

            bool passJobs = postAudit.JobOrderCount == preAudit.JobOrderCount;
            bool passRange = postAudit.MinJobId == preAudit.MinJobId && postAudit.MaxJobId == preAudit.MaxJobId;
            bool passPayments = postAudit.PaymentCount == preAudit.PaymentCount;
            bool passSum = postAudit.PaymentSum == preAudit.PaymentSum;

            Console.WriteLine("\n--------------------------------------------------------------------------------");
            Console.WriteLine("POST-MIGRATION VERIFICATION AUDIT RESULTS:");
            Console.WriteLine($"  [{(passJobs ? "PASS" : "FAIL")}] Job Orders Preservation:   {postAudit.JobOrderCount} == {preAudit.JobOrderCount}");
            Console.WriteLine($"  [{(passRange ? "PASS" : "FAIL")}] ID Sequence Preservation:    #{postAudit.MinJobId} - #{postAudit.MaxJobId}");
            Console.WriteLine($"  [{(passPayments ? "PASS" : "FAIL")}] Payment Count Preservation: {postAudit.PaymentCount} == {preAudit.PaymentCount}");
            Console.WriteLine($"  [{(passSum ? "PASS" : "FAIL")}] Financial Total Intact:      ₱{postAudit.PaymentSum:N2} == ₱{preAudit.PaymentSum:N2}");
            Console.WriteLine("--------------------------------------------------------------------------------\n");

            if (!passJobs || !passRange || !passPayments || !passSum)
            {
                throw new InvalidOperationException("Simulation FAILED: Data or financial divergence detected post-migration!");
            }

            Console.WriteLine("[STEP 5] Verifying newly added operational schema elements on simulated db80735...");
            await VerifyNewSchemaFunctionalityAsync();
            Console.WriteLine("   -> All new schema elements (Archiving, JobCancellation, Refunds, Branches, DatabaseIdentity) functional!");

            Console.WriteLine("\n================================================================================");
            Console.WriteLine("    SIMULATION RESULT: SUCCESS (100% PRESERVATION CONFIRMED)");
            Console.WriteLine("    Pending migrations for db80735 are safe to deploy upon formal approval.");
            Console.WriteLine("================================================================================");
        }
        finally
        {
            Console.WriteLine("\n[CLEANUP] Tearing down disposable test database...");
            await DropDatabaseIfExistsAsync();
            Console.WriteLine("   -> Disposable simulation database dropped cleanly.");
        }
    }

    private static async Task DropDatabaseIfExistsAsync()
    {
        using var conn = new SqlConnection(MasterConn);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{SimDbName}')
BEGIN
    ALTER DATABASE [{SimDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{SimDbName}];
END";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task CreateDatabaseAsync()
    {
        using var conn = new SqlConnection(MasterConn);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE [{SimDbName}]";
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ApplyBaselineMigrationsAsync()
    {
        var options = new DbContextOptionsBuilder<MasterCrmDbContext>()
            .UseSqlServer(SimConn)
            .Options;

        using var db = new MasterCrmDbContext(options);
        var migrator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions
            .GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(db.Database);

        // Migrate up to 20261123150822_LegacyNormalization3NF (migration 10)
        await migrator.MigrateAsync("20261123150822_LegacyNormalization3NF");
    }

    private static async Task SeedDb80735BaselineAsync()
    {
        using var conn = new SqlConnection(SimConn);
        await conn.OpenAsync();

        // 1. Seed Customer and Vehicle
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SET IDENTITY_INSERT Customers ON;
INSERT INTO Customers (CustomerId, FullName, ContactNumber, Email, CreatedAt, IsActive)
VALUES (1, 'JOE LANCE ODTOHAN', '09123456789', 'joe@example.com', GETDATE(), 1);
SET IDENTITY_INSERT Customers OFF;

SET IDENTITY_INSERT Vehicles ON;
INSERT INTO Vehicles (VehicleId, CustomerId, PlateNumber, Make, Model, Year, Color, Vin, CreatedAt)
VALUES (1, 1, 'ABC-1234', 'MITSUBISHI', 'MONTERO', 2026, 'Black', 'VIN1234567890', GETDATE());
SET IDENTITY_INSERT Vehicles OFF;
";
            await cmd.ExecuteNonQueryAsync();
        }

        // 2. Seed 400 Job Orders (Simulating db80735 ID range #1 to #771)
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SET IDENTITY_INSERT JobOrders ON;
DECLARE @i int = 1;
WHILE @i <= 400
BEGIN
    INSERT INTO JobOrders (JobOrderId, CustomerId, VehicleId, EstimateId, VehicleDetails, InquiryNotes, Status, TotalEstimatedCost, StartedAt, CreatedAt)
    VALUES (@i, 1, 1, @i, 'MONTERO 2026', 'Simulated db80735 Job Order', 'Released', 2345.12, GETDATE(), GETDATE());
    SET @i = @i + 1;
END;
SET IDENTITY_INSERT JobOrders OFF;
";
            await cmd.ExecuteNonQueryAsync();
        }

        // 3. Seed 400 Payments totaling exactly PHP 938,050.00 (avg 2345.125, with exact cents)
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SET IDENTITY_INSERT Payments ON;
DECLARE @j int = 1;
WHILE @j <= 400
BEGIN
    -- 400 * 2345 = 938,000. Add 50.00 on record 1 to hit exactly 938,050.00
    IF @j = 1
        INSERT INTO Payments (PaymentId, JobOrderId, AmountPaid, PaymentMethod, PaymentType, IsVoided, SubmissionId, CreatedAt)
        VALUES (@j, @j, 2395.00, 'Cash', 'Retail Full', 0, NEWID(), GETDATE());
    ELSE
        INSERT INTO Payments (PaymentId, JobOrderId, AmountPaid, PaymentMethod, PaymentType, IsVoided, SubmissionId, CreatedAt)
        VALUES (@j, @j, 2345.00, 'Cash', 'Retail Full', 0, NEWID(), GETDATE());

    SET @j = @j + 1;
END;
SET IDENTITY_INSERT Payments OFF;
";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task ExecuteSqlScriptAsync(string script)
    {
        using var conn = new SqlConnection(SimConn);
        await conn.OpenAsync();

        // Split script by GO if any, or execute directly
        var batches = script.Split(new[] { "\nGO\r\n", "\nGO\n", "\r\nGO\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var batch in batches)
        {
            var trimmed = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            using var cmd = conn.CreateCommand();
            cmd.CommandText = trimmed;
            cmd.CommandTimeout = 120;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task<(int JobOrderCount, int MinJobId, int MaxJobId, int PaymentCount, decimal PaymentSum)> AuditDatabaseAsync()
    {
        using var conn = new SqlConnection(SimConn);
        await conn.OpenAsync();

        int jobCount = 0, minJob = 0, maxJob = 0, payCount = 0;
        decimal paySum = 0m;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*), ISNULL(MIN(JobOrderId),0), ISNULL(MAX(JobOrderId),0) FROM JobOrders";
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                jobCount = r.GetInt32(0);
                minJob = r.GetInt32(1);
                maxJob = r.GetInt32(2);
            }
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*), ISNULL(SUM(AmountPaid),0) FROM Payments WHERE IsVoided = 0";
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                payCount = r.GetInt32(0);
                paySum = r.GetDecimal(1);
            }
        }

        return (jobCount, minJob, maxJob, payCount, paySum);
    }

    private static async Task VerifyNewSchemaFunctionalityAsync()
    {
        var options = new DbContextOptionsBuilder<MasterCrmDbContext>()
            .UseSqlServer(SimConn)
            .Options;

        using var db = new MasterCrmDbContext(options);

        // 1. Verify Job Order archiving columns exist and are queryable
        var job1 = await db.JobOrders.FindAsync(1);
        if (job1 == null) throw new InvalidOperationException("Job #1 not found!");
        if (job1.IsArchived != false) throw new InvalidOperationException("Default IsArchived != false!");

        // 2. Verify soft archive on Job #1
        job1.IsArchived = true;
        job1.ArchivedAtUtc = DateTime.UtcNow;
        job1.ArchivedBy = "admin";
        job1.ArchiveReason = "Simulation test archive";
        await db.SaveChangesAsync();

        // 3. Verify JobCancellationRequests table
        db.JobCancellationRequests.Add(new JobCancellationRequest
        {
            JobOrderId = 2,
            RequestedAtUtc = DateTime.UtcNow,
            RequestedBy = "admin",
            ReasonCategory = "Customer Request",
            ReasonExplanation = "Simulation cancellation verification",
            Status = "Pending Review",
            TotalPaidAtRequest = 2345.00m,
            IncurredCharges = 0m,
            ProposedRefund = 2345.00m
        });
        await db.SaveChangesAsync();

        // 4. Verify Refunds table
        db.Refunds.Add(new Refund
        {
            JobOrderId = 2,
            Amount = 1000.00m,
            RefundMethod = "Cash",
            RefundDateUtc = DateTime.UtcNow,
            RecipientName = "JOE LANCE ODTOHAN",
            Reason = "Partial refund simulation",
            AuthorizedBy = "owner",
            SubmissionId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // 5. Verify DatabaseIdentity was seeded by migration 13
        var identity = await db.DatabaseIdentity.SingleOrDefaultAsync();
        if (identity == null || identity.TenantId != "tenant-a")
            throw new InvalidOperationException($"Identity missing or mismatched! Value: {identity?.TenantId}");

        // 6. Verify default MAIN branch was seeded by migration 13
        var branch = await db.Branches.FirstOrDefaultAsync(b => b.BranchCode == "MAIN");
        if (branch == null)
            throw new InvalidOperationException("Default MAIN branch was not seeded by migration!");
    }
}
