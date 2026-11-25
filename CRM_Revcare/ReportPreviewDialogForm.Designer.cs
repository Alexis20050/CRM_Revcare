namespace CRM_Revcare_
{
    partial class ReportPreviewDialogForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CancelPendingReadiness();
                if (renderTimeoutTimer != null)
                {
                    renderTimeoutTimer.Dispose();
                    renderTimeoutTimer = null;
                }
                if (webBrowser != null)
                {
                    webBrowser.DocumentCompleted -= WebBrowser_DocumentCompleted;
                }
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            topToolbar = new System.Windows.Forms.Panel();
            lblTitle = new System.Windows.Forms.Label();
            lblStatus = new System.Windows.Forms.Label();
            btnPrint = new System.Windows.Forms.Button();
            btnPageSetup = new System.Windows.Forms.Button();
            btnClose = new System.Windows.Forms.Button();
            webBrowser = new System.Windows.Forms.WebBrowser();
            topToolbar.SuspendLayout();
            SuspendLayout();
            // 
            // topToolbar
            // 
            topToolbar.BackColor = System.Drawing.Color.FromArgb(246, 248, 250);
            topToolbar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            topToolbar.Controls.Add(lblTitle);
            topToolbar.Controls.Add(lblStatus);
            topToolbar.Controls.Add(btnPrint);
            topToolbar.Controls.Add(btnPageSetup);
            topToolbar.Controls.Add(btnClose);
            topToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            topToolbar.Location = new System.Drawing.Point(0, 0);
            topToolbar.Name = "topToolbar";
            topToolbar.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            topToolbar.Size = new System.Drawing.Size(900, 48);
            topToolbar.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            lblTitle.ForeColor = System.Drawing.Color.FromArgb(28, 38, 56);
            lblTitle.Location = new System.Drawing.Point(14, 13);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(133, 20);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Document Preview";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            lblStatus.ForeColor = System.Drawing.Color.FromArgb(100, 110, 125);
            lblStatus.Location = new System.Drawing.Point(360, 16);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(100, 15);
            lblStatus.TabIndex = 1;
            lblStatus.Text = "Loading preview...";
            // 
            // btnPrint
            // 
            btnPrint.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnPrint.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnPrint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnPrint.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnPrint.ForeColor = System.Drawing.Color.White;
            btnPrint.Location = new System.Drawing.Point(580, 8);
            btnPrint.Name = "btnPrint";
            btnPrint.Size = new System.Drawing.Size(100, 30);
            btnPrint.TabIndex = 2;
            btnPrint.Text = "🖨 Print...";
            btnPrint.UseVisualStyleBackColor = false;
            // 
            // btnPageSetup
            // 
            btnPageSetup.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnPageSetup.Location = new System.Drawing.Point(688, 8);
            btnPageSetup.Name = "btnPageSetup";
            btnPageSetup.Size = new System.Drawing.Size(95, 30);
            btnPageSetup.TabIndex = 3;
            btnPageSetup.Text = "Page Setup";
            btnPageSetup.UseVisualStyleBackColor = true;
            // 
            // btnClose
            // 
            btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnClose.Location = new System.Drawing.Point(791, 8);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(85, 30);
            btnClose.TabIndex = 4;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // webBrowser
            // 
            webBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            webBrowser.Location = new System.Drawing.Point(0, 48);
            webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            webBrowser.Name = "webBrowser";
            webBrowser.Size = new System.Drawing.Size(900, 672);
            webBrowser.TabIndex = 1;
            // 
            // ReportPreviewDialogForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(900, 720);
            Controls.Add(webBrowser);
            Controls.Add(topToolbar);
            Font = new System.Drawing.Font("Segoe UI", 9F);
            MinimumSize = new System.Drawing.Size(750, 500);
            Name = "ReportPreviewDialogForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "RevCare Auto Hub - Document Print Preview";
            topToolbar.ResumeLayout(false);
            topToolbar.PerformLayout();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel topToolbar;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnPrint;
        private System.Windows.Forms.Button btnPageSetup;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.WebBrowser webBrowser;
    }
}
