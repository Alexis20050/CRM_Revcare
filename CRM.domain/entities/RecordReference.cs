#nullable enable
using System;

namespace crm.domain.entities
{
    /// <summary>
    /// Centralized record reference formatting and search matching for RevCare CRM entities.
    /// Standardizes on canonical six-digit references:
    /// CUST-, VEH-, INQ-, APT-, JO-, TASK-, EST-, RCPT-, SVC-, STF-, CMP-, FB-.
    /// </summary>
    public static class RecordReference
    {
        public const string CustomerPrefix = "CUST-";
        public const string VehiclePrefix = "VEH-";
        public const string InquiryPrefix = "INQ-";
        public const string AppointmentPrefix = "APT-";
        public const string JobOrderPrefix = "JO-";
        public const string JobTaskPrefix = "TASK-";
        public const string EstimatePrefix = "EST-";
        public const string ReceiptPrefix = "RCPT-";
        public const string ServicePrefix = "SVC-";
        public const string StaffPrefix = "STF-";
        public const string ComplaintPrefix = "CMP-";
        public const string FeedbackPrefix = "FB-";
        public const string RefundPrefix = "REF-";
        public const string CancellationRequestPrefix = "REQ-";

        public static string Format(string prefix, int id) =>
            id > 0 ? $"{prefix}{id:D6}" : "Pending Save";

        public static string FormatUnpadded(string prefix, int id) =>
            id > 0 ? $"{prefix}{id}" : "Pending Save";

        public static string Customer(int id) => Format(CustomerPrefix, id);
        public static string Vehicle(int id) => Format(VehiclePrefix, id);
        public static string Inquiry(int id) => Format(InquiryPrefix, id);
        public static string Appointment(int id) => Format(AppointmentPrefix, id);
        public static string JobOrder(int id) => Format(JobOrderPrefix, id);
        public static string JobTask(int id) => Format(JobTaskPrefix, id);
        public static string Estimate(int id) => Format(EstimatePrefix, id);
        public static string Receipt(int id) => Format(ReceiptPrefix, id);
        public static string Service(int id) => Format(ServicePrefix, id);
        public static string Staff(int id) => Format(StaffPrefix, id);
        public static string Complaint(int id) => Format(ComplaintPrefix, id);
        public static string Feedback(int id) => Format(FeedbackPrefix, id);
        public static string Refund(int id) => Format(RefundPrefix, id);
        public static string CancellationRequest(int id) => Format(CancellationRequestPrefix, id);

        public static string FormatForRecord(string recordName, int id)
        {
            if (id <= 0) return "Pending Save";
            return recordName.Trim().ToLowerInvariant() switch
            {
                "customer" => Customer(id),
                "vehicle" => Vehicle(id),
                "inquiry" => Inquiry(id),
                "appointment" => Appointment(id),
                "job order" or "joborder" or "job" => JobOrder(id),
                "job task" or "jobtask" or "task" => JobTask(id),
                "estimate" => Estimate(id),
                "payment" or "receipt" => Receipt(id),
                "service" => Service(id),
                "staff" or "staff member" => Staff(id),
                "complaint" => Complaint(id),
                "feedback" => Feedback(id),
                "refund" => Refund(id),
                "cancellation request" or "cancellationrequest" or "cancellation" => CancellationRequest(id),
                _ => $"{recordName} #{id}"
            };
        }

        public static bool Matches(string prefix, int id, string query)
        {
            if (string.IsNullOrWhiteSpace(query) || id <= 0) return false;
            query = query.Trim();
            string idStr = id.ToString();
            string paddedRef = Format(prefix, id);
            string unpaddedRef = FormatUnpadded(prefix, id);

            if (query.Equals(idStr, StringComparison.OrdinalIgnoreCase)
                || idStr.Contains(query, StringComparison.OrdinalIgnoreCase)
                || paddedRef.Contains(query, StringComparison.OrdinalIgnoreCase)
                || unpaddedRef.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Also support normalized query formats such as "JO 25", "JO#25", "Job 25", "Job #25", "CUST 1"
            string normalized = query.Replace(" ", "-").Replace("#", "");
            if (paddedRef.Contains(normalized, StringComparison.OrdinalIgnoreCase)
                || unpaddedRef.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
