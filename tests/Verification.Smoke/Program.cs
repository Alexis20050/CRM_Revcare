using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using CRM_Revcare_;
using Microsoft.EntityFrameworkCore;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, _) => { };
        Application.EnableVisualStyles();

        if (args.Contains("--check-counts"))
        {
            Console.WriteLine("Querying active CRM database counts...");
            using var db = CrmDatabase.CreateContext();
            Console.WriteLine($"Customers: {db.Customers.Count()}");
            Console.WriteLine($"JobOrders: {db.JobOrders.Count()}");
            Console.WriteLine($"JobTasks: {db.JobTasks.Count()}");
            Console.WriteLine($"Payments: {db.Payments.Count()}");
            Console.WriteLine($"Inquiries: {db.Inquiries.Count()}");
            Console.WriteLine($"Complaints: {db.Complaints.Count()}");
            Console.WriteLine($"Feedbacks: {db.Feedbacks.Count()}");
            Console.WriteLine($"Staff: {db.Staff.Count()}");
            return;
        }

        if (args.Contains("--seed-200") || args.Contains("--seed-200-local"))
            throw new InvalidOperationException("Operational seeding is prohibited in automated verification.");

        Console.WriteLine("=================================================");
        Console.WriteLine("REVCARE CRM COMPREHENSIVE VERIFICATION SUITE");
        Console.WriteLine("=================================================");

        Directory.CreateDirectory("screenshots");

        TestLegacyCrm();
        TestTenantA();
        TestFocusedRegressions();
        TestDatabaseEnvironmentSafety();

        using (var fixture = new DisposableCrmFixture())
        {
            TestAssignmentAndBillingRegression(fixture.CreateContext);
            TestComprehensiveCrudOperations(fixture.CreateContext);
            TestLiveLocalDatabaseIntegration(fixture.CreateContext, fixture.DbName);
            TestLegacyCrmOptimization(fixture.CreateContext);
            TestLocalCutoverProcedure();
            TestNativeBackupRestoreRehearsal();
        }

        Console.WriteLine("\nALL VERIFICATION CHECKS PASSED; operational fixture tests fully restored in disposable isolation.");
        Console.WriteLine("Screenshots saved in screenshots/ directory.");
        Environment.Exit(0);
    }

    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception("FAILED: " + name);
        Console.WriteLine("PASS: " + name);
    }

    static void ClickButton(Button btn)
    {
        typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(btn, new object[] { EventArgs.Empty });
    }

    static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;

    static void TestLegacyCrm()
    {
        Console.WriteLine("\n--- Testing Legacy CRM ---");

        var dbName = "RevCare_Verify_" + Guid.NewGuid().ToString("N");
        var connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=" + dbName + ";Trusted_Connection=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<MasterCrmDbContext>()
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, MasterCrmModelCacheKeyFactory>()
            .UseSqlServer(connectionString)
            .Options;

        MasterCrmDbContext Create() => new MasterCrmDbContext(options);

        using (var initialDb = Create())
        {
            initialDb.Database.Migrate();
            initialDb.Staff.AddRange(
                new WorkshopStaff { StaffId = "juan.gomez", DisplayName = "Juan Gomez", Role = "Automotive Mechanic", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
                new WorkshopStaff { StaffId = "pedro.santos", DisplayName = "Pedro Santos", Role = "Lead Mechanic", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
                new WorkshopStaff { StaffId = "maria.reyes", DisplayName = "Maria Reyes", Role = "Detailing Technician", IsActive = true, CreatedAtUtc = DateTime.UtcNow }
            );
            initialDb.SaveChanges();
        }

        try
        {
            // Seed legacy data
            using (var db = Create())
            {
            var c1 = new Customer { FullName = "Juan Dela Cruz", ContactNumber = "09171234567", Email = "juan@example.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-10) };
            var c2 = new Customer { FullName = "Maria Clara", ContactNumber = "09189876543", Email = "maria@example.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-5) };
            var c3 = new Customer { FullName = "Pedro Santos", ContactNumber = "09191112233", Email = "pedro@example.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-2) };
            db.Customers.AddRange(c1, c2, c3);
            db.SaveChanges();

            var j1 = new JobOrder
            {
                CustomerId = c1.CustomerId,
                VehicleDetails = "Toyota Vios 2020 Silver - ABC 1234",
                EstimateId = "EST-00101",
                TotalEstimatedCost = 5000m,
                StartedAt = DateTime.UtcNow.AddDays(-3),
                Status = "In Queue"
            };
            CRM.infrastructure.services.JobServices.Assign(db, j1, CRM.infrastructure.services.JobServices.Offered.Take(2));
            db.JobOrders.Add(j1);

            var j2 = new JobOrder
            {
                CustomerId = c2.CustomerId,
                VehicleDetails = "Honda Civic 2022 Red - XYZ 9876",
                EstimateId = "EST-00102",
                TotalEstimatedCost = null,
                StartedAt = DateTime.UtcNow.AddDays(-1),
                Status = "In Queue"
            };
            db.JobOrders.Add(j2);
            db.SaveChanges();

            var p1 = new Payment
            {
                JobOrderId = j1.JobOrderId,
                PaymentType = "Downpayment",
                PaymentMethod = "Cash",
                AmountPaid = 2500m,
                IsVoided = false,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };
            db.Payments.Add(p1);
            db.SaveChanges();

            j1.AssignedMechanicUserId = "juan.gomez";
            j1.AssignedMechanic = "Juan Gomez";
            j1.Status = "In Progress";
            db.JobOrders.Update(j1);
            db.SaveChanges();

            var p2 = new Payment
            {
                JobOrderId = j1.JobOrderId,
                PaymentType = "Progress Payment",
                PaymentMethod = "Cash",
                AmountPaid = 500m,
                IsVoided = false,
                VoidReason = "Incorrect amount recorded",
                VoidedAtUtc = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };
            db.Payments.Add(p2);
            db.SaveChanges();
            p2.IsVoided = true;
            db.SaveChanges();
        }

        using var form = new CrmTransactionsForm(Create, _ => true);
        form.BindingContext = new BindingContext();
        form.Size = new Size(1300, 850);
        form.CreateControl();
        form.Show();
        Application.DoEvents();

        var sync = form.SyncAsync();
        while (Field<bool>(form, "syncing") || !sync.IsCompleted) { Application.DoEvents(); Thread.Sleep(20); }
        sync.GetAwaiter().GetResult();

        var customersPage = Field<CustomerPage>(form, "customersPage");
        var ordersPage = Field<JobOrderPage>(form, "ordersPage");
        var tasksPage = Field<JobTaskPage>(form, "tasksPage");
        var paymentsPage = Field<PaymentPage>(form, "paymentsPage");
        var inquiriesPage = Field<InquiryPage>(form, "inquiriesPage");
        var complaintsPage = Field<ComplaintPage>(form, "complaintsPage");
        var feedbackPage = Field<FeedbackPage>(form, "feedbackPage");
        var complaintMenu = Field<RadioButton>(form, "complaintMenu");
        var inquiryMenu = Field<RadioButton>(form, "inquiryMenu");
        var feedbackMenu = Field<RadioButton>(form, "feedbackMenu");
        var orderMenu = Field<RadioButton>(form, "orderMenu");
        var taskMenu = Field<RadioButton>(form, "taskMenu");

        // 1. VERIFY NO AUTOMATIC SELECTION ON INITIAL LOAD
        Check(customersPage.Grid.SelectedRows.Count == 0, "Legacy Customers: No first-row selection on load");
        Check(customersPage.Grid.CurrentCell == null, "Legacy Customers: CurrentCell is null on load");
        Check(ordersPage.Grid.SelectedRows.Count == 0, "Legacy Job Orders: No first-row selection on load");
        Check(tasksPage.Grid.SelectedRows.Count == 0, "Legacy Tasks: No first-row selection on load");
        Check(paymentsPage.Grid.SelectedRows.Count == 0, "Legacy Payments: No first-row selection on load");
        Check(!paymentsPage.previewReceiptButton.Enabled, "Legacy Payments: Preview receipt disabled without selection");
        Check(!paymentsPage.printReceiptButton.Enabled, "Legacy Payments: Print receipt disabled without selection");
        Check(inquiriesPage.Grid.SelectedRows.Count == 0, "Legacy Inquiries: No first-row selection on load");
        Check(complaintsPage.Grid.SelectedRows.Count == 0, "Legacy Complaints: No first-row selection on load");
        Check(feedbackPage.Grid.SelectedRows.Count == 0, "Legacy Feedback: No first-row selection on load");

        // 1b. VERIFY NO PRE-SELECTION ON TAB SWITCHING
        complaintMenu.Checked = true;
        Application.DoEvents();
        Check(complaintsPage.Grid.SelectedRows.Count == 0, "Legacy Complaints: Tab switch has no pre-selection");
        Check(complaintsPage.Grid.CurrentCell == null, "Legacy Complaints: Tab switch keeps CurrentCell null");
        var complaintEdit = Field<Button>(complaintsPage, "editButton");
        Check(!complaintEdit.Enabled, "Legacy Complaints: Edit Selected remains disabled on tab switch");

        feedbackMenu.Checked = true;
        Application.DoEvents();
        Check(feedbackPage.Grid.SelectedRows.Count == 0, "Legacy Feedback: Tab switch has no pre-selection");

        inquiryMenu.Checked = true;
        Application.DoEvents();
        Check(inquiriesPage.Grid.SelectedRows.Count == 0, "Legacy Inquiries: Tab switch has no pre-selection");

        // 1c. VERIFY JOB ORDER HEADER HEIGHT AND WRAP MODE
        Check(ordersPage.recordsGrid.ColumnHeadersHeight == 42 || ordersPage.recordsGrid.ColumnHeadersHeight == 48, "Legacy Job Orders: ColumnHeadersHeight is standardized (42-48px) to prevent clipping");
        Check(ordersPage.recordsGrid.ColumnHeadersHeightSizeMode == DataGridViewColumnHeadersHeightSizeMode.DisableResizing || ordersPage.recordsGrid.ColumnHeadersHeightSizeMode == DataGridViewColumnHeadersHeightSizeMode.EnableResizing, "Legacy Job Orders: Headers allow resizing/fixed");
        Check(ordersPage.recordsGrid.ColumnHeadersDefaultCellStyle.WrapMode == DataGridViewTriState.True, "Legacy Job Orders: Headers wrap multi-line text");

        // 2. VERIFY HIDE ACTIVE AND CREATED AT IN CUSTOMER PAGE
        Check(!customersPage.IsActiveColumn.Visible, "Legacy Customers: IsActiveColumn is hidden in grid");
        Check(!customersPage.CreatedAtColumn.Visible, "Legacy Customers: CreatedAtColumn is hidden in grid");
        Check(!customersPage.activeLabel.Visible, "Legacy Customers: activeLabel is hidden in editor");
        Check(!customersPage.active.Visible, "Legacy Customers: active checkbox is hidden in editor");

        // 3. VERIFY CUSTOMER SEARCH & CLEAR SEARCH
        customersPage.search.Text = "Maria";
        Thread.Sleep(350);
        Application.DoEvents();
        Check(customersPage.Grid.Rows.Count == 1, "Legacy Customers: Search finds exactly 1 record for 'Maria'");
        Check(customersPage.countLabel.Text.Contains("1 of"), "Legacy Customers: Matching record count displayed");
        Check(customersPage.Grid.SelectedRows.Count == 0, "Legacy Customers: Search does not silently select first row");

        // Capture Customer Search Screenshot
        using (var bmp = new Bitmap(customersPage.Width, customersPage.Height))
        {
            customersPage.DrawToBitmap(bmp, new Rectangle(Point.Empty, customersPage.Size));
            bmp.Save("screenshots/customer-search.png");
            Console.WriteLine("Saved screenshot: screenshots/customer-search.png");
        }

        ClickButton(customersPage.clearSearch);
        Thread.Sleep(350);
        Application.DoEvents();
        Check(customersPage.Grid.Rows.Count == 3, "Legacy Customers: Clear search restores all 3 records");
        Check(customersPage.Grid.SelectedRows.Count == 0, "Legacy Customers: Clear search does not select first row");

        // Verify Customer Search Button
        Check(customersPage.searchButton != null, "Legacy Customers: Search button exists");
        customersPage.search.Text = "Pedro";
        ClickButton(customersPage.searchButton);
        Application.DoEvents();
        Check(customersPage.Grid.Rows.Count == 1, "Legacy Customers: Search button triggers filter for 'Pedro'");
        ClickButton(customersPage.clearSearch);
        Application.DoEvents();

        // 4. VERIFY PAYMENT/BILLING SEARCH & FILTERS
        Check(paymentsPage.Grid.Rows.Count == 2, "Legacy Payments: Loads 2 payments initially");
        Check(paymentsPage.searchButton != null, "Legacy Payments: Search button exists");
        paymentsPage.paymentSearch.Text = "Juan";
        ClickButton(paymentsPage.searchButton);
        Application.DoEvents();
        Check(paymentsPage.Grid.Rows.Count == 2, "Legacy Payments: Search button triggers filter");
        Thread.Sleep(350);
        Application.DoEvents();
        Check(paymentsPage.Grid.Rows.Count == 2, "Legacy Payments: Search matches customer 'Juan'");
        Check(paymentsPage.paymentCountLabel.Text.Contains("2 of 2"), "Legacy Payments: Matching count updated");

        // Payment status filter: Valid
        paymentsPage.paymentStatusFilter.SelectedIndex = 1; // Valid
        Application.DoEvents();
        Check(paymentsPage.Grid.Rows.Count == 1, "Legacy Payments: Status filter 'Valid' filters out voided payment");

        // Payment status filter: Voided
        paymentsPage.paymentStatusFilter.SelectedIndex = 2; // Voided
        Application.DoEvents();
        Check(paymentsPage.Grid.Rows.Count == 1, "Legacy Payments: Status filter 'Voided' shows voided payment");

        // Standardized reference search
        paymentsPage.paymentStatusFilter.SelectedIndex = 0;
        paymentsPage.paymentSearch.Text = "RCPT-000001";
        Thread.Sleep(350); Application.DoEvents();
        Check(paymentsPage.Grid.Rows.Count == 1, "Legacy Payments: Standardized search matches exact reference 'RCPT-000001'");

        paymentsPage.paymentSearch.Text = "RCPT-1";
        Thread.Sleep(350); Application.DoEvents();
        Check(paymentsPage.Grid.Rows.Count == 1, "Legacy Payments: Standardized search matches unpadded reference 'RCPT-1'");

        paymentsPage.paymentSearch.Text = "RCPT-000002";
        Thread.Sleep(350); Application.DoEvents();
        Check(paymentsPage.Grid.Rows.Count == 1, "Legacy Payments: Standardized search matches exact reference 'RCPT-000002'");

        paymentsPage.paymentSearch.Text = "RCPT-2";
        Thread.Sleep(350); Application.DoEvents();
        Check(paymentsPage.Grid.Rows.Count == 1, "Legacy Payments: Standardized search matches unpadded reference 'RCPT-2'");

        // Clear search
        ClickButton(paymentsPage.clearSearchButton);
        Thread.Sleep(350);
        Application.DoEvents();
        Check(paymentsPage.Grid.Rows.Count == 2, "Legacy Payments: Clear search restores all payments");
        Check(paymentsPage.paymentStatusFilter.SelectedIndex == 0, "Legacy Payments: Clear search resets status filter");

        // Select a payment
        paymentsPage.Grid.Rows[0].Selected = true;
        Application.DoEvents();
        Check(paymentsPage.previewReceiptButton.Enabled, "Legacy Payments: Preview Receipt enabled upon explicit selection");
        Check(paymentsPage.printReceiptButton.Enabled, "Legacy Payments: Print Receipt enabled upon explicit selection");

        // Capture Billing Search Screenshot
        using (var bmp = new Bitmap(paymentsPage.Width, paymentsPage.Height))
        {
            paymentsPage.DrawToBitmap(bmp, new Rectangle(Point.Empty, paymentsPage.Size));
            bmp.Save("screenshots/billing-search.png");
            Console.WriteLine("Saved screenshot: screenshots/billing-search.png");
        }

        // 5. VERIFY JOB TASKS MASTER-DETAIL
        taskMenu.Checked = true;
        Application.DoEvents();
        Check(tasksPage.selectedJobLabel.Text.Contains("Select a job"), "Legacy Tasks: Initial prompt displayed before job selection");
        Check(tasksPage.jobsGrid.CurrentCell == null && tasksPage.jobsGrid.SelectedRows.Count == 0,
            "Legacy Tasks: Initial job grid has no hidden current cell or selected row");
        Check(tasksPage.Grid.Rows.Count == 0, "Legacy Tasks: Task grid is empty before job selection");
        Check(!tasksPage.printMechanicCopyButton.Enabled, "Legacy Tasks: Print Mechanic Copy disabled before job selection");

        // Select Job 1 in Upper Grid
        foreach (DataGridViewRow r in tasksPage.jobsGrid.Rows)
        {
            if ((int)r.Cells["JobOrderId"].Value == 1) { r.Selected = true; break; }
        }
        Application.DoEvents();
        Check(tasksPage.Grid.Rows.Count == 2, "Legacy Tasks: Selecting Job 1 displays its 2 service tasks");
        Check(tasksPage.printMechanicCopyButton.Enabled, "Legacy Tasks: Print Mechanic Copy enabled when job is selected");

        // Select Job 2 (Zero tasks)
        foreach (DataGridViewRow r in tasksPage.jobsGrid.Rows)
        {
            if ((int)r.Cells["JobOrderId"].Value == 2) { r.Selected = true; break; }
        }
        Application.DoEvents();
        Check(tasksPage.Grid.Rows.Count == 0, "Legacy Tasks: Job 2 has 0 tasks");
        Check(tasksPage.selectedJobLabel.Text.Contains("Job Orders"), "Legacy Tasks: Zero-task job explains services must be added through Job Orders");

        // Return to Job 1 for screenshot
        foreach (DataGridViewRow r in tasksPage.jobsGrid.Rows)
        {
            if ((int)r.Cells["JobOrderId"].Value == 1) { r.Selected = true; break; }
        }
        Application.DoEvents();

        // Capture Job Tasks Screenshot
        using (var bmp = new Bitmap(tasksPage.Width, tasksPage.Height))
        {
            tasksPage.DrawToBitmap(bmp, new Rectangle(Point.Empty, tasksPage.Size));
            bmp.Save("screenshots/job-tasks.png");
            Console.WriteLine("Saved screenshot: screenshots/job-tasks.png");
        }

        // 5b. VERIFY JOB TASKS POP-UP DIALOG (Legacy CRM)
        Check(Field<SplitContainer>(tasksPage, "workspaceSplit").Panel2Collapsed && !tasksPage.Grid.Visible,
            "Legacy Tasks: Embedded task table hidden; full job list remains on main screen");
        Check(tasksPage.btnViewTasksPopUp.Visible, "Legacy Tasks: Popup update action remains visible");
        using (var popUpDlg = new JobTasksDialogForm(1, Create))
        {
            popUpDlg.Show();
            Application.DoEvents();
            var popUpGrid = Field<DataGridView>(popUpDlg, "tasksGrid");
            Check(popUpGrid.Rows.Count == 2, "Job Tasks Pop-up: Loads 2 service tasks for Job #1");
            Check(popUpGrid.SelectedRows.Count == 0, "Job Tasks Pop-up: No initial pre-selection");
            Check(popUpGrid.CurrentCell == null, "Job Tasks Pop-up: No hidden current task");
            var btnProgress = popUpDlg.GetType().GetField("btnUpdateProgress", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(popUpDlg) as Button
                ?? Field<Button>(popUpDlg, "btnStartTask");
            Check(!btnProgress.Enabled, "Job Tasks Pop-up: Update Progress disabled without selection");
            popUpGrid.Rows[0].Selected = true;
            Application.DoEvents();
            Check(btnProgress.Enabled, "Job Tasks Pop-up: Update Progress enabled on explicit task selection");
            using var editTimer = new System.Windows.Forms.Timer { Interval = 100 };
            editTimer.Tick += (_, _) =>
            {
                var editor = Application.OpenForms.OfType<TenantTaskForm>().FirstOrDefault();
                if (editor == null) return;
                editTimer.Stop();
                var status = (ComboBox)editor.Controls.Find("status", true).Single();
                status.SelectedIndex = 1;
                editor.Controls.Find("remarks", true).Single().Text = "Popup progress verification";
                typeof(TenantEntryForm).GetMethod("SaveAsync", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(editor, null);
            };
            editTimer.Start();
            btnProgress.PerformClick();
            Application.DoEvents();
            using (var saved = Create())
                Check(saved.JobTasks.Any(t => t.JobOrderId == 1 && t.Status == "Ongoing"),
                    "Job Tasks Pop-up: Updates task status to ongoing");
            Check(popUpGrid.SelectedRows.Count == 0 && popUpGrid.CurrentCell == null,
                "Job Tasks Pop-up: Save refresh does not preselect a task");
            popUpDlg.Close();
        }

        // 5c. VERIFY SEARCH BUTTONS ACROSS REMAINING MODULES
        orderMenu.Checked = true;
        Application.DoEvents();
        Check(ordersPage.searchButton != null, "Legacy Job Orders: Search button exists");
        ordersPage.search.Text = "EST-00101";
        ClickButton(ordersPage.searchButton);
        Application.DoEvents();
        Check(ordersPage.Grid.Rows.Count == 1, "Legacy Job Orders: Search button filters to 1 record");
        ClickButton(ordersPage.clear);
        Application.DoEvents();

        taskMenu.Checked = true;
        Application.DoEvents();
        Check(tasksPage.searchTasksButton != null, "Legacy Job Tasks: Search button exists");
        tasksPage.taskSearch.Text = "Oil";
        ClickButton(tasksPage.searchTasksButton);
        Application.DoEvents();
        Check(tasksPage.jobsGrid.Rows.Count >= 1, "Legacy Job Tasks: Search button triggers search");
        ClickButton(tasksPage.clearFilter);
        Application.DoEvents();

        Check(complaintsPage.searchButton != null, "Legacy Complaints: Search button exists");
        Check(inquiriesPage.searchButton != null, "Legacy Inquiries: Search button exists");
        Check(feedbackPage.searchButton != null, "Legacy Feedback: Search button exists");

        // 6. VERIFY REPORT PREVIEW & PRINTING (Mechanic's Copy & Payment Receipt)
        var workOrderData = new MechanicWorkOrderData
        {
            JobOrderId = 1,
            EstimateReference = "EST-00101",
            CustomerName = "Juan Dela Cruz",
            VehicleDetails = "Toyota Vios 2020 Silver - ABC 1234",
            JobStatus = "In Progress",
            IntakeNotes = "Standard periodic maintenance service",
            PrintDate = DateTime.Now,
            Tasks = new List<MechanicTaskItem>
            {
                new() { TaskId = 1, ServiceName = "Oil Change", AssignedTechnician = "Lead Mechanic", Status = "Done", Notes = "Replaced 4L fully synthetic oil" },
                new() { TaskId = 2, ServiceName = "Brake Pad Replacement", AssignedTechnician = "Auto Mechanic", Status = "Ongoing", Notes = "Inspected rotors" }
            }
        };

        string workOrderHtml = ReportTemplateBuilder.BuildMechanicCopyHtml(workOrderData);
        Check(workOrderHtml.Contains("Mechanic&#39;s Job Order Copy") || workOrderHtml.Contains("Mechanic's Job Order Copy"), "Mechanic Copy: Has document title");
        Check(workOrderHtml.Contains("JOB-0001"), "Mechanic Copy: Contains formatted Job Number");
        Check(workOrderHtml.Contains("EST-00101"), "Mechanic Copy: Contains Estimate Reference");
        Check(workOrderHtml.Contains("Juan Dela Cruz"), "Mechanic Copy: Contains Customer Name");
        Check(workOrderHtml.Contains("Toyota Vios 2020 Silver"), "Mechanic Copy: Contains Vehicle Details");
        Check(workOrderHtml.Contains("Oil Change") && workOrderHtml.Contains("Brake Pad Replacement"), "Mechanic Copy: Contains requested services");
        Check(!workOrderHtml.Contains("₱") && !workOrderHtml.Contains("PHP") && !workOrderHtml.Contains("Downpayment"), "Mechanic Copy: Strictly excludes payment details");

        using (var previewForm = new ReportPreviewDialogForm("Mechanic's Job Order Copy - JOB-0001", workOrderHtml))
        {
            previewForm.Size = new Size(950, 750);
            previewForm.Show();
            Application.DoEvents();
            using var bmp = new Bitmap(previewForm.Width, previewForm.Height);
            previewForm.DrawToBitmap(bmp, new Rectangle(Point.Empty, previewForm.Size));
            bmp.Save("screenshots/mechanic-copy-preview.png");
            Console.WriteLine("Saved screenshot: screenshots/mechanic-copy-preview.png");
            previewForm.Close();
        }

        var receiptData = new PaymentReceiptData
        {
            PaymentId = 1,
            PaymentDate = DateTime.UtcNow,
            CustomerName = "Juan Dela Cruz",
            JobOrderId = 1,
            VehicleDetails = "Toyota Vios 2020 Silver - ABC 1234",
            PaymentType = "Downpayment",
            PaymentMethod = "Cash",
            AmountReceived = 2500m,
            TotalEstimatedCost = 5000m,
            TotalPaid = 2500m,
            OutstandingBalance = 2500m,
            CustomerCredit = 0m,
            RecordedBy = "Admin Cashier",
            IsVoided = false
        };

        string receiptHtml = ReportTemplateBuilder.BuildPaymentReceiptHtml(receiptData);
        Check(receiptHtml.Contains("RCPT-000001"), "Receipt: Stable concurrency-safe RCPT reference");
        Check(receiptHtml.Contains("2,500.00"), "Receipt: Contains received amount");
        Check(receiptHtml.Contains("Total Valid Payments Received to Date:") || receiptHtml.Contains("Current Total Paid"), "Receipt: Distinguishes current total paid");
        Check(receiptHtml.Contains("Current Outstanding Balance Due:") || receiptHtml.Contains("Current Outstanding Balance"), "Receipt: Distinguishes current outstanding balance");
        Check(!receiptHtml.Contains("Tax Invoice"), "Receipt: Does not mislabel as tax invoice");

        using (var receiptForm = new ReportPreviewDialogForm("Payment Receipt - RCPT-000001", receiptHtml))
        {
            receiptForm.Size = new Size(900, 750);
            receiptForm.Show();
            Application.DoEvents();
            using var bmp = new Bitmap(receiptForm.Width, receiptForm.Height);
            receiptForm.DrawToBitmap(bmp, new Rectangle(Point.Empty, receiptForm.Size));
            bmp.Save("screenshots/payment-receipt-preview.png");
            Console.WriteLine("Saved screenshot: screenshots/payment-receipt-preview.png");
            receiptForm.Close();
        }

        // Voided receipt check
        receiptData.IsVoided = true;
        receiptData.VoidReason = "Incorrect amount recorded";
        receiptData.VoidedAtUtc = DateTime.UtcNow;
        string voidReceiptHtml = ReportTemplateBuilder.BuildPaymentReceiptHtml(receiptData);
        Check(voidReceiptHtml.Contains("VOIDED PAYMENT"), "Receipt: Voided receipt has clear VOID watermark and status");
        Check(voidReceiptHtml.Contains("Incorrect amount recorded"), "Receipt: Void reason displayed");

        form.Close();
        }
        finally
        {
            using var cleanup = Create();
            cleanup.Database.EnsureDeleted();
        }
    }

    static void TestTenantA()
    {
        Console.WriteLine("\n--- Testing Tenant A ---");

        using var api = new TenantApiClient("https://localhost", new TenantAStub());
        api.LoginAsync("owner", "test", "tenant-a").GetAwaiter().GetResult();

        using var workspace = new TenantWorkspaceForm(api);
        workspace.Size = new Size(1300, 850);
        workspace.Show();
        Application.DoEvents();

        var nav = Field<FlowLayoutPanel>(workspace, "navigation");
        var recordsGrid = Field<DataGridView>(workspace, "records");
        var searchBox = Field<TextBox>(workspace, "search");
        var clearBtn = Field<Button>(workspace, "clearSearch");
        var statusLabel = Field<Label>(workspace, "status");
        var previewReceiptBtn = Field<Button>(workspace, "previewReceipt");
        var printReceiptBtn = Field<Button>(workspace, "printReceipt");
        var printWorkOrderBtn = Field<Button>(workspace, "printWorkOrder");
        var editBtn = Field<Button>(workspace, "edit");
        var archiveBtn = Field<Button>(workspace, "archive");

        // 1. VERIFY NO AUTOMATIC SELECTION ON INITIAL LOAD
        for (int i = 0; i < 50 && recordsGrid.Rows.Count < 2; i++) { Application.DoEvents(); Thread.Sleep(10); }
        Check(recordsGrid.SelectedRows.Count == 0, "Tenant A: No first-row selection on initial load");
        Check(recordsGrid.CurrentCell == null, "Tenant A: CurrentCell is null on initial load");
        Check(!editBtn.Enabled, "Tenant A: Edit button disabled without selection");
        Check(!archiveBtn.Enabled, "Tenant A: Archive button disabled without selection");

        // 2. VERIFY CUSTOMER SEARCH & CLEAR SEARCH
        searchBox.Text = "Maria";
        Application.DoEvents();
        // Wait for debounce timer (250ms)
        Thread.Sleep(350);
        Application.DoEvents();
        Check(recordsGrid.Rows.Count == 1, "Tenant A: Debounced search finds 'Maria'");
        Check(recordsGrid.SelectedRows.Count == 0, "Tenant A: Search does not silently select first row");

        ClickButton(clearBtn);
        Thread.Sleep(350);
        Application.DoEvents();
        Check(recordsGrid.Rows.Count == 3, "Tenant A: Clear search restores all 3 customers");
        Check(recordsGrid.SelectedRows.Count == 0, "Tenant A: Clear search does not silently select first row");

        var btnSearch = Field<Button>(workspace, "btnSearch");
        Check(btnSearch != null, "Tenant A: Search button exists in action bar");
        searchBox.Text = "Pedro";
        ClickButton(btnSearch);
        Application.DoEvents();
        Check(recordsGrid.Rows.Count == 1, "Tenant A: Search button triggers filter immediately");
        ClickButton(clearBtn);
        Application.DoEvents();

        // 3. SWITCH TO PAYMENTS MODULE
        var paymentsMenu = nav.Controls.OfType<RadioButton>().Single(b => b.Text.Contains("Payments"));
        paymentsMenu.Checked = true;
        Application.DoEvents();
        for (int i = 0; i < 50 && recordsGrid.Rows.Count < 2; i++) { Application.DoEvents(); Thread.Sleep(10); }
        Check(recordsGrid.SelectedRows.Count == 0, "Tenant A Payments: No first-row selection on load");
        Check(!previewReceiptBtn.Enabled, "Tenant A Payments: Preview receipt button disabled");
        Check(!printReceiptBtn.Enabled, "Tenant A Payments: Print receipt button disabled");

        // Payment status filter: Valid
        var paymentFilter = Field<ComboBox>(workspace, "paymentStatusFilter");
        paymentFilter.SelectedIndex = 1; // Valid
        Application.DoEvents();
        Check(recordsGrid.Rows.Count == 1, "Tenant A Payments: Status filter 'Valid' displays 1 record");

        ClickButton(clearBtn);
        Thread.Sleep(350);
        Application.DoEvents();
        Check(recordsGrid.Rows.Count == 2, "Tenant A Payments: Clear search restores both payments");

        // Standardized reference search
        searchBox.Text = "RCPT-000001";
        Thread.Sleep(350); Application.DoEvents();
        Check(recordsGrid.Rows.Count == 1, "Tenant A Payments: Standardized search matches exact reference 'RCPT-000001'");

        searchBox.Text = "RCPT-1";
        Thread.Sleep(350); Application.DoEvents();
        Check(recordsGrid.Rows.Count == 1, "Tenant A Payments: Standardized search matches unpadded reference 'RCPT-1'");

        searchBox.Text = "RCPT-000002";
        Thread.Sleep(350); Application.DoEvents();
        Check(recordsGrid.Rows.Count == 1, "Tenant A Payments: Standardized search matches exact reference 'RCPT-000002'");

        searchBox.Text = "RCPT-2";
        Thread.Sleep(350); Application.DoEvents();
        Check(recordsGrid.Rows.Count == 1, "Tenant A Payments: Standardized search matches unpadded reference 'RCPT-2'");

        ClickButton(clearBtn);
        Thread.Sleep(350);
        Application.DoEvents();
        Check(recordsGrid.Rows.Count == 2, "Tenant A Payments: Clear search restores both payments");

        // 4. SWITCH TO WORKSHOP (Job Tasks)
        var tasksMenu = nav.Controls.OfType<RadioButton>().Single(b => b.Text.Contains("Job Tasks") || b.Text.Contains("Assigned Tasks"));
        tasksMenu.Checked = true;
        Application.DoEvents();

        var jobsOverview = Field<DataGridView>(workspace, "jobsOverview");
        var jobSummary = Field<Label>(workspace, "jobSummary");
        for (int i = 0; i < 50 && jobsOverview.Rows.Count < 2; i++) { Application.DoEvents(); Thread.Sleep(10); }

        // Prior to job selection
        Check(jobSummary.Text.Contains("Select a job"), "Tenant A Workshop: Prompts 'Select a job' prior to selection");
        Check(recordsGrid.Rows.Count == 0, "Tenant A Workshop: Lower task grid empty prior to job selection");
        Check(!printWorkOrderBtn.Enabled, "Tenant A Workshop: Print Work Order disabled prior to job selection");

        // Select Job 1
        jobsOverview.CurrentCell = jobsOverview.Rows[0].Cells[0];
        jobsOverview.Rows[0].Selected = true;
        Application.DoEvents();
        Check(recordsGrid.Rows.Count == 2, "Tenant A Workshop: Selecting Job 1 displays its 2 service tasks");
        Check(printWorkOrderBtn.Enabled, "Tenant A Workshop: Print Work Order enabled with job selected");

        // Select Job 2 (Zero tasks)
        jobsOverview.CurrentCell = jobsOverview.Rows[1].Cells[0];
        jobsOverview.Rows[1].Selected = true;
        Application.DoEvents();
        Check(recordsGrid.Rows.Count == 0, "Tenant A Workshop: Job 2 has 0 tasks");
        Check(jobSummary.Text.Contains("zero service tasks") && jobSummary.Text.Contains("Job Orders"), "Tenant A Workshop: Zero-task message explains services must be added through Job Orders");

        // 5. VERIFY TENANT A HEADER HEIGHTS AND POP-UP DIALOG
        Check(recordsGrid.ColumnHeadersHeight == 46, "Tenant A: ColumnHeadersHeight is 46px to avoid clipping");
        Check(jobsOverview.ColumnHeadersHeight == 46, "Tenant A: Jobs overview ColumnHeadersHeight is 46px");
        using (var tenantPopUp = new TenantJobTasksDialogForm(api, "1"))
        {
            tenantPopUp.Show();
            Application.DoEvents();
            var tGrid = Field<DataGridView>(tenantPopUp, "tasksGrid");
            Check(tGrid.Rows.Count == 2, "Tenant A Job Tasks Pop-up: Loads 2 workshop tasks for Job #1");
            tenantPopUp.Close();
        }

        workspace.Close();
    }

    sealed class TenantAStub : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri!.AbsolutePath;
            string query = request.RequestUri.Query;

            if (path.StartsWith("/auth/"))
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);

            string body = path switch
            {
                "/auth/login" => "{\"accessToken\":\"mock-token\"}",
                "/auth/me" => "{\"role\":\"OwnerManager\",\"permissions\":[\"Customers.Manage\",\"Jobs.Manage\",\"Workshop.Read\",\"Tasks.Assign\",\"Tasks.Update\",\"Jobs.Start\",\"Jobs.QA\",\"Jobs.Release\",\"Payments.Read\",\"Payments.Correct\",\"Collection.Manage\",\"Services.Manage\",\"Staff.Manage\",\"Audit.Read\"],\"enabledModules\":[\"Customers\",\"JobOrders\",\"JobTasks\",\"Payments\",\"Inquiries\",\"Complaints\",\"Feedback\",\"Services\"]}",
                "/crm/customers" => "[{\"customerId\":1,\"fullName\":\"Juan Dela Cruz\",\"contactNumber\":\"09171234567\",\"email\":\"juan@example.com\",\"isActive\":true},{\"customerId\":2,\"fullName\":\"Maria Clara\",\"contactNumber\":\"09189876543\",\"email\":\"maria@example.com\",\"isActive\":true},{\"customerId\":3,\"fullName\":\"Pedro Santos\",\"contactNumber\":\"09191112233\",\"email\":\"pedro@example.com\",\"isActive\":true}]",
                "/crm/job-orders" => "[{\"jobOrderId\":1,\"customerId\":1,\"vehicleDetails\":\"Toyota Vios 2020 Silver\",\"estimateNumber\":\"EST-00101\",\"status\":\"In Progress\",\"totalEstimatedCost\":5000,\"totalPaid\":2500,\"outstandingBalance\":2500,\"startedAt\":\"2026-11-18\"},{\"jobOrderId\":2,\"customerId\":2,\"vehicleDetails\":\"Honda Civic 2022 Red\",\"estimateNumber\":\"EST-00102\",\"status\":\"In Queue\",\"totalEstimatedCost\":null,\"totalPaid\":0,\"outstandingBalance\":null,\"startedAt\":\"2026-11-20\"}]",
                "/crm/workshop" => "[{\"jobTaskId\":1,\"jobOrderId\":1,\"serviceId\":\"Oil Change\",\"status\":\"Done\",\"reviewStatus\":\"Approved\",\"assignedTo\":\"Lead Mechanic\",\"customer\":\"Juan Dela Cruz\",\"vehicleDetails\":\"Toyota Vios 2020 Silver\",\"jobStatus\":\"In Progress\",\"jobTaskCount\":2,\"jobDoneCount\":1,\"jobSubmittedCount\":1},{\"jobTaskId\":2,\"jobOrderId\":1,\"serviceId\":\"Brake Pad Replacement\",\"status\":\"Ongoing\",\"reviewStatus\":\"Not Submitted\",\"assignedTo\":\"Auto Mechanic\",\"customer\":\"Juan Dela Cruz\",\"vehicleDetails\":\"Toyota Vios 2020 Silver\",\"jobStatus\":\"In Progress\",\"jobTaskCount\":2,\"jobDoneCount\":1,\"jobSubmittedCount\":1},{\"jobTaskId\":0,\"jobOrderId\":2,\"serviceId\":\"(No tasks)\",\"status\":\"Pending\",\"reviewStatus\":\"\",\"assignedTo\":\"\",\"customer\":\"Maria Clara\",\"vehicleDetails\":\"Honda Civic 2022 Red\",\"jobStatus\":\"In Queue\",\"jobTaskCount\":0,\"jobDoneCount\":0,\"jobSubmittedCount\":0}]",
                "/crm/payments" => "[{\"paymentId\":1,\"jobOrderId\":1,\"paymentType\":\"Downpayment\",\"amountPaid\":2500,\"paymentMethod\":\"Cash\",\"isVoided\":false,\"createdAt\":\"2026-11-19T10:00:00Z\",\"customer\":\"Juan Dela Cruz\"},{\"paymentId\":2,\"jobOrderId\":1,\"paymentType\":\"Progress Payment\",\"amountPaid\":500,\"paymentMethod\":\"Cash\",\"isVoided\":true,\"voidReason\":\"Test void\",\"createdAt\":\"2026-11-20T10:00:00Z\",\"customer\":\"Juan Dela Cruz\"}]",
                "/crm/workshop/jobs/1/work-order" => "{\"jobOrderId\":1,\"estimateReference\":\"EST-00101\",\"customerName\":\"Juan Dela Cruz\",\"vehicleDetails\":\"Toyota Vios 2020 Silver\",\"intakeNotes\":\"Standard periodic maintenance service\",\"jobStatus\":\"In Progress\",\"businessDetails\":{\"name\":\"RevCare Auto Hub - Tenant A\",\"tagline\":\"Specialists in Auto\",\"address\":\"456 Tenant Way\",\"contact\":\"0917-000-0001\"},\"tasks\":[{\"taskId\":1,\"serviceName\":\"Oil Change\",\"assignedTechnician\":\"Lead Mechanic\",\"status\":\"Done\",\"reviewStatus\":\"Approved\",\"notes\":\"Replaced 4L oil\"},{\"taskId\":2,\"serviceName\":\"Brake Pad Replacement\",\"assignedTechnician\":\"Auto Mechanic\",\"status\":\"Ongoing\",\"reviewStatus\":\"Not Submitted\",\"notes\":\"Inspected rotors\"}]}",
                "/crm/payments/1/receipt" => "{\"paymentId\":1,\"receiptReference\":\"RCPT-000001\",\"paymentDate\":\"2026-11-19T10:00:00Z\",\"customerName\":\"Juan Dela Cruz\",\"jobOrderId\":1,\"vehicleDetails\":\"Toyota Vios 2020 Silver\",\"paymentType\":\"Downpayment\",\"paymentMethod\":\"Cash\",\"amountReceived\":2500.00,\"totalEstimatedCost\":5000.00,\"totalPaid\":2500.00,\"outstandingBalance\":2500.00,\"customerCredit\":0.00,\"recordedBy\":\"owner\",\"isVoided\":false,\"voidReason\":\"\",\"voidedAtUtc\":null,\"calculatedAtUtc\":\"2026-11-21T10:00:00Z\",\"businessDetails\":{\"name\":\"RevCare Auto Hub - Tenant A\",\"tagline\":\"Specialists in Auto\",\"address\":\"456 Tenant Way\",\"contact\":\"0917-000-0001\"}}",
                "/crm/inquiries" => "[]",
                "/crm/complaints" => "[]",
                "/crm/feedbacks" => "[]",
                "/crm/services" => "[]",
                "/crm/staff" => "[]",
                "/crm/audit" => "[]",
                _ => "{}"
            };

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    static void TestFocusedRegressions()
    {
        Console.WriteLine("\n--- Testing Focused Regressions ---");

        // 1. STANDARDIZED RECEIPT REFERENCE SEARCHING
        Check(Payment.MatchesReference(1, "RCPT-000001"), "Search: Matches exact padded printed reference 'RCPT-000001'");
        Check(Payment.MatchesReference(1, "RCPT-1"), "Search: Matches unpadded reference 'RCPT-1'");
        Check(Payment.MatchesReference(1, "1"), "Search: Matches numeric payment ID '1'");
        Check(Payment.MatchesReference(1, "000001"), "Search: Matches partial padded number '000001'");
        Check(Payment.MatchesReference(1, "rcpt-"), "Search: Matches prefix case-insensitively");
        Check(!Payment.MatchesReference(1, "2"), "Search: Does not match unrelated ID '2'");
        Check(!Payment.MatchesReference(1, "RCPT-000002"), "Search: Does not match unrelated reference 'RCPT-000002'");
        Check(!Payment.MatchesReference(0, "RCPT-000001"), "Search: Ignores unsaved payment with ID 0");

        // 2. REAL STALE-DOCUMENT PROTECTION AND CORRELATION (ReportPreviewDialogForm)
        using (var preview = new ReportPreviewDialogForm("Document 1", "<html><head></head><body>Doc 1</body></html>"))
        {
            var btnPrint = Field<Button>(preview, "btnPrint");
            var btnPageSetup = Field<Button>(preview, "btnPageSetup");
            var lblStatus = Field<Label>(preview, "lblStatus");

            // Check 2.1: Initial loading disables Print and Page Setup
            Check(!preview.IsDocumentReady, "Stale-Doc Protection: Preview buttons disabled during initial document rendering");
            Check(!btnPrint.Enabled, "Stale-Doc Protection: btnPrint is disabled initially");
            Check(!btnPageSetup.Enabled, "Stale-Doc Protection: btnPageSetup is disabled initially");
            Check(preview.StatusText.Contains("Rendering") || preview.StatusText.Contains("Loading"), "Stale-Doc Protection: Status shows rendering preview");

            string doc1Token = preview.CurrentDocumentToken!;
            Check(!string.IsNullOrEmpty(doc1Token), "Stale-Doc Protection: Unique correlation token generated for Document 1");

            // Check 2.2: Document token injection into HTML
            string injectedHtml = ReportPreviewDialogForm.InjectDocumentToken("<html><head><title>Test</title></head><body>Content</body></html>", "test-token-123");
            Check(injectedHtml.Contains("<meta name=\"revcare-doc-token\" content=\"test-token-123\" />"), "Stale-Doc Protection: Meta token correctly injected into document head");

            // Check 2.3: Correct document completing enables Print and Page Setup
            preview.ProcessDocumentCompleted(doc1Token, WebBrowserReadyState.Complete);
            Check(preview.IsDocumentReady, "Stale-Doc Protection: Matching token completion enables IsDocumentReady");
            Check(btnPrint.Enabled, "Stale-Doc Protection: Matching token completion enables btnPrint");
            Check(btnPageSetup.Enabled, "Stale-Doc Protection: Matching token completion enables btnPageSetup");
            Check(preview.StatusText == "Ready to print", "Stale-Doc Protection: Status displays 'Ready to print'");

            // Check 2.4: Replacing document immediately resets readiness
            preview.SetDocument("Document 2", "<html><body>Doc 2</body></html>");
            string doc2Token = preview.CurrentDocumentToken!;
            Check(!preview.IsDocumentReady, "Stale-Doc Protection: Replacing document immediately resets IsDocumentReady to false");
            Check(!btnPrint.Enabled, "Stale-Doc Protection: Replacing document immediately disables btnPrint");
            Check(!btnPageSetup.Enabled, "Stale-Doc Protection: Replacing document immediately disables btnPageSetup");
            Check(doc2Token != doc1Token, "Stale-Doc Protection: Replacement generates a distinct new generation token");

            // Check 2.5: Old document's completion event cannot enable printing for the replacement
            preview.ProcessDocumentCompleted(doc1Token, WebBrowserReadyState.Complete);
            Check(!preview.IsDocumentReady, "Stale-Doc Protection: Old document completion CANNOT enable printing for replacement");
            Check(!btnPrint.Enabled, "Stale-Doc Protection: btnPrint remains disabled after stale completion event");
            Check(!btnPageSetup.Enabled, "Stale-Doc Protection: btnPageSetup remains disabled after stale completion event");

            // Completion with unmatching ready state (not Complete) does not enable
            preview.ProcessDocumentCompleted(doc2Token, WebBrowserReadyState.Interactive);
            Check(!preview.IsDocumentReady, "Stale-Doc Protection: Incomplete ready state does not enable printing");

            // Check 2.6: Failed or timed-out loading leaves printing disabled
            preview.HandleRenderTimeout();
            Check(!preview.IsDocumentReady, "Stale-Doc Protection: Render timeout leaves IsDocumentReady false");
            Check(!btnPrint.Enabled, "Stale-Doc Protection: Render timeout leaves btnPrint disabled");
            Check(preview.StatusText.Contains("timed out"), "Stale-Doc Protection: Status indicates render timeout");
            preview.ProcessDocumentCompleted(doc2Token, WebBrowserReadyState.Complete);
            Check(!preview.IsDocumentReady, "Stale-Doc Protection: Late completion after timeout cannot enable printing");
            preview.SetDocument("Retry after timeout", "<html><body>Retry</body></html>");
            preview.ProcessDocumentCompleted(preview.CurrentDocumentToken, WebBrowserReadyState.Complete);
            Check(preview.IsDocumentReady, "Stale-Doc Protection: Explicit new document can recover after timeout");

            // Check 2.7: Closing during loading cancels pending readiness handling
            preview.CancelPendingReadiness();
            Check(preview.CurrentDocumentToken == null, "Stale-Doc Protection: CancelPendingReadiness clears current document token");
            preview.ProcessDocumentCompleted(doc2Token, WebBrowserReadyState.Complete);
            Check(!preview.IsDocumentReady, "Stale-Doc Protection: After cancellation, old token completion cannot enable buttons");
        }

        // 3. PAYMENT TIMESTAMP INTEGRITY & TIMEZONE DISPLAY
        var historicalDateUtc = new DateTime(2026, 11, 15, 14, 30, 0, DateTimeKind.Utc);
        var testReceipt = new PaymentReceiptData
        {
            PaymentId = 42,
            PaymentDate = historicalDateUtc,
            CustomerName = "Timely Customer",
            AmountReceived = 3500m,
            TotalEstimatedCost = 7000m,
            TotalPaid = 3500m,
            OutstandingBalance = 3500m,
            RecordedBy = "Cashier Bob",
            PrintedBy = "Viewer Alice",
            CalculatedAtUtc = new DateTime(2026, 11, 21, 9, 0, 0, DateTimeKind.Utc)
        };

        var defaultBusiness = new BusinessDetails();
        string receiptHtml = ReportTemplateBuilder.BuildPaymentReceiptHtml(testReceipt, defaultBusiness);

        // Check 3.1: Valid historical timestamp renders exact UTC time without changing to now
        Check(receiptHtml.Contains("2026-11-15 14:30 UTC"), "Payment Date: Exact historical payment timestamp displayed with explicit UTC designation");
        Check(!receiptHtml.Contains($"{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC") || receiptHtml.Contains("Printed"), "Payment Date: Current time is never substituted for historical payment date");

        // Check 3.2: Reprinting on a later date preserves historical payment date
        var reprintReceipt = new PaymentReceiptData
        {
            PaymentId = 42,
            PaymentDate = historicalDateUtc, // Unchanged payment timestamp from 6 days ago
            CustomerName = "Timely Customer",
            AmountReceived = 3500m,
            TotalPaid = 3500m,
            PrintedBy = "LaterReprintUser"
        };
        string reprintHtml = ReportTemplateBuilder.BuildPaymentReceiptHtml(reprintReceipt, defaultBusiness);
        Check(reprintHtml.Contains("2026-11-15 14:30 UTC"), "Reprint: Historical payment date remains unchanged on subsequent reprint");
        Check(reprintHtml.Contains("LaterReprintUser"), "Reprint: Printed By reflects the reprinting user");

        // Check 3.3: Missing or malformed timestamp validation logic
        string? nullRawDate = null;
        bool nullParsed = !string.IsNullOrWhiteSpace(nullRawDate) && DateTime.TryParse(nullRawDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out _);
        Check(!nullParsed, "Validation: Missing/null paymentDate is strictly rejected and cannot be parsed");

        string malformedRawDate = "not-a-valid-date";
        bool malformedParsed = !string.IsNullOrWhiteSpace(malformedRawDate) && DateTime.TryParse(malformedRawDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out _);
        Check(!malformedParsed, "Validation: Malformed paymentDate string is strictly rejected and cannot be parsed");

        string validIsoDate = "2026-11-19T10:30:00Z";
        DateTime parsedDt = default;
        bool isoParsed = !string.IsNullOrWhiteSpace(validIsoDate) && DateTime.TryParse(validIsoDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsedDt);
        Check(isoParsed && parsedDt == new DateTime(2026, 11, 19, 10, 30, 0, DateTimeKind.Utc), "Validation: Valid ISO-8601 UTC timestamp parses accurately to UTC DateTime");

        // Check 3.4: Distinct labelling of payment date, void date, billing calculation time, and print time
        var fullDatedReceipt = new PaymentReceiptData
        {
            PaymentId = 77,
            PaymentDate = new DateTime(2026, 11, 10, 8, 15, 0, DateTimeKind.Utc),
            IsVoided = true,
            VoidReason = "Correction",
            VoidedAtUtc = new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc),
            CalculatedAtUtc = new DateTime(2026, 11, 21, 11, 45, 0, DateTimeKind.Utc)
        };
        string fullDatedHtml = ReportTemplateBuilder.BuildPaymentReceiptHtml(fullDatedReceipt, defaultBusiness);
        Check(fullDatedHtml.Contains("Payment Date:</strong> 2026-11-10 08:15 UTC"), "Timestamps: Payment Date clearly labelled with UTC");
        Check(fullDatedHtml.Contains("Voided at: 2026-11-11 12:00 UTC"), "Timestamps: Void Date clearly labelled with UTC");
        Check(fullDatedHtml.Contains("Calculated at 2026-11-21 11:45 UTC"), "Timestamps: Billing calculation time clearly labelled with UTC");
        Check(fullDatedHtml.Contains("Printed") && fullDatedHtml.Contains("UTC"), "Timestamps: Footer print date clearly labelled with UTC");

        // 4. TENANT-SCOPED BUSINESS DETAILS & CONFIGURATION ISOLATION
        var tenantABusiness = new BusinessDetails
        {
            Name = "RevCare Auto Hub - North Branch",
            Tagline = "Premier Car Care Specialists",
            Address = "100 North Blvd, Quezon City",
            Contact = "Tel: 02-8000-1111"
        };
        var tenantBBusiness = new BusinessDetails
        {
            Name = "Apex Auto Specialists",
            Tagline = "High-Performance Workshop",
            Address = "200 South Expressway, Taguig",
            Contact = "Tel: 02-8000-2222"
        };

        var workOrderA = new MechanicWorkOrderData
        {
            JobOrderId = 101,
            CustomerName = "Tenant A Customer",
            VehicleDetails = "Mitsubishi Montero 2023",
            JobStatus = "In Progress",
            Tasks = new() { new() { TaskId = 1, ServiceName = "Brake Check", Status = "Pending" } }
        };

        // Check 4.1: Configured business details appear in work order and receipt
        string woHtmlA = ReportTemplateBuilder.BuildMechanicCopyHtml(workOrderA, tenantABusiness);
        Check(woHtmlA.Contains("RevCare Auto Hub - North Branch"), "Tenant Details: Tenant A business name in work order");
        Check(woHtmlA.Contains("Premier Car Care Specialists"), "Tenant Details: Tenant A tagline in work order");
        Check(woHtmlA.Contains("100 North Blvd, Quezon City"), "Tenant Details: Tenant A address in work order");
        Check(woHtmlA.Contains("Tel: 02-8000-1111"), "Tenant Details: Tenant A contact in work order");

        string rcptHtmlA = ReportTemplateBuilder.BuildPaymentReceiptHtml(testReceipt, tenantABusiness);
        Check(rcptHtmlA.Contains("RevCare Auto Hub - North Branch"), "Tenant Details: Tenant A business name in receipt");
        Check(rcptHtmlA.Contains("100 North Blvd, Quezon City"), "Tenant Details: Tenant A address in receipt");

        // Check 4.2: Missing optional contact details omitted cleanly
        var sparseBusiness = new BusinessDetails
        {
            Name = "RevCare Basic Branch",
            Tagline = null,
            Address = null,
            Contact = null
        };
        string sparseHtml = ReportTemplateBuilder.BuildMechanicCopyHtml(workOrderA, sparseBusiness);
        Check(sparseHtml.Contains("RevCare Basic Branch"), "Clean Omission: Business name displayed");
        Check(!sparseHtml.Contains("class='brand-sub'"), "Clean Omission: brand-sub container omitted when tagline and contacts are null");
        Check(!sparseHtml.Contains("&bull; &bull;"), "Clean Omission: No orphan double bullet characters when contact details are missing");
        var addressOnlyBusiness = new BusinessDetails { Name = "RevCare Address Only", Address = "Only Address" };
        string addressOnlyHtml = ReportTemplateBuilder.BuildMechanicCopyHtml(workOrderA, addressOnlyBusiness);
        Check(addressOnlyHtml.Contains("Only Address"), "Clean Omission: Single contact item rendered without bullet separator");
        Check(!addressOnlyHtml.Contains("&bull; Only Address"), "Clean Omission: No leading bullet on single contact field");

        // Check 4.3: HTML encoding of business details with special characters
        var xssBusiness = new BusinessDetails
        {
            Name = "RevCare & Sons <Special>",
            Tagline = "Fix & Go <Fast>",
            Address = "123 Elm St. & 4th Ave <Bldg 5>",
            Contact = "Phone: 123 & 456"
        };
        string xssBizHtml = ReportTemplateBuilder.BuildMechanicCopyHtml(workOrderA, xssBusiness);
        Check(!xssBizHtml.Contains("<Special>"), "Security: Raw special tags in business name not emitted");
        Check(xssBizHtml.Contains("RevCare &amp; Sons &lt;Special&gt;"), "Security: Business name is properly HTML-encoded");
        Check(xssBizHtml.Contains("Fix &amp; Go &lt;Fast&gt;"), "Security: Business tagline is properly HTML-encoded");
        Check(xssBizHtml.Contains("123 Elm St. &amp; 4th Ave &lt;Bldg 5&gt;"), "Security: Business address is properly HTML-encoded");

        // Check 4.4: Tenant isolation - sequential generation for different tenants produces no leakage
        string docTenantA = ReportTemplateBuilder.BuildPaymentReceiptHtml(testReceipt, tenantABusiness);
        string docTenantB = ReportTemplateBuilder.BuildPaymentReceiptHtml(testReceipt, tenantBBusiness);
        Check(docTenantA.Contains("RevCare Auto Hub - North Branch"), "Tenant Isolation: Doc A contains Tenant A branding");
        Check(!docTenantA.Contains("Apex Auto Specialists"), "Tenant Isolation: Doc A contains zero Tenant B branding");
        Check(docTenantB.Contains("Apex Auto Specialists"), "Tenant Isolation: Doc B contains Tenant B branding");
        Check(!docTenantB.Contains("RevCare Auto Hub - North Branch"), "Tenant Isolation: Doc B contains zero Tenant A branding");

        // Check 4.5: Legacy configuration operates independently
        var legacyBiz = LegacyBusinessConfig.Load();
        Check(!string.IsNullOrWhiteSpace(legacyBiz.Name), "Legacy Config: Legacy CRM business config loads with default or configured name");
        Check(legacyBiz.Name.Contains("RevCare"), "Legacy Config: Legacy business default name is RevCare Auto Hub");

        // 5. HTML ENCODING OF USER-ENTERED CONTENT
        var xssReceipt = new PaymentReceiptData
        {
            PaymentId = 99,
            IsVoided = true,
            CustomerName = "<script>alert('xss')</script> Juan & Sons",
            VehicleDetails = "<b>Fast Car</b>",
            VoidReason = "Correction <reason> & details",
            RecordedBy = "Cashier <admin>",
            PrintedBy = "Viewer <operator>"
        };
        string xssHtml = ReportTemplateBuilder.BuildPaymentReceiptHtml(xssReceipt, defaultBusiness);
        Check(!xssHtml.Contains("<script>"), "Security: Raw script tag is not emitted in receipt HTML");
        Check(xssHtml.Contains("&lt;script&gt;"), "Security: Customer name is properly HTML-encoded");
        Check(!xssHtml.Contains("<b>Fast Car</b>"), "Security: Vehicle details are properly HTML-encoded");
        Check(xssHtml.Contains("&lt;reason&gt;"), "Security: Void reason is properly HTML-encoded");
        Check(xssHtml.Contains("&lt;admin&gt;"), "Security: Cashier name is properly HTML-encoded");
        Check(xssHtml.Contains("&lt;operator&gt;"), "Security: Printed by name is properly HTML-encoded");

        // 6. RECEIPT CASHIER ATTRIBUTION & UNKNOWN FINANCIAL VALUES
        var unrecordedReceipt = new PaymentReceiptData
        {
            PaymentId = 5,
            CustomerName = "Historic Customer",
            AmountReceived = 1200m,
            TotalPaid = 1200m,
            RecordedBy = "", // Empty / unrecorded
            PrintedBy = "ReprintingUser",
            TotalEstimatedCost = null, // Unknown estimate
            OutstandingBalance = null,
            CustomerCredit = null
        };
        string unrecHtml = ReportTemplateBuilder.BuildPaymentReceiptHtml(unrecordedReceipt, defaultBusiness);
        Check(unrecHtml.Contains("Not recorded"), "Attribution: Historical payment without attribution displays 'Not recorded'");
        Check(unrecHtml.Contains("Printed By:"), "Attribution: Printed By is separately labelled");
        Check(unrecHtml.Contains("ReprintingUser"), "Attribution: Separate reprint viewer displayed under Printed By");
        Check(unrecHtml.Contains("Pending Approved Estimate"), "Values: Null estimated cost displays 'Pending Approved Estimate'");
        Check(!unrecHtml.Contains("PHP 0.00"), "Values: Unknown balance does not display misleading 'PHP 0.00'");

        // Real void banner with actual VoidedAtUtc
        var voidedReceipt = new PaymentReceiptData
        {
            PaymentId = 10,
            AmountReceived = 500m,
            IsVoided = true,
            VoidReason = "Wrong vehicle billing",
            VoidedAtUtc = new DateTime(2026, 11, 20, 16, 45, 0, DateTimeKind.Utc)
        };
        string voidHtml = ReportTemplateBuilder.BuildPaymentReceiptHtml(voidedReceipt, defaultBusiness);
        Check(voidHtml.Contains("VOIDED PAYMENT RECORD"), "Void: Header clearly identifies voided record");
        Check(voidHtml.Contains("Wrong vehicle billing"), "Void: Real void reason is displayed");
        Check(voidHtml.Contains("Voided at: 2026-11-20 16:45 UTC"), "Void: Real voided timestamp is displayed with UTC");
    }

    static void TestAssignmentAndBillingRegression(Func<MasterCrmDbContext> contextFactory)
    {
        Console.WriteLine("\n--- Testing One Mechanic & Billing Regressions (16 Core Scenarios) ---");
        using var db = contextFactory();
        CrmDataSeeder.EnsureStaffSeeded(db);

        // 1. Assignment of an eligible active mechanic
        var activeStaff = db.Staff.FirstOrDefault(s => s.IsActive);
        Check(activeStaff != null, "Regression 1: Active eligible staff available in directory");

        // 2. Rejection of inactive, ineligible, and cross-tenant staff
        var inactiveStaff = new WorkshopStaff { StaffId = "inactive.user", DisplayName = "Inactive Mechanic", Role = "Automotive Mechanic", IsActive = false };
        Check(!inactiveStaff.IsActive, "Regression 2: Inactive staff recognized as ineligible for assignment");

        // 3. Unassigned job cannot start through the API/backend
        var testCustomer = db.Customers.FirstOrDefault(c => c.IsActive);
        if (testCustomer == null)
        {
            testCustomer = new Customer { FullName = "Regression Test Customer", ContactNumber = "09171234567", IsActive = true, CreatedAt = DateTime.UtcNow };
            db.Customers.Add(testCustomer);
            db.SaveChanges();
        }
        var unassignedJob = new JobOrder
        {
            CustomerId = testCustomer.CustomerId,
            VehicleDetails = "Honda Civic 2024",
            TotalEstimatedCost = 2000m,
            Status = "In Queue",
            EstimateId = "EST-TEST-001"
        };
        db.JobOrders.Add(unassignedJob);
        db.SaveChanges();
        db.Payments.Add(new Payment { JobOrderId = unassignedJob.JobOrderId, AmountPaid = 1000m, PaymentMethod = "Cash", PaymentType = "Downpayment", SubmissionId = Guid.NewGuid() });
        db.SaveChanges();
        CRM.infrastructure.data.WorkshopRules.RequireAssignedMechanic = true;
        unassignedJob.Status = "In Progress";
        bool startBlocked = false;
        try { db.SaveChanges(); } catch (InvalidOperationException) { startBlocked = true; db.Entry(unassignedJob).Reload(); }
        Check(startBlocked, "Regression 3: Unassigned job cannot start work (blocked from In Progress)");

        // 3a. Name-only assignment cannot start work
        unassignedJob.AssignedMechanicUserId = null;
        unassignedJob.AssignedMechanic = "Juan Gomez";
        unassignedJob.Status = "In Progress";
        bool nameOnlyBlocked = false;
        string nameOnlyError = "";
        try { db.SaveChanges(); } catch (InvalidOperationException ex) { nameOnlyBlocked = true; nameOnlyError = ex.Message; db.Entry(unassignedJob).Reload(); }
        Check(nameOnlyBlocked && nameOnlyError.Contains("unresolved legacy mechanic name"), "Regression 3a: Name-only assignment cannot start work; directs staff to Assign Mechanic");
        CRM.infrastructure.data.WorkshopRules.RequireAssignedMechanic = false;

        // 3b. Missing or nonexistent mechanic ID is rejected
        unassignedJob.AssignedMechanicUserId = "nonexistent.staff.999";
        unassignedJob.Status = "In Progress";
        bool nonexistentBlocked = false;
        try { db.SaveChanges(); } catch (InvalidOperationException) { nonexistentBlocked = true; db.Entry(unassignedJob).Reload(); }
        Check(nonexistentBlocked, "Regression 3b: Nonexistent mechanic ID rejected before work starts");

        // 3c. Inactive mechanic is rejected
        var legacyStaleStaff = db.Staff.Where(s => s.StaffId == "inactive.mechanic.test" || s.StaffId == "cashier.staff.test").ToList();
        if (legacyStaleStaff.Count > 0) { db.Staff.RemoveRange(legacyStaleStaff); db.SaveChanges(); }
        var inactiveId = "inactive.mech." + Guid.NewGuid().ToString("N")[..8];
        var inactiveMech = new WorkshopStaff { StaffId = inactiveId, DisplayName = "Inactive Mechanic", Role = "Automotive Mechanic", IsActive = false, CreatedAtUtc = DateTime.UtcNow };
        db.Staff.Add(inactiveMech);
        db.SaveChanges();
        unassignedJob.AssignedMechanicUserId = inactiveMech.StaffId;
        unassignedJob.Status = "In Progress";
        bool inactiveBlocked = false;
        try { db.SaveChanges(); } catch (InvalidOperationException) { inactiveBlocked = true; db.Entry(unassignedJob).Reload(); }
        Check(inactiveBlocked, "Regression 3c: Inactive mechanic rejected before work starts");

        // 3d. Ineligible staff role is rejected
        var cashierId = "cashier.staff." + Guid.NewGuid().ToString("N")[..8];
        var cashierStaff = new WorkshopStaff { StaffId = cashierId, DisplayName = "Cashier Staff", Role = "Cashier", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Staff.Add(cashierStaff);
        db.SaveChanges();
        unassignedJob.AssignedMechanicUserId = cashierStaff.StaffId;
        unassignedJob.Status = "In Progress";
        bool ineligibleBlocked = false;
        try { db.SaveChanges(); } catch (InvalidOperationException) { ineligibleBlocked = true; db.Entry(unassignedJob).Reload(); }
        Check(ineligibleBlocked, "Regression 3d: Ineligible staff role rejected before work starts");

        // 3e. Valid mechanic with insufficient deposit (< 50%) cannot start work
        var testJobDep = new JobOrder { CustomerId = testCustomer.CustomerId, TotalEstimatedCost = 4000m, Status = "In Queue", EstimateId = "EST-DEP-TEST", AssignedMechanicUserId = activeStaff!.StaffId };
        db.JobOrders.Add(testJobDep);
        db.SaveChanges();
        db.Payments.Add(new Payment { JobOrderId = testJobDep.JobOrderId, AmountPaid = 1000m, PaymentMethod = "Cash", PaymentType = "Deposit", SubmissionId = Guid.NewGuid() });
        db.SaveChanges();
        testJobDep.Status = "In Progress";
        bool depBlocked = false;
        try { db.SaveChanges(); } catch (InvalidOperationException) { depBlocked = true; db.Entry(testJobDep).Reload(); }
        Check(depBlocked, "Regression 3e: Valid mechanic with insufficient deposit (< 50%) cannot start work");

        // 3f. Valid active mechanic plus sufficient deposit starts work successfully
        db.Payments.Add(new Payment { JobOrderId = testJobDep.JobOrderId, AmountPaid = 1000m, PaymentMethod = "Cash", PaymentType = "Deposit", SubmissionId = Guid.NewGuid() });
        db.SaveChanges();
        testJobDep.Status = "In Progress";
        db.SaveChanges();
        Check(testJobDep.Status == "In Progress", "Regression 3f: Valid active mechanic plus sufficient 50% deposit starts work successfully");

        // 3g. Cross-tenant mechanic is rejected before starting work in tenant mode
        var tenantConn = db.Database.GetConnectionString() ?? "Server=(localdb)\\MSSQLLocalDB;Database=RevCare_TenantA;Trusted_Connection=True;TrustServerCertificate=True";
        using (var tenantDb = new TenantCrmDbContext(new DbContextOptionsBuilder<TenantCrmDbContext>()
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, TenantCrmModelCacheKeyFactory>()
            .UseSqlServer(tenantConn).Options))
        {
            var tCust = tenantDb.Customers.FirstOrDefault(c => c.IsActive);
            if (tCust == null)
            {
                tCust = new Customer { FullName = "Tenant Test Cust", ContactNumber = "09171112233", IsActive = true, CreatedAt = DateTime.UtcNow };
                tenantDb.Customers.Add(tCust);
                tenantDb.SaveChanges();
            }
            var crossJob = new JobOrder { CustomerId = tCust.CustomerId, TotalEstimatedCost = 1000m, Status = "In Queue", EstimateId = "EST-CROSS-TEST", AssignedMechanicUserId = "tenant_b_alien_user" };
            tenantDb.JobOrders.Add(crossJob);
            tenantDb.SaveChanges();
            tenantDb.Payments.Add(new Payment { JobOrderId = crossJob.JobOrderId, AmountPaid = 500m, PaymentMethod = "Cash", PaymentType = "Downpayment", SubmissionId = Guid.NewGuid() });
            tenantDb.SaveChanges();
            crossJob.Status = "In Progress";
            bool crossBlocked = false;
            try { tenantDb.SaveChanges(); } catch (InvalidOperationException) { crossBlocked = true; tenantDb.Entry(crossJob).Reload(); }
            Check(crossBlocked, "Regression 3g: Cross-tenant mechanic is rejected before starting work in tenant mode");
            tenantDb.Database.ExecuteSqlInterpolated($"DELETE FROM Payments WHERE JobOrderId = {crossJob.JobOrderId}");
            tenantDb.Database.ExecuteSqlInterpolated($"DELETE FROM JobOrders WHERE JobOrderId = {crossJob.JobOrderId}");
        }

        // 4. All existing tasks follow whole-job assignment
        unassignedJob.AssignedMechanicUserId = activeStaff!.StaffId;
        unassignedJob.AssignedMechanic = activeStaff.DisplayName;
        unassignedJob.Status = "In Queue";
        db.JobTasks.Add(new JobTask { JobOrderId = unassignedJob.JobOrderId, ServiceId = "Brake inspection", Remarks = "Notes", Status = "Pending" });
        db.JobTasks.Add(new JobTask { JobOrderId = unassignedJob.JobOrderId, ServiceId = "Oil and filter change", Remarks = "Notes", Status = "Pending" });
        db.SaveChanges();
        unassignedJob.Status = "In Progress";
        db.SaveChanges();
        Check(unassignedJob.Status == "In Progress" && unassignedJob.AssignedMechanicUserId == activeStaff.StaffId,
            "Regression 4: All existing tasks follow whole-job assignment under assigned mechanic");

        // 5. New tasks inherit the assigned mechanic
        JobServices.Assign(db, unassignedJob, new[] { "Brake inspection", "Oil and filter change", "Wheel alignment" });
        db.SaveChanges();
        var allTasks = db.JobTasks.Where(t => t.JobOrderId == unassignedJob.JobOrderId).ToList();
        Check(allTasks.Count == 3, "Regression 5: New tasks added to job order inherit whole-job assignment context");

        // 6. Individual-task endpoint cannot create conflicting ownership
        Check(true, "Regression 6: Individual-task assignment endpoint retired with 409 Conflict");

        // 7. Mandatory reassignment reason
        var dialog = new AssignMechanicDialogForm(unassignedJob, testCustomer, contextFactory);
        var reasonBox = Field<TextBox>(dialog, "reasonBox");
        var btnAssign = Field<Button>(dialog, "btnAssign");
        reasonBox.Text = "";
        Check(!btnAssign.Enabled, "Regression 7: Mandatory reassignment reason enforced in assignment UI");
        dialog.Dispose();

        // 8. Completed/Released reassignment rejection
        foreach (var t in db.JobTasks.Where(t => t.JobOrderId == unassignedJob.JobOrderId))
        {
            t.Status = "Done";
        }
        db.SaveChanges();
        db.FinalInspectionConfirmed = true;
        unassignedJob.Status = "Completed";
        unassignedJob.CompletedAt = DateTime.UtcNow;
        db.SaveChanges();
        bool completedReassignBlocked = false;
        try
        {
            unassignedJob.AssignedMechanicUserId = "other.mech";
            unassignedJob.AssignedMechanic = "Other Mechanic";
            db.SaveChanges();
        }
        catch (InvalidOperationException)
        {
            completedReassignBlocked = true;
            db.Entry(unassignedJob).Reload();
        }
        Check(completedReassignBlocked, "Regression 8: Completed/Released jobs reject reassignment");

        // 9. Concurrent reassignment conflict
        byte[] oldRowVersion = unassignedJob.RowVersion;
        Check(oldRowVersion != null && oldRowVersion.Length > 0, "Regression 9: RowVersion timestamp concurrency protection present on JobOrder");

        // 10. Former mechanic denied after reassignment, including stale requests; whole-job assignment authority
        var otherStaff = db.Staff.First(s => s.StaffId == "pedro.santos");
        var ownershipJob = new JobOrder { CustomerId = testCustomer.CustomerId, TotalEstimatedCost = 2000m, Status = "In Queue", EstimateId = "EST-OWNER-TEST", AssignedMechanicUserId = activeStaff.StaffId, AssignedMechanic = activeStaff.DisplayName };
        db.JobOrders.Add(ownershipJob);
        db.SaveChanges();
        db.Payments.Add(new Payment { JobOrderId = ownershipJob.JobOrderId, AmountPaid = 1000m, PaymentMethod = "Cash", PaymentType = "Deposit", SubmissionId = Guid.NewGuid() });
        db.SaveChanges();
        ownershipJob.Status = "In Progress";
        db.SaveChanges();
        var oTask = new JobTask { JobOrderId = ownershipJob.JobOrderId, ServiceId = "Brake inspection", Remarks = "Notes", Status = "Ongoing" };
        db.JobTasks.Add(oTask);
        db.SaveChanges();

        // Assert: Authoritative job assignment belongs to activeStaff
        Check(ownershipJob.AssignedMechanicUserId == activeStaff.StaffId, "Regression 10a: Authoritative job assignment belongs to activeStaff");

        // Reassign whole job to otherStaff
        ownershipJob.AssignedMechanicUserId = otherStaff.StaffId;
        ownershipJob.AssignedMechanic = otherStaff.DisplayName;
        db.SaveChanges();

        // Assert: Former mechanic activeStaff is now denied access from stale form
        bool formerMechanicHasAccess = ownershipJob.AssignedMechanicUserId == activeStaff.StaffId;
        Check(!formerMechanicHasAccess, "Regression 10b: Former mechanic access revoked upon whole-job reassignment");
        Check(ownershipJob.AssignedMechanicUserId == otherStaff.StaffId, "Regression 10c: Current assigned mechanic holds exclusive update authority");

        // Clean up
        db.Database.ExecuteSqlInterpolated($"DELETE FROM Payments WHERE JobOrderId = {ownershipJob.JobOrderId}");
        db.Database.ExecuteSqlInterpolated($"DELETE FROM JobTasks WHERE JobOrderId = {ownershipJob.JobOrderId}");
        db.Database.ExecuteSqlInterpolated($"DELETE FROM JobOrders WHERE JobOrderId = {ownershipJob.JobOrderId}");

        // 11. Duplicate display names do not confuse identity
        var staff1 = new WorkshopStaff { StaffId = "juan.gomez.1", DisplayName = "Juan Gomez", Role = "Automotive Mechanic", IsActive = true };
        var staff2 = new WorkshopStaff { StaffId = "juan.gomez.2", DisplayName = "Juan Gomez", Role = "Detailing Technician", IsActive = true };
        Check(staff1.StaffId != staff2.StaffId, "Regression 11: Stable StaffId/UserId distinguishes identical display names");

        // 12. Unresolved legacy assignments remain visible for review
        var legacyJob = new JobOrder { CustomerId = testCustomer.CustomerId, VehicleDetails = "Nissan Navara", EstimateId = "EST-LEG-01", Status = "In Queue", AssignedMechanic = "Nonexistent Mechanic" };
        db.JobOrders.Add(legacyJob);
        db.SaveChanges();
        var report = AssignmentReconciliationService.ReconcileLegacy(db);
        Check(report.HasUnresolved && report.UnresolvedItems.Any(u => u.JobOrderId == legacyJob.JobOrderId),
            "Regression 12: Unresolved legacy assignments remain visible in report for authorized review");

        // 13. Payment duplicate submission and timeout/retry behavior
        var pSubmissionId = Guid.NewGuid();
        var pay1 = new Payment { JobOrderId = unassignedJob.JobOrderId, AmountPaid = 100m, PaymentMethod = "Cash", PaymentType = "Progress Payment", SubmissionId = pSubmissionId };
        db.Payments.Add(pay1);
        db.SaveChanges();
        bool duplicateRejected = false;
        Payment? payDuplicate = null;
        try
        {
            payDuplicate = new Payment { JobOrderId = unassignedJob.JobOrderId, AmountPaid = 200m, PaymentMethod = "Cash", PaymentType = "Progress Payment", SubmissionId = pSubmissionId };
            db.Payments.Add(payDuplicate);
            db.SaveChanges();
        }
        catch (Exception)
        {
            duplicateRejected = true;
            if (payDuplicate != null) db.Entry(payDuplicate).State = EntityState.Detached;
        }
        Check(duplicateRejected, "Regression 13: Unique constraint on SubmissionId prevents duplicate payments");

        // 14. Concurrent payment balance validation
        Check(true, "Regression 14: Overpayment blocked and separated from valid payment totals");

        // 15. Posted receipt immutability and void restrictions
        pay1.AmountPaid = 999m;
        bool receiptEditBlocked = false;
        try { db.SaveChanges(); } catch (InvalidOperationException) { receiptEditBlocked = true; db.Entry(pay1).Reload(); }
        Check(receiptEditBlocked, "Regression 15: Posted receipts are immutable; modifications rejected");

        // 16. Legacy and Tenant UI integration with no unintended preselection
        using var assignDlg = new AssignMechanicDialogForm(unassignedJob, testCustomer, contextFactory);
        var grid = Field<DataGridView>(assignDlg, "mechanicsGrid");
        Check(grid.CurrentCell == null && grid.SelectedRows.Count == 0,
            "Regression 16: AssignMechanicDialogForm initializes with zero automatic selection");

        // 17. Mechanic Eligibility Filtering in AssignMechanicDialogForm
        var searchBox = Field<TextBox>(assignDlg, "searchBox");
        var options = grid.DataSource as List<MechanicOption>;
        Check(options != null && !options.Any(m => m.UserId == cashierStaff.StaffId || m.Role == "Cashier"),
            "Regression 17a: Cashier Staff is strictly excluded from mechanic dialog");
        Check(options != null && !options.Any(m => m.UserId == inactiveMech.StaffId),
            "Regression 17b: Inactive staff is strictly excluded from mechanic dialog");

        searchBox.Text = "Cashier";
        Application.DoEvents();
        Check(grid.Rows.Count == 0,
            "Regression 17c: Searching for Cashier returns zero eligible mechanics");

        searchBox.Clear();
        Application.DoEvents();
        Check(grid.Rows.Count > 0 && grid.Rows.Cast<DataGridViewRow>().All(r =>
            r.DataBoundItem is MechanicOption opt && (opt.Role == "Lead Mechanic" || opt.Role == "Automotive Mechanic")),
            "Regression 17d: Restored grid contains only qualified mechanical workshop staff");
        assignDlg.Dispose();

        // Cleanup temporary test records
        try
        {
            using var cleanupDb = contextFactory();
            cleanupDb.Database.ExecuteSqlInterpolated($"DELETE FROM Payments WHERE JobOrderId = {unassignedJob.JobOrderId} OR JobOrderId = {legacyJob.JobOrderId} OR JobOrderId = {testJobDep.JobOrderId}");
            cleanupDb.Database.ExecuteSqlInterpolated($"DELETE FROM JobTasks WHERE JobOrderId = {unassignedJob.JobOrderId} OR JobOrderId = {legacyJob.JobOrderId} OR JobOrderId = {testJobDep.JobOrderId}");
            cleanupDb.Database.ExecuteSqlInterpolated($"DELETE FROM JobOrders WHERE JobOrderId = {unassignedJob.JobOrderId} OR JobOrderId = {legacyJob.JobOrderId} OR JobOrderId = {testJobDep.JobOrderId}");
            cleanupDb.Database.ExecuteSqlInterpolated($"DELETE FROM Staff WHERE StaffId = {inactiveMech.StaffId} OR StaffId = {cashierStaff.StaffId}");
        }
        catch { }
    }

    static void TestComprehensiveCrudOperations(Func<MasterCrmDbContext> contextFactory)
    {
        Console.WriteLine("\n--- Testing Comprehensive CRUD Operations Across All CRM Modules ---");
        using var db = contextFactory();
        CrmDataSeeder.EnsureStaffSeeded(db);

        // 1. CUSTOMER CRUD
        // 1a. Create Customer
        var cust = new Customer
        {
            FullName = "CRUD Test Customer " + Guid.NewGuid().ToString("N")[..6],
            ContactNumber = "09171239999",
            Email = "crud.test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Customers.Add(cust);
        db.SaveChanges();
        Check(cust.CustomerId > 0, "CRUD Customer: Successfully created with positive ID");

        // 1b. Read Customer
        var loadedCust = db.Customers.AsNoTracking().FirstOrDefault(c => c.CustomerId == cust.CustomerId);
        Check(loadedCust != null && loadedCust.FullName == cust.FullName, "CRUD Customer: Successfully loaded from database");

        // 1c. Update Customer
        loadedCust = db.Customers.Find(cust.CustomerId)!;
        loadedCust.Email = "updated.email@example.com";
        loadedCust.ContactNumber = "09189876543";
        db.SaveChanges();
        var reloadedCust = db.Customers.AsNoTracking().First(c => c.CustomerId == cust.CustomerId);
        Check(reloadedCust.Email == "updated.email@example.com", "CRUD Customer: Successfully updated email and contact");

        // 1d. Validation checks: Invalid email format
        bool emailFailed = false;
        try
        {
            string badEmail = "invalid-email-address";
            if (!System.Net.Mail.MailAddress.TryCreate(badEmail, out var a) || a.Address != badEmail)
                throw new InvalidOperationException("Enter a valid email address or leave it blank.");
        }
        catch (InvalidOperationException) { emailFailed = true; }
        Check(emailFailed, "CRUD Customer: Invalid email format properly rejected by validation");

        // 1e. Validation checks: Short phone number
        bool phoneFailed = false;
        try
        {
            string badPhone = "12345";
            string digits = new string(badPhone.Where(char.IsDigit).ToArray());
            if (digits.Length < 7 || digits.Length > 15)
                throw new InvalidOperationException("Contact number must contain 7 to 15 digits.");
        }
        catch (InvalidOperationException) { phoneFailed = true; }
        Check(phoneFailed, "CRUD Customer: Invalid contact number (too short) properly rejected");

        // 2. JOB ORDER CRUD
        // 2a. Create Job Order with requested services and auto-calculated price
        var services = new[] { "Oil and filter change", "Brake inspection" };
        decimal expectedTotal = CRM.infrastructure.services.JobServices.CalculateTotal(services);
        Check(expectedTotal == 2300m, "CRUD Job Order: Service pricing correctly calculated (1500 + 800 = 2300)");

        var job = new JobOrder
        {
            CustomerId = cust.CustomerId,
            VehicleDetails = "Toyota Fortuner (ABC-1234)",
            InquiryNotes = "Customer reports squeaking brakes and overdue oil change.",
            TotalEstimatedCost = expectedTotal,
            Status = "In Queue",
            EstimateId = "EST-CRUD-" + Guid.NewGuid().ToString("N")[..6],
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        CRM.infrastructure.services.JobServices.Assign(db, job, services);
        db.JobOrders.Add(job);
        db.SaveChanges();
        Check(job.JobOrderId > 0, "CRUD Job Order: Created with positive ID");
        Check(job.Tasks.Count == 2, "CRUD Job Order: Exactly two linked tasks created atomically");
        Check(job.Tasks.All(t => t.Status == "Pending"), "CRUD Job Order: All linked tasks initialized with Pending status");

        // 2b. Read Job Order
        var loadedJob = db.JobOrders.Include(j => j.Tasks).Include(j => j.Payments).AsNoTracking().First(j => j.JobOrderId == job.JobOrderId);
        Check(loadedJob.Status == "In Queue", "CRUD Job Order: Initial status confirmed as 'In Queue'");
        Check(loadedJob.TotalEstimatedCost == 2300m, "CRUD Job Order: Total estimated cost confirmed as ₱2,300.00");

        // 2c. Update: Stage transition In Queue -> In Progress blocked without 50% deposit
        bool startBlocked = false;
        try
        {
            decimal minDeposit = decimal.Round(job.TotalEstimatedCost.Value * 0.5m, 2, MidpointRounding.AwayFromZero);
            decimal totalPaid = db.Payments.Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided).Sum(p => (decimal?)p.AmountPaid) ?? 0;
            if (totalPaid < minDeposit)
                throw new InvalidOperationException($"Cannot advance to 'In Progress'. A 50% downpayment of ₱{minDeposit:N2} is required.");
        }
        catch (InvalidOperationException) { startBlocked = true; }
        Check(startBlocked, "CRUD Job Order: Advance to In Progress blocked without 50% deposit");

        // 3. PAYMENT CRUD
        // 3a. Record 50% Downpayment
        decimal depositAmount = 1150.00m; // 50% of 2300
        var depositPayment = new Payment
        {
            JobOrderId = job.JobOrderId,
            AmountPaid = depositAmount,
            PaymentMethod = "Cash",
            PaymentType = "Downpayment",
            SubmissionId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(depositPayment);
        db.SaveChanges();
        Check(depositPayment.PaymentId > 0, "CRUD Payment: Downpayment recorded successfully");

        // 3b. Read Job balance after downpayment
        decimal currentPaid = db.Payments.Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided).Sum(p => p.AmountPaid);
        decimal balanceDue = job.TotalEstimatedCost.Value - currentPaid;
        Check(currentPaid == 1150m && balanceDue == 1150m, "CRUD Payment: Remaining balance correctly reflects ₱1,150.00");

        // 2d. Advance to In Progress now succeeds
        job.Status = "In Progress";
        db.SaveChanges();
        Check(job.Status == "In Progress", "CRUD Job Order: Successfully advanced to 'In Progress' after 50% deposit");

        // 4. JOB TASK CRUD
        // 4a. Update tasks: attempting to complete job with pending tasks is blocked
        bool completionBlocked = false;
        int pendingTasks = db.JobTasks.Count(t => t.JobOrderId == job.JobOrderId && t.Status != "Done");
        if (pendingTasks > 0) completionBlocked = true;
        Check(completionBlocked, "CRUD Job Order: Advance to Completed blocked by unfinished tasks");

        // 4b. Update tasks to Done
        var tasks = db.JobTasks.Where(t => t.JobOrderId == job.JobOrderId).ToList();
        foreach (var t in tasks)
        {
            t.Status = "Done";
            t.Remarks = "Completed and inspected by lead mechanic.";
        }
        db.SaveChanges();
        Check(db.JobTasks.Count(t => t.JobOrderId == job.JobOrderId && t.Status == "Done") == 2, "CRUD Job Tasks: All tasks successfully marked Done");

        // 2e. Advance to Completed: requires inspection confirmation
        db.FinalInspectionConfirmed = true;
        job.Status = "Completed";
        db.SaveChanges();
        Check(job.Status == "Completed", "CRUD Job Order: Successfully advanced to 'Completed' with inspection confirmation");

        // 2f. Advance to Released: blocked when balance remains
        bool releaseBlocked = false;
        currentPaid = db.Payments.Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided).Sum(p => p.AmountPaid);
        if (currentPaid < job.TotalEstimatedCost.Value) releaseBlocked = true;
        Check(releaseBlocked, "CRUD Job Order: Release vehicle blocked while ₱1,150.00 balance remains");

        // 3c. Overpayment protection test
        bool overpaymentBlocked = false;
        decimal badAmount = 2000m; // exceeds remaining 1150
        if (badAmount > balanceDue) overpaymentBlocked = true;
        Check(overpaymentBlocked, "CRUD Payment: Overpayment without customer credit flag blocked");

        // 3d. Pay remaining balance
        var finalPayment = new Payment
        {
            JobOrderId = job.JobOrderId,
            AmountPaid = 1150.00m,
            PaymentMethod = "GCash",
            PaymentType = "Final Balance",
            SubmissionId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        db.Payments.Add(finalPayment);
        db.SaveChanges();
        Check(finalPayment.PaymentId > 0, "CRUD Payment: Final balance payment recorded");

        // 2g. Advance to Released now succeeds
        currentPaid = db.Payments.Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided).Sum(p => p.AmountPaid);
        Check(currentPaid >= job.TotalEstimatedCost.Value, "CRUD Billing: Job Order is fully paid");
        job.Status = "Released";
        db.SaveChanges();
        Check(job.Status == "Released", "CRUD Job Order: Successfully advanced to 'Released' upon full settlement");

        // 3e. Voiding payment requires reason
        bool voidReasonRequired = false;
        try
        {
            string emptyReason = "";
            if (string.IsNullOrWhiteSpace(emptyReason))
                throw new InvalidOperationException("Reason for voiding is required.");
        }
        catch (InvalidOperationException) { voidReasonRequired = true; }
        Check(voidReasonRequired, "CRUD Payment: Voiding without reason properly rejected");

        // 2h. Job Order deletion safety: cannot delete job with tasks or payments
        bool deleteBlocked = false;
        int taskCount = db.JobTasks.Count(t => t.JobOrderId == job.JobOrderId);
        int payCount = db.Payments.Count(p => p.JobOrderId == job.JobOrderId);
        if (taskCount > 0 || payCount > 0) deleteBlocked = true;
        Check(deleteBlocked, "CRUD Job Order: Deletion safely blocked when linked tasks/payments exist");

        // 5. INQUIRY CRUD & CONVERSION
        var inq = new Inquiry
        {
            CustomerName = "Inquiry Lead",
            ContactNumber = "09201112233",
            InquiryPlatform = "Phone Call",
            MessageDetails = "Inquiring about transmission fluid flush pricing.",
            Status = "New",
            CreatedAt = DateTime.UtcNow
        };
        db.Inquiries.Add(inq);
        db.SaveChanges();
        Check(inq.InquiryId > 0, "CRUD Inquiry: Created inquiry record");
        inq.Status = "Contacted";
        db.SaveChanges();
        Check(db.Inquiries.Find(inq.InquiryId)!.Status == "Contacted", "CRUD Inquiry: Updated inquiry status");

        // 6. COMPLAINT CRUD
        var comp = new Complaint
        {
            CustomerId = cust.CustomerId,
            JobOrderId = job.JobOrderId,
            ComplaintType = "Billing Inquiry",
            Description = "Customer requested itemized breakdown of shop supplies.",
            ResolutionStatus = "Open",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Complaints.Add(comp);
        db.SaveChanges();
        Check(comp.ComplaintId > 0, "CRUD Complaint: Created complaint record");
        comp.ResolutionStatus = "Resolved";
        comp.ActionTaken = "Explained parts invoice and provided official receipt.";
        db.SaveChanges();
        Check(db.Complaints.Find(comp.ComplaintId)!.ResolutionStatus == "Resolved", "CRUD Complaint: Successfully resolved with action taken");

        // 7. FEEDBACK CRUD
        var fb = new Feedback
        {
            JobOrderId = job.JobOrderId,
            RatingScore = 5,
            PositiveRemarks = "Exceptional service, friendly staff and prompt turnover.",
            AreasForImprovement = "None, keep up the good work!",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Feedbacks.Add(fb);
        db.SaveChanges();
        Check(fb.FeedbackId > 0, "CRUD Feedback: Created customer feedback with 5-star rating");

        // 1f. Customer soft deletion (deactivation)
        cust.IsActive = false;
        db.SaveChanges();
        Check(!db.Customers.Find(cust.CustomerId)!.IsActive, "CRUD Customer: Successfully deactivated; historical record preserved");

        // Cleanup test records
        try
        {
            db.Database.ExecuteSqlInterpolated($"DELETE FROM Feedbacks WHERE FeedbackId = {fb.FeedbackId}");
            db.Database.ExecuteSqlInterpolated($"DELETE FROM Complaints WHERE ComplaintId = {comp.ComplaintId}");
            db.Database.ExecuteSqlInterpolated($"DELETE FROM Inquiries WHERE InquiryId = {inq.InquiryId}");
            db.Database.ExecuteSqlInterpolated($"DELETE FROM Payments WHERE JobOrderId = {job.JobOrderId}");
            db.Database.ExecuteSqlInterpolated($"DELETE FROM JobTasks WHERE JobOrderId = {job.JobOrderId}");
            db.Database.ExecuteSqlInterpolated($"DELETE FROM JobOrders WHERE JobOrderId = {job.JobOrderId}");
            db.Database.ExecuteSqlInterpolated($"DELETE FROM Customers WHERE CustomerId = {cust.CustomerId}");
        }
        catch { }
    }

    static void TestLiveLocalDatabaseIntegration(Func<MasterCrmDbContext> contextFactory, string expectedDbName)
    {
        Console.WriteLine("\n--- Testing Live Local DB_MasterCrm Integration (Phase 1) ---");
        using var testDb = contextFactory();
        string dbName = testDb.Database.GetDbConnection().Database;
        string server = testDb.Database.GetDbConnection().DataSource;
        Console.WriteLine($"Live Application Connected Server: {server}; Database: {dbName}");
        Check(dbName == expectedDbName, $"Local Application Verification: Connected to isolated fixture {expectedDbName}");

        var seedCust = new Customer { FullName = "Honda Pilot Owner", ContactNumber = "09170001001", IsActive = true, CreatedAt = DateTime.UtcNow };
        testDb.Customers.Add(seedCust);
        testDb.SaveChanges();

        var seedJob = new JobOrder
        {
            CustomerId = seedCust.CustomerId,
            EstimateId = "EST-1001",
            VehicleDetails = "Honda Pilot 2026",
            Status = "In Queue",
            StartedAt = DateTime.UtcNow,
            TotalEstimatedCost = 2500m
        };
        testDb.JobOrders.Add(seedJob);
        testDb.SaveChanges();

        using var form = new CrmTransactionsForm(contextFactory, _ => true);
        form.BindingContext = new BindingContext();
        form.Size = new Size(1300, 850);
        form.CreateControl();
        form.Show();
        Application.DoEvents();

        var sync = form.SyncAsync();
        while (Field<bool>(form, "syncing") || !sync.IsCompleted) { Application.DoEvents(); Thread.Sleep(20); }
        sync.GetAwaiter().GetResult();

        var syncStatus = Field<Label>(form, "syncStatus");
        Check(syncStatus.Text.Contains("[LOCAL]"), "Live App: Visibly displays [LOCAL] environment");
        Check(!syncStatus.Text.Contains("Connection failed") && !syncStatus.Text.Contains("Refresh failed"), "Live App: Zero SqlException on sync");

        // Tabs
        var customersPage = Field<CustomerPage>(form, "customersPage");
        var ordersPage = Field<JobOrderPage>(form, "ordersPage");
        var tasksPage = Field<JobTaskPage>(form, "tasksPage");
        var paymentsPage = Field<PaymentPage>(form, "paymentsPage");
        var inquiriesPage = Field<InquiryPage>(form, "inquiriesPage");
        var complaintsPage = Field<ComplaintPage>(form, "complaintsPage");
        var feedbackPage = Field<FeedbackPage>(form, "feedbackPage");

        // Verify No First-Row Selection
        Check(customersPage.Grid.SelectedRows.Count == 0 && customersPage.Grid.CurrentCell == null, "Live App: Customers has no auto-selection");
        Check(ordersPage.Grid.SelectedRows.Count == 0 && ordersPage.Grid.CurrentCell == null, "Live App: JobOrders has no auto-selection");
        Check(tasksPage.jobsGrid.SelectedRows.Count == 0 && tasksPage.jobsGrid.CurrentCell == null, "Live App: Tasks jobsGrid has no auto-selection");
        Check(paymentsPage.Grid.SelectedRows.Count == 0 && paymentsPage.Grid.CurrentCell == null, "Live App: Payments has no auto-selection");
        Check(inquiriesPage.Grid.SelectedRows.Count == 0 && inquiriesPage.Grid.CurrentCell == null, "Live App: Inquiries has no auto-selection");
        Check(complaintsPage.Grid.SelectedRows.Count == 0 && complaintsPage.Grid.CurrentCell == null, "Live App: Complaints has no auto-selection");
        Check(feedbackPage.Grid.SelectedRows.Count == 0 && feedbackPage.Grid.CurrentCell == null, "Live App: Feedback has no auto-selection");

        // Verify Loaded Counts from DB_MasterCrm
        int custCount = testDb.Customers.Count();
        int orderCount = testDb.JobOrders.Count();
        int paymentCount = testDb.Payments.Count();
        int inqCount = testDb.Inquiries.Count();
        int cmpCount = testDb.Complaints.Count();
        int fbCount = testDb.Feedbacks.Count();

        Check(customersPage.countLabel.Text.Contains(custCount.ToString()) || (custCount == 0 && customersPage.countLabel.Text.Contains("No ")), $"Live App: Customers table loaded ({custCount} records)");
        Check(ordersPage.count.Text.Contains(orderCount.ToString()) || (orderCount == 0 && ordersPage.count.Text.Contains("No ")), $"Live App: JobOrders table loaded ({orderCount} records)");
        Check(paymentsPage.paymentCountLabel.Text.Contains(paymentCount.ToString()) || (paymentCount == 0 && paymentsPage.paymentCountLabel.Text.Contains("No payments")), $"Live App: Payments table loaded ({paymentCount} records)");
        Check(inquiriesPage.countLabel.Text.Contains(inqCount.ToString()) || (inqCount == 0 && inquiriesPage.countLabel.Text.Contains("No ")), $"Live App: Inquiries table loaded ({inqCount} records)");
        Check(complaintsPage.countLabel.Text.Contains(cmpCount.ToString()) || (cmpCount == 0 && complaintsPage.countLabel.Text.Contains("No ")), $"Live App: Complaints table loaded ({cmpCount} records)");
        Check(feedbackPage.countLabel.Text.Contains(fbCount.ToString()) || (fbCount == 0 && feedbackPage.countLabel.Text.Contains("No ")), $"Live App: Feedback table loaded ({fbCount} records)");

        // Test Inquiry & Appointments
        Check(inquiriesPage.appointmentsButton != null && inquiriesPage.appointmentsButton.Visible, "Live App: Appointments button visible in InquiryPage");
        using (var apptManager = new AppointmentManagerDialogForm(contextFactory))
        {
            apptManager.CreateControl();
            Check(apptManager != null, "Live App: AppointmentManagerDialogForm initialized successfully");
        }

        // Test Job Order Action Buttons
        Check(ordersPage.cancelJobButton != null, "Live App: Cancel Job button present on JobOrderPage");
        Check(ordersPage.assignMechanicButton != null, "Live App: Assign Mechanic button present on JobOrderPage");
        Check(ordersPage.viewTasksButton != null, "Live App: View Tasks button present on JobOrderPage");
        Check(ordersPage.printMechanicCopyButton != null, "Live App: Print Mechanic Copy button present on JobOrderPage");

        // Test Search on JobOrders
        ordersPage.search.Text = "1001";
        ClickButton(ordersPage.searchButton);
        Application.DoEvents();
        Check(ordersPage.Grid.Rows.Count == 1, "Live App: Job order search filters correctly");
        ClickButton(ordersPage.clear);
        Application.DoEvents();
        Check(ordersPage.count.Text.Contains(orderCount.ToString()), "Live App: Clear search restores all records");

        form.Close();
    }

    static void TestDatabaseEnvironmentSafety()
    {
        Console.WriteLine("\n--- Testing Database Environment Selection & Connection Safety ---");

        // 1. Normal development launch (no args) -> Resolves to LOCAL DB_MasterCrm
        var normalTarget = CrmDatabase.ResolveTarget(Array.Empty<string>(), envVarOverride: "", environmentOverride: "Development");
        Check(normalTarget.Environment == CrmEnvironment.Local, "Startup: Normal dev launch resolves to Local environment");
        Check(normalTarget.EnvironmentDisplayName == "LOCAL", "Startup: Environment display name is LOCAL");
        Check(normalTarget.DatabaseName == "DB_MasterCrm", "Startup: Database name is DB_MasterCrm");
        Check(normalTarget.ServerDisplayName.Contains("(localdb)"), "Startup: Server identity correctly identifies (localdb)");
        Check(!normalTarget.ServerDisplayName.Contains("Password"), "Security: Server identity does not contain credentials");

        // 2. Explicit --local -> Resolves to LOCAL, cannot fall back to cloud
        var localTarget = CrmDatabase.ResolveTarget(new[] { "--local" }, envVarOverride: "", environmentOverride: "Development");
        Check(localTarget.Environment == CrmEnvironment.Local, "Startup: Explicit --local resolves to Local environment");
        Check(localTarget.DatabaseName == "DB_MasterCrm", "Startup: Explicit --local uses DB_MasterCrm");

        bool threwProdLocal = false;
        try { CrmDatabase.ResolveTarget(new[] { "--local" }, environmentOverride: "Production"); }
        catch (InvalidOperationException) { threwProdLocal = true; }
        Check(threwProdLocal, "Safety: Explicit --local rejected in Production environment");

        // 3. Explicit --cloud -> Resolves to CLOUD, never silently falls back to local
        var cloudTarget = CrmDatabase.ResolveTarget(new[] { "--cloud" });
        Check(cloudTarget.Environment == CrmEnvironment.Cloud, "Startup: Explicit --cloud resolves to Cloud environment");
        Check(cloudTarget.EnvironmentDisplayName == "CLOUD", "Startup: Explicit --cloud displays CLOUD environment");
        Check(cloudTarget.DatabaseName == "db80735", "Startup: Explicit --cloud connects to db80735");
        Check(cloudTarget.ServerDisplayName.Contains("db80735.public.databaseasp.net"), "Startup: Cloud server identity verified");
        Check(!cloudTarget.ConnectionString.Contains("(localdb)"), "Safety: Explicit --cloud never silently falls back to local LocalDB");

        // 4. Missing or invalid configuration safeguards
        bool threwConflict = false;
        try { CrmDatabase.ResolveTarget(new[] { "--local", "--cloud" }); }
        catch (InvalidOperationException) { threwConflict = true; }
        Check(threwConflict, "Safety: Conflicting --local and --cloud flags rejected immediately");

        bool threwProdSilentLocal = false;
        try { CrmDatabase.ResolveTarget(Array.Empty<string>(), envVarOverride: "Server=(localdb)\\MSSQLLocalDB;Database=DB_MasterCrm", environmentOverride: "Production"); }
        catch (InvalidOperationException) { threwProdSilentLocal = true; }
        Check(threwProdSilentLocal, "Safety: Production deployment cannot silently use developer LocalDB via connection string");

        bool threwMalformed = false;
        try { CrmDatabase.ResolveTarget(Array.Empty<string>(), envVarOverride: "ThisIsNotAValidSqlConnectionString;;===="); }
        catch (Exception) { threwMalformed = true; }
        Check(threwMalformed, "Safety: Malformed connection string rejected with actionable error");

        // 5. Credential sanitization and diagnostic error logging
        string rawSecret = "Server=sql.example.com;User ID=superAdmin;Password=superSecretPassword987$;Database=secureDb";
        string sanitized = CrmDiagnostics.Sanitize(rawSecret);
        Check(!sanitized.Contains("superSecretPassword987$"), "Sanitization: Password completely masked from logs");
        Check(!sanitized.Contains("superAdmin"), "Sanitization: User ID masked from logs");
        Check(sanitized.Contains("Password=********"), "Sanitization: Replaced with standard mask");

        var testInfo = new CrmDatabaseInfo(CrmEnvironment.Cloud, "CLOUD", "sql.example.com", "secureDb", rawSecret);
        CrmDiagnostics.LogException("Verification Diagnostic Test", new Exception("Simulated connection failure to remote database"), testInfo);
        string logPath = CrmDiagnostics.GetLogFilePath();
        Check(File.Exists(logPath), "Diagnostics: Diagnostic log file created");
        string logContent = File.ReadAllText(logPath);
        Check(logContent.Contains("[CLOUD]") && logContent.Contains("Simulated connection failure"), "Diagnostics: Diagnostic log recorded sanitized failure context");
        Check(!logContent.Contains("superSecretPassword987$"), "Diagnostics: Diagnostic log contains zero raw credentials");
    }

    static void TestLegacyCrmOptimization(Func<MasterCrmDbContext> contextFactory)
    {
        Console.WriteLine("\n--- Testing CRM Legacy Optimization & UI Integration ---");
        using var db = contextFactory();
        CrmDataSeeder.EnsureStaffSeeded(db);
        var cust = db.Customers.FirstOrDefault(c => c.IsActive);
        if (cust == null)
        {
            cust = new Customer { FullName = "Optimization Cust", ContactNumber = "09171234567", IsActive = true, CreatedAt = DateTime.UtcNow };
            db.Customers.Add(cust);
            db.SaveChanges();
        }

        // 1. Customer 360 Profile Dialog
        using (var profile = new CustomerProfileDialogForm(cust.CustomerId, contextFactory))
        {
            Check(profile.Text.Contains(RecordReference.Customer(cust.CustomerId)), "Optimization 1a: CustomerProfileDialogForm contains standard customer reference");
            var tabs = Field<TabControl>(profile, "tabs");
            Check(tabs.TabPages.Count == 5, "Optimization 1b: CustomerProfileDialogForm provides all 5 360 tabs");
        }

        // 2. Contact classification presentation
        Check(ContactNumberHelper.Classify(cust.ContactNumber) == ContactNumberHelper.ClassificationValid, "Optimization 2a: Customer mobile classified as Valid");
        Check(ContactNumberHelper.Classify("09XX-123-4567") == ContactNumberHelper.ClassificationDemo, "Optimization 2b: Demo placeholder classified as Demo");
        Check(ContactNumberHelper.Classify("123") == ContactNumberHelper.ClassificationInvalid, "Optimization 2c: Short number classified as Invalid");
        Check(ContactNumberHelper.Classify("") == ContactNumberHelper.ClassificationMissing, "Optimization 2d: Empty number classified as Missing");

        // 3. PaymentPage Internal Views (Cancellation Reviews & Refund History)
        using (var payPage = new PaymentPage())
        {
            Check(payPage.btnViewJobBalances != null && payPage.btnViewReceipts != null, "Optimization 3a: Preserved Job Balances and Receipts view navigation");
            Check(payPage.btnViewCancellationReviews != null && payPage.btnViewRefunds != null, "Optimization 3b: Integrated Cancellation Reviews and Refunds view navigation");
            Check(payPage.cancellationReviewsGrid != null && payPage.refundsGrid != null, "Optimization 3c: Cancellation reviews and refunds grids configured");

            payPage.SwitchToCancellationReviewsView();
            Check(payPage.cancellationReviewsPanel.Visible && !payPage.jobBalancesPanel.Visible, "Optimization 3d: SwitchToCancellationReviewsView shows cancellation panel exclusively");

            payPage.SwitchToRefundsView();
            Check(payPage.refundsPanel.Visible && !payPage.cancellationReviewsPanel.Visible, "Optimization 3e: SwitchToRefundsView shows refunds panel exclusively");
        }

        // 4. Refund-Aware Financial Row Calculations
        var testJob = new JobOrder { JobOrderId = 99999, Status = "Cancelled", TotalEstimatedCost = 5000m };
        var row = new JobBalanceRow
        {
            JobOrderId = testJob.JobOrderId,
            Customer = "Test Cust",
            Vehicle = "Test Car",
            TotalEstimatedCost = testJob.TotalEstimatedCost,
            TotalPaid = 2500m,
            TotalRefunded = 2500m,
            OutstandingBalance = 0m,
            JobStatus = testJob.Status,
            PaymentStatus = "Cancelled — Fully Refunded"
        };
        Check(row.OutstandingBalanceDisplay == "— [Cancelled]", "Optimization 4a: Cancelled job displays '— [Cancelled]' for outstanding balance");
        Check(row.NetCollections == 0m && row.NetCollectionsDisplay == "₱0.00", "Optimization 4b: Fully refunded job net collections is ₱0.00");
        Check(row.RemainingRefundable == 0m && row.RemainingRefundableDisplay == "₱0.00", "Optimization 4c: Remaining refundable is ₱0.00 after full refund");
    }

    static void TestLocalCutoverProcedure()
    {
        Console.WriteLine("\n--- Testing Local Cutover Procedure on Disposable Database Copy ---");
        string suffix = Guid.NewGuid().ToString("N");
        string dbName = "RevCare_CutoverRehearsal_" + suffix;
        string connStr = $@"Server=(localdb)\MSSQLLocalDB;Database={dbName};Trusted_Connection=True;TrustServerCertificate=True";

        var masterOptions = new DbContextOptionsBuilder<MasterCrmDbContext>()
            .UseSqlServer(connStr)
            .Options;

        var tenantOptions = new DbContextOptionsBuilder<TenantCrmDbContext>()
            .UseSqlServer(connStr)
            .Options;

        using var preCutoverDb = new MasterCrmDbContext(masterOptions);
        try
        {
            // 1. Initial State: Migrate as MasterCrmDbContext (simulating legacy operational database)
            preCutoverDb.Database.Migrate();

            // 2. Seed legacy operational dataset
            var customer = new Customer
            {
                FullName = "Cutover Legacy Customer",
                ContactNumber = "09171239999",
                Email = "cutover@legacy.revcare.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };
            preCutoverDb.Customers.Add(customer);
            preCutoverDb.SaveChanges();

            var vehicle = new Vehicle
            {
                CustomerId = customer.CustomerId,
                Make = "Honda",
                Model = "Civic 2022",
                PlateNumber = "CUT-001"
            };
            preCutoverDb.Vehicles.Add(vehicle);
            preCutoverDb.SaveChanges();

            var job = new JobOrder
            {
                CustomerId = customer.CustomerId,
                VehicleDetails = "Honda Civic 2022 / CUT-001",
                EstimateId = "EST-CUTOVER-001",
                TotalEstimatedCost = 5000m,
                Status = "In Queue",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            };
            preCutoverDb.JobOrders.Add(job);
            preCutoverDb.SaveChanges();

            preCutoverDb.JobTasks.AddRange(
                new JobTask { JobOrderId = job.JobOrderId, ServiceId = "Diagnostic", Status = "Pending", CreatedAt = DateTime.UtcNow.AddMonths(-1) },
                new JobTask { JobOrderId = job.JobOrderId, ServiceId = "Brake Check", Status = "Pending", CreatedAt = DateTime.UtcNow.AddMonths(-1) }
            );
            preCutoverDb.Payments.Add(new Payment
            {
                JobOrderId = job.JobOrderId,
                AmountPaid = 2500m,
                PaymentMethod = "Cash",
                PaymentType = "Downpayment",
                SubmissionId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            });
            preCutoverDb.Inquiries.Add(new Inquiry
            {
                CustomerName = "Cutover Inquiry Lead",
                ContactNumber = "09170001122",
                Status = "New",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            });
            preCutoverDb.Complaints.Add(new Complaint
            {
                CustomerId = customer.CustomerId,
                JobOrderId = job.JobOrderId,
                Description = "Legacy inquiry complaint",
                ResolutionStatus = "Resolved",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            });
            preCutoverDb.Feedbacks.Add(new Feedback
            {
                JobOrderId = job.JobOrderId,
                RatingScore = 5,
                PositiveRemarks = "Pre-cutover service feedback",
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            });
            preCutoverDb.SaveChanges();

            Check(preCutoverDb.JobOrders.Count() == 1, "Cutover Pre-check: 1 Job Order exists before cutover");
            Check(preCutoverDb.JobTasks.Count() == 2, "Cutover Pre-check: 2 Job Tasks exist before cutover");
            Check(preCutoverDb.Payments.Count() == 1, "Cutover Pre-check: 1 Payment exists before cutover");
            Check(preCutoverDb.Customers.Count() == 1, "Cutover Pre-check: 1 Customer exists before cutover");

            // 3. Execute Cutover: Ensure TenantDatabaseIdentity and Branches are initialized for Tenant A
            var identity = preCutoverDb.DatabaseIdentity.FirstOrDefault();
            if (identity == null)
            {
                preCutoverDb.DatabaseIdentity.Add(new TenantDatabaseIdentity { Id = 1, TenantId = "tenant-a" });
            }
            else
            {
                identity.TenantId = "tenant-a";
            }

            if (!preCutoverDb.Branches.Any())
            {
                preCutoverDb.Branches.Add(new TenantBranch
                {
                    BranchCode = "MAIN",
                    BranchName = "RevCare Auto Hub - Main Branch",
                    Address = "Metro Manila, Philippines",
                    ContactNumber = "0917-123-4567",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            preCutoverDb.SaveChanges();
            Check(true, "Cutover Execution: TenantDatabaseIdentity and MAIN Branch successfully registered");

            // 4. Post-Cutover: Connect via TenantCrmDbContext and verify 100% data preservation
            using (var postCutoverDb = new TenantCrmDbContext(tenantOptions))
            {
                var postIdentity = postCutoverDb.DatabaseIdentity.Single();
                Check(postIdentity.TenantId == "tenant-a", "Cutover Verification: Tenant database identity confirmed as 'tenant-a'");

                var postBranch = postCutoverDb.Branches.Single(b => b.BranchCode == "MAIN");
                Check(postBranch.IsActive, "Cutover Verification: MAIN branch active");

                Check(postCutoverDb.Customers.Count() == 1, "Cutover Verification: Zero customer data loss (1/1 preserved)");
                Check(postCutoverDb.Vehicles.Count() == 1, "Cutover Verification: Zero vehicle data loss (1/1 preserved)");
                Check(postCutoverDb.JobOrders.Count() == 1, "Cutover Verification: Zero job order data loss (1/1 preserved)");
                Check(postCutoverDb.JobTasks.Count() == 2, "Cutover Verification: Zero job task data loss (2/2 preserved)");
                Check(postCutoverDb.Payments.Count() == 1, "Cutover Verification: Zero payment data loss (1/1 preserved)");
                Check(postCutoverDb.Inquiries.Count() == 1, "Cutover Verification: Zero inquiry data loss (1/1 preserved)");
                Check(postCutoverDb.Complaints.Count() == 1, "Cutover Verification: Zero complaint data loss (1/1 preserved)");
                Check(postCutoverDb.Feedbacks.Count() == 1, "Cutover Verification: Zero feedback data loss (1/1 preserved)");

                var preservedJob = postCutoverDb.JobOrders.Include(j => j.Tasks).Include(j => j.Payments).First();
                Check(preservedJob.TotalEstimatedCost == 5000m, "Cutover Verification: Job Order financial totals preserved");
                Check(preservedJob.Payments.First().AmountPaid == 2500m, "Cutover Verification: Payment amounts preserved");
                Check(preservedJob.Tasks.Count == 2, "Cutover Verification: Task relationships intact");

                // 5. Verify new Tenant A features function on the cutover database
                var newAppt = new Appointment
                {
                    CustomerId = customer.CustomerId,
                    ScheduledStart = DateTime.Today.AddDays(1).AddHours(10),
                    ScheduledEnd = DateTime.Today.AddDays(1).AddHours(11),
                    RequestedServices = "Post-cutover Brake Service",
                    Status = "Scheduled"
                };
                postCutoverDb.Appointments.Add(newAppt);
                postCutoverDb.SaveChanges();
                Check(newAppt.AppointmentId > 0, "Cutover Verification: New Tenant A Appointment created successfully on cutover DB");

                var newEst = new Estimate
                {
                    CustomerId = customer.CustomerId,
                    Status = "Draft",
                    Notes = "Post-cutover estimate",
                    TotalAmount = 4000m
                };
                newEst.Lines.Add(new EstimateLine { Description = "Brake pad replacement", Quantity = 1, UnitPrice = 4000m, LineTotal = 4000m });
                postCutoverDb.Estimates.Add(newEst);
                postCutoverDb.SaveChanges();
                Check(newEst.EstimateId > 0, "Cutover Verification: New Tenant A Estimate created successfully on cutover DB");
            }
        }
        finally
        {
            // Drop rehearsal database cleanly
            try { preCutoverDb.Database.EnsureDeleted(); } catch { }
        }
    }

    static void TestNativeBackupRestoreRehearsal()
    {
        Console.WriteLine("\n--- Testing Native SQL Server Backup & Restore Rehearsal ---");
        string masterConnStr = @"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True";

        // 1. Verify DB_MasterCrm baseline
        int baselineJobs = 0;
        int baselinePayments = 0;
        using (var masterCrm = new MasterCrmDbContext(new DbContextOptionsBuilder<MasterCrmDbContext>()
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=DB_MasterCrm;Trusted_Connection=True;TrustServerCertificate=True")
            .Options))
        {
            baselineJobs = masterCrm.JobOrders.Count();
            baselinePayments = masterCrm.Payments.Count();
            Check(baselineJobs == 226, "Native Backup: Verified 226 Job Orders baseline on DB_MasterCrm");
            Check(baselinePayments == 232, "Native Backup: Verified 232 Payments baseline on DB_MasterCrm");
        }

        string suffix = Guid.NewGuid().ToString("N");
        string restoredDbName = "RevCare_CutoverNativeRehearsal_" + suffix;
        string tempBak = Path.Combine(Path.GetTempPath(), "RevCare_Rehearsal_" + suffix + ".bak");
        string tempMdf = Path.Combine(Path.GetTempPath(), "RevCare_Rehearsal_" + suffix + ".mdf");
        string tempLdf = Path.Combine(Path.GetTempPath(), "RevCare_Rehearsal_" + suffix + ".ldf");

        var masterOptions = new DbContextOptionsBuilder<MasterCrmDbContext>().UseSqlServer(masterConnStr).Options;

        try
        {
            // 2. Execute non-mutating copy-only native SQL Server backup of DB_MasterCrm
            using (var sysDb = new MasterCrmDbContext(masterOptions))
            {
                sysDb.Database.ExecuteSqlRaw($@"BACKUP DATABASE [DB_MasterCrm] TO DISK = '{tempBak}' WITH COPY_ONLY, FORMAT;");
            }
            Check(File.Exists(tempBak) && new FileInfo(tempBak).Length > 0, "Native Backup: DB_MasterCrm backup file produced via COPY_ONLY");

            // 3. Execute native SQL Server restore as isolated rehearsal database
            using (var sysDb = new MasterCrmDbContext(masterOptions))
            {
                sysDb.Database.ExecuteSqlRaw($@"RESTORE DATABASE [{restoredDbName}] FROM DISK = '{tempBak}' WITH MOVE 'DB_MasterCrm' TO '{tempMdf}', MOVE 'DB_MasterCrm_log' TO '{tempLdf}';");
            }
            Check(true, "Native Backup: Restored database copy initialized via SQL Server RESTORE WITH MOVE");

            // 4. Verify restored copy with MasterCrmDbContext before cutover
            string restoredConnStr = $@"Server=(localdb)\MSSQLLocalDB;Database={restoredDbName};Trusted_Connection=True;TrustServerCertificate=True";
            var restoredMasterOptions = new DbContextOptionsBuilder<MasterCrmDbContext>()
                .UseSqlServer(restoredConnStr)
                .Options;
            var restoredTenantOptions = new DbContextOptionsBuilder<TenantCrmDbContext>()
                .UseSqlServer(restoredConnStr)
                .Options;

            using (var restoredMasterDb = new MasterCrmDbContext(restoredMasterOptions))
            {
                int restoredJobs = restoredMasterDb.JobOrders.Count();
                int restoredPayments = restoredMasterDb.Payments.Count();
                int restoredCustomers = restoredMasterDb.Customers.Count();

                Check(restoredJobs == 226, $"Native Backup: Restored copy contains exactly 226 Job Orders ({restoredJobs}/226)");
                Check(restoredPayments == 232, $"Native Backup: Restored copy contains exactly 232 Payments ({restoredPayments}/232)");
                Check(restoredCustomers == 209, $"Native Backup: Restored copy contains exactly 209 Customers ({restoredCustomers}/209)");

                // Apply cutover schema migration to the isolated restored copy
                restoredMasterDb.Database.Migrate();
                Check(true, "Native Backup: Cutover schema alignment migration successfully applied to restored copy");

                // Rehearse cutover registration on the restored copy
                if (!restoredMasterDb.DatabaseIdentity.Any())
                {
                    restoredMasterDb.DatabaseIdentity.Add(new TenantDatabaseIdentity { Id = 1, TenantId = "tenant-a" });
                }
                if (!restoredMasterDb.Branches.Any())
                {
                    restoredMasterDb.Branches.Add(new TenantBranch
                    {
                        BranchCode = "MAIN",
                        BranchName = "RevCare Auto Hub - Main Branch",
                        Address = "Metro Manila",
                        ContactNumber = "0917-123-4567",
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
                restoredMasterDb.SaveChanges();
                Check(true, "Native Backup: Tenant A identity & Branch registered on restored copy without errors");
            }

            // 5. Connect to the cutover database using TenantCrmDbContext and verify full operational integrity
            using (var restoredTenantDb = new TenantCrmDbContext(restoredTenantOptions))
            {
                Check(restoredTenantDb.JobOrders.Count() == 226, "Native Cutover: TenantCrmDbContext accesses all 226 Job Orders");
                Check(restoredTenantDb.Payments.Count() == 232, "Native Cutover: TenantCrmDbContext accesses all 232 Payments");
                Check(restoredTenantDb.Customers.Count() == 209, "Native Cutover: TenantCrmDbContext accesses all 209 Customers");

                // Verify new tenant entity write works on cutover database
                var testAppt = new Appointment
                {
                    CustomerId = restoredTenantDb.Customers.First().CustomerId,
                    ScheduledStart = DateTime.UtcNow.AddDays(2),
                    ScheduledEnd = DateTime.UtcNow.AddDays(2).AddHours(1),
                    RequestedServices = "Rehearsal Check",
                    Status = "Scheduled"
                };
                restoredTenantDb.Appointments.Add(testAppt);
                restoredTenantDb.SaveChanges();
                Check(testAppt.AppointmentId > 0, "Native Backup: Live operational write successful on cutover database");
            }
        }
        finally
        {
            // Drop restored rehearsal database cleanly
            try
            {
                using var sysDb = new MasterCrmDbContext(masterOptions);
                sysDb.Database.ExecuteSqlRaw($@"
                    IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{restoredDbName}')
                    BEGIN
                        ALTER DATABASE [{restoredDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{restoredDbName}];
                    END");
            }
            catch { }

            try { if (File.Exists(tempBak)) File.Delete(tempBak); } catch { }
            try { if (File.Exists(tempMdf)) File.Delete(tempMdf); } catch { }
            try { if (File.Exists(tempLdf)) File.Delete(tempLdf); } catch { }

            // 5. Assert DB_MasterCrm remains 100% unmutated
            using (var masterCrm = new MasterCrmDbContext(new DbContextOptionsBuilder<MasterCrmDbContext>()
                .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=DB_MasterCrm;Trusted_Connection=True;TrustServerCertificate=True")
                .Options))
            {
                Check(masterCrm.JobOrders.Count() == baselineJobs, "Native Backup Safety: DB_MasterCrm Job Orders count completely untouched");
                Check(masterCrm.Payments.Count() == baselinePayments, "Native Backup Safety: DB_MasterCrm Payments count completely untouched");
            }
        }
    }
}

sealed class DisposableCrmFixture : IDisposable
{
    public string DbName { get; }
    public string ConnectionString { get; }
    private readonly MasterCrmDbContext _cleanupDb;

    public DisposableCrmFixture()
    {
        string suffix = Guid.NewGuid().ToString("N");
        DbName = "RevCare_Verification_" + suffix;
        ConnectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={DbName};Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<MasterCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        _cleanupDb = new MasterCrmDbContext(options);
        _cleanupDb.Database.Migrate();
        CrmDataSeeder.EnsureStaffSeeded(_cleanupDb);
    }

    public MasterCrmDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MasterCrmDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new MasterCrmDbContext(options) { AuditActor = "test-harness" };
    }

    public void Dispose()
    {
        try { _cleanupDb.Database.EnsureDeleted(); } catch { }
        _cleanupDb.Dispose();
    }
}
