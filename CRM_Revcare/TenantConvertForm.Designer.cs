namespace CRM_Revcare_;
partial class TenantConvertForm
{
    private System.Windows.Forms.ComboBox customerIdInput;
    private System.Windows.Forms.Label customerIdLabel;
    private System.Windows.Forms.TextBox vehicleDetailsInput;
    private System.Windows.Forms.Label vehicleDetailsLabel;
    private void InitializeComponent()
    {
        customerIdInput = new System.Windows.Forms.ComboBox(); customerIdLabel = new System.Windows.Forms.Label();
        customerIdInput.Name = "customerId"; customerIdInput.Dock = System.Windows.Forms.DockStyle.Fill;
        customerIdLabel.Text = "Customer"; customerIdLabel.AutoSize = true;
        customerIdInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 12);
        customerIdInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList; customerIdInput.DropDownWidth = 650;
        fieldsLayout.Controls.Add(customerIdLabel, 0, 0); fieldsLayout.Controls.Add(customerIdInput, 1, 0);
        vehicleDetailsInput = new System.Windows.Forms.TextBox(); vehicleDetailsLabel = new System.Windows.Forms.Label();
        vehicleDetailsInput.Name = "vehicleDetails"; vehicleDetailsInput.Dock = System.Windows.Forms.DockStyle.Fill;
        vehicleDetailsLabel.Text = "Vehicle details *"; vehicleDetailsLabel.AutoSize = true;
        vehicleDetailsInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 12);
        fieldsLayout.Controls.Add(vehicleDetailsLabel, 0, 1); fieldsLayout.Controls.Add(vehicleDetailsInput, 1, 1);
    }
}

