using System.Text.Json;
using Microsoft.Data.SqlClient;
using CRM_Revcare_;

internal static class CompatibilityEvidence
{
    internal const string Output = "artifacts/historical-compatibility";
    public static async Task RunAsync()
    {
        Directory.CreateDirectory(Output);
        foreach(var mode in new[]{"local","cloud"})
        {
            var target=CrmDatabase.ResolveTarget(new[]{"--"+mode});
            if(target.DatabaseName != (mode=="cloud"?"db80735":"DB_MasterCrm")) throw new Exception("Unexpected read target");
            using var c=new SqlConnection(target.ConnectionString); await c.OpenAsync();
            var queries=new Dictionary<string,string>
            {
                ["keys"]="SELECT j.*, c.Email CustomerEmail, c.FullName Customer FROM JobOrders j LEFT JOIN Customers c ON c.CustomerId=j.CustomerId ORDER BY JobOrderId",
                ["identity"]="SELECT DB_NAME() DatabaseName, IDENT_CURRENT('JobOrders') LastIdentity, IDENT_INCR('JobOrders') IncrementValue",
                ["gap-references"]="SELECT 'JobTasks' SourceTable, JobOrderId FROM JobTasks WHERE JobOrderId BETWEEN 542 AND 571 UNION ALL SELECT 'Payments', JobOrderId FROM Payments WHERE JobOrderId BETWEEN 542 AND 571 UNION ALL SELECT 'Inquiries', ConvertedJobOrderId FROM Inquiries WHERE ConvertedJobOrderId BETWEEN 542 AND 571 UNION ALL SELECT 'Complaints', JobOrderId FROM Complaints WHERE JobOrderId BETWEEN 542 AND 571 UNION ALL SELECT 'Feedbacks', JobOrderId FROM Feedbacks WHERE JobOrderId BETWEEN 542 AND 571",
                ["audit"]="SELECT * FROM AuditEntries ORDER BY AuditEntryId",
                ["estimates"]="SELECT * FROM Estimates ORDER BY EstimateId",
                ["estimate-lines"]="SELECT * FROM EstimateLines ORDER BY EstimateLineId"
            };
            foreach(var (key,sql) in queries)
            {
                using var cmd=new SqlCommand(sql,c) {CommandTimeout=120}; using var reader=await cmd.ExecuteReaderAsync();
                var rows=new List<Dictionary<string,object>>();
                while(await reader.ReadAsync())
                {
                    var row=new Dictionary<string,object>();
                    for(int i=0;i<reader.FieldCount;i++) row[reader.GetName(i)]=reader.IsDBNull(i)?null!:reader.GetValue(i);
                    rows.Add(row);
                }
                await File.WriteAllTextAsync(Path.Combine(Output,mode+"-"+key+".json"),JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
                Console.WriteLine($"SELECT only {mode}/{target.DatabaseName}/{key}: {rows.Count}");
            }
        }
    }
}
