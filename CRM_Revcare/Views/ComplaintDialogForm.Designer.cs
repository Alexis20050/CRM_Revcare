namespace CRM_Revcare_
{
    partial class ComplaintDialogForm
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
            browseCustomerButton = new System.Windows.Forms.Button();
            clearCustomerButton = new System.Windows.Forms.Button();
            customerInfoLabel = new System.Windows.Forms.Label();
            customerInfoText = new System.Windows.Forms.TextBox();
            typeLabel = new System.Windows.Forms.Label();
            typeCombo = new System.Windows.Forms.ComboBox();
            statusLabel = new System.Windows.Forms.Label();
            statusCombo = new System.Windows.Forms.ComboBox();
            descLabel = new System.Windows.Forms.Label();
            descText = new System.Windows.Forms.TextBox();
            actionLabel = new System.Windows.Forms.Label();
            actionText = new System.Windows.Forms.TextBox();
            stateLabel = new System.Windows.Forms.Label();
            stateCombo = new System.Windows.Forms.ComboBox();
            urgentCheck = new System.Windows.Forms.CheckBox();
            warrantyCheck = new System.Windows.Forms.CheckBox();
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
            formTitle.Size = new System.Drawing.Size(155, 25);
            formTitle.TabIndex = 0;
            formTitle.Text = "Complaint Details";
            // 
            // jobLabel
            // 
            jobLabel.AutoSize = true;
            jobLabel.Location = new System.Drawing.Point(20, 52);
            jobLabel.Name = "jobLabel";
            jobLabel.Size = new System.Drawing.Size(185, 15);
            jobLabel.TabIndex = 1;
            jobLabel.Text = "Job Order (optional - link to job):";
            // 
            // jobText
            // 
            jobText.Location = new System.Drawing.Point(20, 72);
            jobText.Name = "jobText";
            jobText.ReadOnly = true;
            jobText.BackColor = System.Drawing.Color.White;
            jobText.PlaceholderText = "No job order (General complaint)";
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
            // customerInfoLabel
            // 
            customerInfoLabel.AutoSize = true;
            customerInfoLabel.Location = new System.Drawing.Point(20, 106);
            customerInfoLabel.Name = "customerInfoLabel";
            customerInfoLabel.Size = new System.Drawing.Size(130, 15);
            customerInfoLabel.TabIndex = 5;
            customerInfoLabel.Text = "Customer from Job:";
            // 
            // customerInfoText
            // 
            customerInfoText.Location = new System.Drawing.Point(20, 126);
            customerInfoText.Name = "customerInfoText";
            customerInfoText.ReadOnly = true;
            customerInfoText.Size = new System.Drawing.Size(460, 23);
            customerInfoText.TabIndex = 6;
            // 
            // customerLabel
            // 
            customerLabel.AutoSize = true;
            customerLabel.Location = new System.Drawing.Point(20, 160);
            customerLabel.Name = "customerLabel";
            customerLabel.Size = new System.Drawing.Size(268, 15);
            customerLabel.TabIndex = 7;
            customerLabel.Text = "Customer (choose if no job order is linked):";
            // 
            // customerText
            // 
            customerText.Location = new System.Drawing.Point(20, 180);
            customerText.Name = "customerText";
            customerText.ReadOnly = true;
            customerText.BackColor = System.Drawing.Color.White;
            customerText.PlaceholderText = "— No customer linked (Anonymous complaint) —";
            customerText.Size = new System.Drawing.Size(350, 23);
            customerText.TabIndex = 8;
            // 
            // browseCustomerButton
            // 
            browseCustomerButton.Location = new System.Drawing.Point(375, 179);
            browseCustomerButton.Name = "browseCustomerButton";
            browseCustomerButton.Size = new System.Drawing.Size(76, 25);
            browseCustomerButton.TabIndex = 9;
            browseCustomerButton.Text = "🔍 Browse";
            browseCustomerButton.UseVisualStyleBackColor = true;
            // 
            // clearCustomerButton
            // 
            clearCustomerButton.Location = new System.Drawing.Point(454, 179);
            clearCustomerButton.Name = "clearCustomerButton";
            clearCustomerButton.Size = new System.Drawing.Size(26, 25);
            clearCustomerButton.TabIndex = 10;
            clearCustomerButton.Text = "✕";
            clearCustomerButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            clearCustomerButton.UseVisualStyleBackColor = true;
            // 
            // typeLabel
            // 
            typeLabel.AutoSize = true;
            typeLabel.Location = new System.Drawing.Point(20, 214);
            typeLabel.Name = "typeLabel";
            typeLabel.Size = new System.Drawing.Size(103, 15);
            typeLabel.TabIndex = 7;
            typeLabel.Text = "Complaint Type *:";
            // 
            // typeCombo
            // 
            typeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            typeCombo.FormattingEnabled = true;
            typeCombo.Items.AddRange(new object[] {
            "Service Quality",
            "Delay in Delivery",
            "Pricing Issue",
            "Staff Behavior",
            "Other"});
            typeCombo.Location = new System.Drawing.Point(20, 234);
            typeCombo.Name = "typeCombo";
            typeCombo.Size = new System.Drawing.Size(220, 23);
            typeCombo.TabIndex = 8;
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.Location = new System.Drawing.Point(260, 214);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new System.Drawing.Size(110, 15);
            statusLabel.TabIndex = 9;
            statusLabel.Text = "Resolution Status *:";
            // 
            // statusCombo
            // 
            statusCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            statusCombo.FormattingEnabled = true;
            statusCombo.Items.AddRange(new object[] {
            "Open",
            "Investigating",
            "Resolved"});
            statusCombo.Location = new System.Drawing.Point(260, 234);
            statusCombo.Name = "statusCombo";
            statusCombo.Size = new System.Drawing.Size(220, 23);
            statusCombo.TabIndex = 10;
            // 
            // descLabel
            // 
            descLabel.AutoSize = true;
            descLabel.Location = new System.Drawing.Point(20, 269);
            descLabel.Name = "descLabel";
            descLabel.Size = new System.Drawing.Size(81, 15);
            descLabel.TabIndex = 11;
            descLabel.Text = "Description *:";
            // 
            // descText
            // 
            descText.Location = new System.Drawing.Point(20, 289);
            descText.MaxLength = 1000;
            descText.Multiline = true;
            descText.Name = "descText";
            descText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            descText.Size = new System.Drawing.Size(460, 70);
            descText.TabIndex = 12;
            // 
            // actionLabel
            // 
            actionLabel.AutoSize = true;
            actionLabel.Location = new System.Drawing.Point(20, 370);
            actionLabel.Name = "actionLabel";
            actionLabel.Size = new System.Drawing.Size(80, 15);
            actionLabel.TabIndex = 13;
            actionLabel.Text = "Action Taken:";
            // 
            // actionText
            // 
            actionText.Location = new System.Drawing.Point(20, 390);
            actionText.MaxLength = 1000;
            actionText.Multiline = true;
            actionText.Name = "actionText";
            actionText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            actionText.Size = new System.Drawing.Size(460, 65);
            actionText.TabIndex = 14;
            // 
            // stateLabel
            // 
            stateLabel.AutoSize = true;
            stateLabel.Location = new System.Drawing.Point(20, 466);
            stateLabel.Name = "stateLabel";
            stateLabel.Size = new System.Drawing.Size(83, 15);
            stateLabel.TabIndex = 15;
            stateLabel.Text = "Record State *:";
            // 
            // stateCombo
            // 
            stateCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            stateCombo.FormattingEnabled = true;
            stateCombo.Items.AddRange(new object[] {
            "Active",
            "Inactive"});
            stateCombo.Location = new System.Drawing.Point(20, 486);
            stateCombo.Name = "stateCombo";
            stateCombo.Size = new System.Drawing.Size(130, 23);
            stateCombo.TabIndex = 16;
            // 
            // urgentCheck
            // 
            urgentCheck.AutoSize = true;
            urgentCheck.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            urgentCheck.ForeColor = System.Drawing.Color.DarkRed;
            urgentCheck.Location = new System.Drawing.Point(170, 488);
            urgentCheck.Name = "urgentCheck";
            urgentCheck.Size = new System.Drawing.Size(115, 19);
            urgentCheck.TabIndex = 17;
            urgentCheck.Text = "🚨 Urgent Case";
            urgentCheck.UseVisualStyleBackColor = true;
            // 
            // warrantyCheck
            // 
            warrantyCheck.AutoSize = true;
            warrantyCheck.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            warrantyCheck.ForeColor = System.Drawing.Color.DarkGreen;
            warrantyCheck.Location = new System.Drawing.Point(305, 488);
            warrantyCheck.Name = "warrantyCheck";
            warrantyCheck.Size = new System.Drawing.Size(140, 19);
            warrantyCheck.TabIndex = 18;
            warrantyCheck.Text = "🛡️ Under Warranty";
            warrantyCheck.UseVisualStyleBackColor = true;
            // 
            // errorLabel
            // 
            errorLabel.ForeColor = System.Drawing.Color.Red;
            errorLabel.Location = new System.Drawing.Point(20, 520);
            errorLabel.Name = "errorLabel";
            errorLabel.Size = new System.Drawing.Size(460, 35);
            errorLabel.TabIndex = 17;
            // 
            // saveButton
            // 
            saveButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            saveButton.Location = new System.Drawing.Point(240, 565);
            saveButton.Name = "saveButton";
            saveButton.Size = new System.Drawing.Size(140, 36);
            saveButton.TabIndex = 18;
            saveButton.Text = "Save Complaint";
            saveButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(390, 565);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(90, 36);
            cancelButton.TabIndex = 19;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // ComplaintDialogForm
            // 
            AcceptButton = saveButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(504, 615);
            Controls.Add(cancelButton);
            Controls.Add(saveButton);
            Controls.Add(errorLabel);
            Controls.Add(urgentCheck);
            Controls.Add(warrantyCheck);
            Controls.Add(stateCombo);
            Controls.Add(stateLabel);
            Controls.Add(actionText);
            Controls.Add(actionLabel);
            Controls.Add(descText);
            Controls.Add(descLabel);
            Controls.Add(statusCombo);
            Controls.Add(statusLabel);
            Controls.Add(typeCombo);
            Controls.Add(typeLabel);
            Controls.Add(clearCustomerButton);
            Controls.Add(browseCustomerButton);
            Controls.Add(customerText);
            Controls.Add(customerLabel);
            Controls.Add(customerInfoText);
            Controls.Add(customerInfoLabel);
            Controls.Add(clearJobButton);
            Controls.Add(browseJobButton);
            Controls.Add(jobText);
            Controls.Add(jobLabel);
            Controls.Add(formTitle);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ComplaintDialogForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "RevCare CRM - Complaint";
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label formTitle;
        private System.Windows.Forms.Label jobLabel;
        internal System.Windows.Forms.TextBox jobText;
        internal System.Windows.Forms.Button browseJobButton;
        internal System.Windows.Forms.Button clearJobButton;
        private System.Windows.Forms.Label customerInfoLabel;
        private System.Windows.Forms.TextBox customerInfoText;
        private System.Windows.Forms.Label customerLabel;
        internal System.Windows.Forms.TextBox customerText;
        internal System.Windows.Forms.Button browseCustomerButton;
        internal System.Windows.Forms.Button clearCustomerButton;
        private System.Windows.Forms.Label typeLabel;
        private System.Windows.Forms.ComboBox typeCombo;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.ComboBox statusCombo;
        private System.Windows.Forms.Label descLabel;
        private System.Windows.Forms.TextBox descText;
        private System.Windows.Forms.Label actionLabel;
        private System.Windows.Forms.TextBox actionText;
        private System.Windows.Forms.Label stateLabel;
        private System.Windows.Forms.ComboBox stateCombo;
        internal System.Windows.Forms.CheckBox urgentCheck;
        internal System.Windows.Forms.CheckBox warrantyCheck;
        private System.Windows.Forms.Label errorLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
