using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using crm.domain.entities;
using CRM.infrastructure.services;
using CRM_Revcare_;

// Offline rendering/ledger verification only. No context, connection, save or reconciliation.
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
string output = args.Length == 1 ? args[0] : "artifacts/business-review-20261124";
if (!Directory.Exists(output)) throw new InvalidOperationException("Generate the review package first.");
var sourceFiles = new[] {
    "artifacts/historical-compatibility/cloud-keys.json",
    "artifacts/historical-safety/payments.json",
    "artifacts/historical-safety/customers.json",
    "artifacts/historical-safety/tasks.json"
};
var hashes = sourceFiles.ToDictionary(p=>p,p=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
T Read<T>(string file) => JsonSerializer.Deserialize<T>(File.ReadAllText(file))!;
var jobs = Read<List<JobOrder>>(sourceFiles[0]);
var payments = Read<List<Payment>>(sourceFiles[1]);
var customers = Read<List<Customer>>(sourceFiles[2]).ToDictionary(c=>c.CustomerId);
var tasks = Read<List<JobTask>>(sourceFiles[3]);
var results = new List<object>();
int receiptCount = 0, workshopCount = 0, emptyHistories = 0;
foreach (var job in jobs.Where(j=>j.ApprovedEstimateId == null))
{
    if (!HistoricalCompatibility.IsProtected(job)) throw new Exception($"Job {job.JobOrderId} outside protection inventory");
    if (job.CustomerId is not int customerId || !customers.TryGetValue(customerId,out var customer)) throw new Exception("Customer record unavailable");
    job.Payments = payments.Where(p=>p.JobOrderId==job.JobOrderId).ToList();
    var validPaid = job.Payments.Where(p=>!p.IsVoided).Sum(p=>p.AmountPaid);
    var originalTotal = job.TotalEstimatedCost;
    var expectedBalance = originalTotal.HasValue ? Math.Max(0m,originalTotal.Value-validPaid) : (decimal?)null;
    if (job.TotalPaid != validPaid || job.OutstandingBalance != expectedBalance) throw new Exception("Application billing mismatch");
    if (job.Payments.Count==0) emptyHistories++;
    foreach (var p in job.Payments)
    {
        var html = ReportTemplateBuilder.BuildPaymentReceiptHtml(new PaymentReceiptData {
            PaymentId=p.PaymentId, PaymentDate=p.CreatedAt, CustomerName=customer.FullName,
            JobOrderId=job.JobOrderId, VehicleDetails=job.VehicleDisplay,
            PaymentType=p.PaymentType, PaymentMethod=p.PaymentMethod, AmountReceived=p.AmountPaid,
            TotalEstimatedCost=job.TotalEstimatedCost, TotalPaid=job.TotalPaid,
            OutstandingBalance=job.OutstandingBalance, CustomerCredit=job.CustomerCredit,
            IsVoided=p.IsVoided, VoidReason=p.VoidReason, VoidedAtUtc=p.VoidedAtUtc,
            RecordedBy="Not verified in this offline rendering check", PrintedBy="Offline review verification",
            HistoricalQualification=HistoricalCompatibility.Qualification(job)
        },new BusinessDetails { Name="RevCare — offline verification, not a newly issued receipt" });
        if (!html.Contains(WebUtility.HtmlEncode(customer.FullName)) ||
            !html.Contains(p.ReceiptReference) || !html.Contains("Recorded Job Total") ||
            !html.Contains("PHP "+p.AmountPaid.ToString("N2")) ||
            !html.Contains("verified itemized quotation unavailable") || html.Contains("Approved Total Estimated Cost"))
            throw new Exception($"Receipt renderer failed for {p.PaymentId}");
        if (originalTotal.HasValue && !html.Contains("PHP "+originalTotal.Value.ToString("N2"))) throw new Exception("Receipt substituted total");
        receiptCount++;
    }
    var jt=tasks.Where(t=>t.JobOrderId==job.JobOrderId).ToList();
    var work=ReportTemplateBuilder.BuildMechanicCopyHtml(new MechanicWorkOrderData {
        JobOrderId=job.JobOrderId, EstimateReference=HistoricalCompatibility.Reference(job),
        CustomerName=customer.FullName, VehicleDetails=job.VehicleDisplay, IntakeNotes=job.InquiryNotes,
        JobStatus=job.Status, AssignedMechanic=job.AssignedMechanicDisplay,
        Tasks=jt.Select(t=>new MechanicTaskItem { TaskId=t.JobTaskId,ServiceName=t.ServiceId,Status=t.Status,Notes=t.Remarks }).ToList()
    },new BusinessDetails { Name="RevCare — offline workshop report verification" });
    if (!work.Contains(WebUtility.HtmlEncode(customer.FullName)) || jt.Any(t=>!work.Contains(WebUtility.HtmlEncode(t.ServiceId))))
        throw new Exception("Workshop report lost customer/service history");
    if (work.Contains("PHP ") || job.TotalEstimatedCost!=originalTotal) throw new Exception("Workshop report financial leakage/mutation");
    workshopCount++;
    results.Add(new { job.JobOrderId, Customer=customer.FullName, PreservedTotal=originalTotal,
        ReceiptIds=job.Payments.Select(p=>p.PaymentId).ToArray(), NonvoidPayments=job.TotalPaid,
        job.OutstandingBalance, job.CustomerCredit, ReceiptRenderPassed=true, WorkshopRenderPassed=true,
        EmptyPaymentHistory=job.Payments.Count==0 });
}
if (workshopCount!=247 || emptyHistories!=12) throw new Exception("Unexpected coverage");
foreach (var file in sourceFiles)
    if(hashes[file]!=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)))) throw new Exception("Source evidence changed");
var report = new { Verification="PASS", Scope="Offline application domain calculations and receipt/workshop HTML rendering; no live database or new GUI interaction", Jobs=workshopCount,
    ReceiptsRendered=receiptCount, EmptyHistories=emptyHistories, SourceFilesUnchanged=true, SourceHashes=hashes, Results=results };
using (var file=new FileStream(Path.Combine(output,"usability-verification.json"),FileMode.CreateNew,FileAccess.Write))
    JsonSerializer.Serialize(file,report,new JsonSerializerOptions{WriteIndented=true});
Console.WriteLine($"PASS: {workshopCount} historical jobs retain customer/task reporting and authoritative billing calculations.");
Console.WriteLine($"PASS: {receiptCount} existing receipts rendered; {emptyHistories} empty histories retained explicitly.");
Console.WriteLine("PASS: Original source files unchanged. No database connections, saves, reconciliation or deployment.");
