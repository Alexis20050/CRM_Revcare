#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using crm.domain.entities;

namespace CRM_Revcare_
{
    public sealed class BusinessDetails
    {
        public string Name { get; set; } = "RevCare Auto Hub";
        public string? Tagline { get; set; } = "Automotive Care, Repair & Maintenance Specialists";
        public string? Address { get; set; }
        public string? Contact { get; set; }
    }

    public sealed class MechanicWorkOrderData
    {
        public int JobOrderId { get; set; }
        public string EstimateReference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string VehicleDetails { get; set; } = string.Empty;
        public string IntakeNotes { get; set; } = string.Empty;
        public string JobStatus { get; set; } = "In Progress";
        public string AssignedMechanic { get; set; } = "Unassigned";
        public DateTime PrintDate { get; set; } = DateTime.UtcNow;
        public List<MechanicTaskItem> Tasks { get; set; } = new();
    }

    public sealed class MechanicTaskItem
    {
        public int TaskId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string AssignedTechnician { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string ReviewStatus { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    public sealed class PaymentReceiptData
    {
        public string HistoricalQualification { get; set; } = string.Empty;
        public int PaymentId { get; set; }
        public string ReceiptReference => ReportTemplateBuilder.FormatReceiptReference(PaymentId);
        public DateTime PaymentDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int JobOrderId { get; set; }
        public string VehicleDetails { get; set; } = string.Empty;
        public string PaymentType { get; set; } = "Downpayment";
        public string PaymentMethod { get; set; } = "Cash";
        public decimal AmountReceived { get; set; }
        public decimal AmountPaid { get => AmountReceived; set => AmountReceived = value; }
        public decimal? TotalEstimatedCost { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPaidValid { get => TotalPaid; set => TotalPaid = value; }
        public decimal? OutstandingBalance { get; set; }
        public decimal? CustomerCredit { get; set; }
        public string RecordedBy { get; set; } = string.Empty;
        public string PrintedBy { get; set; } = string.Empty;
        public bool IsVoided { get; set; }
        public string VoidReason { get; set; } = string.Empty;
        public DateTime? VoidedAtUtc { get; set; }
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public static class ReportTemplateBuilder
    {
        [Obsolete("Pass BusinessDetails explicitly when generating documents to prevent shared mutable state across tenants.")]
        public static BusinessDetails CurrentBusiness { get; set; } = new();

        public static string FormatReceiptReference(int paymentId) => Payment.FormatReceiptReference(paymentId);
        public static string FormatUnpaddedReceiptReference(int paymentId) => Payment.FormatUnpaddedReceiptReference(paymentId);
        public static bool MatchesReceiptReference(int paymentId, string query) => Payment.MatchesReference(paymentId, query);

        public static string FormatUtcTimestamp(DateTime dt)
        {
            var utc = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
            return $"{utc:yyyy-MM-dd HH:mm} UTC";
        }

        private static string RenderBrandSub(BusinessDetails business)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(business.Tagline))
            {
                sb.Append(WebUtility.HtmlEncode(business.Tagline.Trim()));
            }

            var contactParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(business.Address))
            {
                contactParts.Add(WebUtility.HtmlEncode(business.Address.Trim()));
            }
            if (!string.IsNullOrWhiteSpace(business.Contact))
            {
                contactParts.Add(WebUtility.HtmlEncode(business.Contact.Trim()));
            }

            if (contactParts.Count > 0)
            {
                if (sb.Length > 0) sb.Append("<br/>");
                sb.Append(string.Join(" &bull; ", contactParts));
            }
            return sb.ToString();
        }

        public static string BuildMechanicCopyHtml(MechanicWorkOrderData data, BusinessDetails? business = null)
        {
            business ??= new BusinessDetails();
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'/>");
            sb.AppendLine("<title>Mechanic's Job Order Copy - " + crm.domain.entities.RecordReference.JobOrder(data.JobOrderId) + "</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("  * { box-sizing: border-box; }");
            sb.AppendLine("  body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 24px; color: #1c2638; background-color: #fff; font-size: 13px; line-height: 1.4; }");
            sb.AppendLine("  .header { border-bottom: 2px solid #0078d7; padding-bottom: 12px; margin-bottom: 18px; display: table; width: 100%; }");
            sb.AppendLine("  .header-left { display: table-cell; vertical-align: top; width: 60%; }");
            sb.AppendLine("  .header-right { display: table-cell; vertical-align: top; text-align: right; width: 40%; }");
            sb.AppendLine("  .brand-title { font-size: 22px; font-weight: bold; color: #0078d7; margin: 0; }");
            sb.AppendLine("  .brand-sub { font-size: 11px; color: #555; margin-top: 2px; }");
            sb.AppendLine("  .doc-title { font-size: 16px; font-weight: bold; color: #222; margin: 0; text-transform: uppercase; letter-spacing: 0.5px; }");
            sb.AppendLine("  .doc-meta { font-size: 12px; color: #444; margin-top: 4px; }");
            sb.AppendLine("  .info-table { width: 100%; border-collapse: collapse; margin-bottom: 18px; page-break-inside: avoid; }");
            sb.AppendLine("  .info-table td { padding: 6px 10px; vertical-align: top; border: 1px solid #e1e4e8; }");
            sb.AppendLine("  .info-label { font-weight: bold; background-color: #f6f8fa; width: 18%; color: #333; }");
            sb.AppendLine("  .info-val { width: 32%; }");
            sb.AppendLine("  .section-title { font-size: 14px; font-weight: bold; background-color: #0078d7; color: white; padding: 6px 10px; margin-top: 14px; margin-bottom: 0; border-radius: 2px 2px 0 0; }");
            sb.AppendLine("  .task-table { width: 100%; border-collapse: collapse; margin-bottom: 18px; }");
            sb.AppendLine("  .task-table th { background-color: #f1f4f8; color: #24292e; font-weight: 600; text-align: left; padding: 8px 10px; border: 1px solid #d1d5da; font-size: 12px; }");
            sb.AppendLine("  .task-table td { padding: 8px 10px; border: 1px solid #e1e4e8; vertical-align: top; word-break: break-word; }");
            sb.AppendLine("  .task-table tr { page-break-inside: avoid; }");
            sb.AppendLine("  .task-table tr:nth-child(even) { background-color: #fbfcfd; }");
            sb.AppendLine("  .checkbox-box { display: inline-block; width: 14px; height: 14px; border: 1.5px solid #333; vertical-align: middle; margin-right: 4px; }");
            sb.AppendLine("  .notes-box { border: 1px dashed #abb2bf; padding: 10px; background-color: #fdfefe; margin-bottom: 18px; min-height: 40px; page-break-inside: avoid; word-break: break-word; }");
            sb.AppendLine("  .signatures-table { width: 100%; border-collapse: collapse; margin-top: 28px; page-break-inside: avoid; }");
            sb.AppendLine("  .signatures-table td { width: 50%; vertical-align: top; padding: 0 15px; }");
            sb.AppendLine("  .sig-line { border-top: 1px solid #333; margin-top: 45px; text-align: center; padding-top: 6px; font-size: 11px; }");
            sb.AppendLine("  .footer { border-top: 1px solid #e1e4e8; padding-top: 10px; margin-top: 24px; font-size: 11px; color: #777; text-align: center; page-break-inside: avoid; }");
            sb.AppendLine("  @media print {");
            sb.AppendLine("    @page { margin: 12mm; }");
            sb.AppendLine("    body { padding: 0; font-size: 12px; }");
            sb.AppendLine("    .section-title { -webkit-print-color-adjust: exact; print-color-adjust: exact; }");
            sb.AppendLine("    .signatures-table { page-break-inside: avoid; }");
            sb.AppendLine("    .task-table tr { page-break-inside: avoid; }");
            sb.AppendLine("  }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("  <div class='header-left'>");
            sb.AppendLine($"    <div class='brand-title'>{WebUtility.HtmlEncode(business.Name)}</div>");
            string subHtml = RenderBrandSub(business);
            if (!string.IsNullOrWhiteSpace(subHtml))
            {
                sb.AppendLine($"    <div class='brand-sub'>{subHtml}</div>");
            }
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class='header-right'>");
            sb.AppendLine("    <div class='doc-title'>Mechanic's Job Order Copy</div>");
            sb.AppendLine($"    <div class='doc-meta'><strong>Job Order:</strong> {crm.domain.entities.RecordReference.JobOrder(data.JobOrderId)} (JOB-{data.JobOrderId:D4})</div>");
            sb.AppendLine($"    <div class='doc-meta'><strong>Recorded Reference:</strong> {WebUtility.HtmlEncode(data.EstimateReference)}</div>");
            sb.AppendLine($"    <div class='doc-meta'><strong>Print Date:</strong> {FormatUtcTimestamp(data.PrintDate)}</div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</div>");

            // Vehicle and Customer Info (Contacts and financial details strictly excluded)
            sb.AppendLine("<table class='info-table'>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td class='info-label'>Customer:</td>");
            sb.AppendLine($"    <td class='info-val'><strong>{WebUtility.HtmlEncode(data.CustomerName)}</strong></td>");
            sb.AppendLine("    <td class='info-label'>Job Status:</td>");
            sb.AppendLine($"    <td class='info-val'><strong>{WebUtility.HtmlEncode(data.JobStatus)}</strong></td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td class='info-label'>Vehicle:</td>");
            sb.AppendLine($"    <td class='info-val'><strong>{WebUtility.HtmlEncode(data.VehicleDetails)}</strong></td>");
            sb.AppendLine("    <td class='info-label'>Assigned Mechanic:</td>");
            sb.AppendLine($"    <td class='info-val'><strong style='color:#0078d7;'>{WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(data.AssignedMechanic) ? "Unassigned" : data.AssignedMechanic)}</strong></td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("</table>");

            // Intake / Mechanic Instructions
            if (!string.IsNullOrWhiteSpace(data.IntakeNotes))
            {
                sb.AppendLine("<div class='section-title'>Intake &amp; Service Instructions</div>");
                sb.AppendLine($"<div class='notes-box'>{WebUtility.HtmlEncode(data.IntakeNotes).Replace("\n", "<br/>")}</div>");
            }

            // Tasks Table
            sb.AppendLine("<div class='section-title'>Requested Services &amp; Work Tasks</div>");
            sb.AppendLine("<table class='task-table'>");
            sb.AppendLine("  <thead>");
            sb.AppendLine("    <tr>");
            sb.AppendLine("      <th style='width: 34px; text-align: center;'>Done</th>");
            sb.AppendLine("      <th style='width: 65px;'>Task #</th>");
            sb.AppendLine("      <th style='width: 30%;'>Service / Task</th>");
            sb.AppendLine("      <th style='width: 20%;'>Assigned Tech</th>");
            sb.AppendLine("      <th style='width: 15%;'>Status</th>");
            sb.AppendLine("      <th>Mechanic Notes / Inspection Findings</th>");
            sb.AppendLine("    </tr>");
            sb.AppendLine("  </thead>");
            sb.AppendLine("  <tbody>");

            if (data.Tasks.Count == 0)
            {
                sb.AppendLine("    <tr><td colspan='6' style='text-align: center; color: #777; padding: 16px;'>No tasks requested for this job yet. Services must be assigned through Job Orders.</td></tr>");
            }
            else
            {
                foreach (var t in data.Tasks)
                {
                    bool isDone = string.Equals(t.Status, "Done", StringComparison.OrdinalIgnoreCase);
                    string checkHtml = isDone ? "<span style='font-weight:bold; color:#0078d7;'>&#10003;</span>" : "<span class='checkbox-box'></span>";
                    string statusDisp = WebUtility.HtmlEncode(t.Status);
                    if (!string.IsNullOrWhiteSpace(t.ReviewStatus) && t.ReviewStatus != "Not Submitted")
                    {
                        statusDisp += $" ({WebUtility.HtmlEncode(t.ReviewStatus)})";
                    }

                    sb.AppendLine("    <tr>");
                    sb.AppendLine($"      <td style='text-align: center; vertical-align: middle;'>{checkHtml}</td>");
                    sb.AppendLine($"      <td>#{t.TaskId}</td>");
                    sb.AppendLine($"      <td><strong>{WebUtility.HtmlEncode(t.ServiceName)}</strong></td>");
                    sb.AppendLine($"      <td>{WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(t.AssignedTechnician) ? "Unassigned" : t.AssignedTechnician)}</td>");
                    sb.AppendLine($"      <td>{statusDisp}</td>");
                    sb.AppendLine($"      <td>{WebUtility.HtmlEncode(t.Notes)}</td>");
                    sb.AppendLine("    </tr>");
                }
            }

            sb.AppendLine("  </tbody>");
            sb.AppendLine("</table>");

            // Signatures
            sb.AppendLine("<table class='signatures-table'>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td>");
            sb.AppendLine("      <div class='sig-line'><strong>Assigned Mechanic / Technician</strong><br/>Signature over Printed Name &amp; Date</div>");
            sb.AppendLine("    </td>");
            sb.AppendLine("    <td>");
            sb.AppendLine("      <div class='sig-line'><strong>Lead Mechanic / QA Inspector</strong><br/>Approval Signature &amp; Date</div>");
            sb.AppendLine("    </td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("</table>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine($"  {WebUtility.HtmlEncode(business.Name)} Workshop Management &bull; Work Order Copy &bull; Confirmed mechanical repair documentation.");
            sb.AppendLine("</div>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        public static string BuildReceiptHtml(PaymentReceiptData data, BusinessDetails? business = null) => BuildPaymentReceiptHtml(data, business);

        public static string BuildPaymentReceiptHtml(PaymentReceiptData data, BusinessDetails? business = null)
        {
            business ??= new BusinessDetails();
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'/>");
            sb.AppendLine("<title>Payment Receipt - " + WebUtility.HtmlEncode(data.ReceiptReference) + "</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("  * { box-sizing: border-box; }");
            sb.AppendLine("  body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 24px; color: #1c2638; background-color: #fff; font-size: 13px; line-height: 1.4; }");
            sb.AppendLine("  .header { border-bottom: 2px solid #28a745; padding-bottom: 12px; margin-bottom: 18px; display: table; width: 100%; }");
            sb.AppendLine("  .header.voided { border-bottom-color: #dc3545; }");
            sb.AppendLine("  .header-left { display: table-cell; vertical-align: top; width: 60%; }");
            sb.AppendLine("  .header-right { display: table-cell; vertical-align: top; text-align: right; width: 40%; }");
            sb.AppendLine("  .brand-title { font-size: 22px; font-weight: bold; color: #28a745; margin: 0; }");
            sb.AppendLine("  .brand-title.voided { color: #dc3545; }");
            sb.AppendLine("  .brand-sub { font-size: 11px; color: #555; margin-top: 2px; }");
            sb.AppendLine("  .doc-title { font-size: 16px; font-weight: bold; color: #222; margin: 0; text-transform: uppercase; letter-spacing: 0.5px; }");
            sb.AppendLine("  .doc-meta { font-size: 12px; color: #444; margin-top: 4px; }");
            sb.AppendLine("  .void-banner { background-color: #f8d7da; border: 2px solid #f5c6cb; color: #721c24; padding: 12px 16px; margin-bottom: 18px; text-align: center; font-weight: bold; font-size: 14px; border-radius: 4px; page-break-inside: avoid; }");
            sb.AppendLine("  .info-table { width: 100%; border-collapse: collapse; margin-bottom: 18px; page-break-inside: avoid; }");
            sb.AppendLine("  .info-table td { padding: 6px 10px; vertical-align: top; border: 1px solid #e1e4e8; }");
            sb.AppendLine("  .info-label { font-weight: bold; background-color: #f6f8fa; width: 22%; color: #333; }");
            sb.AppendLine("  .info-val { width: 28%; }");
            sb.AppendLine("  .section-title { font-size: 14px; font-weight: bold; background-color: #28a745; color: white; padding: 6px 10px; margin-top: 14px; margin-bottom: 0; border-radius: 2px 2px 0 0; }");
            sb.AppendLine("  .section-title.voided { background-color: #dc3545; }");
            sb.AppendLine("  .payment-highlight { background-color: #eaf7ed; border: 2px solid #28a745; padding: 14px 18px; margin-bottom: 18px; border-radius: 4px; display: table; width: 100%; page-break-inside: avoid; }");
            sb.AppendLine("  .payment-highlight.voided { background-color: #fdf2f2; border-color: #dc3545; }");
            sb.AppendLine("  .hl-cell { display: table-cell; vertical-align: middle; }");
            sb.AppendLine("  .hl-label { font-size: 12px; font-weight: 600; color: #555; text-transform: uppercase; }");
            sb.AppendLine("  .hl-amount { font-size: 24px; font-weight: bold; color: #28a745; }");
            sb.AppendLine("  .hl-amount.voided { color: #dc3545; text-decoration: line-through; }");
            sb.AppendLine("  .summary-table { width: 100%; border-collapse: collapse; margin-bottom: 18px; page-break-inside: avoid; }");
            sb.AppendLine("  .summary-table td { padding: 8px 12px; border: 1px solid #e1e4e8; }");
            sb.AppendLine("  .summary-label { font-weight: 600; width: 60%; }");
            sb.AppendLine("  .summary-val { text-align: right; width: 40%; font-family: 'Consolas', monospace; font-size: 13px; }");
            sb.AppendLine("  .disclaimer { font-size: 11px; color: #666; font-style: italic; border-top: 1px solid #e1e4e8; padding-top: 10px; margin-top: 20px; text-align: center; page-break-inside: avoid; }");
            sb.AppendLine("  .footer { border-top: 1px solid #e1e4e8; padding-top: 10px; margin-top: 16px; font-size: 11px; color: #777; text-align: center; page-break-inside: avoid; }");
            sb.AppendLine("  @media print {");
            sb.AppendLine("    @page { margin: 12mm; }");
            sb.AppendLine("    body { padding: 0; font-size: 12px; }");
            sb.AppendLine("    .section-title, .payment-highlight { -webkit-print-color-adjust: exact; print-color-adjust: exact; }");
            sb.AppendLine("    tr { page-break-inside: avoid; }");
            sb.AppendLine("  }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Void Banner if Voided
            if (data.IsVoided)
            {
                sb.AppendLine("<div class='void-banner'>");
                sb.AppendLine("  &#9888; VOIDED PAYMENT RECORD &#9888;<br/>");
                sb.AppendLine("  <span style='font-size: 12px; font-weight: normal;'>This payment has been voided and is excluded from current billing totals. Retained strictly for audit history.</span>");
                if (!string.IsNullOrWhiteSpace(data.VoidReason))
                {
                    sb.AppendLine($"<div style='margin-top: 6px; font-size: 12px;'><strong>Reason for Void:</strong> {WebUtility.HtmlEncode(data.VoidReason)}</div>");
                }
                if (data.VoidedAtUtc.HasValue)
                {
                    sb.AppendLine($"<div style='font-size: 11px; color: #721c24; margin-top: 2px;'>Voided at: {FormatUtcTimestamp(data.VoidedAtUtc.Value)}</div>");
                }
                sb.AppendLine("</div>");
            }

            // Header
            string headerClass = data.IsVoided ? "header voided" : "header";
            string brandClass = data.IsVoided ? "brand-title voided" : "brand-title";
            sb.AppendLine($"<div class='{headerClass}'>");
            sb.AppendLine("  <div class='header-left'>");
            sb.AppendLine($"    <div class='{brandClass}'>{WebUtility.HtmlEncode(business.Name)}</div>");
            string subHtml = RenderBrandSub(business);
            if (!string.IsNullOrWhiteSpace(subHtml))
            {
                sb.AppendLine($"    <div class='brand-sub'>{subHtml}</div>");
            }
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class='header-right'>");
            sb.AppendLine("    <div class='doc-title'>Payment Receipt</div>");
            sb.AppendLine($"    <div class='doc-meta'><strong>Receipt Reference:</strong> {WebUtility.HtmlEncode(data.ReceiptReference)}</div>");
            sb.AppendLine($"    <div class='doc-meta'><strong>Payment Date:</strong> {FormatUtcTimestamp(data.PaymentDate)}</div>");
            sb.AppendLine($"    <div class='doc-meta'><strong>Job Order:</strong> {crm.domain.entities.RecordReference.JobOrder(data.JobOrderId)} (JOB-{data.JobOrderId:D4})</div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</div>");

            // Customer and Job Details
            sb.AppendLine("<table class='info-table'>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td class='info-label'>Customer:</td>");
            sb.AppendLine($"    <td class='info-val'><strong>{WebUtility.HtmlEncode(data.CustomerName)}</strong></td>");
            sb.AppendLine("    <td class='info-label'>Payment Method:</td>");
            sb.AppendLine($"    <td class='info-val'><strong>{WebUtility.HtmlEncode(data.PaymentMethod)}</strong></td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td class='info-label'>Vehicle:</td>");
            sb.AppendLine($"    <td class='info-val'><strong>{WebUtility.HtmlEncode(data.VehicleDetails)}</strong></td>");
            sb.AppendLine("    <td class='info-label'>Payment Category:</td>");
            sb.AppendLine($"    <td class='info-val'><strong>{WebUtility.HtmlEncode(data.PaymentType)}</strong></td>");
            sb.AppendLine("  </tr>");

            // Cashier Attribution & Printed By
            string recordedText = string.IsNullOrWhiteSpace(data.RecordedBy) ? "Not recorded" : data.RecordedBy;
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td class='info-label'>Recorded By:</td>");
            sb.AppendLine($"    <td class='info-val'><strong>{WebUtility.HtmlEncode(recordedText)}</strong></td>");
            sb.AppendLine("    <td class='info-label'>Printed By:</td>");
            string printedText = string.IsNullOrWhiteSpace(data.PrintedBy) ? "System Viewer" : data.PrintedBy;
            sb.AppendLine($"    <td class='info-val'>{WebUtility.HtmlEncode(printedText)}</td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("</table>");

            // Highlight of Amount Received for this individual payment
            string hlClass = data.IsVoided ? "payment-highlight voided" : "payment-highlight";
            string amtClass = data.IsVoided ? "hl-amount voided" : "hl-amount";
            sb.AppendLine($"<div class='{hlClass}'>");
            sb.AppendLine("  <div class='hl-cell'>");
            sb.AppendLine("    <div class='hl-label'>Amount Received (This Payment)</div>");
            sb.AppendLine($"    <div class='{amtClass}'>PHP {data.AmountReceived:N2}</div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class='hl-cell' style='text-align: right;'>");
            sb.AppendLine($"    <div class='hl-label'>Receipt Status</div>");
            sb.AppendLine($"    <div style='font-size: 16px; font-weight: bold; color: {(data.IsVoided ? "#dc3545" : "#28a745")};'>{(data.IsVoided ? "VOIDED" : "VALID &amp; RECORDED")}</div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</div>");

            // Current Billing Summary
            string secClass = data.IsVoided ? "section-title voided" : "section-title";
            sb.AppendLine($"<div class='{secClass}'>Current Job Billing Summary (Calculated at {FormatUtcTimestamp(data.CalculatedAtUtc)})</div>");
            sb.AppendLine("<table class='summary-table'>");
            if (!string.IsNullOrWhiteSpace(data.HistoricalQualification))
                sb.AppendLine($"<tr><td colspan='2'>{WebUtility.HtmlEncode(data.HistoricalQualification)}</td></tr>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td class='summary-label'>Recorded Job Total:</td>");
            string estCostDisp = data.TotalEstimatedCost.HasValue
                ? "PHP " + data.TotalEstimatedCost.Value.ToString("N2", CultureInfo.InvariantCulture)
                : "Pending Approved Estimate";
            sb.AppendLine($"    <td class='summary-val'>{WebUtility.HtmlEncode(estCostDisp)}</td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td class='summary-label'>Total Valid Payments Received to Date:</td>");
            sb.AppendLine($"    <td class='summary-val' style='color: #28a745; font-weight: bold;'>PHP {data.TotalPaid:N2}</td>");
            sb.AppendLine("  </tr>");
            sb.AppendLine("  <tr>");
            sb.AppendLine("    <td class='summary-label'>Current Outstanding Balance Due:</td>");
            string balDisp = data.TotalEstimatedCost.HasValue && data.OutstandingBalance.HasValue
                ? "PHP " + data.OutstandingBalance.Value.ToString("N2", CultureInfo.InvariantCulture)
                : "Unknown until job total is recorded";
            string balColor = (data.OutstandingBalance ?? 0) > 0 ? "#b22222" : "#28a745";
            sb.AppendLine($"    <td class='summary-val' style='color: {balColor}; font-weight: bold;'>{WebUtility.HtmlEncode(balDisp)}</td>");
            sb.AppendLine("  </tr>");

            // Customer Credit
            if (data.TotalEstimatedCost.HasValue && data.CustomerCredit.HasValue && data.CustomerCredit.Value > 0)
            {
                sb.AppendLine("  <tr>");
                sb.AppendLine("    <td class='summary-label'>Customer Credit Available (Overpayment):</td>");
                sb.AppendLine($"    <td class='summary-val' style='color: #0078d7; font-weight: bold;'>PHP {data.CustomerCredit.Value:N2}</td>");
                sb.AppendLine("  </tr>");
            }
            sb.AppendLine("</table>");

            // Disclaimer
            sb.AppendLine("<div class='disclaimer'>");
            sb.AppendLine($"  This document serves as an official acknowledgment of payment receipt for workshop services rendered by {WebUtility.HtmlEncode(business.Name)}.<br/>");
            sb.AppendLine("  Please retain this receipt for warranty claims and vehicle service records. This document is not a tax invoice.");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine($"  Receipt #{WebUtility.HtmlEncode(data.ReceiptReference)} &bull; Reference &bull; Printed {FormatUtcTimestamp(DateTime.UtcNow)} &bull; {WebUtility.HtmlEncode(business.Name)} CRM");
            sb.AppendLine("</div>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        public static string BuildOperationalSummaryReportHtml(System.Text.Json.Nodes.JsonNode summary, BusinessDetails? business = null, string? printedBy = null)
        {
            business ??= new BusinessDetails();
            printedBy = string.IsNullOrWhiteSpace(printedBy) ? "Authorized Workshop Staff" : printedBy.Trim();
            var sb = new StringBuilder();

            var overview = summary["overview"];
            var execution = summary["workshopExecution"];
            var financial = summary["financialSummary"];
            var cs = summary["customerService"];

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'/>");
            sb.AppendLine("<title>Operational Executive Summary - " + WebUtility.HtmlEncode(business.Name) + "</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("  * { box-sizing: border-box; }");
            sb.AppendLine("  body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 24px; color: #1c2638; background-color: #fff; font-size: 13px; line-height: 1.4; }");
            sb.AppendLine("  .header { border-bottom: 2px solid #0078d7; padding-bottom: 12px; margin-bottom: 18px; display: table; width: 100%; }");
            sb.AppendLine("  .header-left { display: table-cell; vertical-align: top; width: 60%; }");
            sb.AppendLine("  .header-right { display: table-cell; vertical-align: top; text-align: right; width: 40%; }");
            sb.AppendLine("  .brand-title { font-size: 22px; font-weight: bold; color: #0078d7; margin: 0; }");
            sb.AppendLine("  .brand-sub { font-size: 11px; color: #555; margin-top: 2px; }");
            sb.AppendLine("  .doc-title { font-size: 16px; font-weight: bold; color: #222; margin: 0; text-transform: uppercase; letter-spacing: 0.5px; }");
            sb.AppendLine("  .doc-meta { font-size: 12px; color: #444; margin-top: 4px; }");
            sb.AppendLine("  .kpi-grid { display: table; width: 100%; border-collapse: separate; border-spacing: 12px; margin-bottom: 18px; }");
            sb.AppendLine("  .kpi-row { display: table-row; }");
            sb.AppendLine("  .kpi-card { display: table-cell; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 4px; padding: 12px 16px; text-align: center; vertical-align: middle; }");
            sb.AppendLine("  .kpi-val { font-size: 22px; font-weight: bold; color: #0f172a; margin-bottom: 4px; }");
            sb.AppendLine("  .kpi-label { font-size: 11px; font-weight: 600; text-transform: uppercase; color: #64748b; letter-spacing: 0.5px; }");
            sb.AppendLine("  .section-title { font-size: 14px; font-weight: bold; background-color: #0078d7; color: white; padding: 6px 10px; margin-top: 16px; margin-bottom: 0; border-radius: 2px 2px 0 0; }");
            sb.AppendLine("  .data-table { width: 100%; border-collapse: collapse; margin-bottom: 18px; }");
            sb.AppendLine("  .data-table th { background-color: #f1f4f8; color: #24292e; font-weight: 600; text-align: left; padding: 8px 10px; border: 1px solid #d1d5da; font-size: 12px; }");
            sb.AppendLine("  .data-table td { padding: 8px 10px; border: 1px solid #e1e4e8; vertical-align: top; }");
            sb.AppendLine("  .data-table tr:nth-child(even) { background-color: #fbfcfd; }");
            sb.AppendLine("  .footer { margin-top: 24px; padding-top: 10px; border-top: 1px solid #d1d5da; font-size: 11px; color: #586069; text-align: center; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("  <div class='header-left'>");
            sb.AppendLine($"    <div class='brand-title'>{WebUtility.HtmlEncode(business.Name)}</div>");
            sb.AppendLine($"    <div class='brand-sub'>{RenderBrandSub(business)}</div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("  <div class='header-right'>");
            sb.AppendLine("    <div class='doc-title'>Operational Executive Summary</div>");
            sb.AppendLine($"    <div class='doc-meta'>Generated: {FormatUtcTimestamp(DateTime.UtcNow)}<br/>Report By: {WebUtility.HtmlEncode(printedBy)}</div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</div>");

            // KPI Grid
            sb.AppendLine("<div class='kpi-grid'>");
            sb.AppendLine("  <div class='kpi-row'>");
            sb.AppendLine($"    <div class='kpi-card'><div class='kpi-val'>{overview?["totalJobOrders"]?.ToString() ?? "0"}</div><div class='kpi-label'>Total Job Orders</div></div>");
            sb.AppendLine($"    <div class='kpi-card'><div class='kpi-val' style='color:#0284c7;'>{overview?["activeJobOrders"]?.ToString() ?? "0"}</div><div class='kpi-label'>Active in Workshop</div></div>");
            sb.AppendLine($"    <div class='kpi-card'><div class='kpi-val' style='color:#16a34a;'>₱{financial?["netCollections"]?.GetValue<decimal>():N2}</div><div class='kpi-label'>Net Collections</div></div>");
            sb.AppendLine($"    <div class='kpi-card'><div class='kpi-val' style='color:#ea580c;'>₱{financial?["totalReceivables"]?.GetValue<decimal>():N2}</div><div class='kpi-label'>Outstanding Receivables</div></div>");
            sb.AppendLine($"    <div class='kpi-card'><div class='kpi-val' style='color:#8b5cf6;'>{cs?["averageCsatScore"]?.ToString() ?? "5.0"} ⭐</div><div class='kpi-label'>Average CSAT Index</div></div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</div>");

            // Stage Breakdown Table
            sb.AppendLine("<div class='section-title'>1. Workshop Lifecycle Stage Distribution</div>");
            sb.AppendLine("<table class='data-table'>");
            sb.AppendLine("  <tr><th>Stage</th><th>Active Jobs</th><th>Operational Notes</th></tr>");
            var jobsByStage = overview?["jobsByStage"]?.AsObject();
            if (jobsByStage != null)
            {
                foreach (var kvp in jobsByStage)
                {
                    sb.AppendLine($"  <tr><td><strong>{WebUtility.HtmlEncode(kvp.Key)}</strong></td><td>{kvp.Value?.ToString() ?? "0"}</td><td>Operational stage: {WebUtility.HtmlEncode(kvp.Key)}</td></tr>");
                }
            }
            sb.AppendLine("</table>");

            // Workshop Execution Table
            sb.AppendLine("<div class='section-title'>2. Workshop Execution & Technician Output</div>");
            sb.AppendLine("<table class='data-table'>");
            sb.AppendLine("  <tr><th>Metric</th><th>Value</th><th>Status</th></tr>");
            sb.AppendLine($"  <tr><td>Total Work Tasks</td><td>{execution?["totalTasks"]?.ToString() ?? "0"}</td><td>Tracked across active jobs</td></tr>");
            sb.AppendLine($"  <tr><td>Completed Tasks</td><td>{execution?["completedTasks"]?.ToString() ?? "0"}</td><td>Verified done by technician</td></tr>");
            sb.AppendLine($"  <tr><td>Task Completion Percentage</td><td><strong>{execution?["taskCompletionPercentage"]?.ToString() ?? "0"}%</strong></td><td>Overall throughput</td></tr>");
            sb.AppendLine($"  <tr><td>Average Turnaround Time</td><td>{execution?["averageTurnaroundHours"]?.ToString() ?? "0"} Hours</td><td>From job start to vehicle completion</td></tr>");
            sb.AppendLine("</table>");

            // Financial Breakdown Table
            sb.AppendLine("<div class='section-title'>3. Financial Summary & Collections</div>");
            sb.AppendLine("<table class='data-table'>");
            sb.AppendLine("  <tr><th>Financial Category</th><th>Amount</th><th>Accounting Notes</th></tr>");
            sb.AppendLine($"  <tr><td>Gross Collections (Valid Receipts)</td><td>PHP {financial?["grossCollections"]?.GetValue<decimal>():N2}</td><td>Excludes voided receipts</td></tr>");
            sb.AppendLine($"  <tr><td>Approved Refunds Disbursed</td><td style='color:#dc2626;'>- PHP {financial?["totalRefunds"]?.GetValue<decimal>():N2}</td><td>Subtracted from collections</td></tr>");
            sb.AppendLine($"  <tr><td><strong>Net Realized Collections</strong></td><td style='color:#16a34a; font-weight:bold;'>PHP {financial?["netCollections"]?.GetValue<decimal>():N2}</td><td>Net cash and electronic payments</td></tr>");
            sb.AppendLine($"  <tr><td>Outstanding Active Receivables</td><td style='color:#b91c1c; font-weight:bold;'>PHP {financial?["totalReceivables"]?.GetValue<decimal>():N2}</td><td>Collectible balances due upon vehicle release</td></tr>");
            sb.AppendLine("</table>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine($"  Confidential Operational Report &bull; {WebUtility.HtmlEncode(business.Name)} &bull; Printed {FormatUtcTimestamp(DateTime.UtcNow)} &bull; CRM Platform");
            sb.AppendLine("</div>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }
    }
}
