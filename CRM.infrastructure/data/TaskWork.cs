using System.ComponentModel.DataAnnotations;
using crm.domain.entities;

namespace CRM.infrastructure.data;

public sealed class TaskWork
{
    public int JobTaskId { get; set; }
    public JobTask Task { get; set; } = null!;
    [MaxLength(150)] public string ServiceNameAtApproval { get; set; } = "";
    public decimal? ReferencePriceAtApproval { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    [MaxLength(30)] public string ReviewStatus { get; set; } = "Not Submitted";
    [MaxLength(450)] public string? SubmittedBy { get; set; }
    [MaxLength(450)] public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    [MaxLength(2000)] public string ReviewNotes { get; set; } = "";
}
public sealed class TaskReview
{
    public long Id { get; set; }
    public int JobTaskId { get; set; }
    [MaxLength(30)] public string Result { get; set; } = "";
    [MaxLength(450)] public string Actor { get; set; } = "";
    [MaxLength(2000)] public string Notes { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
