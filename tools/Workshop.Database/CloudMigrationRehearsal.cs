#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using CRM.infrastructure.data;
using crm.domain.entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Workshop.Database;

public static class CloudMigrationRehearsal
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("FAILED ASSERTION: " + message);
        Console.WriteLine("   PASS: " + message);
    }

    public static async Task RunAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("    REVCARE CRM - CLOUD MIGRATION REHEARSAL ON DISPOSABLE DATABASE");
        Console.WriteLine("    (AddTenantBranchFoundation -> AlignTenantCrmWithOperationalSchema)");
        Console.WriteLine("================================================================================");

        string dbName = "RevCare_CloudRehearsal_" + Guid.NewGuid().ToString("N");
        string masterConnStr = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
        string targetConnStr = $@"Server=(localdb)\MSSQLLocalDB;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True";

        // Create disposable catalog
        await using (var admin = new SqlConnection(masterConnStr))
        {
            await admin.OpenAsync();
            using var cmd = admin.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE [{dbName}];";
            await cmd.ExecuteNonQueryAsync();
        }
        Console.WriteLine($"\n[1] Created isolated rehearsal database: {dbName}");

        var tenantOptions = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(targetConnStr)
            .Options;

        try
        {
            // 2. Migrate up to AddTenantBranchFoundation (the exact cloud state of db81189)
            Console.WriteLine("\n[2] Applying migrations up to target: 20261124062717_AddTenantBranchFoundation...");
            using (var db = new TenantCrmDbContext(tenantOptions))
            {
                var migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();
                migrator.Migrate("20261124062717_AddTenantBranchFoundation");

                var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
                Check(applied.Contains("20261124062717_AddTenantBranchFoundation"), "Applied AddTenantBranchFoundation successfully");
                Check(!applied.Contains("20261124131459_AlignTenantCrmWithOperationalSchema"), "AlignTenantCrmWithOperationalSchema is correctly PENDING");
            }

            // 3. Seed pre-cutover state representing cloud db81189 records
            Console.WriteLine("\n[3] Seeding cloud baseline state (TenantDatabaseIdentity, Branches, Customers, Jobs, Payments)...");
            decimal expectedPaymentsTotal = 0m;
            decimal expectedJobsTotal = 0m;

            await using (var conn = new SqlConnection(targetConnStr))
            {
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();

                cmd.CommandText = "INSERT INTO [dbo].[TenantDatabaseIdentity] ([Id], [TenantId]) VALUES (1, 'tenant-a');";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = @"INSERT INTO [dbo].[Branches] ([BranchCode], [BranchName], [Address], [ContactNumber], [IsActive], [CreatedAtUtc])
                                    VALUES ('MAIN', 'RevCare Auto Hub - Main Branch', 'Metro Manila', '0917-123-4567', 1, GETUTCDATE());";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = @"INSERT INTO [dbo].[Customers] ([FullName], [ContactNumber], [Email], [IsActive], [CreatedAt])
                                    OUTPUT INSERTED.CustomerId
                                    VALUES ('Elena Rostova', '0918-777-6655', 'elena.rostova@example.ph', 1, GETUTCDATE());";
                int custId = (int)(await cmd.ExecuteScalarAsync())!;

                cmd.CommandText = $@"INSERT INTO [dbo].[JobOrders] ([CustomerId], [EstimateId], [Status], [TotalEstimatedCost], [VehicleDetails], [InquiryNotes], [StartedAt], [CreatedAt])
                                    OUTPUT INSERTED.JobOrderId
                                    VALUES ({custId}, 'EST-CLOUD-REHEARSAL-01', 'In Queue', 15000.00, 'Toyota Vios 2021', 'Cloud rehearsal intake', GETUTCDATE(), GETUTCDATE());";
                int jobId = (int)(await cmd.ExecuteScalarAsync())!;
                expectedJobsTotal += 15000.00m;

                cmd.CommandText = $@"INSERT INTO [dbo].[JobTasks] ([JobOrderId], [ServiceId], [Remarks], [Status], [CreatedAt])
                                    VALUES ({jobId}, 'Brake Service', 'Front brake pads', 'Pending', GETUTCDATE()),
                                           ({jobId}, 'Full Detailing', 'Exterior detail', 'Pending', GETUTCDATE());";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = $@"INSERT INTO [dbo].[Payments] ([JobOrderId], [AmountPaid], [PaymentMethod], [PaymentType], [CreatedAt], [SubmissionId], [IsVoided], [VoidReason], [AllowCustomerCredit])
                                    VALUES ({jobId}, 7500.00, 'Cash', 'Retail Full', GETUTCDATE(), NEWID(), 0, '', 0);";
                await cmd.ExecuteNonQueryAsync();
                expectedPaymentsTotal += 7500.00m;

                Check(true, "Seeded baseline records via ADO.NET: 1 TenantIdentity, 1 Branch, 1 Customer, 1 JobOrder, 2 Tasks, 1 Payment");
            }

            // 4. Capture pre-migration fingerprints & financial baseline
            Console.WriteLine("\n[4] Capturing pre-migration financial baseline and row counts...");
            int preCustomers, preJobs, preTasks, prePayments;
            decimal prePaidSum, preCostSum;

            await using (var conn = new SqlConnection(targetConnStr))
            {
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Customers; SELECT COUNT(*) FROM JobOrders; SELECT COUNT(*) FROM JobTasks; SELECT COUNT(*), COALESCE(SUM(AmountPaid),0) FROM Payments; SELECT COALESCE(SUM(TotalEstimatedCost),0) FROM JobOrders;";
                using var reader = await cmd.ExecuteReaderAsync();
                
                await reader.ReadAsync(); preCustomers = reader.GetInt32(0);
                await reader.NextResultAsync(); await reader.ReadAsync(); preJobs = reader.GetInt32(0);
                await reader.NextResultAsync(); await reader.ReadAsync(); preTasks = reader.GetInt32(0);
                await reader.NextResultAsync(); await reader.ReadAsync(); prePayments = reader.GetInt32(0); prePaidSum = reader.GetDecimal(1);
                await reader.NextResultAsync(); await reader.ReadAsync(); preCostSum = reader.GetDecimal(0);
            }

            Check(prePayments == 1 && prePaidSum == 7500.00m, $"Pre-migration payments verified: {prePayments} payment(s), total ₱{prePaidSum:N2}");
            Check(preJobs == 1 && preCostSum == 15000.00m, $"Pre-migration jobs verified: {preJobs} job(s), estimated ₱{preCostSum:N2}");

            // 5. Apply the pending migration: AlignTenantCrmWithOperationalSchema
            Console.WriteLine("\n[5] Executing pending migration: AlignTenantCrmWithOperationalSchema...");
            using (var db = new TenantCrmDbContext(tenantOptions))
            {
                var migrator = db.GetInfrastructure().GetRequiredService<IMigrator>();
                migrator.Migrate("20261124131459_AlignTenantCrmWithOperationalSchema");

                var applied = (await db.Database.GetAppliedMigrationsAsync()).ToList();
                Check(applied.Contains("20261124131459_AlignTenantCrmWithOperationalSchema"), "AlignTenantCrmWithOperationalSchema applied successfully");
            }

            // 6. Verify post-migration schema gaps were resolved
            Console.WriteLine("\n[6] Verifying schema alignment and newly created operational tables...");
            string[] expectedNewTables = ["Estimates", "EstimateLines", "Vehicles", "Appointments", "AppointmentHistories", "JobCancellationRequests", "Refunds"];
            await using (var conn = new SqlConnection(targetConnStr))
            {
                await conn.OpenAsync();
                foreach (var tbl in expectedNewTables)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT COUNT(*) FROM sys.tables WHERE name = '{tbl}';";
                    int count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                    Check(count == 1, $"Table '{tbl}' exists in aligned schema");
                }

                // Verify added columns
                string[] expectedCols = ["Payments.BranchId", "Payments.PayerType", "Payments.PaymentPurpose", "JobTasks.AgreedPrice", "JobOrders.ApprovedEstimateId", "JobOrders.IsArchived", "JobOrders.VehicleId"];
                foreach (var col in expectedCols)
                {
                    var parts = col.Split('.');
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.{parts[0]}') AND name = '{parts[1]}';";
                    int count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                    Check(count == 1, $"Column '{col}' exists in aligned schema");
                }
            }

            // 7. Verify zero data loss on pre-existing records & financials
            Console.WriteLine("\n[7] Verifying 100% data preservation and financial integrity...");
            await using (var conn = new SqlConnection(targetConnStr))
            {
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Customers; SELECT COUNT(*) FROM JobOrders; SELECT COUNT(*) FROM JobTasks; SELECT COUNT(*), COALESCE(SUM(AmountPaid),0) FROM Payments; SELECT COALESCE(SUM(TotalEstimatedCost),0) FROM JobOrders;";
                int postCustomers, postJobs, postTasks, postPayments;
                decimal postPaidSum, postCostSum;
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync(); postCustomers = reader.GetInt32(0);
                    await reader.NextResultAsync(); await reader.ReadAsync(); postJobs = reader.GetInt32(0);
                    await reader.NextResultAsync(); await reader.ReadAsync(); postTasks = reader.GetInt32(0);
                    await reader.NextResultAsync(); await reader.ReadAsync(); postPayments = reader.GetInt32(0); postPaidSum = reader.GetDecimal(1);
                    await reader.NextResultAsync(); await reader.ReadAsync(); postCostSum = reader.GetDecimal(0);

                    Check(postCustomers == preCustomers, $"Zero customer loss ({postCustomers}/{preCustomers} preserved)");
                    Check(postJobs == preJobs, $"Zero job order loss ({postJobs}/{preJobs} preserved)");
                    Check(postTasks == preTasks, $"Zero job task loss ({postTasks}/{preTasks} preserved)");
                    Check(postPayments == prePayments, $"Zero payment count loss ({postPayments}/{prePayments} preserved)");
                    Check(postPaidSum == prePaidSum, $"Exact financial total preserved: ₱{postPaidSum:N2} == ₱{prePaidSum:N2}");
                    Check(postCostSum == preCostSum, $"Exact estimated total preserved: ₱{postCostSum:N2} == ₱{preCostSum:N2}");
                }

                // Verify historical payments have PayerType and PaymentPurpose as "Unknown" (no invented classifications)
                cmd.CommandText = "SELECT COUNT(*) FROM Payments WHERE PayerType = 'Unknown' AND PaymentPurpose = 'Unknown';";
                int unknownPayments = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                Check(unknownPayments == postPayments, $"Historical payments correctly defaulted to 'Unknown' remitter/purpose ({unknownPayments}/{postPayments}) without invented defaults");

                // Verify historical task prices remain NULL (no price fabrication)
                cmd.CommandText = "SELECT COUNT(*) FROM JobTasks WHERE AgreedPrice IS NULL;";
                int nullAgreedPrices = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                Check(nullAgreedPrices == postTasks, $"Historical task prices preserved as NULL ({nullAgreedPrices}/{postTasks}) without price fabrication");

                // Verify historical job estimate approvals remain NULL (no quotation fabrication)
                cmd.CommandText = "SELECT COUNT(*) FROM JobOrders WHERE ApprovedEstimateId IS NULL;";
                int nullApprovedEstimates = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                Check(nullApprovedEstimates == postJobs, $"Historical approved estimate IDs preserved as NULL ({nullApprovedEstimates}/{postJobs}) without quotation fabrication");

                // Check constraints
                cmd.CommandText = "DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;";
                using (var chkReader = await cmd.ExecuteReaderAsync())
                {
                    Check(!await chkReader.ReadAsync(), "Zero foreign key or check constraint violations detected");
                }
            }

            // 8. Test live writes on aligned schema
            Console.WriteLine("\n[8] Testing live operational writes on aligned schema (Estimates, Vehicles, Appointments)...");
            using (var db = new TenantCrmDbContext(tenantOptions))
            {
                var cust = await db.Customers.FirstAsync();
                
                var veh = new Vehicle
                {
                    CustomerId = cust.CustomerId,
                    Make = "Toyota",
                    Model = "RAV4",
                    PlateNumber = "RAV-2026",
                    Year = 2024,
                    CreatedAt = DateTime.UtcNow
                };
                db.Vehicles.Add(veh);
                await db.SaveChangesAsync();
                Check(veh.VehicleId > 0, $"Vehicle persisted successfully (VehicleId: {veh.VehicleId})");

                var est = new Estimate
                {
                    CustomerId = cust.CustomerId,
                    VehicleId = veh.VehicleId,
                    EstimateNumber = "EST-REHEARSAL-002",
                    Status = "Draft",
                    TotalAmount = 8500.00m,
                    Notes = "Aligned schema estimate write test",
                    CreatedAt = DateTime.UtcNow
                };
                est.Lines.Add(new EstimateLine
                {
                    Description = "Brake Rotor Replacement",
                    Quantity = 2,
                    UnitPrice = 3500.00m,
                    LineTotal = 7000.00m
                });
                est.Lines.Add(new EstimateLine
                {
                    Description = "Brake Fluid",
                    Quantity = 1,
                    UnitPrice = 1500.00m,
                    LineTotal = 1500.00m
                });
                db.Estimates.Add(est);
                await db.SaveChangesAsync();
                Check(est.EstimateId > 0 && est.Lines.Count == 2, $"Estimate with 2 lines persisted successfully (EstimateId: {est.EstimateId})");

                var appt = new Appointment
                {
                    CustomerId = cust.CustomerId,
                    VehicleId = veh.VehicleId,
                    ScheduledStart = DateTime.UtcNow.AddDays(1),
                    ScheduledEnd = DateTime.UtcNow.AddDays(1).AddHours(2),
                    RequestedServices = "Brake Rotor Replacement",
                    Status = "Scheduled",
                    Notes = "Aligned appointment test"
                };
                db.Appointments.Add(appt);
                await db.SaveChangesAsync();
                Check(appt.AppointmentId > 0, $"Appointment persisted successfully (AppointmentId: {appt.AppointmentId})");
            }

            Console.WriteLine("\n================================================================================");
            Console.WriteLine("PASS: CLOUD MIGRATION REHEARSAL COMPLETED WITH ZERO DATA LOSS AND 100% INTEGRITY");
            Console.WriteLine("================================================================================");
        }
        finally
        {
            // Drop disposable rehearsal database
            try
            {
                await using var admin = new SqlConnection(masterConnStr);
                await admin.OpenAsync();
                using var cmd = admin.CreateCommand();
                cmd.CommandText = $@"
                    IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{dbName}')
                    BEGIN
                        ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{dbName}];
                    END";
                await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"\n[Teardown] Dropped disposable rehearsal database: {dbName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Teardown Warning] Failed to drop {dbName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Applies the reviewed AlignTenantCrmWithOperationalSchema migration exclusively to db81189
    /// after preflight identity, migration, financial baseline, and SHA-256 table fingerprint checks.
    /// </summary>
    public static async Task ApplyToCloudDb81189Async()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("    REVCARE CRM - CONTROLLED CLOUD MIGRATION EXECUTION (db81189)");
        Console.WriteLine("================================================================================");

        var rawConnection = Environment.GetEnvironmentVariable("REVCARE_CLOUD_TENANT_CONNECTION");
        if (string.IsNullOrWhiteSpace(rawConnection))
            throw new InvalidOperationException("REVCARE_CLOUD_TENANT_CONNECTION environment variable is required.");

        var builder = new SqlConnectionStringBuilder(rawConnection);
        if (!string.Equals(builder.InitialCatalog, "db81189", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(builder.DataSource, "db81189.public.databaseasp.net,1433", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Approved migration target is strictly db81189.public.databaseasp.net,1433 / db81189. Actual: {builder.DataSource} / {builder.InitialCatalog}");
        }

        builder.Encrypt = true;
        builder.TrustServerCertificate = false;
        builder.ConnectTimeout = 15;
        string cloudConnStr = builder.ConnectionString;

        Console.WriteLine($"Target Host: {builder.DataSource}");
        Console.WriteLine($"Target DB:   {builder.InitialCatalog}");
        Console.WriteLine($"Encryption:  Encrypt={builder.Encrypt}, TrustServerCertificate={builder.TrustServerCertificate}\n");

        var tenantOptions = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(cloudConnStr, sql => sql.EnableRetryOnFailure(3))
            .Options;

        // [1] Preflight checks on EF Migrations and TenantDatabaseIdentity
        Console.WriteLine("[1] Verifying preflight tenant identity and pending migrations...");
        using (var db = new TenantCrmDbContext(tenantOptions))
        {
            var identityCount = await db.DatabaseIdentity.CountAsync(x => x.Id == 1 && x.TenantId == "tenant-a");
            Check(identityCount == 1, "TenantDatabaseIdentity confirmed as 'tenant-a'");

            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            Check(pending.Count == 1 && pending[0] == "20261124131459_AlignTenantCrmWithOperationalSchema",
                $"Pending migrations strictly match expected: [{string.Join(", ", pending)}]");
        }

        // [2] Capture pre-migration table columns, SHA-256 fingerprints & financial baseline
        Console.WriteLine("\n[2] Capturing pre-migration table fingerprints and financial baseline...");
        var columns = new Dictionary<string, List<string>>();
        int preCustomers, preJobs, preTasks, prePayments;
        decimal preValidPaidSum;

        await using (var conn = new SqlConnection(cloudConnStr))
        {
            await conn.OpenAsync();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT s.name, t.name, c.name
                    FROM sys.tables t
                    JOIN sys.schemas s ON t.schema_id = s.schema_id
                    JOIN sys.columns c ON c.object_id = t.object_id
                    WHERE t.name <> '__EFMigrationsHistory' AND c.system_type_id <> 189
                    ORDER BY s.name, t.name, c.column_id;";
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    string q(string v) => "[" + v.Replace("]", "]]") + "]";
                    var table = q(r.GetString(0)) + "." + q(r.GetString(1));
                    if (!columns.ContainsKey(table)) columns[table] = [];
                    columns[table].Add(q(r.GetString(2)));
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT COUNT(*) FROM Customers;
                    SELECT COUNT(*) FROM JobOrders;
                    SELECT COUNT(*) FROM JobTasks;
                    SELECT COUNT(*), COALESCE(SUM(AmountPaid), 0) FROM Payments WHERE IsVoided = 0;";
                using var r = await cmd.ExecuteReaderAsync();
                await r.ReadAsync(); preCustomers = r.GetInt32(0);
                await r.NextResultAsync(); await r.ReadAsync(); preJobs = r.GetInt32(0);
                await r.NextResultAsync(); await r.ReadAsync(); preTasks = r.GetInt32(0);
                await r.NextResultAsync(); await r.ReadAsync(); prePayments = r.GetInt32(0); preValidPaidSum = r.GetDecimal(1);
            }
        }

        static async Task<Dictionary<string, string>> CaptureFingerprintsAsync(string connStr, Dictionary<string, List<string>> cols)
        {
            var result = new Dictionary<string, string>();
            await using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            foreach (var table in cols)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT " + string.Join(",", table.Value) + " FROM " + table.Key;
                cmd.CommandTimeout = 120;
                var hashes = new List<string>();
                await using var rows = await cmd.ExecuteReaderAsync();
                while (await rows.ReadAsync())
                {
                    var values = Enumerable.Range(0, rows.FieldCount).Select(i => rows.IsDBNull(i) ? null : rows.GetValue(i)).ToArray();
                    hashes.Add(Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(values))));
                }
                hashes.Sort(StringComparer.Ordinal);
                result[table.Key] = hashes.Count.ToString(CultureInfo.InvariantCulture) + ":" + Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", hashes))));
            }
            return result;
        }

        var beforeFingerprints = await CaptureFingerprintsAsync(cloudConnStr, columns);
        Check(beforeFingerprints.Count > 0, $"Captured pre-migration SHA-256 fingerprints for {beforeFingerprints.Count} tables");
        Console.WriteLine($"   Baseline: Customers={preCustomers}, JobOrders={preJobs}, JobTasks={preTasks}, ValidPayments={prePayments} (PHP {preValidPaidSum:N2})");

        // [3] Execute EF Core migration against db81189
        Console.WriteLine("\n[3] Applying pending migration: 20261124131459_AlignTenantCrmWithOperationalSchema...");
        using (var db = new TenantCrmDbContext(tenantOptions))
        {
            await db.Database.MigrateAsync();
            var remainingPending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            Check(remainingPending.Count == 0, "Zero pending migrations remain on db81189");
        }

        // [4] Post-migration verification: Fingerprints, Counts, Unknown Historical Defaults, New Tables & Constraints
        Console.WriteLine("\n[4] Verifying 100% historical preservation, schema alignment, and constraints...");
        var afterFingerprints = await CaptureFingerprintsAsync(cloudConnStr, columns);
        foreach (var kv in beforeFingerprints)
        {
            Check(afterFingerprints[kv.Key] == kv.Value, $"Table {kv.Key} preserved byte-for-byte ({kv.Value.Split(':')[0]} rows)");
        }

        await using (var conn = new SqlConnection(cloudConnStr))
        {
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();

            string[] expectedTables = ["Estimates", "EstimateLines", "Vehicles", "Appointments", "AppointmentHistories", "JobCancellationRequests", "Refunds"];
            foreach (var tbl in expectedTables)
            {
                cmd.CommandText = $"SELECT COUNT(*) FROM sys.tables WHERE name = '{tbl}';";
                int exists = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                Check(exists == 1, $"Table '{tbl}' exists on db81189");
            }

            cmd.CommandText = "SELECT COUNT(*), COALESCE(SUM(AmountPaid), 0) FROM Payments WHERE IsVoided = 0;";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                await r.ReadAsync();
                int postPayments = r.GetInt32(0);
                decimal postSum = r.GetDecimal(1);
                Check(postPayments == prePayments && postSum == preValidPaidSum,
                    $"Exact payment baseline preserved: {postPayments} payments totaling PHP {postSum:N2}");
            }

            cmd.CommandText = "SELECT COUNT(*) FROM Payments WHERE PayerType = 'Unknown' AND PaymentPurpose = 'Unknown';";
            int unknownPayments = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            Check(unknownPayments == prePayments, $"Historical payments preserved with 'Unknown' PayerType & PaymentPurpose ({unknownPayments}/{prePayments})");

            cmd.CommandText = "SELECT COUNT(*) FROM JobTasks WHERE AgreedPrice IS NULL;";
            int nullPrices = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            Check(nullPrices == preTasks, $"Historical task AgreedPrice preserved as NULL ({nullPrices}/{preTasks})");

            cmd.CommandText = "SELECT COUNT(*) FROM JobOrders WHERE ApprovedEstimateId IS NULL;";
            int nullApprovals = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            Check(nullApprovals == preJobs, $"Historical job ApprovedEstimateId preserved as NULL ({nullApprovals}/{preJobs})");

            cmd.CommandText = "DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;";
            cmd.CommandTimeout = 300;
            using var chk = await cmd.ExecuteReaderAsync();
            Check(!await chk.ReadAsync(), "Zero foreign key or check constraint violations on db81189");
        }

        Console.WriteLine("\n================================================================================");
        Console.WriteLine("PASS: CLOUD DB81189 ALIGNED SUCCESSFULLY WITH ZERO DATA LOSS AND 100% INTEGRITY");
        Console.WriteLine("================================================================================");
    }
}
