using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace CRM.api;

// Design-time defaults point only to the NEW local databases, never to the legacy/cloud database.
public sealed class PlatformMasterDesignFactory : IDesignTimeDbContextFactory<PlatformMasterDbContext>
{
    public PlatformMasterDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<PlatformMasterDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__PlatformMaster") ??
            @"Server=(localdb)\MSSQLLocalDB;Database=RevCare_PlatformMaster;Trusted_Connection=True;TrustServerCertificate=True").Options);
}
public sealed class TenantDesignFactory : IDesignTimeDbContextFactory<TenantCrmDbContext>
{
    public TenantCrmDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<TenantCrmDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__TenantA") ??
            @"Server=(localdb)\MSSQLLocalDB;Database=RevCare_TenantA_Design;Trusted_Connection=True;TrustServerCertificate=True").Options);
}
public sealed class MasterCrmDesignFactory : IDesignTimeDbContextFactory<MasterCrmDbContext>
{
    public MasterCrmDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<MasterCrmDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("REVCARE_MASTER_CONNECTION_STRING") ??
            @"Server=(localdb)\MSSQLLocalDB;Database=RevCare_MasterCrm_Design;Trusted_Connection=True;TrustServerCertificate=True").Options);
}