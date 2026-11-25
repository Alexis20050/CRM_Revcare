namespace CRM.infrastructure.services;
public sealed class TenantDatabaseInfo
{
    public string TenantId { get; init; } = "";
    public string ConnectionKey { get; init; } = "";
}