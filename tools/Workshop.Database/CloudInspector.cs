#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CRM_Revcare_;
using Microsoft.Data.SqlClient;

namespace Workshop.Database;

public static class CloudInspector
{
    public static async Task InspectLocalOnlyAsync()
    {
        await InspectLocalDatabaseAsync(CrmDatabase.ResolveTarget(new[] { "--local" }).ConnectionString);
        using var connection = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=RevCare_PlatformMaster;Integrated Security=True;TrustServerCertificate=True");
        await connection.OpenAsync();
        Console.WriteLine("LOCAL platform master (not cloud):");
        await InspectMasterDatabaseAsync(connection);
    }

    public static async Task InspectAsync()
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("    REVCARE CRM - READ-ONLY CLOUD DATABASE INSPECTION & COMPARISON");
        Console.WriteLine("================================================================================");
        Console.WriteLine("SAFETY MODE: READ-ONLY. Zero writes will be performed.\n");

        var cloudConnection = Environment.GetEnvironmentVariable("REVCARE_CLOUD_TENANT_CONNECTION")
            ?? CrmDatabase.ResolveTarget(new[] { "--cloud" }).ConnectionString;
        var builderA = new SqlConnectionStringBuilder(cloudConnection);
        string targetCatalog = builderA.InitialCatalog;
        if (targetCatalog != "db80735" || builderA.DataSource != "db80735.public.databaseasp.net,1433")
            throw new InvalidOperationException($"Tenant inspection requires the approved public destination (db80735). Actual: {builderA.DataSource} / {builderA.InitialCatalog}");
        builderA.Encrypt = true;
        builderA.TrustServerCertificate = false;
        builderA.ConnectTimeout = 10;

        Console.WriteLine($"[1] INSPECTING TENANT A CLOUD DESTINATION ({targetCatalog}):");
        Console.WriteLine($"   Server Host:     {builderA.DataSource}");
        Console.WriteLine($"   Database:        {builderA.InitialCatalog}");
        
        Console.WriteLine($"   Encrypt:         {builderA.Encrypt}");
        Console.WriteLine($"   TrustServerCert: {builderA.TrustServerCertificate}\n");

        await InspectTenantADatabaseAsync(builderA.ConnectionString, targetCatalog);

        Console.WriteLine("\n[2] INSPECTING PLATFORM MASTER CLOUD DESTINATION (db81188):");
        await ProbePlatformMasterAsync(builderA);

