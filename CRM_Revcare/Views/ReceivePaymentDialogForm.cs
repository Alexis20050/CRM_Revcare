#nullable enable
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public partial class ReceivePaymentDialogForm : Form
    {
        private readonly int jobOrderId;
        private readonly Func<MasterCrmDbContext> factory;
        private readonly Action onPaymentRecorded;
        private JobOrder? job;
        private Customer? customer;
        private decimal totalEstimatedCost;
        private decimal alreadyPaid;
        private decimal outstandingBalance;
        private decimal requiredDeposit;
        private decimal depositDue;
        private Guid paymentSubmissionId = Guid.NewGuid();
        private bool isSaving;
        private Payment? savedPayment;

        public ReceivePaymentDialogForm(int jobOrderId, Func<MasterCrmDbContext> factory, Action onPaymentRecorded)
        {
            this.jobOrderId = jobOrderId;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.onPaymentRecorded = onPaymentRecorded ?? throw new ArgumentNullException(nameof(onPaymentRecorded));

            InitializeComponent();

            amountBox.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                if (char.IsDigit(e.KeyChar)) return;
                if (e.KeyChar == '.' && !amountBox.Text.Contains('.')) return;
                e.Handled = true;
            };

            amountBox.Leave += (s, e) =>
            {
                if (decimal.TryParse(amountBox.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var v) ||
                    decimal.TryParse(amountBox.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out v))
                {
                    if (v > 0) amountBox.Text = v.ToString("N2");
                }
            };

            amountBox.TextChanged += (s, e) => UpdateLivePreview();
            methodBox.SelectedIndexChanged += (s, e) => UpdateLivePreview();
            categoryBox.SelectedIndexChanged += (s, e) => UpdateLivePreview();
            allowCreditCheck.CheckedChanged += (s, e) => UpdateLivePreview();

            btnDepositShortcut.Click += (s, e) =>
            {
                if (depositDue > 0)
                {
                    amountBox.Text = depositDue.ToString("F2");
                    amountBox.Focus();
                }
            };

            btnBalanceShortcut.Click += (s, e) =>
            {
                if (outstandingBalance > 0)
                {
                    amountBox.Text = outstandingBalance.ToString("F2");
                    amountBox.Focus();
                }
            };

            btnRecordPayment.Click += (s, e) => ExecuteRecordPayment();

            btnPreviewReceipt.Click += (s, e) =>
            {
                if (savedPayment != null && job != null)
                {
                    OpenReceipt(savedPayment, job, false);
                }
            };

            btnPrintReceipt.Click += (s, e) =>
            {
                if (savedPayment != null && job != null)
                {
                    OpenReceipt(savedPayment, job, true);
                }
            };

            btnCloseDialog.Click += (s, e) =>
            {
                DialogResult = DialogResult.OK;
                Close();
            };

            LoadJobDetails();
        }

        private void LoadJobDetails()
        {
            try
            {
                using var db = factory();
                job = db.JobOrders.Include(j => j.Payments).AsNoTracking().SingleOrDefault(j => j.JobOrderId == jobOrderId);

                if (job == null)
                {
                    MessageBox.Show(this, $"Job Order #{jobOrderId} was not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                if (job.CustomerId.HasValue)
                {
                    customer = db.Customers.AsNoTracking().SingleOrDefault(c => c.CustomerId == job.CustomerId.Value);
                }

                totalEstimatedCost = job.TotalEstimatedCost ?? 0;
                alreadyPaid = job.TotalPaid;
                outstandingBalance = Math.Max(0, totalEstimatedCost - alreadyPaid);
                requiredDeposit = totalEstimatedCost > 0 ? decimal.Round(totalEstimatedCost * 0.5m, 2, MidpointRounding.AwayFromZero) : 0;
                depositDue = Math.Max(0, requiredDeposit - alreadyPaid);

                lblJobHeader.Text = $"{RecordReference.JobOrder(job.JobOrderId)} ({job.EstimateNumber}) — Status: {JobOrderWorkflow.Normalize(job.Status)}";
                lblCustomerInfo.Text = $"Customer: {customer?.FullName ?? "Walk-in Customer"} ({customer?.ContactNumber ?? "No phone"})";
                lblVehicleInfo.Text = $"Vehicle: {job.VehicleDisplay}";

                lblTotalCost.Text = $"Approved Total: PHP {totalEstimatedCost:N2}";
                lblTotalPaid.Text = $"Already Paid: PHP {alreadyPaid:N2}";
                lblBalance.Text = $"Outstanding Balance: PHP {outstandingBalance:N2}";

                lblDepositRequired.Text = $"50% Deposit Requirement: PHP {requiredDeposit:N2}";
                lblDepositDue.Text = depositDue > 0
                    ? $"Still Needed for Deposit: PHP {depositDue:N2}"
                    : "✓ 50% Deposit Requirement Met";
                lblDepositDue.ForeColor = depositDue > 0 ? Color.FromArgb(30, 64, 175) : Color.FromArgb(22, 101, 52);

                btnDepositShortcut.Enabled = depositDue > 0;
                btnBalanceShortcut.Enabled = outstandingBalance > 0;

                methodBox.SelectedIndex = 0; // Cash default

                // Smart initial category selection
                if (alreadyPaid == 0 && depositDue > 0) categoryBox.SelectedItem = "Downpayment";
                else if (outstandingBalance > 0) categoryBox.SelectedItem = "Progress Payment";
                else categoryBox.SelectedItem = "Final Balance";

                UpdateLivePreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load billing details: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void UpdateLivePreview()
        {
            if (job == null) return;

            bool valid = decimal.TryParse(amountBox.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var amt)
                || decimal.TryParse(amountBox.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out amt);

            valid = valid && amt > 0 && decimal.Round(amt, 2) == amt;

            if (!valid)
            {
                lblPreviewAmount.Text = "Amount: Enter payment amount";
                lblPreviewNewBalance.Text = $"Balance after payment: PHP {outstandingBalance:N2}";
                lblPreviewNewBalance.ForeColor = Color.Black;
                lblPreviewPaymentStatus.Text = "Status: Pending valid input";
                lblPreviewDepositStatus.Text = depositDue > 0 ? $"Deposit: PHP {depositDue:N2} still needed" : "Deposit: Met";
                lblPreviewDepositStatus.ForeColor = depositDue > 0 ? Color.FromArgb(185, 28, 28) : Color.FromArgb(22, 101, 52);

                btnRecordPayment.Enabled = false;
                statusLabel.Text = "Enter amount actually received.";
                allowCreditCheck.Visible = false;
                return;
            }

            decimal newPaid = alreadyPaid + amt;
            decimal newBalance = Math.Max(0, totalEstimatedCost - newPaid);
            decimal excess = amt > outstandingBalance ? amt - outstandingBalance : 0;
            bool overpayment = excess > 0;

            lblPreviewAmount.Text = $"Amount to Record: PHP {amt:N2}";

            if (overpayment)
            {
                lblPreviewNewBalance.Text = $"Overpayment: Excess PHP {excess:N2} will become Customer Credit";
                lblPreviewNewBalance.ForeColor = Color.FromArgb(194, 65, 12);
                allowCreditCheck.Visible = true;
            }
            else
            {
                lblPreviewNewBalance.Text = $"Balance after payment: PHP {newBalance:N2}";
                lblPreviewNewBalance.ForeColor = newBalance == 0 ? Color.FromArgb(22, 101, 52) : Color.Black;
                allowCreditCheck.Visible = false;
            }

            string resultStatus = newBalance == 0 ? (excess > 0 ? "Fully Paid (with Customer Credit)" : "Fully Paid") : "Partially Paid";
            lblPreviewPaymentStatus.Text = $"Resulting Status: {resultStatus}";

            bool depositMet = newPaid >= requiredDeposit;
            if (depositMet)
            {
                lblPreviewDepositStatus.Text = "✓ 50% Deposit Requirement MET";
                lblPreviewDepositStatus.ForeColor = Color.FromArgb(22, 101, 52);
            }
            else
            {
                decimal rem = requiredDeposit - newPaid;
                lblPreviewDepositStatus.Text = $"⚠️ Deposit: PHP {rem:N2} still required to start work";
                lblPreviewDepositStatus.ForeColor = Color.FromArgb(185, 28, 28);
            }

            // Auto-select category based on payment size
            if (alreadyPaid == 0 && amt <= requiredDeposit)
            {
                categoryBox.SelectedItem = "Downpayment";
            }
            else if (newBalance == 0)
            {
                categoryBox.SelectedItem = "Final Balance";
            }
            else
            {
                categoryBox.SelectedItem = "Progress Payment";
            }

            bool blockedOverpayment = overpayment && !allowCreditCheck.Checked;
            btnRecordPayment.Enabled = !isSaving && valid && !blockedOverpayment;

            if (blockedOverpayment)
            {
                statusLabel.Text = $"Payment exceeds balance by PHP {excess:N2}. Check credit permission to allow.";
            }
            else
            {
                statusLabel.Text = "";
            }
        }

        private void ExecuteRecordPayment()
        {
            if (isSaving || job == null) return;

            if (!decimal.TryParse(amountBox.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var amt) &&
                !decimal.TryParse(amountBox.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out amt))
            {
                MessageBox.Show(this, "Enter a valid payment amount.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (amt <= 0 || decimal.Round(amt, 2) != amt)
            {
                MessageBox.Show(this, "Payment amount must be positive with at most two decimal places.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string method = methodBox.SelectedItem?.ToString() ?? "Cash";
            string category = categoryBox.SelectedItem?.ToString() ?? "Customer payment";
            bool credit = allowCreditCheck.Checked;

            decimal excess = amt > outstandingBalance ? amt - outstandingBalance : 0;
            if (excess > 0 && !credit)
            {
                MessageBox.Show(this, $"This payment would exceed the balance by PHP {excess:N2}. Explicitly allow customer credit to proceed.", "Overpayment Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string confirmMsg = $"Confirm recording payment of PHP {amt:N2} via {method} for {RecordReference.JobOrder(job.JobOrderId)}?\n\nCustomer: {customer?.FullName ?? "Walk-in"}\nVehicle: {job.VehicleDisplay}\nCategory: {category}";
            if (MessageBox.Show(this, confirmMsg, "Confirm Payment", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            isSaving = true;
            btnRecordPayment.Enabled = false;
            statusLabel.Text = "Recording payment transaction...";

            try
            {
                using var db = factory();

                // Idempotency check: if this submission was already recorded (e.g. timeout / retry), reuse it
                var existing = db.Payments.SingleOrDefault(p => p.SubmissionId == paymentSubmissionId);
                Payment newPayment;
                if (existing != null)
                {
                    newPayment = existing;
                }
                else
                {
                    newPayment = new Payment
                    {
                        JobOrderId = job.JobOrderId,
                        AmountPaid = amt,
                        PaymentMethod = method,
                        PaymentType = category,
                        AllowCustomerCredit = credit,
                        CreatedAt = DateTime.UtcNow,
                        SubmissionId = paymentSubmissionId
                    };

                    db.Payments.Add(newPayment);
                    db.SaveChanges();
                }

                savedPayment = newPayment;
                onPaymentRecorded();

                // Switch to success UI
                inputsPanel.Visible = false;
                livePreviewPanel.Visible = false;
                actionsPanel.Visible = false;

                successReceiptRef.Text = $"Receipt Reference: {newPayment.ReceiptReference}\nAmount: PHP {newPayment.AmountPaid:N2} | Method: {newPayment.PaymentMethod}\nRecorded: {newPayment.CreatedAt:dd MMM yyyy HH:mm} UTC";
                successPanel.Visible = true;
            }
            catch (Exception ex)
            {
                // In case of error/timeout, check if the record was actually committed before claiming definite failure
                try
                {
                    using var verifyDb = factory();
                    var confirmed = verifyDb.Payments.SingleOrDefault(p => p.SubmissionId == paymentSubmissionId);
                    if (confirmed != null)
                    {
                        savedPayment = confirmed;
                        onPaymentRecorded();
                        inputsPanel.Visible = false;
                        livePreviewPanel.Visible = false;
                        actionsPanel.Visible = false;
                        successReceiptRef.Text = $"Receipt Reference: {confirmed.ReceiptReference}\nAmount: PHP {confirmed.AmountPaid:N2} | Method: {confirmed.PaymentMethod}\nRecorded: {confirmed.CreatedAt:dd MMM yyyy HH:mm} UTC";
                        successPanel.Visible = true;
                        return;
                    }
                }
                catch { }

                isSaving = false;
                btnRecordPayment.Enabled = true;
                statusLabel.Text = "Error saving payment.";
                MessageBox.Show(this, "Payment could not be recorded: " + ex.Message, "Transaction Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenReceipt(Payment p, JobOrder j, bool printImmediately)
        {
            try
            {
                using var db = factory();
                string cashier = db.AuditActor;
                var dbJob = db.JobOrders.Include(x => x.Payments).AsNoTracking().SingleOrDefault(x => x.JobOrderId == j.JobOrderId) ?? j;
                decimal validPaid = dbJob.Payments.Where(x => !x.IsVoided).Sum(x => x.AmountPaid);
                decimal? balance = dbJob.TotalEstimatedCost.HasValue ? Math.Max(0, dbJob.TotalEstimatedCost.Value - validPaid) : null;
                var receiptData = new PaymentReceiptData
                {
                    PaymentId = p.PaymentId,
                    PaymentDate = p.CreatedAt,
                    CustomerName = customer?.FullName ?? "Walk-in Customer",
                    JobOrderId = j.JobOrderId,
                    VehicleDetails = j.VehicleDisplay,
                    PaymentType = p.PaymentType,
                    PaymentMethod = p.PaymentMethod,
                    AmountPaid = p.AmountPaid,
                    TotalEstimatedCost = dbJob.TotalEstimatedCost,
                    TotalPaidValid = validPaid,
                    OutstandingBalance = balance,
                    RecordedBy = cashier,
                    IsVoided = p.IsVoided,
                    VoidReason = p.VoidReason,
                    VoidedAtUtc = p.VoidedAtUtc
                };

                var business = LegacyBusinessConfig.Load();
                string html = ReportTemplateBuilder.BuildReceiptHtml(receiptData, business);
                ReportPreviewDialogForm.ShowPreview(this, $"Official Receipt - {p.ReceiptReference}", html);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not generate receipt: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
