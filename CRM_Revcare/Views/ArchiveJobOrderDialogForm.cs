#nullable enable
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public sealed class ArchiveJobOrderDialogForm : Form
    {
        private readonly JobOrder job;
        private readonly Func<MasterCrmDbContext> factory;

        private Label lblHeaderInfo;
        private Label lblCustomerInfo;
        private Label lblVehicleInfo;
        private Label lblFinancialInfo;
        private Panel eligibilityBanner;
        private Label lblEligibility;

        private ComboBox cmbReasonCategory;
        private TextBox txtExplanation;
        private Label lblError;

        private Button btnConfirmArchive;
        private Button btnClose;

        private ReadinessResult eligibility = null!;

        public ArchiveJobOrderDialogForm(JobOrder job, Func<MasterCrmDbContext> factory)
        {
            this.job = job ?? throw new ArgumentNullException(nameof(job));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

            InitializeUi();
            RefreshEligibility();
        }

        private void InitializeUi()
        {
            Text = $"Archive Job Order — {RecordReference.JobOrder(job.JobOrderId)}";
            Size = new Size(580, 520);
            MinimumSize = new Size(540, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(246, 248, 250),
                Padding = new Padding(16, 10, 16, 10)
            };
            var title = new Label
            {
                Text = $"Archive Job Order — {RecordReference.JobOrder(job.JobOrderId)}",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(14, 8),
                AutoSize = true
            };
            lblHeaderInfo = new Label
            {
                Text = $"Status: {JobOrderWorkflow.Normalize(job.Status)} | Estimate: {job.EstimateNumber}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.DimGray,
                Location = new Point(15, 34),
                AutoSize = true
            };
            topPanel.Controls.Add(title);
            topPanel.Controls.Add(lblHeaderInfo);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            int y = 10;
            lblCustomerInfo = new Label { Location = new Point(20, y), Size = new Size(250, 20), Font = new Font("Segoe UI", 9F) };
            lblVehicleInfo = new Label { Location = new Point(280, y), Size = new Size(250, 20), Font = new Font("Segoe UI", 9F) };
            y += 24;
            lblFinancialInfo = new Label { Location = new Point(20, y), Size = new Size(510, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };

            y += 28;
            eligibilityBanner = new Panel { Location = new Point(20, y), Size = new Size(510, 60), Padding = new Padding(8) };
            lblEligibility = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5F) };
            eligibilityBanner.Controls.Add(lblEligibility);

            y += 70;
            var lblCat = new Label { Text = "Archive Reason Category *", Location = new Point(20, y), Size = new Size(510, 18), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            y += 22;
            cmbReasonCategory = new ComboBox
            {
                Location = new Point(20, y),
                Size = new Size(510, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbReasonCategory.Items.AddRange(new object[]
            {
                "Completed & Released with full financial settlement",
                "Cancelled job with completed financial resolution",
                "Customer archive request",
                "Administrative record archiving",
                "Other (specify below)"
            });
            cmbReasonCategory.SelectedIndex = 0;
            cmbReasonCategory.SelectedIndexChanged += (_, _) => ValidateInputs();

            y += 34;
            var lblExp = new Label { Text = "Detailed Archive Reason / Operational Notes *", Location = new Point(20, y), Size = new Size(510, 18), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            y += 22;
            txtExplanation = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(510, 65),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            txtExplanation.TextChanged += (_, _) => ValidateInputs();

            y += 72;
            lblError = new Label
            {
                Location = new Point(20, y),
                Size = new Size(510, 36),
                ForeColor = Color.Firebrick,
                Font = new Font("Segoe UI", 8.5F)
            };

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(246, 248, 250),
                Padding = new Padding(14, 8, 14, 8)
            };

            btnConfirmArchive = new Button
            {
                Text = "📦 Confirm Archival",
                Size = new Size(160, 34),
                Location = new Point(16, 9),
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnConfirmArchive.Click += async (_, _) => await ConfirmArchiveAsync();

            btnClose = new Button
            {
                Text = "Cancel",
                Size = new Size(90, 34),
                Location = new Point(460, 9),
                DialogResult = DialogResult.Cancel
            };

            bottomPanel.Controls.Add(btnConfirmArchive);
            bottomPanel.Controls.Add(btnClose);

            body.Controls.Add(lblCustomerInfo);
            body.Controls.Add(lblVehicleInfo);
            body.Controls.Add(lblFinancialInfo);
            body.Controls.Add(eligibilityBanner);
            body.Controls.Add(lblCat);
            body.Controls.Add(cmbReasonCategory);
            body.Controls.Add(lblExp);
            body.Controls.Add(txtExplanation);
            body.Controls.Add(lblError);

            Controls.Add(body);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
        }

        private void RefreshEligibility()
        {
            try
            {
                using var db = factory();
                var freshJob = db.JobOrders.AsNoTracking().FirstOrDefault(j => j.JobOrderId == job.JobOrderId) ?? job;
                var cust = freshJob.CustomerId.HasValue ? db.Customers.AsNoTracking().FirstOrDefault(c => c.CustomerId == freshJob.CustomerId.Value) : null;

                lblCustomerInfo.Text = $"Customer: {(cust != null ? cust.FullName : "Unassigned")}";
                lblVehicleInfo.Text = $"Vehicle: {freshJob.VehicleDisplay}";

                decimal cost = freshJob.TotalEstimatedCost ?? 0m;
                decimal validPaid = db.Payments.Where(p => p.JobOrderId == freshJob.JobOrderId && !p.IsVoided).Sum(p => (decimal?)p.AmountPaid) ?? 0m;
                decimal refunded = db.Refunds.Where(r => r.JobOrderId == freshJob.JobOrderId).Sum(r => (decimal?)r.Amount) ?? 0m;
                decimal netPaid = Math.Max(0, validPaid - refunded);
                decimal balance = Math.Max(0, cost - netPaid);

                lblFinancialInfo.Text = $"Cost: ₱{cost:N2} | Net Paid: ₱{netPaid:N2} | Balance: ₱{balance:N2} | Status: {freshJob.Status}";

                eligibility = WorkshopReadinessService.EvaluateArchive(db, freshJob, "validation_probe");

                if (eligibility.IsPermitted)
                {
                    eligibilityBanner.BackColor = Color.FromArgb(240, 253, 244);
                    lblEligibility.ForeColor = Color.FromArgb(22, 101, 52);
                    lblEligibility.Text = $"✓ Eligible for Archival.\nThis {freshJob.Status} job has settled financial obligations and is safe to archive.";
                }
                else
                {
                    eligibilityBanner.BackColor = Color.FromArgb(254, 242, 242);
                    lblEligibility.ForeColor = Color.FromArgb(153, 27, 27);
                    lblEligibility.Text = $"✕ Archival Blocked:\n{eligibility.Message}";
                }

                ValidateInputs();
            }
            catch (Exception ex)
            {
                lblEligibility.Text = "Error assessing eligibility: " + ex.Message;
                btnConfirmArchive.Enabled = false;
            }
        }

        private void ValidateInputs()
        {
            lblError.Text = "";
            if (!eligibility.IsPermitted)
            {
                btnConfirmArchive.Enabled = false;
                lblError.Text = eligibility.Message;
                return;
            }

            string exp = txtExplanation.Text.Trim();
            if (string.IsNullOrWhiteSpace(exp) && cmbReasonCategory.SelectedIndex == cmbReasonCategory.Items.Count - 1)
            {
                btnConfirmArchive.Enabled = false;
                lblError.Text = "Please enter specific details for 'Other' reason category.";
                return;
            }

            btnConfirmArchive.Enabled = true;
        }

        private async Task ConfirmArchiveAsync()
        {
            string cat = cmbReasonCategory.SelectedItem?.ToString() ?? "Archived";
            string exp = txtExplanation.Text.Trim();
            string fullReason = string.IsNullOrWhiteSpace(exp) ? cat : $"{cat}: {exp}";

            var confirm = MessageBox.Show(this,
                $"Are you sure you want to archive {RecordReference.JobOrder(job.JobOrderId)}?\n\n" +
                $"Reason: {fullReason}\n\n" +
                "The job will be moved to Archived Records. All task details, payments, receipts, and audit history will remain intact.",
                "Confirm Archival",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            btnConfirmArchive.Enabled = false;
            try
            {
                var result = await JobOrderArchivingService.ArchiveJobOrderAsync(factory, job.JobOrderId, fullReason, "System Administrator");
                if (result.IsPermitted)
                {
                    MessageBox.Show(this, result.Message, "Job Archived", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show(this, result.Message, "Archival Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    RefreshEligibility();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to archive job: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnConfirmArchive.Enabled = true;
            }
        }
    }
}
