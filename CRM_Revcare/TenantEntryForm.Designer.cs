namespace CRM_Revcare_;
partial class TenantEntryForm
{
    private System.ComponentModel.IContainer components;
    protected System.Windows.Forms.TableLayoutPanel fieldsLayout;
    protected System.Windows.Forms.Panel editorHost;
    private System.Windows.Forms.Label titleLabel, recordLabel, resultLabel;
    private System.Windows.Forms.Button saveButton, cancelButton;
    private System.Windows.Forms.FlowLayoutPanel footer;
    protected override void Dispose(bool disposing) { if (disposing) components?.Dispose(); base.Dispose(disposing); }
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        fieldsLayout = new System.Windows.Forms.TableLayoutPanel(); editorHost = new System.Windows.Forms.Panel();
        titleLabel = new System.Windows.Forms.Label(); recordLabel = new System.Windows.Forms.Label(); resultLabel = new System.Windows.Forms.Label();
        saveButton = new System.Windows.Forms.Button(); cancelButton = new System.Windows.Forms.Button(); footer = new System.Windows.Forms.FlowLayoutPanel();
        SuspendLayout();
        titleLabel.Dock = System.Windows.Forms.DockStyle.Top; titleLabel.Height = 55; titleLabel.Padding = new System.Windows.Forms.Padding(16);
        titleLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold); titleLabel.Text = "RevCare Auto Hub CRM";
        recordLabel.Dock = System.Windows.Forms.DockStyle.Top; recordLabel.Height = 32; recordLabel.Padding = new System.Windows.Forms.Padding(16, 4, 0, 0);
        editorHost.Dock = System.Windows.Forms.DockStyle.Fill; editorHost.AutoScroll = true; editorHost.Padding = new System.Windows.Forms.Padding(16, 8, 16, 8);
        fieldsLayout.Dock = System.Windows.Forms.DockStyle.Top; fieldsLayout.AutoSize = true; fieldsLayout.ColumnCount = 2;
        fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 190));
        fieldsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100));
        editorHost.Controls.Add(fieldsLayout);
        resultLabel.Dock = System.Windows.Forms.DockStyle.Bottom; resultLabel.Height = 65; resultLabel.Padding = new System.Windows.Forms.Padding(16, 8, 16, 4);
        resultLabel.ForeColor = System.Drawing.Color.Firebrick;
        footer.Dock = System.Windows.Forms.DockStyle.Bottom; footer.Height = 55; footer.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6); footer.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
        saveButton.Text = "Save"; saveButton.Size = new System.Drawing.Size(125, 36); saveButton.UseVisualStyleBackColor = true;
        cancelButton.Text = "Cancel"; cancelButton.Size = new System.Drawing.Size(100, 36); cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        footer.Controls.Add(saveButton); footer.Controls.Add(cancelButton);
        Controls.Add(editorHost); Controls.Add(recordLabel); Controls.Add(titleLabel); Controls.Add(resultLabel); Controls.Add(footer);
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font; Font = new System.Drawing.Font("Segoe UI", 10F);
        ClientSize = new System.Drawing.Size(780, 700); MinimumSize = new System.Drawing.Size(660, 520);
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent; CancelButton = cancelButton;
        ResumeLayout(false);
    }
}
