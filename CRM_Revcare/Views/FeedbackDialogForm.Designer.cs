namespace CRM_Revcare_
{
    partial class FeedbackDialogForm
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
            formTitle = new System.Windows.Forms.Label();
            jobLabel = new System.Windows.Forms.Label();
            jobText = new System.Windows.Forms.TextBox();
            browseJobButton = new System.Windows.Forms.Button();
            clearJobButton = new System.Windows.Forms.Button();
            customerLabel = new System.Windows.Forms.Label();
            customerText = new System.Windows.Forms.TextBox();
            ratingLabel = new System.Windows.Forms.Label();
            ratingCombo = new System.Windows.Forms.ComboBox();
            positiveLabel = new System.Windows.Forms.Label();
            positiveText = new System.Windows.Forms.TextBox();
            improvementsLabel = new System.Windows.Forms.Label();
            improvementsText = new System.Windows.Forms.TextBox();
            stateLabel = new System.Windows.Forms.Label();
            stateCombo = new System.Windows.Forms.ComboBox();
            errorLabel = new System.Windows.Forms.Label();
            saveButton = new System.Windows.Forms.Button();
            cancelButton = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // formTitle
            // 
            formTitle.AutoSize = true;
            formTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            formTitle.Location = new System.Drawing.Point(20, 16);
            formTitle.Name = "formTitle";
            formTitle.Size = new System.Drawing.Size(147, 25);
            formTitle.TabIndex = 0;
            formTitle.Text = "Feedback Details";
            // 
            // jobLabel
            // 
            jobLabel.AutoSize = true;
            jobLabel.Location = new System.Drawing.Point(20, 52);
            jobLabel.Name = "jobLabel";
            jobLabel.Size = new System.Drawing.Size(146, 15);
            jobLabel.TabIndex = 1;
            jobLabel.Text = "Job Order (select job) *:";
            // 
            // jobText
            // 
            jobText.Location = new System.Drawing.Point(20, 72);
            jobText.Name = "jobText";
            jobText.ReadOnly = true;
            jobText.BackColor = System.Drawing.Color.White;
            jobText.PlaceholderText = "Click [ 🔍 Browse... ] to select job order...";
            jobText.Size = new System.Drawing.Size(350, 23);
            jobText.TabIndex = 2;
            // 
            // browseJobButton
            // 
            browseJobButton.Location = new System.Drawing.Point(375, 71);
            browseJobButton.Name = "browseJobButton";
            browseJobButton.Size = new System.Drawing.Size(76, 25);
            browseJobButton.TabIndex = 3;
            browseJobButton.Text = "🔍 Browse";
            browseJobButton.UseVisualStyleBackColor = true;
            // 
            // clearJobButton
            // 
            clearJobButton.Location = new System.Drawing.Point(454, 71);
            clearJobButton.Name = "clearJobButton";
            clearJobButton.Size = new System.Drawing.Size(26, 25);
            clearJobButton.TabIndex = 4;
            clearJobButton.Text = "✕";
            clearJobButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            clearJobButton.UseVisualStyleBackColor = true;
            // 
            // customerLabel
            // 
            customerLabel.AutoSize = true;
            customerLabel.Location = new System.Drawing.Point(20, 106);
            customerLabel.Name = "customerLabel";
            customerLabel.Size = new System.Drawing.Size(161, 15);
            customerLabel.TabIndex = 3;
            customerLabel.Text = "Customer (derived from job):";
            // 
            // customerText
            // 
            customerText.Location = new System.Drawing.Point(20, 126);
            customerText.Name = "customerText";
            customerText.ReadOnly = true;
            customerText.Size = new System.Drawing.Size(460, 23);
            customerText.TabIndex = 4;
            // 
            // ratingLabel
            // 
            ratingLabel.AutoSize = true;
            ratingLabel.Location = new System.Drawing.Point(20, 160);
            ratingLabel.Name = "ratingLabel";
            ratingLabel.Size = new System.Drawing.Size(84, 15);
            ratingLabel.TabIndex = 5;
            ratingLabel.Text = "Rating Score *:";
            // 
            // ratingCombo
            // 
            ratingCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            ratingCombo.FormattingEnabled = true;
            ratingCombo.Items.AddRange(new object[] {
            "5 - Excellent (⭐⭐⭐⭐⭐)",
            "4 - Very Good (⭐⭐⭐⭐)",
            "3 - Good (⭐⭐⭐)",
            "2 - Fair (⭐⭐)",
            "1 - Poor (⭐)"});
            ratingCombo.Location = new System.Drawing.Point(20, 180);
            ratingCombo.Name = "ratingCombo";
            ratingCombo.Size = new System.Drawing.Size(460, 23);
            ratingCombo.TabIndex = 6;
            // 
            // positiveLabel
            // 
            positiveLabel.AutoSize = true;
            positiveLabel.Location = new System.Drawing.Point(20, 215);
            positiveLabel.Name = "positiveLabel";
            positiveLabel.Size = new System.Drawing.Size(99, 15);
            positiveLabel.TabIndex = 7;
            positiveLabel.Text = "Positive Remarks:";
            // 
            // positiveText
            // 
            positiveText.Location = new System.Drawing.Point(20, 235);
            positiveText.MaxLength = 1000;
            positiveText.Multiline = true;
            positiveText.Name = "positiveText";
            positiveText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            positiveText.Size = new System.Drawing.Size(460, 65);
            positiveText.TabIndex = 8;
            // 
            // improvementsLabel
            // 
            improvementsLabel.AutoSize = true;
            improvementsLabel.Location = new System.Drawing.Point(20, 312);
            improvementsLabel.Name = "improvementsLabel";
            improvementsLabel.Size = new System.Drawing.Size(134, 15);
            improvementsLabel.TabIndex = 9;
            improvementsLabel.Text = "Areas for Improvement:";
            // 
            // improvementsText
            // 
            improvementsText.Location = new System.Drawing.Point(20, 332);
            improvementsText.MaxLength = 1000;
            improvementsText.Multiline = true;
            improvementsText.Name = "improvementsText";
            improvementsText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            improvementsText.Size = new System.Drawing.Size(460, 65);
            improvementsText.TabIndex = 10;
            // 
            // stateLabel
            // 
            stateLabel.AutoSize = true;
            stateLabel.Location = new System.Drawing.Point(20, 410);
            stateLabel.Name = "stateLabel";
            stateLabel.Size = new System.Drawing.Size(83, 15);
            stateLabel.TabIndex = 11;
            stateLabel.Text = "Record State *:";
            // 
            // stateCombo
            // 
            stateCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            stateCombo.FormattingEnabled = true;
            stateCombo.Items.AddRange(new object[] {
            "Active",
            "Inactive"});
            stateCombo.Location = new System.Drawing.Point(20, 430);
            stateCombo.Name = "stateCombo";
            stateCombo.Size = new System.Drawing.Size(460, 23);
            stateCombo.TabIndex = 12;
            // 
            // errorLabel
            // 
            errorLabel.ForeColor = System.Drawing.Color.Red;
            errorLabel.Location = new System.Drawing.Point(20, 464);
            errorLabel.Name = "errorLabel";
            errorLabel.Size = new System.Drawing.Size(460, 32);
            errorLabel.TabIndex = 13;
            // 
            // saveButton
            // 
            saveButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            saveButton.Location = new System.Drawing.Point(240, 505);
            saveButton.Name = "saveButton";
            saveButton.Size = new System.Drawing.Size(140, 36);
            saveButton.TabIndex = 14;
            saveButton.Text = "Save Feedback";
            saveButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(390, 505);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(90, 36);
            cancelButton.TabIndex = 15;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // FeedbackDialogForm
            // 
            AcceptButton = saveButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(504, 555);
            Controls.Add(cancelButton);
            Controls.Add(saveButton);
            Controls.Add(errorLabel);
            Controls.Add(stateCombo);
            Controls.Add(stateLabel);
            Controls.Add(improvementsText);
            Controls.Add(improvementsLabel);
            Controls.Add(positiveText);
            Controls.Add(positiveLabel);
            Controls.Add(ratingCombo);
            Controls.Add(ratingLabel);
            Controls.Add(customerText);
            Controls.Add(customerLabel);
            Controls.Add(clearJobButton);
            Controls.Add(browseJobButton);
            Controls.Add(jobText);
            Controls.Add(jobLabel);
            Controls.Add(formTitle);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FeedbackDialogForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "RevCare CRM - Feedback";
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label formTitle;
        private System.Windows.Forms.Label jobLabel;
        internal System.Windows.Forms.TextBox jobText;
        internal System.Windows.Forms.Button browseJobButton;
        internal System.Windows.Forms.Button clearJobButton;
        private System.Windows.Forms.Label customerLabel;
        private System.Windows.Forms.TextBox customerText;
        private System.Windows.Forms.Label ratingLabel;
        private System.Windows.Forms.ComboBox ratingCombo;
        private System.Windows.Forms.Label positiveLabel;
        private System.Windows.Forms.TextBox positiveText;
        private System.Windows.Forms.Label improvementsLabel;
        private System.Windows.Forms.TextBox improvementsText;
        private System.Windows.Forms.Label stateLabel;
        private System.Windows.Forms.ComboBox stateCombo;
        private System.Windows.Forms.Label errorLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
