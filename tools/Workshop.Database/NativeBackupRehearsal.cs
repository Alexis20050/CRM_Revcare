#nullable enable
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using CRM.infrastructure.data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Workshop.Database;

// Native backup rehearsal only. Never accepts a destination connection string.
public static class NativeBackupRehearsal
{
    static string Q(string value) => "[" + value.Replace("]", "]]") + "]";
    static string S(string value) => "N'" + value.Replace("'", "''") + "'";
    public static async Task RunAsync(string source, string file)
    {
        if (source is not ("db80735" or "db81188" or "db81189")) throw new ArgumentException("Expected db80735, db81188 or db81189.");
        file = Path.GetFullPath(file);
        if (!File.Exists(file)) throw new FileNotFoundException("Native provider backup is required.", file);
        using var input = File.OpenRead(file);
        Console.WriteLine($"Backup SHA256: {Convert.ToHexString(await SHA256.HashDataAsync(input))}");
        input.Close();
        var name = "RevCare_Restore_" + source + "_" + Guid.NewGuid().ToString("N");
        var connection = new SqlConnectionStringBuilder { DataSource = @"(localdb)\MSSQLLocalDB", InitialCatalog = "master", IntegratedSecurity = true, TrustServerCertificate = true };
        await using var admin = new SqlConnection(connection.ConnectionString);
        await admin.OpenAsync();
        async Task Execute(string sql)
        {
            await using var cmd = admin.CreateCommand(); cmd.CommandText = sql; cmd.CommandTimeout = 300;
            await cmd.ExecuteNonQueryAsync();
        }
        await using (var header = admin.CreateCommand())
        {
            header.CommandText = "RESTORE HEADERONLY FROM DISK = " + S(file);
            await using var rows = await header.ExecuteReaderAsync();
            if (!await rows.ReadAsync() || Convert.ToString(rows["DatabaseName"]) != source
                || Convert.ToInt32(rows["BackupType"]) != 1 || Convert.ToInt32(rows["Position"]) != 1)
                throw new InvalidOperationException("Backup set 1 must be a full native backup of the requested database.");
            Console.WriteLine($"Native source: {source}; backup finished: {rows["BackupFinishDate"]}; SQL major: {rows["SoftwareVersionMajor"]}");
        }
        await Execute("RESTORE VERIFYONLY FROM DISK = " + S(file) + " WITH FILE = 1");
        var directory = Path.Combine(Path.GetTempPath(), name);
        Directory.CreateDirectory(directory);
        var moves = new List<string>();
        await using (var list = admin.CreateCommand())
        {
            list.CommandText = "RESTORE FILELISTONLY FROM DISK = " + S(file) + " WITH FILE = 1";
            await using var rows = await list.ExecuteReaderAsync();
            while (await rows.ReadAsync())
            {
                var type = Convert.ToString(rows["Type"]);
                if (type is not ("D" or "L")) throw new InvalidOperationException("Unsupported backup file type; manual isolated restore required.");
                var physical = Path.Combine(directory, moves.Count + (type == "L" ? ".ldf" : ".mdf"));
                moves.Add("MOVE " + S(Convert.ToString(rows["LogicalName"])!) + " TO " + S(physical));
            }
        }
        // Unique catalog + unique physical files; deliberately no REPLACE or DROP.
        await Execute("IF DB_ID(" + S(name) + ") IS NOT NULL THROW 50000, 'Destination exists', 1; RESTORE DATABASE "
            + Q(name) + " FROM DISK = " + S(file) + " WITH FILE = 1, " + string.Join(",", moves));
        Console.WriteLine("Restored disposable database: " + name);
        connection.InitialCatalog = name;
        await using var restored = new SqlConnection(connection.ConnectionString);
        await restored.OpenAsync();
        var columns = new Dictionary<string, List<string>>();
        await using (var cmd = restored.CreateCommand())
        {
            cmd.CommandText = "SELECT s.name,t.name,c.name FROM sys.tables t JOIN sys.schemas s ON t.schema_id=s.schema_id JOIN sys.columns c ON c.object_id=t.object_id WHERE t.name <> '__EFMigrationsHistory' AND c.system_type_id <> 189 ORDER BY s.name,t.name,c.column_id";
            await using var rows = await cmd.ExecuteReaderAsync();
            while (await rows.ReadAsync())
            {
                var table = Q(rows.GetString(0)) + "." + Q(rows.GetString(1));
                if (!columns.ContainsKey(table)) columns[table] = [];
                columns[table].Add(Q(rows.GetString(2)));
            }
        }
        async Task<Dictionary<string,string>> Fingerprints()
        {
            var result = new Dictionary<string,string>();
            foreach (var table in columns)
            {
                await using var cmd = restored.CreateCommand();
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
        var before = await Fingerprints();
        if (source == "db80735")
        {
            await using var db = new MasterCrmDbContext(new DbContextOptionsBuilder<MasterCrmDbContext>().UseSqlServer(connection.ConnectionString).Options);
            Console.WriteLine("Pending migrations: " + string.Join(", ", await db.Database.GetPendingMigrationsAsync()));
            await db.Database.MigrateAsync();
            if (await db.DatabaseIdentity.CountAsync(x => x.Id == 1 && x.TenantId == "tenant-a") != 1)
                throw new InvalidOperationException("Tenant identity validation failed.");
        }
        else if (source == "db81189")
        {
            await using var db = new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(connection.ConnectionString).Options);
            Console.WriteLine("Pending migrations: " + string.Join(", ", await db.Database.GetPendingMigrationsAsync()));
            await db.Database.MigrateAsync();
            if (await db.DatabaseIdentity.CountAsync(x => x.Id == 1 && x.TenantId == "tenant-a") != 1)
                throw new InvalidOperationException("Tenant identity validation failed.");
        }
        else Console.WriteLine("Master restored for inspection; no master migrations or registrations changed.");
        var after = await Fingerprints();
        foreach (var table in before)
            if (after[table.Key] != table.Value) throw new InvalidOperationException("Historical data changed in " + table.Key);
        await using (var check = restored.CreateCommand())
        {
            check.CommandText = "DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS"; check.CommandTimeout = 300;
            await using var rows = await check.ExecuteReaderAsync();
            if (await rows.ReadAsync()) throw new InvalidOperationException("Constraint violations detected.");
        }
        Console.WriteLine($"PASS: {before.Count} existing tables preserved (all original values except rowversion); constraints validated.");
        Console.WriteLine("Retained disposable restore for review: " + name);
    }
}
