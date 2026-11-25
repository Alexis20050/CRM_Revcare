using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_Revcare_;

internal partial class TenantWorkspaceForm : Form
{
    private readonly TenantApiClient api;
    private readonly List<Module> modules = new();
    private List<JsonObject> rows = new();
    private List<JsonObject> visible = new();
    private bool busy;
    internal bool LogoutRequested { get; private set; }
    private Module selectedModule;
    private Module Current => selectedModule;
    private string selectedRecordId;
    private bool bindingRecords;
    private JsonObject Selected => (records.SelectedRows.Count > 0 || records.CurrentCell != null) && records.CurrentRow?.DataBoundItem is DataRowView view && Current != null
        ? visible.FirstOrDefault(r => r[Current.Key]?.ToString() == view["__key"]?.ToString()) : null;

    private readonly Button retryProfileSync = new()
    {
        Text = "Retry Profile Sync",
        AutoSize = true,
        MinimumSize = new Size(130, 34),
        UseVisualStyleBackColor = true,
        Visible = false
    };

    private readonly Button promoteOwner = new()
    {
        Text = "👑 Promote to Owner...",
        AutoSize = true,
        MinimumSize = new Size(150, 34),
        UseVisualStyleBackColor = true,
        Visible = false
    };

    private readonly System.Windows.Forms.Timer searchDebounceTimer = new() { Interval = 250 };
    private Panel welcomePanel;

    private sealed record Module(string Name, string Path, string Key, string Permission) { public override string ToString() => Name; }

    public TenantWorkspaceForm() { InitializeComponent(); }

    internal TenantWorkspaceForm(TenantApiClient api)
    {
        InitializeComponent();
        this.api = api;
        void DisplayHistoricalStage(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex >= 0 && e.Value is string value && value == "QA Checking")
            {
                e.Value = "In Progress";
                e.FormattingApplied = true;
            }
        }
        records.CellFormatting += DisplayHistoricalStage;
        jobsOverview.CellFormatting += DisplayHistoricalStage;

        string companyDisplay = !string.IsNullOrWhiteSpace(api.CompanyName) ? api.CompanyName : (!string.IsNullOrWhiteSpace(api.TenantId) ? api.TenantId : "RevCare Auto Hub");
        string branchDisplay = !string.IsNullOrWhiteSpace(api.BranchCode) ? api.BranchCode : "All Branches (Central)";
        sessionLabel.Text = $"Company: {companyDisplay} | User: {api.Username} | Role: {api.Role} | Branch: {branchDisplay} | Env: {api.EnvironmentDisplayName}";

        bool ModuleIsEnabled(string path) => path switch
        {
            "customers" => api.HasModule("Customers"),
            "job-orders" => api.HasModule("JobOrders"),
            "workshop" => api.HasModule("JobTasks"),
            "payments" => api.HasModule("Payments"),
            "inquiries" => api.HasModule("Inquiries"),
            "appointments" => api.HasModule("Inquiries") || api.HasModule("Customers"),
            "estimates" => api.HasModule("JobOrders"),
            "reports/operational-summary" => api.Has("Reports.Read") && api.HasModule("Payments"),
            "complaints" => api.HasModule("Complaints"),
            "feedbacks" => api.HasModule("Feedback"),
            "services" => api.HasModule("Services"),
            "workshop/assignment-history" => api.HasModule("JobTasks"),
            "staff" or "staff/access-history" => true,
            "audit" => true,
            _ => false
        };

        var all = new[] {
            new Module("Customers & Vehicles", "customers", "customerId", "Customers.Manage"),
            new Module("Job Orders", "job-orders", "jobOrderId", "Jobs.Manage"),
            new Module(api.Has("Tasks.UpdateAssigned") ? "My Assigned Tasks" : "Job Tasks & QA", "workshop", "jobTaskId", "Workshop.Read"),
            new Module("Billing & Payments", "payments", "paymentId", "Payments.Read"),
            new Module("Leads & Inquiries", "inquiries", "inquiryId", "Collection.Manage"),
            new Module("Appointments", "appointments", "appointmentId", "Collection.Manage"),
            new Module("Estimates & Approvals", "estimates", "estimateId", "Jobs.Manage"),
            new Module("Complaints & Warranty", "complaints", "complaintId", "Collection.Manage"),
            new Module("Feedback & CSAT", "feedbacks", "feedbackId", "Collection.Manage"),
            new Module("Operational Reports", "reports/operational-summary", "metric", "Reports.Read"),
            new Module("Service Catalog", "services", "id", "Services.Read"),
            new Module("Staff Accounts", "staff", "userId", "Staff.Manage"),
            new Module("Staff Access History", "staff/access-history", "id", "Staff.Manage"),
            new Module("Assignment History", "workshop/assignment-history", "id", "Tasks.Assign"),
            new Module("Audit History", "audit", "auditEntryId", "Audit.Read")
        };

        modules.AddRange(all.Where(m => api.Has(m.Permission) && ModuleIsEnabled(m.Path)));
        ConfigureNavigation();

        // Add retry profile sync and promote owner buttons to actions panel
        actions.Controls.Add(retryProfileSync);
        retryProfileSync.Click += async (_, _) => await Run(RetryProfileSyncAsync);
        actions.Controls.Add(promoteOwner);
        promoteOwner.Click += async (_, _) => await Run(PromoteOwnerAsync);

        paymentStatusFilter.Items.AddRange(new object[] { "All Payment Statuses", "Valid Payments", "Voided Payments" });
        paymentStatusFilter.SelectedIndex = 0;
        paymentStatusFilter.SelectedIndexChanged += (_, _) => { selectedRecordId = null; Filter(); };

        billingStatusFilter.Items.AddRange(new object[] { "All Billing Statuses", "Unpaid", "Partially Paid", "Fully Paid" });
        billingStatusFilter.SelectedIndex = 0;
        billingStatusFilter.SelectedIndexChanged += (_, _) => { selectedRecordId = null; Filter(); };

        searchDebounceTimer.Tick += (_, _) =>
        {
            searchDebounceTimer.Stop();
            selectedJobId = null;
            selectedRecordId = null;
            Filter();
        };

        search.TextChanged += (_, _) =>
        {
            selectedRecordId = null;
            searchDebounceTimer.Stop();
            searchDebounceTimer.Start();
        };

        btnSearch.Click += (_, _) =>
        {
            searchDebounceTimer.Stop();
            selectedRecordId = null;
            selectedJobId = null;
            Filter();
        };

        clearSearch.Click += (_, _) =>
        {
            searchDebounceTimer.Stop();
            search.Text = "";
            if (paymentStatusFilter.Items.Count > 0) paymentStatusFilter.SelectedIndex = 0;
            if (billingStatusFilter.Items.Count > 0) billingStatusFilter.SelectedIndex = 0;
            selectedJobId = null;
            selectedRecordId = null;
            Filter();
        };

        previewReceipt.Click += async (_, _) => await Run(() => OpenReceiptAsync(false));
        printReceipt.Click += async (_, _) => await Run(() => OpenReceiptAsync(false));
        printWorkOrder.Click += async (_, _) => await Run(OpenWorkOrderAsync);

