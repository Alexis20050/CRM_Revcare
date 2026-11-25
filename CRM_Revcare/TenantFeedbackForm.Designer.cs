namespace CRM_Revcare_;
partial class TenantFeedbackForm
{
    private System.Windows.Forms.Label jobOrderIdLabel;
    private System.Windows.Forms.ComboBox jobOrderIdInput;
    private System.Windows.Forms.Label ratingScoreLabel;
    private System.Windows.Forms.ComboBox ratingScoreInput;
    private System.Windows.Forms.Label positiveRemarksLabel;
    private System.Windows.Forms.TextBox positiveRemarksInput;
    private System.Windows.Forms.Label areasForImprovementLabel;
    private System.Windows.Forms.TextBox areasForImprovementInput;
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
        ratingScoreLabel = new System.Windows.Forms.Label();
        ratingScoreInput = new System.Windows.Forms.ComboBox();
        ratingScoreLabel.Name = "ratingScoreLabel"; ratingScoreLabel.Text = "Rating *";
        ratingScoreLabel.AutoSize = true; ratingScoreLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        ratingScoreInput.Name = "ratingScore"; ratingScoreInput.Dock = System.Windows.Forms.DockStyle.Fill;
        ratingScoreInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        ratingScoreInput.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        ratingScoreInput.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems; ratingScoreInput.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
        ratingScoreInput.DropDownWidth = 650;
        fieldsLayout.Controls.Add(ratingScoreLabel, 0, 1); fieldsLayout.Controls.Add(ratingScoreInput, 1, 1);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        positiveRemarksLabel = new System.Windows.Forms.Label();
        positiveRemarksInput = new System.Windows.Forms.TextBox();
        positiveRemarksLabel.Name = "positiveRemarksLabel"; positiveRemarksLabel.Text = "Positive remarks";
        positiveRemarksLabel.AutoSize = true; positiveRemarksLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        positiveRemarksInput.Name = "positiveRemarks"; positiveRemarksInput.Dock = System.Windows.Forms.DockStyle.Fill;
        positiveRemarksInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        positiveRemarksInput.Multiline = true; positiveRemarksInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; positiveRemarksInput.Height = 120;
        fieldsLayout.Controls.Add(positiveRemarksLabel, 0, 2); fieldsLayout.Controls.Add(positiveRemarksInput, 1, 2);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        areasForImprovementLabel = new System.Windows.Forms.Label();
        areasForImprovementInput = new System.Windows.Forms.TextBox();
        areasForImprovementLabel.Name = "areasForImprovementLabel"; areasForImprovementLabel.Text = "Areas for improvement";
        areasForImprovementLabel.AutoSize = true; areasForImprovementLabel.Margin = new System.Windows.Forms.Padding(3, 10, 10, 8);
        areasForImprovementInput.Name = "areasForImprovement"; areasForImprovementInput.Dock = System.Windows.Forms.DockStyle.Fill;
        areasForImprovementInput.Margin = new System.Windows.Forms.Padding(3, 6, 3, 8);
        areasForImprovementInput.Multiline = true; areasForImprovementInput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical; areasForImprovementInput.Height = 120;
        fieldsLayout.Controls.Add(areasForImprovementLabel, 0, 3); fieldsLayout.Controls.Add(areasForImprovementInput, 1, 3);
        fieldsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
        fieldsLayout.RowCount = 4;
        Text = "RevCare Auto Hub CRM";
        fieldsLayout.ResumeLayout(false); fieldsLayout.PerformLayout();
        ResumeLayout(false);
    }
}