        Console.WriteLine("\n[3] INSPECTING LOCAL DB_MASTERCRM (TENANT A LOCAL):");
        var localTarget = CrmDatabase.ResolveTarget(new[] { "--local" });
        await InspectLocalDatabaseAsync(localTarget.ConnectionString);
    }

    private static async Task InspectTenantADatabaseAsync(string connectionString, string targetDb = "db80735")
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            Console.WriteLine($"   -> CONNECTED SUCCESSFULLY TO {targetDb}!");

            using var cmd = conn.CreateCommand();

            // SQL Server Version
            cmd.CommandText = "SELECT @@VERSION, DB_NAME(), USER_NAME(), SUSER_SNAME();";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    Console.WriteLine($"   SQL Server:    {r.GetString(0).Split('\n')[0].Trim()}");
                    Console.WriteLine($"   Database Name: {r.GetString(1)}");
                    Console.WriteLine($"   DB User:       {r.GetString(2)}");
                    Console.WriteLine($"   Login Name:    {r.GetString(3)}");
                }
            }

            // Applied Migrations
            Console.WriteLine($"\n   --- Applied Migrations (__EFMigrationsHistory) on {targetDb} ---");
            cmd.CommandText = @"
                IF OBJECT_ID('dbo.__EFMigrationsHistory') IS NOT NULL
                    SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
                ELSE
                    SELECT 'Table dbo.__EFMigrationsHistory not found', '';";
            var migrations = new List<string>();
            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    var id = r.GetString(0);
                    migrations.Add(id);
                    Console.WriteLine($"      [MIGRATION] {id}");
                }
            }

            // Tables and row counts
            Console.WriteLine($"\n   --- Tables & Record Counts on {targetDb} ---");
            cmd.CommandText = @"
                SELECT t.name AS TableName, SUM(p.rows) AS [RowCount]
                FROM sys.tables t
                INNER JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
                GROUP BY t.name
                ORDER BY t.name;";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    Console.WriteLine($"      Table: {r.GetString(0),-30} Rows: {r.GetInt64(1)}");
                }
            }

            // Check User Permissions
            Console.WriteLine($"\n   --- Database Permissions on {targetDb} ---");
            cmd.CommandText = @"
                SELECT 
                    HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'BACKUP DATABASE') AS CanBackupDb,
                    HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE TABLE') AS CanCreateTable,
                    HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'ALTER') AS CanAlterDb,
                    IS_MEMBER('db_owner') AS IsDbOwner,
                    IS_SRVROLEMEMBER('sysadmin') AS IsSysAdmin;";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    Console.WriteLine($"      BACKUP DATABASE permission: {r.GetInt32(0) == 1}");
                    Console.WriteLine($"      CREATE TABLE permission:    {r.GetInt32(1) == 1}");
                    Console.WriteLine($"      ALTER permission:           {r.GetInt32(2) == 1}");
                    Console.WriteLine($"      db_owner role:              {r.GetInt32(3) == 1}");
                    Console.WriteLine($"      sysadmin role:              {r.GetInt32(4) == 1}");
                }
            }

            // Columns in JobOrders
            Console.WriteLine($"\n   --- JobOrders Schema Columns on {targetDb} ---");
            cmd.CommandText = @"
                SELECT c.name, t.name AS DataType, c.max_length, c.is_nullable
                FROM sys.columns c
                INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                WHERE c.object_id = OBJECT_ID('dbo.JobOrders')
                ORDER BY c.column_id;";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    Console.WriteLine($"      Col: {r.GetString(0),-25} Type: {r.GetString(1),-15} Nullable: {r.GetBoolean(3)}");
                }
            }

            // Check specific columns
            Console.WriteLine($"\n   --- Key Column Checks on {targetDb} ---");
            string[] checkCols = ["IsArchived", "ArchivedAtUtc", "ArchivedBy", "ArchiveReason", "BranchId", "VehicleId", "ApprovedEstimateId"];
            foreach (var col in checkCols)
            {
                cmd.CommandText = $"SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID('dbo.JobOrders') AND name = '{col}';";
                int exists = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                Console.WriteLine($"      JobOrders.{col,-20} Exists: {(exists > 0 ? "YES" : "NO")}");
            }

            // Check TenantDatabaseIdentity table
            cmd.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = 'TenantDatabaseIdentity';";
            int hasTenantIdentity = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            Console.WriteLine($"      TenantDatabaseIdentity Table: {(hasTenantIdentity > 0 ? "EXISTS" : "MISSING")}");

            // Check Branches table
            cmd.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = 'Branches';";
            int hasBranches = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            Console.WriteLine($"      Branches Table:               {(hasBranches > 0 ? "EXISTS" : "MISSING")}");

            // Financial summary in targetDb
            Console.WriteLine($"\n   --- Operational Financial Summary on {targetDb} ---");
            cmd.CommandText = @"
                SELECT 
                    COUNT(*) AS TotalPayments,
                    COALESCE(SUM(CASE WHEN IsVoided = 0 THEN AmountPaid ELSE 0 END), 0) AS ValidPaid,
                    COALESCE(SUM(CASE WHEN IsVoided = 1 THEN AmountPaid ELSE 0 END), 0) AS VoidedPaid,
                    COALESCE(MIN(PaymentId), 0) AS MinPayId,
                    COALESCE(MAX(PaymentId), 0) AS MaxPayId
                FROM dbo.Payments;";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    Console.WriteLine($"      Payments Count: {r.GetInt32(0)}");
                    Console.WriteLine($"      Valid Total:    PHP {r.GetDecimal(1):N2}");
                    Console.WriteLine($"      Voided Total:   PHP {r.GetDecimal(2):N2}");
                    Console.WriteLine($"      Payment ID Range: #{r.GetInt32(3)} - #{r.GetInt32(4)}");
                }
            }

            // Job Orders Summary in targetDb
            Console.WriteLine($"\n   --- Job Orders Summary on {targetDb} ---");
            cmd.CommandText = @"
                SELECT 
                    COUNT(*) AS TotalJobs,
                    COALESCE(MIN(JobOrderId), 0) AS MinJobId,
                    COALESCE(MAX(JobOrderId), 0) AS MaxJobId,
                    COALESCE(MIN(StartedAt), '1900-01-01') AS EarliestDate,
                    COALESCE(MAX(StartedAt), '1900-01-01') AS LatestDate
                FROM dbo.JobOrders;";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    Console.WriteLine($"      Job Orders Count: {r.GetInt32(0)}");
                    Console.WriteLine($"      Job ID Range:     #{r.GetInt32(1)} - #{r.GetInt32(2)}");
                    Console.WriteLine($"      Date Range:       {r.GetDateTime(3):yyyy-MM-dd} to {r.GetDateTime(4):yyyy-MM-dd}");
                }
            }
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 2;
            Console.WriteLine($"   [FAIL] {targetDb} inspection failed ({ex.GetType().Name}, SQL code {(ex as SqlException)?.Number}).");
        }
    }

    private static async Task ProbePlatformMasterAsync(SqlConnectionStringBuilder baseBuilder)
    {
        var connection = Environment.GetEnvironmentVariable("REVCARE_CLOUD_PLATFORM_CONNECTION")
            ?? Environment.GetEnvironmentVariable("REVCARE_CLOUD_PLATFORM_CONNECTION", EnvironmentVariableTarget.User)
            ?? Environment.GetEnvironmentVariable("REVCARE_CLOUD_PLATFORM_CONNECTION", EnvironmentVariableTarget.Machine);
        if (string.IsNullOrWhiteSpace(connection))
        {
            Console.WriteLine("   [BLOCKED] Dedicated REVCARE_CLOUD_PLATFORM_CONNECTION is missing. No credential guessing performed.");
            Environment.ExitCode = 2;
            return;
        }
        try
        {
            var target = new SqlConnectionStringBuilder(connection);
            if (!string.Equals(target.InitialCatalog, "db81188", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(target.DataSource, "db81188.public.databaseasp.net,1433", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Dedicated master destination does not match the approved host/catalog.");
            target.Encrypt = true;
            target.TrustServerCertificate = false;
            target.ConnectTimeout = 10;
            using var conn = new SqlConnection(target.ConnectionString);
            await conn.OpenAsync();
            Console.WriteLine("   CONNECTED to dedicated cloud master db81188.");
            await InspectMasterDatabaseAsync(conn);
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 2;
            Console.WriteLine($"   [BLOCKED] Master inspection failed ({ex.GetType().Name}, SQL code {(ex as SqlException)?.Number}); credentials omitted.");
        }
    }

    private static async Task InspectMasterDatabaseAsync(SqlConnection conn)
    {
        using var cmd = conn.CreateCommand();

        Console.WriteLine($"\n   --- Tables in {conn.Database} ---");
        cmd.CommandText = @"
            SELECT t.name AS TableName, SUM(p.rows) AS [RowCount]
            FROM sys.tables t
            INNER JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
            GROUP BY t.name
            ORDER BY t.name;";
        using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                Console.WriteLine($"      Table: {r.GetString(0),-30} Rows: {r.GetInt64(1)}");
            }
        }

        // Applied migrations
        Console.WriteLine($"\n   --- Applied Migrations in {conn.Database} ---");
        cmd.CommandText = @"
            IF OBJECT_ID('dbo.__EFMigrationsHistory') IS NOT NULL
                SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
            ELSE SELECT 'No __EFMigrationsHistory table';";
        using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                Console.WriteLine($"      [MIGRATION] {r.GetString(0)}");
            }
        }

        // Registered Tenants
        Console.WriteLine($"\n   --- Registered Tenants in {conn.Database} ---");
        cmd.CommandText = @"
            IF OBJECT_ID('dbo.Tenants') IS NOT NULL
                SELECT TenantId, DisplayName, ConnectionKey, Status, CurrentPlanId, IsActive, ServerName, DatabaseName FROM dbo.Tenants WHERE TenantId = 'tenant-a';
            ELSE SELECT 'No Tenants table';";
        try
        {
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                Console.WriteLine($"      Tenant: {r.GetString(0),-15} Name: {r.GetString(1),-25} ConnKey: {r.GetString(2),-15} Plan: {r.GetString(4),-10} Status: {r.GetString(3)} Active: {r.GetBoolean(5)} Host: {r.GetValue(6)} Database: {r.GetValue(7)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      Could not query Tenants: {ex.Message}");
        }

        foreach (var query in new[] {
            "SELECT Name FROM dbo.AspNetRoles ORDER BY Name",
            "SELECT PlanId, PlanName, IsActive FROM dbo.SubscriptionPlans ORDER BY PlanId",
            "SELECT PlanId, Status, StartDateUtc, EndDateUtc FROM dbo.Subscriptions WHERE TenantId = 'tenant-a'",
            "SELECT ModuleId, IsEnabled FROM dbo.ModuleEntitlements WHERE TenantId = 'tenant-a' ORDER BY ModuleId"
        })
        {
            cmd.CommandText = query;
            using var rows = await cmd.ExecuteReaderAsync();
            while (await rows.ReadAsync())
                Console.WriteLine("      Auth metadata: " + string.Join(" | ", Enumerable.Range(0, rows.FieldCount).Select(i => rows.GetValue(i).ToString())));
        }

        // Users
        Console.WriteLine($"\n   --- Users in {conn.Database} ---");
        cmd.CommandText = @"
            IF OBJECT_ID('dbo.AspNetUsers') IS NOT NULL
                SELECT Id, UserName, Email, LockoutEnabled, LockoutEnd FROM dbo.AspNetUsers WHERE Id IN (SELECT UserId FROM dbo.Memberships WHERE TenantId = 'tenant-a') OR Id IN (SELECT ur.UserId FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE r.Name='SuperAdmin');
            ELSE SELECT 'No AspNetUsers table';";
        try
        {
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                Console.WriteLine($"      User: {r.GetString(1),-20} Id: {r.GetString(0)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      Could not query AspNetUsers: {ex.Message}");
        }

        // Memberships
        Console.WriteLine($"\n   --- Memberships in {conn.Database} ---");
        cmd.CommandText = @"
            IF OBJECT_ID('dbo.Memberships') IS NOT NULL
                SELECT m.TenantId, u.UserName, m.Role, m.DisplayName, m.IsActive, m.ProfileProvisioningStatus
                FROM dbo.Memberships m
                LEFT JOIN dbo.AspNetUsers u ON m.UserId = u.Id WHERE m.TenantId = 'tenant-a';
            ELSE SELECT 'No Memberships table';";
        try
        {
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                Console.WriteLine($"      Tenant: {r.GetString(0),-12} User: {r.GetString(1),-15} Role: {r.GetString(2),-20} Profile: {r.GetString(5)} Active: {r.GetBoolean(4)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      Could not query Memberships: {ex.Message}");
        }
    }

    private static async Task InspectLocalDatabaseAsync(string connectionString)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            Console.WriteLine("   -> CONNECTED SUCCESSFULLY TO LOCAL DB_MasterCrm!");

            using var cmd = conn.CreateCommand();

            // Tables and row counts
            Console.WriteLine("\n   --- Tables & Record Counts on local DB_MasterCrm ---");
            cmd.CommandText = @"
                SELECT t.name AS TableName, SUM(p.rows) AS [RowCount]
                FROM sys.tables t
                INNER JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0, 1)
                GROUP BY t.name
                ORDER BY t.name;";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    Console.WriteLine($"      Table: {r.GetString(0),-30} Rows: {r.GetInt64(1)}");
                }
            }

            cmd.CommandText = "SELECT EntityType, Action, COUNT_BIG(*) FROM dbo.AuditEntries WHERE Actor LIKE '%test%' OR Actor LIKE '%smoke%' GROUP BY EntityType, Action ORDER BY EntityType, Action";
            using (var evidence = await cmd.ExecuteReaderAsync())
                while (await evidence.ReadAsync())
                    Console.WriteLine($"      Test-attributed audit: {evidence.GetString(0)} {evidence.GetString(1)} {evidence.GetInt64(2)}");
            cmd.CommandText = "SELECT COUNT(*) FROM dbo.JobOrders WHERE JobOrderId = 1";
            Console.WriteLine($"      Local Job #1 exact count: {await cmd.ExecuteScalarAsync()}");

            cmd.CommandText = "SELECT COUNT(*) FROM dbo.Customers WHERE FullName LIKE 'CRUD Test Customer %'";
            Console.WriteLine($"      Local CRUD-test customer markers: {await cmd.ExecuteScalarAsync()}");
            cmd.CommandText = "SELECT COUNT(*) FROM dbo.JobOrders WHERE EstimateId IN ('EST-TEST-001','EST-DEP-TEST','EST-OWNER-TEST','EST-LEG-01') OR EstimateId LIKE 'EST-CRUD-%'";
            Console.WriteLine($"      Local automated-test job markers: {await cmd.ExecuteScalarAsync()}");

            // Migrations on local DB_MasterCrm
            Console.WriteLine("\n   --- Applied Migrations on local DB_MasterCrm ---");
            cmd.CommandText = "SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    Console.WriteLine($"      [MIGRATION] {r.GetString(0)}");
                }
            }

            // Financial summary on local DB_MasterCrm
            Console.WriteLine("\n   --- Operational Financial Summary on local DB_MasterCrm ---");
            cmd.CommandText = @"
                SELECT 
                    COUNT(*) AS TotalPayments,
                    COALESCE(SUM(CASE WHEN IsVoided = 0 THEN AmountPaid ELSE 0 END), 0) AS ValidPaid,
                    COALESCE(SUM(CASE WHEN IsVoided = 1 THEN AmountPaid ELSE 0 END), 0) AS VoidedPaid,
                    COALESCE(MIN(PaymentId), 0) AS MinPayId,
                    COALESCE(MAX(PaymentId), 0) AS MaxPayId
                FROM dbo.Payments;";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    Console.WriteLine($"      Payments Count: {r.GetInt32(0)}");
                    Console.WriteLine($"      Valid Total:    PHP {r.GetDecimal(1):N2}");
                    Console.WriteLine($"      Voided Total:   PHP {r.GetDecimal(2):N2}");
                    Console.WriteLine($"      Payment ID Range: #{r.GetInt32(3)} - #{r.GetInt32(4)}");
                }
            }

            // Job Orders summary on local DB_MasterCrm
            Console.WriteLine("\n   --- Job Orders Summary on local DB_MasterCrm ---");
            cmd.CommandText = @"
                SELECT 
                    COUNT(*) AS TotalJobs,
                    COALESCE(MIN(JobOrderId), 0) AS MinJobId,
                    COALESCE(MAX(JobOrderId), 0) AS MaxJobId,
                    COALESCE(MIN(StartedAt), '1900-01-01') AS EarliestDate,
                    COALESCE(MAX(StartedAt), '1900-01-01') AS LatestDate
                FROM dbo.JobOrders;";
            using (var r = await cmd.ExecuteReaderAsync())
            {
                if (await r.ReadAsync())
                {
                    Console.WriteLine($"      Job Orders Count: {r.GetInt32(0)}");
                    Console.WriteLine($"      Job ID Range:     #{r.GetInt32(1)} - #{r.GetInt32(2)}");
                    Console.WriteLine($"      Date Range:       {r.GetDateTime(3):yyyy-MM-dd} to {r.GetDateTime(4):yyyy-MM-dd}");
                }
            }
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 2;
            Console.WriteLine($"   [FAIL] Local inspection failed ({ex.GetType().Name}, SQL code {(ex as SqlException)?.Number}).");
        }
    }
}
