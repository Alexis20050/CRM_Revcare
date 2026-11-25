#nullable enable
using System;
using System.Collections.Generic;

namespace crm.domain.entities;

public class JobCancellationRequest
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public int RequestId { get; set; }
    public int JobOrderId { get; set; }
    public JobOrder? JobOrder { get; set; }

    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public string RequestedBy { get; set; } = "";
    public string ReasonCategory { get; set; } = "Customer requested cancellation";
    public string ReasonExplanation { get; set; } = "";

    // Status: Pending Review, Approved, Pending Refund, Completed, Rejected, Withdrawn, Requires Information
    public string Status { get; set; } = "Pending Review";

    public decimal TotalPaidAtRequest { get; set; }
    public decimal IncurredCharges { get; set; } = 0m;
    public decimal ProposedRefund { get; set; } = 0m;
    public decimal? ApprovedRefund { get; set; }

    public string? ReviewJustification { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public List<Refund> Refunds { get; set; } = new();

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string RequestReference => RequestId > 0 ? RecordReference.CancellationRequest(RequestId) : "Pending Save";
}
