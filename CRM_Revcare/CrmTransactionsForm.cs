using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public partial class CrmTransactionsForm : Form
    {
        private readonly Func<Task> syncAction;
        private bool syncing;
        private readonly CancellationTokenSource refreshLifetime = new CancellationTokenSource();
        private bool automaticRefreshStarted;
        private DateTime? lastSuccessfulRefresh;
        private readonly System.Collections.Generic.List<JobOrderLookup> jobLookups = new();
        private System.Collections.Generic.List<JobOrder> jobRecords = new();
        private JobOrderLookup BindJobLookup(ComboBox input, bool required, TransactionPage page, Func<MasterCrmDbContext> factory)
        {
            var lookup = new JobOrderLookup(input, required, factory, page.FindControl<Label>("Result"));
            jobLookups.Add(lookup);
            page.Grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && page.Grid.Columns[e.ColumnIndex].DataPropertyName == "JobOrderId" && e.Value is int id)
                {
                    e.Value = lookup.Describe(id);
                    e.FormattingApplied = true;
                }
            };
            return lookup;
        }
        private JobOrderLookup BindJobLookup(TextBox input, bool required, TransactionPage page, Func<MasterCrmDbContext> factory)
        {
            var lookup = new JobOrderLookup(input, required, factory, page.FindControl<Label>("Result"));
            jobLookups.Add(lookup);
            page.Grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && page.Grid.Columns[e.ColumnIndex].DataPropertyName == "JobOrderId" && e.Value is int id)
                {
                    e.Value = lookup.Describe(id);
                    e.FormattingApplied = true;
                }
            };
            return lookup;
        }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool LogoutRequested { get; private set; }
        public Task SyncAsync() => syncAction?.Invoke() ?? Task.CompletedTask;
        public CrmTransactionsForm() : this(CrmDatabase.CreateContext) { }

        public CrmTransactionsForm(Func<MasterCrmDbContext> contextFactory, Func<string, bool> confirmDelete = null)
        {
            if (contextFactory == null) throw new ArgumentNullException(nameof(contextFactory));
            InitializeComponent();
            if (System.ComponentModel.LicenseManager.UsageMode == System.ComponentModel.LicenseUsageMode.Designtime) return;
            try { using var initDb = contextFactory(); CrmDataSeeder.EnsureStaffSeeded(initDb); } catch { }
            confirmDelete ??= message => MessageBox.Show(this, message, "Confirm action",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            var customers = BuildCustomers(contextFactory, confirmDelete);
            var inquiries = BuildInquiries(contextFactory, confirmDelete);
            var complaints = BuildComplaints(contextFactory, confirmDelete);
            var feedback = BuildFeedback(contextFactory, confirmDelete);
            var orders = BuildJobOrders(contextFactory, confirmDelete);
            var tasks = BuildJobTasks(contextFactory, confirmDelete);
            var payments = BuildPayments(contextFactory, confirmDelete);
            orders.RecordsLoaded += rows =>
            {
                jobRecords = rows.ToList();
                foreach (var lookup in jobLookups) lookup.Replace(rows);
                refreshTaskJobs?.Invoke();
                tasksPage.Grid.Invalidate();
                paymentsPage.Grid.Invalidate();
                complaintsPage.Grid.Invalidate();
                feedbackPage.Grid.Invalidate();
            };

            void EnableCellTooltips(DataGridView grid)
            {
                grid.ShowCellToolTips = true;
                grid.CellToolTipTextNeeded += (s, e) =>
                {
                    if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                    {
                        var val = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                        if (val != null)
                        {
                            string sVal = val.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(sVal) && sVal.Length > 15)
                            {
                                e.ToolTipText = sVal;
                            }
                        }
                    }
                };
            }

            EnableCellTooltips(inquiriesPage.Grid);
            EnableCellTooltips(complaintsPage.Grid);
            EnableCellTooltips(feedbackPage.Grid);
            EnableCellTooltips(ordersPage.Grid);
            EnableCellTooltips(tasksPage.Grid);
            EnableCellTooltips(paymentsPage.Grid);
            EnableCellTooltips(customersPage.Grid);
            var pages = new Control[] { inquiriesPage, complaintsPage, feedbackPage, ordersPage, tasksPage, paymentsPage, customersPage };
            var buttons = new RadioButton[] { inquiryMenu, complaintMenu, feedbackMenu, orderMenu, taskMenu, paymentMenu, customerMenu };
            for (int i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                var button = buttons[i];
                button.Tag = page;
                button.CheckedChanged += (s, e) =>
                {
                    if (!button.Checked) return;
                    foreach (var otherPage in pages) otherPage.Visible = otherPage == page;
                    page.BringToFront();

                    // Remove automatic pre-selection when switching modules
                    if (page is TransactionPage tp && tp.Grid != null)
                    {
                        tp.Grid.ClearSelection();
                        tp.Grid.CurrentCell = null;
                        var editBtn = tp.Controls.Find("EditRecord", true).FirstOrDefault() as Button;
                        if (editBtn != null) editBtn.Enabled = false;
                        var delBtn = tp.Controls.Find("DeleteRecord", true).FirstOrDefault() as Button;
                        if (delBtn != null) delBtn.Enabled = false;
                        var resLbl = tp.Controls.Find("Result", true).FirstOrDefault() as Label;
                        if (resLbl != null) resLbl.Text = "Select a row to edit or view details.";
                    }
                    if (page is JobTaskPage jtp)
                    {
                        jtp.jobsGrid.ClearSelection();
                        jtp.jobsGrid.CurrentCell = null;
                        jtp.Grid.ClearSelection();
                        jtp.Grid.CurrentCell = null;
                        jtp.editRecordButton.Enabled = false;
                        jtp.printMechanicCopyButton.Enabled = false;
                        if (jtp.btnViewTasksPopUp != null) jtp.btnViewTasksPopUp.Enabled = false;
                        jtp.selectedJobLabel.Text = "Select a job or double-click to view tasks in pop-up.";
                    }
                };
            }
            inquiryMenu.Checked = true;
            var dbInfo = CrmDatabase.GetActiveDatabaseInfo();
            syncButton.Text = dbInfo.Environment == CrmEnvironment.Cloud ? "Refresh Cloud" : "Refresh Local";
            syncAction = async () =>
            {
                if (syncing || IsDisposed || refreshLifetime.IsCancellationRequested) return;
                refreshTimer.Stop();
                syncing = true; syncButton.Enabled = false; logoutButton.Enabled = false; content.Enabled = false;
                syncStatus.Text = $"[{dbInfo.EnvironmentDisplayName}] Refreshing records ({dbInfo.ServerDisplayName})... Last successful refresh: {LastRefreshText()}";
                try
                {
                    // Stage every read before updating the UI; failed reads leave every grid intact.
                    var snapshot = await Task.Run(async () =>
                    {
                        using var db = contextFactory();
                        var customerRows = await db.Customers.AsNoTracking().ToListAsync(refreshLifetime.Token);
                        var inquiryRows = await db.Inquiries.AsNoTracking().ToListAsync(refreshLifetime.Token);
                        var complaintRows = await db.Complaints.AsNoTracking().ToListAsync(refreshLifetime.Token);
                        var feedbackRows = await db.Feedbacks.AsNoTracking().ToListAsync(refreshLifetime.Token);
                        var orderRows = await db.JobOrders.AsNoTracking().ToListAsync(refreshLifetime.Token);
                        var taskRows = await db.JobTasks.AsNoTracking().ToListAsync(refreshLifetime.Token);
                        var paymentRows = await db.Payments.AsNoTracking().ToListAsync(refreshLifetime.Token);
                        return (customerRows, inquiryRows, complaintRows, feedbackRows, orderRows, taskRows, paymentRows);
                    });
                    if (IsDisposed || refreshLifetime.IsCancellationRequested) return;
                    customers.ApplySyncedRecords(snapshot.customerRows);
                    inquiries.ApplySyncedRecords(snapshot.inquiryRows); complaints.ApplySyncedRecords(snapshot.complaintRows);
                    feedback.ApplySyncedRecords(snapshot.feedbackRows); orders.ApplySyncedRecords(snapshot.orderRows);
                    tasks.ApplySyncedRecords(snapshot.taskRows); payments.ApplySyncedRecords(snapshot.paymentRows);
                    jobRecords = snapshot.orderRows.ToList();
                    lastSuccessfulRefresh = DateTime.Now;
                    syncStatus.Text = $"[{dbInfo.EnvironmentDisplayName}] Connected: {dbInfo.ServerDisplayName} / {dbInfo.DatabaseName}. Last successful refresh: {LastRefreshText()}. Auto-refresh every 60 seconds.";
                }
                catch (OperationCanceledException) when (refreshLifetime.IsCancellationRequested) { }
                catch (Exception ex)
                {
                    CrmDiagnostics.LogException("Record Synchronization", ex, dbInfo);
                    if (!IsDisposed && !refreshLifetime.IsCancellationRequested)
                    {
                        syncStatus.Text = $"[{dbInfo.EnvironmentDisplayName}] Connection failed. Unable to reach database ({dbInfo.ServerDisplayName} / {dbInfo.DatabaseName}). Retrying in 60 seconds.";
                    }
                }
                finally
                {
                    syncing = false;
                    if (!IsDisposed && !refreshLifetime.IsCancellationRequested)
                    {
                        syncButton.Enabled = true; logoutButton.Enabled = true; content.Enabled = true;
                        if (automaticRefreshStarted) refreshTimer.Start();
                    }
                }
            };
            syncButton.Click += async (s, e) => await SyncAsync();
            logoutButton.Click += (s, e) =>
            {
                if (MessageBox.Show(this, "Log out? Unsaved editor changes will be discarded.", "Logout",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                LogoutRequested = true; Close();
            };
            refreshTimer.Tick += async (s, e) =>
            {
                // Defer while a confirmation dialog is open rather than refreshing beneath it.
                if (!Enabled || Form.ActiveForm?.Modal == true) return;
                await SyncAsync();
            };
            FormClosed += (s, e) => { automaticRefreshStarted = false; refreshLifetime.Cancel(); refreshTimer.Stop(); };
            Shown += async (s, e) =>
            {
                automaticRefreshStarted = true;
                await SyncAsync();
                foreach (var p in pages)
                {
                    if (p is TransactionPage tp && tp.Grid != null)
                    {
                        tp.Grid.ClearSelection();
                        tp.Grid.CurrentCell = null;
                        var editBtn = tp.Controls.Find("EditRecord", true).FirstOrDefault() as Button;
                        if (editBtn != null) editBtn.Enabled = false;
                        var delBtn = tp.Controls.Find("DeleteRecord", true).FirstOrDefault() as Button;
                        if (delBtn != null) delBtn.Enabled = false;
                    }
                    if (p is JobTaskPage jtp)
                    {
                        jtp.jobsGrid.ClearSelection();
                        jtp.jobsGrid.CurrentCell = null;
                        jtp.Grid.ClearSelection();
                        jtp.Grid.CurrentCell = null;
                        jtp.editRecordButton.Enabled = false;
                        jtp.printMechanicCopyButton.Enabled = false;
                        if (jtp.btnViewTasksPopUp != null) jtp.btnViewTasksPopUp.Enabled = false;
                    }
                }
            };
        }

        private string LastRefreshText() => lastSuccessfulRefresh?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                automaticRefreshStarted = false;
                refreshLifetime.Cancel();
                refreshTimer?.Stop();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private CrmCollectionController<Inquiry> BuildInquiries(Func<MasterCrmDbContext> factory, Func<string, bool> confirm)
        {
            var tab = new CrmCollectionController<Inquiry>(inquiriesPage, "Inquiry", x => x.InquiryId, factory, confirm);
            var search = inquiriesPage.search;
            var clear = inquiriesPage.clearButton;
            var searchBtn = inquiriesPage.searchButton;
            var count = inquiriesPage.countLabel;

            tab.ConfigureListTools(row =>
            {
                string query = search.Text.Trim();
                if (query.Length == 0) return true;
                string custName = row.CustomerName ?? (row.CustomerId is int id && customerRecords.TryGetValue(id, out var c) ? c.FullName : "");
                string custPhone = row.ContactNumber ?? (row.CustomerId is int cid && customerRecords.TryGetValue(cid, out var cc) ? cc.ContactNumber : "");
                return RecordReference.Matches(RecordReference.InquiryPrefix, row.InquiryId, query)
                    || custName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || custPhone.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (row.InquiryPlatform ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (row.MessageDetails ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (row.Status ?? "").Contains(query, StringComparison.OrdinalIgnoreCase);
            }, (visible, total) =>
            {
                string query = search.Text.Trim();
                count.Text = visible == 0 && total > 0
                    ? $"No matching records found for \"{query}\"."
                    : total == 0 ? "No inquiries recorded yet."
                    : $"Showing {visible} of {total} inquiries.";
            });

            var inquiryDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
            inquiryDebounceTimer.Tick += (s, e) => { inquiryDebounceTimer.Stop(); tab.ApplyListFilter(); };
            search.TextChanged += (s, e) => { inquiryDebounceTimer.Stop(); inquiryDebounceTimer.Start(); };
            searchBtn.Click += (s, e) => { inquiryDebounceTimer.Stop(); tab.ApplyListFilter(); };
            clear.Click += (s, e) => { inquiryDebounceTimer.Stop(); search.Clear(); tab.ApplyListFilter(); };

            inquiriesPage.convertButton.Click += async (s, e) =>
            {
                if (tab.SelectedRecord == null) { inquiriesPage.resultLabel.Text = "Select an inquiry to convert."; return; }
                using var dialog = new InquiryConversionDialogForm(tab.SelectedRecord.InquiryId, factory);
                if (dialog.ShowDialog(this) == DialogResult.OK) await SyncAsync();
            };
            inquiriesPage.appointmentsButton.Click += async (s, e) =>
            {
                int? inqId = tab.SelectedRecord?.InquiryId;
                int? custId = tab.SelectedRecord?.CustomerId;
                using var dialog = new AppointmentManagerDialogForm(factory, inqId, custId);
                dialog.ShowDialog(this);
                await SyncAsync();
            };
            tab.OpenDialogEditor = row =>
            {
                using var dlg = new InquiryDialogForm(row, factory, customerRecords.Values);
                return dlg.ShowDialog(inquiriesPage.FindForm()) == DialogResult.OK;
            };
            inquiriesPage.Grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || inquiriesPage.Grid.Rows[e.RowIndex].DataBoundItem is not Inquiry row) return;
                string property = inquiriesPage.Grid.Columns[e.ColumnIndex].DataPropertyName;
                if (property == "InquiryId" && e.Value is int inqId)
                {
                    e.Value = RecordReference.Inquiry(inqId);
                    e.FormattingApplied = true;
                }
                else if (property == "CustomerName" && row.CustomerId is int id && customerRecords.TryGetValue(id, out var c))
                {
                    e.Value = c.FullName;
                    e.FormattingApplied = true;
                }
                else if (property == "ContactNumber" && row.CustomerId is int cid && customerRecords.TryGetValue(cid, out var cc))
                {
                    string rawPhone = cc.ContactNumber;
                    string classification = CRM.infrastructure.services.ContactNumberHelper.Classify(rawPhone);
                    if (classification == CRM.infrastructure.services.ContactNumberHelper.ClassificationDemo)
                        e.Value = $"{rawPhone} [Demo]";
                    else if (classification == CRM.infrastructure.services.ContactNumberHelper.ClassificationInvalid)
                        e.Value = $"{rawPhone} [Invalid]";
                    else if (classification == CRM.infrastructure.services.ContactNumberHelper.ClassificationMissing)
                        e.Value = "[Missing]";
                    else
                        e.Value = rawPhone;
                    e.FormattingApplied = true;
                }
                else if (property == "CreatedAt" && e.Value is DateTime dt)
                {
                    e.Value = dt.ToString("yyyy-MM-dd HH:mm");
                    e.FormattingApplied = true;
                }
            };
            return tab;
        }

        private CrmCollectionController<Complaint> BuildComplaints(Func<MasterCrmDbContext> factory, Func<string, bool> confirm)
        {
            var tab = new CrmCollectionController<Complaint>(complaintsPage, "Complaint", x => x.ComplaintId, factory, confirm);
            var search = complaintsPage.search;
            var clear = complaintsPage.clearButton;
            var searchBtn = complaintsPage.searchButton;
            var count = complaintsPage.countLabel;

            tab.ConfigureListTools(row =>
            {
                string query = search.Text.Trim();
                if (query.Length == 0) return true;
                int? cid = row.CustomerId;
                if (!cid.HasValue && row.JobOrderId is int jid)
                {
                    var j = jobRecords.FirstOrDefault(x => x.JobOrderId == jid);
                    if (j?.CustomerId != null) cid = j.CustomerId;
                }
                string custName = cid.HasValue && customerRecords.TryGetValue(cid.Value, out var c) ? c.FullName : "";
                string custPhone = cid.HasValue && customerRecords.TryGetValue(cid.Value, out var c2) ? c2.ContactNumber : "";
                return RecordReference.Matches(RecordReference.ComplaintPrefix, row.ComplaintId, query)
                    || (row.JobOrderId.HasValue && RecordReference.Matches(RecordReference.JobOrderPrefix, row.JobOrderId.Value, query))
                    || custName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || custPhone.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (row.ComplaintType ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (row.Description ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (row.ResolutionStatus ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (row.ActionTaken ?? "").Contains(query, StringComparison.OrdinalIgnoreCase);
            }, (visible, total) =>
            {
                string query = search.Text.Trim();
                count.Text = visible == 0 && total > 0
                    ? $"No matching records found for \"{query}\"."
                    : total == 0 ? "No complaints recorded yet."
                    : $"Showing {visible} of {total} complaints.";
            });

            var complaintDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
            complaintDebounceTimer.Tick += (s, e) => { complaintDebounceTimer.Stop(); tab.ApplyListFilter(); };
            search.TextChanged += (s, e) => { complaintDebounceTimer.Stop(); complaintDebounceTimer.Start(); };
            searchBtn.Click += (s, e) => { complaintDebounceTimer.Stop(); tab.ApplyListFilter(); };
            clear.Click += (s, e) => { complaintDebounceTimer.Stop(); search.Clear(); tab.ApplyListFilter(); };

            tab.OpenDialogEditor = row =>
            {
                using var dlg = new ComplaintDialogForm(row, factory, customerRecords.Values, jobRecords);
                return dlg.ShowDialog(complaintsPage.FindForm()) == DialogResult.OK;
            };
            complaintsPage.Grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || complaintsPage.Grid.Rows[e.RowIndex].DataBoundItem is not Complaint row) return;
                string property = complaintsPage.Grid.Columns[e.ColumnIndex].DataPropertyName;
                if (property == "ComplaintId" && e.Value is int cmpId)
                {
                    e.Value = RecordReference.Complaint(cmpId);
                    e.FormattingApplied = true;
                }
                else if (property == "CustomerId")
                {
                    int? cid = row.CustomerId;
                    if (!cid.HasValue && row.JobOrderId is int jid)
                    {
                        var j = jobRecords.FirstOrDefault(x => x.JobOrderId == jid);
                        if (j?.CustomerId != null) cid = j.CustomerId;
                    }
                    if (cid.HasValue && customerRecords.TryGetValue(cid.Value, out var c))
                    {
                        e.Value = $"{c.FullName} ({c.ContactNumber})";
                    }
                    else
                    {
                        e.Value = "General / No Customer";
                    }
                    e.FormattingApplied = true;
                }
                else if (property == "JobOrderId")
                {
                    if (row.JobOrderId is int jid)
                    {
                        var j = jobRecords.FirstOrDefault(x => x.JobOrderId == jid);
                        if (j != null)
                        {
                            string cust = j.CustomerId is int jcid && customerRecords.TryGetValue(jcid, out var jc) ? jc.FullName : "Customer Unlinked";
                            e.Value = $"{RecordReference.JobOrder(jid)} - {cust} | {j.EstimateId} [{j.Status}]";
                        }
                        else
                        {
                            e.Value = RecordReference.JobOrder(jid);
                        }
                    }
                    else
                    {
                        e.Value = "General (No Job)";
                    }
                    e.FormattingApplied = true;
                }
                else if (property == "CreatedAt" && e.Value is DateTime dt)
                {
                    e.Value = dt.ToString("yyyy-MM-dd HH:mm");
                    e.FormattingApplied = true;
                }
            };
            return tab;
        }

        private CrmCollectionController<Feedback> BuildFeedback(Func<MasterCrmDbContext> factory, Func<string, bool> confirm)
        {
            var tab = new CrmCollectionController<Feedback>(feedbackPage, "Feedback", x => x.FeedbackId, factory, confirm);
            var search = feedbackPage.search;
            var clear = feedbackPage.clearButton;
            var searchBtn = feedbackPage.searchButton;
            var count = feedbackPage.countLabel;

            tab.ConfigureListTools(row =>
            {
                string query = search.Text.Trim();
                if (query.Length == 0) return true;
                var j = jobRecords.FirstOrDefault(x => x.JobOrderId == row.JobOrderId);
                string custName = j?.CustomerId is int cid && customerRecords.TryGetValue(cid, out var c) ? c.FullName : "";
                string custPhone = j?.CustomerId is int cid2 && customerRecords.TryGetValue(cid2, out var c2) ? c2.ContactNumber : "";
                return RecordReference.Matches(RecordReference.FeedbackPrefix, row.FeedbackId, query)
                    || RecordReference.Matches(RecordReference.JobOrderPrefix, row.JobOrderId, query)
                    || custName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || custPhone.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || row.RatingScore.ToString().Contains(query)
                    || (row.PositiveRemarks ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (row.AreasForImprovement ?? "").Contains(query, StringComparison.OrdinalIgnoreCase);
            }, (visible, total) =>
            {
                string query = search.Text.Trim();
                count.Text = visible == 0 && total > 0
                    ? $"No matching records found for \"{query}\"."
                    : total == 0 ? "No feedback recorded yet."
                    : $"Showing {visible} of {total} feedback records.";
            });

            var feedbackDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
            feedbackDebounceTimer.Tick += (s, e) => { feedbackDebounceTimer.Stop(); tab.ApplyListFilter(); };
            search.TextChanged += (s, e) => { feedbackDebounceTimer.Stop(); feedbackDebounceTimer.Start(); };
            searchBtn.Click += (s, e) => { feedbackDebounceTimer.Stop(); tab.ApplyListFilter(); };
            clear.Click += (s, e) => { feedbackDebounceTimer.Stop(); search.Clear(); tab.ApplyListFilter(); };

            tab.OpenDialogEditor = row =>
            {
                using var dlg = new FeedbackDialogForm(row, factory, customerRecords.Values, jobRecords);
                bool saved = dlg.ShowDialog(feedbackPage.FindForm()) == DialogResult.OK;
                if (saved) complaintsPage.RefreshRecords();
                return saved;
            };
            feedbackPage.Grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || feedbackPage.Grid.Rows[e.RowIndex].DataBoundItem is not Feedback row) return;
                string property = feedbackPage.Grid.Columns[e.ColumnIndex].DataPropertyName;
                if (property == "FeedbackId" && e.Value is int fbId)
                {
                    e.Value = RecordReference.Feedback(fbId);
                    e.FormattingApplied = true;
                }
                else if (property == "JobOrderId")
                {
                    var j = jobRecords.FirstOrDefault(x => x.JobOrderId == row.JobOrderId);
                    if (j != null)
                    {
                        string cust = j.CustomerId is int cid && customerRecords.TryGetValue(cid, out var c) ? $"{c.FullName} ({c.ContactNumber})" : "Customer Unlinked";
                        e.Value = $"{RecordReference.JobOrder(row.JobOrderId)} - {cust} | {j.EstimateId} [{j.Status}]";
                    }
                    else
                    {
                        e.Value = RecordReference.JobOrder(row.JobOrderId);
                    }
                    e.FormattingApplied = true;
                }
                else if (property == "RatingScore")
                {
                    e.Value = row.RatingScore switch
                    {
                        5 => "★★★★★ 5.0 (Excellent)",
                        4 => "★★★★☆ 4.0 (Good)",
                        3 => "★★★☆☆ 3.0 (Average)",
                        2 => "★★☆☆☆ 2.0 (Poor)",
                        1 => "★☆☆☆☆ 1.0 (Terrible)",
                        _ => row.RatingScore.ToString()
                    };
                    e.FormattingApplied = true;
                }
                else if (property == "CreatedAt" && e.Value is DateTime dt)
                {
                    e.Value = dt.ToString("yyyy-MM-dd HH:mm");
                    e.FormattingApplied = true;
                }
            };
            return tab;
        }

        private static void SelectValue(ComboBox input, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) { input.SelectedIndex = 0; return; }
            // Preserve existing legacy values rather than silently replacing them during an edit.
            if (!input.Items.Contains(value)) input.Items.Add(value);
            input.SelectedItem = value;
        }

        private static string Required(Control input, string label)
        {
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                input.Focus();
                throw new InvalidOperationException(label + " is required.");
            }
            return input.Text.Trim();
        }

        private static int? Reference(TextBox input, string label, bool required)
        {
            if (!required && string.IsNullOrWhiteSpace(input.Text)) return null;
            if (!int.TryParse(input.Text, out var id) || id <= 0)
            {
                input.Focus();
                throw new InvalidOperationException(label + " must be a positive whole number.");
            }
            return id;
        }

        private void inquiriesPage_Load(object sender, EventArgs e)
        {

        }
    }
}



