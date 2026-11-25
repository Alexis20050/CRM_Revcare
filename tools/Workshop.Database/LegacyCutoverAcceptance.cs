#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CRM_Revcare_;
using CRM.infrastructure.data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Workshop.Database;

internal static class LegacyCutoverAcceptance
{
    private const string LocalDbServer = @"(localdb)\MSSQLLocalDB";

    public static async Task RunAsync(string repoRoot)
    {
        string guid = Guid.NewGuid().ToString("N");
        string masterConnStr = $@"Server={LocalDbServer};Database=master;Trusted_Connection=True;TrustServerCertificate=True";
        string legacyConnStr = $@"Server={LocalDbServer};Database=DB_MasterCrm;Trusted_Connection=True;TrustServerCertificate=True";
        string platformMasterConnStr = $@"Server={LocalDbServer};Database=RevCare_PlatformMaster;Trusted_Connection=True;TrustServerCertificate=True";

        string restoredDbName = $"RevCare_LegacyRestore_{guid}";
        string acceptMasterDbName = $"RevCare_MasterAccept_{guid}";
        string restoredConnStr = $@"Server={LocalDbServer};Database={restoredDbName};Trusted_Connection=True;TrustServerCertificate=True";
        string acceptMasterConnStr = $@"Server={LocalDbServer};Database={acceptMasterDbName};Trusted_Connection=True;TrustServerCertificate=True";

        string tempDir = Path.Combine(Path.GetTempPath(), $"revcare_cutover_{guid}");
        Directory.CreateDirectory(tempDir);
        string bakPath = Path.Combine(tempDir, $"DB_MasterCrm_{guid}.bak");

        Process? apiProcess = null;
        try
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("  REVCARE CRM — LEGACY HISTORICAL PRESERVATION & DESKTOP ACCEPTANCE PROOF");
            Console.WriteLine("================================================================================");

            // Capture protected baselines read-only before starting
            var legacyBaselineBefore = await CaptureCoreProtectedBaselineAsync(legacyConnStr, "DB_MasterCrm");
            var masterBaselineBefore = await CaptureProtectedMasterBaselineAsync(platformMasterConnStr);

            // 1. Native SQL Server COPY_ONLY Backup + VERIFYONLY + RESTORE WITH MOVE
            Console.WriteLine($"\n[1] Creating COPY_ONLY native backup of DB_MasterCrm -> {bakPath}");
            await ExecuteNonQueryAsync(masterConnStr,
                $"BACKUP DATABASE [DB_MasterCrm] TO DISK = N'{EscapeSqlLiteral(bakPath)}' WITH COPY_ONLY, FORMAT, INIT;");
            string bakSha256 = await ComputeFileSha256Async(bakPath);
            long bakBytes = new FileInfo(bakPath).Length;
            Console.WriteLine($"    Backup SHA-256: {bakSha256} ({bakBytes:N0} bytes)");

            await ExecuteNonQueryAsync(masterConnStr, $"RESTORE VERIFYONLY FROM DISK = N'{EscapeSqlLiteral(bakPath)}';");
            var headerInfo = await ReadHeaderAndFileListAsync(masterConnStr, bakPath);

            Console.WriteLine($"[2] Restoring native .bak into disposable database [{restoredDbName}]...");
            string logicalData = headerInfo.DataLogicalName;
            string logicalLog = headerInfo.LogLogicalName;
            string dataPath = Path.Combine(tempDir, $"{restoredDbName}.mdf");
            string logPath = Path.Combine(tempDir, $"{restoredDbName}_log.ldf");
            await ExecuteNonQueryAsync(masterConnStr, $@"
RESTORE DATABASE [{restoredDbName}] FROM DISK = N'{EscapeSqlLiteral(bakPath)}'
WITH MOVE N'{EscapeSqlLiteral(logicalData)}' TO N'{EscapeSqlLiteral(dataPath)}',
     MOVE N'{EscapeSqlLiteral(logicalLog)}' TO N'{EscapeSqlLiteral(logPath)}',
     REPLACE;");

            // 2. Capture deterministic PK-linked pre-migration snapshot (all user tables)
            Console.WriteLine("[3] Capturing deterministic pre-migration PK-linked table fingerprints...");
            var preSnapshot = await CaptureDatabaseFingerprintAsync(restoredConnStr);

            // 3. Apply MasterCrmDbContext EF Migrations cleanly (NO ConfigureWarnings suppression)
            Console.WriteLine("[4] Applying MasterCrmDbContext migrations without warning suppression...");
            List<string> pendingBefore;
            List<string> pendingAfter;
            bool hasPendingModelChanges;
            await using (var migrationDb = new MasterCrmDbContext(new DbContextOptionsBuilder<MasterCrmDbContext>()
                .UseSqlServer(restoredConnStr).Options))
            {
                pendingBefore = (await migrationDb.Database.GetPendingMigrationsAsync()).ToList();
                Console.WriteLine($"    Pending migrations before MigrateAsync: {(pendingBefore.Count == 0 ? "(none)" : string.Join(", ", pendingBefore))}");
                await migrationDb.Database.MigrateAsync();
                pendingAfter = (await migrationDb.Database.GetPendingMigrationsAsync()).ToList();
                hasPendingModelChanges = migrationDb.Database.HasPendingModelChanges();
            }
            AssertTrue(pendingAfter.Count == 0, "Zero pending MasterCrmDbContext migrations remain after MigrateAsync.");
            AssertTrue(!hasPendingModelChanges, "MasterCrmDbContext reports HasPendingModelChanges() == false.");

            // 4. Capture deterministic post-migration snapshot & compare per-table
            Console.WriteLine("[5] Capturing deterministic post-migration fingerprints & comparing record-by-record...");
            var postSnapshot = await CaptureDatabaseFingerprintAsync(restoredConnStr);
            var tableComparisons = CompareSnapshots(preSnapshot, postSnapshot);

            foreach (var c in tableComparisons)
            {
                if (string.Equals(c.TableName, "__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
                {
                    AssertTrue(c.PostRowCount == c.PreRowCount + pendingBefore.Count,
                        $"__EFMigrationsHistory appended exactly {pendingBefore.Count} applied migrations ({c.PreRowCount} -> {c.PostRowCount})");
                    AssertTrue(c.MismatchedPkCount == 0, "All pre-existing __EFMigrationsHistory rows preserved identically");
                    continue;
                }
                AssertTrue(c.PreRowCount == c.PostRowCount, $"Row count preserved on {c.TableName}: {c.PreRowCount} == {c.PostRowCount}");
                AssertTrue(c.PreCommonColumnsSha256 == c.PostCommonColumnsSha256,
                    $"Deterministic business-column PK digest preserved on {c.TableName}");
                AssertTrue(c.MismatchedPkCount == 0, $"Zero per-PK hash mismatches on {c.TableName}");
            }

            // Historical non-fabrication & financial preservation assertions
            var finPre = preSnapshot.Financials;
            var finPost = postSnapshot.Financials;
            AssertTrue(finPre.ValidPaymentsCount == finPost.ValidPaymentsCount && finPre.ValidPaymentsSum == finPost.ValidPaymentsSum,
                $"Valid payments preserved ({finPost.ValidPaymentsCount} rows, PHP {finPost.ValidPaymentsSum:N2})");
            AssertTrue(finPre.VoidedPaymentsCount == finPost.VoidedPaymentsCount && finPre.VoidedPaymentsSum == finPost.VoidedPaymentsSum,
                $"Voided payments preserved ({finPost.VoidedPaymentsCount} rows, PHP {finPost.VoidedPaymentsSum:N2})");
            AssertTrue(finPre.JobTasksAgreedPriceNullCount == finPost.JobTasksAgreedPriceNullCount && finPre.JobTasksAgreedPriceSum == finPost.JobTasksAgreedPriceSum,
                $"JobTasks AgreedPrice NULL count & sum unchanged (NULLs={finPost.JobTasksAgreedPriceNullCount})");
            AssertTrue(finPre.JobOrdersApprovedEstimateNullCount == finPost.JobOrdersApprovedEstimateNullCount,
                $"JobOrders ApprovedEstimateId NULL count unchanged (NULLs={finPost.JobOrdersApprovedEstimateNullCount})");
            AssertTrue(finPre.PaymentPayerTypeJson == finPost.PaymentPayerTypeJson && finPre.PaymentPurposeJson == finPost.PaymentPurposeJson,
                "Payment PayerType and PaymentPurpose distributions unchanged");

            int servicesCount = await ScalarIntAsync(restoredConnStr, "SELECT COUNT(*) FROM dbo.Services");
            int tenantServicesCount = await ScalarIntAsync(restoredConnStr, "SELECT COUNT(*) FROM dbo.TenantServices");
            int serviceMatches = await ScalarIntAsync(restoredConnStr, @"
                SELECT COUNT(*) FROM dbo.Services s
                INNER JOIN dbo.TenantServices ts ON ts.Id = s.ServiceId
                WHERE ts.Name = s.Name AND ts.Price = s.CurrentPrice AND ts.IsActive = s.IsActive");
            AssertTrue(servicesCount == tenantServicesCount && servicesCount == serviceMatches,
                $"TenantServices non-destructive bridge matches legacy Services 1:1 ({servicesCount} rows)");

            int constraintViolations = await CountConstraintViolationsAsync(restoredConnStr);
            AssertTrue(constraintViolations == 0, $"DBCC CHECKCONSTRAINTS reports 0 violations (actual={constraintViolations})");

            // 5. Materialize ALL 23 TenantCrmDbContext DbSets with UseLegacyCatalogTables + run column checks
            Console.WriteLine("[6] Verifying TenantCrmDbContext materialization of all mapped DbSets & relational columns...");
            var materializedCounts = await VerifyTenantDbContextMaterializationAsync(restoredConnStr);

            // Persist Task 6 JSON + Markdown artifacts (zero raw PII)
            string proofJsonPath = Path.Combine(repoRoot, "artifacts", "LEGACY_HISTORICAL_PRESERVATION_PROOF.json");
            string proofMdPath = Path.Combine(repoRoot, "artifacts", "LEGACY_HISTORICAL_PRESERVATION_REPORT.md");
            WritePreservationArtifacts(proofJsonPath, proofMdPath, new
            {
                GeneratedAtUtc = DateTime.UtcNow,
                SourceDatabase = "DB_MasterCrm",
                DisposableRestoredDatabase = restoredDbName,
                NativeBackup = new
                {
                    FileName = Path.GetFileName(bakPath),
                    SizeBytes = bakBytes,
                    Sha256 = bakSha256,
                    VerifyOnlyPassed = true,
                    LogicalDataFile = logicalData,
                    LogicalLogFile = logicalLog
                },
                Migrations = new
                {
                    PendingBeforeMigrate = pendingBefore,
                    PendingAfterMigrate = pendingAfter,
                    HasPendingModelChanges = hasPendingModelChanges
                },
                ConstraintViolations = constraintViolations,
                FinancialAndFabricationChecks = new
                {
                    Before = finPre,
                    After = finPost,
                    ServicesBridgeMatched = serviceMatches,
                    ServicesCount = servicesCount,
                    TenantServicesCount = tenantServicesCount
                },
                TableComparisons = tableComparisons,
                PostOnlyTables = postSnapshot.Tables.Keys
                    .Where(k => !preSnapshot.Tables.ContainsKey(k))
                    .Select(k => new { Table = k, RowCount = postSnapshot.Tables[k].RowCount, TableSha256 = postSnapshot.Tables[k].CommonSha256 })
                    .ToList(),
                TenantCrmDbContextMaterializedCounts = materializedCounts
            }, tableComparisons, finPost, materializedCounts, bakSha256, bakBytes);
            Console.WriteLine($"    Persisted proof artifacts: {proofJsonPath} & {proofMdPath}");

            // 6. Task 7 — Authenticated Desktop Acceptance against live CRM.api + restored aligned disposable DB
            Console.WriteLine("\n[7] Provisioning disposable Master & initializing via real CRM.api.dll...");
            string apiDll = Path.Combine(repoRoot, "CRM.api", "bin", "Release", "net10.0", "CRM.api.dll");
            if (!File.Exists(apiDll)) apiDll = Path.Combine(repoRoot, "CRM.api", "bin", "Debug", "net10.0", "CRM.api.dll");
            string desktopExe = Path.Combine(repoRoot, "CRM_Revcare", "bin", "Release", "net10.0-windows", "CRM_Revcare.exe");
            if (!File.Exists(desktopExe)) desktopExe = Path.Combine(repoRoot, "CRM_Revcare", "bin", "Debug", "net10.0-windows", "CRM_Revcare.exe");
            AssertTrue(File.Exists(apiDll), $"Found built CRM.api.dll: {apiDll}");
            AssertTrue(File.Exists(desktopExe), $"Found built CRM_Revcare.exe: {desktopExe}");

            var acceptEnv = new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ConnectionStrings__PlatformMaster"] = acceptMasterConnStr,
                ["ConnectionStrings__TenantA"] = restoredConnStr,
                ["REVCARE_MASTER_CONNECTION_STRING"] = restoredConnStr,
                ["Bootstrap__SuperAdminUsername"] = "superadmin",
                ["Bootstrap__SuperAdminPassword"] = "Test-only!Super2026",
                ["Bootstrap__OwnerUsername"] = "owner",
                ["Bootstrap__OwnerPassword"] = "Test-only!Owner2026",
                ["StaffSeed__LaboratoryPassword"] = "Test-only!Staff2026",
                ["StaffSeed__InitialPassword"] = "Test-only!Staff2026"
            };

            await RunDotNetExecAsync(apiDll, "--initialize", repoRoot, acceptEnv);
            await RunDotNetExecAsync(apiDll, "--provision-tenant-a-staff", repoRoot, acceptEnv);
            await ExecuteNonQueryAsync(acceptMasterConnStr,
                $"UPDATE Tenants SET ServerName = N'{LocalDbServer}', DatabaseName = N'{restoredDbName}' WHERE TenantId = N'tenant-a';");

            int port = GetFreeTcpPort();
            string apiUrl = $"http://127.0.0.1:{port}";
            Console.WriteLine($"[8] Launching live CRM.api on {apiUrl} against [{acceptMasterDbName}] + [{restoredDbName}]...");
            apiProcess = StartApiProcess(apiDll, apiUrl, repoRoot, acceptEnv);
            await WaitForApiReadyAsync(apiUrl);

            // Drive real WinForms TenantWorkspaceForm + CustomerProfileDialogForm + live API on STA thread
            Console.WriteLine("[9] Executing STA WinForms Desktop Acceptance (7-Module Historical Grids, OwnerManager & ServiceAdvisor)...");
            var gridBaseline = await CaptureHistoricalGridBaselineAsync(restoredConnStr);
            RunOnStaThread(() => RunDesktopAcceptanceStaAsync(apiUrl, restoredConnStr, finPost, gridBaseline));

            // Launch actual CRM_Revcare.exe --tenant pointing at live CRM.api to confirm clean GUI startup
            Console.WriteLine("[10] Launching CRM_Revcare.exe --tenant against live CRM.api...");
            await VerifyDesktopProcessLaunchesCleanlyAsync(desktopExe, apiUrl);

            // Re-verify protected operational databases remain untouched
            var legacyBaselineAfter = await CaptureCoreProtectedBaselineAsync(legacyConnStr, "DB_MasterCrm");
            var masterBaselineAfter = await CaptureProtectedMasterBaselineAsync(platformMasterConnStr);
            AssertTrue(legacyBaselineBefore == legacyBaselineAfter, "Protected DB_MasterCrm unchanged after full acceptance suite.");
            AssertTrue(masterBaselineBefore == masterBaselineAfter, "Protected RevCare_PlatformMaster unchanged after full acceptance suite.");

            Console.WriteLine("\n================================================================================");
            Console.WriteLine("PASS: HISTORICAL PRESERVATION & AUTHENTICATED DESKTOP ACCEPTANCE COMPLETE");
            Console.WriteLine("================================================================================");
        }
        finally
        {
            if (apiProcess != null && !apiProcess.HasExited)
            {
                try { apiProcess.Kill(entireProcessTree: true); } catch { }
                apiProcess.Dispose();
            }
            await DropDatabaseIfExistsAsync(masterConnStr, restoredDbName);
            await DropDatabaseIfExistsAsync(masterConnStr, acceptMasterDbName);
            try { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    private sealed record HistoricalGridBaseline(
        int CustomerId, string CustomerSearchName,
        int InquiryId, string InquiryPlatform, string InquiryStatus,
        int ComplaintId, string ComplaintType, string ComplaintResolution,
        int FeedbackId, int FeedbackRating,
        int JobOrderId, string JobStatus,
        int JobTaskId, string TaskStatus,
        int PaymentId, decimal PaymentAmount, string PaymentMethod,
        string TenantIdentity);

    private static async Task<HistoricalGridBaseline> CaptureHistoricalGridBaselineAsync(string connStr)
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        async Task<T> Scalar<T>(string sql)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return (T)Convert.ChangeType(await cmd.ExecuteScalarAsync(), typeof(T))!;
        }
        int custId = await Scalar<int>("SELECT TOP 1 CustomerId FROM dbo.Customers WHERE IsActive = 1 AND LEN(FullName) >= 3 ORDER BY CustomerId DESC");
        string custName = await Scalar<string>($"SELECT FullName FROM dbo.Customers WHERE CustomerId = {custId}");
        int inqId = await Scalar<int>("SELECT TOP 1 InquiryId FROM dbo.Inquiries WHERE IsActive = 1 ORDER BY CreatedAt DESC");
        string inqPlat = await Scalar<string>($"SELECT InquiryPlatform FROM dbo.Inquiries WHERE InquiryId = {inqId}");
        string inqStat = await Scalar<string>($"SELECT Status FROM dbo.Inquiries WHERE InquiryId = {inqId}");
        int compId = await Scalar<int>("SELECT TOP 1 ComplaintId FROM dbo.Complaints WHERE IsActive = 1 ORDER BY CreatedAt DESC");
        string compType = await Scalar<string>($"SELECT ComplaintType FROM dbo.Complaints WHERE ComplaintId = {compId}");
        string compRes = await Scalar<string>($"SELECT ResolutionStatus FROM dbo.Complaints WHERE ComplaintId = {compId}");
        int fbId = await Scalar<int>("SELECT TOP 1 FeedbackId FROM dbo.Feedbacks WHERE IsActive = 1 ORDER BY CreatedAt DESC");
        int fbRate = await Scalar<int>($"SELECT RatingScore FROM dbo.Feedbacks WHERE FeedbackId = {fbId}");
        int jobId = await Scalar<int>("SELECT TOP 1 JobOrderId FROM dbo.JobOrders WHERE IsArchived = 0 AND EXISTS (SELECT 1 FROM dbo.JobTasks t WHERE t.JobOrderId = dbo.JobOrders.JobOrderId) ORDER BY JobOrderId DESC");
        string jobStatus = await Scalar<string>($"SELECT Status FROM dbo.JobOrders WHERE JobOrderId = {jobId}");
        int taskId = await Scalar<int>($"SELECT TOP 1 JobTaskId FROM dbo.JobTasks WHERE JobOrderId = {jobId} ORDER BY JobTaskId");
        string taskStatus = await Scalar<string>($"SELECT Status FROM dbo.JobTasks WHERE JobTaskId = {taskId}");
        int payId = await Scalar<int>("SELECT TOP 1 PaymentId FROM dbo.Payments WHERE IsVoided = 0 ORDER BY PaymentId DESC");
        decimal payAmt = await Scalar<decimal>($"SELECT AmountPaid FROM dbo.Payments WHERE PaymentId = {payId}");
        string payMeth = await Scalar<string>($"SELECT PaymentMethod FROM dbo.Payments WHERE PaymentId = {payId}");
        string tenantId = await Scalar<string>("SELECT TOP 1 TenantId FROM dbo.TenantDatabaseIdentity WHERE Id = 1");
        return new HistoricalGridBaseline(custId, custName, inqId, inqPlat, inqStat, compId, compType, compRes, fbId, fbRate, jobId, jobStatus, taskId, taskStatus, payId, payAmt, payMeth, tenantId);
    }

    private static async Task RunDesktopAcceptanceStaAsync(string apiUrl, string restoredConnStr, FinancialBaseline expectedBaseline, HistoricalGridBaseline expectedGrid)
    {
        AssertTrue(expectedGrid.TenantIdentity == "tenant-a", $"Restored database TenantDatabaseIdentity confirmed as '{expectedGrid.TenantIdentity}'");

        // A. OwnerManager login & WinForms workspace
        using (var ownerApi = new TenantApiClient(apiUrl))
        {
            ownerApi.CompanyName = "RevCare Auto Hub";
            AssertTrue(await ownerApi.LoginAsync("owner", "Test-only!Owner2026", "tenant-a"), "OwnerManager authenticated via live CRM.api");
            AssertTrue(ownerApi.Has("Reports.Read"), "OwnerManager receives Reports.Read permission");

            using var workspace = new TenantWorkspaceForm(ownerApi);
            workspace.Show();
            await WaitUntilWorkspaceIdleAsync(workspace);

            var nav = GetPrivateField<FlowLayoutPanel>(workspace, "navigation");
            var visibleMenus = nav.Controls.OfType<RadioButton>().Where(r => r.Visible).ToList();
            AssertTrue(visibleMenus.Any(r => r.Text == "Operational Reports"),
                "OwnerManager UI navigation displays Operational Reports");

            var records = GetPrivateField<DataGridView>(workspace, "records");
            var jobsOverview = GetPrivateField<DataGridView>(workspace, "jobsOverview");
            var search = GetPrivateField<TextBox>(workspace, "search");
            var btnSearch = GetPrivateField<Button>(workspace, "btnSearch");
            var clearSearch = GetPrivateField<Button>(workspace, "clearSearch");
            var refreshBtn = GetPrivateField<Button>(workspace, "refresh");

            void VerifyGridHistoricalRecord(DataGridView grid, string moduleLabel, string keyValue, params (string Col, string ExpectedSubstr)[] checks)
            {
                var keys = new HashSet<string>(StringComparer.Ordinal);
                DataGridViewRow? matchedRow = null;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    string k = row.Cells["__key"]?.Value?.ToString() ?? "";
                    if (!keys.Add(k)) throw new InvalidOperationException($"{moduleLabel} grid contains duplicate PK '{k}'.");
                    if (k == keyValue) matchedRow = row;
                }
                AssertTrue(matchedRow != null, $"{moduleLabel} grid displays historical record #{keyValue} ({grid.Rows.Count} unique rows)");
                foreach (var (col, expectedSubstr) in checks)
                {
                    AssertTrue(grid.Columns.Contains(col), $"{moduleLabel} grid contains column '{col}'");
                    string actual = matchedRow!.Cells[col]?.Value?.ToString() ?? "";
                    string displayActual = string.Equals(col, "customer", StringComparison.OrdinalIgnoreCase) ? "<redacted-customer>" : actual;
                    AssertTrue(actual.Contains(expectedSubstr, StringComparison.OrdinalIgnoreCase),
                        $"{moduleLabel} historical #{keyValue} column '{col}' matches expected '{expectedSubstr}' (actual='{displayActual}')");
                }
            }

            void VerifyGridSearchAndSort(DataGridView grid, string moduleLabel, string queryToken, string expectedKey, bool redactToken = false)
            {
                int beforeCount = grid.Rows.Count;
                search.Text = queryToken;
                btnSearch.PerformClick();
                PumpEvents(100);
                bool found = grid.Rows.Cast<DataGridViewRow>().Any(r => (r.Cells["__key"]?.Value?.ToString() ?? "") == expectedKey);
                string labelToken = redactToken ? "<redacted-name-token>" : $"'{queryToken}'";
                AssertTrue(found && grid.Rows.Count <= beforeCount, $"{moduleLabel} search for {labelToken} returns historical #{expectedKey}");
                clearSearch.PerformClick();
                PumpEvents(100);
                AssertTrue(grid.Rows.Count == beforeCount, $"{moduleLabel} clear search restores {beforeCount} rows");
                if (grid.Columns.Count > 1)
                    grid.Sort(grid.Columns[1], System.ComponentModel.ListSortDirection.Descending);
            }

            // 1. Customers & Vehicles
            VerifyGridHistoricalRecord(records, "Customers", expectedGrid.CustomerId.ToString());
            VerifyGridSearchAndSort(records, "Customers", expectedGrid.CustomerSearchName, expectedGrid.CustomerId.ToString(), redactToken: true);

            // 2. Leads & Inquiries
            ClickMenu(visibleMenus, "Leads & Inquiries");
            await WaitUntilWorkspaceIdleAsync(workspace);
            VerifyGridHistoricalRecord(records, "Inquiries", expectedGrid.InquiryId.ToString(),
                ("inquiryPlatform", expectedGrid.InquiryPlatform), ("status", expectedGrid.InquiryStatus));
            VerifyGridSearchAndSort(records, "Inquiries", expectedGrid.InquiryPlatform, expectedGrid.InquiryId.ToString());

            // 3. Complaints & Warranty
            ClickMenu(visibleMenus, "Complaints & Warranty");
            await WaitUntilWorkspaceIdleAsync(workspace);
            VerifyGridHistoricalRecord(records, "Complaints", expectedGrid.ComplaintId.ToString(),
                ("complaintType", expectedGrid.ComplaintType), ("resolutionStatus", expectedGrid.ComplaintResolution));
            VerifyGridSearchAndSort(records, "Complaints", expectedGrid.ComplaintType, expectedGrid.ComplaintId.ToString());

            // 4. Feedback & CSAT
            ClickMenu(visibleMenus, "Feedback & CSAT");
            await WaitUntilWorkspaceIdleAsync(workspace);
            VerifyGridHistoricalRecord(records, "Feedback", expectedGrid.FeedbackId.ToString(),
                ("ratingScore", expectedGrid.FeedbackRating.ToString()));

            // 5. Job Orders
            ClickMenu(visibleMenus, "Job Orders");
            await WaitUntilWorkspaceIdleAsync(workspace);
            VerifyGridHistoricalRecord(records, "JobOrders", expectedGrid.JobOrderId.ToString(),
                ("status", expectedGrid.JobStatus), ("customer", ""));
            AssertTrue(!string.IsNullOrWhiteSpace(records.Rows.Cast<DataGridViewRow>().First(r => r.Cells["__key"].Value?.ToString() == expectedGrid.JobOrderId.ToString()).Cells["customer"].Value?.ToString()),
                "JobOrders grid resolves Customer relationship name");

            // 6. Job Tasks & QA (select historical JobOrder in jobsOverview to load its JobTasks)
            ClickMenu(visibleMenus, "Job Tasks & QA");
            await WaitUntilWorkspaceIdleAsync(workspace);
            int jobRowIdx = -1;
            for (int i = 0; i < jobsOverview.Rows.Count; i++)
            {
                if (jobsOverview.Rows[i].Cells["Job"]?.Value?.ToString() == expectedGrid.JobOrderId.ToString())
                {
                    jobRowIdx = i;
                    break;
                }
            }
            AssertTrue(jobRowIdx >= 0, $"Job Tasks & QA overview lists historical JobOrder #{expectedGrid.JobOrderId}");
            jobsOverview.ClearSelection();
            jobsOverview.Rows[jobRowIdx].Selected = true;
            jobsOverview.CurrentCell = jobsOverview.Rows[jobRowIdx].Cells[0];
            PumpEvents(150);
            VerifyGridHistoricalRecord(records, "JobTasks", expectedGrid.JobTaskId.ToString(),
                ("status", expectedGrid.TaskStatus));

            // 7. Billing & Payments
            ClickMenu(visibleMenus, "Billing & Payments");
            await WaitUntilWorkspaceIdleAsync(workspace);
            VerifyGridHistoricalRecord(records, "Payments", expectedGrid.PaymentId.ToString(),
                ("paymentMethod", expectedGrid.PaymentMethod));
            VerifyGridSearchAndSort(records, "Payments", expectedGrid.PaymentMethod, expectedGrid.PaymentId.ToString());

            // Create fictional demonstration Inquiry, Complaint, Feedback via API and verify they load in UI grids
            var inqRes = await ownerApi.Send("POST", "crm/inquiries", new JsonObject
            {
                ["customerId"] = expectedGrid.CustomerId,
                ["customerName"] = "Demo Customer Probe",
                ["contactNumber"] = "09170009999",
                ["vehicleDetails"] = "2023 Demo Sedan",
                ["inquiryPlatform"] = "Walk-in",
                ["messageDetails"] = "Desktop acceptance demo inquiry",
                ["status"] = "Unanswered"
            });
            int demoInqId = inqRes!["inquiryId"]!.GetValue<int>();

            var compRes = await ownerApi.Send("POST", "crm/complaints", new JsonObject
            {
                ["customerId"] = expectedGrid.CustomerId,
                ["jobOrderId"] = expectedGrid.JobOrderId,
                ["complaintType"] = "Service Quality",
                ["description"] = "Desktop acceptance demo complaint",
                ["isUrgent"] = false,
                ["resolutionStatus"] = "Open",
                ["actionTaken"] = ""
            });
            int demoCompId = compRes!["complaintId"]!.GetValue<int>();

            var fbRes = await ownerApi.Send("POST", "crm/feedbacks", new JsonObject
            {
                ["jobOrderId"] = expectedGrid.JobOrderId,
                ["ratingScore"] = 5,
                ["positiveRemarks"] = "Desktop acceptance demo feedback",
                ["areasForImprovement"] = "None"
            });
            int demoFbId = fbRes!["feedbackId"]!.GetValue<int>();

            ClickMenu(visibleMenus, "Leads & Inquiries");
            await WaitUntilWorkspaceIdleAsync(workspace);
            refreshBtn.PerformClick();
            await WaitUntilWorkspaceIdleAsync(workspace);
            VerifyGridHistoricalRecord(records, "Inquiries (Demo Refresh)", demoInqId.ToString());

            // Confirm persistence directly in restored disposable SQL DB
            AssertTrue(await ScalarIntAsync(restoredConnStr, $"SELECT COUNT(*) FROM dbo.Inquiries WHERE InquiryId = {demoInqId}") == 1,
                "Demo Inquiry persisted to restored SQL database");
            AssertTrue(await ScalarIntAsync(restoredConnStr, $"SELECT COUNT(*) FROM dbo.Complaints WHERE ComplaintId = {demoCompId}") == 1,
                "Demo Complaint persisted to restored SQL database");
            AssertTrue(await ScalarIntAsync(restoredConnStr, $"SELECT COUNT(*) FROM dbo.Feedbacks WHERE FeedbackId = {demoFbId}") == 1,
                "Demo Feedback persisted to restored SQL database");

            // Open Customer 360 dialog for sampleCustomerId against restored disposable DB
            using (var profile = new CustomerProfileDialogForm(expectedGrid.CustomerId, () => new MasterCrmDbContext(
                new DbContextOptionsBuilder<MasterCrmDbContext>().UseSqlServer(restoredConnStr).Options)))
            {
                profile.Show();
                PumpEvents(250);
                AssertTrue(profile.Text.Contains("Customer 360"), "Customer 360 dialog rendered cleanly");
                profile.Close();
            }

            // OwnerManager reads Operational Summary matching restored financial totals
            var opSummary = await ownerApi.Send("GET", "crm/reports/operational-summary");
            decimal gross = opSummary!["financialSummary"]!["grossCollections"]!.GetValue<decimal>();
            AssertTrue(gross == expectedBaseline.ValidPaymentsSum,
                $"OwnerManager operational summary grossCollections matches restored baseline (PHP {gross:N2})");

            // Trigger logout on UI & verify session cleared cleanly
            var logoutBtn = FindControlByText<Button>(workspace, "Logout") ?? FindControlByText<Button>(workspace, "Sign Out");
            AssertTrue(logoutBtn != null, "Logout button exists in TenantWorkspaceForm");
            logoutBtn!.PerformClick();
            PumpEvents(100);
            AssertTrue(workspace.LogoutRequested, "OwnerManager Logout sets LogoutRequested and clears session");
        }

        // B. ServiceAdvisor login & report exclusion on WinForms + live API
        using (var advisorApi = new TenantApiClient(apiUrl))
        {
            advisorApi.CompanyName = "RevCare Auto Hub";
            AssertTrue(await advisorApi.LoginAsync("advisor", "Test-only!Staff2026", "tenant-a"), "ServiceAdvisor authenticated via live CRM.api");
            AssertTrue(!advisorApi.Has("Reports.Read"), "ServiceAdvisor does NOT receive Reports.Read");

            using var workspace = new TenantWorkspaceForm(advisorApi);
            workspace.Show();
            PumpEvents(250);

            var nav = GetPrivateField<FlowLayoutPanel>(workspace, "navigation");
            var visibleMenus = nav.Controls.OfType<RadioButton>().Where(r => r.Visible).ToList();
            AssertTrue(!visibleMenus.Any(r => r.Text == "Operational Reports"),
                "ServiceAdvisor UI navigation hides Operational Reports");
            AssertTrue(visibleMenus.Any(r => r.Text == "Leads & Inquiries") && visibleMenus.Any(r => r.Text == "Customers & Vehicles"),
                "ServiceAdvisor retains legitimate CRM module navigation");

            // Verify API returns 403 Forbidden for all report routes directly via raw HTTP
            using var rawHttp = new HttpClient { BaseAddress = new Uri(apiUrl) };
            var tokObj = JsonNode.Parse(await (await rawHttp.PostAsync("/auth/login",
                new StringContent("{\"username\":\"advisor\",\"password\":\"Test-only!Staff2026\",\"tenantId\":\"tenant-a\"}", Encoding.UTF8, "application/json")))
                .Content.ReadAsStringAsync());
            string token = tokObj!["accessToken"]!.GetValue<string>();
            rawHttp.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            foreach (string path in new[] { "/crm/reports/operational-summary", "/crm/reports/financial-summary", "/crm/reports/workshop-performance", "/crm/analytics" })
            {
                var r = await rawHttp.GetAsync(path);
                AssertTrue(r.StatusCode == HttpStatusCode.Forbidden, $"ServiceAdvisor HTTP {path} returns 403 Forbidden (actual={r.StatusCode})");
            }
            workspace.Close();
        }
    }

    private static async Task<Dictionary<string, int>> VerifyTenantDbContextMaterializationAsync(string restoredConnStr)
    {
        var opts = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(restoredConnStr)
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantCrmModelCacheKeyFactory>()
            .UseLegacyCatalogTables()
            .Options;

        await using var db = new TenantCrmDbContext(opts);
        // TenantCrmDbContext(UseLegacyCatalogTables) bridges to MasterCrmDbContext-migrated Legacy DBs (TenantStaff/TenantServices).
        // Verify all mapped relational columns exist directly in SQL Server and every DbSet materializes cleanly.

        // Same relational schema column verification used at runtime by TenantDbContextFactory
        await db.Database.OpenConnectionAsync();
        try
        {
            foreach (var table in db.Model.GetRelationalModel().Tables)
            {
                static string Q(string v) => "[" + v.Replace("]", "]]") + "]";
                await using var cmd = db.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT TOP (0) " + string.Join(",", table.Columns.Select(c => Q(c.Name)))
                    + " FROM " + Q(table.Schema ?? "dbo") + "." + Q(table.Name);
                await using var reader = await cmd.ExecuteReaderAsync();
            }
        }
        finally { await db.Database.CloseConnectionAsync(); }

        return new Dictionary<string, int>
        {
            ["Customers"] = (await db.Customers.AsNoTracking().ToListAsync()).Count,
            ["Vehicles"] = (await db.Vehicles.AsNoTracking().ToListAsync()).Count,
            ["Inquiries"] = (await db.Inquiries.AsNoTracking().ToListAsync()).Count,
            ["Complaints"] = (await db.Complaints.AsNoTracking().ToListAsync()).Count,
            ["Feedbacks"] = (await db.Feedbacks.AsNoTracking().ToListAsync()).Count,
            ["Appointments"] = (await db.Appointments.AsNoTracking().ToListAsync()).Count,
            ["AppointmentHistories"] = (await db.AppointmentHistories.AsNoTracking().ToListAsync()).Count,
            ["Estimates"] = (await db.Estimates.AsNoTracking().ToListAsync()).Count,
            ["EstimateLines"] = (await db.EstimateLines.AsNoTracking().ToListAsync()).Count,
            ["JobOrders"] = (await db.JobOrders.AsNoTracking().ToListAsync()).Count,
            ["JobTasks"] = (await db.JobTasks.AsNoTracking().ToListAsync()).Count,
            ["TaskWork"] = (await db.TaskWork.AsNoTracking().ToListAsync()).Count,
            ["TaskReviews"] = (await db.TaskReviews.AsNoTracking().ToListAsync()).Count,
            ["TaskAssignments"] = (await db.TaskAssignments.AsNoTracking().ToListAsync()).Count,
            ["AssignmentHistory"] = (await db.AssignmentHistory.AsNoTracking().ToListAsync()).Count,
            ["Payments"] = (await db.Payments.AsNoTracking().ToListAsync()).Count,
            ["Refunds"] = (await db.Refunds.AsNoTracking().ToListAsync()).Count,
            ["JobCancellationRequests"] = (await db.JobCancellationRequests.AsNoTracking().ToListAsync()).Count,
            ["Services"] = (await db.Services.AsNoTracking().ToListAsync()).Count,
            ["Staff"] = (await db.Staff.AsNoTracking().ToListAsync()).Count,
            ["Branches"] = (await db.Branches.AsNoTracking().ToListAsync()).Count,
            ["DatabaseIdentity"] = (await db.DatabaseIdentity.AsNoTracking().ToListAsync()).Count,
            ["AuditEntries"] = (await db.AuditEntries.AsNoTracking().ToListAsync()).Count
        };
    }

    private sealed record TableFingerprint(string TableName, string[] Columns, Dictionary<string, Dictionary<string, string>> Rows, string CommonSha256)
    {
        public int RowCount => Rows.Count;
    }

    private sealed record FinancialBaseline(
        int ValidPaymentsCount,
        decimal ValidPaymentsSum,
        int VoidedPaymentsCount,
        decimal VoidedPaymentsSum,
        int JobTasksAgreedPriceNullCount,
        decimal JobTasksAgreedPriceSum,
        int JobOrdersApprovedEstimateNullCount,
        string PaymentPayerTypeJson,
        string PaymentPurposeJson);

    private sealed record DatabaseFingerprint(Dictionary<string, TableFingerprint> Tables, FinancialBaseline Financials);

    private sealed record TableComparison(
        string TableName,
        int PreRowCount,
        int PostRowCount,
        int CommonColumnCount,
        string[] AddedColumns,
        string PreCommonColumnsSha256,
        string PostCommonColumnsSha256,
        int MismatchedPkCount);

    private static async Task<DatabaseFingerprint> CaptureDatabaseFingerprintAsync(string connStr)
    {
        var tables = new Dictionary<string, TableFingerprint>(StringComparer.OrdinalIgnoreCase);
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();

        var tableNames = new List<string>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sys.tables WHERE is_ms_shipped = 0 ORDER BY name";
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) tableNames.Add(r.GetString(0));
        }

        foreach (string tbl in tableNames)
        {
            var pks = new List<string>();
            await using (var pkCmd = conn.CreateCommand())
            {
                pkCmd.CommandText = @"
SELECT kcu.COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
  ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
WHERE tc.TABLE_SCHEMA = 'dbo' AND tc.TABLE_NAME = @t AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
ORDER BY kcu.ORDINAL_POSITION";
                pkCmd.Parameters.AddWithValue("@t", tbl);
                await using var r = await pkCmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) pks.Add(r.GetString(0));
            }

            var cols = new List<string>();
            await using (var colCmd = conn.CreateCommand())
            {
                colCmd.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @t ORDER BY ORDINAL_POSITION";
                colCmd.Parameters.AddWithValue("@t", tbl);
                await using var r = await colCmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) cols.Add(r.GetString(0));
            }

