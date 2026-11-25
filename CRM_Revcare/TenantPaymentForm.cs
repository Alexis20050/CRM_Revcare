namespace CRM_Revcare_;

public partial class TenantPaymentForm : TenantEntryForm
{
    public TenantPaymentForm()
    {
        InitializeComponent();
        amountPaidInput.KeyPress += (s, e) =>
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsDigit(e.KeyChar)) return;
            if (e.KeyChar == '.' && !amountPaidInput.Text.Contains('.')) return;
            e.Handled = true;
        };
    }

    internal void ConfigureBilling(System.Text.Json.Nodes.JsonArray jobs, string existingJobId = null)
    {
        if (existingJobId != null)
        {
            amountPaidInput.ReadOnly = true;
            jobOrderIdInput.Enabled = paymentTypeInput.Enabled = paymentMethodInput.Enabled = false;
            allowCustomerCreditInput.Enabled = false;
            ShowReadOnlyReceipt();
        }
        System.Text.Json.Nodes.JsonNode SelectedJob()
        {
            var choice = jobOrderIdInput.SelectedItem as TenantChoice;
            string id = existingJobId ?? choice?.Value?.ToString();
            foreach (var job in jobs) if (job["jobOrderId"]?.ToString() == id) return job;
            return null;
        }

        void RefreshSummary()
        {
            var job = SelectedJob();
            depositButton.Enabled = balanceButton.Enabled = existingJobId == null && job?["outstandingBalance"] != null;
            if (job == null) { billingSummary.Text = "Select a job to view its billing summary."; return; }
            var total = job["totalEstimatedCost"]?.GetValue<decimal>();
            decimal paid = job["totalPaid"]?.GetValue<decimal>() ?? 0;
            var balance = job["outstandingBalance"]?.GetValue<decimal>();
            string state = total == null ? "ESTIMATE REQUIRED" : balance == 0 ? "FULLY PAID" : paid > 0 ? "PARTIALLY PAID" : "UNPAID";
            billingSummary.Text = $"Job #{job["jobOrderId"]} - {state}\nEstimated total: PHP {total:N2}    Paid: PHP {paid:N2}\nOutstanding balance: PHP {balance:N2}    Credit: PHP {job["customerCredit"]}";
            if (balance == 0) depositButton.Enabled = balanceButton.Enabled = false;
        }

        jobOrderIdInput.SelectedIndexChanged += (_, _) => RefreshSummary();
        depositButton.Click += (_, _) =>
        {
            var job = SelectedJob(); if (job?["totalEstimatedCost"] == null) return;
            decimal required = System.Math.Max(0, decimal.Round(job["totalEstimatedCost"].GetValue<decimal>() * .5m, 2, System.MidpointRounding.AwayFromZero) - job["totalPaid"].GetValue<decimal>());
            amountPaidInput.Text = required.ToString("F2"); paymentTypeInput.Text = "Downpayment";
        };
        balanceButton.Click += (_, _) =>
        {
            var job = SelectedJob();
            if (job?["outstandingBalance"] != null)
            {
                amountPaidInput.Text = job["outstandingBalance"].GetValue<decimal>().ToString("F2");
                paymentTypeInput.Text = "Balance Settlement";
            }
        };
        RefreshSummary();
    }
}
