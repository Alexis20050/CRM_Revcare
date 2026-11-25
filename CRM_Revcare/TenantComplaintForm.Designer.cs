namespace CRM_Revcare_;
partial class TenantComplaintForm
{
    private System.Windows.Forms.Label jobOrderIdLabel;
    private System.Windows.Forms.ComboBox jobOrderIdInput;
    private System.Windows.Forms.Label complaintTypeLabel;
    private System.Windows.Forms.TextBox complaintTypeInput;
    private System.Windows.Forms.Label descriptionLabel;
    private System.Windows.Forms.TextBox descriptionInput;
    private System.Windows.Forms.Label resolutionStatusLabel;
    private System.Windows.Forms.ComboBox resolutionStatusInput;
    private System.Windows.Forms.Label actionTakenLabel;
    private System.Windows.Forms.TextBox actionTakenInput;
    private void InitializeComponent()
    {
        fieldsLayout.SuspendLayout();
        SuspendLayout();
        jobOrderIdLabel = new System.Windows.Forms.Label();
        jobOrderIdInput = new System.Windows.Forms.ComboBox();
        jobOrderIdLabel.Name = "jobOrderIdLabel"; jobOrderIdLabel.Text = "Job order *";
        jobOrderIdLabel.AutoSize = true; jobOrderIdLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        jobOrderIdInput.Name = "jobOrderId"; jobOrderIdInput.Dock = System.Windows.Forms.DockStyle.Fill;
        jobOrderIdInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        jobOrderIdInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        jobOrderIdInput.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems; jobOrderIdInput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
        jobOrderIdInput.DropDownWidth = 650;
        fieldsLayout.Controls.Add(jobOrderIdLabel, 0, 0); fieldsLayout.Controls.Add(jobOrderIdInput, 1, 0);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        complaintTypeLabel = new System.Windows.Forms.Label();
        complaintTypeInput = new System.Windows.Forms.TextBox();
        complaintTypeLabel.Name = "complaintTypeLabel"; complaintTypeLabel.Text = "Complaint type *";
        complaintTypeLabel.AutoSize = true; complaintTypeLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        complaintTypeInput.Name = "complaintType"; complaintTypeInput.Dock = System.Windows.Forms.DockStyle.Fill;
        complaintTypeInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        fieldsLayout.Controls.Add(complaintTypeLabel, 0, 1); fieldsLayout.Controls.Add(complaintTypeInput, 1, 1);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        descriptionLabel = new System.Windows.Forms.Label();
        descriptionInput = new System.Windows.Forms.TextBox();
        descriptionLabel.Name = "descriptionLabel"; descriptionLabel.Text = "Description *";
        descriptionLabel.AutoSize = true; descriptionLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        descriptionInput.Name = "description"; descriptionInput.Dock = System.Windows.Forms.DockStyle.Fill;
        descriptionInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        descriptionInput.Multiline = true; descriptionInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; descriptionInput.Height = 120;
        fieldsLayout.Controls.Add(descriptionLabel, 0, 2); fieldsLayout.Controls.Add(descriptionInput, 1, 2);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        resolutionStatusLabel = new System.Windows.Forms.Label();
        resolutionStatusInput = new System.Windows.Forms.ComboBox();
        resolutionStatusLabel.Name = "resolutionStatusLabel"; resolutionStatusLabel.Text = "Resolution status";
        resolutionStatusLabel.AutoSize = true; resolutionStatusLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        resolutionStatusInput.Name = "resolutionStatus"; resolutionStatusInput.Dock = System.Windows.Forms.DockStyle.Fill;
        resolutionStatusInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        resolutionStatusInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        resolutionStatusInput.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems; resolutionStatusInput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
        resolutionStatusInput.DropDownWidth = 650;
        fieldsLayout.Controls.Add(resolutionStatusLabel, 0, 3); fieldsLayout.Controls.Add(resolutionStatusInput, 1, 3);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        actionTakenLabel = new System.Windows.Forms.Label();
        actionTakenInput = new System.Windows.Forms.TextBox();
        actionTakenLabel.Name = "actionTakenLabel"; actionTakenLabel.Text = "Action taken";
        actionTakenLabel.AutoSize = true; actionTakenLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        actionTakenInput.Name = "actionTaken"; actionTakenInput.Dock = System.Windows.Forms.DockStyle.Fill;
        actionTakenInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        actionTakenInput.Multiline = true; actionTakenInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; actionTakenInput.Height = 100;
        fieldsLayout.Controls.Add(actionTakenLabel, 0, 4); fieldsLayout.Controls.Add(actionTakenInput, 1, 4);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        fieldsLayout.RowCount = 5;
        Text = "RevCare Auto Hub CRM";
        fieldsLayout.ResumeLayout(false); fieldsLayout.PerformLayout();
        ResumeLayout(false);
    }
}

