namespace crm.domain.entities;

public class WorkshopService
{
    public int ServiceId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal CurrentPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
