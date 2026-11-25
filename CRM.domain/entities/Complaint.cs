using System;

namespace crm.domain.entities;

public class Complaint
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public int ComplaintId { get; set; }
    public bool IsUrgent { get; set; }
    public bool UnderWarrantyService { get; set; }
    public int? SourceFeedbackId { get; set; }
    public Feedback? SourceFeedback { get; set; }
    public int? CustomerId { get; set; } // General complaints only; linked jobs provide their customer.
    public int? LegacyCustomerId { get; set; } // Old external reference, retained for review.
    public int? JobOrderId { get; set; } // Nullable if it's a general shop complaint
    public string ComplaintType { get; set; } = string.Empty; // Service Quality, Staff Behavior, Pricing
    public string Description { get; set; } = string.Empty;
    public string ResolutionStatus { get; set; } = "Open"; // Open, Investigating, Resolved
    public string ActionTaken { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
