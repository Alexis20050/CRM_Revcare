#nullable enable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;

namespace CRM_Revcare_
{
    public sealed class RecordRefundDialogForm : Form
    {
        private readonly int jobOrderId;
        private readonly int? cancellationRequestId;
        private readonly Payment? targetPayment;
        private readonly Customer? customer;
        private readonly Func<MasterCrmDbContext> factory;

        private Label lblHeaderInfo;
        private NumericUpDown numAmount;
        private ComboBox cmbMethod;
        private TextBox txtRecipient;
        private TextBox txtReason;
        private TextBox txtAuthorizer;
        private Label lblError;
        private Button btnConfirm;
        private Button btnCancel;

        public Refund? RecordedRefund { get; private set; }

        public RecordRefundDialogForm(
            int jobOrderId,
            int? cancellationRequestId,
            Payment? targetPayment,
            Customer? customer,
            Func<MasterCrmDbContext> factory)
        {
            this.jobOrderId = jobOrderId;
            this.cancellationRequestId = cancellationRequestId;
            this.targetPayment = targetPayment;
            this.customer = customer;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

            InitializeUi();
        }

        private void InitializeUi()
        {
            Text = $"Record Customer Refund — {RecordReference.JobOrder(jobOrderId)}";
            Size = new Size(540, 520);
            MinimumSize = new Size(500, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            decimal remainingRefundable;
            using (var db = factory())
            {
                remainingRefundable = targetPayment != null
                    ? CancellationRefundService.GetPaymentRemainingRefundable(db, targetPayment.PaymentId)
                    : CancellationRefundService.GetRemainingRefundable(db, jobOrderId);
            }

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(246, 248, 250),
                Padding = new Padding(16, 10, 16, 10)
            };
            var title = new Label
            {
                Text = "Record Customer Refund",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(14, 8),
                AutoSize = true
            };
            string subText = targetPayment != null
                ? $"Original Receipt: {RecordReference.Receipt(targetPayment.PaymentId)} (Paid: ₱{targetPayment.AmountPaid:N2}) — Max Refundable: ₱{remainingRefundable:N2}"
                : $"Job Order: {RecordReference.JobOrder(jobOrderId)} — Max Refundable: ₱{remainingRefundable:N2}";
            var subtitle = new Label
            {
                Text = subText,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.DimGray,
                Location = new Point(15, 34),
                AutoSize = true
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            int y = 15;
            var lblAmount = new Label { Text = "Refund Amount (PHP):", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            numAmount = new NumericUpDown
            {
                Location = new Point(20, y + 22),
                Size = new Size(220, 24),
                DecimalPlaces = 2,
                Maximum = remainingRefundable > 0 ? remainingRefundable : 1000000m,
                Value = remainingRefundable > 0 ? remainingRefundable : 0m,
                Font = new Font("Segoe UI", 9.5F)
            };

            var lblMethod = new Label { Text = "Refund Payment Method:", Location = new Point(260, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            cmbMethod = new ComboBox
            {
                Location = new Point(260, y + 22),
                Size = new Size(230, 24),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            cmbMethod.Items.AddRange(CancellationRefundService.RefundMethods);
            cmbMethod.SelectedIndex = 0;

            y += 60;
            var lblRecipient = new Label { Text = "Recipient Name (Verified Payer):", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtRecipient = new TextBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(470, 24),
                Text = customer?.FullName ?? "Original Payer",
                Font = new Font("Segoe UI", 9.5F)
            };

            y += 60;
            var lblReason = new Label { Text = "Refund Reason / Justification (Required):", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtReason = new TextBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(470, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "Refund of deposit following approved cancellation review.",
                Font = new Font("Segoe UI", 9.5F)
            };

            y += 95;
            var lblAuth = new Label { Text = "Authorized Manager / Supervisor (Required):", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtAuthorizer = new TextBox
            {
                Location = new Point(20, y + 22),
                Size = new Size(470, 24),
                PlaceholderText = "Enter active Manager or Workshop Owner name...",
                Font = new Font("Segoe UI", 9.5F)
            };

            // Prefill authorizer from DB if manager staff exists
            using (var db = factory())
            {
                var mgr = db.Staff.FirstOrDefault(s => s.IsActive && (s.Role.Contains("Manager") || s.Role.Contains("Owner")));
                if (mgr != null) txtAuthorizer.Text = mgr.DisplayName;
            }

            y += 60;
            lblError = new Label
            {
                Location = new Point(20, y),
                Size = new Size(470, 36),
                ForeColor = Color.Firebrick,
                Font = new Font("Segoe UI", 8.5F),
                Text = ""
            };

            y += 40;
            btnConfirm = new Button
            {
                Text = "Record Refund",
                Location = new Point(330, y),
                Size = new Size(160, 32),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 164, 79),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += (s, e) => ExecuteRefund();

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(230, y),
                Size = new Size(90, 32),
                DialogResult = DialogResult.Cancel,
                Font = new Font("Segoe UI", 9F)
            };

            body.Controls.AddRange(new Control[]
            {
                lblAmount, numAmount, lblMethod, cmbMethod,
                lblRecipient, txtRecipient, lblReason, txtReason,
                lblAuth, txtAuthorizer, lblError, btnConfirm, btnCancel
            });

            Controls.Add(body);
            Controls.Add(header);
            CancelButton = btnCancel;
        }

        private void ExecuteRefund()
        {
            decimal amount = numAmount.Value;
            string method = cmbMethod.SelectedItem?.ToString() ?? "Cash";
            string recipient = txtRecipient.Text.Trim();
            string reason = txtReason.Text.Trim();
            string authorizer = txtAuthorizer.Text.Trim();

            if (amount <= 0)
            {
                lblError.Text = "Please enter a positive refund amount.";
                return;
            }

            if (string.IsNullOrWhiteSpace(recipient))
            {
                lblError.Text = "Recipient name is required.";
                txtRecipient.Focus();
                return;
            }

            if (reason.Length < 4)
            {
                lblError.Text = "A detailed refund reason of at least 4 characters is required.";
                txtReason.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(authorizer))
            {
                lblError.Text = "Authorized manager identity is required.";
                txtAuthorizer.Focus();
                return;
            }

            try
            {
                using var db = factory();
                RecordedRefund = CancellationRefundService.RecordRefund(
                    db,
                    jobOrderId,
                    cancellationRequestId,
                    targetPayment?.PaymentId,
                    amount,
                    method,
                    recipient,
                    reason,
                    authorizer
                );

                MessageBox.Show(this,
                    $"Refund successfully processed!\n\nReference: {RecordedRefund.RefundReference}\nAmount: ₱{RecordedRefund.Amount:N2}\nRecipient: {RecordedRefund.RecipientName}\nMethod: {RecordedRefund.RefundMethod}",
                    "Refund Recorded",
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
    }
}
