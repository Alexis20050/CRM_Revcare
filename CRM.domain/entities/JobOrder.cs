namespace crm.domain.entities;

public class JobOrder
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public int JobOrderId { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string EstimateNumber => JobOrderId > 0 ? $"EST-{JobOrderId:D6}" : "Assigned on save";
    public decimal? TotalEstimatedCost { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string VehicleDetails { get; set; } = "";
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string VehicleDisplay
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(VehicleDetails)) return VehicleDetails;
            var parts = EstimateId.Split(" / ", StringSplitOptions.TrimEntries);
            return parts.Length == 3 && parts[0].StartsWith("EST-", StringComparison.OrdinalIgnoreCase)
                ? parts[2] + " (legacy reference)" : "Vehicle details missing";
        }
    }
    public string InquiryNotes { get; set; } = "";
    public List<Payment> Payments { get; set; } = new();
    public List<JobTask> Tasks { get; set; } = new();
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal TotalPaid => Payments.Where(p => !p.IsVoided).Sum(p => p.AmountPaid);
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal? OutstandingBalance => TotalEstimatedCost is decimal cost ? Math.Max(0, cost - TotalPaid) : null;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal? CustomerCredit => TotalEstimatedCost is decimal cost ? Math.Max(0, TotalPaid - cost) : null;
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public double? TurnaroundHours => CompletedAt.HasValue ? (CompletedAt.Value - StartedAt).TotalHours : null;
    public int? CustomerId { get; set; } // Nullable only to preserve unreviewed legacy jobs.
    public int? BranchId { get; set; } = 1;
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public int? ApprovedEstimateId { get; set; }
    public Estimate? ApprovedEstimate { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public string? CancelledBy { get; set; }
    public string EstimateId { get; set; } = "";
    public string Status { get; set; } = "In Queue";
    public string? AssignedMechanicUserId { get; set; }
    public string? AssignedMechanic { get; set; }
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string AssignedMechanicDisplay => string.IsNullOrWhiteSpace(AssignedMechanic) ? "Unassigned" : AssignedMechanic;
    public DateTime StartedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAtUtc { get; set; }
    public string? ArchivedBy { get; set; }
    public string? ArchiveReason { get; set; }
}
