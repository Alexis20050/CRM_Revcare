namespace crm.domain.entities;

public class JobTask
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public int JobTaskId { get; set; }
    public int JobOrderId { get; set; }
    public int? ServiceCatalogId { get; set; }
    public WorkshopService? ServiceCatalog { get; set; }
    public decimal? AgreedPrice { get; set; }
    public string? ServiceNameSnapshot { get; set; }
    public string ServiceId { get; set; } = "";
    public string Remarks { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAtUtc { get; set; }
    public string? ArchivedBy { get; set; }
    public string? ArchiveReason { get; set; }
}
