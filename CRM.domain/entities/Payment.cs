namespace crm.domain.entities;

public class Payment
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public bool IsVoided { get; set; }
    public string VoidReason { get; set; } = "";
    public DateTime? VoidedAtUtc { get; set; }
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
    public int PaymentId { get; set; }
    public int? BranchId { get; set; } = 1;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string ReceiptReference => FormatReceiptReference(PaymentId);

    public static string FormatReceiptReference(int paymentId) => RecordReference.Receipt(paymentId);
    public static string FormatUnpaddedReceiptReference(int paymentId) => RecordReference.FormatUnpadded(RecordReference.ReceiptPrefix, paymentId);

    public static bool MatchesReference(int paymentId, string query) => RecordReference.Matches(RecordReference.ReceiptPrefix, paymentId, query);

    public bool AllowCustomerCredit { get; set; }
    public int JobOrderId { get; set; }
    public string PaymentType { get; set; } = "Retail Full";
    // A legacy label does not identify the remitter or payment stage. Existing
    // persisted classifications are retained; new receipts start explicitly unknown.
    public string PaymentPurpose { get; set; } = "Unknown";
    public string PayerType { get; set; } = "Unknown";
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
