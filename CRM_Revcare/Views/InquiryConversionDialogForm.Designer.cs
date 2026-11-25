namespace CRM_Revcare_
{
    partial class InquiryConversionDialogForm
    {
        private void InitializeComponent()
        {
            customer = new System.Windows.Forms.TextBox();
            browseCustomerButton = new System.Windows.Forms.Button();
            clearCustomerButton = new System.Windows.Forms.Button();
            vehicle = new System.Windows.Forms.TextBox();
            customerLabel = new System.Windows.Forms.Label();
            vehicleLabel = new System.Windows.Forms.Label();
            result = new System.Windows.Forms.Label();
            convert = new System.Windows.Forms.Button();
            cancel = new System.Windows.Forms.Button();
            SuspendLayout();
            customerLabel.Text = "Select customer, or leave blank to create from inquiry";
            customerLabel.Location = new System.Drawing.Point(16, 16);
            customerLabel.Size = new System.Drawing.Size(420, 28);
            customer.Location = new System.Drawing.Point(16, 48);
            customer.Size = new System.Drawing.Size(310, 26);
            customer.ReadOnly = true;
            customer.BackColor = System.Drawing.Color.White;
            customer.PlaceholderText = "Leave blank to register from inquiry";
            browseCustomerButton.Location = new System.Drawing.Point(332, 47);
            browseCustomerButton.Size = new System.Drawing.Size(76, 27);
            browseCustomerButton.Text = "🔍 Browse";
            browseCustomerButton.UseVisualStyleBackColor = true;
            clearCustomerButton.Location = new System.Drawing.Point(410, 47);
            clearCustomerButton.Size = new System.Drawing.Size(26, 27);
            clearCustomerButton.Text = "✕";
            clearCustomerButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            clearCustomerButton.UseVisualStyleBackColor = true;
            vehicleLabel.Text = "Vehicle details (required)";
            vehicleLabel.Location = new System.Drawing.Point(16, 90);
            vehicleLabel.Size = new System.Drawing.Size(420, 24);
            vehicle.Location = new System.Drawing.Point(16, 118);
            vehicle.Size = new System.Drawing.Size(420, 28);
            convert.Text = "Convert to Job Order";
            convert.Location = new System.Drawing.Point(16, 165);
            convert.Size = new System.Drawing.Size(200, 34);
            cancel.Text = "Cancel";
            cancel.Location = new System.Drawing.Point(230, 165);
            cancel.Size = new System.Drawing.Size(100, 34);
            cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            result.Location = new System.Drawing.Point(16, 215);
            result.Size = new System.Drawing.Size(420, 100);
            Controls.Add(customerLabel);
            Controls.Add(customer);
            Controls.Add(browseCustomerButton);
            Controls.Add(clearCustomerButton);
            Controls.Add(vehicleLabel);
            Controls.Add(vehicle);
            Controls.Add(convert);
            Controls.Add(cancel);
            Controls.Add(result);
            ClientSize = new System.Drawing.Size(455, 330);
            Text = "Convert Inquiry";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            CancelButton = cancel;
            AcceptButton = convert;
            ResumeLayout(false);
            PerformLayout();
        }
        internal System.Windows.Forms.TextBox customer;
        internal System.Windows.Forms.Button browseCustomerButton;
        internal System.Windows.Forms.Button clearCustomerButton;
        private System.Windows.Forms.TextBox vehicle;
        private System.Windows.Forms.Label customerLabel;
        private System.Windows.Forms.Label vehicleLabel;
        private System.Windows.Forms.Label result;
        private System.Windows.Forms.Button convert;
        private System.Windows.Forms.Button cancel;
    }
}
