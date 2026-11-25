using Microsoft.Data.SqlClient;

namespace CRM.api;

public static class DatabaseTargetRouting
{
    public static bool IsCloudEnvironment(string[] args, Func<string, string?> getEnv)
        => args.Contains("--cloud", StringComparer.OrdinalIgnoreCase)
           || string.Equals(getEnv("REVCARE_ENVIRONMENT"), "CLOUD", StringComparison.OrdinalIgnoreCase);

    public static string ValidateCloudConnection(string value, string expectedDatabase)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Cloud configuration for '{expectedDatabase}' cannot be empty.");

        var parsed = new SqlConnectionStringBuilder(value);
        if (!string.Equals(parsed.InitialCatalog, expectedDatabase, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(parsed.DataSource, expectedDatabase + ".public.databaseasp.net,1433", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Cloud configuration must use the approved public host and database '{expectedDatabase}.public.databaseasp.net,1433 / {expectedDatabase}'.");
        }

        parsed.Encrypt = true;
        parsed.TrustServerCertificate = false;
        return parsed.ConnectionString;
    }

    public static void ValidateLocalTarget(string? connStr, string key, bool allowPrototype)
    {
        if (string.IsNullOrWhiteSpace(connStr)) return;
        var parsed = new SqlConnectionStringBuilder(connStr);
        if ((parsed.DataSource ?? "").Contains("databaseasp.net", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Local mode cannot target cloud host for '{key}'. Pass --cloud explicitly for cloud operation.");

        if (!allowPrototype && string.Equals(key, "TenantA", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(parsed.InitialCatalog, "RevCare_TenantA", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(parsed.InitialCatalog, "db81189", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Prototype databases (RevCare_TenantA / db81189) cannot be used silently. Intended local Tenant A must target DB_MasterCrm or an explicit disposable catalog.");
        }
    }

    public static string ResolvePlatformMasterAndConfigure(
        IConfiguration configuration,
        string[] args,
        Func<string, string?>? getEnv = null)
    {
        getEnv ??= name => Environment.GetEnvironmentVariable(name)
            ?? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);

        bool isCloud = IsCloudEnvironment(args, getEnv);
        if (isCloud)
        {
            if (args.Any(a => a.Equals("--initialize", StringComparison.OrdinalIgnoreCase) || a.StartsWith("--provision", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Cloud bootstrap is prohibited. Use the reviewed cutover procedure.");

            string rawMaster = getEnv("REVCARE_CLOUD_PLATFORM_CONNECTION")
                ?? throw new InvalidOperationException("Cloud mode requires REVCARE_CLOUD_PLATFORM_CONNECTION to be configured.");
            string rawTenant = getEnv("REVCARE_CLOUD_TENANT_CONNECTION")
                ?? throw new InvalidOperationException("Cloud mode requires REVCARE_CLOUD_TENANT_CONNECTION. Local fallback is prohibited.");

            string masterConn = ValidateCloudConnection(rawMaster, "db81188");
            string tenantConn = ValidateCloudConnection(rawTenant, "db80735");

            configuration["ConnectionStrings:PlatformMaster"] = masterConn;
            configuration["ConnectionStrings:TenantA"] = tenantConn;
            configuration["ConnectionStrings:TenantB"] = null;
            configuration["ConnectionStrings:TenantC"] = null;
            return masterConn;
        }

        string localMaster = configuration.GetConnectionString("PlatformMaster")
            ?? throw new InvalidOperationException("Connection string 'PlatformMaster' not found in configuration.");
        bool allowPrototype = args.Contains("--allow-prototype", StringComparer.OrdinalIgnoreCase);
        ValidateLocalTarget(localMaster, "PlatformMaster", allowPrototype);
        ValidateLocalTarget(configuration.GetConnectionString("TenantA"), "TenantA", allowPrototype);
        return localMaster;
    }
}
