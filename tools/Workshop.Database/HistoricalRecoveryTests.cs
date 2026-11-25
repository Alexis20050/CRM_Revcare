using System.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// LOCAL TEST HARNESS ONLY. This is not a production reconciliation write path.
internal static class HistoricalRecoveryTests
{
    const string Prefix = "RevCare_HistorySafety_";
    static readonly (string Table, string File)[] Tables = [("Staff","staff"),("Customers","customers"),("Services","services"),("Vehicles","vehicles"),("Estimates","estimates"),("EstimateLines","estimate-lines"),("JobOrders","jobs"),("JobTasks","tasks"),("Payments","payments"),("AuditEntries","audit")];
    static string Connection(string name)
    {
        if (!name.StartsWith(Prefix, StringComparison.Ordinal) || name.Any(c=>!char.IsAsciiLetterOrDigit(c) && c!='_')) throw new Exception("Disposable name required");
        return $@"Server=(localdb)\MSSQLLocalDB;Database={name};Integrated Security=True;TrustServerCertificate=True;Pooling=False";
    }
    internal static MasterCrmDbContext Db(string name) => new(new DbContextOptionsBuilder<MasterCrmDbContext>().UseSqlServer(Connection(name)).Options);
    static async Task Execute(SqlConnection c, string sql)
    {
        using var cmd = new SqlCommand(sql,c) { CommandTimeout=120 };
        await cmd.ExecuteNonQueryAsync();
    }
    static void Check(bool result,string description)
    {
        if(!result) throw new Exception("FAIL: "+description);
        Console.WriteLine("PASS: "+description);
    }
    internal static async Task<string> Fingerprint(string name, bool financialOnly=false)
    {
        using var c = new SqlConnection(Connection(name)); await c.OpenAsync();
        var output = new StringBuilder();
        foreach(var (table,_) in Tables)
        {
            if(financialOnly && table is "Estimates" or "EstimateLines") continue;
            using var cmd=new SqlCommand($"SELECT * FROM [{table}] ORDER BY 1",c);
            using var r=await cmd.ExecuteReaderAsync();
            while(await r.ReadAsync())
            {
                output.Append(table);
                for(int i=0;i<r.FieldCount;i++)
                {
                    var field=r.GetName(i);
                    if (financialOnly && (table is "Estimates" or "EstimateLines" || field is "ApprovedEstimateId" or "RowVersion")) continue;
                    output.Append('|').Append(field).Append(':').Append(r.IsDBNull(i)?"<NULL>":r.GetValue(i) is byte[] bytes?Convert.ToHexString(bytes):Convert.ToString(r.GetValue(i),System.Globalization.CultureInfo.InvariantCulture));
                }
                if (!financialOnly || table is not ("Estimates" or "EstimateLines")) output.AppendLine();
            }
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output.ToString())));
    }
    internal static async Task<string> CreateVerifiedCopyAsync()
    {
        var name=Prefix+Guid.NewGuid().ToString("N");
        Console.WriteLine("Disposable target: (localdb)\\MSSQLLocalDB / "+name);
        using(var db=Db(name)) await db.Database.MigrateAsync();
        using(var c=new SqlConnection(Connection(name)))
        {
            await c.OpenAsync();
            foreach(var (table,file) in Tables)
            {
                using var doc=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(HistoricalSafety.Output,file+".json")));
                using var cmd=new SqlCommand($"SELECT TOP(0) * FROM [{table}]",c);
                using var reader=await cmd.ExecuteReaderAsync();
                var data=new DataTable();
                for(int i=0;i<reader.FieldCount;i++) if(reader.GetName(i)!="RowVersion") data.Columns.Add(reader.GetName(i),reader.GetFieldType(i));
                await reader.CloseAsync();
                foreach(var item in doc.RootElement.EnumerateArray())
                {
                    var row=data.NewRow();
                    foreach(DataColumn col in data.Columns)
                    {
                        var v=item.GetProperty(col.ColumnName);
                        row[col]=v.ValueKind==JsonValueKind.Null?DBNull.Value:JsonSerializer.Deserialize(v.GetRawText(),col.DataType)!;
                    }
                    data.Rows.Add(row);
                }
                using var bulk=new SqlBulkCopy(c,SqlBulkCopyOptions.KeepIdentity|SqlBulkCopyOptions.CheckConstraints,null) { DestinationTableName=table };
                foreach(DataColumn col in data.Columns) bulk.ColumnMappings.Add(col.ColumnName,col.ColumnName);
                await bulk.WriteToServerAsync(data);
                // Compare every copied field to the source, including identity and financial values.
                using var verify=new SqlCommand($"SELECT * FROM [{table}] ORDER BY 1",c);
                using var actual=await verify.ExecuteReaderAsync();
                var expected=data.Rows.Cast<DataRow>().ToDictionary(r=>r[0].ToString()!);
                int count=0;
                while(await actual.ReadAsync())
                {
                    var expectedRow=expected[actual.GetValue(0).ToString()!];
                    foreach(DataColumn col in data.Columns)
                        if(!Equals(expectedRow[col],actual[col.ColumnName])) throw new Exception($"Copy mismatch {table}.{col.ColumnName}");
                    count++;
                }
                Check(count==data.Rows.Count,$"Verified copy: {table}, {count} rows, every non-rowversion field equal");
            }
        }
        return name;
    }

    public static async Task RunAsync()
    {
        var name=await CreateVerifiedCopyAsync();
        var before=await Fingerprint(name);
        using(var db=Db(name))
        {
            var dry=await Legacy3NfReconciliationService.Reconcile3NfAsync(db,true);
            var first=await Legacy3NfReconciliationService.Reconcile3NfAsync(db,false);
            var second=await Legacy3NfReconciliationService.Reconcile3NfAsync(db,false);
            Check(first.NewlyLinkedCount==0 && first.PaymentsCorrectedCount==0 && second.NewlyLinkedCount==0,"Current reconciler refuses unsupported history in both modes and on retry");
            Check(!db.ChangeTracker.HasChanges(),"No tracked changes after dry run or execution");
        }
        await Task.WhenAll(Enumerable.Range(0,2).Select(async _=> { using var db=Db(name); await Legacy3NfReconciliationService.Reconcile3NfAsync(db,false); }));
        Check(before==await Fingerprint(name),"Current reconciler concurrent attempts and second success leave all copied rows and rowversions unchanged");

        // Exercise physical SQL rollback with clearly synthetic test artifacts only.
        // No historical approval is asserted by this probe. Future authorized writer
        // must adopt and independently validate this transaction/locking boundary.
        var financial=await Fingerprint(name,true);
        foreach(var phase in new[]{"during-estimate","before-link"})
        {
            int job=phase=="during-estimate"?496:497;
            var baseline=await Fingerprint(name);
            var marker=Path.GetFullPath(Path.Combine(HistoricalSafety.Output,name+"-"+phase+".signal"));
            var psi=new ProcessStartInfo("dotnet") { UseShellExecute=false,CreateNoWindow=true };
            psi.ArgumentList.Add(typeof(HistoricalRecoveryTests).Assembly.Location);
            foreach(var arg in new[]{"--recovery-child",name,job.ToString(),phase,marker}) psi.ArgumentList.Add(arg);
            using var child=Process.Start(psi)!;
            var timeout=Stopwatch.StartNew();
            while(!File.Exists(marker) && !child.HasExited && timeout.Elapsed<TimeSpan.FromSeconds(30)) await Task.Delay(100);
            if(!File.Exists(marker)) { if(!child.HasExited) child.Kill(true); throw new Exception("Child did not reach interruption point"); }
            child.Kill(true); await child.WaitForExitAsync();
            Check(baseline==await Fingerprint(name),$"Hard process interruption {phase}: full transaction rolled back, no orphan header/lines/link/receipt");
            await ProbeAsync(name,job,null,null);
            var success=await Fingerprint(name);
            await ProbeAsync(name,job,null,null);
            Check(success==await Fingerprint(name),$"Retry after {phase} succeeds; second success makes zero changes including rowversions");
        }
        await Task.WhenAll(ProbeAsync(name,498,null,null),ProbeAsync(name,498,null,null));
        using(var db=Db(name))
        {
            Check(await db.Estimates.CountAsync(e=>e.EstimateNumber.StartsWith("SAFETY-TEST-"))==3,"Concurrent same-job attempts: one estimate per job");
            Check(await db.JobOrders.CountAsync(j=>j.JobOrderId>=496 && j.JobOrderId<=498 && j.ApprovedEstimateId!=null)==3,"Exactly three test links");
            Check(!await db.Estimates.GroupBy(e=>e.EstimateNumber).AnyAsync(g=>g.Count()>1),"No duplicate estimates");
            Check(!await db.Payments.GroupBy(p=>p.SubmissionId).AnyAsync(g=>g.Count()>1),"No duplicate receipt submission IDs");
        }
        Check(financial==await Fingerprint(name,true),"All original financial/task/payment fields and receipt counts unchanged");
        await BackupRestoreAsync(name);
        Console.WriteLine("LIMITATION: mutation/crash probe is local test infrastructure, not an authorized historical reconstruction path. Provider backup artifact/account restore remains unverified.");
    }

    public static async Task ProbeAsync(string name,int job,string phase,string marker)
    {
        using var c=new SqlConnection(Connection(name)); await c.OpenAsync();
        using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable);
        async Task<object> Scalar(string sql)
        {
            using var cmd=new SqlCommand(sql,c,tx) { CommandTimeout=60 };
            cmd.Parameters.AddWithValue("@job",job);
            return (await cmd.ExecuteScalarAsync())!;
        }
        await Scalar("DECLARE @r int, @resource nvarchar(255)=CONCAT('SafetyProbe-',@job); EXEC @r=sp_getapplock @Resource=@resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000; IF @r<0 THROW 51000,'Lock unavailable',1; SELECT @r;");
        var link=await Scalar("SELECT ApprovedEstimateId FROM JobOrders WITH(UPDLOCK,HOLDLOCK) WHERE JobOrderId=@job");
        if(link is not DBNull) { await tx.CommitAsync(); return; }
        var id=Convert.ToInt32(await Scalar("INSERT Estimates(EstimateNumber,CustomerId,VehicleId,Status,TotalAmount,ApprovedAt,Notes,CreatedAt) SELECT CONCAT('SAFETY-TEST-',JobOrderId),CustomerId,VehicleId,'Draft',TotalEstimatedCost,NULL,'SYNTHETIC DISPOSABLE RECOVERY PROBE: NOT HISTORICAL APPROVAL',CreatedAt FROM JobOrders WHERE JobOrderId=@job; SELECT CAST(SCOPE_IDENTITY() AS int);"));
        async Task Pause(string at)
        {
            if(phase!=at) return;
            await File.WriteAllTextAsync(marker,"Uncommitted transaction reached "+at);
            await Task.Delay(TimeSpan.FromSeconds(45));
            throw new Exception("Expected parent to terminate test process");
        }
        await Pause("during-estimate"); // Header exists; lines have not been inserted.
        await Scalar($"INSERT EstimateLines(EstimateId,ServiceCatalogId,Description,Quantity,UnitPrice,LineTotal) SELECT {id},ServiceCatalogId,ServiceId,1,AgreedPrice,AgreedPrice FROM JobTasks WHERE JobOrderId=@job; SELECT 1;");
        await Pause("before-link"); // Header and lines exist, no link yet.
        await Scalar($"UPDATE JobOrders SET ApprovedEstimateId={id} WHERE JobOrderId=@job; SELECT 1;");
        await tx.CommitAsync();
    }

    static async Task BackupRestoreAsync(string name)
    {
        var restored=name+"_Restored";
        var path=Path.GetFullPath(Path.Combine(HistoricalSafety.Output,name+".bak"));
        using var c=new SqlConnection(Connection(name)); await c.OpenAsync();
        await Execute(c,$"BACKUP DATABASE [{name}] TO DISK=N'{path.Replace("'","''")}' WITH COPY_ONLY, CHECKSUM;");
        await Execute(c,$"RESTORE VERIFYONLY FROM DISK=N'{path.Replace("'","''")}' WITH CHECKSUM;");
        using var fileCmd=new SqlCommand($"RESTORE FILELISTONLY FROM DISK=N'{path.Replace("'","''")}'",c);
        var moves=new List<string>();
        using(var reader=await fileCmd.ExecuteReaderAsync()) while(await reader.ReadAsync())
        {
            var logical=reader["LogicalName"].ToString()!.Replace("'","''");
            var extension=reader["Type"].ToString()=="L"?"ldf":"mdf";
            var file=Path.GetFullPath(Path.Combine(HistoricalSafety.Output,restored+"."+extension)).Replace("'","''");
            moves.Add($"MOVE N'{logical}' TO N'{file}'");
        }
        await Execute(c,$"RESTORE DATABASE [{restored}] FROM DISK=N'{path.Replace("'","''")}' WITH "+string.Join(',',moves)+", CHECKSUM;");
        Check(await Fingerprint(name)==await Fingerprint(restored),"Native COPY_ONLY .bak restored to distinct local database; every copied row including rowversions matches");
        using var restoredConnection=new SqlConnection(Connection(restored)); await restoredConnection.OpenAsync();
        await Execute(restoredConnection,"DBCC CHECKDB WITH NO_INFOMSGS, ALL_ERRORMSGS;");
        Check(true,"Restored database DBCC CHECKDB succeeds");
    }
}