            string orderClause = pks.Count > 0 ? string.Join(", ", pks.Select(p => $"[{p}]")) : "(SELECT NULL)";
            var rowMap = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            await using (var dataCmd = conn.CreateCommand())
            {
                dataCmd.CommandText = $"SELECT * FROM [dbo].[{tbl}] ORDER BY {orderClause}";
                await using var r = await dataCmd.ExecuteReaderAsync();
                int idx = 0;
                while (await r.ReadAsync())
                {
                    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < r.FieldCount; i++)
                        values[r.GetName(i)] = FormatSqlValue(r.GetValue(i));
                    string pkKey = pks.Count > 0
                        ? string.Join("|", pks.Select(p => values[p]))
                        : $"ROW#{idx++}";
                    rowMap[pkKey] = values;
                }
            }

            string digest = ComputeTableCommonSha256(cols, rowMap);
            tables[tbl] = new TableFingerprint(tbl, cols.ToArray(), rowMap, digest);
        }

        var fin = await CaptureFinancialBaselineAsync(conn);
        return new DatabaseFingerprint(tables, fin);
    }

    private static List<TableComparison> CompareSnapshots(DatabaseFingerprint pre, DatabaseFingerprint post)
    {
        var result = new List<TableComparison>();
        foreach (var kvp in pre.Tables.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            string tbl = kvp.Key;
            var before = kvp.Value;
            AssertTrue(post.Tables.TryGetValue(tbl, out var after), $"Pre-existing table '{tbl}' still exists after migration.");
            bool branchBackfilled = tbl is "Appointments" or "JobOrders" or "Payments";
            var comparedCols = before.Columns
                .Where(c => after!.Columns.Contains(c, StringComparer.OrdinalIgnoreCase))
                .Where(c => !(branchBackfilled && string.Equals(c, "RowVersion", StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            var addedCols = after!.Columns.Where(c => !before.Columns.Contains(c, StringComparer.OrdinalIgnoreCase)).ToArray();

            string preSha = ComputeTableCommonSha256(comparedCols, before.Rows);
            string postSha = string.Equals(tbl, "__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase)
                ? ComputeTableCommonSha256(comparedCols, after.Rows.Where(r => before.Rows.ContainsKey(r.Key)).ToDictionary(x => x.Key, x => x.Value))
                : ComputeTableCommonSha256(comparedCols, after.Rows);
            int mismatches = 0;
            foreach (var row in before.Rows)
            {
                if (!after.Rows.TryGetValue(row.Key, out var afterRow)) { mismatches++; continue; }
                string h1 = ComputeRowSha256(comparedCols, row.Key, row.Value);
                string h2 = ComputeRowSha256(comparedCols, row.Key, afterRow);
                if (h1 != h2) mismatches++;
            }

            result.Add(new TableComparison(tbl, before.RowCount, after.RowCount, comparedCols.Length, addedCols, preSha, postSha, mismatches));
        }
        return result;
    }

    private static string ComputeTableCommonSha256(IReadOnlyList<string> cols, Dictionary<string, Dictionary<string, string>> rows)
    {
        using var sha = SHA256.Create();
        var sb = new StringBuilder();
        foreach (string pk in rows.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            sb.Append(pk).Append(':').Append(ComputeRowSha256(cols, pk, rows[pk])).Append('\n');
        }
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    private static string ComputeRowSha256(IReadOnlyList<string> cols, string pk, Dictionary<string, string> row)
    {
        var sb = new StringBuilder(pk);
        foreach (string col in cols)
            sb.Append('|').Append(col).Append('=').Append(row.TryGetValue(col, out var v) ? v : "<MISSING>");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    private static string FormatSqlValue(object? val)
    {
        if (val == null || val is DBNull) return "<NULL>";
        return val switch
        {
            DateTime dt => dt.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("O", CultureInfo.InvariantCulture),
            decimal d => d.ToString("0.0000", CultureInfo.InvariantCulture),
            byte[] b => "0x" + Convert.ToHexString(b),
            _ => Convert.ToString(val, CultureInfo.InvariantCulture) ?? ""
        };
    }

    private static async Task<FinancialBaseline> CaptureFinancialBaselineAsync(SqlConnection conn)
    {
        async Task<(int, decimal)> QueryPair(string sql)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await using var r = await cmd.ExecuteReaderAsync();
            await r.ReadAsync();
            return (Convert.ToInt32(r.GetValue(0)), r.IsDBNull(1) ? 0m : Convert.ToDecimal(r.GetValue(1)));
        }
        async Task<string> QueryDist(string col)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT ISNULL(CAST([{col}] AS NVARCHAR(100)), '<NULL>') AS K, COUNT(*) AS C FROM dbo.Payments GROUP BY [{col}] ORDER BY K";
            var dict = new Dictionary<string, int>();
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) dict[r.GetString(0)] = r.GetInt32(1);
            return JsonSerializer.Serialize(dict);
        }

        var (validCnt, validSum) = await QueryPair("SELECT COUNT(*), ISNULL(SUM(AmountPaid), 0) FROM dbo.Payments WHERE IsVoided = 0");
        var (voidCnt, voidSum) = await QueryPair("SELECT COUNT(*), ISNULL(SUM(AmountPaid), 0) FROM dbo.Payments WHERE IsVoided = 1");
        var (nullPriceCnt, priceSum) = await QueryPair("SELECT SUM(CASE WHEN AgreedPrice IS NULL THEN 1 ELSE 0 END), ISNULL(SUM(AgreedPrice), 0) FROM dbo.JobTasks");
        var (nullEstCnt, _) = await QueryPair("SELECT SUM(CASE WHEN ApprovedEstimateId IS NULL THEN 1 ELSE 0 END), 0 FROM dbo.JobOrders");
        return new FinancialBaseline(validCnt, validSum, voidCnt, voidSum, nullPriceCnt, priceSum, nullEstCnt,
            await QueryDist("PayerType"), await QueryDist("PaymentPurpose"));
    }

    private static void WritePreservationArtifacts(
        string jsonPath,
        string mdPath,
        object proofObject,
        List<TableComparison> comparisons,
        FinancialBaseline fin,
        Dictionary<string, int> materialized,
        string bakSha256,
        long bakBytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(jsonPath)!);
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(proofObject, new JsonSerializerOptions { WriteIndented = true }));

        var md = new StringBuilder();
        md.AppendLine("# Legacy Historical Preservation Proof (`DB_MasterCrm` Native Backup & Migration)");
        md.AppendLine();
        md.AppendLine($"- **Generated (UTC):** {DateTime.UtcNow:O}");
        md.AppendLine($"- **Native Backup SHA-256:** `{bakSha256}` ({bakBytes:N0} bytes, `WITH COPY_ONLY`, `RESTORE VERIFYONLY` passed)");
        md.AppendLine($"- **Valid Payments:** {fin.ValidPaymentsCount} rows, PHP {fin.ValidPaymentsSum:N2} (Voided: {fin.VoidedPaymentsCount}, PHP {fin.VoidedPaymentsSum:N2})");
        md.AppendLine($"- **Historical NULL Preservation:** `JobTasks.AgreedPrice IS NULL` = {fin.JobTasksAgreedPriceNullCount}, `JobOrders.ApprovedEstimateId IS NULL` = {fin.JobOrdersApprovedEstimateNullCount}");
        md.AppendLine("- **PII Handling:** Zero raw customer PII written; deterministic PK-ordered common-column SHA-256 digests recorded per table.");
        md.AppendLine();
        md.AppendLine("## Per-Table Deterministic Comparison (Before vs After `MigrateAsync`)");
        md.AppendLine();
        md.AppendLine("| Table | Pre Rows | Post Rows | PK Mismatches | Added Columns | Common-Column SHA-256 Match |");
        md.AppendLine("| --- | ---: | ---: | ---: | --- | --- |");
        foreach (var c in comparisons)
        {
            string added = c.AddedColumns.Length == 0 ? "(none)" : string.Join(", ", c.AddedColumns);
            string match = c.PreCommonColumnsSha256 == c.PostCommonColumnsSha256 ? "EXACT" : "DIFF";
            md.AppendLine($"| `{c.TableName}` | {c.PreRowCount} | {c.PostRowCount} | {c.MismatchedPkCount} | {added} | {match} |");
        }
        md.AppendLine();
        md.AppendLine("## `TenantCrmDbContext` Full Materialization Counts");
        md.AppendLine();
        md.AppendLine("| Entity Set | Materialized Count |");
        md.AppendLine("| --- | ---: |");
        foreach (var kv in materialized.OrderBy(k => k.Key))
            md.AppendLine($"| `{kv.Key}` | {kv.Value} |");

        File.WriteAllText(mdPath, md.ToString());
    }

    private static async Task<string> CaptureCoreProtectedBaselineAsync(string connStr, string name)
    {
        int customers = await ScalarIntAsync(connStr, "SELECT COUNT(*) FROM dbo.Customers");
        int jobs = await ScalarIntAsync(connStr, "SELECT COUNT(*) FROM dbo.JobOrders");
        int payments = await ScalarIntAsync(connStr, "SELECT COUNT(*) FROM dbo.Payments");
        int tasks = await ScalarIntAsync(connStr, "SELECT COUNT(*) FROM dbo.JobTasks");
        int tables = await ScalarIntAsync(connStr, "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0");
        return $"{name}:C={customers}|J={jobs}|P={payments}|T={tasks}|TBL={tables}";
    }

    private static async Task<string> CaptureProtectedMasterBaselineAsync(string connStr)
    {
        int tenants = await ScalarIntAsync(connStr, "SELECT COUNT(*) FROM dbo.Tenants");
        int users = await ScalarIntAsync(connStr, "SELECT COUNT(*) FROM dbo.AspNetUsers");
        int memberships = await ScalarIntAsync(connStr, "SELECT COUNT(*) FROM dbo.Memberships");
        return $"Master:T={tenants}|U={users}|M={memberships}";
    }

    private static async Task<int> CountConstraintViolationsAsync(string connStr)
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS";
        await using var r = await cmd.ExecuteReaderAsync();
        int count = 0;
        while (await r.ReadAsync()) count++;
        return count;
    }

    private static async Task<(string DataLogicalName, string LogLogicalName)> ReadHeaderAndFileListAsync(string connStr, string bakPath)
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"RESTORE FILELISTONLY FROM DISK = N'{EscapeSqlLiteral(bakPath)}'";
        string dataName = "", logName = "";
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            string logical = r["LogicalName"].ToString() ?? "";
            string type = r["Type"].ToString() ?? "";
            if (type == "D" && string.IsNullOrEmpty(dataName)) dataName = logical;
            else if (type == "L" && string.IsNullOrEmpty(logName)) logName = logical;
        }
        if (string.IsNullOrEmpty(dataName) || string.IsNullOrEmpty(logName))
            throw new InvalidOperationException("Failed to read logical data/log names from native .bak file.");
        return (dataName, logName);
    }

    private static async Task ExecuteNonQueryAsync(string connStr, string sql)
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 120;
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> ScalarIntAsync(string connStr, string sql)
    {
        await using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task DropDatabaseIfExistsAsync(string masterConnStr, string dbName)
    {
        try
        {
            await ExecuteNonQueryAsync(masterConnStr, $@"
IF DB_ID(N'{EscapeSqlLiteral(dbName)}') IS NOT NULL
BEGIN
    ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{dbName}];
END");
        }
        catch { }
    }

    private static async Task<string> ComputeFileSha256Async(string path)
    {
        await using var fs = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(fs));
    }

    private static string EscapeSqlLiteral(string s) => s.Replace("'", "''");

    private static async Task RunDotNetExecAsync(string dllPath, string args, string cwd, Dictionary<string, string?> env)
    {
        var psi = new ProcessStartInfo("dotnet", $"exec \"{dllPath}\" {args}")
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var kv in env) psi.Environment[kv.Key] = kv.Value;
        using var proc = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {args}");
        string stdOut = await proc.StandardOutput.ReadToEndAsync();
        string stdErr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"CRM.api {args} failed (exit={proc.ExitCode}):\n{stdOut}\n{stdErr}");
    }

    private static Process StartApiProcess(string dllPath, string url, string cwd, Dictionary<string, string?> env)
    {
        var psi = new ProcessStartInfo("dotnet", $"exec \"{dllPath}\" --urls {url}")
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var kv in env) psi.Environment[kv.Key] = kv.Value;
        var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start CRM.api process.");
        proc.OutputDataReceived += (_, _) => { };
        proc.ErrorDataReceived += (_, _) => { };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        return proc;
    }

    private static async Task WaitForApiReadyAsync(string apiUrl)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        for (int i = 0; i < 40; i++)
        {
            try
            {
                var r = await http.PostAsync($"{apiUrl}/auth/resolve-tenants",
                    new StringContent("{}", Encoding.UTF8, "application/json"));
                if ((int)r.StatusCode < 500) return;
            }
            catch { }
            await Task.Delay(250);
        }
        throw new TimeoutException($"Timed out waiting for CRM.api at {apiUrl}");
    }

    private static async Task VerifyDesktopProcessLaunchesCleanlyAsync(string exePath, string apiUrl)
    {
        var psi = new ProcessStartInfo(exePath, "--tenant")
        {
            UseShellExecute = false
        };
        psi.Environment["REVCARE_API_BASE_URL"] = apiUrl;
        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start CRM_Revcare.exe --tenant");
        try
        {
            await Task.Delay(2500);
            AssertTrue(!proc.HasExited, "CRM_Revcare.exe --tenant launched cleanly and remained running awaiting login.");
        }
        finally
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { }
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void RunOnStaThread(Func<Task> action)
    {
        Exception? failure = null;
        using var watchdog = new System.Threading.Timer(_ =>
        {
            Console.Error.WriteLine("FATAL: STA Desktop Acceptance hung > 45s. Exiting.");
            Environment.Exit(99);
        }, null, 45_000, Timeout.Infinite);

        var t = new Thread(() =>
        {
            Exception? uiFailure = null;
            ThreadExceptionEventHandler onUiException = (_, e) =>
            {
                uiFailure ??= e.Exception;
                Console.Error.WriteLine("[UI-THREAD-EX] " + e.Exception);
            };
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += onUiException;
                Application.EnableVisualStyles();
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
                var task = action();
                var sw = Stopwatch.StartNew();
                while (!task.IsCompleted)
                {
                    Application.DoEvents();
                    Thread.Sleep(15);
                    if (sw.ElapsedMilliseconds > 40_000)
                        throw new TimeoutException("STA Desktop Acceptance timed out after 40s.");
                }
                task.GetAwaiter().GetResult();
                if (uiFailure != null)
                    throw new InvalidOperationException("Desktop rendering raised a UI-thread exception; acceptance cannot pass.", uiFailure);
            }
            catch (Exception ex) { failure = ex; }
            finally { Application.ThreadException -= onUiException; }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (failure != null) throw new Exception("STA Desktop Acceptance failed: " + failure.Message, failure);
    }

    private static async Task WaitUntilWorkspaceIdleAsync(TenantWorkspaceForm form)
    {
        var sw = Stopwatch.StartNew();
        await Task.Delay(50);
        while (sw.ElapsedMilliseconds < 15_000)
        {
            Application.DoEvents();
            bool isBusy = GetPrivateField<bool>(form, "busy");
            var grid = GetPrivateField<DataGridView>(form, "records");
            if (!isBusy && grid.DataSource != null)
            {
                PumpEvents(60);
                return;
            }
            await Task.Delay(25);
        }
        throw new TimeoutException("Timed out waiting for TenantWorkspaceForm async module load.");
    }

    private static void PumpEvents(int ms)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < ms)
        {
            Application.DoEvents();
            Thread.Sleep(20);
        }
    }

    private static void ClickMenu(IEnumerable<RadioButton> menus, string text)
    {
        var btn = menus.FirstOrDefault(r => string.Equals(r.Text, text, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Navigation menu '{text}' not found.");
        btn.PerformClick();
    }

    private static T GetPrivateField<T>(object target, string name)
    {
        var f = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?? throw new MissingFieldException(target.GetType().FullName, name);
        return (T)f.GetValue(target)!;
    }

    private static T? FindControlByText<T>(Control root, string text) where T : Control
    {
        foreach (Control c in root.Controls)
        {
            if (c is T match && string.Equals(match.Text, text, StringComparison.OrdinalIgnoreCase))
                return match;
            var child = FindControlByText<T>(c, text);
            if (child != null) return child;
        }
        return null;
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("FAILED: " + message);
        Console.WriteLine("PASS: " + message);
    }
}
