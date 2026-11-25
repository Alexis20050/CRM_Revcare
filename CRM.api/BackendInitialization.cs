using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM.api;

public static class BackendInitialization
{
    private static readonly HashSet<string> ProtectedDatabases = new(StringComparer.OrdinalIgnoreCase)
    {
        "DB_MasterCrm",
        "RevCare_PlatformMaster",
        "RevCare_TenantA",
        "RevCare_TenantB",
        "RevCare_TenantC",
        "db81188",
        "db80735",
        "db81189",
        "db82362",
        "db82364"
    };

    private static void EnsureMutableDatabaseTarget(string connectionString, string label)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        if (ProtectedDatabases.Contains(builder.InitialCatalog))
            throw new InvalidOperationException($"Refusing initialization/provisioning mutation against protected database '{builder.InitialCatalog}' ({label}). Use a disposable database.");
    }

    public static async Task RunAsync(IServiceProvider services, IConfiguration configuration)
    {
        var masterConnection = configuration.GetConnectionString("PlatformMaster") ?? throw new InvalidOperationException("Configure PlatformMaster.");
        var tenantConnection = configuration.GetConnectionString("TenantA") ?? throw new InvalidOperationException("Configure TenantA.");
        EnsureMutableDatabaseTarget(masterConnection, "PlatformMaster");
        EnsureMutableDatabaseTarget(tenantConnection, "TenantA");
        TenantDbContextFactory.EnsureSeparate(masterConnection, tenantConnection);

        string adminName = configuration["Bootstrap:SuperAdminUsername"] ?? "superadmin";
        string ownerName = configuration["Bootstrap:OwnerUsername"] ?? "owner";
        if (string.Equals(adminName, ownerName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Use separate Super Admin and Owner accounts.");

        string adminPassword = configuration["Bootstrap:SuperAdminPassword"] ?? throw new InvalidOperationException("Set Bootstrap:SuperAdminPassword in secrets.");
        string ownerPassword = configuration["Bootstrap:OwnerPassword"] ?? throw new InvalidOperationException("Set Bootstrap:OwnerPassword in secrets.");

        using var scope = services.CreateScope();
        var master = scope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>();
        if (!string.Equals(master.Database.GetConnectionString(), masterConnection, StringComparison.Ordinal))
            throw new InvalidOperationException("Resolved master differs from initialization configuration. Refusing all mutations.");

        if (await master.Database.CanConnectAsync() && await master.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name IN ('JobOrders','Customers','Inquiries')").SingleAsync() > 0)
            throw new InvalidOperationException("The master target contains legacy workshop data. Use a new empty master database.");
        await master.Database.MigrateAsync();

        var tenantOpts = new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(tenantConnection);
        bool useBridge = await TenantCrmDbContextOptionsExtensions.ShouldUseLegacyCatalogBridgeAsync(tenantConnection, allowMissingDatabase: true);
        if (useBridge)
            tenantOpts.UseLegacyCatalogTables();
        await using var tenant = new TenantCrmDbContext(tenantOpts.Options);
        if (await tenant.Database.CanConnectAsync() && await tenant.Database.SqlQueryRaw<int>(
            useBridge
                ? "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name IN ('Tenants','Memberships','SubscriptionPlans')"
                : "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name IN ('AspNetUsers','Companies','Tenants')").SingleAsync() > 0)
            throw new InvalidOperationException("The Tenant A target contains identity or master tables. Use a separate tenant database.");
        if (!useBridge)
            await tenant.Database.MigrateAsync();

        var identity = await tenant.DatabaseIdentity.SingleOrDefaultAsync();
        if (identity != null && identity.TenantId != "tenant-a")
            throw new InvalidOperationException("The target database already belongs to another tenant.");
        if (identity == null) { tenant.DatabaseIdentity.Add(new TenantDatabaseIdentity()); await tenant.SaveChangesAsync(); }

        // Seed Tenants: A (Ready), B (Provisioning), C (Provisioning)
        var now = DateTime.UtcNow;
        var existingA = await master.Tenants.FindAsync("tenant-a");
        if (existingA == null)
        {
            master.Tenants.Add(new TenantRegistration
            {
                TenantId = "tenant-a",
                DisplayName = "RevCare Auto Hub",
                CompanyCode = "REVC-A",
                ConnectionKey = "TenantA",
                ServerName = @"(localdb)\MSSQLLocalDB",
                DatabaseName = "DB_MasterCrm",
                CurrentPlanId = "PlanA",
                Status = "Ready",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }
        else
        {
            existingA.Status = "Ready";
            existingA.CurrentPlanId = "PlanA";
            if (string.IsNullOrWhiteSpace(existingA.CompanyCode)) existingA.CompanyCode = "REVC-A";
            if (string.IsNullOrWhiteSpace(existingA.DatabaseName)) existingA.DatabaseName = "DB_MasterCrm";
            if (existingA.ConnectionKey != "TenantA")
                throw new InvalidOperationException("Tenant A has a different connection registration. Review it before initializing.");
        }

        if (await master.Tenants.FindAsync("tenant-b") == null)
        {
            master.Tenants.Add(new TenantRegistration
            {
                TenantId = "tenant-b",
                DisplayName = "RevCare Tenant B",
                CompanyCode = "REVC-B",
                ConnectionKey = "TenantB",
                ServerName = @"(localdb)\MSSQLLocalDB",
                DatabaseName = "RevCare_TenantB",
                CurrentPlanId = "PlanB",
                Status = "Provisioning",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        if (await master.Tenants.FindAsync("tenant-c") == null)
        {
            master.Tenants.Add(new TenantRegistration
            {
                TenantId = "tenant-c",
                DisplayName = "RevCare Tenant C",
                CompanyCode = "REVC-C",
                ConnectionKey = "TenantC",
                ServerName = @"(localdb)\MSSQLLocalDB",
                DatabaseName = "RevCare_TenantC",
                CurrentPlanId = "PlanC",
                Status = "Provisioning",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        // Seed Module Catalog
        var moduleCatalog = new List<ModuleDefinition>
        {
            new() { ModuleId = "Customers", DisplayName = "Customer & Vehicle Hub", Description = "Unified customer profiles, vehicle asset registry, and contact directories.", IsImplemented = true, RequiredDependencies = "" },
            new() { ModuleId = "Inquiries", DisplayName = "Leads & Inquiries", Description = "Omnichannel lead capture, inquiries intake, and job order conversion.", IsImplemented = true, RequiredDependencies = "" },
            new() { ModuleId = "Complaints", DisplayName = "Complaints & Warranty", Description = "Customer grievance resolution, warranty tracking, and corrective actions.", IsImplemented = true, RequiredDependencies = "" },
            new() { ModuleId = "Feedback", DisplayName = "Customer Feedback & CSAT", Description = "Customer satisfaction ratings, feedback, and automated complaint escalation.", IsImplemented = true, RequiredDependencies = "" },
            new() { ModuleId = "JobOrders", DisplayName = "Job Orders & Estimation", Description = "Work order authoring, status lifecycle, cost estimates, and vehicle intake.", IsImplemented = true, RequiredDependencies = "Customers" },
            new() { ModuleId = "JobTasks", DisplayName = "Workshop & Task Execution", Description = "Technician work assignments, task tracking, and QA inspection.", IsImplemented = true, RequiredDependencies = "JobOrders" },
            new() { ModuleId = "Payments", DisplayName = "Billing & Financial Ledger", Description = "Payment processing, customer billing, deposit enforcement, and receipts.", IsImplemented = true, RequiredDependencies = "JobOrders" },
            new() { ModuleId = "Services", DisplayName = "Service Catalog & Labor Rates", Description = "Shop service offerings, standard labor hours, and reference pricing.", IsImplemented = true, RequiredDependencies = "" },
            new() { ModuleId = "BusinessIntelligence", DisplayName = "Business Intelligence & Analytics", Description = "Workshop KPIs, graphical trends, retention metrics, and BI reporting (Coming Soon).", IsImplemented = false, RequiredDependencies = "" },
            new() { ModuleId = "Reports", DisplayName = "Analytical Reports & KPIs", Description = "Workshop KPI analytics, executive summaries, and reporting.", IsImplemented = true, RequiredDependencies = "" },
            new() { ModuleId = "BusinessActions", DisplayName = "Documented Business Actions", Description = "Documented BI-based corrective and operational actions.", IsImplemented = true, RequiredDependencies = "" },
            new() { ModuleId = "CustomerRetention", DisplayName = "Customer Retention", Description = "Customer retention metrics and repeat business tracking.", IsImplemented = true, RequiredDependencies = "" },
            new() { ModuleId = "Branching", DisplayName = "Multi-Branch Operations", Description = "Multi-branch operational coordination (Coming Soon).", IsImplemented = false, RequiredDependencies = "" },
            new() { ModuleId = "BranchSpecificAccess", DisplayName = "Branch-Specific Access", Description = "Branch user isolation and role enforcement.", IsImplemented = true, RequiredDependencies = "" },
            new() { ModuleId = "ConsolidatedAnalytics", DisplayName = "Consolidated Analytics", Description = "Consolidated multi-branch performance analytics.", IsImplemented = true, RequiredDependencies = "" }
        };

        foreach (var def in moduleCatalog)
        {
            var existingMod = await master.Modules.FindAsync(def.ModuleId);
            if (existingMod == null)
            {
                master.Modules.Add(def);
            }
            else
            {
                existingMod.DisplayName = def.DisplayName;
                existingMod.Description = def.Description;
                existingMod.IsImplemented = def.IsImplemented;
                existingMod.RequiredDependencies = def.RequiredDependencies;
            }
        }
        await master.SaveChangesAsync();

        // Seed initial entitlements for Tenant A (all implemented modules enabled)
        var implementedModules = moduleCatalog.Where(m => m.IsImplemented).Select(m => m.ModuleId).ToList();
        foreach (var modId in implementedModules)
        {
            if (!await master.ModuleEntitlements.AnyAsync(e => e.TenantId == "tenant-a" && e.ModuleId == modId))
            {
                master.ModuleEntitlements.Add(new TenantModuleEntitlement
                {
                    TenantId = "tenant-a",
                    ModuleId = modId,
                    IsEnabled = true,
                    Reason = "Initial provisioned modules",
                    UpdatedBy = "System",
                    UpdatedAtUtc = now
                });
            }
        }
        await master.SaveChangesAsync();

        // Seed Subscription Plans: Plan A, Plan B, Plan C
        var subscriptionPlans = new List<SubscriptionPlan>
        {
            new()
            {
                PlanId = "PlanA",
                PlanName = "Plan A - Transactions & Data Collection",
                Description = "Core workshop operations and lead intake data collection.",
                IncludedModules = "Customers,Inquiries,Complaints,Feedback,JobOrders,JobTasks,Payments,Services",
                AllowsBI = false,
                AllowsBranching = false,
                MaxBranches = 1,
                IsActive = true,
                CreatedAtUtc = now
            },
            new()
            {
                PlanId = "PlanB",
                PlanName = "Plan B - Professional BI & Analytics",
                Description = "All Plan A functionality plus Business Intelligence, KPIs, analytical reports, BI actions, and customer retention.",
                IncludedModules = "Customers,Inquiries,Complaints,Feedback,JobOrders,JobTasks,Payments,Services,BusinessIntelligence,Reports,BusinessActions,CustomerRetention",
                AllowsBI = true,
                AllowsBranching = false,
                MaxBranches = 1,
                IsActive = true,
                CreatedAtUtc = now
            },
            new()
            {
                PlanId = "PlanC",
                PlanName = "Plan C - Enterprise Multi-Branch & Analytics",
                Description = "All Plan B functionality plus Branch management, branch-specific access, and consolidated analytics.",
                IncludedModules = "Customers,Inquiries,Complaints,Feedback,JobOrders,JobTasks,Payments,Services,BusinessIntelligence,Reports,BusinessActions,CustomerRetention,Branching,BranchSpecificAccess,ConsolidatedAnalytics",
                AllowsBI = true,
                AllowsBranching = true,
                MaxBranches = 999,
                IsActive = true,
                CreatedAtUtc = now
            }
        };

        foreach (var p in subscriptionPlans)
        {
            var existingPlan = await master.SubscriptionPlans.FindAsync(p.PlanId);
            if (existingPlan == null)
            {
                master.SubscriptionPlans.Add(p);
            }
            else
            {
                existingPlan.PlanName = p.PlanName;
                existingPlan.Description = p.Description;
                existingPlan.IncludedModules = p.IncludedModules;
                existingPlan.AllowsBI = p.AllowsBI;
                existingPlan.AllowsBranching = p.AllowsBranching;
                existingPlan.MaxBranches = p.MaxBranches;
            }
        }
        await master.SaveChangesAsync();

        // Seed Tenant A active Plan A subscription
        var subA = await master.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == "tenant-a" && s.Status == "Active");
        if (subA == null)
        {
            master.Subscriptions.Add(new TenantSubscription
            {
                TenantId = "tenant-a",
                PlanId = "PlanA",
                Status = "Active",
                StartDateUtc = now,
                AutoRenew = true,
                UpdatedBy = "SystemProvisioning",
                UpdatedAtUtc = now
            });
            master.SubscriptionAudits.Add(new SubscriptionAuditEntry
            {
                TenantId = "tenant-a",
                PlanId = "PlanA",
                Actor = "SystemProvisioning",
                Action = "Created",
                Reason = "Initial provisioned Plan A subscription",
                OccurredAtUtc = now
            });
            await master.SaveChangesAsync();
        }

        // Seed Roles & Users
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("SuperAdmin")) Ensure(await roles.CreateAsync(new IdentityRole("SuperAdmin")));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        async Task<IdentityUser> CreateUser(string name, string password)
        {
            var existing = await users.FindByNameAsync(name);
            if (existing != null) return existing; // Never reset existing passwords on rerun.
            var user = new IdentityUser(name) { LockoutEnabled = true };
            Ensure(await users.CreateAsync(user, password));
            return user;
        }

        var admin = await CreateUser(adminName, adminPassword);
        if (!await users.IsInRoleAsync(admin, "SuperAdmin")) Ensure(await users.AddToRoleAsync(admin, "SuperAdmin"));

        var owner = await CreateUser(ownerName, ownerPassword);
        var ownerMembership = await master.Memberships.SingleOrDefaultAsync(m => m.UserId == owner.Id && m.TenantId == "tenant-a");
        if (ownerMembership == null)
        {
            master.Memberships.Add(new TenantMembership
            {
                UserId = owner.Id,
                TenantId = "tenant-a",
                Role = "OwnerManager",
                DisplayName = ownerName,
                ProfileProvisioningStatus = "Synced",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            await master.SaveChangesAsync();
        }

        if (!await tenant.Staff.AnyAsync(s => s.UserId == owner.Id))
            tenant.Staff.Add(new TenantStaff { UserId = owner.Id, DisplayName = ownerName });

        if (!await tenant.Services.AnyAsync())
            tenant.Services.AddRange(JobServices.Offered.Select(name => new ServiceOffering { Name = name }));

        if (!await tenant.Branches.AnyAsync())
        {
            tenant.Branches.Add(new TenantBranch
            {
                BranchCode = "MAIN",
                BranchName = "RevCare Auto Hub - Main Branch",
                Address = "Metro Manila, Philippines",
                ContactNumber = "0917-123-4567",
                IsActive = true,
                CreatedAtUtc = now
            });
        }

        await tenant.SaveChangesAsync();
        Console.WriteLine("Master, Tenant A, and module entitlements initialized. Existing passwords and business records were preserved.");
    }

    public static async Task<List<StaffProvisioningReport>> ProvisionTenantAStaffAsync(IServiceProvider services, IConfiguration configuration)
    {
        var masterConnection = configuration.GetConnectionString("PlatformMaster") ?? throw new InvalidOperationException("Configure PlatformMaster.");
        var tenantConnection = configuration.GetConnectionString("TenantA") ?? throw new InvalidOperationException("Configure TenantA.");
        EnsureMutableDatabaseTarget(masterConnection, "PlatformMaster");
        EnsureMutableDatabaseTarget(tenantConnection, "TenantA");
        TenantDbContextFactory.EnsureSeparate(masterConnection, tenantConnection);

        string initialPassword = configuration["StaffSeed:LaboratoryPassword"]
            ?? configuration["StaffSeed:InitialPassword"]
            ?? throw new InvalidOperationException("Set StaffSeed:LaboratoryPassword in secrets or configuration.");

        using var scope = services.CreateScope();
        var master = scope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

        var tenantOpts = new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(tenantConnection);
        if (await TenantCrmDbContextOptionsExtensions.ShouldUseLegacyCatalogBridgeAsync(tenantConnection))
            tenantOpts.UseLegacyCatalogTables();
        await using var tenant = new TenantCrmDbContext(tenantOpts.Options);

        var staffToProvision = new (string Username, string DisplayName, string Role)[]
        {
            ("advisor", "Service Advisor", "ServiceAdvisor"),
            ("leadmechanic", "Lead Mechanic", "LeadMechanic"),
            ("mechanic", "Automotive Mechanic", "AutomotiveMechanic"),
            ("detailer", "Detailing Technician", "DetailingTechnician")
        };

        var reports = new List<StaffProvisioningReport>();
        var now = DateTime.UtcNow;

        foreach (var (username, displayName, role) in staffToProvision)
        {
            var existingUser = await users.FindByNameAsync(username);
            IdentityUser user;
            bool wasCreated = false;

            if (existingUser != null)
            {
                user = existingUser;
            }
            else
            {
                user = new IdentityUser(username) { LockoutEnabled = true };
                Ensure(await users.CreateAsync(user, initialPassword));
                wasCreated = true;
            }

            var membership = await master.Memberships.SingleOrDefaultAsync(m => m.UserId == user.Id && m.TenantId == "tenant-a");
            if (membership == null)
            {
                membership = new TenantMembership
                {
                    UserId = user.Id,
                    TenantId = "tenant-a",
                    Role = role,
                    DisplayName = displayName,
                    ProfileProvisioningStatus = "Pending",
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                master.Memberships.Add(membership);
                master.StaffAccessAudits.Add(new StaffAccessAudit
                {
                    TenantId = "tenant-a",
                    UserId = user.Id,
                    Actor = "SystemProvisioning",
                    Change = $"Provisioned laboratory staff {displayName} ({role})",
                    OccurredAtUtc = now
                });
                await master.SaveChangesAsync();
            }
            else
            {
                if (membership.Role != role)
                {
                    // Existing user has different role - do not silently change
                    reports.Add(new StaffProvisioningReport(username, displayName, membership.Role, "tenant-a", membership.IsActive, false, membership.ProfileProvisioningStatus, $"Role mismatch: expected {role}, found {membership.Role}"));
                    continue;
                }
            }

            // Sync profile to Tenant A database
            try
            {
                var profile = await tenant.Staff.FindAsync(user.Id);
                if (profile == null)
                {
                    tenant.Staff.Add(new TenantStaff { UserId = user.Id, DisplayName = displayName });
                    await tenant.SaveChangesAsync();
                }
                else if (profile.DisplayName != displayName)
                {
                    profile.DisplayName = displayName;
                    await tenant.SaveChangesAsync();
                }

                membership.ProfileProvisioningStatus = "Synced";
                membership.ProvisioningFailureReason = null;
                membership.UpdatedAtUtc = DateTime.UtcNow;
                await master.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                membership.ProfileProvisioningStatus = "Pending";
                membership.ProvisioningFailureReason = "Tenant DB profile sync failed: " + ex.Message;
                membership.UpdatedAtUtc = DateTime.UtcNow;
                await master.SaveChangesAsync();
            }

            reports.Add(new StaffProvisioningReport(
                username,
                displayName,
                membership.Role,
                "tenant-a",
                membership.IsActive,
                wasCreated,
                membership.ProfileProvisioningStatus,
                membership.ProvisioningFailureReason
            ));
        }

        return reports;
    }

    public static async Task ProvisionAllLocalDatabasesAsync(IServiceProvider services, IConfiguration configuration)
    {
        // 1. Baseline initialization: Master & Tenant A
        await RunAsync(services, configuration);

        // 2. Provision Tenant B (Plan B - BI, Reports, Actions, Customer Retention)
        string ownerBPassword = configuration["Bootstrap:OwnerBPassword"]
            ?? configuration["Bootstrap:OwnerPassword"]
            ?? "LocalDev!OwnerB2026";
        await ProvisionTenantDatabaseAsync(
            services,
            configuration,
            tenantId: "tenant-b",
            displayName: "RevCare Tenant B",
            companyCode: "REVC-B",
            connectionKey: "TenantB",
            planId: "PlanB",
            ownerUsername: "owner_b",
            ownerPassword: ownerBPassword,
            customBranches: null, // Single default branch
            makeReady: true
        );

        // 3. Provision Tenant C (Plan C - Multi-Branch Operations & Consolidated Analytics)
        string ownerCPassword = configuration["Bootstrap:OwnerCPassword"]
            ?? configuration["Bootstrap:OwnerPassword"]
            ?? "LocalDev!OwnerC2026";
        var tenantCBranches = new List<TenantBranch>
        {
            new() { BranchCode = "MAIN", BranchName = "RevCare C - Head Office", Address = "100 Central Ave, Quezon City", ContactNumber = "0918-111-0001", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { BranchCode = "NORTH", BranchName = "RevCare C - North Hub", Address = "250 North Blvd, Caloocan", ContactNumber = "0918-111-0002", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { BranchCode = "SOUTH", BranchName = "RevCare C - South Express", Address = "75 South Expressway, Alabang", ContactNumber = "0918-111-0003", IsActive = true, CreatedAtUtc = DateTime.UtcNow }
        };

        await ProvisionTenantDatabaseAsync(
            services,
            configuration,
            tenantId: "tenant-c",
            displayName: "RevCare Tenant C",
            companyCode: "REVC-C",
            connectionKey: "TenantC",
            planId: "PlanC",
            ownerUsername: "owner_c",
            ownerPassword: ownerCPassword,
            customBranches: tenantCBranches, // Multiple branches in the single Tenant C database
            makeReady: true
        );

        Console.WriteLine("All local databases provisioned successfully.");
    }

    public static async Task ProvisionTenantDatabaseAsync(
        IServiceProvider services,
        IConfiguration configuration,
        string tenantId,
        string displayName,
        string companyCode,
        string connectionKey,
        string planId,
        string ownerUsername,
        string ownerPassword,
        List<TenantBranch>? customBranches = null,
        bool makeReady = true)
    {
        var masterConnection = configuration.GetConnectionString("PlatformMaster")
            ?? throw new InvalidOperationException("Configure PlatformMaster.");
        var tenantConnection = configuration.GetConnectionString(connectionKey)
            ?? throw new InvalidOperationException($"Configure connection string for '{connectionKey}'.");
        EnsureMutableDatabaseTarget(masterConnection, "PlatformMaster");
        EnsureMutableDatabaseTarget(tenantConnection, connectionKey);
        TenantDbContextFactory.EnsureSeparate(masterConnection, tenantConnection);

        using var scope = services.CreateScope();
        var master = scope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var now = DateTime.UtcNow;

        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(tenantConnection);
        string dbName = builder.InitialCatalog;
        string serverName = builder.DataSource;

        // 1. Master Tenant Registration
        var tenant = await master.Tenants.FindAsync(tenantId);
        if (tenant == null)
        {
            tenant = new TenantRegistration
            {
                TenantId = tenantId,
                DisplayName = displayName,
                CompanyCode = companyCode,
                ConnectionKey = connectionKey,
                ServerName = serverName,
                DatabaseName = dbName,
                CurrentPlanId = planId,
                Status = makeReady ? "Ready" : "Provisioning",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            master.Tenants.Add(tenant);
        }
        else
        {
            tenant.DisplayName = displayName;
            tenant.CompanyCode = companyCode;
            tenant.ConnectionKey = connectionKey;
            tenant.ServerName = serverName;
            tenant.DatabaseName = dbName;
            tenant.CurrentPlanId = planId;
            if (makeReady) tenant.Status = "Ready";
            tenant.IsActive = true;
            tenant.UpdatedAtUtc = now;
        }

        // 2. Subscription lifecycle
        var sub = await master.Subscriptions.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Status == "Active");
        if (sub == null)
        {
            master.Subscriptions.Add(new TenantSubscription
            {
                TenantId = tenantId,
                PlanId = planId,
                Status = "Active",
                StartDateUtc = now,
                AutoRenew = true,
                UpdatedBy = "SystemProvisioning",
                UpdatedAtUtc = now
            });
            master.SubscriptionAudits.Add(new SubscriptionAuditEntry
            {
                TenantId = tenantId,
                PlanId = planId,
                Actor = "SystemProvisioning",
                Action = "Created",
                Reason = $"Provisioned subscription for {planId}",
                OccurredAtUtc = now
            });
        }
        else if (sub.PlanId != planId)
        {
            sub.PlanId = planId;
            sub.UpdatedAtUtc = now;
            master.SubscriptionAudits.Add(new SubscriptionAuditEntry
            {
                TenantId = tenantId,
                PlanId = planId,
                Actor = "SystemProvisioning",
                Action = "Upgraded",
                Reason = $"Updated subscription plan to {planId}",
                OccurredAtUtc = now
            });
        }
        await master.SaveChangesAsync();

        // 3. Module entitlements configuration
        var plan = await master.SubscriptionPlans.FindAsync(planId);
        var modulesToEnable = plan != null
            ? plan.IncludedModules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : new[] { "Customers", "Inquiries", "Complaints", "Feedback", "JobOrders", "JobTasks", "Payments", "Services" };

        var validModules = await master.Modules.Select(m => m.ModuleId).ToListAsync();
        foreach (var modId in modulesToEnable.Where(m => validModules.Contains(m)))
        {
            var existingEnt = await master.ModuleEntitlements.SingleOrDefaultAsync(e => e.TenantId == tenantId && e.ModuleId == modId);
            if (existingEnt == null)
            {
                master.ModuleEntitlements.Add(new TenantModuleEntitlement
                {
                    TenantId = tenantId,
                    ModuleId = modId,
                    IsEnabled = true,
                    Reason = $"Entitled per {planId}",
                    UpdatedBy = "SystemProvisioning",
                    UpdatedAtUtc = now
                });
            }
            else
            {
                existingEnt.IsEnabled = true;
                existingEnt.UpdatedAtUtc = now;
            }
        }
        await master.SaveChangesAsync();

        // 4. Owner user & membership
        var existingUser = await users.FindByNameAsync(ownerUsername);
        IdentityUser user;
        if (existingUser != null)
        {
            user = existingUser;
        }
        else
        {
            user = new IdentityUser(ownerUsername) { LockoutEnabled = true };
            Ensure(await users.CreateAsync(user, ownerPassword));
        }

        var membership = await master.Memberships.SingleOrDefaultAsync(m => m.UserId == user.Id && m.TenantId == tenantId);
        if (membership == null)
        {
            master.Memberships.Add(new TenantMembership
            {
                UserId = user.Id,
                TenantId = tenantId,
                Role = "OwnerManager",
                DisplayName = ownerUsername,
                ProfileProvisioningStatus = "Synced",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
            await master.SaveChangesAsync();
        }

        // 5. Tenant Database Migration and Setup
        await using var tenantDb = new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(tenantConnection, sql => sql.EnableRetryOnFailure(3)).Options);
        await tenantDb.Database.MigrateAsync();

        var identity = await tenantDb.DatabaseIdentity.SingleOrDefaultAsync();
        if (identity == null)
        {
            tenantDb.DatabaseIdentity.Add(new TenantDatabaseIdentity { Id = 1, TenantId = tenantId });
            await tenantDb.SaveChangesAsync();
        }
        else if (identity.TenantId != tenantId)
        {
            throw new InvalidOperationException($"Database identity mismatch: database has '{identity.TenantId}', expected '{tenantId}'.");
        }

        if (!await tenantDb.Staff.AnyAsync(s => s.UserId == user.Id))
        {
            tenantDb.Staff.Add(new TenantStaff { UserId = user.Id, DisplayName = ownerUsername });
            await tenantDb.SaveChangesAsync();
        }

        if (!await tenantDb.Services.AnyAsync())
        {
            tenantDb.Services.AddRange(JobServices.Offered.Select(name => new ServiceOffering { Name = name }));
            await tenantDb.SaveChangesAsync();
        }

        if (customBranches != null && customBranches.Count > 0)
        {
            foreach (var cb in customBranches)
            {
                var existing = await tenantDb.Branches.FirstOrDefaultAsync(b => b.BranchCode == cb.BranchCode);
                if (existing == null)
                {
                    tenantDb.Branches.Add(cb);
                }
                else
                {
                    existing.BranchName = cb.BranchName;
                    existing.Address = cb.Address;
                    existing.ContactNumber = cb.ContactNumber;
                    existing.IsActive = cb.IsActive;
                }
            }
            await tenantDb.SaveChangesAsync();
        }
        else if (!await tenantDb.Branches.AnyAsync())
        {
            tenantDb.Branches.Add(new TenantBranch
            {
                BranchCode = "MAIN",
                BranchName = $"{displayName} - Main Branch",
                Address = "Metro Manila, Philippines",
                ContactNumber = "0917-000-0000",
                IsActive = true,
                CreatedAtUtc = now
            });
            await tenantDb.SaveChangesAsync();
        }

        Console.WriteLine($"[PROVISION] Tenant '{tenantId}' ({displayName}) successfully provisioned with {planId} on database '{dbName}'.");
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}

public sealed record StaffProvisioningReport(
    string Username,
    string DisplayName,
    string Role,
    string TenantId,
    bool IsActive,
    bool WasCreated,
    string ProfileProvisioningStatus,
    string? FailureReason
);
