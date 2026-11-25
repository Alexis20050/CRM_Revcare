using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using CRM_Revcare_;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

internal static class ProductionCompatibilityTests
{
    static void Check(bool condition,string description)
    {
        if(!condition) throw new Exception("FAIL: "+description);
        Console.WriteLine("PASS: "+description);
    }
    sealed class InterruptReads(string phase,string marker) : DbCommandInterceptor
    {
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,CommandExecutedEventData eventData,DbDataReader result,CancellationToken cancellationToken=default)
        {
            if((phase=="jobs" && command.CommandText.Contains("[JobOrders]")) || (phase=="payments" && command.CommandText.Contains("[Payments]")))
            {
                await File.WriteAllTextAsync(marker,"Actual Legacy3NfReconciliationService query reached: "+phase,cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(45),cancellationToken);
                throw new Exception("Parent did not terminate child");
            }
            return result;
        }
    }
    public static async Task ChildAsync(string name,string phase,string marker)
    {
        using var guarded=HistoricalRecoveryTests.Db(name); // validates disposable name, hardcoded LocalDB
        var options=new DbContextOptionsBuilder<MasterCrmDbContext>().UseSqlServer(guarded.Database.GetConnectionString()).AddInterceptors(new InterruptReads(phase,marker)).Options;
        using var db=new MasterCrmDbContext(options);
        await Legacy3NfReconciliationService.Reconcile3NfAsync(db,false);
    }
    public static async Task RunAsync()
    {
        Directory.CreateDirectory(CompatibilityEvidence.Output);
        Check(new Payment().PayerType=="Unknown" && new Payment().PaymentPurpose=="Unknown","New receipts do not default to an invented payer or payment stage");
        var name=await HistoricalRecoveryTests.CreateVerifiedCopyAsync();
        var before=await HistoricalRecoveryTests.Fingerprint(name);
        foreach(var phase in new[]{"jobs","payments"})
        {
            var marker=Path.GetFullPath(Path.Combine(CompatibilityEvidence.Output,name+"-"+phase+".signal"));
            var psi=new ProcessStartInfo("dotnet") {UseShellExecute=false,CreateNoWindow=true};
            psi.ArgumentList.Add(typeof(ProductionCompatibilityTests).Assembly.Location);
            foreach(var arg in new[]{"--production-compatibility-child",name,phase,marker}) psi.ArgumentList.Add(arg);
            using var process=Process.Start(psi)!;
            var watch=Stopwatch.StartNew();
            while(!File.Exists(marker) && !process.HasExited && watch.Elapsed<TimeSpan.FromSeconds(30)) await Task.Delay(100);
            if(!File.Exists(marker)) {if(!process.HasExited) process.Kill(true);throw new Exception("Actual-service checkpoint not reached");}
            process.Kill(true); await process.WaitForExitAsync();
            Check(before==await HistoricalRecoveryTests.Fingerprint(name),$"Actual production service hard-killed at {phase} SELECT: all rows/rowversions/audits unchanged");
            using var retry=HistoricalRecoveryTests.Db(name);
            var report=await Legacy3NfReconciliationService.Reconcile3NfAsync(retry,false);
            Check(report.TotalJobsEvaluated==400 && report.UnresolvedJobs.Count==247 && report.NewlyLinkedCount==0,$"Actual service retry after {phase} interruption completes without reconstruction");
        }
        using(var db=HistoricalRecoveryTests.Db(name))
        {
            var first=await Legacy3NfReconciliationService.Reconcile3NfAsync(db,false);
            var second=await Legacy3NfReconciliationService.Backfill3NfAsync(db,false);
            Check(first.UnresolvedJobs.SequenceEqual(second.UnresolvedJobs) && second.PaymentsCorrectedCount==0,"Both production entry points repeat with identical review results and no corrections");
            using var cancel=new CancellationTokenSource(); cancel.Cancel();
            try {await Legacy3NfReconciliationService.Reconcile3NfAsync(db,false,cancel.Token);throw new Exception("Cancellation ignored");}
            catch(OperationCanceledException) {Console.WriteLine("PASS: Actual service cancellation propagates");}
            await Legacy3NfReconciliationService.Reconcile3NfAsync(db,false);
        }
        await Task.WhenAll(Enumerable.Range(0,8).Select(async _=> {using var db=HistoricalRecoveryTests.Db(name);await Legacy3NfReconciliationService.Reconcile3NfAsync(db,false);}));
        Check(before==await HistoricalRecoveryTests.Fingerprint(name),"Actual service eight concurrent attempts, cancellation retry and second success preserve every row, audit, receipt and link");
        Console.WriteLine("No estimate-creation/link-write phases exist in the actual implementation. Those interruption boundaries are inapplicable; no probe substituted.");

        async Task Reject(Action<MasterCrmDbContext> change,string message)
        {
            using var db=HistoricalRecoveryTests.Db(name);change(db);
            try {await db.SaveChangesAsync();throw new Exception("Guard allowed "+message);}
            catch(InvalidOperationException e) when(e.Message.Contains("Historical financial record is protected") || e.Message.Contains("Saved receipts cannot be changed")) {Console.WriteLine("PASS: "+message+" rejected");}
        }
        await Reject(db=>db.JobOrders.Find(572)!.TotalEstimatedCost=1500m,"Catalog total replacing authoritative total");
        await Reject(db=>db.JobOrders.Find(572)!.TotalEstimatedCost=null,"Clearing authoritative total");
        await Reject(db=>db.JobOrders.Find(572)!.CreatedAt=DateTime.UtcNow,"Changing identity fingerprint to bypass protection");
        await Reject(db=>db.JobOrders.Find(572)!.ApprovedEstimateId=154,"Linking unsupported estimate");
        await Reject(db=>db.JobOrders.Remove(db.JobOrders.IgnoreAutoIncludes().Single(j=>j.JobOrderId==572)),"Deleting historical job");
        await Reject(db=>db.JobTasks.First(t=>t.JobOrderId==572).AgreedPrice=3500m,"Changing task price");
        await Reject(db=>db.JobTasks.Remove(db.JobTasks.First(t=>t.JobOrderId==572)),"Deleting historical task");
        await Reject(db=>db.JobTasks.Add(new JobTask{JobOrderId=572,ServiceId="Brake inspection"}),"Adding balancing task");
        await Reject(db=>db.Estimates.Find(154)!.ApprovedAt=DateTime.UtcNow,"Changing reconstructed approval timestamp");
        await Reject(db=>db.EstimateLines.First(l=>l.EstimateId==154).UnitPrice=1,"Changing reconstructed line price");
        await Reject(db=>db.Payments.First(p=>p.JobOrderId==572).AmountPaid=1,"Changing original receipt amount");
        await Reject(db=>db.Payments.First(p=>p.JobOrderId==572).PayerType="Insurance","Changing receipt payer through ordinary save");
        await Reject(db=> {db.IsBackfillOperation=true;db.JobOrders.Find(572)!.TotalEstimatedCost=1;},"Bypassing historical total guard with backfill flag");
        Check(before==await HistoricalRecoveryTests.Fingerprint(name),"All rejected ordinary edits leave every original field unchanged");
        using(var db=HistoricalRecoveryTests.Db(name))
        {
            await using var tx=await db.Database.BeginTransactionAsync();
            var job=await db.JobOrders.Include(j=>j.Payments).SingleAsync(j=>j.JobOrderId==572);
            var total=job.TotalEstimatedCost; var paid=job.TotalPaid;
            job.InquiryNotes+=" Compatibility test service follow-up.";
            await db.SaveChangesAsync();
            Check(job.TotalEstimatedCost==total && job.TotalPaid==paid && await db.AuditEntries.AnyAsync(a=>a.EntityType=="JobOrder" && a.Changes.Contains("Compatibility test service follow-up")),"Service notes remain editable and audited without changing finances");
            db.Payments.Add(new Payment{JobOrderId=572,AmountPaid=10,PaymentMethod="Cash",PaymentType="Progress Payment",PayerType="Unknown",PaymentPurpose="Progress"});
            await db.SaveChangesAsync();
            Check(await db.Payments.CountAsync()==401,"New receipt accepted against preserved total without requiring a quotation (rolled-back test only)");
            await tx.RollbackAsync();
        }
        Check(before==await HistoricalRecoveryTests.Fingerprint(name),"Rolled-back notes/payment usability test restores original receipts, finances and audits");
        VerifyUi(name);
        Check(before==await HistoricalRecoveryTests.Fingerprint(name),"UI loading and receipt rendering do not modify historical data");
        Console.WriteLine("PASS: actual production implementation and compatibility guard tests complete on "+name);
    }
    static void VerifyUi(string name)
    {
        Exception failure=null;
        var thread=new Thread(()=>
        {
            try
            {
                Func<MasterCrmDbContext> factory=()=>HistoricalRecoveryTests.Db(name);
                using var db=factory();var job=db.JobOrders.Include(j=>j.Payments).Single(j=>j.JobOrderId==572);
                using var form=new CrmTransactionsForm(factory,_=>false);
                var page=typeof(CrmTransactionsForm).GetField("ordersPage",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(form)!;
                var controller=page.GetType().BaseType!.GetField("controller",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(page)!;
                var load=controller.GetType().GetMethod("LoadEditor")!;
                load.Invoke(controller,new object[]{job});
                object Field(string field)=>page.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)!.GetValue(page)!;
                var cost=(System.Windows.Forms.TextBox)Field("estimatedCost");
                Check(cost.ReadOnly && decimal.Parse(cost.Text)==3500m,"Actual job editor retains 3500 authoritative total instead of 1500 catalog price");
                Check(!((System.Windows.Forms.CheckedListBox)Field("requestedServices")).Enabled,"Historical service list cannot reprice/add tasks through ordinary editor");
                Check(((System.Windows.Forms.TextBox)Field("estimate")).Text=="Not recorded","Absent quotation reference is not replaced with generated EST number");
                job.TotalEstimatedCost=null;load.Invoke(controller,new object[]{job});
                Check(cost.Text=="","Missing historical total stays unknown in editor");
                job.TotalEstimatedCost=0;load.Invoke(controller,new object[]{job});
                Check(decimal.Parse(cost.Text)==0,"Zero historical total stays zero in editor");
                var html=ReportTemplateBuilder.BuildPaymentReceiptHtml(new PaymentReceiptData{PaymentId=1,JobOrderId=572,AmountReceived=1750,TotalEstimatedCost=3500,TotalPaid=1750,OutstandingBalance=1750,HistoricalQualification=HistoricalCompatibility.Qualification(job)});
                Check(html.Contains("Recorded Job Total") && html.Contains("verified itemized quotation unavailable") && !html.Contains("Approved Total Estimated Cost"),"Receipt qualifies historical itemization without claiming approval");
                File.WriteAllText(Path.Combine(CompatibilityEvidence.Output,"receipt-preview.html"),html);
            }
            catch(Exception e){failure=e;}
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();
        if(failure!=null) throw new Exception("UI compatibility failed",failure);
    }
}
