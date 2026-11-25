namespace crm.domain.entities;

public class Vehicle
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public int VehicleId { get; set; }
    public string PlateNumber { get; set; } = "";
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int? Year { get; set; }
    public string? Color { get; set; }
    public string? Vin { get; set; }
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string DisplayName => string.IsNullOrWhiteSpace(PlateNumber)
        ? $"{Year} {Make} {Model}".Trim()
        : $"{PlateNumber} - {Make} {Model}".Trim();
}
