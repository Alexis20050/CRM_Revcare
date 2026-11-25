namespace CRM_Revcare_;
partial class TenantTaskForm
{
    private System.Windows.Forms.Label statusLabel;
    private System.Windows.Forms.ComboBox statusInput;
    private System.Windows.Forms.Label remarksLabel;
    private System.Windows.Forms.TextBox remarksInput;
    private void InitializeComponent()
    {
        fieldsLayout.SuspendLayout();
        SuspendLayout();
        statusLabel = new System.Windows.Forms.Label();
        statusInput = new System.Windows.Forms.ComboBox();
        statusLabel.Name = "statusLabel"; statusLabel.Text = "Task status *";
        statusLabel.AutoSize = true; statusLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        statusInput.Name = "status"; statusInput.Dock = System.Windows.Forms.DockStyle.Fill;
        statusInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        statusInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        statusInput.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems; statusInput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
        statusInput.DropDownWidth = 650;
        fieldsLayout.Controls.Add(statusLabel, 0, 0); fieldsLayout.Controls.Add(statusInput, 1, 0);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        remarksLabel = new System.Windows.Forms.Label();
        remarksInput = new System.Windows.Forms.TextBox();
        remarksLabel.Name = "remarksLabel"; remarksLabel.Text = "Mechanic notes";
        remarksLabel.AutoSize = true; remarksLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        remarksInput.Name = "remarks"; remarksInput.Dock = System.Windows.Forms.DockStyle.Fill;
        remarksInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        remarksInput.Multiline = true; remarksInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; remarksInput.Height = 180;
        fieldsLayout.Controls.Add(remarksLabel, 0, 1); fieldsLayout.Controls.Add(remarksInput, 1, 1);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        fieldsLayout.RowCount = 2;
        Text = "RevCare Auto Hub CRM";
        fieldsLayout.ResumeLayout(false); fieldsLayout.PerformLayout();
        ResumeLayout(false);
    }
}

