namespace CRM_Revcare_
{
    partial class JobTasksDialogForm
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
            lblIntakeNotes = new System.Windows.Forms.Label();
            tasksGrid = new System.Windows.Forms.DataGridView();
            TaskIdCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ServiceNameCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            StatusCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            RemarksCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            bottomPanel = new System.Windows.Forms.Panel();
            btnMarkAllDone = new System.Windows.Forms.Button();
            btnMarkDone = new System.Windows.Forms.Button();
            btnStartTask = new System.Windows.Forms.Button();
            btnPrintMechanicCopy = new System.Windows.Forms.Button();
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
            headerPanel.Controls.Add(lblIntakeNotes);
            headerPanel.Controls.Add(lblStatus);
            headerPanel.Controls.Add(lblVehicle);
            headerPanel.Controls.Add(lblCustomer);
            headerPanel.Controls.Add(lblTitle);
            headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            headerPanel.Location = new System.Drawing.Point(0, 0);
            headerPanel.Name = "headerPanel";
            headerPanel.Padding = new System.Windows.Forms.Padding(16, 10, 16, 10);
            headerPanel.Size = new System.Drawing.Size(920, 120);
            headerPanel.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            lblTitle.ForeColor = System.Drawing.Color.FromArgb(0, 120, 215);
            lblTitle.Location = new System.Drawing.Point(14, 10);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(250, 21);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Job Order # — Assigned Services";
            // 
            // lblCustomer
            // 
            lblCustomer.AutoSize = true;
            lblCustomer.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            lblCustomer.ForeColor = System.Drawing.Color.FromArgb(33, 37, 41);
            lblCustomer.Location = new System.Drawing.Point(15, 36);
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
            lblVehicle.Location = new System.Drawing.Point(15, 58);
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
            lblStatus.Location = new System.Drawing.Point(620, 14);
            lblStatus.Name = "lblStatus";
            lblStatus.RightToLeft = System.Windows.Forms.RightToLeft.No;
            lblStatus.Size = new System.Drawing.Size(120, 17);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "Status: In Progress";
            lblStatus.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblIntakeNotes
            // 
            lblIntakeNotes.AutoSize = true;
            lblIntakeNotes.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Italic);
            lblIntakeNotes.ForeColor = System.Drawing.Color.FromArgb(108, 117, 125);
            lblIntakeNotes.Location = new System.Drawing.Point(15, 80);
            lblIntakeNotes.Name = "lblIntakeNotes";
            lblIntakeNotes.Size = new System.Drawing.Size(147, 15);
            lblIntakeNotes.TabIndex = 4;
            lblIntakeNotes.Text = "Intake Instructions: None";
            // 
            // tasksGrid
            // 
            tasksGrid.AllowUserToAddRows = false;
            tasksGrid.AllowUserToDeleteRows = false;
            tasksGrid.AllowUserToResizeRows = false;
            tasksGrid.AllowUserToResizeColumns = true;
            tasksGrid.AllowUserToOrderColumns = false;
            tasksGrid.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            tasksGrid.RowTemplate.Height = 36;
            tasksGrid.ShowCellToolTips = true;
            tasksGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            tasksGrid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.None;
            tasksGrid.BackgroundColor = System.Drawing.Color.White;
            tasksGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            tasksGrid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            tasksGrid.ColumnHeadersHeight = 42;
            tasksGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            tasksGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            TaskIdCol,
            ServiceNameCol,
            StatusCol,
            RemarksCol});
            tasksGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            tasksGrid.Location = new System.Drawing.Point(0, 120);
            tasksGrid.MultiSelect = false;
            tasksGrid.Name = "tasksGrid";
            tasksGrid.ReadOnly = false;
            tasksGrid.RowHeadersVisible = false;
            tasksGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            tasksGrid.Size = new System.Drawing.Size(920, 390);
            tasksGrid.TabIndex = 1;
            // 
            // TaskIdCol
            // 
            TaskIdCol.Width = 125;
            TaskIdCol.MinimumWidth = 80;
            TaskIdCol.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            TaskIdCol.HeaderText = "Task #";
            TaskIdCol.Name = "TaskIdCol";
            TaskIdCol.ReadOnly = true;
            // 
            // ServiceNameCol
            // 
            ServiceNameCol.Width = 280;
            ServiceNameCol.MinimumWidth = 140;
            ServiceNameCol.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            ServiceNameCol.HeaderText = "Service / Task Name";
            ServiceNameCol.Name = "ServiceNameCol";
            ServiceNameCol.ReadOnly = true;
            // 
            // StatusCol
            // 
            StatusCol.Width = 130;
            StatusCol.MinimumWidth = 85;
            StatusCol.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            StatusCol.HeaderText = "Status";
            StatusCol.Name = "StatusCol";
            StatusCol.ReadOnly = true;
            // 
            // RemarksCol
            // 
            RemarksCol.Width = 300;
            RemarksCol.MinimumWidth = 140;
            RemarksCol.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            RemarksCol.HeaderText = "Mechanic Remarks / Inspection Notes (Editable)";
            RemarksCol.Name = "RemarksCol";
            RemarksCol.ReadOnly = false;
            // 
            // bottomPanel
            // 
            bottomPanel.BackColor = System.Drawing.Color.FromArgb(246, 248, 250);
            bottomPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            bottomPanel.Controls.Add(btnMarkAllDone);
            bottomPanel.Controls.Add(btnMarkDone);
            bottomPanel.Controls.Add(btnStartTask);
            bottomPanel.Controls.Add(btnPrintMechanicCopy);
            bottomPanel.Controls.Add(btnRefresh);
            bottomPanel.Controls.Add(btnClose);
            bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            bottomPanel.Location = new System.Drawing.Point(0, 510);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new System.Windows.Forms.Padding(14, 8, 14, 8);
            bottomPanel.Size = new System.Drawing.Size(960, 52);
            bottomPanel.TabIndex = 2;
            // 
            // btnMarkAllDone
            // 
            btnMarkAllDone.BackColor = System.Drawing.Color.FromArgb(22, 101, 52);
            btnMarkAllDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkAllDone.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnMarkAllDone.ForeColor = System.Drawing.Color.White;
            btnMarkAllDone.Location = new System.Drawing.Point(14, 9);
            btnMarkAllDone.Name = "btnMarkAllDone";
            btnMarkAllDone.Size = new System.Drawing.Size(145, 34);
            btnMarkAllDone.TabIndex = 0;
            btnMarkAllDone.Text = "⚡ Mark ALL Done";
            btnMarkAllDone.UseVisualStyleBackColor = false;
            // 
            // btnMarkDone
            // 
            btnMarkDone.Location = new System.Drawing.Point(165, 9);
            btnMarkDone.Name = "btnMarkDone";
            btnMarkDone.Size = new System.Drawing.Size(110, 34);
            btnMarkDone.TabIndex = 1;
            btnMarkDone.Text = "✓ Mark Done";
            btnMarkDone.UseVisualStyleBackColor = true;
            // 
            // btnStartTask
            // 
            btnStartTask.Location = new System.Drawing.Point(281, 9);
            btnStartTask.Name = "btnStartTask";
            btnStartTask.Size = new System.Drawing.Size(105, 34);
            btnStartTask.TabIndex = 2;
            btnStartTask.Text = "▶ Start Task";
            btnStartTask.UseVisualStyleBackColor = true;
            // 
            // btnPrintMechanicCopy
            // 
            btnPrintMechanicCopy.Location = new System.Drawing.Point(392, 9);
            btnPrintMechanicCopy.Name = "btnPrintMechanicCopy";
            btnPrintMechanicCopy.Size = new System.Drawing.Size(145, 34);
            btnPrintMechanicCopy.TabIndex = 4;
            btnPrintMechanicCopy.Text = "📋 Mechanic's Copy";
            btnPrintMechanicCopy.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new System.Drawing.Point(664, 9);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new System.Drawing.Size(85, 34);
            btnRefresh.TabIndex = 5;
            btnRefresh.Text = "🔄 Refresh";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnClose
            // 
            btnClose.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnClose.Location = new System.Drawing.Point(860, 9);
            btnClose.Name = "btnClose";
            btnClose.Size = new System.Drawing.Size(85, 34);
            btnClose.TabIndex = 6;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            // 
            // JobTasksDialogForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(960, 562);
            Controls.Add(tasksGrid);
            Controls.Add(bottomPanel);
            Controls.Add(headerPanel);
            Font = new System.Drawing.Font("Segoe UI", 9F);
            MinimumSize = new System.Drawing.Size(850, 480);
            Name = "JobTasksDialogForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "RevCare Auto Hub - Job Tasks (Pop-up)";
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
        private System.Windows.Forms.Label lblIntakeNotes;
        private System.Windows.Forms.DataGridView tasksGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn TaskIdCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn ServiceNameCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn StatusCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn RemarksCol;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Button btnMarkAllDone;
        private System.Windows.Forms.Button btnMarkDone;
        private System.Windows.Forms.Button btnStartTask;
        private System.Windows.Forms.Button btnPrintMechanicCopy;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnClose;
    }
}
