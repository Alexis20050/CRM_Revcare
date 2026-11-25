#nullable enable
using System;

namespace crm.domain.entities;

public class Refund
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int RefundId { get; set; }
    public int? CancellationRequestId { get; set; }
    public JobCancellationRequest? CancellationRequest { get; set; }

    public int JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }

    public int? PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public decimal Amount { get; set; }
    public string RefundMethod { get; set; } = "Cash"; // Cash, Card, GCash, Bank Transfer, Check
    public DateTime RefundDateUtc { get; set; } = DateTime.UtcNow;
    public string RecipientName { get; set; } = "";
    public string Reason { get; set; } = "";
    public string AuthorizedBy { get; set; } = "";
    public Guid SubmissionId { get; set; } = Guid.NewGuid();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string RefundReference => RefundId > 0 ? RecordReference.Refund(RefundId) : "Pending Save";
}
