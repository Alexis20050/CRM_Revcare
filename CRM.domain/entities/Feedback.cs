using System;

namespace crm.domain.entities;

public class Feedback
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public int FeedbackId { get; set; }
    public int LegacyCustomerId { get; set; } // Archived external ID. Customer is now derived from JobOrderId.
    public int JobOrderId { get; set; }
    public int RatingScore { get; set; } // 1 to 5 Stars
    public string PositiveRemarks { get; set; } = string.Empty;
    public string AreasForImprovement { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
