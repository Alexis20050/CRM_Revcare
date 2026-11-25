using System;

namespace crm.domain.entities;

public class Company
{
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;  // Unique short code, e.g. "REVC001"
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<CompanyDatabase> Databases { get; set; } = new List<CompanyDatabase>();
}
