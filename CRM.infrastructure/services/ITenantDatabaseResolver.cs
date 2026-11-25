namespace CRM.infrastructure.services;
public interface ITenantDatabaseResolver
{
    Task<TenantDatabaseInfo> GetDatabaseInfoAsync(string tenantId);
}