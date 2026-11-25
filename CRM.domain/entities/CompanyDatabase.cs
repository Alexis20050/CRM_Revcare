using System.ComponentModel.DataAnnotations.Schema;

namespace crm.domain.entities;

public class CompanyDatabase
{
    public int CompanyDatabaseId { get; set; }
    public int CompanyId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string CredentialKey { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Company Company { get; set; } = null!;
}
