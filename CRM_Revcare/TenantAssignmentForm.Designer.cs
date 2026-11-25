namespace CRM_Revcare_;
partial class TenantAssignmentForm
{
    private System.Windows.Forms.ComboBox userIdInput;
    private System.Windows.Forms.Label userIdLabel;
    private System.Windows.Forms.TextBox reasonInput;
    private System.Windows.Forms.Label reasonLabel;
    private void InitializeComponent()
    {
        userIdInput = new System.Windows.Forms.ComboBox();
        userIdLabel = new System.Windows.Forms.Label();
        reasonLabel = new System.Windows.Forms.Label();
        reasonInput = new System.Windows.Forms.TextBox();
        SuspendLayout();
        // 
        // editorHost
        // 
        editorHost.Location = new System.Drawing.Point(0, 87);
        editorHost.Size = new System.Drawing.Size(780, 493);
        // 
        // userIdInput
        // 
        userIdInput.Dock = System.Windows.Forms.DockStyle.Fill;
        userIdInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        userIdInput.DropDownWidth = 650;
        userIdInput.Location = new System.Drawing.Point(193, 6);
        userIdInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 12);
        userIdInput.Name = "userIdInput";
        userIdInput.Size = new System.Drawing.Size(552, 31);
        userIdInput.TabIndex = 1;
        // 
        // userIdLabel
        // 
        userIdLabel.AutoSize = true;
        userIdLabel.Location = new System.Drawing.Point(3, 0);
        userIdLabel.Name = "userIdLabel";
        userIdLabel.Size = new System.Drawing.Size(124, 20);
        userIdLabel.TabIndex = 0;
        userIdLabel.Text = "Staff member *";
        // 
        // reasonLabel
        // 
        reasonLabel.AutoSize = true;
        reasonLabel.Location = new System.Drawing.Point(3, 20);
        reasonLabel.Name = "reasonLabel";
        reasonLabel.Size = new System.Drawing.Size(167, 20);
        reasonLabel.TabIndex = 2;
        reasonLabel.Text = "Assignment reason *";
        // 
        // reasonInput
        // 
        reasonInput.Dock = System.Windows.Forms.DockStyle.Fill;
        reasonInput.Location = new System.Drawing.Point(193, 26);
        reasonInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 12);
        reasonInput.Multiline = true;
        reasonInput.Name = "reasonInput";
        reasonInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        reasonInput.Size = new System.Drawing.Size(552, 2);
        reasonInput.TabIndex = 3;
        // 
        // TenantAssignmentForm
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(9F, 23F);
        ClientSize = new System.Drawing.Size(780, 700);
        Name = "TenantAssignmentForm";
        ResumeLayout(false);
    }
}

