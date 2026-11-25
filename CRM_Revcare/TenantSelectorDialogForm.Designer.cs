namespace CRM_Revcare_
{
    partial class TenantSelectorDialogForm
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
            titleLabel = new System.Windows.Forms.Label();
            descriptionLabel = new System.Windows.Forms.Label();
            workspaceList = new System.Windows.Forms.ListBox();
            enterButton = new System.Windows.Forms.Button();
            cancelButton = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            titleLabel.Location = new System.Drawing.Point(24, 20);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new System.Drawing.Size(164, 25);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "Select Workspace";
            // 
            // descriptionLabel
            // 
            descriptionLabel.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            descriptionLabel.ForeColor = System.Drawing.Color.DimGray;
            descriptionLabel.Location = new System.Drawing.Point(24, 52);
            descriptionLabel.Name = "descriptionLabel";
            descriptionLabel.Size = new System.Drawing.Size(460, 25);
            descriptionLabel.TabIndex = 1;
            descriptionLabel.Text = "Your account has access to multiple workspaces. Choose one to enter:";
            // 
            // workspaceList
            // 
            workspaceList.Font = new System.Drawing.Font("Segoe UI", 10F);
            workspaceList.IntegralHeight = false;
            workspaceList.ItemHeight = 28;
            workspaceList.Location = new System.Drawing.Point(24, 85);
            workspaceList.Name = "workspaceList";
            workspaceList.Size = new System.Drawing.Size(455, 220);
            workspaceList.TabIndex = 2;
            // 
            // enterButton
            // 
            enterButton.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            enterButton.FlatAppearance.BorderSize = 0;
            enterButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            enterButton.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            enterButton.ForeColor = System.Drawing.Color.White;
            enterButton.Location = new System.Drawing.Point(235, 325);
            enterButton.Name = "enterButton";
            enterButton.Size = new System.Drawing.Size(140, 36);
            enterButton.TabIndex = 3;
            enterButton.Text = "Enter Workspace";
            enterButton.UseVisualStyleBackColor = false;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            cancelButton.Location = new System.Drawing.Point(385, 325);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(94, 36);
            cancelButton.TabIndex = 4;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // TenantSelectorDialogForm
            // 
            AcceptButton = enterButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.White;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(504, 381);
            Controls.Add(titleLabel);
            Controls.Add(descriptionLabel);
            Controls.Add(workspaceList);
            Controls.Add(enterButton);
            Controls.Add(cancelButton);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "TenantSelectorDialogForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "RevCare - Select Workspace";
            ResumeLayout(false);
            PerformLayout();
        }

        internal System.Windows.Forms.Label titleLabel;
        internal System.Windows.Forms.Label descriptionLabel;
        internal System.Windows.Forms.ListBox workspaceList;
        internal System.Windows.Forms.Button enterButton;
        internal System.Windows.Forms.Button cancelButton;
    }
}
