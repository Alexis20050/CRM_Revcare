namespace CRM_Revcare_
{
    partial class TransactionDialogForm
    {
        private System.ComponentModel.IContainer components;
        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            editorHost = new System.Windows.Forms.Panel();
            messageHost = new System.Windows.Forms.Panel();
            cancelButton = new System.Windows.Forms.Button();
            footer = new System.Windows.Forms.Panel();
            footer.SuspendLayout();
            SuspendLayout();
            editorHost.Dock = System.Windows.Forms.DockStyle.Fill;
            editorHost.Name = "EditorHost";
            editorHost.AutoScroll = true;
            messageHost.Dock = System.Windows.Forms.DockStyle.Bottom;
            messageHost.Height = 44;
            messageHost.Name = "MessageHost";
            footer.Dock = System.Windows.Forms.DockStyle.Bottom;
            footer.Height = 48;
            cancelButton.Text = "Cancel";
            cancelButton.Name = "Cancel";
            cancelButton.Size = new System.Drawing.Size(100, 30);
            cancelButton.Location = new System.Drawing.Point(760, 8);
            cancelButton.Anchor = System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top;
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            footer.Controls.Add(cancelButton);
            Controls.Add(editorHost);
            Controls.Add(messageHost);
            Controls.Add(footer);
            CancelButton = cancelButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(890, 680);
            MinimumSize = new System.Drawing.Size(890, 520);
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            Text = "Transaction Details";
            footer.ResumeLayout(false);
            ResumeLayout(false);
        }
        private System.Windows.Forms.Panel editorHost;
        private System.Windows.Forms.Panel messageHost;
        private System.Windows.Forms.Panel footer;
        private System.Windows.Forms.Button cancelButton;
    }
}
