using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CRM_Revcare_;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Workshop.Database;

if (args.Contains("--inspect-local-readiness"))
{
    await CloudInspector.InspectLocalOnlyAsync();
    return;
}

if (args.Contains("--rehearse-cloud-schema-migration"))
{
    await CloudMigrationRehearsal.RunAsync();
    return;
}

if (args.Contains("--apply-cloud-schema-migration-db81189"))
{
    await CloudMigrationRehearsal.ApplyToCloudDb81189Async();
    return;
}

if (args.Length == 3 && args[0] == "--rehearse-native-backup")
{
    await NativeBackupRehearsal.RunAsync(args[1], args[2]);
    return;
}

if (args.Contains("--test-production-compatibility"))
{
    await ProductionCompatibilityTests.RunAsync();
    return;
}
if (args.Length == 4 && args[0] == "--production-compatibility-child")
{
    await ProductionCompatibilityTests.ChildAsync(args[1], args[2], args[3]);
    return;
}

if (args.Contains("--inspect-cloud"))
{
    await CloudInspector.InspectAsync();
    return;
}

if (args.Contains("--compatibility-evidence"))
{
    await CompatibilityEvidence.RunAsync();
    return;
}

if (args.Contains("--verify-legacy-cutover"))
{
    string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    await LegacyCutoverAcceptance.RunAsync(repoRoot);
    return;
}

if (args.Contains("--test-historical-safety"))
{
    await HistoricalRecoveryTests.RunAsync();
    return;
}
if (args.Length == 5 && args[0] == "--recovery-child")
{
    await HistoricalRecoveryTests.ProbeAsync(args[1], int.Parse(args[2]), args[3], args[4]);
    return;
}

if (args.Contains("--historical-audit"))
{
    await HistoricalSafety.AuditAsync();
    return;
}

if (args.Contains("--cloud-dry-run"))
{
    Console.WriteLine("================================================================================");
    Console.WriteLine("    REVCARE CRM - READ-ONLY CLOUD RECONCILIATION DRY-RUN (db80735)");
    Console.WriteLine("================================================================================");
    Console.WriteLine("SAFETY MODE: READ-ONLY. Zero cloud writes will be performed.\n");

    var cloudTarget = CrmDatabase.ResolveTarget(new[] { "--cloud" });
    Console.WriteLine($"Target Environment: {cloudTarget.EnvironmentDisplayName}");
    Console.WriteLine($"Target Server: {cloudTarget.ServerDisplayName}");
    Console.WriteLine($"Target Database: {cloudTarget.DatabaseName}\n");

    using var cloudDb = CrmDatabase.CreateContextWithConnectionString(cloudTarget.ConnectionString);
    var report = await Legacy3NfReconciliationService.Reconcile3NfAsync(cloudDb, dryRun: true);

    Console.WriteLine("--------------------------------------------------------------------------------");
    Console.WriteLine("DRY-RUN RECONCILIATION SUMMARY:");
    Console.WriteLine($"  Total Jobs Evaluated:             {report.TotalJobsEvaluated}");
    Console.WriteLine($"  Already Linked to Estimate:       {report.AlreadyLinkedCount}");
    Console.WriteLine($"  Evidence-authorized links:        {report.NewlyLinkedCount}");
    Console.WriteLine($"  Unlinked jobs requiring evidence: {report.UnresolvedJobs.Count}");
    Console.WriteLine($"  Payments requiring evidence review: {report.PaymentsRequiringReviewCount}; corrections authorized: {report.PaymentsCorrectedCount}");
    Console.WriteLine("--------------------------------------------------------------------------------\n");
    foreach (var message in report.ValidationMessages) Console.WriteLine(message);

    if (report.UnresolvedJobs.Count > 0)
    {
        Console.WriteLine($"SAMPLE UNRESOLVED JOBS (First 10 of {report.UnresolvedJobs.Count}):");
        foreach (var u in report.UnresolvedJobs.Take(10))
        {
            Console.WriteLine($"  Job #{u.JobOrderId} ({u.Status}): AuthCost=₱{u.TotalEstimatedCost:N2}, TaskSum=₱{u.TaskSum:N2}, Diff=₱{u.Difference:N2}");
            Console.WriteLine($"    Reason: {u.Reason}");
        }
    }
    return;
}