        refresh.Click += async (_, _) => await LoadAsync();
        add.Click += async (_, _) => await Run(() => EditAsync(false));
        edit.Click += async (_, _) => await Run(() => EditAsync(true));
        archive.Click += async (_, _) => await Run(ArchiveAsync);
        progress.Click += async (_, _) => await Run(ProgressAsync);
        assign.Click += async (_, _) => await Run(AssignAsync);
        convert.Click += async (_, _) => await Run(ConvertAsync);
        checkInAppointment.Click += async (_, _) => await Run(CheckInAppointmentAsync);
        approveEstimate.Click += async (_, _) => await Run(ApproveEstimateAsync);
        convertEstimate.Click += async (_, _) => await Run(ConvertEstimateAsync);
        termsAcceptance.Click += async (_, _) => await Run(RecordTermsAcceptanceAsync);
        resetPassword.Click += async (_, _) => await Run(ResetStaffPasswordAsync);
        logout.Click += (_, _) => { LogoutRequested = true; api.ClearSession(); Close(); };

        records.SelectionChanged += (_, _) =>
        {
            if (bindingRecords) return;
            if (records.SelectedRows.Count > 0 && records.CurrentRow?.DataBoundItem is DataRowView view)
            {
                selectedRecordId = view["__key"]?.ToString();
            }
            else
            {
                selectedRecordId = null;
            }
            UpdateRecordActionButtons();
            UpdateTaskButtons();
        };

