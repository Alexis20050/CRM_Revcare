#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    partial class ReceivePaymentDialogForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            headerPanel = new Panel();
            titleLabel = new Label();
            subtitleLabel = new Label();
            financialSummaryPanel = new Panel();
            lblJobHeader = new Label();
            lblCustomerInfo = new Label();
            lblVehicleInfo = new Label();
            dividerSummary = new Label();
            lblTotalCost = new Label();
            lblTotalPaid = new Label();
            lblBalance = new Label();
            lblDepositRequired = new Label();
            lblDepositDue = new Label();
            inputsPanel = new Panel();
            lblAmount = new Label();
            amountBox = new TextBox();
            btnDepositShortcut = new Button();
            btnBalanceShortcut = new Button();
            lblMethod = new Label();
            methodBox = new ComboBox();
            lblCategory = new Label();
            categoryBox = new ComboBox();
            allowCreditCheck = new CheckBox();
            livePreviewPanel = new Panel();
            previewTitle = new Label();
            lblPreviewAmount = new Label();
            lblPreviewNewBalance = new Label();
            lblPreviewPaymentStatus = new Label();
            lblPreviewDepositStatus = new Label();
            actionsPanel = new FlowLayoutPanel();
            btnCancel = new Button();
            btnRecordPayment = new Button();
            statusLabel = new Label();
            successPanel = new Panel();
            successTitle = new Label();
            successReceiptRef = new Label();
            successButtonsFlow = new FlowLayoutPanel();
            btnPreviewReceipt = new Button();
            btnPrintReceipt = new Button();
            btnCloseDialog = new Button();

            headerPanel.SuspendLayout();
            financialSummaryPanel.SuspendLayout();
            inputsPanel.SuspendLayout();
            livePreviewPanel.SuspendLayout();
            actionsPanel.SuspendLayout();
            successPanel.SuspendLayout();
            successButtonsFlow.SuspendLayout();
            SuspendLayout();

            // headerPanel
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 60;
            headerPanel.BackColor = Color.FromArgb(241, 245, 249);
            headerPanel.Padding = new Padding(16, 10, 16, 10);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);

            titleLabel.Text = "Receive Customer Payment";
            titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            titleLabel.Location = new Point(14, 8);
            titleLabel.AutoSize = true;

            subtitleLabel.Text = "Record incoming funds against Job Order balance with automatic deposit verification.";
            subtitleLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            subtitleLabel.ForeColor = Color.FromArgb(71, 85, 105);
            subtitleLabel.Location = new Point(15, 34);
            subtitleLabel.AutoSize = true;

            // financialSummaryPanel
            financialSummaryPanel.Dock = DockStyle.Top;
            financialSummaryPanel.Height = 125;
            financialSummaryPanel.BackColor = Color.FromArgb(248, 250, 252);
            financialSummaryPanel.Padding = new Padding(16, 8, 16, 8);
            financialSummaryPanel.Controls.Add(lblJobHeader);
            financialSummaryPanel.Controls.Add(lblCustomerInfo);
            financialSummaryPanel.Controls.Add(lblVehicleInfo);
            financialSummaryPanel.Controls.Add(dividerSummary);
            financialSummaryPanel.Controls.Add(lblTotalCost);
            financialSummaryPanel.Controls.Add(lblTotalPaid);
            financialSummaryPanel.Controls.Add(lblBalance);
            financialSummaryPanel.Controls.Add(lblDepositRequired);
            financialSummaryPanel.Controls.Add(lblDepositDue);

            lblJobHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblJobHeader.Location = new Point(16, 8);
            lblJobHeader.AutoSize = true;

            lblCustomerInfo.Location = new Point(16, 30);
            lblCustomerInfo.AutoSize = true;

            lblVehicleInfo.Location = new Point(340, 30);
            lblVehicleInfo.AutoSize = true;

            dividerSummary.BorderStyle = BorderStyle.Fixed3D;
            dividerSummary.Location = new Point(16, 52);
            dividerSummary.Size = new Size(610, 2);

            lblTotalCost.Location = new Point(16, 60);
            lblTotalCost.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblTotalCost.AutoSize = true;

            lblTotalPaid.Location = new Point(190, 60);
            lblTotalPaid.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblTotalPaid.AutoSize = true;

            lblBalance.Location = new Point(360, 60);
            lblBalance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblBalance.ForeColor = Color.FromArgb(185, 28, 28);
            lblBalance.AutoSize = true;

            lblDepositRequired.Location = new Point(16, 88);
            lblDepositRequired.ForeColor = Color.FromArgb(71, 85, 105);
            lblDepositRequired.AutoSize = true;

            lblDepositDue.Location = new Point(360, 88);
            lblDepositDue.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDepositDue.ForeColor = Color.FromArgb(30, 64, 175);
            lblDepositDue.AutoSize = true;

            // inputsPanel
            inputsPanel.Dock = DockStyle.Top;
            inputsPanel.Height = 150;
            inputsPanel.Padding = new Padding(16, 10, 16, 10);
            inputsPanel.Controls.Add(lblAmount);
            inputsPanel.Controls.Add(amountBox);
            inputsPanel.Controls.Add(btnDepositShortcut);
            inputsPanel.Controls.Add(btnBalanceShortcut);
            inputsPanel.Controls.Add(lblMethod);
            inputsPanel.Controls.Add(methodBox);
            inputsPanel.Controls.Add(lblCategory);
            inputsPanel.Controls.Add(categoryBox);
            inputsPanel.Controls.Add(allowCreditCheck);

            lblAmount.Text = "Amount Received (PHP) *:";
            lblAmount.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblAmount.Location = new Point(16, 14);
            lblAmount.AutoSize = true;

            amountBox.Location = new Point(180, 11);
            amountBox.Size = new Size(160, 24);
            amountBox.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            amountBox.PlaceholderText = "0.00";

            btnDepositShortcut.Text = "Use Deposit Due";
            btnDepositShortcut.Location = new Point(350, 10);
            btnDepositShortcut.Size = new Size(130, 26);
            btnDepositShortcut.UseVisualStyleBackColor = true;

            btnBalanceShortcut.Text = "Use Remaining Balance";
            btnBalanceShortcut.Location = new Point(488, 10);
            btnBalanceShortcut.Size = new Size(150, 26);
            btnBalanceShortcut.UseVisualStyleBackColor = true;

            lblMethod.Text = "Payment Method *:";
            lblMethod.Location = new Point(16, 52);
            lblMethod.AutoSize = true;

            methodBox.Location = new Point(180, 49);
            methodBox.Size = new Size(160, 23);
            methodBox.DropDownStyle = ComboBoxStyle.DropDownList;
            methodBox.Items.AddRange(new object[] { "Cash", "GCash", "Credit Card", "Debit Card", "Bank Transfer", "Check" });

            lblCategory.Text = "Payment Category:";
            lblCategory.Location = new Point(350, 52);
            lblCategory.AutoSize = true;

            categoryBox.Location = new Point(468, 49);
            categoryBox.Size = new Size(170, 23);
            categoryBox.DropDownStyle = ComboBoxStyle.DropDownList;
            categoryBox.Items.AddRange(new object[] { "Downpayment", "Progress Payment", "Final Balance", "Retail Full", "Customer Deductible", "Insurer Share" });

            allowCreditCheck.Text = "Allow excess payment as customer credit (requires explicit authorization)";
            allowCreditCheck.Location = new Point(180, 85);
            allowCreditCheck.AutoSize = true;
            allowCreditCheck.ForeColor = Color.FromArgb(71, 85, 105);

            // livePreviewPanel
            livePreviewPanel.Dock = DockStyle.Fill;
            livePreviewPanel.BackColor = Color.FromArgb(241, 245, 249);
            livePreviewPanel.Padding = new Padding(16, 8, 16, 8);
            livePreviewPanel.BorderStyle = BorderStyle.FixedSingle;
            livePreviewPanel.Controls.Add(previewTitle);
            livePreviewPanel.Controls.Add(lblPreviewAmount);
            livePreviewPanel.Controls.Add(lblPreviewNewBalance);
            livePreviewPanel.Controls.Add(lblPreviewPaymentStatus);
            livePreviewPanel.Controls.Add(lblPreviewDepositStatus);

            previewTitle.Text = "LIVE PAYMENT PREVIEW";
            previewTitle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            previewTitle.ForeColor = Color.FromArgb(100, 116, 139);
            previewTitle.Location = new Point(14, 8);
            previewTitle.AutoSize = true;

            lblPreviewAmount.Location = new Point(14, 28);
            lblPreviewAmount.AutoSize = true;

            lblPreviewNewBalance.Location = new Point(14, 48);
            lblPreviewNewBalance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblPreviewNewBalance.AutoSize = true;

            lblPreviewPaymentStatus.Location = new Point(340, 28);
            lblPreviewPaymentStatus.AutoSize = true;

            lblPreviewDepositStatus.Location = new Point(340, 48);
            lblPreviewDepositStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblPreviewDepositStatus.AutoSize = true;

            // actionsPanel
            actionsPanel.Dock = DockStyle.Bottom;
            actionsPanel.Height = 50;
            actionsPanel.FlowDirection = FlowDirection.RightToLeft;
            actionsPanel.Padding = new Padding(16, 8, 16, 8);
            actionsPanel.Controls.Add(btnCancel);
            actionsPanel.Controls.Add(btnRecordPayment);
            actionsPanel.Controls.Add(statusLabel);

            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(85, 32);
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.UseVisualStyleBackColor = true;

            btnRecordPayment.Text = "Record Payment";
            btnRecordPayment.Size = new Size(150, 32);
            btnRecordPayment.BackColor = Color.FromArgb(22, 101, 52);
            btnRecordPayment.ForeColor = Color.White;
            btnRecordPayment.UseVisualStyleBackColor = false;

            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.Firebrick;
            statusLabel.Padding = new Padding(0, 8, 16, 0);

            // successPanel
            successPanel.Dock = DockStyle.Fill;
            successPanel.BackColor = Color.FromArgb(240, 253, 244);
            successPanel.Padding = new Padding(24);
            successPanel.Visible = false;
            successPanel.Controls.Add(successTitle);
            successPanel.Controls.Add(successReceiptRef);
            successPanel.Controls.Add(successButtonsFlow);

            successTitle.Text = "✓ Payment Recorded Successfully!";
            successTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            successTitle.ForeColor = Color.FromArgb(22, 101, 52);
            successTitle.Location = new Point(24, 20);
            successTitle.AutoSize = true;

            successReceiptRef.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            successReceiptRef.ForeColor = Color.FromArgb(15, 23, 42);
            successReceiptRef.Location = new Point(26, 52);
            successReceiptRef.AutoSize = true;

            successButtonsFlow.Location = new Point(26, 90);
            successButtonsFlow.Size = new Size(580, 45);
            successButtonsFlow.Controls.Add(btnPreviewReceipt);
            successButtonsFlow.Controls.Add(btnPrintReceipt);
            successButtonsFlow.Controls.Add(btnCloseDialog);

            btnPreviewReceipt.Text = "👁 Preview Receipt";
            btnPreviewReceipt.Size = new Size(140, 34);
            btnPreviewReceipt.UseVisualStyleBackColor = true;

            btnPrintReceipt.Text = "🖨 Print Receipt";
            btnPrintReceipt.Size = new Size(130, 34);
            btnPrintReceipt.UseVisualStyleBackColor = true;

            btnCloseDialog.Text = "Done / Close";
            btnCloseDialog.Size = new Size(110, 34);
            btnCloseDialog.UseVisualStyleBackColor = true;

            // ReceivePaymentDialogForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(660, 480);
            MinimumSize = new Size(620, 450);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Text = "Receive Payment";

            Controls.Add(successPanel);
            Controls.Add(livePreviewPanel);
            Controls.Add(inputsPanel);
            Controls.Add(financialSummaryPanel);
            Controls.Add(headerPanel);
            Controls.Add(actionsPanel);

            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            financialSummaryPanel.ResumeLayout(false);
            financialSummaryPanel.PerformLayout();
            inputsPanel.ResumeLayout(false);
            inputsPanel.PerformLayout();
            livePreviewPanel.ResumeLayout(false);
            livePreviewPanel.PerformLayout();
            actionsPanel.ResumeLayout(false);
            actionsPanel.PerformLayout();
            successPanel.ResumeLayout(false);
            successPanel.PerformLayout();
            successButtonsFlow.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Panel headerPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Panel financialSummaryPanel;
        private Label lblJobHeader;
        private Label lblCustomerInfo;
        private Label lblVehicleInfo;
        private Label dividerSummary;
        private Label lblTotalCost;
        private Label lblTotalPaid;
        private Label lblBalance;
        private Label lblDepositRequired;
        private Label lblDepositDue;
        private Panel inputsPanel;
        private Label lblAmount;
        private TextBox amountBox;
        private Button btnDepositShortcut;
        private Button btnBalanceShortcut;
        private Label lblMethod;
        private ComboBox methodBox;
        private Label lblCategory;
        private ComboBox categoryBox;
        private CheckBox allowCreditCheck;
        private Panel livePreviewPanel;
        private Label previewTitle;
        private Label lblPreviewAmount;
        private Label lblPreviewNewBalance;
        private Label lblPreviewPaymentStatus;
        private Label lblPreviewDepositStatus;
        private FlowLayoutPanel actionsPanel;
        private Button btnCancel;
        private Button btnRecordPayment;
        private Label statusLabel;
        private Panel successPanel;
        private Label successTitle;
        private Label successReceiptRef;
        private FlowLayoutPanel successButtonsFlow;
        private Button btnPreviewReceipt;
        private Button btnPrintReceipt;
        private Button btnCloseDialog;
    }
}
