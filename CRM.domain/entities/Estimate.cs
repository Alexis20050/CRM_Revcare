namespace crm.domain.entities;

public class Estimate
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public int EstimateId { get; set; }
    public string EstimateNumber { get; set; } = "";
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Approved, Declined, Converted
    public decimal TotalAmount { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string Notes { get; set; } = "";
    public List<EstimateLine> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class EstimateLine
{
    public int EstimateLineId { get; set; }
    public int EstimateId { get; set; }
    public Estimate? Estimate { get; set; }
    public int? ServiceCatalogId { get; set; }
    public WorkshopService? ServiceCatalog { get; set; }
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
