namespace CRM_Revcare_
{
    partial class ComplaintPage
    {
        private System.Windows.Forms.DataGridViewTextBoxColumn ComplaintIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn CustomerIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn JobOrderIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ComplaintTypeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn DescriptionColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ResolutionStatusColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ActionTakenColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IsActiveColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn CreatedAtColumn;
        internal System.Windows.Forms.Button newButton;
        internal System.Windows.Forms.Button editButton;
        internal System.Windows.Forms.Button deleteButton;
        internal System.Windows.Forms.Button refreshButton;
        internal System.Windows.Forms.Label resultLabel;
        internal System.Windows.Forms.Panel searchPanel;
        internal System.Windows.Forms.Label searchLabel;
        internal System.Windows.Forms.TextBox search;
        internal System.Windows.Forms.Button searchButton;
        internal System.Windows.Forms.Button clearButton;
        internal System.Windows.Forms.Label countLabel;
        private System.Windows.Forms.Panel toolbarPanel;
        private System.Windows.Forms.DataGridView recordsGrid;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IsUrgentColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn UnderWarrantyServiceColumn;
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
            components = new System.ComponentModel.Container();
            toolbarPanel = new System.Windows.Forms.Panel();
            searchPanel = new System.Windows.Forms.Panel();
            searchLabel = new System.Windows.Forms.Label();
            search = new System.Windows.Forms.TextBox();
            searchButton = new System.Windows.Forms.Button();
            clearButton = new System.Windows.Forms.Button();
            countLabel = new System.Windows.Forms.Label();
            newButton = new System.Windows.Forms.Button();
            editButton = new System.Windows.Forms.Button();
            deleteButton = new System.Windows.Forms.Button();
            refreshButton = new System.Windows.Forms.Button();
            resultLabel = new System.Windows.Forms.Label();
            recordsGrid = new System.Windows.Forms.DataGridView();
            ComplaintIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CustomerIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            JobOrderIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ComplaintTypeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            DescriptionColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ResolutionStatusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ActionTakenColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            IsActiveColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            CreatedAtColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolbarPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)recordsGrid).BeginInit();
            SuspendLayout();
            // 
            // toolbarPanel
            // 
            toolbarPanel.Controls.Add(newButton);
            toolbarPanel.Controls.Add(editButton);
            toolbarPanel.Controls.Add(deleteButton);
            toolbarPanel.Controls.Add(refreshButton);
            toolbarPanel.Controls.Add(resultLabel);
            toolbarPanel.Dock = System.Windows.Forms.DockStyle.Top;
            toolbarPanel.Location = new System.Drawing.Point(0, 0);
            toolbarPanel.Name = "toolbarPanel";
            toolbarPanel.Padding = new System.Windows.Forms.Padding(6);
            toolbarPanel.Size = new System.Drawing.Size(1000, 48);
            toolbarPanel.TabIndex = 0;
            // 
            // newButton
            // 
            newButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            newButton.Location = new System.Drawing.Point(8, 8);
            newButton.Name = "NewRecord";
            newButton.Size = new System.Drawing.Size(130, 32);
            newButton.TabIndex = 0;
            newButton.Text = "+ Add Complaint";
            newButton.UseVisualStyleBackColor = true;
            // 
            // editButton
            // 
            editButton.Location = new System.Drawing.Point(144, 8);
            editButton.Name = "EditRecord";
            editButton.Size = new System.Drawing.Size(110, 32);
            editButton.TabIndex = 1;
            editButton.Text = "Edit Selected";
            editButton.UseVisualStyleBackColor = true;
            // 
            // deleteButton
            // 
            deleteButton.Location = new System.Drawing.Point(260, 8);
            deleteButton.Name = "DeleteRecord";
            deleteButton.Size = new System.Drawing.Size(130, 32);
            deleteButton.TabIndex = 2;
            deleteButton.Text = "Delete / Deactivate";
            deleteButton.UseVisualStyleBackColor = true;
            // 
            // refreshButton
            // 
            refreshButton.Location = new System.Drawing.Point(396, 8);
            refreshButton.Name = "RefreshRecords";
            refreshButton.Size = new System.Drawing.Size(90, 32);
            refreshButton.TabIndex = 3;
            refreshButton.Text = "Refresh";
            refreshButton.UseVisualStyleBackColor = true;
            // 
            // resultLabel
            // 
            resultLabel.AutoSize = true;
            resultLabel.Location = new System.Drawing.Point(500, 16);
            resultLabel.Name = "Result";
            resultLabel.Size = new System.Drawing.Size(125, 15);
            resultLabel.TabIndex = 4;
            resultLabel.Text = "Loading complaints...";
            // 
            // recordsGrid
            // 
            recordsGrid.AllowUserToAddRows = false;
            recordsGrid.AllowUserToDeleteRows = false;
            recordsGrid.AutoGenerateColumns = false;
            recordsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            recordsGrid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.None;
            recordsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            recordsGrid.ColumnHeadersHeight = 42;
            recordsGrid.AllowUserToResizeColumns = true;
            recordsGrid.AllowUserToResizeRows = false;
            recordsGrid.AllowUserToOrderColumns = false;
            recordsGrid.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            recordsGrid.RowTemplate.Height = 36;
            recordsGrid.ShowCellToolTips = true;
            recordsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            ComplaintIdColumn,
            CustomerIdColumn,
            JobOrderIdColumn,
            ComplaintTypeColumn,
            DescriptionColumn,
            ResolutionStatusColumn,
            ActionTakenColumn,
            IsActiveColumn,
            CreatedAtColumn});
            recordsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            recordsGrid.Location = new System.Drawing.Point(0, 48);
            recordsGrid.MultiSelect = false;
            recordsGrid.Name = "Records";
            recordsGrid.ReadOnly = true;
            recordsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            recordsGrid.Size = new System.Drawing.Size(1000, 592);
            recordsGrid.TabIndex = 1;
            // 
            // ComplaintIdColumn
            // 
            ComplaintIdColumn.DataPropertyName = "ComplaintId";
            ComplaintIdColumn.Width = 125;
            ComplaintIdColumn.MinimumWidth = 80;
            ComplaintIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            ComplaintIdColumn.HeaderText = "Complaint Id";
            ComplaintIdColumn.Name = "ComplaintId";
            ComplaintIdColumn.ReadOnly = true;
            // 
            // CustomerIdColumn
            // 
            CustomerIdColumn.DataPropertyName = "CustomerId";
            CustomerIdColumn.Width = 220;
            CustomerIdColumn.MinimumWidth = 120;
            CustomerIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            CustomerIdColumn.HeaderText = "Customer";
            CustomerIdColumn.Name = "CustomerId";
            CustomerIdColumn.ReadOnly = true;
            // 
            // JobOrderIdColumn
            // 
            JobOrderIdColumn.DataPropertyName = "JobOrderId";
            JobOrderIdColumn.Width = 160;
            JobOrderIdColumn.MinimumWidth = 100;
            JobOrderIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            JobOrderIdColumn.HeaderText = "Job Order";
            JobOrderIdColumn.Name = "JobOrderId";
            JobOrderIdColumn.ReadOnly = true;
            // 
            // ComplaintTypeColumn
            // 
            ComplaintTypeColumn.DataPropertyName = "ComplaintType";
            ComplaintTypeColumn.Width = 160;
            ComplaintTypeColumn.MinimumWidth = 100;
            ComplaintTypeColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            ComplaintTypeColumn.HeaderText = "Complaint Type";
            ComplaintTypeColumn.Name = "ComplaintType";
            ComplaintTypeColumn.ReadOnly = true;
            // 
            // DescriptionColumn
            // 
            DescriptionColumn.DataPropertyName = "Description";
            DescriptionColumn.Width = 350;
            DescriptionColumn.MinimumWidth = 150;
            DescriptionColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            DescriptionColumn.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            DescriptionColumn.HeaderText = "Description";
            DescriptionColumn.Name = "Description";
            DescriptionColumn.ReadOnly = true;
            // 
            // ResolutionStatusColumn
            // 
            ResolutionStatusColumn.DataPropertyName = "ResolutionStatus";
            ResolutionStatusColumn.Width = 130;
            ResolutionStatusColumn.MinimumWidth = 80;
            ResolutionStatusColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            ResolutionStatusColumn.HeaderText = "Status";
            ResolutionStatusColumn.Name = "ResolutionStatus";
            ResolutionStatusColumn.ReadOnly = true;
            // 
            // ActionTakenColumn
            // 
            ActionTakenColumn.DataPropertyName = "ActionTaken";
            ActionTakenColumn.Width = 260;
            ActionTakenColumn.MinimumWidth = 120;
            ActionTakenColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            ActionTakenColumn.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            ActionTakenColumn.HeaderText = "Action Taken";
            ActionTakenColumn.Name = "ActionTaken";
            ActionTakenColumn.ReadOnly = true;
            // 
            // IsActiveColumn
            // 
            IsActiveColumn.DataPropertyName = "IsActive";
            IsActiveColumn.FillWeight = 45F;
            IsActiveColumn.HeaderText = "Active";
            IsActiveColumn.Visible = false;
            IsActiveColumn.Name = "IsActive";
            IsActiveColumn.ReadOnly = true;
            // 
            // CreatedAtColumn
            // 
            CreatedAtColumn.DataPropertyName = "CreatedAt";
            CreatedAtColumn.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
            CreatedAtColumn.FillWeight = 90F;
            CreatedAtColumn.HeaderText = "Created At";
            CreatedAtColumn.Visible = false;
            CreatedAtColumn.Name = "CreatedAt";
            CreatedAtColumn.ReadOnly = true;
            // 
            // ComplaintPage
            // 
            IsUrgentColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            IsUrgentColumn.Name = "IsUrgent";
            IsUrgentColumn.DataPropertyName = "IsUrgent";
            IsUrgentColumn.HeaderText = "Urgent";
            recordsGrid.Columns.Add(IsUrgentColumn);
            UnderWarrantyServiceColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            UnderWarrantyServiceColumn.Name = "UnderWarrantyService";
            UnderWarrantyServiceColumn.DataPropertyName = "UnderWarrantyService";
            UnderWarrantyServiceColumn.HeaderText = "Under Warranty Service";
            recordsGrid.Columns.Add(UnderWarrantyServiceColumn);
            // 
            // searchPanel
            // 
            searchPanel.Dock = System.Windows.Forms.DockStyle.Top;
            searchPanel.Height = 40;
            searchPanel.Padding = new System.Windows.Forms.Padding(6);
            searchPanel.Name = "searchPanel";
            searchLabel.Text = "Search:";
            searchLabel.AutoSize = true;
            searchLabel.Location = new System.Drawing.Point(8, 10);
            search.Name = "ComplaintSearch";
            search.Location = new System.Drawing.Point(62, 7);
            search.Size = new System.Drawing.Size(320, 24);
            search.PlaceholderText = "Search complaint #, customer, job #, description, action...";
            searchButton.Name = "SearchComplaintButton";
            searchButton.Text = "Search";
            searchButton.Location = new System.Drawing.Point(388, 6);
            searchButton.Size = new System.Drawing.Size(75, 26);
            searchButton.UseVisualStyleBackColor = true;
            clearButton.Name = "ClearComplaintSearch";
            clearButton.Text = "Clear";
            clearButton.Location = new System.Drawing.Point(469, 6);
            clearButton.Size = new System.Drawing.Size(65, 26);
            clearButton.UseVisualStyleBackColor = true;
            countLabel.Name = "ComplaintCountLabel";
            countLabel.Text = "Showing 0 of 0 complaints";
            countLabel.AutoSize = true;
            countLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            countLabel.Location = new System.Drawing.Point(544, 10);
            searchPanel.Controls.Add(searchLabel);
            searchPanel.Controls.Add(search);
            searchPanel.Controls.Add(searchButton);
            searchPanel.Controls.Add(clearButton);
            searchPanel.Controls.Add(countLabel);
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(recordsGrid);
            Controls.Add(searchPanel);
            Controls.Add(toolbarPanel);
            Name = "ComplaintPage";
            Size = new System.Drawing.Size(1000, 640);
            toolbarPanel.ResumeLayout(false);
            toolbarPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)recordsGrid).EndInit();
            ResumeLayout(false);
        }
    }
}
