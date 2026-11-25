#nullable enable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public sealed class CancelJobDialogForm : Form
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
        private Label lblExplanationHelp;
        private NumericUpDown numIncurredCharges;
        private NumericUpDown numProposedRefund;
        private Label lblProposedRefund;
        private Label lblIncurredCharges;
        private Label lblError;

        private Button btnConfirmDirect;
        private Button btnSubmitRequest;
        private Button btnViewRequest;
        private Button btnClose;

        private CancellationEligibility eligibility = null!;

        public string CancellationReason { get; private set; } = "";

        public CancelJobDialogForm(JobOrder job, Func<MasterCrmDbContext> factory)
        {
            this.job = job ?? throw new ArgumentNullException(nameof(job));
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

            InitializeUi();
            RefreshEligibility();
        }

        private void InitializeUi()
        {
            Text = $"Cancel Job Order — {RecordReference.JobOrder(job.JobOrderId)}";
            Size = new Size(580, 560);
            MinimumSize = new Size(540, 500);
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
                Text = $"Job Order Cancellation & Review — {RecordReference.JobOrder(job.JobOrderId)}",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
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
            // Customer & Vehicle summary
            lblCustomerInfo = new Label { Location = new Point(20, y), Size = new Size(250, 20), Font = new Font("Segoe UI", 9F) };
            lblVehicleInfo = new Label { Location = new Point(280, y), Size = new Size(250, 20), Font = new Font("Segoe UI", 9F) };
            y += 24;
            lblFinancialInfo = new Label { Location = new Point(20, y), Size = new Size(510, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };

            y += 28;
            // Eligibility Banner
            eligibilityBanner = new Panel { Location = new Point(20, y), Size = new Size(510, 55), Padding = new Padding(8) };
            lblEligibility = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5F) };
            eligibilityBanner.Controls.Add(lblEligibility);

            y += 65;
            var lblCategory = new Label { Text = "Cancellation Reason Category (Required):", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            cmbReasonCategory = new ComboBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(510, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cmbReasonCategory.Items.AddRange(CancellationRefundService.CancellationReasonCategories);
            cmbReasonCategory.SelectedIndex = 0;
            cmbReasonCategory.SelectedIndexChanged += (s, e) => UpdateContextualControls();

            y += 55;
            var lblExp = new Label { Text = "Detailed Explanation / Notes:", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblExplanationHelp = new Label { Text = "(Required if 'Other' is chosen)", Location = new Point(230, y), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Italic), ForeColor = Color.DimGray };
            txtExplanation = new TextBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(510, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9F)
            };
            txtExplanation.TextChanged += (s, e) => UpdateContextualControls();

            y += 90;
            lblIncurredCharges = new Label { Text = "Incurred Charges (PHP):", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            numIncurredCharges = new NumericUpDown { Location = new Point(20, y + 20), Size = new Size(230, 23), DecimalPlaces = 2, Maximum = 1000000m, Font = new Font("Segoe UI", 9F) };

            lblProposedRefund = new Label { Text = "Proposed Refund (PHP):", Location = new Point(280, y), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold) };
            numProposedRefund = new NumericUpDown { Location = new Point(280, y + 20), Size = new Size(250, 23), DecimalPlaces = 2, Maximum = 1000000m, Font = new Font("Segoe UI", 9F) };

            y += 55;
            lblError = new Label { Location = new Point(20, y), Size = new Size(510, 30), Font = new Font("Segoe UI", 8.5F), ForeColor = Color.Firebrick };

            y += 35;
            var btnFlow = new FlowLayoutPanel { Location = new Point(20, y), Size = new Size(510, 40), FlowDirection = FlowDirection.RightToLeft };

            btnClose = new Button { Text = "Close", Size = new Size(80, 32), DialogResult = DialogResult.Cancel, Font = new Font("Segoe UI", 9F) };

            btnConfirmDirect = new Button
            {
                Text = "Confirm Cancellation",
                Size = new Size(160, 32),
                BackColor = Color.FromArgb(215, 58, 73),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnConfirmDirect.FlatAppearance.BorderSize = 0;
            btnConfirmDirect.Click += (s, e) => ExecuteDirectCancellation();

            btnSubmitRequest = new Button
            {
                Text = "Submit Cancellation Request",
                Size = new Size(190, 32),
                BackColor = Color.FromArgb(36, 137, 203),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnSubmitRequest.FlatAppearance.BorderSize = 0;
            btnSubmitRequest.Click += (s, e) => ExecuteSubmitRequest();

            btnViewRequest = new Button
            {
                Text = "View Existing Request...",
                Size = new Size(180, 32),
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnViewRequest.FlatAppearance.BorderSize = 0;
            btnViewRequest.Click += (s, e) => OpenExistingRequestReview();

            btnFlow.Controls.AddRange(new Control[] { btnClose, btnConfirmDirect, btnSubmitRequest, btnViewRequest });

            body.Controls.AddRange(new Control[]
            {
                lblCustomerInfo, lblVehicleInfo, lblFinancialInfo, eligibilityBanner,
                lblCategory, cmbReasonCategory, lblExp, lblExplanationHelp, txtExplanation,
                lblIncurredCharges, numIncurredCharges, lblProposedRefund, numProposedRefund,
                lblError, btnFlow
            });

            Controls.Add(body);
            Controls.Add(topPanel);
            CancelButton = btnClose;
        }

        private void RefreshEligibility()
        {
            using var db = factory();
            var targetJob = db.JobOrders.Include(j => j.Tasks).FirstOrDefault(j => j.JobOrderId == job.JobOrderId) ?? job;
            var customer = targetJob.CustomerId is int cid ? db.Customers.Find(cid) : null;
            eligibility = CancellationRefundService.EvaluateEligibility(db, targetJob);

            lblCustomerInfo.Text = $"Customer: {customer?.FullName ?? "Walk-in"}";
            lblVehicleInfo.Text = $"Vehicle: {targetJob.VehicleDisplay}";
            lblFinancialInfo.Text = $"Total Paid: ₱{eligibility.TotalPaid:N2}  |  Refunded: ₱{eligibility.TotalRefunded:N2}  |  Refundable: ₱{eligibility.RemainingRefundable:N2}";

            numProposedRefund.Maximum = eligibility.RemainingRefundable > 0 ? eligibility.RemainingRefundable : 1000000m;
            numProposedRefund.Value = eligibility.RemainingRefundable;

            if (eligibility.IsBlocked)
            {
                eligibilityBanner.BackColor = Color.FromArgb(254, 226, 226);
                lblEligibility.ForeColor = Color.FromArgb(153, 27, 27);
                lblEligibility.Text = "⛔ " + eligibility.StatusExplanation;
            }
            else if (eligibility.ExistingActiveRequest != null)
            {
                eligibilityBanner.BackColor = Color.FromArgb(254, 243, 199);
                lblEligibility.ForeColor = Color.FromArgb(146, 64, 14);
                lblEligibility.Text = "⏳ " + eligibility.StatusExplanation;
            }
            else if (eligibility.RequiresReview)
            {
                eligibilityBanner.BackColor = Color.FromArgb(238, 242, 255);
                lblEligibility.ForeColor = Color.FromArgb(55, 48, 163);
                lblEligibility.Text = "ℹ " + eligibility.StatusExplanation;
            }
            else
            {
                eligibilityBanner.BackColor = Color.FromArgb(236, 253, 245);
                lblEligibility.ForeColor = Color.FromArgb(6, 95, 70);
                lblEligibility.Text = "✔ " + eligibility.StatusExplanation;
            }

            UpdateContextualControls();
        }

        private void UpdateContextualControls()
        {
            if (eligibility == null) return;

            bool isOther = cmbReasonCategory.SelectedItem?.ToString() == "Other";
            lblExplanationHelp.Visible = isOther;

            if (eligibility.IsBlocked)
            {
                btnConfirmDirect.Visible = false;
                btnSubmitRequest.Visible = false;
                btnViewRequest.Visible = false;
                cmbReasonCategory.Enabled = false;
                txtExplanation.Enabled = false;
                numIncurredCharges.Enabled = false;
                numProposedRefund.Enabled = false;
                return;
            }

            if (eligibility.ExistingActiveRequest != null)
            {
                btnConfirmDirect.Visible = false;
                btnSubmitRequest.Visible = false;
                btnViewRequest.Visible = true;
                cmbReasonCategory.Enabled = false;
                txtExplanation.Enabled = false;
                numIncurredCharges.Enabled = false;
                numProposedRefund.Enabled = false;
                return;
            }

            btnViewRequest.Visible = false;

            if (eligibility.CanDirectCancel)
            {
                btnConfirmDirect.Visible = true;
                btnSubmitRequest.Visible = false;
                cmbReasonCategory.Enabled = true;
                txtExplanation.Enabled = true;
                numIncurredCharges.Visible = false;
                lblIncurredCharges.Visible = false;
                numProposedRefund.Visible = false;
                lblProposedRefund.Visible = false;

                bool reasonValid = !isOther || txtExplanation.Text.Trim().Length >= 4;
                btnConfirmDirect.Enabled = reasonValid;
            }
            else if (eligibility.RequiresReview)
            {
                btnConfirmDirect.Visible = false;
                btnSubmitRequest.Visible = true;
                cmbReasonCategory.Enabled = true;
                txtExplanation.Enabled = true;
                numIncurredCharges.Visible = true;
                lblIncurredCharges.Visible = true;
                numProposedRefund.Visible = true;
                lblProposedRefund.Visible = true;

                bool reasonValid = !isOther || txtExplanation.Text.Trim().Length >= 4;
                btnSubmitRequest.Enabled = reasonValid;
            }
        }

        private void ExecuteDirectCancellation()
        {
            string category = cmbReasonCategory.SelectedItem?.ToString() ?? "Customer requested cancellation";
            string explanation = txtExplanation.Text.Trim();

            try
            {
                using var db = factory();
                CancellationRefundService.DirectCancelUnpaidJob(db, job.JobOrderId, category, explanation, db.AuditActor);

                CancellationReason = category + (string.IsNullOrWhiteSpace(explanation) ? "" : ": " + explanation);
                MessageBox.Show(this,
                    $"{RecordReference.JobOrder(job.JobOrderId)} has been directly cancelled. Work stopped and active mechanic workload relieved.",
                    "Job Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
        }

        private void ExecuteSubmitRequest()
        {
            string category = cmbReasonCategory.SelectedItem?.ToString() ?? "Customer requested cancellation";
            string explanation = txtExplanation.Text.Trim();
            decimal incurred = numIncurredCharges.Value;
            decimal proposedRefund = numProposedRefund.Value;

            try
            {
                using var db = factory();
                var req = CancellationRefundService.SubmitCancellationRequest(
                    db,
                    job.JobOrderId,
                    category,
                    explanation,
                    db.AuditActor,
                    incurred,
                    proposedRefund
                );

                MessageBox.Show(this,
                    $"Cancellation request successfully submitted!\n\nReference: {RecordReference.CancellationRequest(req.RequestId)}\nStatus: {req.Status}\nTotal Paid: ₱{req.TotalPaidAtRequest:N2}\nProposed Refund: ₱{req.ProposedRefund:N2}\n\nManagement will review this request and settle any refund obligations before cancellation is finalized.",
                    "Cancellation Request Submitted",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
        }

        private void OpenExistingRequestReview()
        {
            if (eligibility?.ExistingActiveRequest == null) return;
            using var reviewDlg = new JobCancellationReviewDialogForm(eligibility.ExistingActiveRequest.RequestId, factory);
            reviewDlg.ShowDialog(this);
            RefreshEligibility();
        }
    }
}