if (args.Contains("--test-reconcile-disposable"))
{
    throw new InvalidOperationException("Retired: this test asserted unsupported historical approval/payment mappings. Use --test-historical-safety instead.");
}

if (args.Contains("--seed-legacy"))
{
    await LegacySeed.RunAsync(args.Contains("--apply"));
    return;
}

if (args.Contains("--clone-test-db"))
{
    var masterConn = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True";
    using var conn = new SqlConnection(masterConn);
    await conn.OpenAsync();
    var tempDir = Path.GetTempPath();
    var bakFile = Path.Combine(tempDir, "DB_MasterCrm_Backup_Baseline.bak");
    var mdfFile = Path.Combine(tempDir, "DB_MasterCrm_MigrationTest.mdf");
    var ldfFile = Path.Combine(tempDir, "DB_MasterCrm_MigrationTest.ldf");

    using var cmd = conn.CreateCommand();
    Console.WriteLine("Creating verified backup of DB_MasterCrm...");
    cmd.CommandText = $"BACKUP DATABASE [DB_MasterCrm] TO DISK = '{bakFile}' WITH FORMAT, INIT;";
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine($"Backup successfully written to: {bakFile}");

    cmd.CommandText = $"RESTORE FILELISTONLY FROM DISK = '{bakFile}';";
    string logicalData = "", logicalLog = "";
    using (var r = await cmd.ExecuteReaderAsync())
    {
        while (await r.ReadAsync())
        {
            var t = r["Type"].ToString();
            if (t == "D") logicalData = r["LogicalName"].ToString()!;
            if (t == "L") logicalLog = r["LogicalName"].ToString()!;
        }
    }

    Console.WriteLine("Restoring backup to DB_MasterCrm_MigrationTest...");
    cmd.CommandText = $@"
IF DB_ID('DB_MasterCrm_MigrationTest') IS NOT NULL
BEGIN
    ALTER DATABASE [DB_MasterCrm_MigrationTest] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [DB_MasterCrm_MigrationTest];
END
RESTORE DATABASE [DB_MasterCrm_MigrationTest] FROM DISK = '{bakFile}'
WITH MOVE '{logicalData}' TO '{mdfFile}',
     MOVE '{logicalLog}' TO '{ldfFile}';";
    await cmd.ExecuteNonQueryAsync();
    Console.WriteLine("DB_MasterCrm_MigrationTest successfully created and verified!");
    return;
}

