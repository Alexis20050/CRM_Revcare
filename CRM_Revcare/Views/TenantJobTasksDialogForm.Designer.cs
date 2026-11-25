namespace CRM_Revcare_
{
    partial class TenantJobTasksDialogForm
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
            headerPanel = new System.Windows.Forms.Panel();
            lblTitle = new System.Windows.Forms.Label();
            lblCustomer = new System.Windows.Forms.Label();
            lblVehicle = new System.Windows.Forms.Label();
            lblStatus = new System.Windows.Forms.Label();
            tasksGrid = new System.Windows.Forms.DataGridView();
            TaskIdCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ServiceNameCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            StatusCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ReviewStatusCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            AssignedToCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            RemarksCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            bottomPanel = new System.Windows.Forms.Panel();
            btnStartTask = new System.Windows.Forms.Button();
            btnFinishTask = new System.Windows.Forms.Button();
            btnSubmitReview = new System.Windows.Forms.Button();
            btnPrintWorkOrder = new System.Windows.Forms.Button();
            btnRefresh = new System.Windows.Forms.Button();
            btnClose = new System.Windows.Forms.Button();
            headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tasksGrid).BeginInit();
            bottomPanel.SuspendLayout();
            SuspendLayout();
            // 
            // headerPanel
            // 
            headerPanel.BackColor = System.Drawing.Color.FromArgb(246, 248, 250);
            headerPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            headerPanel.Controls.Add(lblStatus);
            headerPanel.Controls.Add(lblVehicle);
            headerPanel.Controls.Add(lblCustomer);
            headerPanel.Controls.Add(lblTitle);
            headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            headerPanel.Location = new System.Drawing.Point(0, 0);
            headerPanel.Name = "headerPanel";
            headerPanel.Padding = new System.Windows.Forms.Padding(16, 10, 16, 10);
            headerPanel.Size = new System.Drawing.Size(960, 95);
            headerPanel.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            lblTitle.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            lblTitle.Location = new System.Drawing.Point(14, 10);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(260, 21);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Job Order # — Assigned Services";
            // 
            // lblCustomer
            // 
            lblCustomer.AutoSize = true;
            lblCustomer.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            lblCustomer.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            lblCustomer.Location = new System.Drawing.Point(15, 38);
            lblCustomer.Name = "lblCustomer";
            lblCustomer.Size = new System.Drawing.Size(130, 17);
            lblCustomer.TabIndex = 1;
            lblCustomer.Text = "Customer: Loading...";
            // 
            // lblVehicle
            // 
            lblVehicle.AutoSize = true;
            lblVehicle.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblVehicle.ForeColor = System.Drawing.Color.FromArgb(55, 65, 81);
            lblVehicle.Location = new System.Drawing.Point(15, 62);
            lblVehicle.Name = "lblVehicle";
            lblVehicle.Size = new System.Drawing.Size(110, 15);
            lblVehicle.TabIndex = 2;
            lblVehicle.Text = "Vehicle: Loading...";
            // 
            // lblStatus
            // 
            lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lblStatus.AutoSize = true;
            lblStatus.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            lblStatus.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            lblStatus.Location = new System.Drawing.Point(680, 14);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(120, 17);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "Status: In Progress";
            lblStatus.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // tasksGrid
            // 
            tasksGrid.AllowUserToAddRows = false;
            tasksGrid.AllowUserToDeleteRows = false;
            tasksGrid.AllowUserToResizeRows = false;
            tasksGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            tasksGrid.BackgroundColor = System.Drawing.Color.White;
            tasksGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            tasksGrid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            tasksGrid.ColumnHeadersHeight = 36;
            tasksGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            tasksGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            TaskIdCol,
            ServiceNameCol,
            StatusCol,
            ReviewStatusCol,
            AssignedToCol,
            RemarksCol});
            tasksGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            tasksGrid.Location = new System.Drawing.Point(0, 95);
            tasksGrid.MultiSelect = false;
            tasksGrid.Name = "tasksGrid";
            tasksGrid.ReadOnly = true;
            tasksGrid.RowHeadersVisible = false;
            tasksGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            tasksGrid.Size = new System.Drawing.Size(960, 415);
            tasksGrid.TabIndex = 1;
            // 
            // TaskIdCol
            // 
            TaskIdCol.FillWeight = 12F;
            TaskIdCol.HeaderText = "Task #";
            TaskIdCol.MinimumWidth = 65;
            TaskIdCol.Name = "TaskIdCol";
            TaskIdCol.ReadOnly = true;
            // 
            // ServiceNameCol
            // 
            ServiceNameCol.FillWeight = 28F;
            ServiceNameCol.HeaderText = "Service / Task Name";
            ServiceNameCol.MinimumWidth = 150;
            ServiceNameCol.Name = "ServiceNameCol";
            ServiceNameCol.ReadOnly = true;
            // 
            // StatusCol
            // 
            StatusCol.FillWeight = 15F;
            StatusCol.HeaderText = "Status";
            StatusCol.MinimumWidth = 90;
            StatusCol.Name = "StatusCol";
            StatusCol.ReadOnly = true;
            // 
            // ReviewStatusCol
            // 
            ReviewStatusCol.FillWeight = 15F;
            ReviewStatusCol.HeaderText = "Review";
            ReviewStatusCol.MinimumWidth = 90;
            ReviewStatusCol.Name = "ReviewStatusCol";
            ReviewStatusCol.ReadOnly = true;
            // 
            // AssignedToCol
            // 
            AssignedToCol.FillWeight = 15F;
            AssignedToCol.HeaderText = "Assigned To";
            AssignedToCol.MinimumWidth = 95;
            AssignedToCol.Name = "AssignedToCol";
            AssignedToCol.ReadOnly = true;
            // 
            // RemarksCol
            // 
            RemarksCol.FillWeight = 30F;
            RemarksCol.HeaderText = "Remarks";
            RemarksCol.MinimumWidth = 150;
            RemarksCol.Name = "RemarksCol";
            RemarksCol.ReadOnly = true;
            // 
            // bottomPanel
            // 
            bottomPanel.BackColor = System.Drawing.Color.FromArgb(246, 248, 250);
            bottomPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            bottomPanel.Controls.Add(btnStartTask);
            bottomPanel.Controls.Add(btnFinishTask);
            bottomPanel.Controls.Add(btnSubmitReview);
            bottomPanel.Controls.Add(btnPrintWorkOrder);
            bottomPanel.Controls.Add(btnRefresh);
            bottomPanel.Controls.Add(btnClose);
            bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            bottomPanel.Location = new System.Drawing.Point(0, 510);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new System.Windows.Forms.Padding(14, 8, 14, 8);
            bottomPanel.Size = new System.Drawing.Size(960, 52);
            bottomPanel.TabIndex = 2;
            // 
            // btnStartTask
            // 
            btnStartTask.Location = new System.Drawing.Point(14, 9);
            btnStartTask.Name = "btnStartTask";
            btnStartTask.Size = new System.Drawing.Size(100, 32);
            btnStartTask.TabIndex = 0;
            btnStartTask.Text = "Start Task";
            btnStartTask.UseVisualStyleBackColor = true;
            // 
            // btnFinishTask
            // 
            btnFinishTask.Location = new System.Drawing.Point(120, 9);
            btnFinishTask.Name = "btnFinishTask";
            btnFinishTask.Size = new System.Drawing.Size(115, 32);
            btnFinishTask.TabIndex = 1;
            btnFinishTask.Text = "Mark Done";
            btnFinishTask.UseVisualStyleBackColor = true;
            // 
            // btnSubmitReview
            // 
            btnSubmitReview.Location = new System.Drawing.Point(241, 9);
            btnSubmitReview.Name = "btnSubmitReview";
            btnSubmitReview.Size = new System.Drawing.Size(130, 32);
            btnSubmitReview.TabIndex = 2;
            btnSubmitReview.Text = "Submit Review";
            btnSubmitReview.UseVisualStyleBackColor = true;
            // 
            // btnPrintWorkOrder
            // 
            btnPrintWorkOrder.Location = new System.Drawing.Point(377, 9);
            btnPrintWorkOrder.Name = "btnPrintWorkOrder";
            btnPrintWorkOrder.Size = new System.Drawing.Size(145, 32);
            btnPrintWorkOrder.TabIndex = 3;
            btnPrintWorkOrder.Text = "📋 Work Order Copy";
            btnPrintWorkOrder.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new System.Drawing.Point(528, 9);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new System.Drawing.Size(90, 32);
            btnRefresh.TabIndex = 4;
            btnRefresh.Text = "🔄 Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnClose
            // 
            btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnClose.Location = new System.Drawing.Point(858, 9);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(85, 32);
            btnClose.TabIndex = 5;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // TenantJobTasksDialogForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(960, 562);
            Controls.Add(tasksGrid);
            Controls.Add(bottomPanel);
            Controls.Add(headerPanel);
            Font = new System.Drawing.Font("Segoe UI", 9F);
            MinimumSize = new System.Drawing.Size(850, 480);
            Name = "TenantJobTasksDialogForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "RevCare Auto Hub - Workshop Tasks (Tenant A)";
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)tasksGrid).EndInit();
            bottomPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblCustomer;
        private System.Windows.Forms.Label lblVehicle;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.DataGridView tasksGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn TaskIdCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn ServiceNameCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn StatusCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn ReviewStatusCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn AssignedToCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn RemarksCol;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Button btnStartTask;
        private System.Windows.Forms.Button btnFinishTask;
        private System.Windows.Forms.Button btnSubmitReview;
        private System.Windows.Forms.Button btnPrintWorkOrder;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnClose;
    }
}
