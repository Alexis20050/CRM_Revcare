using CRM_Revcare_;
using CRM.infrastructure.data;
using crm.domain.entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

class Program
{
    [STAThread]
    static void Main()
    {
        var options = new DbContextOptionsBuilder<MasterCrmDbContext>().UseSqlServer(
            @"Server=(localdb)\MSSQLLocalDB;Database=RevCare_PaymentUi_" + Guid.NewGuid().ToString("N") + ";Trusted_Connection=True;TrustServerCertificate=True").Options;
        MasterCrmDbContext Create() => new(options);
        using var db = Create();
        try
        {
            db.Database.Migrate();
            var authentication = new AdminAuthentication(Create);
            authentication.EnsureAdminAccount();
            authentication.EnsureAdminAccount();
            Check(authentication.Validate("admin", "admin123") && !authentication.Validate("admin", "wrong-password"),
                "Account provisioning is repeatable and login still validates the password");
            Check(db.Users.Count() == 1 && db.UserRoles.Count() == 1, "Repeated login setup does not duplicate accounts or memberships");
            var customer = new Customer { FullName = "Payment test", ContactNumber = "09171234567" };
            db.Add(customer); db.SaveChanges();
            var job = new JobOrder { CustomerId = customer.CustomerId, EstimateId = "Test vehicle", TotalEstimatedCost = 1000, StartedAt = DateTime.Now };
            db.Add(job); db.SaveChanges();
            CRM.infrastructure.services.JobServices.Assign(db, job, CRM.infrastructure.services.JobServices.Offered.Take(2));
            db.Update(job); db.SaveChanges();
            db.Add(new Payment { JobOrderId = job.JobOrderId, AmountPaid = 200 }); db.SaveChanges();
            using var form = new CrmTransactionsForm(Create, _ => true);
            form.BindingContext = new BindingContext(); form.CreateControl();
            var sync = form.SyncAsync();
            while (!sync.IsCompleted) { Application.DoEvents(); Thread.Sleep(10); }
            sync.GetAwaiter().GetResult();
            var orders = All(form).OfType<JobOrderPage>().Single();
            var tasks = All(form).OfType<JobTaskPage>().Single();
            orders.RefreshRecords(); orders.Grid.Rows[0].Selected = true;
            Check(Find<CheckedListBox>(orders, "RequestedServices").CheckedItems.Count == 2, "Order editor restores its assigned services");
            var jobStatus = Find<ComboBox>(orders, "Status");
            jobStatus.SelectedItem = "In Progress";
            Check(!Find<Button>(orders, "SaveRecord").Enabled && Find<Label>(orders, "PaymentRequirement").Text.Contains("300.00"),
                "Insufficient deposit disables starting work and shows the shortfall");
            jobStatus.SelectedItem = "In Queue";
            Check(Find<Button>(orders, "SaveRecord").Enabled, "Queued job details can still be saved before payment");
            Click(orders, "ViewJobTasks");
            Check(tasks.Grid.Rows.Count == 2 && Find<ComboBox>(tasks, "TaskJobFilter").SelectedIndex > 0, "View Job Tasks filters the selected order automatically");
            tasks.Grid.ClearSelection();
            tasks.Grid.Rows[0].Selected = true;
            Check(!Find<ComboBox>(tasks, "JobOrderId").Enabled && !Find<ComboBox>(tasks, "ServiceId").Enabled, "Task editing locks the order and service identity");
            Check(!Find<ComboBox>(tasks, "Status").Enabled && Find<ComboBox>(tasks, "Status").Items.Count == 1,
                "Queued job offers no premature task status transitions");
            Check(Find<Label>(tasks, "TaskWorkflowHint").Text.Contains("50%") && !Find<TextBox>(tasks, "Remarks").ReadOnly,
                "Queued task explains the next step and permits notes");
            Check(Find<TextBox>(tasks, "AssignedJobDetails").Text.Contains("Payment test")
                && Find<TextBox>(tasks, "AssignedServiceDetails").ReadOnly, "Assigned details stay readable and read-only");
            var taskSearch = Find<TextBox>(tasks, "TaskSearch");
            taskSearch.Text = "Brake";
            Check(tasks.Grid.Rows.Count == 1, "Task search filters services within the selected job");
            taskSearch.Text = "no matching task";
            Check(tasks.Grid.Rows.Count == 0, "Unmatched task search displays no rows");
            taskSearch.Clear();
            Check(tasks.Grid.Rows.Count == 2, "Clearing search restores assigned tasks");
            var secondJob = new JobOrder { CustomerId = customer.CustomerId, VehicleDetails = "Second vehicle", TotalEstimatedCost = 500, StartedAt = DateTime.Now };
            CRM.infrastructure.services.JobServices.Assign(db, secondJob, CRM.infrastructure.services.JobServices.Offered.Take(1));
            db.Add(secondJob); db.SaveChanges();
            Click(tasks, "RefreshRecords");
            var overview = Find<DataGridView>(tasks, "JobsOverview");
            Check(overview.Rows.Cast<DataGridViewRow>().Any(r => (int)r.Cells["JobOrderId"].Value == secondJob.JobOrderId),
                "Task Refresh discovers newly saved jobs without waiting for cloud refresh");
            overview.ClearSelection();
            var originalJobSource = overview.DataSource;
            int originalScroll = overview.FirstDisplayedScrollingRowIndex;
            foreach (DataGridViewRow r in overview.Rows)
                if ((int)r.Cells["JobOrderId"].Value == secondJob.JobOrderId) r.Selected = true;
            Check(ReferenceEquals(originalJobSource, overview.DataSource) && originalScroll == overview.FirstDisplayedScrollingRowIndex,
                "Selecting a job preserves the overview data source and scroll position");
            Check(tasks.Grid.Rows.Count == 1 && ((JobTask)tasks.Grid.Rows[0].DataBoundItem).JobOrderId == secondJob.JobOrderId, "Same customer's separate jobs keep their tasks separate");
            Check(!tasks.Grid.Columns["JobOrderId"].Visible, "Selected-job task list omits repeated customer details");
            var page = All(form).OfType<PaymentPage>().Single();
            var jobBox = Find<Control>(page, "JobOrderId");
            SelectJob(form, jobBox, job.JobOrderId);
            var amount = Find<TextBox>(page, "AmountPaid");
            Check(amount.Text == "", "Selecting a job never assumes how much was received");
            Click(page, "UseDeposit");
            Check(decimal.Parse(amount.Text) == 300, "Deposit shortcut subtracts existing payments");
            Click(page, "FillBalance");
            Check(decimal.Parse(amount.Text) == 800, "Balance shortcut uses the remaining bill");
            amount.Text = "900";
            Check(!Find<Button>(page, "SaveRecord").Enabled, "Overpayment is blocked before saving");
            Find<CheckBox>(page, "AllowCustomerCredit").Checked = true;
            Check(Find<Button>(page, "SaveRecord").Enabled && Find<Label>(page, "PaymentPreview").Text.Contains("100.00"), "Explicit credit shows the excess");
            page.RefreshRecords(); page.Grid.Rows[0].Selected = true;
            Check(Find<Label>(page, "BillingSummary").Text.Contains("1,000.00"), "Correction excludes its existing receipt from other payments");
            Check(!Find<Button>(page, "UseDeposit").Enabled && !Find<Button>(page, "FillBalance").Enabled,
                "Correction cannot use new-money shortcuts");
            Check(!Find<Button>(page, "clearJobButton").Enabled, "Correction keeps its job link locked");
            Check(amount.ReadOnly && !Find<Button>(page, "SaveRecord").Enabled, "Saved receipt amount is read-only and cannot be saved over");
            Check(Find<Label>(page, "PaymentPreview").Text.Contains("read only"), "Saved receipt explains read-only mode");
            Check(page.Grid.Columns.Contains("IsVoided") && page.Grid.Columns.Contains("VoidReason"), "History exposes void status and reason");
            Check(Find<Label>(page, "BillingStatusLabel").Text.Contains("Partially Paid"), "Saved billing status includes the receipt being corrected");
            var finalPayment = new Payment { JobOrderId = job.JobOrderId, AmountPaid = 800 };
            db.Add(finalPayment); db.SaveChanges();
            void ReloadBilling()
            {
                var refresh = form.SyncAsync();
                while (!refresh.IsCompleted) { Application.DoEvents(); Thread.Sleep(10); }
                refresh.GetAwaiter().GetResult();
                SelectJob(form, jobBox, null); SelectJob(form, jobBox, job.JobOrderId);
            }
            ReloadBilling();
            Check(Find<Label>(page, "BillingStatusLabel").Text.Contains("Fully Paid"), "Full settlement displays Fully Paid");
            Check(page.Grid.Rows[0].Cells["BillingStatus"].FormattedValue.ToString() == "Fully Paid", "Receipt list displays current job billing status");
            finalPayment.IsVoided = true; finalPayment.VoidReason = "Test reversal"; db.SaveChanges();
            ReloadBilling();
            Check(Find<Label>(page, "BillingStatusLabel").Text.Contains("Partially Paid"), "Voiding a payment removes Fully Paid status");
            taskSearch.Text = "no matching job";
            Click(tasks, "ShowNewestJob");
            Check(taskSearch.Text == "" && overview.SelectedRows.Count == 1
                && (int)overview.SelectedRows[0].Cells["JobOrderId"].Value == secondJob.JobOrderId
                && overview.FirstDisplayedScrollingRowIndex == overview.SelectedRows[0].Index,
                "Newest job clears a hiding search and reveals the selected row");
            Click(tasks, "AllTasks");
            Check(overview.FirstDisplayedScrollingRowIndex == 0 && tasks.Grid.Rows.Count == 3,
                "All tasks returns the overview to the top and displays every task");
            foreach (string name in new[] { "Nin Edano", "Katrina Pineda" })
            {
                var searchCustomer = new Customer { FullName = name, ContactNumber = "09171112222" };
                db.Add(searchCustomer); db.SaveChanges();
                var searchJob = new JobOrder { CustomerId = searchCustomer.CustomerId, StartedAt = DateTime.Now, TotalEstimatedCost = 100 };
                CRM.infrastructure.services.JobServices.Assign(db, searchJob, CRM.infrastructure.services.JobServices.Offered.Take(1));
                db.Add(searchJob); db.SaveChanges();
            }
            var refreshSearch = form.SyncAsync();
            while (!refreshSearch.IsCompleted) { Application.DoEvents(); Thread.Sleep(10); }
            refreshSearch.GetAwaiter().GetResult();
            Find<ComboBox>(tasks, "TaskJobFilter").SelectedIndex = 3;
            taskSearch.Text = "eda";
            Check(overview.Rows.Count == 2 && tasks.Grid.Rows.Count == 2, "Customer search finds Edano and Pineda across previously selected jobs");
            using var picker = new JobOrderPickerDialogForm(db.JobOrders.AsNoTracking().ToList(), db.Customers.AsNoTracking().ToList());
            picker.BindingContext = new BindingContext(); picker.CreateControl();
            All(picker).OfType<TextBox>().First().Text = "eda";
            Check(All(picker).OfType<DataGridView>().Single().Rows.Count == overview.Rows.Count,
                "Main job overview and picker agree for the same customer search");
            Console.WriteLine("All payment UI checks passed.");
        }
        finally { db.Database.EnsureDeleted(); }
        Environment.Exit(0);
    }
    static IEnumerable<Control> All(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(All(c)));
    static T Find<T>(Control root, string name) where T : Control => (T)root.Controls.Find(name, true).Single();
    static void SelectJob(Form form, Control input, int? id)
    {
        var lookups = (System.Collections.IEnumerable)form.GetType().GetField("jobLookups", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(form)!;
        var lookup = lookups.Cast<object>().Single(candidate => candidate.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(field => ReferenceEquals(field.GetValue(candidate), input)));
        lookup.GetType().GetMethod("Select")!.Invoke(lookup, new object[] { id });
    }
    static void Click(Control root, string name) => typeof(Button).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Find<Button>(root, name), new object[] { EventArgs.Empty });
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS: " + name); }
}
