using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public sealed class TenantDatabaseResolver(PlatformMasterDbContext master) : ITenantDatabaseResolver
{
    public async Task<TenantDatabaseInfo> GetDatabaseInfoAsync(string tenantId)
    {
        var tenant = await master.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.TenantId == tenantId)
            ?? throw new InvalidOperationException($"Tenant '{tenantId}' is not registered.");

        if (!tenant.IsActive)
            throw new InvalidOperationException($"Tenant '{tenant.DisplayName}' is deactivated.");

        if (string.Equals(tenant.Status, "Provisioning", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Tenant '{tenant.DisplayName}' is currently provisioning. Database setup is pending.");

        if (string.Equals(tenant.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Tenant '{tenant.DisplayName}' is suspended.");

        if (!string.Equals(tenant.Status, "Ready", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Tenant '{tenant.DisplayName}' is not ready for operational database access.");

        return new TenantDatabaseInfo { TenantId = tenant.TenantId, ConnectionKey = tenant.ConnectionKey };
    }
}
