using System.Data;
using System.Text.Json;
using CRM_Revcare_;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// Explicit, one-time business-data copy. Never copies accounts or routing configuration.
using var legacy = CrmDatabase.CreateContext();
var settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "Microsoft", "UserSecrets", "RevCare-Backend-Master-TenantA", "secrets.json");
var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(settingsPath))!;
using var source = new SqlConnection(legacy.Database.GetConnectionString());
using var target = new SqlConnection(settings["ConnectionStrings:TenantA"]);
if (source.Database != "db80735" || target.Database != "db81189")
    throw new InvalidOperationException("This reviewed import is restricted to db80735 -> db81189. Check configuration before proceeding.");
await source.OpenAsync(); await target.OpenAsync();
using var sourceTx = source.BeginTransaction(IsolationLevel.Serializable);
using var targetTx = target.BeginTransaction(IsolationLevel.Serializable);
using (var marker = new SqlCommand("SELECT TenantId FROM TenantDatabaseIdentity WHERE Id=1", target, targetTx))
    if ((string?)await marker.ExecuteScalarAsync() != "tenant-a") throw new InvalidOperationException("Destination is not Tenant A.");
string[] tables = ["Customers", "JobOrders", "JobTasks", "Payments", "Inquiries", "Feedbacks", "Complaints", "AuditEntries"];
long previousAuditId;
using (var auditMax = new SqlCommand("SELECT COALESCE(MAX(AuditEntryId),0) FROM AuditEntries", target, targetTx)) previousAuditId = (long)(await auditMax.ExecuteScalarAsync())!;
var datasets = new Dictionary<string, DataTable>();
foreach (string table in tables)
{
    using var count = new SqlCommand($"SELECT COUNT_BIG(*) FROM [{table}] WITH (UPDLOCK,HOLDLOCK)", target, targetTx);
    if ((long)(await count.ExecuteScalarAsync())! != 0 && table != "AuditEntries") throw new InvalidOperationException($"Target {table} is not empty. Import stopped; existing data was not replaced.");
    using var columnsCommand = new SqlCommand("SELECT name FROM sys.columns WHERE object_id=OBJECT_ID(@table) AND is_computed=0 AND system_type_id<>189 ORDER BY column_id", target, targetTx);
    columnsCommand.Parameters.AddWithValue("@table", table);
    var columns = new List<string>();
    using (var reader = await columnsCommand.ExecuteReaderAsync()) while (await reader.ReadAsync()) columns.Add(reader.GetString(0));
    if (columns.Count == 0) throw new InvalidOperationException("Missing target schema: " + table);
    if (table == "AuditEntries") columns.Remove("AuditEntryId");
    string columnList = string.Join(",", columns.Select(c => "[" + c.Replace("]", "]]") + "]"));
    using var read = new SqlCommand($"SELECT {columnList} FROM [{table}] ORDER BY {(table == "AuditEntries" ? "AuditEntryId" : "1")}", source, sourceTx) { CommandTimeout = 60 };
    using var dataReader = await read.ExecuteReaderAsync();
    var data = new DataTable(table); data.Load(dataReader); datasets[table] = data;
    Console.WriteLine($"{table}: {data.Rows.Count} source records ready");
}
if (!args.Contains("--apply"))
{
    Console.WriteLine("Read-only preflight passed. Use --apply during the agreed cutover window; keep legacy writes stopped.");
    return;
}
foreach (var (table, data) in datasets)
{
    using var copy = new SqlBulkCopy(target, (table == "AuditEntries" ? SqlBulkCopyOptions.Default : SqlBulkCopyOptions.KeepIdentity) | SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.KeepNulls, targetTx)
        { DestinationTableName = "[" + table + "]", BulkCopyTimeout = 120 };
    foreach (DataColumn column in data.Columns) copy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
    await copy.WriteToServerAsync(data);
    var columnList = string.Join(",", data.Columns.Cast<DataColumn>().Select(c => "[" + c.ColumnName + "]"));
    using var verify = new SqlCommand($"SELECT {columnList} FROM [{table}] {(table == "AuditEntries" ? "WHERE AuditEntryId > " + previousAuditId : "")} ORDER BY {(table == "AuditEntries" ? "AuditEntryId" : "1")}", target, targetTx);
    using var reader = await verify.ExecuteReaderAsync();
    var actual = new DataTable(); actual.Load(reader);
    if (actual.Rows.Count != data.Rows.Count) throw new InvalidOperationException("Count mismatch: " + table);
    for (int row = 0; row < data.Rows.Count; row++)
        for (int col = 0; col < data.Columns.Count; col++)
            if (!Equals(data.Rows[row][col], actual.Rows[row][col])) throw new InvalidOperationException($"Reconciliation failed: {table}, row {row}, column {data.Columns[col].ColumnName}");
    Console.WriteLine(table + ": every imported value verified (database-generated rowversions excluded)");
}
targetTx.Commit(); sourceTx.Commit();
Console.WriteLine("Import committed. Source data and accounts unchanged. Do not resume legacy writes after cutover.");
