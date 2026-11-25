namespace crm.domain.entities;

public class WorkshopStaff
{
    public string StaffId { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "Automotive Mechanic";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
