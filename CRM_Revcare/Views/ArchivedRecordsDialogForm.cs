#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.common;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public sealed class ArchivedRecordsDialogForm : Form
    {
        private readonly Func<MasterCrmDbContext> factory;
        private readonly Action onRestoreRefresh;

        private DataGridView grid;
        private TextBox txtSearch;
        private Button btnRestore;
        private Button btnViewTasks;
        private Button btnRefresh;
        private Button btnClose;
        private Label lblCount;

        private List<JobOrder> allArchivedJobs = new();
        private List<JobOrder> filteredArchivedJobs = new();

        public ArchivedRecordsDialogForm(Func<MasterCrmDbContext> factory, Action onRestoreRefresh)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.onRestoreRefresh = onRestoreRefresh ?? (() => { });

            InitializeUi();
            LoadData();
        }

        private void InitializeUi()
        {
            Text = "RevCare Auto Hub — Archived Records Manager";
            Size = new Size(1100, 600);
            MinimumSize = new Size(950, 480);
            StartPosition = FormStartPosition.CenterParent;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(246, 248, 250),
                Padding = new Padding(16, 10, 16, 10)
            };

            var title = new Label
            {
                Text = "📦 Archived Workshop Records",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, 8),
                AutoSize = true
            };

            var subtitle = new Label
            {
                Text = "Viewing completed, released, or cancelled job orders that were archived. Historical records, tasks, payments, and receipts remain completely preserved.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.DimGray,
                Location = new Point(15, 34),
                AutoSize = true
            };

            var searchPanel = new FlowLayoutPanel
            {
                Location = new Point(14, 54),
                Size = new Size(700, 28),
                FlowDirection = FlowDirection.LeftToRight
            };

            var lblSearch = new Label { Text = "Search:", AutoSize = true, Margin = new Padding(0, 5, 4, 0) };
            txtSearch = new TextBox { Width = 280, PlaceholderText = "Search by job #, customer, vehicle, reason..." };
            txtSearch.TextChanged += (_, _) => FilterData();

            lblCount = new Label { Text = "0 archived job(s)", AutoSize = true, Margin = new Padding(12, 5, 0, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            searchPanel.Controls.Add(lblSearch);
            searchPanel.Controls.Add(txtSearch);
            searchPanel.Controls.Add(lblCount);

            topPanel.Controls.Add(title);
            topPanel.Controls.Add(subtitle);
            topPanel.Controls.Add(searchPanel);

            // Grid
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                BackgroundColor = Color.White,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(226, 232, 240)
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "JobOrderId", HeaderText = "Job Order #", Width = 125, MinimumWidth = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "Customer", Width = 180, MinimumWidth = 120 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vehicle", HeaderText = "Vehicle", Width = 160, MinimumWidth = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 110, MinimumWidth = 80 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cost", HeaderText = "Total Cost", Width = 120, MinimumWidth = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ArchivedAt", HeaderText = "Archived At", Width = 140, MinimumWidth = 100 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ArchivedBy", HeaderText = "Archived By", Width = 130, MinimumWidth = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ArchiveReason", HeaderText = "Archive Reason", Width = 280, MinimumWidth = 150 });

            CrmGridStandardizer.ApplyStandardSettings(grid);
            GridRowNumbering.Enable(grid);

            grid.SelectionChanged += (_, _) =>
            {
                bool hasSel = grid.SelectedRows.Count > 0;
                btnRestore.Enabled = hasSel;
                btnViewTasks.Enabled = hasSel;
            };

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(246, 248, 250),
                Padding = new Padding(14, 8, 14, 8)
            };

            btnRestore = new Button
            {
                Text = "🔄 Restore Selected Job",
                Size = new Size(180, 34),
                Location = new Point(14, 9),
                BackColor = Color.FromArgb(22, 101, 52),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Enabled = false
            };
            btnRestore.Click += async (_, _) => await RestoreSelectedJobAsync();

            btnViewTasks = new Button
            {
                Text = "📋 View Tasks History",
                Size = new Size(160, 34),
                Location = new Point(200, 9),
                Enabled = false
            };
            btnViewTasks.Click += (_, _) => ViewSelectedJobTasks();

            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Size = new Size(95, 34),
                Location = new Point(366, 9)
            };
            btnRefresh.Click += (_, _) => LoadData();

            btnClose = new Button
            {
                Text = "Close",
                Size = new Size(90, 34),
                Location = new Point(980, 9),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                DialogResult = DialogResult.OK
            };

            bottomPanel.Controls.Add(btnRestore);
            bottomPanel.Controls.Add(btnViewTasks);
            bottomPanel.Controls.Add(btnRefresh);
            bottomPanel.Controls.Add(btnClose);

            Controls.Add(grid);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
        }

        private void LoadData()
        {
            try
            {
                using var db = factory();
                allArchivedJobs = db.JobOrders
                    .AsNoTracking()
                    .Where(j => j.IsArchived)
                    .OrderByDescending(j => j.ArchivedAtUtc ?? j.CreatedAt)
                    .ToList();

                FilterData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load archived records: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterData()
        {
            string q = txtSearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(q))
            {
                filteredArchivedJobs = allArchivedJobs.ToList();
            }
            else
            {
                filteredArchivedJobs = allArchivedJobs.Where(j =>
                    RecordReference.JobOrder(j.JobOrderId).Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (j.VehicleDisplay ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (j.Status ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (j.ArchiveReason ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (j.ArchivedBy ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            lblCount.Text = $"{filteredArchivedJobs.Count} archived job(s)";
            PopulateGrid();
        }

        private void PopulateGrid()
        {
            grid.Rows.Clear();
            using var db = factory();
            var customerIds = filteredArchivedJobs.Select(j => j.CustomerId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            var customers = db.Customers.AsNoTracking().Where(c => customerIds.Contains(c.CustomerId)).ToDictionary(c => c.CustomerId, c => c.FullName);

            foreach (var job in filteredArchivedJobs)
            {
                string custName = job.CustomerId.HasValue && customers.TryGetValue(job.CustomerId.Value, out var cName) ? cName : "Unlinked";
                string costText = job.TotalEstimatedCost.HasValue ? $"₱{job.TotalEstimatedCost.Value:N2}" : "Not set";
                string archDate = job.ArchivedAtUtc.HasValue ? job.ArchivedAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "Unknown";

                int rowIdx = grid.Rows.Add(
                    RecordReference.JobOrder(job.JobOrderId),
                    custName,
                    job.VehicleDisplay,
                    job.Status,
                    costText,
                    archDate,
                    job.ArchivedBy ?? "Admin",
                    job.ArchiveReason ?? ""
                );
                grid.Rows[rowIdx].Tag = job;
            }

            if (grid.Rows.Count > 0)
            {
                grid.Rows[0].Selected = true;
                btnRestore.Enabled = true;
                btnViewTasks.Enabled = true;
            }
            else
            {
                btnRestore.Enabled = false;
                btnViewTasks.Enabled = false;
            }
        }

        private async Task RestoreSelectedJobAsync()
        {
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not JobOrder job) return;

            string reason = "";
            using (var promptForm = new Form())
            {
                promptForm.Text = $"Restore Record — {RecordReference.JobOrder(job.JobOrderId)}";
                promptForm.Size = new Size(460, 230);
                promptForm.StartPosition = FormStartPosition.CenterParent;
                promptForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                promptForm.MaximizeBox = false;
                promptForm.MinimizeBox = false;

                var lbl = new Label
                {
                    Text = $"Restoring {RecordReference.JobOrder(job.JobOrderId)} will make it visible in active lists again.\nIts operational status ({job.Status}) and financial records remain unchanged.\n\nEnter mandatory restoration reason:",
                    Location = new Point(16, 14),
                    Size = new Size(410, 55)
                };
                var txt = new TextBox { Location = new Point(16, 75), Size = new Size(410, 48), Multiline = true };
                var btnOk = new Button { Text = "Confirm Restore", DialogResult = DialogResult.OK, Location = new Point(190, 135), Size = new Size(130, 32), BackColor = Color.FromArgb(22, 101, 52), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(326, 135), Size = new Size(100, 32) };

                promptForm.Controls.Add(lbl);
                promptForm.Controls.Add(txt);
                promptForm.Controls.Add(btnOk);
                promptForm.Controls.Add(btnCancel);
                promptForm.AcceptButton = btnOk;
                promptForm.CancelButton = btnCancel;

                if (promptForm.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(txt.Text))
                {
                    if (string.IsNullOrWhiteSpace(txt.Text)) MessageBox.Show(this, "Restoration reason is required.", "Restoration Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                reason = txt.Text.Trim();
            }

            try
            {
                var result = await JobOrderArchivingService.RestoreJobOrderAsync(factory, job.JobOrderId, reason, "System Administrator");
                if (result.IsPermitted)
                {
                    MessageBox.Show(this, result.Message, "Job Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData();
                    onRestoreRefresh();
                }
                else
                {
                    MessageBox.Show(this, result.Message, "Restore Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to restore job: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ViewSelectedJobTasks()
        {
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not JobOrder job) return;

            using var dlg = new JobTasksDialogForm(job.JobOrderId, factory);
            dlg.ShowDialog(this);
        }
    }
}
