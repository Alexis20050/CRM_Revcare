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
    public sealed class JobCancellationReviewDialogForm : Form
    {
        private readonly int requestId;
        private readonly Func<MasterCrmDbContext> factory;

        private Label lblRequestHeader;
        private Label lblReasonDetails;
        private Label lblFinancialSummary;
        private DataGridView paymentsGrid;
        private DataGridView tasksGrid;
        private DataGridView refundsGrid;
        private NumericUpDown numApprovedRefund;
        private TextBox txtJustification;
        private TextBox txtReviewer;
        private Label lblStatusMessage;

        private Button btnApprove;
        private Button btnReject;
        private Button btnRequestInfo;
        private Button btnWithdraw;
        private Button btnRecordRefund;
        private Button btnClose;

        public JobCancellationReviewDialogForm(int requestId, Func<MasterCrmDbContext> factory)
        {
            this.requestId = requestId;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

            InitializeUi();
            LoadRequestData();
        }

        private void InitializeUi()
        {
            Text = $"Management Cancellation & Refund Review — {RecordReference.CancellationRequest(requestId)}";
            Size = new Size(820, 680);
            MinimumSize = new Size(760, 600);
            StartPosition = FormStartPosition.CenterParent;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(246, 248, 250),
                Padding = new Padding(16, 10, 16, 10)
            };
            var title = new Label
            {
                Text = "Management Cancellation & Financial Review",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(14, 8),
                AutoSize = true
            };
            lblRequestHeader = new Label
            {
                Text = "Loading request details...",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.DimGray,
                Location = new Point(15, 34),
                AutoSize = true
            };
            topPanel.Controls.Add(title);
            topPanel.Controls.Add(lblRequestHeader);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(16)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));  // Reason & status
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Financial summary
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));   // Grids (payments & tasks)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130)); // Decision input
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));  // Buttons

            // Row 0: Reason details
            var reasonPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(254, 249, 195), Padding = new Padding(10) };
            lblReasonDetails = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F) };
            reasonPanel.Controls.Add(lblReasonDetails);
            mainLayout.Controls.Add(reasonPanel, 0, 0);

            // Row 1: Financial summary
            var finPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(10) };
            lblFinancialSummary = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) };
            finPanel.Controls.Add(lblFinancialSummary);
            mainLayout.Controls.Add(finPanel, 0, 1);

            // Row 2: Tabs for Payments, Tasks, and Refunds
            var tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F) };

            // Payments Tab
            var tabPayments = new TabPage("Original Receipts / Payments");
            paymentsGrid = CreateGrid();
            paymentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Receipt #", Width = 90 });
            paymentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date", Width = 110 });
            paymentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category", Width = 90 });
            paymentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Method", Width = 80 });
            paymentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Amount Paid", Width = 100 });
            paymentsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Refundable", Width = 100 });
            tabPayments.Controls.Add(paymentsGrid);
            tabControl.TabPages.Add(tabPayments);

            // Tasks Tab
            var tabTasks = new TabPage("Workshop Tasks & Work Completed");
            tasksGrid = CreateGrid();
            tasksGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Service", Width = 220 });
            tasksGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", Width = 100 });
            tasksGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Remarks / Work Notes", Width = 250 });
            tabTasks.Controls.Add(tasksGrid);
            tabControl.TabPages.Add(tabTasks);

            // Refunds Tab
            var tabRefunds = new TabPage("Recorded Refunds");
            refundsGrid = CreateGrid();
            refundsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Refund #", Width = 100 });
            refundsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date", Width = 110 });
            refundsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Amount", Width = 100 });
            refundsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Method", Width = 90 });
            refundsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Recipient", Width = 150 });
            refundsGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Authorized By", Width = 120 });
            tabRefunds.Controls.Add(refundsGrid);
            tabControl.TabPages.Add(tabRefunds);

            mainLayout.Controls.Add(tabControl, 0, 2);

            // Row 3: Decision controls
            var decisionGroup = new GroupBox { Text = "Management Review Decision & Authorization", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var lblAprRefund = new Label { Text = "Approved Refund (PHP):", Location = new Point(15, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular) };
            numApprovedRefund = new NumericUpDown { Location = new Point(15, 45), Size = new Size(160, 23), DecimalPlaces = 2, Maximum = 1000000m, Font = new Font("Segoe UI", 9F) };

            var lblRev = new Label { Text = "Authorized Manager / Reviewer:", Location = new Point(190, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular) };
            txtReviewer = new TextBox { Location = new Point(190, 45), Size = new Size(200, 23), Font = new Font("Segoe UI", 9F), PlaceholderText = "Active Manager/Owner name..." };

            var lblJust = new Label { Text = "Review Justification / Terms (Required):", Location = new Point(410, 25), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Regular) };
            txtJustification = new TextBox { Location = new Point(410, 45), Size = new Size(350, 50), Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 9F) };

            lblStatusMessage = new Label { Location = new Point(15, 75), Size = new Size(380, 40), Font = new Font("Segoe UI", 8.5F), ForeColor = Color.Firebrick };

            decisionGroup.Controls.AddRange(new Control[] { lblAprRefund, numApprovedRefund, lblRev, txtReviewer, lblJust, txtJustification, lblStatusMessage });
            mainLayout.Controls.Add(decisionGroup, 0, 3);

            // Row 4: Buttons
            var btnFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 5, 0, 0) };
            btnClose = new Button { Text = "Close", Size = new Size(80, 32), DialogResult = DialogResult.OK };
            btnRecordRefund = new Button { Text = "Record Refund...", Size = new Size(130, 32), BackColor = Color.FromArgb(46, 164, 79), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnRecordRefund.FlatAppearance.BorderSize = 0;
            btnRecordRefund.Click += (s, e) => OpenRecordRefund();

            btnWithdraw = new Button { Text = "Withdraw", Size = new Size(90, 32) };
            btnWithdraw.Click += (s, e) => ProcessDecision("Withdraw");

            btnRequestInfo = new Button { Text = "Need Info", Size = new Size(90, 32) };
            btnRequestInfo.Click += (s, e) => ProcessDecision("Requires Information");

            btnReject = new Button { Text = "Reject", Size = new Size(90, 32), BackColor = Color.FromArgb(215, 58, 73), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnReject.FlatAppearance.BorderSize = 0;
            btnReject.Click += (s, e) => ProcessDecision("Reject");

            btnApprove = new Button { Text = "Approve Request", Size = new Size(130, 32), BackColor = Color.FromArgb(36, 137, 203), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnApprove.FlatAppearance.BorderSize = 0;
            btnApprove.Click += (s, e) => ProcessDecision("Approve");

            btnFlow.Controls.AddRange(new Control[] { btnClose, btnRecordRefund, btnWithdraw, btnRequestInfo, btnReject, btnApprove });
            mainLayout.Controls.Add(btnFlow, 0, 4);

            Controls.Add(mainLayout);
            Controls.Add(topPanel);
        }

        private static DataGridView CreateGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            GridRowNumbering.Enable(g);
            return g;
        }

        private void LoadRequestData()
        {
            try
            {
                using var db = factory();
                var req = db.JobCancellationRequests
                    .Include(r => r.JobOrder)
                    .Include(r => r.JobOrder.Tasks)
                    .Include(r => r.Refunds)
                    .FirstOrDefault(r => r.RequestId == requestId);

                if (req == null)
                {
                    MessageBox.Show(this, "Cancellation request not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                var job = req.JobOrder;
                var cust = job?.CustomerId is int cid ? db.Customers.Find(cid) : null;
                decimal totalPaid = CancellationRefundService.GetTotalPaid(db, req.JobOrderId);
                decimal totalRefunded = CancellationRefundService.GetTotalRefunded(db, req.JobOrderId);
                decimal remainingRefundable = Math.Max(0m, totalPaid - totalRefunded);

                lblRequestHeader.Text = $"{RecordReference.CancellationRequest(req.RequestId)} for {RecordReference.JobOrder(req.JobOrderId)} | Status: [{req.Status}] | Customer: {cust?.FullName ?? "Walk-in"} | Vehicle: {job?.VehicleDisplay}";
                lblReasonDetails.Text = $"Reason Category: {req.ReasonCategory}\nExplanation: {(string.IsNullOrWhiteSpace(req.ReasonExplanation) ? "(None provided)" : req.ReasonExplanation)}\nRequested By: {req.RequestedBy} on {req.RequestedAtUtc:yyyy-MM-dd hh:mm tt} UTC";

                lblFinancialSummary.Text = $"Total Paid: ₱{totalPaid:N2}  |  Incurred Charges: ₱{req.IncurredCharges:N2}  |  Previously Refunded: ₱{totalRefunded:N2}  |  Remaining Refundable: ₱{remainingRefundable:N2}  |  Proposed Refund: ₱{req.ProposedRefund:N2}";

                numApprovedRefund.Maximum = remainingRefundable > 0 ? remainingRefundable : 1000000m;
                numApprovedRefund.Value = req.ApprovedRefund ?? Math.Min(req.ProposedRefund, remainingRefundable);
                txtJustification.Text = req.ReviewJustification ?? "";

                if (string.IsNullOrWhiteSpace(txtReviewer.Text))
                {
                    var mgr = db.Staff.FirstOrDefault(s => s.IsActive && (s.Role.Contains("Manager") || s.Role.Contains("Owner")));
                    if (mgr != null) txtReviewer.Text = mgr.DisplayName;
                }

                // Load payments
                var payments = db.Payments.Where(p => p.JobOrderId == req.JobOrderId && !p.IsVoided).OrderBy(p => p.CreatedAt).ToList();
                paymentsGrid.Rows.Clear();
                foreach (var p in payments)
                {
                    decimal pRemaining = CancellationRefundService.GetPaymentRemainingRefundable(db, p.PaymentId);
                    paymentsGrid.Rows.Add(RecordReference.Receipt(p.PaymentId), p.CreatedAt.ToString("yyyy-MM-dd HH:mm"), p.PaymentType, p.PaymentMethod, $"₱{p.AmountPaid:N2}", $"₱{pRemaining:N2}");
                }

                // Load tasks
                var tasks = db.JobTasks.Where(t => t.JobOrderId == req.JobOrderId).ToList();
                tasksGrid.Rows.Clear();
                foreach (var t in tasks)
                {
                    tasksGrid.Rows.Add(t.ServiceId, t.Status, t.Remarks);
                }

                // Load refunds
                var refunds = db.Refunds.Where(r => r.JobOrderId == req.JobOrderId).OrderBy(r => r.CreatedAtUtc).ToList();
                refundsGrid.Rows.Clear();
                foreach (var r in refunds)
                {
                    refundsGrid.Rows.Add(RecordReference.Refund(r.RefundId), r.RefundDateUtc.ToString("yyyy-MM-dd"), $"₱{r.Amount:N2}", r.RefundMethod, r.RecipientName, r.AuthorizedBy);
                }

                // Update button enablement based on request state
                bool isPending = req.Status is "Pending Review" or "Requires Information";
                bool isApproved = req.Status is "Approved" or "Pending Refund";
                bool isTerminal = req.Status is "Completed" or "Rejected" or "Withdrawn";

                btnApprove.Enabled = isPending;
                btnReject.Enabled = isPending;
                btnRequestInfo.Enabled = isPending;
                btnWithdraw.Enabled = isPending || isApproved;
                btnRecordRefund.Enabled = remainingRefundable > 0 && (isApproved || isPending);

                lblStatusMessage.Text = isTerminal
                    ? $"This request was finalized as [{req.Status}] by {req.ReviewedBy} on {req.ReviewedAtUtc:yyyy-MM-dd}."
                    : (isApproved ? "Request is APPROVED. Record refunds to settle financial obligations and complete cancellation." : "");
            }
            catch (Exception ex)
            {
                lblStatusMessage.Text = "Error loading request: " + ex.Message;
            }
        }

        private void ProcessDecision(string decision)
        {
            string justification = txtJustification.Text.Trim();
            string reviewer = txtReviewer.Text.Trim();
            decimal approvedRefund = numApprovedRefund.Value;

            if (justification.Length < 4)
            {
                MessageBox.Show(this, "Please enter a decision justification of at least 4 characters.", "Justification Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtJustification.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(reviewer))
            {
                MessageBox.Show(this, "Please enter an authorized manager or reviewer name.", "Reviewer Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtReviewer.Focus();
                return;
            }

            try
            {
                using var db = factory();
                CancellationRefundService.ReviewCancellationRequest(
                    db,
                    requestId,
                    decision,
                    justification,
                    reviewer,
                    approvedRefund
                );

                MessageBox.Show(this, $"Request successfully updated to: {decision}.", "Review Decision Recorded", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadRequestData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Review Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenRecordRefund()
        {
            using var db = factory();
            var req = db.JobCancellationRequests.Include(r => r.JobOrder).FirstOrDefault(r => r.RequestId == requestId);
            if (req == null) return;

            var cust = req.JobOrder?.CustomerId is int cid ? db.Customers.Find(cid) : null;

            // Pick first payment with remaining refundable balance, if any
            var payment = db.Payments.Where(p => p.JobOrderId == req.JobOrderId && !p.IsVoided).ToList()
                .FirstOrDefault(p => CancellationRefundService.GetPaymentRemainingRefundable(db, p.PaymentId) > 0);

            using var dlg = new RecordRefundDialogForm(req.JobOrderId, req.RequestId, payment, cust, factory);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                LoadRequestData();
            }
        }
    }
}
