using System.Text.Json;
using Microsoft.Data.SqlClient;
using CRM_Revcare_;

internal static class HistoricalSafety
{
    internal const string Output = "artifacts/historical-safety";
    internal static async Task AuditAsync()
    {
        Directory.CreateDirectory(Output);
        var target = CrmDatabase.ResolveTarget(new[] { "--cloud" });
        if (target.DatabaseName != "db80735") throw new InvalidOperationException("Unexpected audit target");
        using var connection = new SqlConnection(target.ConnectionString);
        await connection.OpenAsync();
        var queries = new Dictionary<string, string>
        {
            ["provider"] = "SELECT DB_NAME() DatabaseName, @@SERVERNAME ServerName, @@VERSION Version, SERVERPROPERTY('Edition') Edition, DATABASEPROPERTYEX(DB_NAME(),'Recovery') RecoveryModel, HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','BACKUP DATABASE') CanBackupDatabase, HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','BACKUP LOG') CanBackupLog",
            ["jobs"] = "SELECT j.*, c.FullName Customer, c.Email CustomerEmail FROM JobOrders j JOIN Customers c ON c.CustomerId=j.CustomerId ORDER BY JobOrderId",
            ["tasks"] = "SELECT * FROM JobTasks ORDER BY JobTaskId",
            ["services"] = "SELECT * FROM Services ORDER BY ServiceId",
            ["estimates"] = "SELECT * FROM Estimates ORDER BY EstimateId",
            ["estimate-lines"] = "SELECT * FROM EstimateLines ORDER BY EstimateLineId",
            ["payments"] = "SELECT * FROM Payments ORDER BY PaymentId",
            ["audit"] = "SELECT * FROM AuditEntries ORDER BY AuditEntryId",
            ["schema"] = "SELECT TABLE_NAME,COLUMN_NAME,DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS ORDER BY TABLE_NAME,ORDINAL_POSITION",
            ["migrations"] = "SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId"
        };
        foreach (var table in new[] { "Customers", "Staff", "Vehicles", "Inquiries", "Complaints", "Feedbacks", "Appointments" })
            queries[table.ToLowerInvariant()] = "SELECT * FROM [" + table + "]";
        foreach (var (name, sql) in queries)
        {
            using var command = new SqlCommand(sql, connection) { CommandTimeout = 120 };
            using var reader = await command.ExecuteReaderAsync();
            var rows = new List<Dictionary<string, object>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                rows.Add(row);
            }
            await File.WriteAllTextAsync(Path.Combine(Output, name + ".json"), JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"READ ONLY {name}: {rows.Count} rows");
        }
        Console.WriteLine($"Host: {target.ServerDisplayName}; database: {target.DatabaseName}. No DML, migration, backup or restore issued.");
    }
}
