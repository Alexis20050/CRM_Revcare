    namespace crm.domain.entities;
public class AuditEntry
{
    public long AuditEntryId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Actor { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string Action { get; set; } = "";
    public string Changes { get; set; } = "";
}
