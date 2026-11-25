namespace crm.domain.entities;

public class Appointment
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public int AppointmentId { get; set; }
    public int? BranchId { get; set; } = 1;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public int? InquiryId { get; set; }
    public Inquiry? Inquiry { get; set; }
    public DateTime ScheduledStart { get; set; }
    public DateTime? ScheduledEnd { get; set; }
    public string RequestedServices { get; set; } = "";
    public string? ServiceAdvisorUserId { get; set; }
    public string Status { get; set; } = "Scheduled"; // Scheduled, Checked In, Cancelled, No Show
    public string? CancellationReason { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int? LinkedJobOrderId { get; set; }
    public JobOrder? LinkedJobOrder { get; set; }
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
