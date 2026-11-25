using CRM.infrastructure.data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CRM.infrastructure.services;

public sealed class TenantDbContextFactory(ITenantDatabaseResolver resolver, IConfiguration configuration) : ITenantDbContextFactory
{
    public async Task<TenantCrmDbContext> CreateAsync(string tenantId)
    {
        var info = await resolver.GetDatabaseInfoAsync(tenantId);
        var connection = configuration.GetConnectionString(info.ConnectionKey)
            ?? throw new InvalidOperationException($"Tenant database connection '{info.ConnectionKey}' is not configured.");
        var master = configuration.GetConnectionString("PlatformMaster")
            ?? throw new InvalidOperationException("Master database connection is not configured.");
        EnsureSeparate(master, connection);
        var optionsBuilder = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(connection, sql => sql.EnableRetryOnFailure(3))
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantCrmModelCacheKeyFactory>();
        if (await TenantCrmDbContextOptionsExtensions.ShouldUseLegacyCatalogBridgeAsync(connection))
            optionsBuilder.UseLegacyCatalogTables();
        var db = new TenantCrmDbContext(optionsBuilder.Options);
        try
        {
            if (!await db.DatabaseIdentity.AsNoTracking().AnyAsync(x => x.Id == 1 && x.TenantId == info.TenantId))
                throw new InvalidOperationException("Tenant database identity does not match the authorized tenant.");
            // Identity alone does not prove the operational schema is compatible.
            // Resolve every mapped column before exposing a Tenant A workspace.
            if (string.Equals(info.TenantId, "tenant-a", StringComparison.OrdinalIgnoreCase))
            {
                await db.Database.OpenConnectionAsync();
                try
                {
                    foreach (var table in db.Model.GetRelationalModel().Tables)
                    {
                        static string Quote(string value) => "[" + value.Replace("]", "]]") + "]";
                        await using var command = db.Database.GetDbConnection().CreateCommand();
                        command.CommandText = "SELECT TOP (0) "
                            + string.Join(",", table.Columns.Select(c => Quote(c.Name)))
                            + " FROM " + Quote(table.Schema ?? "dbo") + "." + Quote(table.Name);
                        await using var reader = await command.ExecuteReaderAsync();
                    }
                }
                finally { await db.Database.CloseConnectionAsync(); }
            }
            return db;
        }
        catch { await db.DisposeAsync(); throw; }
    }

    public static void EnsureSeparate(string master, string tenant)
    {
        var a = new SqlConnectionStringBuilder(master);
        var b = new SqlConnectionStringBuilder(tenant);
        if (string.IsNullOrWhiteSpace(a.InitialCatalog) || string.IsNullOrWhiteSpace(b.InitialCatalog))
            throw new InvalidOperationException("Master and tenant must specify database names.");
        if (string.Equals(a.DataSource, b.DataSource, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.InitialCatalog, b.InitialCatalog, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Master and tenant must use separate databases.");
    }
}
