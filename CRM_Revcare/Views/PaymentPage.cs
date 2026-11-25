#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    public sealed class JobBalanceRow
    {
        public int JobOrderId { get; set; }
        public string Customer { get; set; } = string.Empty;
        public string Vehicle { get; set; } = string.Empty;
        public decimal? TotalEstimatedCost { get; set; }
        public string TotalEstimatedCostDisplay => TotalEstimatedCost.HasValue ? $"₱{TotalEstimatedCost.Value:N2}" : "Pending Estimate";
        public decimal TotalPaid { get; set; }
        public string TotalPaidDisplay => $"₱{TotalPaid:N2}";
        public decimal TotalRefunded { get; set; }
        public string TotalRefundedDisplay => TotalRefunded > 0 ? $"₱{TotalRefunded:N2}" : "—";
        public decimal NetCollections => Math.Max(0m, TotalPaid - TotalRefunded);
        public string NetCollectionsDisplay => $"₱{NetCollections:N2}";
        public decimal RemainingRefundable => Math.Max(0m, TotalPaid - TotalRefunded);
        public string RemainingRefundableDisplay => RemainingRefundable > 0 ? $"₱{RemainingRefundable:N2}" : "₱0.00";
        public decimal? OutstandingBalance { get; set; }
        public string OutstandingBalanceDisplay =>
            JobStatus == "Cancelled" ? "— [Cancelled]" :
            OutstandingBalance.HasValue ? $"₱{OutstandingBalance.Value:N2}" : "Pending Estimate";
        public string PaymentStatus { get; set; } = "Unpaid";
        public string JobStatus { get; set; } = "In Queue";
    }

    public partial class PaymentPage : TransactionPage
    {
        public PaymentPage()
        {
            InitializeComponent();
            amount.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                if (char.IsDigit(e.KeyChar)) return;
                if (e.KeyChar == '.' && !amount.Text.Contains('.')) return;
                e.Handled = true;
            };

            btnViewJobBalances.Click += (s, e) => SwitchToJobBalancesView();
            btnViewReceipts.Click += (s, e) => SwitchToReceiptsView();
            btnViewCancellationReviews.Click += (s, e) => SwitchToCancellationReviewsView();
            btnViewRefunds.Click += (s, e) => SwitchToRefundsView();
        }

        private void ResetNavButtons()
        {
            var inactiveBg = Color.FromArgb(241, 245, 249);
            var inactiveFg = Color.FromArgb(30, 41, 59);

            btnViewJobBalances.BackColor = inactiveBg;
            btnViewJobBalances.ForeColor = inactiveFg;
            btnViewJobBalances.UseVisualStyleBackColor = true;

            btnViewReceipts.BackColor = inactiveBg;
            btnViewReceipts.ForeColor = inactiveFg;
            btnViewReceipts.UseVisualStyleBackColor = true;

            btnViewCancellationReviews.BackColor = inactiveBg;
            btnViewCancellationReviews.ForeColor = inactiveFg;
            btnViewCancellationReviews.UseVisualStyleBackColor = true;

            btnViewRefunds.BackColor = inactiveBg;
            btnViewRefunds.ForeColor = inactiveFg;
            btnViewRefunds.UseVisualStyleBackColor = true;
        }

        private void HideAllPanels()
        {
            jobBalancesPanel.Visible = false;
            cancellationReviewsPanel.Visible = false;
            refundsPanel.Visible = false;
            recordsGrid.Visible = false;
            filterPanel.Visible = false;
            actionsPanel.Visible = false;
            resultLabel.Visible = false;
        }

        public void SwitchToJobBalancesView()
        {
            HideAllPanels();
            jobBalancesPanel.Visible = true;
            jobBalancesPanel.BringToFront();

            ResetNavButtons();
            btnViewJobBalances.BackColor = Color.FromArgb(37, 99, 235);
            btnViewJobBalances.ForeColor = Color.White;
            btnViewJobBalances.UseVisualStyleBackColor = false;
        }

        public void SwitchToReceiptsView()
        {
            HideAllPanels();
            recordsGrid.Visible = true;
            filterPanel.Visible = true;
            actionsPanel.Visible = true;
            resultLabel.Visible = true;
            recordsGrid.BringToFront();

            ResetNavButtons();
            btnViewReceipts.BackColor = Color.FromArgb(37, 99, 235);
            btnViewReceipts.ForeColor = Color.White;
            btnViewReceipts.UseVisualStyleBackColor = false;
        }

        public void SwitchToCancellationReviewsView()
        {
            HideAllPanels();
            cancellationReviewsPanel.Visible = true;
            cancellationReviewsPanel.BringToFront();

            ResetNavButtons();
            btnViewCancellationReviews.BackColor = Color.FromArgb(37, 99, 235);
            btnViewCancellationReviews.ForeColor = Color.White;
            btnViewCancellationReviews.UseVisualStyleBackColor = false;
        }

        public void SwitchToRefundsView()
        {
            HideAllPanels();
            refundsPanel.Visible = true;
            refundsPanel.BringToFront();

            ResetNavButtons();
            btnViewRefunds.BackColor = Color.FromArgb(37, 99, 235);
            btnViewRefunds.ForeColor = Color.White;
            btnViewRefunds.UseVisualStyleBackColor = false;
        }
    }
}
