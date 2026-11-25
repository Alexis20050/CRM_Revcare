namespace CRM_Revcare_;
partial class TenantVoidForm
{
    private System.Windows.Forms.TextBox reasonInput;
    private System.Windows.Forms.Label reasonLabel;
    private void InitializeComponent()
    {
        reasonInput = new System.Windows.Forms.TextBox(); reasonLabel = new System.Windows.Forms.Label();
        reasonInput.Name = "reason"; reasonInput.Dock = System.Windows.Forms.DockStyle.Fill;
        reasonLabel.Text = "Reason for void *"; reasonLabel.AutoSize = true;
        reasonInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 12);
        reasonInput.Multiline = true; reasonInput.Height = 140; reasonInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        fieldsLayout.Controls.Add(reasonLabel, 0, 0); fieldsLayout.Controls.Add(reasonInput, 1, 0);
    }
}

