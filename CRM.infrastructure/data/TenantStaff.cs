using System.ComponentModel.DataAnnotations;

namespace CRM.infrastructure.data;

public sealed class TenantStaff
{
    [MaxLength(450)] public string UserId { get; set; } = "";
    [MaxLength(150)] public string DisplayName { get; set; } = "";
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
public sealed class TaskAssignment
{
    public int JobTaskId { get; set; }
    public crm.domain.entities.JobTask? Task { get; set; }
    [MaxLength(450)] public string UserId { get; set; } = "";
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
public sealed class AssignmentHistory
{
    public int Id { get; set; }
    public int JobTaskId { get; set; }
    [MaxLength(450)] public string? PreviousUserId { get; set; }
    [MaxLength(450)] public string UserId { get; set; } = "";
    [MaxLength(450)] public string Actor { get; set; } = "";
    [MaxLength(500)] public string Reason { get; set; } = "";
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
public sealed class ServiceOffering
{
    public int Id { get; set; }
    [MaxLength(150)] public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
