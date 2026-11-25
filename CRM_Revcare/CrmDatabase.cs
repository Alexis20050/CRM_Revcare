#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CRM.infrastructure.data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public enum CrmEnvironment
    {
        Local,
        Cloud,
        Custom
    }

    public sealed record CrmDatabaseInfo(
        CrmEnvironment Environment,
        string EnvironmentDisplayName,
        string ServerDisplayName,
        string DatabaseName,
        string ConnectionString
    );

    public static class CrmDatabase
    {
        private static CrmDatabaseInfo? _cachedInfo;
        private static readonly object _syncLock = new object();

        public static CrmDatabaseInfo ResolveTarget(
            string[]? args = null,
            string? envVarOverride = null,
            string? environmentOverride = null)
        {
            var cmdArgs = args ?? Environment.GetCommandLineArgs();
            bool forceLocal = Array.Exists(cmdArgs, a => string.Equals(a, "--local", StringComparison.OrdinalIgnoreCase));
            bool forceCloud = Array.Exists(cmdArgs, a => string.Equals(a, "--cloud", StringComparison.OrdinalIgnoreCase));

            if (forceLocal && forceCloud)
            {
                throw new InvalidOperationException(
                    "Startup configuration conflict: Cannot specify both '--local' and '--cloud' command-line switches simultaneously. Specify either '--local' or '--cloud'.");
            }

            var envAppMode = environmentOverride
                ?? Environment.GetEnvironmentVariable("REVCARE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            bool isProduction = string.Equals(envAppMode, "Production", StringComparison.OrdinalIgnoreCase);

            string? connectionString = null;
            CrmEnvironment envMode = CrmEnvironment.Local;

            var settingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RevCareAutoHub", "master-connection.bin");

            // 1. Explicit --cloud switch: NEVER silently falls back to local.
            if (forceCloud)
            {
                if (!File.Exists(settingsPath))
                {
                    throw new InvalidOperationException(
                        $"Cloud configuration error: Encrypted cloud configuration file was not found at '{settingsPath}'. Explicit '--cloud' requests will not silently fall back to local database.");
                }

                try
                {
                    var encrypted = File.ReadAllBytes(settingsPath);
                    connectionString = Encoding.UTF8.GetString(
                        ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "Cloud configuration error: Failed to decrypt cloud configuration. Explicit '--cloud' requests will not silently fall back to local database.", ex);
                }

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "Cloud configuration error: Decrypted cloud connection string is empty. Explicit '--cloud' requests will not silently fall back to local database.");
                }

                envMode = CrmEnvironment.Cloud;
            }
            // 2. Explicit --local switch: NEVER silently falls back to cloud.
            else if (forceLocal)
            {
                if (isProduction)
                {
                    throw new InvalidOperationException(
                        "Production safety error: Production environment cannot use developer LocalDB. Explicit '--local' flag is prohibited in Production.");
                }

                connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=DB_MasterCrm;Trusted_Connection=True;TrustServerCertificate=True";
                envMode = CrmEnvironment.Local;
            }
            // 3. Environment variable override
            else
            {
                var envConn = envVarOverride ?? Environment.GetEnvironmentVariable("REVCARE_MASTER_CONNECTION_STRING");
                if (!string.IsNullOrWhiteSpace(envConn))
                {
                    var testBuilder = new SqlConnectionStringBuilder(envConn);
                    if (isProduction && testBuilder.DataSource.Contains("(localdb)", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Production safety error: Production deployment cannot silently use developer LocalDB via REVCARE_MASTER_CONNECTION_STRING.");
                    }

                    connectionString = envConn;
                    envMode = testBuilder.DataSource.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
                        ? CrmEnvironment.Local
                        : CrmEnvironment.Custom;
                }
            }

            // 4. Default configuration when no switch or env var is present
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                if (isProduction)
                {
                    // Production deployment cannot silently use developer LocalDB.
                    if (File.Exists(settingsPath))
                    {
                        try
                        {
                            var encrypted = File.ReadAllBytes(settingsPath);
                            connectionString = Encoding.UTF8.GetString(
                                ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser));
                            envMode = CrmEnvironment.Cloud;
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException(
                                "Production configuration error: Failed to load cloud configuration for Production environment.", ex);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "Production safety error: Production deployment requires a valid cloud configuration or REVCARE_MASTER_CONNECTION_STRING. Silently falling back to developer LocalDB is prohibited.");
                    }
                }
                else
                {
                    // Standard local development launch
                    connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=DB_MasterCrm;Trusted_Connection=True;TrustServerCertificate=True";
                    envMode = CrmEnvironment.Local;
                }
            }

            // Validate and parse target server and database names safely without exposing credentials
            SqlConnectionStringBuilder builder;
            try
            {
                builder = new SqlConnectionStringBuilder(connectionString);
                if (!builder.ShouldSerialize("Connect Timeout")) builder.ConnectTimeout = 30;
                connectionString = builder.ConnectionString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Database configuration error: Connection string format is invalid or malformed.", ex);
            }

            string rawHost = builder.DataSource ?? "";
            string cleanServer = rawHost.Contains(',') ? rawHost.Split(',')[0].Trim() : rawHost.Trim();
            string cleanDb = builder.InitialCatalog ?? "";
            string envDisplayName = envMode switch
            {
                CrmEnvironment.Cloud => "CLOUD",
                CrmEnvironment.Local => "LOCAL",
                _ => "CUSTOM"
            };

            return new CrmDatabaseInfo(
                Environment: envMode,
                EnvironmentDisplayName: envDisplayName,
                ServerDisplayName: cleanServer,
                DatabaseName: cleanDb,
                ConnectionString: connectionString
            );
        }

        public static CrmDatabaseInfo GetActiveDatabaseInfo()
        {
            if (_cachedInfo == null)
            {
                lock (_syncLock)
                {
                    _cachedInfo ??= ResolveTarget();
                }
            }
            return _cachedInfo;
        }

        public static void ResetCache()
        {
            lock (_syncLock)
            {
                _cachedInfo = null;
            }
        }

        public static MasterCrmDbContext CreateContext()
        {
            var info = GetActiveDatabaseInfo();
            return CreateContextWithConnectionString(info.ConnectionString);
        }

        public static MasterCrmDbContext CreateContext(string[]? args)
        {
            var info = ResolveTarget(args);
            return CreateContextWithConnectionString(info.ConnectionString);
        }

        public static MasterCrmDbContext CreateContextWithConnectionString(string connectionString)
        {
            return new MasterCrmDbContext(new DbContextOptionsBuilder<MasterCrmDbContext>()
                .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, MasterCrmModelCacheKeyFactory>()
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)).Options)
            {
                AuditActor = "admin"
            };
        }
    }
}