        records.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < records.Rows.Count)
            {
                if (records.Rows[e.RowIndex].DataBoundItem is DataRowView view)
                {
                    selectedRecordId = view["__key"]?.ToString();
                }
            }
            UpdateRecordActionButtons();
            UpdateTaskButtons();
        };

        records.DataBindingComplete += (_, _) =>
        {
            if (selectedRecordId == null)
            {
                records.CurrentCell = null;
                records.ClearSelection();
                UpdateRecordActionButtons();
                UpdateTaskButtons();
            }
        };

        jobsOverview.DataBindingComplete += (_, _) =>
        {
            if (selectedJobId == null)
            {
                jobsOverview.CurrentCell = null;
                jobsOverview.ClearSelection();
            }
        };

        ActiveControl = search;
        Shown += (_, _) =>
        {
            ActiveControl = search;
            selectedRecordId = null;
            records.CurrentCell = null;
            records.ClearSelection();
            UpdateRecordActionButtons();
            UpdateTaskButtons();
        };

        if (modules.Count == 0)
        {
            ShowWelcomeNoModulesView();
        }
        else
        {
            var btn = navigation.Controls.OfType<RadioButton>().FirstOrDefault(b => b.Tag is Module);
            if (btn != null) btn.Checked = true;
        }

        ConfigureWorkshopActions();
        FormClosing += (_, e) => { if (busy) e.Cancel = true; };
    }

    private void ShowWelcomeNoModulesView()
    {
        actions.Visible = false;
        workspace.Visible = false;

        welcomePanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(40)
        };

        var welcomeTitle = new Label
        {
            Text = "RevCare Auto Hub CRM",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 38, 56),
            Location = new Point(40, 40),
            AutoSize = true
        };

        var welcomeSub = new Label
        {
            Text = $"Workspace: {api.TenantId} | User: {api.Username} | Role: {api.Role}",
            Font = new Font("Segoe UI", 10.5F),
            ForeColor = Color.DimGray,
            Location = new Point(40, 75),
            AutoSize = true
        };

        bool isProvisioning = string.Equals(api.TenantStatus, "Provisioning", StringComparison.OrdinalIgnoreCase);

        var messageBox = new Label
        {
            Text = isProvisioning
                ? "⚠️ Tenant Setup Pending\n\nYour tenant workspace is currently in provisioning status.\nOperational business database access is blocked until database setup is complete.\n\nAccount provisioning in Master is permitted for administrators."
                : "ℹ️ No Modules Enabled\n\nYour account is authenticated and active, but no business modules are currently enabled for your tenant or role.\n\nPlease contact your platform administrator to enable modules for this workspace.",
            Font = new Font("Segoe UI", 11F),
            ForeColor = isProvisioning ? Color.FromArgb(160, 90, 0) : Color.FromArgb(30, 80, 140),
            Location = new Point(40, 120),
            Size = new Size(600, 150)
        };

        var btnSwitch = new Button
        {
            Text = "Switch Workspace / Sign Out",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Location = new Point(40, 290),
            Size = new Size(220, 36),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        btnSwitch.FlatAppearance.BorderSize = 0;
        btnSwitch.Click += (_, _) => { LogoutRequested = true; api.ClearSession(); Close(); };

        welcomePanel.Controls.Add(welcomeTitle);
        welcomePanel.Controls.Add(welcomeSub);
        welcomePanel.Controls.Add(messageBox);
        welcomePanel.Controls.Add(btnSwitch);

        Controls.Add(welcomePanel);
        welcomePanel.BringToFront();
    }

    private async Task RetryProfileSyncAsync()
    {
        var row = RequireSelection();
        string userId = row["userId"]?.ToString() ?? "";
        if (string.IsNullOrEmpty(userId)) return;

        var res = await api.RetryStaffProfileAsync(userId);
        string statusText = res?["profileProvisioningStatus"]?.GetValue<string>() ?? "Synced";
        MessageBox.Show(this, $"Staff profile sync status: {statusText}.", "Staff Profile Sync", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await Reload();
    }

    private async Task PromoteOwnerAsync()
    {
        var row = RequireSelection();
        if (Current?.Path != "staff") return;
        string currentRole = row["role"]?.ToString() ?? "";
        string displayName = row["displayName"]?.ToString() ?? row["userName"]?.ToString() ?? "this staff member";
        if (currentRole == "OwnerManager")
        {
            MessageBox.Show(this, $"{displayName} is already an Owner/Manager.", "Promote Staff", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new TenantVoidForm();
        dialog.Bind($"Promote {displayName} to Owner/Manager",
            new JsonObject { ["rowVersion"] = row["rowVersion"]?.DeepClone() },
            new() { new("reason", "Authorization reason (required)", Required: true) },
            async body =>
            {
                await api.Send("POST", $"crm/staff/{row["userId"]}/promote-owner", body);
            });

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            MessageBox.Show(this, $"Successfully promoted {displayName} to Owner/Manager.", "Owner Promotion", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Reload();
        }
    }

    private async Task Run(Func<Task> work)
    {
        if (busy) return;
        busy = true; actions.Enabled = false; navigation.Enabled = false;
        try { await work(); }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "RevCare", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            busy = false;
            actions.Enabled = true;
            navigation.Enabled = true;
            if (selectedRecordId == null)
            {
                records.CurrentCell = null;
                records.ClearSelection();
            }
            UpdateRecordActionButtons();
        }
    }

    private async Task LoadAsync() => await Run(Reload);

    private async Task Reload()
    {
        if (Current == null) return;
        var data = await api.Send("GET", "crm/" + Current.Path);
        if (data is JsonArray arr)
        {
            rows = arr.Select(n => (JsonObject)n).ToList();
        }
        else if (data is JsonObject obj)
        {
            rows = FlattenReportToRows(obj);
        }
        else
        {
            rows = new List<JsonObject>();
        }

        if (api.Has("Customers.Manage") && Current.Path is "job-orders" or "inquiries" or "complaints" or "payments")
        {
            var customers = (await api.Send("GET", "crm/customers")).AsArray().ToDictionary(c => c["customerId"].ToString());
            var jobs = Current.Path == "payments" ? (await api.Send("GET", "crm/job-orders")).AsArray().ToDictionary(j => j["jobOrderId"].ToString()) : null;
            foreach (var row in rows)
            {
                JsonNode job = null;
                if (jobs != null) jobs.TryGetValue(row["jobOrderId"].ToString(), out job);
                string customerId = (job?["customerId"] ?? row["customerId"])?.ToString();
                if (customerId != null && customers.TryGetValue(customerId, out var customer))
                {
                    row["customer"] = customer["fullName"]?.DeepClone();
                    row["customerContact"] = customer["contactNumber"]?.DeepClone();
                }
                if (job != null)
                {
                    row["estimateNumber"] = job["estimateNumber"]?.DeepClone();
                    row["vehicleDetails"] = job["vehicleDetails"]?.DeepClone();
                    row["jobBalance"] = job["outstandingBalance"]?.DeepClone();
                    row["totalEstimatedCost"] = job["totalEstimatedCost"]?.DeepClone();
                    row["totalPaid"] = job["totalPaid"]?.DeepClone();
                    row["billingStatus"] = job["outstandingBalance"] == null ? "Estimate required" : job["outstandingBalance"].GetValue<decimal>() == 0 ? "Fully paid" : job["totalPaid"].GetValue<decimal>() > 0 ? "Partially paid" : "Unpaid";
                }
            }
        }
        ConfigureActions();
        Filter();
    }

    private void ConfigureActions()
    {
        string path = Current.Path;
        workspace.Panel1Collapsed = path != "workshop";
        jobSummary.Visible = path == "workshop";
        ConfigureWorkshopVisibility();

        bool isPayments = path == "payments";
        paymentStatusFilter.Visible = isPayments;
        billingStatusFilter.Visible = isPayments;
        previewReceipt.Visible = isPayments;
        printReceipt.Visible = isPayments;
        printWorkOrder.Visible = path is "workshop" or "job-orders";
        viewTasksPopUp.Visible = path == "workshop";
        clearSearch.Visible = true;

        bool readOnly = path == "audit" || path.EndsWith("history") || path.StartsWith("reports") || (path == "services" && !api.Has("Services.Manage"));
        add.Visible = !readOnly && path != "workshop" && !path.StartsWith("reports");
        edit.Visible = (!readOnly || path.StartsWith("reports")) && (path != "payments" || api.Has("Payments.Correct")) &&
            (path != "workshop" || api.Has("Tasks.Update") || api.Has("Tasks.UpdateAssigned")) && path != "appointments" && path != "estimates";
        archive.Visible = (path is "customers" or "inquiries" or "complaints" or "feedbacks" or "appointments" ||
            path == "job-orders" && api.Has("Jobs.Delete") || path == "payments" && api.Has("Payments.Correct")) && !path.StartsWith("reports");
        archive.Text = path == "payments" ? "Void payment" : path == "job-orders" ? "Delete job" : path == "appointments" ? "Cancel appt" : "Archive";
        progress.Visible = (path is "job-orders" or "workshop") && (api.Has("Jobs.Start") || api.Has("Jobs.QA") || api.Has("Jobs.Release"));
        assign.Visible = (path is "job-orders" or "workshop") && api.Has("Tasks.Assign");
        assign.Text = "Assign Mechanic";
        convert.Visible = path == "inquiries";
        edit.Text = path == "workshop" ? "Update task" : path == "payments" ? "View Receipt" : path.StartsWith("reports") ? "Preview Report" : "Edit";

        checkInAppointment.Visible = path == "appointments" && api.Has("Collection.Manage");
        approveEstimate.Visible = path == "estimates" && api.Has("Jobs.Manage");
        convertEstimate.Visible = path == "estimates" && api.Has("Jobs.Manage");
        termsAcceptance.Visible = path == "job-orders" && api.Has("Jobs.Manage");
        resetPassword.Visible = path == "staff" && api.Has("Staff.Manage");

        retryProfileSync.Visible = path == "staff" && api.Has("Staff.Manage");
        promoteOwner.Visible = path == "staff" && api.Has("Staff.Manage");

        UpdateRecordActionButtons();
    }

    private void UpdateRecordActionButtons()
    {
        bool hasSel = Selected != null;
        string path = Current?.Path ?? "";
        bool isWorkshop = path == "workshop";
        bool isPayments = path == "payments";
        bool isJobs = path == "job-orders";

        edit.Enabled = hasSel;
        archive.Enabled = hasSel;
        string jobStatus = Selected?[isWorkshop ? "jobStatus" : "status"]?.ToString();
        string nextPermission = JobWorkflowLabels.NextPermission(jobStatus);
        progress.Text = JobWorkflowLabels.NextAction(jobStatus);
        progress.Enabled = hasSel && nextPermission != null && api.Has(nextPermission);
        assign.Enabled = hasSel;
        convert.Enabled = hasSel;
        retryProfileSync.Enabled = hasSel;
        promoteOwner.Enabled = hasSel;

        string selStatus = Selected?["status"]?.ToString() ?? "";
        checkInAppointment.Enabled = hasSel && path == "appointments" && selStatus == "Scheduled";
        approveEstimate.Enabled = hasSel && path == "estimates" && selStatus == "Draft";
        convertEstimate.Enabled = hasSel && path == "estimates" && selStatus == "Approved";
        termsAcceptance.Enabled = hasSel && isJobs;
        resetPassword.Enabled = hasSel && path == "staff";

        previewReceipt.Enabled = hasSel && isPayments;
        printReceipt.Enabled = hasSel && isPayments;
        printWorkOrder.Enabled = (isWorkshop && selectedJobId != null) || (isJobs && hasSel);
        viewTasksPopUp.Enabled = isWorkshop && selectedJobId != null;
    }

    private void Filter(bool rebuildJobs = true)
    {
        bindingRecords = true;
        try
        {
            string selectedId = selectedRecordId;
            string query = search.Text.Trim();

            IEnumerable<JsonObject> filtered = rows;

        if (Current?.Path == "payments")
        {
            if (query.Length > 0)
            {
                filtered = filtered.Where(r =>
                {
                    int pid = int.TryParse(r["paymentId"]?.ToString(), out var p) ? p : 0;
                    bool matchesReceipt = ReportTemplateBuilder.MatchesReceiptReference(pid, query);
                    string cust = r["customer"]?.ToString() ?? "";
                    string contact = r["customerContact"]?.ToString() ?? "";
                    string jid = r["jobOrderId"]?.ToString() ?? "";
                    string est = r["estimateNumber"]?.ToString() ?? "";
                    string ptype = r["paymentType"]?.ToString() ?? "";
                    string pmethod = r["paymentMethod"]?.ToString() ?? "";

                    return matchesReceipt ||
                           cust.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           contact.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           jid.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           est.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           ptype.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           pmethod.Contains(query, StringComparison.OrdinalIgnoreCase);
                });
            }

            if (paymentStatusFilter.SelectedIndex == 1)
                filtered = filtered.Where(r => r["isVoided"]?.GetValue<bool>() == false);
            else if (paymentStatusFilter.SelectedIndex == 2)
                filtered = filtered.Where(r => r["isVoided"]?.GetValue<bool>() == true);

            if (billingStatusFilter.SelectedIndex == 1)
                filtered = filtered.Where(r => r["billingStatus"]?.ToString() == "Unpaid");
            else if (billingStatusFilter.SelectedIndex == 2)
                filtered = filtered.Where(r => r["billingStatus"]?.ToString() == "Partially paid");
            else if (billingStatusFilter.SelectedIndex == 3)
                filtered = filtered.Where(r => r["billingStatus"]?.ToString() == "Fully paid");
        }
        else if (Current?.Path == "customers")
        {
            if (query.Length > 0)
            {
                filtered = filtered.Where(r =>
                {
                    string name = r["fullName"]?.ToString() ?? "";
                    string contact = r["contactNumber"]?.ToString() ?? "";
                    string email = r["email"]?.ToString() ?? "";
                    return name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           contact.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           email.Contains(query, StringComparison.OrdinalIgnoreCase);
                });
            }
        }
        else if (Current?.Path == "workshop")
        {
            if (query.Length > 0)
            {
                filtered = filtered.Where(r =>
                {
                    string cust = r["customer"]?.ToString() ?? "";
                    string jid = r["jobOrderId"]?.ToString() ?? "";
                    string veh = r["vehicleDetails"]?.ToString() ?? "";
                    string svc = r["serviceId"]?.ToString() ?? "";
                    string tech = r["assignedTo"]?.ToString() ?? "";
                    return cust.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           jid.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           veh.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           svc.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           tech.Contains(query, StringComparison.OrdinalIgnoreCase);
                });
            }

            if (qaOnly.Checked && api.Has("Jobs.QA"))
                filtered = filtered.Where(r => r["jobStatus"]?.ToString() == "QA Checking" || r["reviewStatus"]?.ToString() == "Submitted");

            var matchingWorkshopRows = filtered.ToList();
            if (rebuildJobs) BindJobOverview(matchingWorkshopRows);

            if (selectedJobId == null)
            {
                visible = new List<JsonObject>();
                jobSummary.Text = "Select a job to view its service tasks.";
            }
            else
            {
                visible = matchingWorkshopRows.Where(r => r["jobOrderId"]?.ToString() == selectedJobId && (r["jobTaskId"]?.GetValue<long>() ?? 0) > 0).ToList();
                if (visible.Count == 0)
                    jobSummary.Text = $"Job #{selectedJobId} has zero service tasks. Requested services must be added through Job Orders.";
                else
                    jobSummary.Text = $"Tasks for Job #{selectedJobId} ({visible.Count} task(s))";
            }
        }
        else
        {
            if (query.Length > 0)
            {
                filtered = filtered.Where(r => r.Any(p => p.Key != "rowVersion" && (p.Value?.ToString() ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)));
            }
        }

        if (Current?.Path != "workshop")
        {
            visible = filtered.ToList();
        }

        var table = new DataTable();
        table.Columns.Add("__key", typeof(string));
        var available = visible.SelectMany(r => r.Select(p => p.Key)).Distinct().Where(k => !k.Contains("rowVersion", StringComparison.OrdinalIgnoreCase) && k != "tasks" && k != "payments" && k != "submissionId" && k != "passwordHash").ToList();
        var columns = DisplayColumns(Current?.Path);
        var keys = columns == null ? available : columns.Where(available.Contains).ToList();
        foreach (string key in keys) table.Columns.Add(key, typeof(string));
        foreach (var row in visible) table.Rows.Add(new object[] { row[Current.Key]?.ToString() ?? "" }.Concat(keys.Select(k => (object)(row[k]?.ToString() ?? ""))).ToArray());
        records.DataSource = table;
        records.Columns["__key"].Visible = false;
        foreach (DataGridViewColumn column in records.Columns)
        {
            column.HeaderText = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Regex.Replace(column.Name, "([a-z])([A-Z])", "$1 $2"));
            if (column.Name == "serviceId") column.HeaderText = "Service / Task";
            if (column.Name == "jobTaskId") column.HeaderText = "Task #";
            if (column.Name == "jobOrderId") column.HeaderText = "Job #";
            if (column.Name == "createdAt" && Current?.Path == "payments") column.HeaderText = "Payment Date (UTC)";
        }
        GridMetadataColumns.Hide(records);

        // REMOVE AUTOMATIC RECORD SELECTION
        bool found = false;
        if (!string.IsNullOrEmpty(selectedId))
        {
            int selectedIndex = visible.FindIndex(r => r[Current.Key]?.ToString() == selectedId);
            if (selectedIndex >= 0 && records.Columns.Count > 1 && selectedIndex < records.Rows.Count)
            {
                records.Rows[selectedIndex].Selected = true;
                records.CurrentCell = records.Rows[selectedIndex].Cells[1];
                found = true;
            }
        }
        if (!found)
        {
            selectedRecordId = null;
            records.CurrentCell = null;
            records.ClearSelection();
            if (IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    if (selectedRecordId == null)
                    {
                        records.CurrentCell = null;
                        records.ClearSelection();
                        UpdateRecordActionButtons();
                        UpdateTaskButtons();
                    }
                }));
            }
        }

        if (visible.Count == 0)
        {
            status.Text = query.Length > 0 ? "No matching records found." : Current?.Path == "workshop" && selectedJobId == null ? "Select a job to view its service tasks." : "No records to display.";
        }
        else
        {
            status.Text = $"{visible.Count} matching record(s) - Tenant: {api.TenantId} - Online";
        }

        UpdateRecordActionButtons();
        UpdateTaskButtons();
        }
        finally
        {
            bindingRecords = false;
        }
    }

    private JsonObject RequireSelection() => Selected ?? throw new InvalidOperationException("Select a record first.");
    private static List<TenantChoice> Options(params string[] values) => values.Select(s => new TenantChoice(s, JsonValue.Create(s))).ToList();

    private async Task<List<TenantChoice>> Customers()
    {
        var data = (await api.Send("GET", "crm/customers")).AsArray();
        return data.Select(c => new TenantChoice($"{c["fullName"]} - {c["contactNumber"]} (#{c["customerId"]})", c["customerId"].DeepClone())).ToList();
    }

    private async Task<List<TenantChoice>> Jobs()
    {
        var customers = await Customers();
        var data = (await api.Send("GET", "crm/job-orders")).AsArray();
        return data.Select(j => new TenantChoice($"Job #{j["jobOrderId"]} - {customers.FirstOrDefault(c => c.Value.ToString() == j["customerId"]?.ToString())?.Label} - {j["vehicleDetails"]} | Balance {j["outstandingBalance"]}", j["jobOrderId"].DeepClone())).ToList();
    }

    private async Task EditAsync(bool existing)
    {
        string path = Current.Path;
        if (path == "appointments" && !existing)
        {
            await CreateAppointmentAsync();
            return;
        }
        if (path == "estimates" && !existing)
        {
            await CreateEstimateAsync();
            return;
        }
        if (path.StartsWith("reports"))
        {
            var summaryNode = await api.Send("GET", "crm/reports/operational-summary");
            if (summaryNode != null)
            {
                string html = ReportTemplateBuilder.BuildOperationalSummaryReportHtml(summaryNode, new BusinessDetails { Name = "RevCare Auto Hub (Tenant A)" }, api.Username);
                using var preview = new ReportPreviewDialogForm("Operational Executive Summary", html);
                preview.ShowDialog(this);
            }
            return;
        }

        var row = existing ? (JsonObject)RequireSelection().DeepClone() : new JsonObject { ["isActive"] = true };
        var fields = new List<TenantField>();
        void Text(string key, string label, bool required = false) => fields.Add(new(key, label, "text", required));
        void Choice(string key, string label, List<TenantChoice> choices, bool required = true) => fields.Add(new(key, label, "text", required, choices));
        string[] services = null, selectedServices = null;

        if (path == "customers")
        {
            Text("fullName", "Full name", true); Text("contactNumber", "Contact number (digits)", true); Text("email", "Email");
        }
        else if (path == "job-orders")
        {
            Choice("customerId", "Customer", await Customers()); Text("vehicleDetails", "Vehicle details", true);
            fields.Add(new("totalEstimatedCost", "Approved estimate", "decimal")); fields.Add(new("startedAt", "Started at", "date")); Text("inquiryNotes", "Intake notes");
            if (!existing) { row["startedAt"] = DateTime.Now; row["status"] = "In Queue"; }
            services = (await api.Send("GET", "crm/services")).AsArray().Where(s => s["isActive"].GetValue<bool>()).Select(s => s["name"].ToString()).ToArray();
            selectedServices = existing ? (await api.Send("GET", "crm/job-tasks?jobOrderId=" + row["jobOrderId"])).AsArray().Select(t => t["serviceId"].ToString()).ToArray() : Array.Empty<string>();
        }
        else if (path == "workshop")
        {
            Choice("status", "Task status", Options("Pending", "Ongoing", "Done")); Text("remarks", "Mechanic notes");
        }
        else if (path == "payments")
        {
            Choice("jobOrderId", "Job order", await Jobs()); fields.Add(new("amountPaid", "Amount paid", "decimal", true));
            Choice("paymentMethod", "Method", Options("Cash", "Card", "GCash", "Transfer"));
            Choice("paymentType", "Type", Options("Downpayment", "Progress Payment", "Balance Settlement", "Direct Retail"));
            fields.Add(new("allowCustomerCredit", "Allow customer credit balance", "bool"));
        }
        else if (path == "inquiries")
        {
            Choice("customerId", "Customer (optional)", await Customers(), false);
            Text("customerName", "Customer name (if not registered)"); Text("contactNumber", "Contact number (digits)");
            Choice("inquiryPlatform", "Platform", Options("Walk-in", "Call", "Facebook", "Website"));
            Text("vehicleDetails", "Vehicle details"); Text("messageDetails", "Inquiry details", true);
            Choice("status", "Status", Options("New", "Converted", "Cancelled"));
            if (!existing) row["status"] = "New";
        }
        else if (path == "complaints")
        {
            Choice("jobOrderId", "Job order", await Jobs()); Text("complaintType", "Complaint type", true); Text("description", "Description", true);
            Choice("resolutionStatus", "Resolution", Options("Open", "Investigating", "Resolved")); Text("actionTaken", "Action taken");
            if (!existing) row["resolutionStatus"] = "Open";
        }
        else if (path == "feedbacks")
        {
            Choice("jobOrderId", "Job order", await Jobs()); Choice("ratingScore", "Rating", Enumerable.Range(1, 5).Select(i => new TenantChoice(i + " stars", JsonValue.Create(i))).ToList());
            Text("positiveRemarks", "Positive remarks"); Text("areasForImprovement", "Areas for improvement");
        }
        else if (path == "services")
        {
            Text("name", "Service name", true); fields.Add(new("price", "Reference price", "decimal", true)); fields.Add(new("isActive", "Active", "bool"));
        }
        else if (path == "staff")
        {
            if (!existing) Text("username", "Username", true);
            Text("displayName", "Display name", true);
            var staffRoles = existing && row["role"]?.ToString() == "OwnerManager"
                ? Options("OwnerManager", "ServiceAdvisor", "LeadMechanic", "AutomotiveMechanic", "DetailingTechnician")
                : Options("ServiceAdvisor", "LeadMechanic", "AutomotiveMechanic", "DetailingTechnician");
            Choice("role", "Role", staffRoles);
            if (existing) fields.Add(new("isActive", "Active", "bool"));
        }
        else return;

        string endpoint = path == "workshop" ? "job-tasks" : path;
        using TenantEntryForm dialog = path switch
        {
            "customers" => new TenantCustomerForm(),
            "job-orders" => new TenantJobOrderForm(),
            "workshop" => new TenantTaskForm(),
            "payments" => new TenantPaymentForm(),
            "staff" => new TenantStaffForm(),
            "services" => new TenantServiceForm(),
            "inquiries" => new TenantInquiryForm(),
            "complaints" => new TenantComplaintForm(),
            "feedbacks" => new TenantFeedbackForm(),
            _ => throw new InvalidOperationException("No editor is available for this module.")
        };

        dialog.Bind((existing ? "Edit " : "Add ") + Current.Name, row, fields, async body =>
        {
            await api.Send(existing ? "PUT" : "POST", "crm/" + endpoint + (existing ? "/" + row[Current.Key] : ""), body);
        }, services, selectedServices, path == "staff" && !existing);

        if (dialog is TenantPaymentForm billing)
            billing.ConfigureBilling((await api.Send("GET", "crm/job-orders")).AsArray(), existing ? row["jobOrderId"].ToString() : null);

        if (dialog.ShowDialog(this) == DialogResult.OK) await Reload();
    }

    private async Task ArchiveAsync()
    {
        var row = RequireSelection(); string path = Current.Path;
        if (path == "payments")
        {
            using var dialog = new TenantVoidForm();
            dialog.Bind("Void payment", new JsonObject { ["rowVersion"] = row["rowVersion"].DeepClone() },
                new() { new("reason", "Reason for void", Required: true) }, async body => { await api.Send("POST", "crm/payments/" + row["paymentId"] + "/void", body); });
            if (dialog.ShowDialog(this) == DialogResult.OK) await Reload();
        }
        else if (path == "appointments")
        {
            using var dialog = new TenantVoidForm();
            dialog.Bind("Cancel appointment", new JsonObject(),
                new() { new("reason", "Reason for cancellation", Required: true) },
                async body =>
                {
                    string reason = body["reason"]?.ToString() ?? "Customer requested cancellation";
                    await api.Send("POST", $"crm/appointments/{row["appointmentId"]}/cancel", new JsonObject { ["reasonCategory"] = reason });
                });
            if (dialog.ShowDialog(this) == DialogResult.OK) await Reload();
        }
        else if (MessageBox.Show(this, "Confirm " + archive.Text.ToLowerInvariant() + " for the selected record?", "RevCare", MessageBoxButtons.YesNo) == DialogResult.Yes)
        { await api.Send("DELETE", "crm/" + path + "/" + row[Current.Key]); await Reload(); }
    }

    private async Task CheckInAppointmentAsync()
    {
        var row = RequireSelection();
        int apptId = int.Parse(row["appointmentId"].ToString());
        string custName = row["customerName"]?.ToString() ?? "Customer";

        if (MessageBox.Show(this, $"Check in appointment #{apptId} for {custName}?\nThis will create a new Job Order in Queue.", "Confirm Check In", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var res = await api.Send("POST", $"crm/appointments/{apptId}/check-in");
        string jobOrderId = res?["jobOrderId"]?.ToString() ?? "";
        MessageBox.Show(this, $"Appointment #{apptId} checked in successfully.\nCreated Job Order #{jobOrderId}.", "Appointment Check-In", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await Reload();
    }

    private async Task ApproveEstimateAsync()
    {
        var row = RequireSelection();
        int estId = int.Parse(row["estimateId"].ToString());
        string estNum = row["estimateNumber"]?.ToString() ?? $"EST-{estId:D6}";

        if (MessageBox.Show(this, $"Approve {estNum}?", "Confirm Approval", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        await api.Send("POST", $"crm/estimates/{estId}/approve");
        MessageBox.Show(this, $"{estNum} has been approved.", "Estimate Approved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await Reload();
    }

    private async Task ConvertEstimateAsync()
    {
        var row = RequireSelection();
        int estId = int.Parse(row["estimateId"].ToString());
        string estNum = row["estimateNumber"]?.ToString() ?? $"EST-{estId:D6}";

        if (MessageBox.Show(this, $"Convert {estNum} to a Job Order?\nThis will create a Job Order in Queue and initialize linked tasks.", "Confirm Conversion", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var res = await api.Send("POST", $"crm/estimates/{estId}/convert");
        string jobOrderId = res?["jobOrderId"]?.ToString() ?? "";
        MessageBox.Show(this, $"{estNum} converted to Job Order #{jobOrderId}.", "Estimate Converted", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await Reload();
    }

    private async Task RecordTermsAcceptanceAsync()
    {
        var row = RequireSelection();
        int jobId = int.Parse(row["jobOrderId"].ToString());

        var termsDoc = await api.Send("GET", "crm/terms");
        string title = termsDoc?["title"]?.ToString() ?? "Standard Workshop Terms & Conditions";
        string version = termsDoc?["version"]?.ToString() ?? "v1.0";

        if (MessageBox.Show(this,
            $"Customer agrees to {title} ({version}) for Job Order #{jobId}?\n\n- 50% deposit required prior to work\n- Customer approval for additional defects\n- Settlement prior to vehicle release",
            "Terms & Conditions Acceptance", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var body = new JsonObject
        {
            ["termsVersion"] = version,
            ["customerAcknowledged"] = true,
            ["notes"] = "Accepted in person by customer via intake session."
        };

        await api.Send("POST", $"crm/job-orders/{jobId}/terms-acceptance", body);
        MessageBox.Show(this, $"Terms acceptance ({version}) successfully recorded for Job Order #{jobId}.", "Terms Accepted", MessageBoxButtons.OK, MessageBoxIcon.Information);
        await Reload();
    }

    private async Task ResetStaffPasswordAsync()
    {
        var row = RequireSelection();
        string userId = row["userId"]?.ToString() ?? "";
        string name = row["displayName"]?.ToString() ?? row["userName"]?.ToString() ?? "Staff";

        using var prompt = new Form
        {
            Text = $"Reset Password for {name}",
            Size = new Size(400, 200),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lbl = new Label { Text = "Enter new temporary password (min 12 characters):", Location = new Point(20, 20), AutoSize = true };
        var txt = new TextBox { Location = new Point(20, 50), Width = 340, UseSystemPasswordChar = true };
        var btnOk = new Button { Text = "Reset Password", Location = new Point(140, 100), Width = 120, Height = 34, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancel", Location = new Point(270, 100), Width = 90, Height = 34, DialogResult = DialogResult.Cancel };

        prompt.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
        prompt.AcceptButton = btnOk; prompt.CancelButton = btnCancel;

        if (prompt.ShowDialog(this) == DialogResult.OK)
        {
            string newPass = txt.Text.Trim();
            if (newPass.Length < 12)
            {
                MessageBox.Show(this, "Password must be at least 12 characters.", "Invalid Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var body = new JsonObject { ["newPassword"] = newPass };
            await api.Send("POST", $"crm/staff/{userId}/reset-password", body);
            MessageBox.Show(this, $"Password for {name} has been reset successfully.", "Password Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Reload();
        }
    }

    private async Task CreateAppointmentAsync()
    {
        var customers = await Customers();
        if (customers.Count == 0)
        {
            MessageBox.Show(this, "No active customers found. Create a customer first.", "Appointments", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new Form
        {
            Text = "Schedule New Appointment",
            Size = new Size(460, 360),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblCust = new Label { Text = "Customer:", Location = new Point(20, 20), AutoSize = true };
        var cboCust = new ComboBox { Location = new Point(20, 45), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = customers, DisplayMember = "Label" };

        var lblDate = new Label { Text = "Scheduled Date & Time:", Location = new Point(20, 85), AutoSize = true };
        var dtp = new DateTimePicker { Location = new Point(20, 110), Width = 400, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd hh:mm tt", Value = DateTime.Now.AddHours(2) };

        var lblSvc = new Label { Text = "Requested Services (semicolon-separated):", Location = new Point(20, 150), AutoSize = true };
        var txtSvc = new TextBox { Location = new Point(20, 175), Width = 400, Text = "Change Oil; Brake Service" };

        var lblNotes = new Label { Text = "Intake Notes:", Location = new Point(20, 215), AutoSize = true };
        var txtNotes = new TextBox { Location = new Point(20, 240), Width = 400 };

        var btnOk = new Button { Text = "Schedule Appointment", Location = new Point(200, 280), Width = 150, Height = 34, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancel", Location = new Point(360, 280), Width = 60, Height = 34, DialogResult = DialogResult.Cancel };

        form.Controls.AddRange(new Control[] { lblCust, cboCust, lblDate, dtp, lblSvc, txtSvc, lblNotes, txtNotes, btnOk, btnCancel });
        form.AcceptButton = btnOk; form.CancelButton = btnCancel;

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            var selCust = (TenantChoice)cboCust.SelectedItem;
            int custId = selCust.Value.GetValue<int>();
            var body = new JsonObject
            {
                ["customerId"] = custId,
                ["scheduledStart"] = dtp.Value.ToUniversalTime(),
                ["requestedServices"] = txtSvc.Text.Trim(),
                ["notes"] = txtNotes.Text.Trim()
            };

            await api.Send("POST", "crm/appointments", body);
            MessageBox.Show(this, "Appointment scheduled successfully.", "Appointment Scheduled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Reload();
        }
    }

    private async Task CreateEstimateAsync()
    {
        var customers = await Customers();
        if (customers.Count == 0)
        {
            MessageBox.Show(this, "No active customers found. Create a customer first.", "Estimates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new Form
        {
            Text = "Create New Estimate",
            Size = new Size(460, 380),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var lblCust = new Label { Text = "Customer:", Location = new Point(20, 20), AutoSize = true };
        var cboCust = new ComboBox { Location = new Point(20, 45), Width = 400, DropDownStyle = ComboBoxStyle.DropDownList, DataSource = customers, DisplayMember = "Label" };

        var lblDesc = new Label { Text = "Service / Line Description:", Location = new Point(20, 85), AutoSize = true };
        var txtDesc = new TextBox { Location = new Point(20, 110), Width = 400, Text = "General Inspection and Maintenance" };

        var lblPrice = new Label { Text = "Unit Price (₱):", Location = new Point(20, 150), AutoSize = true };
        var numPrice = new NumericUpDown { Location = new Point(20, 175), Width = 400, Maximum = 1000000, DecimalPlaces = 2, Value = 1500m };

        var lblNotes = new Label { Text = "Estimate Notes:", Location = new Point(20, 215), AutoSize = true };
        var txtNotes = new TextBox { Location = new Point(20, 240), Width = 400 };

        var btnOk = new Button { Text = "Create Estimate", Location = new Point(220, 290), Width = 130, Height = 34, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancel", Location = new Point(360, 290), Width = 60, Height = 34, DialogResult = DialogResult.Cancel };

        form.Controls.AddRange(new Control[] { lblCust, cboCust, lblDesc, txtDesc, lblPrice, numPrice, lblNotes, txtNotes, btnOk, btnCancel });
        form.AcceptButton = btnOk; form.CancelButton = btnCancel;

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            var selCust = (TenantChoice)cboCust.SelectedItem;
            int custId = selCust.Value.GetValue<int>();
            var lines = new JsonArray
            {
                new JsonObject
                {
                    ["description"] = txtDesc.Text.Trim(),
                    ["quantity"] = 1m,
                    ["unitPrice"] = numPrice.Value
                }
            };
            var body = new JsonObject
            {
                ["customerId"] = custId,
                ["notes"] = txtNotes.Text.Trim(),
                ["lines"] = lines
            };

            await api.Send("POST", "crm/estimates", body);
            MessageBox.Show(this, "Estimate created successfully.", "Estimate Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await Reload();
        }
    }

    private static List<JsonObject> FlattenReportToRows(JsonObject report)
    {
        var list = new List<JsonObject>();
        void AddRow(string category, string metric, object value) => list.Add(new JsonObject
        {
            ["category"] = category,
            ["metric"] = metric,
            ["value"] = value?.ToString() ?? "—"
        });

        if (report["overview"] is JsonObject ov)
        {
            AddRow("Overview", "Total Active Customers", ov["totalCustomers"]);
            AddRow("Overview", "Total Job Orders", ov["totalJobOrders"]);
            AddRow("Overview", "Active Job Orders", ov["activeJobOrders"]);
            AddRow("Overview", "Completed Job Orders", ov["completedJobOrders"]);
        }
        if (report["workshopExecution"] is JsonObject ws)
        {
            AddRow("Workshop Execution", "Total Tasks", ws["totalTasks"]);
            AddRow("Workshop Execution", "Completed Tasks", ws["completedTasks"]);
            AddRow("Workshop Execution", "Pending/Ongoing Tasks", ws["pendingOrOngoingTasks"]);
            AddRow("Workshop Execution", "Task Completion Rate", $"{ws["taskCompletionPercentage"]}%");
            AddRow("Workshop Execution", "Average Turnaround Hours", $"{ws["averageTurnaroundHours"]} hrs");
        }
        if (report["financialSummary"] is JsonObject fin)
        {
            decimal billed = fin["totalEstimatedActive"]?.GetValue<decimal>() ?? 0m;
            decimal gross = fin["grossCollections"]?.GetValue<decimal>() ?? 0m;
            decimal refund = fin["refundsIssued"]?.GetValue<decimal>() ?? 0m;
            decimal net = fin["netCollections"]?.GetValue<decimal>() ?? 0m;
            decimal recv = fin["outstandingReceivables"]?.GetValue<decimal>() ?? 0m;
            AddRow("Financial Summary", "Active Estimated Cost", $"₱{billed:N2}");
            AddRow("Financial Summary", "Gross Collections", $"₱{gross:N2}");
            AddRow("Financial Summary", "Refunds Issued", $"₱{refund:N2}");
            AddRow("Financial Summary", "Net Collections", $"₱{net:N2}");
            AddRow("Financial Summary", "Outstanding Receivables", $"₱{recv:N2}");
        }
        if (report["appointments"] is JsonObject appt)
        {
            AddRow("Appointments", "Total Scheduled Appointments", appt["total"]);
            AddRow("Appointments", "Currently Scheduled", appt["scheduled"]);
            AddRow("Appointments", "Checked In", appt["checkedIn"]);
            AddRow("Appointments", "Cancelled Appointments", appt["cancelled"]);
            AddRow("Appointments", "No Show", appt["noShow"]);
        }
        if (report["customerExperience"] is JsonObject csat)
        {
            AddRow("Customer Satisfaction", "Total Feedback Records", csat["feedbackCount"]);
            AddRow("Customer Satisfaction", "Average CSAT Score", $"{csat["averageCsatScore"]} / 5.0 ⭐");
            AddRow("Customer Satisfaction", "Open Complaints", csat["openComplaints"]);
            AddRow("Customer Satisfaction", "Total Complaints", csat["totalComplaints"]);
        }
        return list;
    }

    private async Task ProgressAsync()
    {
        var row = RequireSelection(); bool workshop = Current.Path == "workshop";
        string current = row[workshop ? "jobStatus" : "status"].ToString();
        string next = current switch { "In Queue" => "In Progress", "In Progress" => "Completed", "QA Checking" => "Completed", "Completed" => "Released", _ => null };
        string permission = next switch { "In Progress" => "Jobs.Start", "QA Checking" or "Completed" => "Jobs.QA", "Released" => "Jobs.Release", _ => "" };
        if (next == null || !api.Has(permission)) throw new InvalidOperationException("Your role cannot advance this job from its current stage.");
        string explanation = current switch
        {
            "In Queue" => "Start work on this job? The required 50% deposit must be recorded.",
            "In Progress" => "Complete this job? All tasks must be finished and their final inspections approved.",
            "QA Checking" => "Complete this job? The final inspection must be approved. Payment and vehicle release are separate steps.",
            _ => "Release this vehicle to the customer? The job must be completed and fully paid."
        };
        if (MessageBox.Show(this, explanation, JobWorkflowLabels.NextAction(current), MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        await api.Send("POST", "crm/job-orders/" + row["jobOrderId"] + "/status", new JsonObject { ["status"] = next, ["rowVersion"] = row[workshop ? "jobRowVersion" : "rowVersion"].DeepClone() });
        await Reload();
    }

    private async Task AssignAsync()
    {
        var row = RequireSelection();
        bool workshop = Current.Path == "workshop";
        string jobOrderId = workshop ? row["jobOrderId"]?.ToString() : row["jobOrderId"]?.ToString();
        var jobRowVersion = workshop ? row["jobRowVersion"] : row["rowVersion"];
        string currentUserId = workshop ? row["assignedUserId"]?.ToString() : row["assignedMechanicUserId"]?.ToString();

        var assignees = (await api.Send("GET", "crm/assignees")).AsArray()
            .Select(s => new TenantChoice(s["displayName"] + " - " + s["role"], s["userId"].DeepClone())).ToList();

        using var dialog = new TenantAssignmentForm();
        dialog.Bind("Assign Mechanic — Job #" + jobOrderId,
            new JsonObject { ["rowVersion"] = jobRowVersion.DeepClone(), ["userId"] = currentUserId != null ? JsonValue.Create(currentUserId) : null },
            new() { new("userId", "Mechanic / Technician", Required: true, Choices: assignees), new("reason", "Assignment reason", Required: true) },
            async body => { await api.Send("PUT", "crm/job-orders/" + jobOrderId + "/assignment", body); });
        if (dialog.ShowDialog(this) == DialogResult.OK) await Reload();
    }

    private async Task ConvertAsync()
    {
        var row = RequireSelection();
        var customers = await Customers(); customers.Insert(0, new TenantChoice("Create from inquiry details", null));
        using var dialog = new TenantConvertForm();
        dialog.Bind("Convert inquiry to job", new JsonObject { ["customerId"] = row["customerId"]?.DeepClone() },
            new() { new("customerId", "Customer", Choices: customers), new("vehicleDetails", "Vehicle details", Required: true) },
            async body => { await api.Send("POST", "crm/inquiries/" + row["inquiryId"] + "/convert", body); });
        if (dialog.ShowDialog(this) == DialogResult.OK) await Reload();
    }

    private async Task OpenReceiptAsync(bool directPrint)
    {
        var row = RequireSelection();
        if (Current?.Path != "payments") return;
        int paymentId = row["paymentId"]?.GetValue<int>() ?? int.Parse(row["paymentId"].ToString());

        var doc = await api.Send("GET", $"crm/payments/{paymentId}/receipt");
        if (doc == null)
        {
            MessageBox.Show(this, "Could not load payment receipt from server.", "RevCare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string rawDate = doc["paymentDate"]?.ToString();
        if (string.IsNullOrWhiteSpace(rawDate) || !DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var paymentDate))
        {
            MessageBox.Show(this, "The receipt cannot be displayed because the payment record is missing a valid transaction timestamp.", "Payment Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string customerName = doc["customerName"]?.ToString() ?? "Valued Customer";
        int jobId = doc["jobOrderId"]?.GetValue<int>() ?? 0;
        string vehicle = doc["vehicleDetails"]?.ToString() ?? "";
        string paymentType = doc["paymentType"]?.ToString() ?? "Payment";
        string paymentMethod = doc["paymentMethod"]?.ToString() ?? "Cash";
        decimal amountReceived = doc["amountReceived"]?.GetValue<decimal>() ?? 0;
        decimal? totalEstimate = doc["totalEstimatedCost"]?.GetValue<decimal>();
        decimal totalPaid = doc["totalPaid"]?.GetValue<decimal>() ?? amountReceived;
        decimal? balance = doc["outstandingBalance"]?.GetValue<decimal>();
        decimal? customerCredit = doc["customerCredit"]?.GetValue<decimal>();
        bool isVoided = doc["isVoided"]?.GetValue<bool>() ?? false;
        string voidReason = doc["voidReason"]?.ToString() ?? "";
        DateTime? voidedAtUtc = DateTime.TryParse(doc["voidedAtUtc"]?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var vdt) ? vdt : null;
        DateTime calculatedAtUtc = DateTime.TryParse(doc["calculatedAtUtc"]?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var cdt) ? cdt : DateTime.UtcNow;
        string recordedBy = doc["recordedBy"]?.ToString() ?? "Not recorded";
        string printedBy = api.Username;

        var bObj = doc["businessDetails"];
        var business = bObj != null
            ? new BusinessDetails
            {
                Name = bObj["name"]?.ToString() ?? "RevCare Auto Hub",
                Tagline = bObj["tagline"]?.ToString(),
                Address = bObj["address"]?.ToString(),
                Contact = bObj["contact"]?.ToString()
            }
            : new BusinessDetails();

        var receiptData = new PaymentReceiptData
        {
            PaymentId = paymentId,
            PaymentDate = paymentDate,
            CustomerName = customerName,
            JobOrderId = jobId,
            VehicleDetails = vehicle,
            PaymentType = paymentType,
            PaymentMethod = paymentMethod,
            AmountReceived = amountReceived,
            TotalEstimatedCost = totalEstimate,
            TotalPaid = totalPaid,
            OutstandingBalance = balance,
            CustomerCredit = customerCredit,
            RecordedBy = recordedBy,
            PrintedBy = printedBy,
            IsVoided = isVoided,
            VoidReason = voidReason,
            VoidedAtUtc = voidedAtUtc,
            CalculatedAtUtc = calculatedAtUtc
        };

        string html = ReportTemplateBuilder.BuildPaymentReceiptHtml(receiptData, business);
        ReportPreviewDialogForm.ShowPreview(this, $"Payment Receipt - {receiptData.ReceiptReference}", html);
    }

    private async Task OpenWorkOrderAsync()
    {
        string jobId = Current?.Path == "workshop" ? selectedJobId : Selected?["jobOrderId"]?.ToString();
        if (string.IsNullOrEmpty(jobId))
        {
            MessageBox.Show(this, "Select a job first to preview the Mechanic's Job Order Copy.", "RevCare", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var doc = await api.Send("GET", $"crm/workshop/jobs/{jobId}/work-order");
        if (doc == null)
        {
            MessageBox.Show(this, "Could not load the work order document.", "RevCare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int jid = doc["jobOrderId"]?.GetValue<int>() ?? int.Parse(jobId);
        string estRef = doc["estimateReference"]?.ToString() ?? $"EST-{jid:D6}";
        string customerName = doc["customerName"]?.ToString() ?? "Valued Customer";
        string vehicleDetails = doc["vehicleDetails"]?.ToString() ?? "";
        string intakeNotes = doc["intakeNotes"]?.ToString() ?? "";
        string jobStatus = doc["jobStatus"]?.ToString() ?? "In Progress";

        var bObj = doc["businessDetails"];
        var business = bObj != null
            ? new BusinessDetails
            {
                Name = bObj["name"]?.ToString() ?? "RevCare Auto Hub",
                Tagline = bObj["tagline"]?.ToString(),
                Address = bObj["address"]?.ToString(),
                Contact = bObj["contact"]?.ToString()
            }
            : new BusinessDetails();

        var taskItems = new List<MechanicTaskItem>();
        if (doc["tasks"] is JsonArray taskArray)
        {
            foreach (var node in taskArray)
            {
                if (node is JsonObject t)
                {
                    taskItems.Add(new MechanicTaskItem
                    {
                        TaskId = t["taskId"]?.GetValue<int>() ?? 0,
                        ServiceName = t["serviceName"]?.ToString() ?? "",
                        AssignedTechnician = t["assignedTechnician"]?.ToString() ?? "Unassigned",
                        Status = t["status"]?.ToString() ?? "Pending",
                        Notes = t["notes"]?.ToString() ?? "",
                        ReviewStatus = t["reviewStatus"]?.ToString() ?? "Not Submitted"
                    });
                }
            }
        }

        var workOrderData = new MechanicWorkOrderData
        {
            JobOrderId = jid,
            EstimateReference = estRef,
            CustomerName = customerName,
            VehicleDetails = vehicleDetails,
            JobStatus = jobStatus,
            IntakeNotes = intakeNotes,
            PrintDate = DateTime.UtcNow,
            Tasks = taskItems
        };

        string html = ReportTemplateBuilder.BuildMechanicCopyHtml(workOrderData, business);
        ReportPreviewDialogForm.ShowPreview(this, $"Mechanic's Job Order Copy - Job #{jobId}", html);
    }
}
