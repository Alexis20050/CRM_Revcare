#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    partial class VoidPaymentDialogForm
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
            detailsPanel = new Panel();
            lblReceiptHeader = new Label();
            lblCustomerInfo = new Label();
            lblVehicleInfo = new Label();
            lblAmount = new Label();
            lblDate = new Label();
            lblMethod = new Label();
            divider = new Label();
            lblCurrentBalance = new Label();
            lblBalanceAfterVoid = new Label();
            reasonPanel = new Panel();
            lblReason = new Label();
            reasonBox = new TextBox();
            warningLabel = new Label();
            actionsPanel = new FlowLayoutPanel();
            btnCancel = new Button();
            btnConfirmVoid = new Button();
            statusLabel = new Label();

            headerPanel.SuspendLayout();
            detailsPanel.SuspendLayout();
            reasonPanel.SuspendLayout();
            actionsPanel.SuspendLayout();
            SuspendLayout();

            // headerPanel
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 60;
            headerPanel.BackColor = Color.FromArgb(254, 242, 242);
            headerPanel.Padding = new Padding(16, 10, 16, 10);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);

            titleLabel.Text = "Void Payment Receipt";
            titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(185, 28, 28);
            titleLabel.Location = new Point(14, 8);
            titleLabel.AutoSize = true;

            subtitleLabel.Text = "Voiding permanently marks this receipt voided while preserving audit history.";
            subtitleLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            subtitleLabel.ForeColor = Color.FromArgb(127, 29, 29);
            subtitleLabel.Location = new Point(15, 34);
            subtitleLabel.AutoSize = true;

            // detailsPanel
            detailsPanel.Dock = DockStyle.Top;
            detailsPanel.Height = 160;
            detailsPanel.BackColor = Color.FromArgb(248, 250, 252);
            detailsPanel.Padding = new Padding(16, 10, 16, 10);
            detailsPanel.Controls.Add(lblReceiptHeader);
            detailsPanel.Controls.Add(lblCustomerInfo);
            detailsPanel.Controls.Add(lblVehicleInfo);
            detailsPanel.Controls.Add(lblAmount);
            detailsPanel.Controls.Add(lblDate);
            detailsPanel.Controls.Add(lblMethod);
            detailsPanel.Controls.Add(divider);
            detailsPanel.Controls.Add(lblCurrentBalance);
            detailsPanel.Controls.Add(lblBalanceAfterVoid);

            lblReceiptHeader.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblReceiptHeader.Location = new Point(16, 10);
            lblReceiptHeader.AutoSize = true;

            lblCustomerInfo.Location = new Point(16, 35);
            lblCustomerInfo.AutoSize = true;

            lblVehicleInfo.Location = new Point(320, 35);
            lblVehicleInfo.AutoSize = true;

            lblAmount.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblAmount.ForeColor = Color.FromArgb(15, 23, 42);
            lblAmount.Location = new Point(16, 60);
            lblAmount.AutoSize = true;

            lblMethod.Location = new Point(230, 60);
            lblMethod.AutoSize = true;

            lblDate.Location = new Point(380, 60);
            lblDate.AutoSize = true;

            divider.BorderStyle = BorderStyle.Fixed3D;
            divider.Location = new Point(16, 90);
            divider.Size = new Size(520, 2);

            lblCurrentBalance.Location = new Point(16, 102);
            lblCurrentBalance.AutoSize = true;

            lblBalanceAfterVoid.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblBalanceAfterVoid.ForeColor = Color.FromArgb(185, 28, 28);
            lblBalanceAfterVoid.Location = new Point(16, 126);
            lblBalanceAfterVoid.AutoSize = true;

            // reasonPanel
            reasonPanel.Dock = DockStyle.Fill;
            reasonPanel.Padding = new Padding(16, 10, 16, 10);
            reasonPanel.Controls.Add(lblReason);
            reasonPanel.Controls.Add(reasonBox);
            reasonPanel.Controls.Add(warningLabel);

            lblReason.Text = "Mandatory Void Reason *:";
            lblReason.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblReason.Location = new Point(16, 10);
            lblReason.AutoSize = true;

            reasonBox.Location = new Point(16, 32);
            reasonBox.Size = new Size(520, 60);
            reasonBox.Multiline = true;
            reasonBox.PlaceholderText = "Explain in detail why this payment is being voided (e.g. wrong amount, duplicate entry, check bounced)...";

            warningLabel.Location = new Point(16, 100);
            warningLabel.Font = new Font("Segoe UI", 8.25F, FontStyle.Italic);
            warningLabel.ForeColor = Color.FromArgb(100, 116, 139);
            warningLabel.Text = "Note: A void is not a refund. If replacement money was received, record a new payment receipt.";
            warningLabel.AutoSize = true;

            // actionsPanel
            actionsPanel.Dock = DockStyle.Bottom;
            actionsPanel.Height = 50;
            actionsPanel.FlowDirection = FlowDirection.RightToLeft;
            actionsPanel.Padding = new Padding(16, 8, 16, 8);
            actionsPanel.Controls.Add(btnCancel);
            actionsPanel.Controls.Add(btnConfirmVoid);
            actionsPanel.Controls.Add(statusLabel);

            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(85, 32);
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.UseVisualStyleBackColor = true;

            btnConfirmVoid.Text = "Confirm Void Payment";
            btnConfirmVoid.Size = new Size(160, 32);
            btnConfirmVoid.BackColor = Color.FromArgb(185, 28, 28);
            btnConfirmVoid.ForeColor = Color.White;
            btnConfirmVoid.UseVisualStyleBackColor = false;

            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.Firebrick;
            statusLabel.Padding = new Padding(0, 8, 16, 0);

            // VoidPaymentDialogForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(560, 420);
            MinimumSize = new Size(520, 380);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Text = "Void Payment Receipt";

            Controls.Add(reasonPanel);
            Controls.Add(detailsPanel);
            Controls.Add(headerPanel);
            Controls.Add(actionsPanel);

            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            detailsPanel.ResumeLayout(false);
            detailsPanel.PerformLayout();
            reasonPanel.ResumeLayout(false);
            reasonPanel.PerformLayout();
            actionsPanel.ResumeLayout(false);
            actionsPanel.PerformLayout();
            ResumeLayout(false);
        }

        private Panel headerPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Panel detailsPanel;
        private Label lblReceiptHeader;
        private Label lblCustomerInfo;
        private Label lblVehicleInfo;
        private Label lblAmount;
        private Label lblDate;
        private Label lblMethod;
        private Label divider;
        private Label lblCurrentBalance;
        private Label lblBalanceAfterVoid;
        private Panel reasonPanel;
        private Label lblReason;
        private TextBox reasonBox;
        private Label warningLabel;
        private FlowLayoutPanel actionsPanel;
        private Button btnCancel;
        private Button btnConfirmVoid;
        private Label statusLabel;
    }
}
