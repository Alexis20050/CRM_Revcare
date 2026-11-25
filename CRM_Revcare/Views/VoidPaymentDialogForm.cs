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
    public partial class VoidPaymentDialogForm : Form
    {
        private readonly Payment payment;
        private readonly JobOrder? job;
        private readonly Customer? customer;
        private readonly Func<MasterCrmDbContext> factory;
        private readonly Action onVoided;
        private decimal currentBalance;
        private decimal balanceAfterVoid;
        private bool isSaving;

        public VoidPaymentDialogForm(Payment payment, JobOrder? job, Customer? customer, Func<MasterCrmDbContext> factory, Action onVoided)
        {
            this.payment = payment ?? throw new ArgumentNullException(nameof(payment));
            this.job = job;
            this.customer = customer;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.onVoided = onVoided ?? throw new ArgumentNullException(nameof(onVoided));

            InitializeComponent();

            SetupDetails();

            reasonBox.TextChanged += (s, e) => ValidateInputs();
            btnConfirmVoid.Click += (s, e) => ExecuteVoid();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        private void SetupDetails()
        {
            string receiptRef = RecordReference.Receipt(payment.PaymentId);
            lblReceiptHeader.Text = $"Receipt Reference: {receiptRef} (ID: {payment.PaymentId})";
            lblCustomerInfo.Text = $"Customer: {customer?.FullName ?? "Unknown Customer"}";
            lblVehicleInfo.Text = $"Vehicle: {job?.VehicleDisplay ?? "N/A"}";
            lblAmount.Text = $"Amount Received: PHP {payment.AmountPaid:N2}";
            lblMethod.Text = $"Method: {payment.PaymentMethod ?? "Cash"} ({payment.PaymentType ?? "Payment"})";
            lblDate.Text = $"Date: {payment.CreatedAt:yyyy-MM-dd HH:mm}";

            try
            {
                using var db = factory();
                if (payment.JobOrderId > 0)
                {
                    var freshJob = db.JobOrders.AsNoTracking().FirstOrDefault(j => j.JobOrderId == payment.JobOrderId);
                    if (freshJob?.TotalEstimatedCost != null)
                    {
                        var payments = db.Payments.AsNoTracking().Where(p => p.JobOrderId == payment.JobOrderId).ToList();
                        decimal validPaid = payments.Where(p => !p.IsVoided).Sum(p => p.AmountPaid);
                        currentBalance = Math.Max(0, freshJob.TotalEstimatedCost.Value - validPaid);
                        // If this payment was part of validPaid, voiding it increases balance by its amount
                        decimal paidWithoutThis = payments.Where(p => !p.IsVoided && p.PaymentId != payment.PaymentId).Sum(p => p.AmountPaid);
                        balanceAfterVoid = Math.Max(0, freshJob.TotalEstimatedCost.Value - paidWithoutThis);
                    }
                    else
                    {
                        currentBalance = 0;
                        balanceAfterVoid = payment.AmountPaid;
                    }
                }
            }
            catch
            {
                currentBalance = 0;
                balanceAfterVoid = payment.AmountPaid;
            }

            lblCurrentBalance.Text = $"Current Job Balance: PHP {currentBalance:N2}";
            lblBalanceAfterVoid.Text = $"Balance After Void: PHP {balanceAfterVoid:N2}";

            if (payment.IsVoided)
            {
                titleLabel.Text = "Receipt Already Voided";
                statusLabel.Text = "This receipt has already been voided and cannot be voided again.";
                btnConfirmVoid.Enabled = false;
                reasonBox.Text = payment.VoidReason ?? "Already voided";
                reasonBox.ReadOnly = true;
            }
            else if (job?.Status == "Released" && balanceAfterVoid > 0)
            {
                statusLabel.Text = $"Cannot void payment: {RecordReference.JobOrder(job.JobOrderId)} is Released and voiding would leave an unpaid balance of PHP {balanceAfterVoid:N2}.";
                btnConfirmVoid.Enabled = false;
            }
            else
            {
                ValidateInputs();
            }
        }

        private void ValidateInputs()
        {
            if (payment.IsVoided)
            {
                btnConfirmVoid.Enabled = false;
                return;
            }

            if (job?.Status == "Released" && balanceAfterVoid > 0)
            {
                btnConfirmVoid.Enabled = false;
                return;
            }

            string reason = reasonBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason))
            {
                statusLabel.Text = "A mandatory void reason is required.";
                btnConfirmVoid.Enabled = false;
            }
            else if (reason.Length < 4)
            {
                statusLabel.Text = "Please provide a meaningful void reason (minimum 4 characters).";
                btnConfirmVoid.Enabled = false;
            }
            else
            {
                statusLabel.Text = "";
                btnConfirmVoid.Enabled = !isSaving;
            }
        }

        private void ExecuteVoid()
        {
            if (isSaving || payment.IsVoided) return;

            string reason = reasonBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 4)
            {
                MessageBox.Show(this, "Please provide a meaningful void reason before confirming.", "Reason Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                reasonBox.Focus();
                return;
            }

            var confirmResult = MessageBox.Show(this,
                $"Are you sure you want to void Receipt {Payment.FormatReceiptReference(payment.PaymentId)} for PHP {payment.AmountPaid:N2}?\n\n" +
                $"Reason: {reason}\n" +
                $"Resulting Job Balance: PHP {balanceAfterVoid:N2}\n\n" +
                "This action cannot be undone. The receipt will remain in audit history.",
                "Confirm Void Receipt",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmResult != DialogResult.Yes) return;

            isSaving = true;
            btnConfirmVoid.Enabled = false;
            btnCancel.Enabled = false;
            statusLabel.ForeColor = Color.Black;
            statusLabel.Text = "Recording void...";

            try
            {
                using var db = factory();
                var dbPayment = db.Payments.FirstOrDefault(p => p.PaymentId == payment.PaymentId);
                if (dbPayment == null)
                {
                    throw new InvalidOperationException("Payment record was not found in the database.");
                }

                if (dbPayment.IsVoided)
                {
                    throw new InvalidOperationException("This receipt has already been voided by another user.");
                }

                // Check concurrency or job released rules
                var dbJob = db.JobOrders.FirstOrDefault(j => j.JobOrderId == dbPayment.JobOrderId);
                if (dbJob?.Status == "Released")
                {
                    decimal otherPaid = db.Payments.Where(p => p.JobOrderId == dbJob.JobOrderId && !p.IsVoided && p.PaymentId != dbPayment.PaymentId).Sum(p => (decimal?)p.AmountPaid) ?? 0;
                    if (dbJob.TotalEstimatedCost.HasValue && otherPaid < dbJob.TotalEstimatedCost.Value)
                    {
                        throw new InvalidOperationException("Cannot void payment because the job is Released and voiding would leave an unpaid balance.");
                    }
                }

                dbPayment.IsVoided = true;
                dbPayment.VoidReason = reason;
                dbPayment.VoidedAtUtc = DateTime.UtcNow;

                db.SaveChanges();

                payment.IsVoided = true;
                payment.VoidReason = reason;
                payment.VoidedAtUtc = dbPayment.VoidedAtUtc;

                MessageBox.Show(this,
                    $"Receipt {Payment.FormatReceiptReference(payment.PaymentId)} has been successfully voided.\n\nUpdated job balance: PHP {balanceAfterVoid:N2}.",
                    "Payment Voided",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                onVoided();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                isSaving = false;
                btnConfirmVoid.Enabled = true;
                btnCancel.Enabled = true;
                statusLabel.ForeColor = Color.Firebrick;
                statusLabel.Text = ex.Message;
                MessageBox.Show(this, "Failed to void payment:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
