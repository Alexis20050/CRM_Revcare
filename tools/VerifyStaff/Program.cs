using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRM.api;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using CRM_Revcare_;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VerifyStaff;

class Program
{
    private static int checksPassed = 0;

    [STAThread]
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("    REVCARE CRM - CLOUD TENANT A STAFF ACCOUNTS & RBAC VERIFICATION");
        Console.WriteLine("================================================================================");
        Console.WriteLine();

        using var watchdog = new System.Threading.Timer(_ => { Console.Error.WriteLine("FAIL: Verification exceeded 5 minutes; see last reported check."); Environment.Exit(2); }, null, TimeSpan.FromMinutes(5), Timeout.InfiniteTimeSpan);

        // 1. Build host with existing User Secrets (points to cloud db81188 and db81189)
        using var factory = new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });

        using var scope = factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var masterDb = scope.ServiceProvider.GetRequiredService<PlatformMasterDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var tenantResolver = scope.ServiceProvider.GetRequiredService<ITenantDatabaseResolver>();
        var tenantFactory = scope.ServiceProvider.GetRequiredService<ITenantDbContextFactory>();

        string? initialPassword = config["StaffSeed:LaboratoryPassword"];
        if (string.IsNullOrWhiteSpace(initialPassword)) throw new InvalidOperationException("Configure StaffSeed:LaboratoryPassword in user secrets.");

        // 2. Query Cloud Databases for Baseline Record Counts & Staff Existence
        Console.WriteLine("\n--- 1. CLOUD DATABASE INSPECTION (db81188 Master & db81189 Tenant A) ---");
        await using var tenantDb = await tenantFactory.CreateAsync("tenant-a");

        int initialCustomers = await tenantDb.Customers.CountAsync();
        int initialJobs = await tenantDb.JobOrders.CountAsync();
        int initialTasks = await tenantDb.JobTasks.CountAsync();
        int initialPayments = await tenantDb.Payments.CountAsync();

        Console.WriteLine($"[BASELINE] Cloud Tenant A Data Counts: Customers={initialCustomers}, JobOrders={initialJobs}, JobTasks={initialTasks}, Payments={initialPayments}");

        var expectedStaff = new (string Username, string DisplayName, string Role)[]
        {
            ("advisor", "Service Advisor", "ServiceAdvisor"),
            ("leadmechanic", "Lead Mechanic", "LeadMechanic"),
            ("mechanic", "Automotive Mechanic", "AutomotiveMechanic"),
            ("detailer", "Detailing Technician", "DetailingTechnician")
        };

        foreach (var staff in expectedStaff)
        {
            var user = await userManager.FindByNameAsync(staff.Username);
            Check(user != null, $"Cloud Master AspNetUsers contains user '{staff.Username}' (Id: {user?.Id})");

            var membership = await masterDb.Memberships.SingleOrDefaultAsync(m => m.UserId == user!.Id && m.TenantId == "tenant-a");
            Check(membership != null, $"Cloud Master Memberships contains '{staff.Username}' for tenant-a");
            Check(membership!.Role == staff.Role, $"Membership role for '{staff.Username}' matches '{staff.Role}'");
            Check(membership.IsActive, $"Membership for '{staff.Username}' is Active");
            Check(membership.ProfileProvisioningStatus == "Synced", $"ProfileProvisioningStatus for '{staff.Username}' is 'Synced'");

            var profile = await tenantDb.Staff.FindAsync(user!.Id);
            Check(profile != null, $"Cloud Tenant A Staff table contains profile for '{staff.Username}' (UserId: {user.Id})");
            Check(profile!.DisplayName == staff.DisplayName, $"Tenant A Staff profile DisplayName is '{profile?.DisplayName}'");
        }

        // Check preserved accounts
        var ownerUser = await userManager.FindByNameAsync("owner");
        Check(ownerUser != null, "Cloud Master preserves 'owner' account");
        var superUser = await userManager.FindByNameAsync("superadmin");
        Check(superUser != null, "Cloud Master preserves 'superadmin' account");

        // 3. API Authentication & Token Claim Verification
        Console.WriteLine("\n--- 2. LIVE API AUTHENTICATION & CLAIMS VERIFICATION ---");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        client.Timeout = TimeSpan.FromSeconds(30);
        var tokens = new Dictionary<string, string>();
        var userAuthMe = new Dictionary<string, JsonElement>();

        foreach (var staff in expectedStaff)
        {
            Console.WriteLine($"[RUN] Login: {staff.Username}");
            var loginResp = await client.PostAsJsonAsync("/auth/login", new
            {
                username = staff.Username,
                password = initialPassword,
                tenantId = "tenant-a"
            });

            Check(loginResp.IsSuccessStatusCode, $"Login succeeded for '{staff.Username}' with laboratory password");
            var loginJson = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            string token = loginJson.GetProperty("accessToken").GetString()!;
            Check(!string.IsNullOrWhiteSpace(token), $"Bearer token issued for '{staff.Username}'");
            tokens[staff.Username] = token;

            // Verify /auth/me
            var meReq = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
            meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var meResp = await client.SendAsync(meReq);
            Check(meResp.IsSuccessStatusCode, $"/auth/me succeeded for '{staff.Username}'");
            var meJson = await meResp.Content.ReadFromJsonAsync<JsonElement>();
            userAuthMe[staff.Username] = meJson;

            Check(meJson.GetProperty("role").GetString() == staff.Role, $"/auth/me role for '{staff.Username}' is '{staff.Role}'");
            Check(meJson.GetProperty("tenantId").GetString() == "tenant-a", $"/auth/me tenantId for '{staff.Username}' is 'tenant-a'");
            Check(meJson.GetProperty("status").GetString() == "Ready", $"/auth/me status for '{staff.Username}' is 'Ready'");
            var staffUser = await userManager.FindByNameAsync(staff.Username);
            Check(meJson.GetProperty("userId").GetString() == staffUser!.Id, $"/auth/me userId for '{staff.Username}' matches Master user id");
        }

        // 4. Role-Specific Permission Checks (Allowed & Forbidden Endpoints)
        Console.WriteLine("\n--- 3. ROLE-SPECIFIC API ACCESS CONTROL (RBAC) ---");

        async Task AssertStatus(string username, HttpMethod method, string path, HttpStatusCode expected, string description, object? body = null)
        {
            Console.WriteLine($"[RUN] {username}: {method} {path}");
            var req = new HttpRequestMessage(method, path);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens[username]);
            if (body != null) req.Content = JsonContent.Create(body);
            var resp = await client.SendAsync(req);
            Check(resp.StatusCode == expected, $"[{username}:{staffRole(username)}] {description} -> Expected: {expected}, Got: {resp.StatusCode}");
        }

        string staffRole(string u) => expectedStaff.First(s => s.Username == u).Role;

        // A. ServiceAdvisor
        Console.WriteLine("\n[Role: ServiceAdvisor]");
        await AssertStatus("advisor", HttpMethod.Get, "/crm/customers", HttpStatusCode.OK, "Read customers");
        await AssertStatus("advisor", HttpMethod.Get, "/crm/inquiries", HttpStatusCode.OK, "Read inquiries");
        await AssertStatus("advisor", HttpMethod.Get, "/crm/complaints", HttpStatusCode.OK, "Read complaints");
        await AssertStatus("advisor", HttpMethod.Get, "/crm/feedbacks", HttpStatusCode.OK, "Read feedback");
        await AssertStatus("advisor", HttpMethod.Get, "/crm/job-orders", HttpStatusCode.OK, "Read job orders");
        await AssertStatus("advisor", HttpMethod.Get, "/crm/payments", HttpStatusCode.OK, "Read payments");
        await AssertStatus("advisor", HttpMethod.Get, "/crm/services", HttpStatusCode.OK, "Read services");

        // Forbidden for ServiceAdvisor
        await AssertStatus("advisor", HttpMethod.Get, "/crm/staff", HttpStatusCode.Forbidden, "Cannot read/manage staff");
        await AssertStatus("advisor", HttpMethod.Post, "/crm/staff", HttpStatusCode.Forbidden, "Cannot create staff", new { username = "x", password = "x", displayName = "x", role = "AutomotiveMechanic" });
        await AssertStatus("advisor", HttpMethod.Put, "/crm/job-tasks/2147483647/assignment", HttpStatusCode.Forbidden, "Cannot assign tasks", new { assigneeUserId = "x", remarks = "x", rowVersion = new byte[8] });
        await AssertStatus("advisor", HttpMethod.Post, "/crm/job-tasks/2147483647/review", HttpStatusCode.Forbidden, "Cannot QA approve tasks", new { status = "Approved", remarks = "x", rowVersion = new byte[8] });
        await AssertStatus("advisor", HttpMethod.Post, "/crm/payments/2147483647/void", HttpStatusCode.Forbidden, "Cannot void payments", new { reason = "Unauthorized", rowVersion = new byte[8] });

        // B. LeadMechanic
        Console.WriteLine("\n[Role: LeadMechanic]");
        await AssertStatus("leadmechanic", HttpMethod.Get, "/crm/workshop", HttpStatusCode.OK, "Read workshop overview");
        await AssertStatus("leadmechanic", HttpMethod.Get, "/crm/job-tasks", HttpStatusCode.OK, "Read job tasks");
        await AssertStatus("leadmechanic", HttpMethod.Get, "/crm/services", HttpStatusCode.OK, "Read service catalog");
        await AssertStatus("leadmechanic", HttpMethod.Get, "/crm/workshop/assignment-history", HttpStatusCode.OK, "Read assignment history");

        // Forbidden for LeadMechanic
        await AssertStatus("leadmechanic", HttpMethod.Get, "/crm/customers", HttpStatusCode.Forbidden, "Cannot read customer list");
        await AssertStatus("leadmechanic", HttpMethod.Get, "/crm/inquiries", HttpStatusCode.Forbidden, "Cannot read inquiries");
        await AssertStatus("leadmechanic", HttpMethod.Get, "/crm/complaints", HttpStatusCode.Forbidden, "Cannot read complaints");
        await AssertStatus("leadmechanic", HttpMethod.Get, "/crm/feedbacks", HttpStatusCode.Forbidden, "Cannot read feedback");
        await AssertStatus("leadmechanic", HttpMethod.Get, "/crm/payments", HttpStatusCode.Forbidden, "Cannot read billing/payments");
        await AssertStatus("leadmechanic", HttpMethod.Post, "/crm/payments", HttpStatusCode.Forbidden, "Cannot record payment", new { jobOrderId = int.MaxValue, amountPaid = 100m, paymentType = "Deposit", paymentMethod = "Cash", allowCustomerCredit = false, submissionId = Guid.NewGuid() });
        await AssertStatus("leadmechanic", HttpMethod.Get, "/crm/staff", HttpStatusCode.Forbidden, "Cannot read staff directory");

        // C. AutomotiveMechanic
        Console.WriteLine("\n[Role: AutomotiveMechanic]");
        var mechWorkReq = new HttpRequestMessage(HttpMethod.Get, "/crm/workshop");
        mechWorkReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens["mechanic"]);
        var mechWorkResp = await client.SendAsync(mechWorkReq);
        Check(mechWorkResp.StatusCode == HttpStatusCode.OK, "Mechanic workshop query returns 200 OK");
        var mechWorkJson = await mechWorkResp.Content.ReadFromJsonAsync<JsonElement>();
        Check(mechWorkJson.GetArrayLength() == 0, "Mechanic with no assigned work returns empty state [] (no error)");

        // Forbidden for AutomotiveMechanic
        await AssertStatus("mechanic", HttpMethod.Get, "/crm/job-orders", HttpStatusCode.Forbidden, "Cannot read unrestricted job orders");
        await AssertStatus("mechanic", HttpMethod.Get, "/crm/job-tasks", HttpStatusCode.Forbidden, "Cannot read unrestricted job tasks");
        await AssertStatus("mechanic", HttpMethod.Get, "/crm/customers", HttpStatusCode.Forbidden, "Cannot read customers");
        await AssertStatus("mechanic", HttpMethod.Get, "/crm/payments", HttpStatusCode.Forbidden, "Cannot read payments");
        await AssertStatus("mechanic", HttpMethod.Get, "/crm/staff", HttpStatusCode.Forbidden, "Cannot read staff");
        await AssertStatus("mechanic", HttpMethod.Post, "/crm/job-tasks/2147483647/review", HttpStatusCode.Forbidden, "Cannot QA approve tasks", new { status = "Approved", remarks = "x", rowVersion = new byte[8] });
        await AssertStatus("mechanic", HttpMethod.Put, "/crm/job-tasks/2147483647/assignment", HttpStatusCode.Forbidden, "Cannot assign tasks", new { assigneeUserId = "x", remarks = "x", rowVersion = new byte[8] });

        // D. DetailingTechnician
        Console.WriteLine("\n[Role: DetailingTechnician]");
        var detWorkReq = new HttpRequestMessage(HttpMethod.Get, "/crm/workshop");
        detWorkReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens["detailer"]);
        var detWorkResp = await client.SendAsync(detWorkReq);
        Check(detWorkResp.StatusCode == HttpStatusCode.OK, "Detailer workshop query returns 200 OK");
        var detWorkJson = await detWorkResp.Content.ReadFromJsonAsync<JsonElement>();
        Check(detWorkJson.GetArrayLength() == 0, "Detailer with no assigned work returns empty state [] (no error)");

        // Forbidden for DetailingTechnician
        await AssertStatus("detailer", HttpMethod.Get, "/crm/job-orders", HttpStatusCode.Forbidden, "Cannot read unrestricted job orders");
        await AssertStatus("detailer", HttpMethod.Get, "/crm/job-tasks", HttpStatusCode.Forbidden, "Cannot read unrestricted job tasks");
        await AssertStatus("detailer", HttpMethod.Get, "/crm/customers", HttpStatusCode.Forbidden, "Cannot read customers");
        await AssertStatus("detailer", HttpMethod.Get, "/crm/payments", HttpStatusCode.Forbidden, "Cannot read payments");
        await AssertStatus("detailer", HttpMethod.Get, "/crm/staff", HttpStatusCode.Forbidden, "Cannot read staff");
        await AssertStatus("detailer", HttpMethod.Post, "/crm/job-tasks/2147483647/review", HttpStatusCode.Forbidden, "Cannot QA approve tasks", new { status = "Approved", remarks = "x", rowVersion = new byte[8] });
        await AssertStatus("detailer", HttpMethod.Put, "/crm/job-tasks/2147483647/assignment", HttpStatusCode.Forbidden, "Cannot assign tasks", new { assigneeUserId = "x", remarks = "x", rowVersion = new byte[8] });

        // 5. WinForms UI Navigation Adaptation Verification
        Console.WriteLine("\n--- 4. WINFORMS UI ROLE NAVIGATION VERIFICATION ---");
        RunUiChecks(() => {
        foreach (var staff in expectedStaff)
        {
            // Use FastStubHandler to immediately satisfy UI queries without hanging on modal or network dialogs
            var me = userAuthMe[staff.Username];
            using var api = new TenantApiClient("https://localhost", new FastStubHandler(me));
            Task.Run(() => api.LoginAsync(staff.Username, initialPassword, "tenant-a")).GetAwaiter().GetResult();
            Check(api.Role == staff.Role, $"TenantApiClient initialized with role '{staff.Role}'");

            using var workspace = new TenantWorkspaceForm(api);
            var modulesList = (IList)Field<object>(workspace, "modules");
            var moduleNames = new List<string>();
            foreach (var mod in modulesList)
            {
                moduleNames.Add(mod.GetType().GetProperty("Name")!.GetValue(mod)!.ToString()!);
            }

            Console.WriteLine($"[UI MODULES] User '{staff.Username}' ({staff.Role}) configured modules: [{string.Join(", ", moduleNames)}]");

            if (staff.Role is "AutomotiveMechanic" or "DetailingTechnician")
            {
                Check(moduleNames.Count == 1 && moduleNames[0] == "My Assigned Tasks",
                    $"{staff.Role} navigation displays ONLY 'My Assigned Tasks'");
                Check(!api.Has("Jobs.QA"), $"{staff.Role} does not have Jobs.QA (QA buttons hidden)");
                Check(api.Has("Tasks.UpdateAssigned"), $"{staff.Role} has Tasks.UpdateAssigned permission");
            }
            else if (staff.Role == "ServiceAdvisor")
            {
                Check(moduleNames.Contains("Customers"), "Advisor sees Customers");
                Check(moduleNames.Contains("Inquiries"), "Advisor sees Inquiries");
                Check(moduleNames.Contains("Complaints"), "Advisor sees Complaints");
                Check(moduleNames.Contains("Feedback"), "Advisor sees Feedback");
                Check(moduleNames.Contains("Job Orders"), "Advisor sees Job Orders");
                Check(moduleNames.Contains("Payments / Billing"), "Advisor sees Payments / Billing");
                Check(!moduleNames.Contains("Staff"), "Advisor does NOT see Staff management");
                Check(!moduleNames.Contains("Audit History"), "Advisor does NOT see Audit History");
            }
            else if (staff.Role == "LeadMechanic")
            {
                Check(moduleNames.Contains("Job Tasks"), "Lead Mechanic sees Job Tasks");
                Check(moduleNames.Contains("Services"), "Lead Mechanic sees Services");
                Check(moduleNames.Contains("Assignment History"), "Lead Mechanic sees Assignment History");
                Check(!moduleNames.Contains("Customers"), "Lead Mechanic does NOT see Customers");
                Check(!moduleNames.Contains("Payments / Billing"), "Lead Mechanic does NOT see Payments / Billing");
                Check(!moduleNames.Contains("Staff"), "Lead Mechanic does NOT see Staff management");
                Check(api.Has("Jobs.QA"), "Lead Mechanic has Jobs.QA (can approve QA & rework)");
            }
        }

        });

        // 6. Confirm Zero Mutations to Cloud Business Data
        Console.WriteLine("\n--- 5. CLOUD DATABASE MUTATION AUDIT ---");
        int finalCustomers = await tenantDb.Customers.CountAsync();
        int finalJobs = await tenantDb.JobOrders.CountAsync();
        int finalTasks = await tenantDb.JobTasks.CountAsync();
        int finalPayments = await tenantDb.Payments.CountAsync();

        Check(finalCustomers == initialCustomers, $"Customer record count preserved ({finalCustomers} == {initialCustomers})");
        Check(finalJobs == initialJobs, $"JobOrder record count preserved ({finalJobs} == {initialJobs})");
        Check(finalTasks == initialTasks, $"JobTask record count preserved ({finalTasks} == {initialTasks})");
        Check(finalPayments == initialPayments, $"Payment record count preserved ({finalPayments} == {initialPayments})");

        Console.WriteLine("\n================================================================================");
        Console.WriteLine($"    ALL {checksPassed} VERIFICATION CHECKS PASSED SUCCESSFULLY!");
        Console.WriteLine("================================================================================");
        return 0;
    }

    private static void RunUiChecks(Action checks)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
                Application.EnableVisualStyles();
                checks();
            }
            catch (Exception ex) { failure = ex; }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(30))) throw new TimeoutException("UI verification exceeded 30 seconds.");
        if (failure != null) throw new InvalidOperationException("UI verification failed.", failure);
    }
    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[FAIL] {message}");
            Console.ResetColor();
            throw new Exception($"Verification assertion failed: {message}");
        }
        checksPassed++;
        Console.WriteLine($"[PASS] {message}");
    }

    private sealed class FastStubHandler : HttpMessageHandler
    {
        private readonly JsonElement authMeJson;

        public FastStubHandler(JsonElement authMeJson)
        {
            this.authMeJson = authMeJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri?.AbsolutePath ?? "";
            string body = path switch
            {
                "/auth/login" => "{\"accessToken\":\"stub-token\"}",
                "/auth/me" => authMeJson.GetRawText(),
                _ => "[]"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
