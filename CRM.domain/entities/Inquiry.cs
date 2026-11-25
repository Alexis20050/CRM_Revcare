using System;

namespace crm.domain.entities;

public class Inquiry
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public int InquiryId { get; set; }
    public string VehicleDetails { get; set; } = "";
    public int? ConvertedJobOrderId { get; set; }
    public int? CustomerId { get; set; }
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string InquiryPlatform { get; set; } = string.Empty; // Messenger, Walk-in, Call
    public string MessageDetails { get; set; } = string.Empty;
    public string Status { get; set; } = "Unanswered"; // Unanswered, Responded, Converted
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
