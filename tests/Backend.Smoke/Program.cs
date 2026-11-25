using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.api;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using crm.domain.entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

class BackendSmoke
{
    static int passed;
    static async Task Main(string[] args)
    {
        string suffix = Guid.NewGuid().ToString("N");
        string masterConnection = @"Server=(localdb)\MSSQLLocalDB;Database=RevCare_BackendMaster_" + suffix + ";Trusted_Connection=True;TrustServerCertificate=True";
        string tenantConnection = @"Server=(localdb)\MSSQLLocalDB;Database=RevCare_BackendTenant_" + suffix + ";Trusted_Connection=True;TrustServerCertificate=True";
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PlatformMaster"] = masterConnection, ["ConnectionStrings:TenantA"] = tenantConnection,
            ["ConnectionStrings:TenantB"] = "", ["ConnectionStrings:TenantC"] = "",
            ["Bootstrap:SuperAdminPassword"] = "Test-only!Master2026", ["Bootstrap:OwnerPassword"] = "Test-only!Owner2026",
            ["Tenants:tenant-a:Name"] = "Document Business A",
            ["Tenants:tenant-a:Address"] = "Address A",
            ["Tenants:document-test:Name"] = "Document Business B",
            ["Tenants:document-test:Address"] = "Address B",
            ["Logging:LogLevel:Default"] = "Warning"
        };
        await using var cleanupMaster = new PlatformMasterDbContext(new DbContextOptionsBuilder<PlatformMasterDbContext>().UseSqlServer(masterConnection).Options);
        await using var cleanupTenant = new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(tenantConnection).Options);
        try
        {
            using var factory = new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder =>
                builder.UseEnvironment("Development").ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings)));
            using (var safetyScope = factory.Services.CreateScope())
            {
                var actual = safetyScope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>().Database.GetConnectionString();
                if (actual != masterConnection || factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("TenantA") != tenantConnection)
                    throw new InvalidOperationException("Test isolation failed before initialization: refusing all database mutations.");
            }
            await BackendInitialization.RunAsync(factory.Services, factory.Services.GetRequiredService<IConfiguration>());
            await BackendInitialization.RunAsync(factory.Services, factory.Services.GetRequiredService<IConfiguration>());
            Check(!cleanupMaster.Model.GetEntityTypes().Any(e => e.ClrType == typeof(Customer)), "Master model has no business entities");
            Check(!cleanupTenant.Model.GetEntityTypes().Any(e => e.ClrType == typeof(IdentityUser)), "Tenant model has no user credentials");
            Check(!cleanupMaster.Database.HasPendingModelChanges() && !cleanupTenant.Database.HasPendingModelChanges(), "Both migration snapshots match their models");
            Check(await cleanupMaster.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = 'Customers'").SingleAsync() == 0,
                "Master SQL database contains no customer table");
            Check(await cleanupTenant.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name = 'AspNetUsers'").SingleAsync() == 0,
                "Tenant SQL database contains no identity table");
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            await Status(await client.GetAsync("/crm/customers"), HttpStatusCode.Unauthorized, "Anonymous business access denied");
            await Status(await client.GetAsync("/master/tenants"), HttpStatusCode.Unauthorized, "Anonymous master access denied");
            await Status(await client.PostAsJsonAsync("/auth/login", new { username = "owner", password = "wrong", tenantId = "tenant-a" }),
                HttpStatusCode.Unauthorized, "Wrong password denied");
            await Status(await client.PostAsJsonAsync("/auth/login", new { username = "owner", password = "Test-only!Owner2026", tenantId = "tenant-b" }),
                HttpStatusCode.Forbidden, "Unconfigured Tenant B denied");
            await Status(await client.PostAsJsonAsync("/auth/login", new { username = "owner", password = "Test-only!Owner2026", tenantId = (string?)null }),
                HttpStatusCode.Forbidden, "Owner cannot enter master administration");
            string ownerToken = await Login(client, "owner", "Test-only!Owner2026", "tenant-a");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
            await Status(await client.GetAsync("/master/tenants"), HttpStatusCode.Forbidden, "Tenant token cannot access master administration");
            var customer = await Read<Customer>(await client.PostAsJsonAsync("/crm/customers",
                new { fullName = "Tenant A customer", contactNumber = "09171234567", email = "" }));
            Check(await cleanupTenant.Customers.CountAsync() == 1, "Customer write reaches Tenant A");
            var createJob = new JobOrderWrite(customer.CustomerId, "Toyota / ABC123", 1000, DateTime.Now, "In Queue", "Backend test",
                JobServices.Offered.Take(2).ToArray(), []);
            var job = await Read<JobOrder>(await client.PostAsJsonAsync("/crm/job-orders", createJob));
            Check(await cleanupTenant.JobTasks.CountAsync(t => t.JobOrderId == job.JobOrderId) == 2, "Tenant job creates exactly two linked tasks");
            var offering = await cleanupTenant.Services.AsNoTracking().FirstAsync();
            await Read<ServiceOffering>(await client.PutAsJsonAsync($"/crm/services/{offering.Id}", new ServiceWrite(offering.Name, 999, true, offering.RowVersion)));
            Check(await cleanupTenant.TaskWork.AllAsync(w => w.ReferencePriceAtApproval == 0), "Catalog price changes preserve existing task price snapshots");
            await Status(await client.PutAsJsonAsync($"/crm/job-orders/{job.JobOrderId}",
                createJob with { Status = "In Progress", RowVersion = job.RowVersion }), HttpStatusCode.Conflict, "Tenant business rule blocks work without deposit");
            var paymentRequest = new PaymentWrite(job.JobOrderId, 500, "Downpayment", "Cash", false, Guid.NewGuid());
            var payment = await Read<Payment>(await client.PostAsJsonAsync("/crm/payments", paymentRequest));
            await Read<Payment>(await client.PostAsJsonAsync("/crm/payments", paymentRequest));
            Check(await cleanupTenant.Payments.CountAsync() == 1, "Payment retry is idempotent");
            await Status(await client.PutAsJsonAsync($"/crm/payments/{payment.PaymentId}",
                new PaymentCorrection(600, "Downpayment", "Cash", false, payment.RowVersion)),
                HttpStatusCode.Conflict, "Saved receipt correction requires void and replacement");
            await Status(await client.PostAsJsonAsync("/crm/payments", paymentRequest with { AmountPaid = 600 }),
                HttpStatusCode.Conflict, "Reused payment key with different amount rejected");
            await Status(await client.PutAsJsonAsync($"/crm/job-orders/{job.JobOrderId}",
                createJob with { Status = "In Progress", RowVersion = job.RowVersion }), HttpStatusCode.Conflict, "Unassigned job cannot start work");
            var initMech = await Read<JsonElement>(await client.PostAsJsonAsync("/crm/staff", new CreateStaff("initmech", "Test-only!Staff2026", "Initial Mechanic", "AutomotiveMechanic")));
            string initMechId = initMech.GetProperty("id").GetString()!;
            job = (await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{job.JobOrderId}"))!;
            await Read<JsonElement>(await client.PutAsJsonAsync($"/crm/job-orders/{job.JobOrderId}/assignment", new AssignJob(initMechId, "Initial assignment before start", job.RowVersion)));
            job = (await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{job.JobOrderId}"))!;
            job = await Read<JobOrder>(await client.PutAsJsonAsync($"/crm/job-orders/{job.JobOrderId}",
                createJob with { Status = "In Progress", RowVersion = job.RowVersion }));
            var tasks = await client.GetFromJsonAsync<List<JobTask>>($"/crm/job-tasks?jobOrderId={job.JobOrderId}") ?? [];
            var task = await Read<JobTask>(await client.PutAsJsonAsync($"/crm/job-tasks/{tasks[0].JobTaskId}",
                new TaskWrite("Ongoing", "Started", tasks[0].RowVersion)));
            await Status(await client.PutAsJsonAsync($"/crm/job-tasks/{task.JobTaskId}",
                new TaskWrite("Done", "Stale", tasks[0].RowVersion)), HttpStatusCode.Conflict, "Stale tenant edit rejected");
            await Read<Payment>(await client.PostAsJsonAsync($"/crm/payments/{payment.PaymentId}/void", new VoidPayment("Test correction", payment.RowVersion)));
            Check((await cleanupTenant.JobOrders.AsNoTracking().SingleAsync()).TotalPaid == 0, "Voided payment excluded from tenant balance");
            Check(await cleanupTenant.AuditEntries.AnyAsync(a => a.Actor != "Unattributed application"), "Tenant audit records signed-in actor");
            await RbacChecks(client, ownerToken, job, task, payment);
            using (var scope = factory.Services.CreateScope())
            {
                var master = scope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>();
                var user = await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByNameAsync("owner");
                var membership = await master.Memberships.SingleAsync(m => m.UserId == user!.Id);
                membership.IsActive = false; await master.SaveChangesAsync();
            }
            await Status(await client.GetAsync("/crm/customers"), HttpStatusCode.Forbidden, "Revoked membership blocks an existing token");
            using (var scope = factory.Services.CreateScope())
            {
                var master = scope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>();
                (await master.Memberships.SingleAsync(m => m.Role == "OwnerManager" && m.TenantId == "tenant-a")).IsActive = true;
                (await master.Tenants.SingleAsync(t => t.TenantId == "tenant-a")).IsActive = false;
                await master.SaveChangesAsync();
            }
            await Status(await client.GetAsync("/crm/customers"), HttpStatusCode.Forbidden, "Inactive tenant blocks an existing token");
            using (var scope = factory.Services.CreateScope())
            {
                var master = scope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>();
                (await master.Tenants.SingleAsync(t => t.TenantId == "tenant-a")).IsActive = true; await master.SaveChangesAsync();
            }
            var marker = await cleanupTenant.DatabaseIdentity.SingleAsync();
            marker.TenantId = "wrong-tenant"; await cleanupTenant.SaveChangesAsync();
            await Status(await client.GetAsync("/crm/customers"), HttpStatusCode.Conflict, "Wrong database tenant marker blocks routing");
            marker.TenantId = "tenant-a"; await cleanupTenant.SaveChangesAsync();
            string masterToken = await Login(client, "superadmin", "Test-only!Master2026", null);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);
            var tenantsList = await client.GetFromJsonAsync<JsonElement>("/master/tenants");
            Check(tenantsList.GetArrayLength() >= 3, "Super Admin can list all tenants (Tenant A, B, C)");
            Check(tenantsList.EnumerateArray().Any(t => t.GetProperty("tenantId").GetString() == "tenant-b" && t.GetProperty("status").GetString() == "Provisioning"),
                "Tenant B registered with Provisioning status");
            Check(tenantsList.EnumerateArray().Any(t => t.GetProperty("tenantId").GetString() == "tenant-c" && t.GetProperty("status").GetString() == "Provisioning"),
                "Tenant C registered with Provisioning status");

            // Super Admin registers Tenant D (Provisioning)
            var regD = await client.PostAsJsonAsync("/master/tenants", new { tenantId = "tenant-d", displayName = "Tenant D Auto", connectionKey = "TenantD" });
            Check(regD.StatusCode == HttpStatusCode.Created, "Super Admin can register new tenant in Provisioning state");

            // Health check without exposing secrets
            var healthB = await client.GetFromJsonAsync<JsonElement>("/master/tenants/tenant-b/health");
            Check(!healthB.GetProperty("isConfigured").GetBoolean() || !healthB.GetProperty("isReachable").GetBoolean(),
                "Health check safely reports unconfigured Tenant B database without exposing secrets");

            // Provision initial owner for Tenant B before database exists
            var provOwnerB = await client.PostAsJsonAsync("/master/tenants/tenant-b/owner",
                new { username = "ownerB", password = "Test-only!OwnerB2026", displayName = "Tenant B Owner" });
            Check(provOwnerB.StatusCode == HttpStatusCode.OK, "Initial owner can be provisioned for Tenant B before database exists");
            var provOwnerBJson = await Read<JsonElement>(provOwnerB);
            Check(provOwnerBJson.GetProperty("profileProvisioningStatus").GetString() == "Pending",
                "Tenant B owner profile provisioning is recorded as Pending in Master");

            // Login as Tenant B Owner
            string ownerBToken = await Login(client, "ownerB", "Test-only!OwnerB2026", "tenant-b");
            var clientB = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerBToken);

            // Operational business access to Tenant B is BLOCKED (never routed to Tenant A or fallback)
            await Status(await clientB.GetAsync("/crm/customers"), HttpStatusCode.Forbidden,
                "Operational database access is blocked for Provisioning Tenant B");

            // Tenant B Owner can manage staff in Master even before tenant database exists
            var createStaffB = await clientB.PostAsJsonAsync("/crm/staff",
                new CreateStaff("advisorB", "Test-only!AdvisorB2026", "Advisor B", "ServiceAdvisor"));
            Check(createStaffB.StatusCode == HttpStatusCode.Created, "Tenant B owner can provision staff in Master while database is pending");
            var staffBJson = await Read<JsonElement>(createStaffB);
            Check(staffBJson.GetProperty("profileProvisioningStatus").GetString() == "Pending",
                "Tenant B staff profile is recorded as Pending");

            // Retrying profile sync reports database pending safely
            var retryB = await clientB.PostAsync($"/crm/staff/{staffBJson.GetProperty("id").GetString()}/retry-profile", null);
            Check(retryB.StatusCode == HttpStatusCode.ServiceUnavailable, "Retrying profile sync for unconfigured tenant safely returns 503");

            // Module Catalog & Entitlements checks
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);
            var modules = await client.GetFromJsonAsync<JsonElement>("/master/modules");
            Check(modules.EnumerateArray().Any(m => m.GetProperty("moduleId").GetString() == "BusinessIntelligence" && !m.GetProperty("isImplemented").GetBoolean()),
                "BI module is cataloged but marked as unimplemented");

            // Cannot enable unimplemented module
            await Status(await client.PutAsJsonAsync("/master/tenants/tenant-a/modules/BusinessIntelligence",
                new { isEnabled = true, reason = "Test enabling BI" }), HttpStatusCode.BadRequest, "Enabling unimplemented BI module rejected");

            // Module disabling with reason
            var disableComplaints = await client.PutAsJsonAsync("/master/tenants/tenant-a/modules/Complaints",
                new { isEnabled = false, reason = "Maintenance on complaints system" });
            Check(disableComplaints.StatusCode == HttpStatusCode.OK, "Disabling Complaints module with mandatory reason succeeded");

            // Calling disabled Complaints as Tenant A owner is forbidden immediately
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
            await Status(await client.GetAsync("/crm/complaints"), HttpStatusCode.Forbidden,
                "Disabled Complaints module is immediately forbidden for Tenant A staff");

            // Dependency protection: cannot disable JobOrders while JobTasks and Payments are enabled
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);
            await Status(await client.PutAsJsonAsync("/master/tenants/tenant-a/modules/JobOrders",
                new { isEnabled = false, reason = "Disable JobOrders" }), HttpStatusCode.Conflict,
                "Disabling parent module blocked by active dependent modules (JobTasks, Payments)");

            // Verify Audit Log records changes
            var auditLog = await client.GetFromJsonAsync<JsonElement>("/master/audit?tenantId=tenant-a");
            Check(auditLog.EnumerateArray().Any(a => a.GetProperty("moduleId").GetString() == "Complaints" && a.GetProperty("action").GetString() == "Disabled"),
                "Module disablement is recorded in immutable audit log with reason");

            // Re-enable Complaints module
            await client.PutAsJsonAsync("/master/tenants/tenant-a/modules/Complaints",
                new { isEnabled = true, reason = "Maintenance finished" });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
            await Status(await client.GetAsync("/crm/complaints"), HttpStatusCode.OK,
                "Re-enabled Complaints module is immediately accessible");

            await Status(await client.GetAsync("/master/tenants"), HttpStatusCode.Forbidden, "Tenant token cannot access master administration");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);
            await Status(await client.GetAsync("/master/tenants"), HttpStatusCode.OK, "Super Admin can list tenant metadata");
            await Status(await client.GetAsync("/crm/customers"), HttpStatusCode.Forbidden, "Super Admin has no implicit customer-data access");
            await Status(await client.GetAsync("/crm/job-orders"), HttpStatusCode.Forbidden, "Super Admin cannot access tenant job orders");
            await Status(await client.GetAsync("/crm/payments"), HttpStatusCode.Forbidden, "Super Admin cannot access tenant payments");
            await Status(await client.GetAsync("/crm/workshop"), HttpStatusCode.Forbidden, "Super Admin cannot access tenant workshop tasks");
            await AssertReportsDeniedAsync(client, HttpStatusCode.Forbidden, "SuperAdmin");
            var anonClient = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
            await AssertReportsDeniedAsync(anonClient, HttpStatusCode.Unauthorized, "Unauthenticated");
            await Status(await client.PostAsJsonAsync("/company-databases", new { }), HttpStatusCode.NotFound, "Old public database-routing endpoint removed");
            try { TenantDbContextFactory.EnsureSeparate(masterConnection, masterConnection); throw new Exception("Same DB accepted"); }
            catch (InvalidOperationException) { Check(true, "Same master/tenant database configuration rejected"); }
            RoutingConfigurationChecks();
            if (!args.Contains("--tenant-a-only"))
                await MultiTenantPlanAndRoutingChecks(factory.Services, client, tenantConnection);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
            await Status(await client.DeleteAsync($"/crm/job-orders/{job.JobOrderId}"), HttpStatusCode.Conflict,
                "Owner cannot physically delete a legitimate Job Order");
            Check(await cleanupTenant.JobOrders.AsNoTracking().AnyAsync(j => j.JobOrderId == job.JobOrderId), "Rejected deletion preserves Job Order");
            await TenantAUseCasesChecks(factory.Services, client, ownerToken, customer.CustomerId, job.JobOrderId);
            await RunTwentyStepAcceptanceTest(factory, client, masterToken, ownerToken);
            await DocumentConfigurationChecks(factory.Services, client, tenantConnection, ownerToken, job.JobOrderId, payment.PaymentId);
            await cleanupTenant.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.JobOrders DROP COLUMN ArchiveReason");
            await Status(await client.PostAsJsonAsync("/auth/login", new { username = "owner", password = "Test-only!Owner2026", tenantId = "tenant-a" }),
                HttpStatusCode.ServiceUnavailable, "Missing operational column prevents Tenant A login");
            Console.WriteLine($"All {passed} backend checks passed.");
        }
        finally { await cleanupTenant.Database.EnsureDeletedAsync(); await cleanupMaster.Database.EnsureDeletedAsync(); }
    }

    static async Task MultiTenantPlanAndRoutingChecks(IServiceProvider services, HttpClient client, string tenantConnection)
    {
        // 1. Verify Master Subscription Plans
        var configuration = services.GetRequiredService<IConfiguration>();
        using (var scope = services.CreateScope())
        {
            var master = scope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>();
            var plans = await master.SubscriptionPlans.AsNoTracking().ToListAsync();
            Check(plans.Count >= 3, "Master database defines Plan A, Plan B, and Plan C");
            Check(plans.Any(p => p.PlanId == "PlanA" && !p.AllowsBI && !p.AllowsBranching && p.MaxBranches == 1), "Plan A restricts BI and branching");
            Check(plans.Any(p => p.PlanId == "PlanB" && p.AllowsBI && !p.AllowsBranching && p.MaxBranches == 1), "Plan B enables BI and restricts branching");
            Check(plans.Any(p => p.PlanId == "PlanC" && p.AllowsBI && p.AllowsBranching && p.MaxBranches > 1), "Plan C enables BI and multi-branch operations");

            var subA = await master.Subscriptions.AsNoTracking().FirstOrDefaultAsync(s => s.TenantId == "tenant-a");
            Check(subA != null && subA.PlanId == "PlanA" && subA.Status == "Active", "Tenant A is associated with active Plan A");
        }

        // 2. Tenant A (Plan A) Feature Restriction Checks
        string ownerAToken = await Login(client, "owner", "Test-only!Owner2026", "tenant-a");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAToken);
        await Status(await client.GetAsync("/crm/analytics"), HttpStatusCode.Forbidden, "Plan A cannot access restricted BI functionality");
        await Status(await client.GetAsync("/crm/branches"), HttpStatusCode.Forbidden, "Plan A cannot access restricted branch management");

        // 3. Provision Disposable Tenant B (Plan B)
        var builderB = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(tenantConnection);
        builderB.InitialCatalog += "_TestTenantB";
        string connB = builderB.ConnectionString;
        configuration["ConnectionStrings:TenantB"] = connB;

        await BackendInitialization.ProvisionTenantDatabaseAsync(
            services, configuration,
            tenantId: "tenant-b",
            displayName: "Test Tenant B Corp",
            companyCode: "REVC-B-TEST",
            connectionKey: "TenantB",
            planId: "PlanB",
            ownerUsername: "owner_b_test",
            ownerPassword: "Test-only!OwnerB2026",
            customBranches: null,
            makeReady: true
        );

        await using (var dbB = new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(connB).Options))
        {
            try
            {
                // Verify Tenant B login & token
                string ownerBToken = await Login(client, "owner_b_test", "Test-only!OwnerB2026", "tenant-b");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerBToken);

                // Verify /auth/me returns Plan B
                var meB = await Read<JsonElement>(await client.GetAsync("/auth/me"));
                Check(meB.GetProperty("subscriptionPlan").GetString() == "PlanB", "Tenant B user resolves Plan B subscription");

                // Plan B allows BI / Analytics
                var analyticsB = await Read<JsonElement>(await client.GetAsync("/crm/analytics"));
                Check(analyticsB.GetProperty("graphs").GetArrayLength() == 6, "Plan B receives six significant graphs in BI analytics");
                Check(analyticsB.TryGetProperty("kpis", out _), "Plan B receives operational KPIs in BI analytics");

                // Plan B restricts Branching
                await Status(await client.GetAsync("/crm/branches"), HttpStatusCode.Forbidden, "Plan B cannot access restricted branch management");

                // Tenant B writes customer
                var custB = await Read<Customer>(await client.PostAsJsonAsync("/crm/customers",
                    new { fullName = "Tenant B Customer", contactNumber = "09180000001", email = "tb@test.com" }));
                Check(await dbB.Customers.CountAsync() == 1, "Customer write reaches Tenant B database");

                // Tenant A token cannot access Tenant B customer data
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAToken);
                var customersFromA = await client.GetFromJsonAsync<List<Customer>>("/crm/customers");
                Check(customersFromA!.All(c => c.FullName != "Tenant B Customer"), "Tenant A cannot access Tenant B customer records");
            }
            finally { await dbB.Database.EnsureDeletedAsync(); }
        }

        // 4. Provision Disposable Tenant C (Plan C - Multi-Branch)
        var builderC = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(tenantConnection);
        builderC.InitialCatalog += "_TestTenantC";
        string connC = builderC.ConnectionString;
        configuration["ConnectionStrings:TenantC"] = connC;

        var testCBranches = new List<TenantBranch>
        {
            new() { BranchCode = "MAIN", BranchName = "Test C - HQ", Address = "HQ Address", ContactNumber = "09190000001", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { BranchCode = "NORTH", BranchName = "Test C - North Hub", Address = "North Address", ContactNumber = "09190000002", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { BranchCode = "SOUTH", BranchName = "Test C - South Express", Address = "South Address", ContactNumber = "09190000003", IsActive = true, CreatedAtUtc = DateTime.UtcNow }
        };

        await BackendInitialization.ProvisionTenantDatabaseAsync(
            services, configuration,
            tenantId: "tenant-c",
            displayName: "Test Tenant C Corp",
            companyCode: "REVC-C-TEST",
            connectionKey: "TenantC",
            planId: "PlanC",
            ownerUsername: "owner_c_test",
            ownerPassword: "Test-only!OwnerC2026",
            customBranches: testCBranches,
            makeReady: true
        );

        await using (var dbC = new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(connC).Options))
        {
            try
            {
                // Login as Tenant C owner
                string ownerCToken = await Login(client, "owner_c_test", "Test-only!OwnerC2026", "tenant-c");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerCToken);

                // Verify /auth/me returns Plan C
                var meC = await Read<JsonElement>(await client.GetAsync("/auth/me"));
                Check(meC.GetProperty("subscriptionPlan").GetString() == "PlanC", "Tenant C user resolves Plan C subscription");

                // Plan C allows BI
                await Status(await client.GetAsync("/crm/analytics"), HttpStatusCode.OK, "Plan C receives BI analytics access");

                // Plan C allows Branching & retrieves all 3 branches from single company database
                var branchesRes = await client.GetAsync("/crm/branches");
                Check(branchesRes.StatusCode == HttpStatusCode.OK, "Plan C receives branch management entitlement");
                var branches = await Read<JsonElement>(branchesRes);
                Check(branches.GetArrayLength() == 3, "Tenant C contains multiple branches within single company database");

                // Tenant B token cannot access Tenant C branches or customers
                string ownerBToken = await Login(client, "owner_b_test", "Test-only!OwnerB2026", "tenant-b");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerBToken);
                await Status(await client.GetAsync("/crm/branches"), HttpStatusCode.Forbidden, "Tenant B token cannot access Tenant C branches");

                // Unauthorized company identifiers are rejected
                await Status(await client.PostAsJsonAsync("/auth/login", new { username = "owner_c_test", password = "Test-only!OwnerC2026", tenantId = "unauthorized-company" }),
                    HttpStatusCode.Forbidden, "Unauthorized company identifiers are rejected");

                // --- Phase 3: Branch Manager & Branch Staff Scoping & Isolation Tests ---
                using (var scope = services.CreateScope())
                {
                    var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                    var master = scope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>();

                    foreach (var (uname, role) in new[] { ("bm_north", "BranchManager:NORTH"), ("bm_south", "BranchManager:SOUTH"), ("sa_north", "ServiceAdvisor:NORTH") })
                    {
                        var u = new IdentityUser(uname) { LockoutEnabled = true };
                        await userMgr.CreateAsync(u, "Test-only!Staff2026");
                        master.Memberships.Add(new TenantMembership { TenantId = "tenant-c", UserId = u.Id, Role = role, DisplayName = uname, IsActive = true });
                    }
                    await master.SaveChangesAsync();
                }

                // 1. Login as bm_north
                string tokenBmNorth = await Login(client, "bm_north", "Test-only!Staff2026", "tenant-c");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBmNorth);

                var meNorth = await Read<JsonElement>(await client.GetAsync("/auth/me"));
                Check(meNorth.GetProperty("role").GetString() == "BranchManager", "bm_north base role is BranchManager");
                Check(meNorth.GetProperty("branchCode").GetString() == "NORTH", "bm_north branchCode claim is NORTH");

                var northBranches = await Read<JsonElement>(await client.GetAsync("/crm/branches"));
                Check(northBranches.GetArrayLength() == 1 && northBranches[0].GetProperty("branchCode").GetString() == "NORTH", "bm_north sees only assigned NORTH branch");
                int northBranchId = northBranches[0].GetProperty("branchId").GetInt32();

                // Create customer in Tenant C
                var custNorth = await Read<Customer>(await client.PostAsJsonAsync("/crm/customers",
                    new { fullName = "Customer North", contactNumber = "09180000002" }));

                // Create JobOrder as bm_north (attempt to forge BranchId=999)
                var createNorthJob = new JobOrderWrite(custNorth.CustomerId, "Honda Civic / NNN-111", 2000, DateTime.Now, "In Queue", "North test",
                    JobServices.Offered.Take(1).ToArray(), [], BranchId: 999);
                var northJob = await Read<JobOrder>(await client.PostAsJsonAsync("/crm/job-orders", createNorthJob));
                Check(northJob.BranchId == northBranchId, "Server enforces assigned branch ID on created JobOrder regardless of client payload");

                // Post payment for North job
                var payNorth = await Read<Payment>(await client.PostAsJsonAsync("/crm/payments",
                    new PaymentWrite(northJob.JobOrderId, 1000, "Deposit", "Cash", false, Guid.NewGuid())));
                Check(payNorth.BranchId == northBranchId, "Payment inherits assigned branch ID");

                // BranchManager permission boundaries:
                await Status(await client.DeleteAsync($"/crm/job-orders/{northJob.JobOrderId}"), HttpStatusCode.Forbidden, "BranchManager cannot delete job orders");
                await Status(await client.PostAsJsonAsync($"/crm/payments/{payNorth.PaymentId}/void", new VoidPayment("Denied", payNorth.RowVersion)),
                    HttpStatusCode.Forbidden, "BranchManager cannot void payments");
                await Status(await client.GetAsync("/crm/staff"), HttpStatusCode.Forbidden, "BranchManager cannot manage staff");
                await AssertReportsDeniedAsync(client, HttpStatusCode.Forbidden, "BranchManager");

                // 2. Login as bm_south and verify branch isolation
                string tokenBmSouth = await Login(client, "bm_south", "Test-only!Staff2026", "tenant-c");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenBmSouth);

                var meSouth = await Read<JsonElement>(await client.GetAsync("/auth/me"));
                Check(meSouth.GetProperty("role").GetString() == "BranchManager", "bm_south base role is BranchManager");
                Check(meSouth.GetProperty("branchCode").GetString() == "SOUTH", "bm_south branchCode claim is SOUTH");

                var southJobs = await client.GetFromJsonAsync<List<JobOrder>>("/crm/job-orders");
                Check(southJobs!.All(j => j.JobOrderId != northJob.JobOrderId), "bm_south cannot list jobs from NORTH branch");

                await Status(await client.GetAsync($"/crm/job-orders/{northJob.JobOrderId}"), HttpStatusCode.NotFound, "bm_south cannot get North job");
                await Status(await client.PutAsJsonAsync($"/crm/job-orders/{northJob.JobOrderId}", createNorthJob with { RowVersion = northJob.RowVersion }),
                    HttpStatusCode.Forbidden, "bm_south cannot update North job");
                await Status(await client.DeleteAsync($"/crm/job-orders/{northJob.JobOrderId}"),
                    HttpStatusCode.Forbidden, "bm_south cannot delete North job");
                await Status(await client.PostAsJsonAsync("/crm/payments",
                    new PaymentWrite(northJob.JobOrderId, 500, "Deposit", "Cash", false, Guid.NewGuid())),
                    HttpStatusCode.Forbidden, "bm_south cannot record payment for job belonging to another branch");

                // 3. Login as sa_north and verify ServiceAdvisor permissions
                string tokenSaNorth = await Login(client, "sa_north", "Test-only!Staff2026", "tenant-c");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenSaNorth);

                var meSa = await Read<JsonElement>(await client.GetAsync("/auth/me"));
                Check(meSa.GetProperty("role").GetString() == "ServiceAdvisor", "sa_north base role is ServiceAdvisor");

                await Status(await client.PostAsJsonAsync($"/crm/payments/{payNorth.PaymentId}/void", new VoidPayment("Denied", payNorth.RowVersion)),
                    HttpStatusCode.Forbidden, "Service Advisor cannot void payments");
                await Status(await client.PutAsJsonAsync("/crm/services/1", new ServiceWrite("Change", 8888, true, [])),
                    HttpStatusCode.Forbidden, "Service Advisor cannot modify service catalog prices");

                // 4. Test direct parent-child branch consistency in DbContext
                var southBranch = await dbC.Branches.SingleAsync(b => b.BranchCode == "SOUTH");
                var invalidPayment = new Payment {
                    JobOrderId = northJob.JobOrderId,
                    BranchId = southBranch.BranchId,
                    AmountPaid = 200,
                    PaymentMethod = "Cash",
                    PaymentType = "Deposit",
                    SubmissionId = Guid.NewGuid()
                };
                dbC.Payments.Add(invalidPayment);
                try
                {
                    await dbC.SaveChangesAsync();
                    throw new Exception("Mismatched payment branch was accepted");
                }
                catch (InvalidOperationException ex)
                {
                    Check(ex.Message.Contains("must match parent Job Order branch"), "Cross-branch payment creation fails closed at DbContext layer");
                }
                dbC.Entry(invalidPayment).State = EntityState.Detached;

                var invalidAppt = new Appointment {
                    CustomerId = custNorth.CustomerId,
                    BranchId = southBranch.BranchId,
                    LinkedJobOrderId = northJob.JobOrderId,
                    ScheduledStart = DateTime.UtcNow.AddDays(1),
                    Status = "Checked In"
                };
                dbC.Appointments.Add(invalidAppt);
                try
                {
                    await dbC.SaveChangesAsync();
                    throw new Exception("Mismatched appointment branch was accepted");
                }
                catch (InvalidOperationException ex)
                {
                    Check(ex.Message.Contains("must match linked Job Order branch"), "Cross-branch appointment linking fails closed at DbContext layer");
                }
                dbC.Entry(invalidAppt).State = EntityState.Detached;
            }
            finally { await dbC.Database.EnsureDeletedAsync(); }
        }
    }

    static async Task DocumentConfigurationChecks(IServiceProvider services, HttpClient client, string tenantConnection, string ownerToken, int jobId, int paymentId)
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(tenantConnection);
        builder.InitialCatalog += "_DocumentsB";
        string connection = builder.ConnectionString;
        var configuration = services.GetRequiredService<IConfiguration>();
        configuration["ConnectionStrings:DocumentTest"] = connection;
        await using var db = new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(connection).Options);
        try
        {
            await db.Database.MigrateAsync();
            db.DatabaseIdentity.Add(new TenantDatabaseIdentity { TenantId = "document-test" });
            var customer = new Customer { FullName = "Business B customer", ContactNumber = "09170000000" };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            var job = new JobOrder { CustomerId = customer.CustomerId, VehicleDetails = "Business B vehicle", Status = "In Queue", StartedAt = DateTime.UtcNow, TotalEstimatedCost = 1000 };
            db.JobOrders.Add(job);
            await db.SaveChangesAsync();
            var payment = new Payment { JobOrderId = job.JobOrderId, AmountPaid = 100, PaymentType = "Downpayment", PaymentMethod = "Cash" };
            db.Payments.Add(payment);
            await db.SaveChangesAsync();
            using var scope = services.CreateScope();
            var master = scope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>();
            string ownerId = await master.Users.Where(u => u.UserName == "owner").Select(u => u.Id).SingleAsync();
            master.Tenants.Add(new TenantRegistration { TenantId = "document-test", DisplayName = "Registry Business B", ConnectionKey = "DocumentTest" });
            master.Memberships.Add(new TenantMembership { TenantId = "document-test", UserId = ownerId });
            foreach (var module in await master.ModuleEntitlements.Where(e => e.TenantId == "tenant-a" && e.IsEnabled).Select(e => e.ModuleId).ToListAsync())
                master.ModuleEntitlements.Add(new TenantModuleEntitlement { TenantId = "document-test", ModuleId = module, IsEnabled = true });
            await master.SaveChangesAsync();
            string tokenB = await Login(client, "owner", "Test-only!Owner2026", "document-test");
            async Task Verify(string token, int jid, int pid, string name, string address)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                foreach (string path in new[] { $"/crm/workshop/jobs/{jid}/work-order", $"/crm/payments/{pid}/receipt" })
                {
                    var document = await Read<JsonElement>(await client.GetAsync(path));
                    var business = document.GetProperty("businessDetails");
                    Check(business.GetProperty("name").GetString() == name && business.GetProperty("address").GetString() == address,
                        $"Document API resolves authorized tenant configuration: {name} ({path})");
                }
            }
            await Verify(ownerToken, jobId, paymentId, "Document Business A", "Address A");
            await Verify(tokenB, job.JobOrderId, payment.PaymentId, "Document Business B", "Address B");
            await Verify(ownerToken, jobId, paymentId, "Document Business A", "Address A");
            configuration["Tenants:document-test:Name"] = null;
            configuration["Tenants:document-test:Address"] = null;
            await Verify(tokenB, job.JobOrderId, payment.PaymentId, "Registry Business B", null!);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }

    static async Task RbacChecks(HttpClient client, string ownerToken, JobOrder job, JobTask task, Payment payment)
    {
        var staff = new Dictionary<string, string>();
        foreach (string role in new[] { "ServiceAdvisor", "LeadMechanic", "AutomotiveMechanic", "DetailingTechnician" })
        {
            var created = await Read<JsonElement>(await client.PostAsJsonAsync("/crm/staff", new CreateStaff(role, "Test-only!Staff2026", role, role)));
            staff[role] = created.GetProperty("id").GetString()!;
        }
        await Status(await client.PostAsJsonAsync("/crm/staff", new CreateStaff("bad", "Test-only!Staff2026", "Bad", "SuperAdmin")), HttpStatusCode.BadRequest, "Tenant owner cannot create a Super Admin");
        // Individual task endpoint is retired with conflict response
        await Status(await client.PutAsJsonAsync($"/crm/job-tasks/{task.JobTaskId}/assignment", new AssignTask(staff["AutomotiveMechanic"], "Task assign", task.RowVersion)), HttpStatusCode.Conflict, "Task assignment endpoint retired with whole-job semantics requirement");
        // Whole job assignment enforces mandatory reason
        job = (await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{job.JobOrderId}"))!;
        await Status(await client.PutAsJsonAsync($"/crm/job-orders/{job.JobOrderId}/assignment", new AssignJob(staff["AutomotiveMechanic"], "", job.RowVersion)), HttpStatusCode.BadRequest, "Assignment reason is required");
        // Assign whole job to AutomotiveMechanic
        await Read<JsonElement>(await client.PutAsJsonAsync($"/crm/job-orders/{job.JobOrderId}/assignment", new AssignJob(staff["AutomotiveMechanic"], "Initial whole-job assignment", job.RowVersion)));
        var rows = await client.GetFromJsonAsync<List<JobTask>>("/crm/job-tasks");
        task = rows!.Single(t => t.JobTaskId == task.JobTaskId);
        foreach (string role in staff.Keys)
        {
            var token = await Login(client, role, "Test-only!Staff2026", "tenant-a");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await Status(await client.GetAsync("/crm/staff"), HttpStatusCode.Forbidden, role + " cannot manage staff");
            await Status(await client.PostAsJsonAsync("/crm/payments/1/void", new VoidPayment("Denied", [])), HttpStatusCode.Forbidden, role + " cannot void payments");
            if (role != "ServiceAdvisor") await Status(await client.GetAsync("/crm/payments"), HttpStatusCode.Forbidden, role + " cannot read payments");
            if (role == "ServiceAdvisor")
            {
                await Status(await client.PutAsJsonAsync($"/crm/job-orders/{job.JobOrderId}/assignment", new AssignJob(staff["AutomotiveMechanic"], "Denied", job.RowVersion)), HttpStatusCode.Forbidden, "Service Advisor cannot assign jobs");
            }
            if (role is "AutomotiveMechanic" or "DetailingTechnician")
            {
                await Status(await client.GetAsync("/crm/job-orders"), HttpStatusCode.Forbidden, role + " cannot read unrestricted jobs");
                var work = await client.GetFromJsonAsync<JsonElement>("/crm/workshop");
                Check(work.GetArrayLength() == (role == "AutomotiveMechanic" ? 2 : 0), role + " sees assigned work only");
                await Status(await client.PutAsJsonAsync($"/crm/job-tasks/{task.JobTaskId}", new TaskWrite("Ongoing", "Assigned progress", task.RowVersion)),
                    role == "AutomotiveMechanic" ? HttpStatusCode.OK : HttpStatusCode.NotFound, role + " task ownership enforced");

                // Work order authorization tests:
                var workOrderRes = await client.GetAsync($"/crm/workshop/jobs/{job.JobOrderId}/work-order");
                if (role == "AutomotiveMechanic")
                {
                    Check(workOrderRes.StatusCode == HttpStatusCode.OK, "Assigned mechanic can retrieve work order");
                    var wo = await Read<JsonElement>(workOrderRes);
                    Check(wo.GetProperty("jobOrderId").GetInt32() == job.JobOrderId, "Work order job ID matches");
                    Check(wo.GetProperty("intakeNotes").GetString() == "Backend test", "Work order includes intake notes from InquiryNotes");
                    Check(wo.GetProperty("tasks").GetArrayLength() == 2, "Assigned mechanic sees all job tasks on work order");
                    Check(!wo.TryGetProperty("contactNumber", out _) && !wo.TryGetProperty("email", out _) && !wo.TryGetProperty("payments", out _),
                        "Work order excludes unnecessary customer contacts and payment details");
                }
                else
                {
                    Check(workOrderRes.StatusCode == HttpStatusCode.Forbidden, "Unassigned technician is forbidden from retrieving work order");
                }

                await Status(await client.GetAsync($"/crm/payments/{payment.PaymentId}/receipt"), HttpStatusCode.Forbidden, role + " cannot access payment receipts");
                await AssertReportsDeniedAsync(client, HttpStatusCode.Forbidden, role);
            }
            if (role == "LeadMechanic")
            {
                var leadWoRes = await client.GetAsync($"/crm/workshop/jobs/{job.JobOrderId}/work-order");
                Check(leadWoRes.StatusCode == HttpStatusCode.OK, "Lead Mechanic can retrieve authorized work order");
                var leadWo = await Read<JsonElement>(leadWoRes);
                Check(leadWo.GetProperty("tasks").GetArrayLength() == 2, "Lead Mechanic sees all job tasks");
                await AssertReportsDeniedAsync(client, HttpStatusCode.Forbidden, role);
            }
            if (role == "ServiceAdvisor")
            {
                await AssertReportsDeniedAsync(client, HttpStatusCode.Forbidden, role);
                await Status(await client.PostAsJsonAsync($"/crm/job-orders/{job.JobOrderId}/status", new JobStatusChange("Completed", job.RowVersion)), HttpStatusCode.Forbidden, "Advisor cannot bypass QA");

                // Receipt authorization and fresh values check
                var receiptRes = await client.GetAsync($"/crm/payments/{payment.PaymentId}/receipt");
                Check(receiptRes.StatusCode == HttpStatusCode.OK, "Service Advisor can retrieve payment receipt");
                var rcpt = await Read<JsonElement>(receiptRes);
                Check(rcpt.GetProperty("recordedBy").GetString() == "owner", "Receipt cashier attribution reflects actual payment creator ('owner'), not current viewer");
                Check(rcpt.GetProperty("isVoided").GetBoolean() == true, "Receipt reflects real isVoided status");
                Check(rcpt.GetProperty("voidReason").GetString() == "Test correction", "Receipt reflects real void reason");
                Check(!string.IsNullOrWhiteSpace(rcpt.GetProperty("voidedAtUtc").GetString()), "Receipt reflects real voided timestamp");
                Check(rcpt.GetProperty("totalPaid").GetDecimal() == 0m, "Receipt recalculates total valid payments excluding voided payments");
                Check(rcpt.GetProperty("outstandingBalance").GetDecimal() == 1000m, "Receipt recalculates fresh outstanding balance");
                Check(rcpt.GetProperty("amountReceived").GetDecimal() == 500m, "Receipt preserves original payment amount");
            }
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var members = await client.GetFromJsonAsync<JsonElement>("/crm/staff");
        var mechanic = members.EnumerateArray().Single(m => m.GetProperty("userId").GetString() == staff["AutomotiveMechanic"]);
        await Status(await client.PutAsJsonAsync("/crm/staff/" + staff["AutomotiveMechanic"], new UpdateStaff("Mechanic", "AutomotiveMechanic", false, mechanic.GetProperty("rowVersion").GetBytesFromBase64())),
            HttpStatusCode.Conflict, "Unfinished tasks prevent staff deactivation");
        var owner = members.EnumerateArray().Single(m => m.GetProperty("role").GetString() == "OwnerManager");
        await Status(await client.PutAsJsonAsync("/crm/staff/" + owner.GetProperty("userId").GetString(), new UpdateStaff("Owner", "ServiceAdvisor", false, owner.GetProperty("rowVersion").GetBytesFromBase64())),
            HttpStatusCode.Conflict, "Owner cannot remove own access");
        await QaChecks(client, ownerToken, job.JobOrderId);
    }
    static async Task QaChecks(HttpClient client, string ownerToken, int jobId)
    {
        async Task<JobTask> TaskRow(int id) => (await client.GetFromJsonAsync<List<JobTask>>("/crm/job-tasks"))!.Single(t => t.JobTaskId == id);
        async Task<HttpResponseMessage> Advance(string stage)
        {
            var job = (await client.GetFromJsonAsync<JobOrder>("/crm/job-orders/" + jobId))!;
            return await client.PostAsJsonAsync($"/crm/job-orders/{jobId}/status", new JobStatusChange(stage, job.RowVersion));
        }
        await Status(await Advance("QA Checking"), HttpStatusCode.Conflict, "QA requires finished and submitted tasks");
        var tasks = (await client.GetFromJsonAsync<List<JobTask>>("/crm/job-tasks"))!;
        foreach (var row in tasks)
        {
            var task = row;
            if (task.Status == "Pending") task = await Read<JobTask>(await client.PutAsJsonAsync($"/crm/job-tasks/{task.JobTaskId}", new TaskWrite("Ongoing", "Started", task.RowVersion)));
            task = await Read<JobTask>(await client.PutAsJsonAsync($"/crm/job-tasks/{task.JobTaskId}", new TaskWrite("Done", "Finished", task.RowVersion)));
            if (task.JobTaskId == tasks[0].JobTaskId)
            {
                // The first task in this list may be unassigned; assignment is checked independently of role.
                var mechanicToken = await Login(client, "AutomotiveMechanic", "Test-only!Staff2026", "tenant-a");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mechanicToken);
                await Status(await client.PostAsJsonAsync($"/crm/job-tasks/{task.JobTaskId}/review", new ReviewRequest("Approved", "Not permitted", task.RowVersion)), HttpStatusCode.Forbidden, "Technician cannot approve QA directly");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
            }
            await Read<JsonElement>(await client.PostAsJsonAsync($"/crm/job-tasks/{task.JobTaskId}/submit", new ReviewRequest(null, "Ready for review", task.RowVersion)));
        }
        await Status(await Advance("QA Checking"), HttpStatusCode.Conflict, "Separate QA stage is retired even after submission");
        await Status(await Advance("Completed"), HttpStatusCode.Conflict, "Completion blocked until QA approval");
        string leadToken = await Login(client, "LeadMechanic", "Test-only!Staff2026", "tenant-a");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leadToken);
        var first = await TaskRow(tasks[0].JobTaskId);
        await Status(await client.PostAsJsonAsync($"/crm/job-tasks/{first.JobTaskId}/review", new ReviewRequest("Rework", "", first.RowVersion)), HttpStatusCode.BadRequest, "Rework requires a reason");
        await Read<JsonElement>(await client.PostAsJsonAsync($"/crm/job-tasks/{first.JobTaskId}/review", new ReviewRequest("Rework", "Adjust and retest", first.RowVersion)));
        first = await TaskRow(first.JobTaskId);
        Check(first.Status == "Ongoing", "QA rejection returns task to ongoing work");
        first = await Read<JobTask>(await client.PutAsJsonAsync($"/crm/job-tasks/{first.JobTaskId}", new TaskWrite("Done", "Retested", first.RowVersion)));
        await Read<JsonElement>(await client.PostAsJsonAsync($"/crm/job-tasks/{first.JobTaskId}/submit", new ReviewRequest(null, "Rework finished", first.RowVersion)));
        foreach (var row in tasks)
        {
            var task = await TaskRow(row.JobTaskId);
            await Read<JsonElement>(await client.PostAsJsonAsync($"/crm/job-tasks/{task.JobTaskId}/review", new ReviewRequest("Approved", "QA passed", task.RowVersion)));
        }
        var history = await client.GetFromJsonAsync<JsonElement>($"/crm/job-tasks/{first.JobTaskId}/reviews");
        Check(history.GetArrayLength() == 4, "Submission, rejection, resubmission and approval history retained");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        await Status(await Advance("Completed"), HttpStatusCode.OK, "Approved QA permits job completion");
        await Status(await Advance("Released"), HttpStatusCode.Conflict, "QA approval does not bypass payment requirement");
    }

    static async Task TenantAUseCasesChecks(IServiceProvider services, HttpClient client, string ownerToken, int customerId, int existingJobId)
    {
        Console.WriteLine("\n--- Testing Tenant A Authenticated Use Cases ---");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        // 1. Appointments: Create, List, Metrics, Check-In
        var apptRes = await client.PostAsJsonAsync("/crm/appointments", new AppointmentCreate(
            customerId, null, DateTime.UtcNow.AddDays(1), "Change Oil; Brake Service", "Customer prefers morning intake"));
        Check(apptRes.StatusCode == HttpStatusCode.Created, "Tenant A: Successfully created appointment");
        var apptJson = await Read<JsonElement>(apptRes);
        int apptId = apptJson.GetProperty("appointmentId").GetInt32();

        var apptList = await client.GetFromJsonAsync<JsonElement>("/crm/appointments");
        Check(apptList.EnumerateArray().Any(a => a.GetProperty("appointmentId").GetInt32() == apptId),
            "Tenant A: Appointment appears in appointments list");

        var apptMetrics = await client.GetFromJsonAsync<JsonElement>("/crm/appointments/metrics");
        Check(apptMetrics.GetProperty("scheduled").GetInt32() >= 1, "Tenant A: Appointment metrics track scheduled appointments");

        // Appointment Check-In creates Job Order atomically
        var checkInRes = await client.PostAsync($"/crm/appointments/{apptId}/check-in", null);
        Check(checkInRes.StatusCode == HttpStatusCode.OK, "Tenant A: Appointment check-in creates Job Order");
        var checkInJson = await Read<JsonElement>(checkInRes);
        int createdJobId = checkInJson.GetProperty("jobOrderId").GetInt32();
        Check(createdJobId > 0, "Tenant A: Created Job Order has positive ID");

        // Verify checked in appointment cannot be checked in again
        var reCheckIn = await client.PostAsync($"/crm/appointments/{apptId}/check-in", null);
        Check(reCheckIn.StatusCode == HttpStatusCode.BadRequest, "Tenant A: Double check-in rejected");

        // 2. Estimates & Approvals: Create, List, Approve, Convert
        var estRes = await client.PostAsJsonAsync("/crm/estimates", new EstimateCreate(
            customerId, null, "Transmission Overhaul Estimate", new List<EstimateLineCreate>
            {
                new(null, "Transmission Fluid Flush", 1, 1500m),
                new(null, "Clutch Plate Replacement", 1, 3500m)
            }));
        Check(estRes.StatusCode == HttpStatusCode.Created, "Tenant A: Created Estimate with 2 lines (Total ₱5,000.00)");
        var estJson = await Read<JsonElement>(estRes);
        int estId = estJson.GetProperty("estimateId").GetInt32();
        Check(estJson.GetProperty("totalAmount").GetDecimal() == 5000m, "Tenant A: Estimate total amount calculated correctly");

        // Convert before approval is blocked
        var prematureConvert = await client.PostAsync($"/crm/estimates/{estId}/convert", null);
        Check(prematureConvert.StatusCode == HttpStatusCode.BadRequest, "Tenant A: Unapproved estimate conversion is blocked");

        // Approve Estimate
        var approveRes = await client.PostAsync($"/crm/estimates/{estId}/approve", null);
        Check(approveRes.StatusCode == HttpStatusCode.OK, "Tenant A: Estimate successfully approved");

        // Convert Estimate to Job Order
        var convertRes = await client.PostAsync($"/crm/estimates/{estId}/convert", null);
        Check(convertRes.StatusCode == HttpStatusCode.OK, "Tenant A: Approved estimate converts to Job Order in Queue");
        var convertJson = await Read<JsonElement>(convertRes);
        int estJobId = convertJson.GetProperty("jobOrderId").GetInt32();
        Check(estJobId > 0, "Tenant A: Converted Job Order has positive ID");

        // Verify job has 2 tasks created
        var estTasks = await client.GetFromJsonAsync<List<JobTask>>($"/crm/job-tasks?jobOrderId={estJobId}");
        Check(estTasks!.Count == 2, "Tenant A: Converted Job Order initialized tasks from estimate lines");

        // 3. Terms & Conditions and Extended Versioning
        var termsRes = await client.GetAsync("/crm/terms");
        Check(termsRes.StatusCode == HttpStatusCode.OK, "Tenant A: Standard terms and conditions endpoint accessible");
        var termsJson = await Read<JsonElement>(termsRes);
        Check(termsJson.GetProperty("version").GetString() == "v1.0", "Tenant A: Terms version is v1.0");

        var termsVersions = await client.GetFromJsonAsync<JsonElement>("/crm/terms/versions");
        Check(termsVersions.GetArrayLength() >= 1, "Tenant A: Terms version history queryable");

        var publishRes = await client.PostAsJsonAsync("/crm/terms", new TermsPublishRequest(
            "v1.1", "RevCare Workshop Terms v1.1", new List<string> { "Updated liability terms", "Enhanced parts warranty" }, DateTime.UtcNow, true));
        Check(publishRes.StatusCode == HttpStatusCode.Created, "Tenant A: Published terms version v1.1");

        var activeTermsCheck = await client.GetFromJsonAsync<JsonElement>("/crm/terms");
        Check(activeTermsCheck.GetProperty("version").GetString() == "v1.1", "Tenant A: v1.1 is now active terms version");

        var termsAcceptRes = await client.PostAsJsonAsync($"/crm/job-orders/{estJobId}/terms-acceptance",
            new TermsAcceptanceRequest("v1.1", true, "Customer signed intake agreement v1.1", "Initial Customer", "Digital Intake Pad"));
        Check(termsAcceptRes.StatusCode == HttpStatusCode.OK, "Tenant A: Terms acceptance recorded for Job Order with v1.1");

        var termsStatusRes = await client.GetFromJsonAsync<JsonElement>($"/crm/job-orders/{estJobId}/terms-status");
        Check(termsStatusRes.GetProperty("termsAccepted").GetBoolean() == true, "Tenant A: Job Order terms status confirmed as accepted");

        // 4. Duplicate Job Order Prevention Workflow
        var custDup = await Read<Customer>(await client.PostAsJsonAsync("/crm/customers",
            new { fullName = "Workflow Test Customer", contactNumber = "09189998877", email = "workflow@test.com" }));
        var vehDup = await Read<Vehicle>(await client.PostAsJsonAsync("/crm/vehicles",
            new Vehicle { CustomerId = custDup.CustomerId, Make = "Honda", Model = "CR-V", PlateNumber = "WF-9988", Year = 2023 }));
        var apptDupRes = await client.PostAsJsonAsync("/crm/appointments", new AppointmentCreate(
            custDup.CustomerId, vehDup.VehicleId, DateTime.UtcNow.AddHours(2), "Scheduled Brake Inspection", "Workflow test"));
        Check(apptDupRes.StatusCode == HttpStatusCode.Created, "Tenant A: Created workflow-test appointment");
        var apptDupJson = await Read<JsonElement>(apptDupRes);
        int apptDupId = apptDupJson.GetProperty("appointmentId").GetInt32();

        var checkInDupRes = await client.PostAsync($"/crm/appointments/{apptDupId}/check-in", null);
        Check(checkInDupRes.StatusCode == HttpStatusCode.OK, "Tenant A: Checked in appointment creates queued Job Order");
        var checkInDupJson = await Read<JsonElement>(checkInDupRes);
        int queuedJobId = checkInDupJson.GetProperty("jobOrderId").GetInt32();

        var estDupRes = await client.PostAsJsonAsync("/crm/estimates", new EstimateCreate(
            custDup.CustomerId, vehDup.VehicleId, "Approved Brake Overhaul", new List<EstimateLineCreate>
            {
                new(null, "Brake Rotor Replacement", 1, 3000m)
            }));
        var estDupJson = await Read<JsonElement>(estDupRes);
        int estDupId = estDupJson.GetProperty("estimateId").GetInt32();
        await client.PostAsync($"/crm/estimates/{estDupId}/approve", null);

        var convDupRes = await client.PostAsync($"/crm/estimates/{estDupId}/convert", null);
        Check(convDupRes.StatusCode == HttpStatusCode.OK, "Tenant A: Converted estimate for checked-in customer");
        var convDupJson = await Read<JsonElement>(convDupRes);
        Check(convDupJson.GetProperty("wasExistingJobOrder").GetBoolean() == true, "Tenant A: Converting estimate detects existing queued Job Order");
        Check(convDupJson.GetProperty("jobOrderId").GetInt32() == queuedJobId, "Tenant A: Converting estimate links to existing Job Order without creating duplicate");

        var custJobs = await client.GetFromJsonAsync<JsonElement>("/crm/job-orders");
        int matchingJobs = custJobs.EnumerateArray().Count(j => j.GetProperty("customerId").GetInt32() == custDup.CustomerId && j.GetProperty("status").GetString() == "In Queue");
        Check(matchingJobs == 1, "Tenant A: Exactly 1 queued Job Order exists for customer, zero duplicates");

        // 5. Insurance Claim Lifecycle & Non-Payment Business Rule
        var insCreate = await client.PostAsJsonAsync("/crm/insurance", new InsuranceClaimCreateRequest(
            "Charter Ping An", "CLM-REV-8801", estJobId, 4000m, 1000m, "Collision dent repair"));
        Check(insCreate.StatusCode == HttpStatusCode.Created, "Tenant A: Filed insurance claim");

        var insGet = await client.GetFromJsonAsync<JsonElement>("/crm/insurance/CLM-REV-8801");
        Check(insGet.GetProperty("claimReference").GetString() == "CLM-REV-8801", "Tenant A: Insurance claim retrieved");

        var insApprove = await client.PostAsJsonAsync("/crm/insurance/CLM-REV-8801/approve", new InsuranceApprovalRequest(
            3500m, 1500m, "APPR-9988", "Approved coverage ₱3,500"));
        Check(insApprove.StatusCode == HttpStatusCode.OK, "Tenant A: Approved insurance claim coverage");

        // Rule verification: TotalPaid MUST NOT increase upon insurance approval
        var jobAfterInsApprove = await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{estJobId}");
        Check(jobAfterInsApprove!.TotalPaid == 0m, "Tenant A: Insurance approval strictly does NOT count as received payment");

        // Record insurance remittance
        var insPayRes = await client.PostAsJsonAsync("/crm/insurance/CLM-REV-8801/record-payment", new InsurancePaymentRecordRequest(
            3500m, "Bank Transfer", "Insurer bank remittance REF-990022"));
        Check(insPayRes.StatusCode == HttpStatusCode.OK, "Tenant A: Recorded insurance payment");
        var jobAfterInsPay = await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{estJobId}");
        Check(jobAfterInsPay!.TotalPaid == 3500m, "Tenant A: Insurance remittance officially updates total paid ledger");

        // 6. Customer Follow-ups & Feedback
        var fuRes = await client.PostAsJsonAsync("/crm/follow-ups", new CreateFollowUpRequest(
            customerId, null, estJobId, "Post-Repair 3-Day Quality Call", DateTime.UtcNow.AddDays(3), "advisor", "Inquire about transmission shift quality"));
        Check(fuRes.StatusCode == HttpStatusCode.Created, "Tenant A: Created customer follow-up");
        var fuJson = await Read<JsonElement>(fuRes);
        Guid fuId = fuJson.GetProperty("followUpId").GetGuid();

        var fuUpdate = await client.PutAsJsonAsync($"/crm/follow-ups/{fuId}/status", new UpdateFollowUpStatusRequest(
            "Completed", "Customer confirmed smooth transmission shifting"));
        Check(fuUpdate.StatusCode == HttpStatusCode.OK, "Tenant A: Completed customer follow-up with outcome notes");

        var custFollowUps = await client.GetFromJsonAsync<JsonElement>($"/crm/customers/{customerId}/follow-ups");
        Check(custFollowUps.EnumerateArray().Any(f => f.GetProperty("followUpId").GetGuid() == fuId && f.GetProperty("status").GetString() == "Completed"),
            "Tenant A: Follow-up visible in Customer 360 profile");

        // 6b. Legacy Behavioral Parity: Inquiries (+ Convert), Complaints, Feedback, Services, Customer Concurrency & Soft-Archive
        var inqCreate = await client.PostAsJsonAsync("/crm/inquiries", new Inquiry
        {
            CustomerId = customerId,
            CustomerName = "Initial Customer",
            ContactNumber = "09171234567",
            VehicleDetails = "2022 Mazda 3",
            InquiryPlatform = "Walk-in",
            MessageDetails = "Requesting PMS quote",
            Status = "Unanswered"
        });
        Check(inqCreate.StatusCode == HttpStatusCode.Created, "Legacy Parity: Create Inquiry succeeds");
        var inq = await Read<Inquiry>(inqCreate);
        inq.Status = "Responded";
        inq.MessageDetails = "Sent PMS quotation";
        var inqPut = await client.PutAsJsonAsync($"/crm/inquiries/{inq.InquiryId}", inq);
        Check(inqPut.StatusCode == HttpStatusCode.OK, "Legacy Parity: Update Inquiry status/details succeeds");
        inq.MessageDetails = "Conflicting edit with stale RowVersion";
        await Status(await client.PutAsJsonAsync($"/crm/inquiries/{inq.InquiryId}", inq), HttpStatusCode.Conflict, "Legacy Parity: Stale Inquiry RowVersion rejected with 409 Conflict");
        var inqConvert = await client.PostAsJsonAsync($"/crm/inquiries/{inq.InquiryId}/convert", new { customerId, vehicleDetails = "2022 Mazda 3" });
        Check(inqConvert.StatusCode == HttpStatusCode.OK, "Legacy Parity: Convert Inquiry to JobOrder succeeds");
        var inqConvJson = await Read<JsonElement>(inqConvert);
        Check(inqConvJson.GetProperty("jobOrderId").GetInt32() > 0, "Legacy Parity: Converted inquiry links to created Job Order");
        var inqArch = await Read<Inquiry>(await client.PostAsJsonAsync("/crm/inquiries", new Inquiry
        {
            CustomerName = "Archive Inquiry Probe",
            ContactNumber = "09170001122",
            InquiryPlatform = "Messenger",
            MessageDetails = "Archive test"
        }));
        await Status(await client.DeleteAsync($"/crm/inquiries/{inqArch.InquiryId}"), HttpStatusCode.OK, "Legacy Parity: Soft-archive Inquiry succeeds");

        var compCreate = await client.PostAsJsonAsync("/crm/complaints", new Complaint
        {
            CustomerId = customerId,
            JobOrderId = estJobId,
            ComplaintType = "Service Quality",
            Description = "Minor cabin rattle after test drive",
            IsUrgent = true,
            ResolutionStatus = "Open"
        });
        Check(compCreate.StatusCode == HttpStatusCode.Created, "Legacy Parity: Create Complaint succeeds");
        var comp = await Read<Complaint>(compCreate);
        comp.ResolutionStatus = "Resolved";
        comp.ActionTaken = "Tightened dashboard trim";
        var compPut = await client.PutAsJsonAsync($"/crm/complaints/{comp.ComplaintId}", comp);
        Check(compPut.StatusCode == HttpStatusCode.OK, "Legacy Parity: Resolve Complaint succeeds");
        await Status(await client.DeleteAsync($"/crm/complaints/{comp.ComplaintId}"), HttpStatusCode.OK, "Legacy Parity: Soft-archive Complaint succeeds");

        var fbCreate = await client.PostAsJsonAsync("/crm/feedbacks", new Feedback
        {
            JobOrderId = estJobId,
            RatingScore = 4,
            PositiveRemarks = "Prompt intake",
            AreasForImprovement = "Waiting lounge coffee"
        });
        Check(fbCreate.StatusCode == HttpStatusCode.Created, "Legacy Parity: Create Feedback succeeds");
        var fb = await Read<Feedback>(fbCreate);
        var fbPut = await client.PutAsJsonAsync($"/crm/feedbacks/{fb.FeedbackId}", new Feedback
        {
            FeedbackId = fb.FeedbackId,
            JobOrderId = estJobId,
            RatingScore = 5,
            PositiveRemarks = "Updated after follow-up call",
            AreasForImprovement = "None",
            RowVersion = fb.RowVersion
        });
        Check(fbPut.StatusCode == HttpStatusCode.OK, "Legacy Parity: Update Feedback succeeds");
        await Status(await client.DeleteAsync($"/crm/feedbacks/{fb.FeedbackId}"), HttpStatusCode.OK, "Legacy Parity: Soft-archive Feedback succeeds");

        var svcList = await client.GetFromJsonAsync<JsonElement>("/crm/services");
        Check(svcList.GetArrayLength() >= 1, "Legacy Parity: Services catalog endpoint returns offerings");

        // 7. Operational Reports
        var opSummary = await client.GetFromJsonAsync<JsonElement>("/crm/reports/operational-summary");
        Check(opSummary.GetProperty("overview").GetProperty("totalJobOrders").GetInt32() >= 3,
            "Tenant A: Operational summary tracks total job orders");
        Check(opSummary.GetProperty("workshopExecution").GetProperty("totalTasks").GetInt32() >= 4,
            "Tenant A: Operational summary tracks workshop execution tasks");
        Check(opSummary.TryGetProperty("financialSummary", out var finSummary) && finSummary.TryGetProperty("grossCollections", out _),
            "Tenant A: Operational summary provides financial collections overview");

        var finReport = await client.GetFromJsonAsync<JsonElement>("/crm/reports/financial-summary");
        Check(finReport.GetProperty("validPaymentCount").GetInt32() >= 0, "Tenant A: Financial report calculates payment breakdowns");

        var perfReport = await client.GetFromJsonAsync<JsonElement>("/crm/reports/workshop-performance");
        Check(perfReport.TryGetProperty("mechanicWorkload", out _) && perfReport.TryGetProperty("taskStatusBreakdown", out _),
            "Tenant A: Workshop performance report returns workload and status breakdown");

        // 8. Staff Password Reset & Security RBAC
        var staffList = await client.GetFromJsonAsync<JsonElement>("/crm/staff");
        var targetStaff = staffList.EnumerateArray().FirstOrDefault(m => m.GetProperty("role").GetString() == "ServiceAdvisor");
        if (targetStaff.ValueKind != JsonValueKind.Undefined)
        {
            string staffUserId = targetStaff.GetProperty("userId").GetString()!;
            string staffUsername = targetStaff.GetProperty("userName").GetString()!;
            string newPassword = "NewPass!Reset2026";

            var resetRes = await client.PostAsJsonAsync($"/crm/staff/{staffUserId}/reset-password", new ResetPasswordRequest(newPassword));
            Check(resetRes.StatusCode == HttpStatusCode.OK, "Tenant A: Admin/Owner successfully reset staff password");

            // Verify login with new password
            string newStaffToken = await Login(client, staffUsername, newPassword, "tenant-a");
            Check(!string.IsNullOrEmpty(newStaffToken), "Tenant A: Staff member successfully logged in with new reset password");

            // Restore client authorization to ownerToken
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        }

        // SuperAdmin protection check
        string? saId = null;
        using (var scope = services.CreateScope())
        {
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var sa = await userMgr.FindByNameAsync("superadmin");
            saId = sa?.Id;
        }
        if (!string.IsNullOrEmpty(saId))
        {
            await Status(await client.PostAsJsonAsync($"/crm/staff/{saId}/reset-password", new ResetPasswordRequest("Hacked!Pass2026")),
                HttpStatusCode.Forbidden, "Tenant A Owner cannot reset SuperAdmin password");
        }
    }

    static async Task RunTwentyStepAcceptanceTest(WebApplicationFactory<global::Program> factory, HttpClient client, string masterToken, string ownerToken)
    {
        Console.WriteLine("\n==========================================================================");
        Console.WriteLine("STARTING 20-STEP AUTHENTICATED MULTI-ACTOR TRANSACTION ACCEPTANCE TEST");
        Console.WriteLine("==========================================================================");

        // STEP 1: Super Admin authenticates, verifies tenant health & modules
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", masterToken);
        var tList = await client.GetFromJsonAsync<JsonElement>("/master/tenants");
        Check(tList.EnumerateArray().Any(t => t.GetProperty("tenantId").GetString() == "tenant-a" && t.GetProperty("isActive").GetBoolean()),
            "Step 1: Super Admin confirms Tenant A is Active and Ready");
        var mods = await client.GetFromJsonAsync<JsonElement>("/master/modules");
        Check(mods.GetArrayLength() >= 5, "Step 1: Super Admin inspects module catalog");

        // STEP 2: Owner/Manager authenticates, reviews staff roster
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var staffList = await client.GetFromJsonAsync<JsonElement>("/crm/staff");
        Check(staffList.EnumerateArray().Any(s => s.GetProperty("role").GetString() == "ServiceAdvisor"),
            "Step 2: Owner verifies Service Advisor is active");
        Check(staffList.EnumerateArray().Any(s => s.GetProperty("role").GetString() == "LeadMechanic"),
            "Step 2: Owner verifies Lead Mechanic is active");

        // STEP 3: Service Advisor authenticates (using password updated during admin reset verification)
        string advisorToken = await Login(client, "ServiceAdvisor", "NewPass!Reset2026", "tenant-a");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", advisorToken);
        Check(!string.IsNullOrEmpty(advisorToken), "Step 3: Service Advisor authenticates with bearer token");

        // STEP 4: Service Advisor creates customer and vehicle
        var custRes = await client.PostAsJsonAsync("/crm/customers",
            new { fullName = "Carlos Mendoza", contactNumber = "09181112233", email = "carlos.mendoza@e2e.revcare.com" });
        Check(custRes.StatusCode == HttpStatusCode.Created, "Step 4: Advisor creates customer profile for Carlos Mendoza");
        var cust = await Read<Customer>(custRes);

        var vehRes = await client.PostAsJsonAsync("/crm/vehicles",
            new Vehicle { CustomerId = cust.CustomerId, Make = "Toyota", Model = "Fortuner 2023", PlateNumber = "NBD-7788", Year = 2023, Color = "Silver", Vin = "VIN-E2E-7788" });
        Check(vehRes.StatusCode == HttpStatusCode.Created, "Step 4: Advisor registers vehicle Toyota Fortuner / NBD-7788");
        var veh = await Read<Vehicle>(vehRes);

        // STEP 5: Service Advisor books appointment
        var apptRes = await client.PostAsJsonAsync("/crm/appointments", new AppointmentCreate(
            cust.CustomerId, veh.VehicleId, DateTime.UtcNow.AddHours(1), "Engine Valve Grinding & Gaskets; Full Exterior Detailing & Ceramic Coat", "VIP client intake"));
        Check(apptRes.StatusCode == HttpStatusCode.Created, "Step 5: Advisor books intake appointment");
        var apptJson = await Read<JsonElement>(apptRes);
        int apptId = apptJson.GetProperty("appointmentId").GetInt32();

        // STEP 6: Service Advisor checks in appointment -> queued Job Order created
        var checkInRes = await client.PostAsync($"/crm/appointments/{apptId}/check-in", null);
        Check(checkInRes.StatusCode == HttpStatusCode.OK, "Step 6: Advisor checks in appointment, generating Job Order");
        var checkInJson = await Read<JsonElement>(checkInRes);
        int e2eJobId = checkInJson.GetProperty("jobOrderId").GetInt32();
        Check(e2eJobId > 0, "Step 6: Queued Job Order created atomically");

        // STEP 7: Service Advisor generates detailed Estimate with labor & parts
        var estRes = await client.PostAsJsonAsync("/crm/estimates", new EstimateCreate(
            cust.CustomerId, veh.VehicleId, "Complete Overhaul & Detailing Estimate", new List<EstimateLineCreate>
            {
                new(null, "Engine Valve Grinding & Gaskets", 1, 7500m),
                new(null, "Full Exterior Detailing & Ceramic Coat", 1, 4500m)
            }));
        Check(estRes.StatusCode == HttpStatusCode.Created, "Step 7: Advisor generates Estimate (₱12,000 total)");
        var estJson = await Read<JsonElement>(estRes);
        int estId = estJson.GetProperty("estimateId").GetInt32();

        // STEP 8: Customer reviews & approves Estimate; Advisor converts Estimate -> links to queued Job Order (duplicate prevention!)
        var appEstRes = await client.PostAsync($"/crm/estimates/{estId}/approve", null);
        Check(appEstRes.StatusCode == HttpStatusCode.OK, "Step 8: Customer approves estimate");
        var convRes = await client.PostAsync($"/crm/estimates/{estId}/convert", null);
        Check(convRes.StatusCode == HttpStatusCode.OK, "Step 8: Advisor converts approved estimate to Job Order");
        var convJson = await Read<JsonElement>(convRes);
        Check(convJson.GetProperty("wasExistingJobOrder").GetBoolean() == true,
            "Step 8: Estimate conversion detects existing queued Job Order (Duplicate Prevention verified)");
        Check(convJson.GetProperty("jobOrderId").GetInt32() == e2eJobId,
            "Step 8: Estimate converts into same Job Order without creating duplicates");

        // STEP 9: Customer executes Terms & Conditions acceptance (v1.1)
        var termsAcceptRes = await client.PostAsJsonAsync($"/crm/job-orders/{e2eJobId}/terms-acceptance",
            new TermsAcceptanceRequest("v1.1", true, "Customer signed intake agreement on tablet", "Carlos Mendoza", "Digital Signature Pad"));
        Check(termsAcceptRes.StatusCode == HttpStatusCode.OK, "Step 9: Customer executes Terms & Conditions acceptance (v1.1)");
        var termsStatus = await client.GetFromJsonAsync<JsonElement>($"/crm/job-orders/{e2eJobId}/terms-status");
        Check(termsStatus.GetProperty("termsAccepted").GetBoolean() == true, "Step 9: Job Order terms status confirmed as accepted");

        // STEP 10: Customer files Insurance claim for repair
        var claimRes = await client.PostAsJsonAsync("/crm/insurance", new InsuranceClaimCreateRequest(
            "Commonwealth Insurance", "CLM-E2E-7788", e2eJobId, 6000m, 2000m, "Engine component claim"));
        Check(claimRes.StatusCode == HttpStatusCode.Created, "Step 10: Advisor files insurance claim CLM-E2E-7788");

        // STEP 11: Insurer approves coverage (₱6,000); verified that outstanding balance does not decrease yet
        var claimApproveRes = await client.PostAsJsonAsync("/crm/insurance/CLM-E2E-7788/approve", new InsuranceApprovalRequest(
            6000m, 6000m, "APPR-COMS-101", "Coverage approved for mechanical repairs ₱6,000"));
        Check(claimApproveRes.StatusCode == HttpStatusCode.OK, "Step 11: Insurer approves coverage of ₱6,000");

        var jobPrePay = await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{e2eJobId}");
        Check(jobPrePay!.TotalPaid == 0m, "Step 11: Business Rule: Insurance approval does NOT count as received payment");
        Check(jobPrePay.OutstandingBalance == 12000m, "Step 11: Outstanding balance remains ₱12,000 prior to actual remittance");

        // STEP 12: Customer pays required 50% downpayment (Cash ₱6,000); receipt issued
        var downpaymentRes = await client.PostAsJsonAsync("/crm/payments", new PaymentWrite(
            e2eJobId, 6000m, "Downpayment", "Cash", false, Guid.NewGuid()));
        Check(downpaymentRes.StatusCode == HttpStatusCode.Created, "Step 12: Customer settles ₱6,000 cash downpayment (mandatory 50% deposit)");
        var downpayment = await Read<Payment>(downpaymentRes);

        var rcptRes = await client.GetAsync($"/crm/payments/{downpayment.PaymentId}/receipt");
        Check(rcptRes.StatusCode == HttpStatusCode.OK, "Step 12: Cash receipt issued for downpayment");
        var rcptJson = await Read<JsonElement>(rcptRes);
        Check(rcptJson.GetProperty("totalPaid").GetDecimal() == 6000m, "Step 12: Validated receipt total paid is ₱6,000");

        // STEP 13: Service Advisor assigns Lead Mechanic to the Job Order
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var allStaff = await client.GetFromJsonAsync<JsonElement>("/crm/staff");
        string leadMechId = allStaff.EnumerateArray().First(s => s.GetProperty("role").GetString() == "LeadMechanic").GetProperty("userId").GetString()!;
        string autoMechId = allStaff.EnumerateArray().First(s => s.GetProperty("role").GetString() == "AutomotiveMechanic").GetProperty("userId").GetString()!;
        string detailMechId = allStaff.EnumerateArray().First(s => s.GetProperty("role").GetString() == "DetailingTechnician").GetProperty("userId").GetString()!;

        var curJob = await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{e2eJobId}");
        var assignJobRes = await client.PutAsJsonAsync($"/crm/job-orders/{e2eJobId}/assignment",
            new AssignJob(leadMechId, "Lead mechanic assigned for overhaul coordination", curJob!.RowVersion));
        Check(assignJobRes.StatusCode == HttpStatusCode.OK, "Step 13: Job Order assigned to Lead Mechanic");

        curJob = await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{e2eJobId}");
        var startJobRes = await client.PostAsJsonAsync($"/crm/job-orders/{e2eJobId}/status", new JobStatusChange("In Progress", curJob!.RowVersion));
        Check(startJobRes.StatusCode == HttpStatusCode.OK, "Step 13: Job Order transitioned to In Progress");

        // STEP 14: Lead Mechanic logs in, reviews work order, assigns tasks
        string leadToken = await Login(client, "LeadMechanic", "Test-only!Staff2026", "tenant-a");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leadToken);

        var woRes = await client.GetAsync($"/crm/workshop/jobs/{e2eJobId}/work-order");
        Check(woRes.StatusCode == HttpStatusCode.OK, "Step 14: Lead Mechanic retrieves authorized work order");
        var woJson = await Read<JsonElement>(woRes);
        Check(woJson.GetProperty("tasks").GetArrayLength() == 2, "Step 14: Work order contains 2 authorized tasks");

        var e2eTasks = await client.GetFromJsonAsync<List<JobTask>>($"/crm/job-tasks?jobOrderId={e2eJobId}");
        var task1 = e2eTasks![0];
        var task2 = e2eTasks[1];

        // STEP 15: Lead Mechanic executes Task 1, completes, and submits for QA review
        task1 = (await client.GetFromJsonAsync<List<JobTask>>($"/crm/job-tasks?jobOrderId={e2eJobId}"))![0];
        task1 = await Read<JobTask>(await client.PutAsJsonAsync($"/crm/job-tasks/{task1.JobTaskId}", new TaskWrite("Ongoing", "Started valve grinding", task1.RowVersion)));
        Check(task1.Status == "Ongoing", "Step 15: Workshop begins Task 1 (Ongoing)");

        task1 = await Read<JobTask>(await client.PutAsJsonAsync($"/crm/job-tasks/{task1.JobTaskId}", new TaskWrite("Done", "Grinding completed and torqued", task1.RowVersion)));
        Check(task1.Status == "Done", "Step 15: Workshop finishes Task 1 (Done)");

        var submit1 = await client.PostAsJsonAsync($"/crm/job-tasks/{task1.JobTaskId}/submit", new ReviewRequest(null, "Ready for Lead inspection", task1.RowVersion));
        Check(submit1.StatusCode == HttpStatusCode.OK, "Step 15: Task 1 submitted for QA review");

        // STEP 16: Workshop executes Task 2, completes detailing, and submits for QA
        task2 = (await client.GetFromJsonAsync<List<JobTask>>($"/crm/job-tasks?jobOrderId={e2eJobId}"))![1];
        task2 = await Read<JobTask>(await client.PutAsJsonAsync($"/crm/job-tasks/{task2.JobTaskId}", new TaskWrite("Ongoing", "Buffing and ceramic application", task2.RowVersion)));
        task2 = await Read<JobTask>(await client.PutAsJsonAsync($"/crm/job-tasks/{task2.JobTaskId}", new TaskWrite("Done", "Ceramic coating cured", task2.RowVersion)));
        var submit2 = await client.PostAsJsonAsync($"/crm/job-tasks/{task2.JobTaskId}/submit", new ReviewRequest(null, "Detailing ready for QA inspection", task2.RowVersion));
        Check(submit2.StatusCode == HttpStatusCode.OK, "Step 16: Task 2 completed and submitted for QA");

        // STEP 17: Lead Mechanic conducts QA inspection: rejects one task for rework, retests, and gives QA approval
        task1 = (await client.GetFromJsonAsync<List<JobTask>>($"/crm/job-tasks?jobOrderId={e2eJobId}"))![0];
        // Reject task 1 with reason
        var reworkRes = await client.PostAsJsonAsync($"/crm/job-tasks/{task1.JobTaskId}/review",
            new ReviewRequest("Rework", "Retorque cylinder head bolts to 85 lb-ft and pressure test", task1.RowVersion));
        Check(reworkRes.StatusCode == HttpStatusCode.OK, "Step 17: Lead Mechanic requests rework on Task 1");

        task1 = (await client.GetFromJsonAsync<List<JobTask>>($"/crm/job-tasks?jobOrderId={e2eJobId}"))![0];
        Check(task1.Status == "Ongoing", "Step 17: Reworked task automatically returns to Ongoing state");

        // Complete rework
        task1 = await Read<JobTask>(await client.PutAsJsonAsync($"/crm/job-tasks/{task1.JobTaskId}", new TaskWrite("Done", "Retorqued to 85 lb-ft, pressure test passed", task1.RowVersion)));
        await client.PostAsJsonAsync($"/crm/job-tasks/{task1.JobTaskId}/submit", new ReviewRequest(null, "Rework complete, re-submitted", task1.RowVersion));

        // Lead approves both tasks
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", leadToken);
        task1 = (await client.GetFromJsonAsync<List<JobTask>>($"/crm/job-tasks?jobOrderId={e2eJobId}"))![0];
        task2 = (await client.GetFromJsonAsync<List<JobTask>>($"/crm/job-tasks?jobOrderId={e2eJobId}"))![1];

        var app1 = await client.PostAsJsonAsync($"/crm/job-tasks/{task1.JobTaskId}/review", new ReviewRequest("Approved", "Valve grinding QA verified passed", task1.RowVersion));
        var app2 = await client.PostAsJsonAsync($"/crm/job-tasks/{task2.JobTaskId}/review", new ReviewRequest("Approved", "Detailing & coating QA verified passed", task2.RowVersion));
        Check(app1.StatusCode == HttpStatusCode.OK && app2.StatusCode == HttpStatusCode.OK, "Step 17: Lead Mechanic approves QA for all tasks");

        var wsAfterApp = await client.GetFromJsonAsync<JsonElement>("/crm/workshop");
        byte[] completeJobRowVersion = wsAfterApp.EnumerateArray()
            .First(j => j.GetProperty("jobOrderId").GetInt32() == e2eJobId)
            .GetProperty("jobRowVersion").GetBytesFromBase64();

        var completeRes = await client.PostAsJsonAsync($"/crm/job-orders/{e2eJobId}/status", new JobStatusChange("Completed", completeJobRowVersion));
        Check(completeRes.StatusCode == HttpStatusCode.OK, "Step 17: Lead Mechanic completes Job Order following successful QA");

        // STEP 18: Insurance remittance settlement & final customer settlement
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", advisorToken);

        // Record insurance payment
        var insPay = await client.PostAsJsonAsync("/crm/insurance/CLM-E2E-7788/record-payment", new InsurancePaymentRecordRequest(
            4000m, "Bank Transfer", "Commonwealth remittance REF-E2E-4000"));
        Check(insPay.StatusCode == HttpStatusCode.OK, "Step 18: Insurance remittance recorded (₱4,000 via Bank Transfer)");

        // Customer settles remaining ₱2,000 balance
        var finalPay = await client.PostAsJsonAsync("/crm/payments", new PaymentWrite(
            e2eJobId, 2000m, "Final Payment", "GCash", false, Guid.NewGuid()));
        Check(finalPay.StatusCode == HttpStatusCode.Created, "Step 18: Customer settles final balance (₱2,000 via GCash)");

        var curJobPaid = await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{e2eJobId}");
        Check(curJobPaid!.TotalPaid == 12000m, "Step 18: Job Order TotalPaid equals ₱12,000 (₱6,000 downpayment + ₱4,000 insurance + ₱2,000 final)");
        Check(curJobPaid.OutstandingBalance == 0m, "Step 18: Job Order outstanding balance settled to ₱0.00");

        // STEP 19: Final inspection & vehicle release
        curJobPaid = await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{e2eJobId}");
        var releaseRes = await client.PostAsJsonAsync($"/crm/job-orders/{e2eJobId}/status", new JobStatusChange("Released", curJobPaid!.RowVersion));
        Check(releaseRes.StatusCode == HttpStatusCode.OK, "Step 19: Vehicle released to customer with release signoff");

        var releasedJob = await client.GetFromJsonAsync<JobOrder>($"/crm/job-orders/{e2eJobId}");
        Check(releasedJob!.Status == "Released" && releasedJob.CompletedAt.HasValue, "Step 19: Job Order confirmed Released with completion timestamp");

        // STEP 20: Customer follow-up, feedback, and operational reports verification
        var e2eFuRes = await client.PostAsJsonAsync("/crm/follow-ups", new CreateFollowUpRequest(
            cust.CustomerId, veh.VehicleId, e2eJobId, "Post-Release 3-Day Check", DateTime.UtcNow.AddDays(3), "advisor", "Confirm engine smoothness"));
        Check(e2eFuRes.StatusCode == HttpStatusCode.Created, "Step 20: Advisor logs post-release customer follow-up");

        var e2eFbRes = await client.PostAsJsonAsync("/crm/feedbacks", new Feedback
        {
            JobOrderId = e2eJobId,
            RatingScore = 5,
            PositiveRemarks = "Engine runs like brand new, brilliant ceramic shine!",
            AreasForImprovement = "None, outstanding service"
        });
        Check(e2eFbRes.StatusCode == HttpStatusCode.Created, "Step 20: 5-Star customer feedback recorded");

        await AssertReportsDeniedAsync(client, HttpStatusCode.Forbidden, "ServiceAdvisor (Step 20)");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var opSummary = await client.GetFromJsonAsync<JsonElement>("/crm/reports/operational-summary");
        Check(opSummary.GetProperty("overview").GetProperty("totalJobOrders").GetInt32() >= 4, "Step 20: Operational summary reflects all executed jobs");
        Check(opSummary.GetProperty("overview").GetProperty("completedJobOrders").GetInt32() >= 2, "Step 20: Operational summary reflects completed & released jobs");

        Console.WriteLine("==========================================================================");
        Console.WriteLine("ALL 20 STEPS OF MULTI-ACTOR TRANSACTION ACCEPTANCE TEST COMPLETED SUCCESSFULLY!");
        Console.WriteLine("==========================================================================\n");
    }

    static void RoutingConfigurationChecks()
    {
        static IConfiguration BuildCfg(string? master, string? tenantA) =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformMaster"] = master,
                ["ConnectionStrings:TenantA"] = tenantA,
                ["ConnectionStrings:TenantB"] = "Server=(localdb)\\MSSQLLocalDB;Database=RevCare_TenantB;Trusted_Connection=True;",
                ["ConnectionStrings:TenantC"] = "Server=(localdb)\\MSSQLLocalDB;Database=RevCare_TenantC;Trusted_Connection=True;"
            }).Build();

        // 1. Local Tenant A resolves DB_MasterCrm + RevCare_PlatformMaster
        var localCfg = BuildCfg(
            @"Server=(localdb)\MSSQLLocalDB;Database=RevCare_PlatformMaster;Trusted_Connection=True;",
            @"Server=(localdb)\MSSQLLocalDB;Database=DB_MasterCrm;Trusted_Connection=True;");
        DatabaseTargetRouting.ResolvePlatformMasterAndConfigure(localCfg, Array.Empty<string>(), _ => null);
        Check(localCfg.GetConnectionString("TenantA")!.Contains("DB_MasterCrm"), "Routing: Local Tenant A accepts DB_MasterCrm");

        // 2. Local rejects prototype RevCare_TenantA, db81189, and cloud host
        foreach (var bad in new[]
        {
            @"Server=(localdb)\MSSQLLocalDB;Database=RevCare_TenantA;Trusted_Connection=True;",
            @"Server=(localdb)\MSSQLLocalDB;Database=db81189;Trusted_Connection=True;",
            @"Server=db80735.public.databaseasp.net,1433;Database=db80735;User Id=u;Password=p;"
        })
        {
            var badCfg = BuildCfg(@"Server=(localdb)\MSSQLLocalDB;Database=RevCare_PlatformMaster;Trusted_Connection=True;", bad);
            try { DatabaseTargetRouting.ResolvePlatformMasterAndConfigure(badCfg, Array.Empty<string>(), _ => null); throw new Exception("Accepted invalid local target: " + bad); }
            catch (InvalidOperationException) { Check(true, "Routing: Local rejects invalid/prototype target"); }
        }

        // 3. Cloud mode accepts db81188 + db80735, enforces TLS, clears TenantB/TenantC
        var cloudEnv = new Dictionary<string, string?>
        {
            ["REVCARE_CLOUD_PLATFORM_CONNECTION"] = "Server=db81188.public.databaseasp.net,1433;Database=db81188;User Id=u;Password=p;",
            ["REVCARE_CLOUD_TENANT_CONNECTION"] = "Server=db80735.public.databaseasp.net,1433;Database=db80735;User Id=u;Password=p;"
        };
        var cloudCfg = BuildCfg(@"Server=(localdb)\MSSQLLocalDB;Database=RevCare_PlatformMaster;Trusted_Connection=True;", @"Server=(localdb)\MSSQLLocalDB;Database=DB_MasterCrm;Trusted_Connection=True;");
        DatabaseTargetRouting.ResolvePlatformMasterAndConfigure(cloudCfg, new[] { "--cloud" }, k => cloudEnv.GetValueOrDefault(k));
        var parsedCloudA = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(cloudCfg.GetConnectionString("TenantA"));
        Check(string.Equals(parsedCloudA.InitialCatalog, "db80735", StringComparison.OrdinalIgnoreCase) && parsedCloudA.Encrypt && !parsedCloudA.TrustServerCertificate,
            "Routing: Cloud Tenant A resolves strictly to db80735 with Encrypt=True, TrustServerCertificate=False");
        Check(cloudCfg.GetConnectionString("TenantB") == null && cloudCfg.GetConnectionString("TenantC") == null,
            "Routing: Cloud mode clears TenantB and TenantC connection strings");

        // 4. Cloud mode rejects db81189, LocalDB fallback, and missing env vars
        foreach (var badCloudTenant in new[]
        {
            "Server=db81189.public.databaseasp.net,1433;Database=db81189;User Id=u;Password=p;",
            @"Server=(localdb)\MSSQLLocalDB;Database=DB_MasterCrm;Trusted_Connection=True;",
            null
        })
        {
            var badEnv = new Dictionary<string, string?>
            {
                ["REVCARE_CLOUD_PLATFORM_CONNECTION"] = cloudEnv["REVCARE_CLOUD_PLATFORM_CONNECTION"],
                ["REVCARE_CLOUD_TENANT_CONNECTION"] = badCloudTenant
            };
            var c = BuildCfg("local", "local");
            try { DatabaseTargetRouting.ResolvePlatformMasterAndConfigure(c, new[] { "--cloud" }, k => badEnv.GetValueOrDefault(k)); throw new Exception("Accepted bad cloud tenant"); }
            catch (InvalidOperationException) { Check(true, "Routing: Cloud mode rejects db81189/LocalDB/missing tenant config"); }
        }
    }

    static async Task AssertReportsDeniedAsync(HttpClient client, HttpStatusCode expected, string actor)
    {
        foreach (string path in new[]
        {
            "/crm/reports/operational-summary",
            "/crm/reports/financial-summary",
            "/crm/reports/workshop-performance",
            "/crm/analytics"
        })
        {
            await Status(await client.GetAsync(path), expected, $"{actor} denied access to {path}");
        }
    }
    static async Task<string> Login(HttpClient client, string username, string password, string? tenantId)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new { username, password, tenantId });
        var body = await Read<JsonElement>(response);
        Check(body.GetProperty("accessToken").GetString()?.Length > 0, "Login issues a bearer access token");
        return body.GetProperty("accessToken").GetString()!;
    }
    static async Task<T> Read<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode) throw new Exception($"HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
    static async Task Status(HttpResponseMessage response, HttpStatusCode expected, string name)
    {
        if (response.StatusCode != expected) throw new Exception($"{name}: expected {expected}, got {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        Check(true, name);
    }
    static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
}
