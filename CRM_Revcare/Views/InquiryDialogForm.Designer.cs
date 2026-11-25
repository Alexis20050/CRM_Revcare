namespace CRM_Revcare_
{
    partial class InquiryDialogForm
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
            customerLabel = new System.Windows.Forms.Label();
            customerText = new System.Windows.Forms.TextBox();
            browseCustomerButton = new System.Windows.Forms.Button();
            clearCustomerButton = new System.Windows.Forms.Button();
            nameLabel = new System.Windows.Forms.Label();
            nameText = new System.Windows.Forms.TextBox();
            contactLabel = new System.Windows.Forms.Label();
            contactText = new System.Windows.Forms.TextBox();
            platformLabel = new System.Windows.Forms.Label();
            platformCombo = new System.Windows.Forms.ComboBox();
            statusLabel = new System.Windows.Forms.Label();
            statusCombo = new System.Windows.Forms.ComboBox();
            messageLabel = new System.Windows.Forms.Label();
            messageText = new System.Windows.Forms.TextBox();
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
            formTitle.Size = new System.Drawing.Size(133, 25);
            formTitle.TabIndex = 0;
            formTitle.Text = "Inquiry Details";
            // 
            // customerLabel
            // 
            customerLabel.AutoSize = true;
            customerLabel.Location = new System.Drawing.Point(20, 52);
            customerLabel.Name = "customerLabel";
            customerLabel.Size = new System.Drawing.Size(161, 15);
            customerLabel.TabIndex = 1;
            customerLabel.Text = "Registered Customer (select):";
            // 
            // customerText
            // 
            customerText.Location = new System.Drawing.Point(20, 72);
            customerText.Name = "customerText";
            customerText.ReadOnly = true;
            customerText.BackColor = System.Drawing.Color.White;
            customerText.PlaceholderText = "— Walk-in / Unregistered lead —";
            customerText.Size = new System.Drawing.Size(330, 23);
            customerText.TabIndex = 2;
            // 
            // browseCustomerButton
            // 
            browseCustomerButton.Location = new System.Drawing.Point(355, 71);
            browseCustomerButton.Name = "browseCustomerButton";
            browseCustomerButton.Size = new System.Drawing.Size(76, 25);
            browseCustomerButton.TabIndex = 3;
            browseCustomerButton.Text = "🔍 Browse";
            browseCustomerButton.UseVisualStyleBackColor = true;
            // 
            // clearCustomerButton
            // 
            clearCustomerButton.Location = new System.Drawing.Point(434, 71);
            clearCustomerButton.Name = "clearCustomerButton";
            clearCustomerButton.Size = new System.Drawing.Size(26, 25);
            clearCustomerButton.TabIndex = 4;
            clearCustomerButton.Text = "✕";
            clearCustomerButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            clearCustomerButton.UseVisualStyleBackColor = true;
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Location = new System.Drawing.Point(20, 106);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new System.Drawing.Size(104, 15);
            nameLabel.TabIndex = 3;
            nameLabel.Text = "Customer Name *:";
            // 
            // nameText
            // 
            nameText.Location = new System.Drawing.Point(20, 126);
            nameText.MaxLength = 100;
            nameText.Name = "nameText";
            nameText.Size = new System.Drawing.Size(210, 23);
            nameText.TabIndex = 4;
            // 
            // contactLabel
            // 
            contactLabel.AutoSize = true;
            contactLabel.Location = new System.Drawing.Point(250, 106);
            contactLabel.Name = "contactLabel";
            contactLabel.Size = new System.Drawing.Size(106, 15);
            contactLabel.TabIndex = 5;
            contactLabel.Text = "Contact Number *:";
            // 
            // contactText
            // 
            contactText.Location = new System.Drawing.Point(250, 126);
            contactText.MaxLength = 50;
            contactText.Name = "contactText";
            contactText.Size = new System.Drawing.Size(210, 23);
            contactText.TabIndex = 6;
            // 
            // platformLabel
            // 
            platformLabel.AutoSize = true;
            platformLabel.Location = new System.Drawing.Point(20, 160);
            platformLabel.Name = "platformLabel";
            platformLabel.Size = new System.Drawing.Size(103, 15);
            platformLabel.TabIndex = 7;
            platformLabel.Text = "Inquiry Platform *:";
            // 
            // platformCombo
            // 
            platformCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            platformCombo.FormattingEnabled = true;
            platformCombo.Items.AddRange(new object[] {
            "Messenger",
            "Walk-in",
            "Call",
            "Email",
            "Other"});
            platformCombo.Location = new System.Drawing.Point(20, 180);
            platformCombo.Name = "platformCombo";
            platformCombo.Size = new System.Drawing.Size(210, 23);
            platformCombo.TabIndex = 8;
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.Location = new System.Drawing.Point(250, 160);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new System.Drawing.Size(49, 15);
            statusLabel.TabIndex = 9;
            statusLabel.Text = "Status *:";
            // 
            // statusCombo
            // 
            statusCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            statusCombo.FormattingEnabled = true;
            statusCombo.Items.AddRange(new object[] {
            "Unanswered",
            "Responded",
            "Converted"});
            statusCombo.Location = new System.Drawing.Point(250, 180);
            statusCombo.Name = "statusCombo";
            statusCombo.Size = new System.Drawing.Size(210, 23);
            statusCombo.TabIndex = 10;
            // 
            // messageLabel
            // 
            messageLabel.AutoSize = true;
            messageLabel.Location = new System.Drawing.Point(20, 215);
            messageLabel.Name = "messageLabel";
            messageLabel.Size = new System.Drawing.Size(150, 15);
            messageLabel.TabIndex = 11;
            messageLabel.Text = "Message / Inquiry Details *:";
            // 
            // messageText
            // 
            messageText.Location = new System.Drawing.Point(20, 235);
            messageText.MaxLength = 500;
            messageText.Multiline = true;
            messageText.Name = "messageText";
            messageText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            messageText.Size = new System.Drawing.Size(440, 85);
            messageText.TabIndex = 12;
            // 
            // stateLabel
            // 
            stateLabel.AutoSize = true;
            stateLabel.Location = new System.Drawing.Point(20, 332);
            stateLabel.Name = "stateLabel";
            stateLabel.Size = new System.Drawing.Size(83, 15);
            stateLabel.TabIndex = 13;
            stateLabel.Text = "Record State *:";
            // 
            // stateCombo
            // 
            stateCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            stateCombo.FormattingEnabled = true;
            stateCombo.Items.AddRange(new object[] {
            "Active",
            "Inactive"});
            stateCombo.Location = new System.Drawing.Point(20, 352);
            stateCombo.Name = "stateCombo";
            stateCombo.Size = new System.Drawing.Size(440, 23);
            stateCombo.TabIndex = 14;
            // 
            // errorLabel
            // 
            errorLabel.ForeColor = System.Drawing.Color.Red;
            errorLabel.Location = new System.Drawing.Point(20, 388);
            errorLabel.Name = "errorLabel";
            errorLabel.Size = new System.Drawing.Size(440, 38);
            errorLabel.TabIndex = 15;
            // 
            // convertButton
            // 
            convertButton = new System.Windows.Forms.Button();
            convertButton.Location = new System.Drawing.Point(20, 440);
            convertButton.Name = "convertButton";
            convertButton.Size = new System.Drawing.Size(205, 36);
            convertButton.TabIndex = 18;
            convertButton.Text = "★ Register & Convert Lead";
            convertButton.UseVisualStyleBackColor = true;
            // 
            // saveButton
            // 
            saveButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            saveButton.Location = new System.Drawing.Point(240, 440);
            saveButton.Name = "saveButton";
            saveButton.Size = new System.Drawing.Size(120, 36);
            saveButton.TabIndex = 16;
            saveButton.Text = "Save Inquiry";
            saveButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(370, 440);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(90, 36);
            cancelButton.TabIndex = 17;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // InquiryDialogForm
            // 
            AcceptButton = saveButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(484, 496);
            Controls.Add(convertButton);
            Controls.Add(cancelButton);
            Controls.Add(saveButton);
            Controls.Add(errorLabel);
            Controls.Add(stateCombo);
            Controls.Add(stateLabel);
            Controls.Add(messageText);
            Controls.Add(messageLabel);
            Controls.Add(statusCombo);
            Controls.Add(statusLabel);
            Controls.Add(platformCombo);
            Controls.Add(platformLabel);
            Controls.Add(contactText);
            Controls.Add(contactLabel);
            Controls.Add(nameText);
            Controls.Add(nameLabel);
            Controls.Add(clearCustomerButton);
            Controls.Add(browseCustomerButton);
            Controls.Add(customerText);
            Controls.Add(customerLabel);
            Controls.Add(formTitle);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "InquiryDialogForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "RevCare CRM - Inquiry";
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label formTitle;
        private System.Windows.Forms.Label customerLabel;
        internal System.Windows.Forms.TextBox customerText;
        internal System.Windows.Forms.Button browseCustomerButton;
        internal System.Windows.Forms.Button clearCustomerButton;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox nameText;
        private System.Windows.Forms.Label contactLabel;
        private System.Windows.Forms.TextBox contactText;
        private System.Windows.Forms.Label platformLabel;
        private System.Windows.Forms.ComboBox platformCombo;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.ComboBox statusCombo;
        private System.Windows.Forms.Label messageLabel;
        private System.Windows.Forms.TextBox messageText;
        private System.Windows.Forms.Label stateLabel;
        private System.Windows.Forms.ComboBox stateCombo;
        private System.Windows.Forms.Label errorLabel;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Button cancelButton;
        internal System.Windows.Forms.Button convertButton;
    }
}