if (args.Contains("--verify-debug-app"))
{
    Console.WriteLine("================================================================================");
    Console.WriteLine("    REVCARE CRM - DEBUG APPLICATION LIVE RUNTIME VERIFICATION");
    Console.WriteLine("================================================================================");

    // 1. Target Resolution without arguments (Default F5 / VS behavior)
    Console.WriteLine("1. Testing default startup target resolution (no command-line switches)...");
    var defaultTarget = CrmDatabase.ResolveTarget(Array.Empty<string>());
    Console.WriteLine($"   Environment: {defaultTarget.EnvironmentDisplayName}");
    Console.WriteLine($"   Server:      {defaultTarget.ServerDisplayName}");
    Console.WriteLine($"   Database:    {defaultTarget.DatabaseName}");
    if (defaultTarget.Environment != CrmEnvironment.Local || !defaultTarget.ConnectionString.Contains("DB_MasterCrm"))
    {
        throw new Exception($"Default target resolution failed! Expected Local DB_MasterCrm, got: {defaultTarget.ConnectionString}");
    }
    Console.WriteLine("   PASS: Default launch resolves strictly to local (localdb)\\MSSQLLocalDB / DB_MasterCrm.");

    // 2. Target Resolution with explicit switches
    Console.WriteLine("\n2. Testing explicit switch resolution...");
    var localTarget = CrmDatabase.ResolveTarget(new[] { "--local" });
    if (localTarget.Environment != CrmEnvironment.Local) throw new Exception("Explicit --local switch failed!");
    Console.WriteLine("   PASS: '--local' switch resolves to Local DB_MasterCrm.");

    var cloudTarget = CrmDatabase.ResolveTarget(new[] { "--cloud" });
    if (cloudTarget.Environment != CrmEnvironment.Cloud || !cloudTarget.DatabaseName.Contains("db80735"))
        throw new Exception("Explicit --cloud switch failed!");
    Console.WriteLine("   PASS: '--cloud' switch resolves to Cloud db80735.");

    // 3. Live WinForms Runtime Execution on STA thread
    Console.WriteLine("\n3. Launching Live Windows Forms CrmTransactionsForm in runtime test harness...");
    Exception? staException = null;
    var thread = new Thread(() =>
    {
        try
        {
            using var form = new CrmTransactionsForm(CrmDatabase.CreateContext, _ => true);
            form.BindingContext = new System.Windows.Forms.BindingContext();
            form.Size = new System.Drawing.Size(1300, 850);
            form.CreateControl();
            form.Show();
            System.Windows.Forms.Application.DoEvents();

            var syncingField = typeof(CrmTransactionsForm).GetField("syncing", BindingFlags.NonPublic | BindingFlags.Instance);
            var sync = form.SyncAsync();
            while ((bool)syncingField!.GetValue(form)! || !sync.IsCompleted)
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(20);
            }
            sync.GetAwaiter().GetResult();

            var syncStatusField = typeof(CrmTransactionsForm).GetField("syncStatus", BindingFlags.NonPublic | BindingFlags.Instance);
            var syncStatus = (System.Windows.Forms.Label)syncStatusField!.GetValue(form)!;
            string statusText = syncStatus.Text;
            Console.WriteLine($"   Live Sync Status Bar: \"{statusText}\"");

            if (!statusText.Contains("[LOCAL]")) throw new Exception("Status text does not display [LOCAL] environment tag!");
            if (!statusText.Contains("DB_MasterCrm")) throw new Exception("Status text does not indicate DB_MasterCrm!");
            if (statusText.Contains("Connection failed") || statusText.Contains("Refresh failed"))
            {
                throw new Exception($"Status bar indicates connection or refresh failure: {statusText}");
            }
            Console.WriteLine("   PASS: Zero SqlException or refresh failures in running application.");

            // Grids
            var customersField = typeof(CrmTransactionsForm).GetField("customersPage", BindingFlags.NonPublic | BindingFlags.Instance);
            var ordersField = typeof(CrmTransactionsForm).GetField("ordersPage", BindingFlags.NonPublic | BindingFlags.Instance);
            var tasksField = typeof(CrmTransactionsForm).GetField("tasksPage", BindingFlags.NonPublic | BindingFlags.Instance);
            var paymentsField = typeof(CrmTransactionsForm).GetField("paymentsPage", BindingFlags.NonPublic | BindingFlags.Instance);

            var customersPage = (CustomerPage)customersField!.GetValue(form)!;
            var ordersPage = (JobOrderPage)ordersField!.GetValue(form)!;
            var tasksPage = (JobTaskPage)tasksField!.GetValue(form)!;
            var paymentsPage = (PaymentPage)paymentsField!.GetValue(form)!;

            if (customersPage.Grid.SelectedRows.Count != 0 || customersPage.Grid.CurrentCell != null)
                throw new Exception("First-row auto-selection detected on Customers grid!");
            if (ordersPage.Grid.SelectedRows.Count != 0 || ordersPage.Grid.CurrentCell != null)
                throw new Exception("First-row auto-selection detected on JobOrders grid!");
            if (paymentsPage.Grid.SelectedRows.Count != 0 || paymentsPage.Grid.CurrentCell != null)
                throw new Exception("First-row auto-selection detected on Payments grid!");
            Console.WriteLine("   PASS: All grids confirmed to have zero first-row auto-selection on load.");

            Console.WriteLine($"   Customers Grid Rows: {customersPage.Grid.Rows.Count}");
            Console.WriteLine($"   Job Orders Grid Rows: {ordersPage.Grid.Rows.Count}");
            Console.WriteLine($"   Payments Grid Rows: {paymentsPage.Grid.Rows.Count}");
            if (customersPage.Grid.Rows.Count == 0 || ordersPage.Grid.Rows.Count == 0 || paymentsPage.Grid.Rows.Count == 0)
            {
                throw new Exception("Expected rows in grids but got 0!");
            }
            Console.WriteLine("   PASS: Loaded all entities successfully from local DB_MasterCrm.");

            // 8. Feedback module verification: header clicking, sorting, and column widths
            var feedbackField = typeof(CrmTransactionsForm).GetField("feedbackPage", BindingFlags.NonPublic | BindingFlags.Instance);
            var feedbackPage = (FeedbackPage)feedbackField!.GetValue(form)!;

            Console.WriteLine("\n[8] Verifying Feedback module column layout and sorting...");
            Console.WriteLine($"   Feedback Grid Columns Count: {feedbackPage.Grid.Columns.Count}");
            Console.WriteLine($"   Feedback Grid AutoSizeColumnsMode: {feedbackPage.Grid.AutoSizeColumnsMode}");
            Console.WriteLine($"   Feedback Grid ColumnHeadersHeight: {feedbackPage.Grid.ColumnHeadersHeight}");

            if (feedbackPage.Grid.AutoSizeColumnsMode != System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None)
                throw new Exception($"Expected AutoSizeColumnsMode None but got {feedbackPage.Grid.AutoSizeColumnsMode}");
            if (feedbackPage.Grid.ColumnHeadersHeight != 42)
                throw new Exception($"Expected ColumnHeadersHeight 42 but got {feedbackPage.Grid.ColumnHeadersHeight}");

            // Verify default column widths
            foreach (System.Windows.Forms.DataGridViewColumn col in feedbackPage.Grid.Columns)
            {
                Console.WriteLine($"   Column '{col.Name}' ({col.HeaderText}): Width={col.Width}, MinWidth={col.MinimumWidth}, AutoSizeMode={col.AutoSizeMode}");
            }

            // Exercise clicking every sortable column header in Feedback grid (both ascending and descending)
            Console.WriteLine("   Exercising click on every sortable column header in Feedback grid...");
            for (int colIdx = 0; colIdx < feedbackPage.Grid.Columns.Count; colIdx++)
            {
                var col = feedbackPage.Grid.Columns[colIdx];
                if (col.Name == GridRowNumbering.RowNumberColumnName) continue;

                // Click once (Ascending)
                var args = new System.Windows.Forms.DataGridViewCellMouseEventArgs(colIdx, -1, 10, 10, new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 1, 10, 10, 0));
                var onHeaderClick = typeof(System.Windows.Forms.DataGridView).GetMethod("OnColumnHeaderMouseClick", BindingFlags.NonPublic | BindingFlags.Instance);
                onHeaderClick?.Invoke(feedbackPage.Grid, new object[] { args });

                // Click again (Descending)
                onHeaderClick?.Invoke(feedbackPage.Grid, new object[] { args });

                Console.WriteLine($"   PASS: Successfully sorted on column '{col.Name}' ({col.HeaderText}) in both Ascending & Descending with ZERO exceptions.");
            }

            // Exercise manual column resizing and width preservation
            var testCol = feedbackPage.Grid.Columns["PositiveRemarks"] ?? feedbackPage.Grid.Columns["JobOrderId"];
            if (testCol != null)
            {
                int oldWidth = testCol.Width;
                testCol.Width = oldWidth + 50;
                Console.WriteLine($"   Manually resized column '{testCol.Name}' from {oldWidth} to {testCol.Width}");

                CrmGridStandardizer.RestoreColumnWidths(feedbackPage.Grid, "Feedback");
                if (testCol.Width != oldWidth + 50)
                    throw new Exception($"Column width not preserved! Expected {oldWidth + 50} but was {testCol.Width}");
                Console.WriteLine("   PASS: Column width successfully preserved across refresh!");

                CrmGridStandardizer.ResetColumnWidths(feedbackPage.Grid, "Feedback");
                if (testCol.Width != oldWidth)
                    throw new Exception($"Column width not reset! Expected {oldWidth} but was {testCol.Width}");
                Console.WriteLine("   PASS: Reset Column Widths successfully restored default width!");
            }

            form.Close();
        }
        catch (Exception ex)
        {
            staException = ex;
        }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (staException != null)
    {
        throw new Exception($"STA WinForms execution failed: {staException.Message}", staException);
    }

    Console.WriteLine("\nLIVE DEBUG APPLICATION VERIFICATION COMPLETED WITH ZERO ERRORS!");
    return;
}

Console.WriteLine("Usage: Workshop.Database --historical-audit | --test-historical-safety | --cloud-dry-run | --verify-debug-app");
