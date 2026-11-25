namespace CRM_Revcare_;
partial class TenantTaskReviewForm
{
    private System.Windows.Forms.TextBox notesInput;
    private System.Windows.Forms.Label notesLabel;
    private void InitializeComponent()
    {
        notesInput = new System.Windows.Forms.TextBox(); notesLabel = new System.Windows.Forms.Label();
        notesLabel.Text = "Notes / rework reason"; notesLabel.AutoSize = true;
        notesInput.Name = "notes"; notesInput.Multiline = true; notesInput.Height = 180;
        notesInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; notesInput.MaxLength = 2000;
        notesInput.Dock = System.Windows.Forms.DockStyle.Fill;
        fieldsLayout.Controls.Add(notesLabel, 0, 0); fieldsLayout.Controls.Add(notesInput, 1, 0);
    }
}
