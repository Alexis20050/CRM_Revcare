namespace CRM_Revcare_
{
    partial class InquiryPage
    {
        private System.Windows.Forms.DataGridViewTextBoxColumn InquiryIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn CustomerNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ContactNumberColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn InquiryPlatformColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn MessageDetailsColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn StatusColumn;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IsActiveColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn CreatedAtColumn;
        internal System.Windows.Forms.Button convertButton;
        internal System.Windows.Forms.Button appointmentsButton;
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
        private System.Windows.Forms.FlowLayoutPanel toolbarPanel;
        private System.Windows.Forms.DataGridView recordsGrid;
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
            toolbarPanel = new System.Windows.Forms.FlowLayoutPanel();
            searchPanel = new System.Windows.Forms.Panel();
            searchLabel = new System.Windows.Forms.Label();
            search = new System.Windows.Forms.TextBox();
            searchButton = new System.Windows.Forms.Button();
            clearButton = new System.Windows.Forms.Button();
            countLabel = new System.Windows.Forms.Label();
            convertButton = new System.Windows.Forms.Button();
            newButton = new System.Windows.Forms.Button();
            editButton = new System.Windows.Forms.Button();
            deleteButton = new System.Windows.Forms.Button();
            refreshButton = new System.Windows.Forms.Button();
            resultLabel = new System.Windows.Forms.Label();
            recordsGrid = new System.Windows.Forms.DataGridView();
            InquiryIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CustomerNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ContactNumberColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            InquiryPlatformColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            MessageDetailsColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            StatusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            IsActiveColumn = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            CreatedAtColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            toolbarPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)recordsGrid).BeginInit();
            SuspendLayout();
            // 
            // toolbarPanel
            // 
            convertButton.Name = "ConvertInquiry";
            convertButton.Text = "Convert to Job Order";
            convertButton.Location = new System.Drawing.Point(490, 8);
            convertButton.Size = new System.Drawing.Size(165, 32);
            appointmentsButton = new System.Windows.Forms.Button();
            appointmentsButton.Name = "AppointmentsButton";
            appointmentsButton.Text = "📅 Appointments";
            appointmentsButton.Location = new System.Drawing.Point(660, 8);
            appointmentsButton.Size = new System.Drawing.Size(140, 32);
            toolbarPanel.Controls.Add(newButton);
            toolbarPanel.Controls.Add(editButton);
            toolbarPanel.Controls.Add(convertButton);
            toolbarPanel.Controls.Add(appointmentsButton);
            toolbarPanel.Controls.Add(refreshButton);
            toolbarPanel.Controls.Add(deleteButton);
            toolbarPanel.AutoSize = true;
            toolbarPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            toolbarPanel.WrapContents = true;
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
            newButton.Size = new System.Drawing.Size(120, 32);
            newButton.TabIndex = 0;
            newButton.Text = "+ Add Inquiry";
            newButton.UseVisualStyleBackColor = true;
            // 
            // editButton
            // 
            editButton.Location = new System.Drawing.Point(134, 8);
            editButton.Name = "EditRecord";
            editButton.Size = new System.Drawing.Size(110, 32);
            editButton.TabIndex = 1;
            editButton.Text = "Edit Selected";
            editButton.UseVisualStyleBackColor = true;
            // 
            // deleteButton
            // 
            deleteButton.Location = new System.Drawing.Point(250, 8);
            deleteButton.Name = "DeleteRecord";
            deleteButton.Size = new System.Drawing.Size(130, 32);
            deleteButton.TabIndex = 2;
            deleteButton.Text = "Delete / Deactivate";
            deleteButton.UseVisualStyleBackColor = true;
            // 
            // refreshButton
            // 
            refreshButton.Location = new System.Drawing.Point(386, 8);
            refreshButton.Name = "RefreshRecords";
            refreshButton.Size = new System.Drawing.Size(90, 32);
            refreshButton.TabIndex = 3;
            refreshButton.Text = "Refresh";
            refreshButton.UseVisualStyleBackColor = true;
            // 
            // resultLabel
            // 
            resultLabel.AutoSize = false;
            resultLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            resultLabel.Height = 44;
            resultLabel.Padding = new System.Windows.Forms.Padding(8);
            resultLabel.Location = new System.Drawing.Point(490, 16);
            resultLabel.Name = "Result";
            resultLabel.Size = new System.Drawing.Size(1000, 44);
            resultLabel.TabIndex = 4;
            resultLabel.Text = "Loading inquiries...";
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
            InquiryIdColumn,
            CustomerNameColumn,
            ContactNumberColumn,
            InquiryPlatformColumn,
            MessageDetailsColumn,
            StatusColumn,
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
            // InquiryIdColumn
            // 
            InquiryIdColumn.DataPropertyName = "InquiryId";
            InquiryIdColumn.Width = 125;
            InquiryIdColumn.MinimumWidth = 80;
            InquiryIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            InquiryIdColumn.HeaderText = "Inquiry Id";
            InquiryIdColumn.Name = "InquiryId";
            InquiryIdColumn.ReadOnly = true;
            // 
            // CustomerNameColumn
            // 
            CustomerNameColumn.DataPropertyName = "CustomerName";
            CustomerNameColumn.Width = 220;
            CustomerNameColumn.MinimumWidth = 120;
            CustomerNameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            CustomerNameColumn.HeaderText = "Customer Name";
            CustomerNameColumn.Name = "CustomerName";
            CustomerNameColumn.ReadOnly = true;
            // 
            // ContactNumberColumn
            // 
            ContactNumberColumn.DataPropertyName = "ContactNumber";
            ContactNumberColumn.Width = 160;
            ContactNumberColumn.MinimumWidth = 110;
            ContactNumberColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            ContactNumberColumn.HeaderText = "Contact Number";
            ContactNumberColumn.Name = "ContactNumber";
            ContactNumberColumn.ReadOnly = true;
            // 
            // InquiryPlatformColumn
            // 
            InquiryPlatformColumn.DataPropertyName = "InquiryPlatform";
            InquiryPlatformColumn.Width = 130;
            InquiryPlatformColumn.MinimumWidth = 80;
            InquiryPlatformColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            InquiryPlatformColumn.HeaderText = "Platform";
            InquiryPlatformColumn.Name = "InquiryPlatform";
            InquiryPlatformColumn.ReadOnly = true;
            // 
            // MessageDetailsColumn
            // 
            MessageDetailsColumn.DataPropertyName = "MessageDetails";
            MessageDetailsColumn.Width = 350;
            MessageDetailsColumn.MinimumWidth = 150;
            MessageDetailsColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            MessageDetailsColumn.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            MessageDetailsColumn.HeaderText = "Message Details";
            MessageDetailsColumn.Name = "MessageDetails";
            MessageDetailsColumn.ReadOnly = true;
            // 
            // StatusColumn
            // 
            StatusColumn.DataPropertyName = "Status";
            StatusColumn.Width = 130;
            StatusColumn.MinimumWidth = 80;
            StatusColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            StatusColumn.HeaderText = "Status";
            StatusColumn.Name = "Status";
            StatusColumn.ReadOnly = true;
            // 
            // IsActiveColumn
            // 
            IsActiveColumn.DataPropertyName = "IsActive";
            IsActiveColumn.FillWeight = 50F;
            IsActiveColumn.HeaderText = "Active";
            IsActiveColumn.Visible = false;
            IsActiveColumn.Name = "IsActive";
            IsActiveColumn.ReadOnly = true;
            // 
            // CreatedAtColumn
            // 
            CreatedAtColumn.DataPropertyName = "CreatedAt";
            CreatedAtColumn.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm";
            CreatedAtColumn.FillWeight = 95F;
            CreatedAtColumn.HeaderText = "Created At";
            CreatedAtColumn.Visible = false;
            CreatedAtColumn.Name = "CreatedAt";
            CreatedAtColumn.ReadOnly = true;
            // 
            // InquiryPage
            // 
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
            search.Name = "InquirySearch";
            search.Location = new System.Drawing.Point(62, 7);
            search.Size = new System.Drawing.Size(320, 24);
            search.PlaceholderText = "Search inquiry #, customer, contact, platform, message...";
            searchButton.Name = "SearchInquiryButton";
            searchButton.Text = "Search";
            searchButton.Location = new System.Drawing.Point(388, 6);
            searchButton.Size = new System.Drawing.Size(75, 26);
            searchButton.UseVisualStyleBackColor = true;
            clearButton.Name = "ClearInquirySearch";
            clearButton.Text = "Clear";
            clearButton.Location = new System.Drawing.Point(469, 6);
            clearButton.Size = new System.Drawing.Size(65, 26);
            clearButton.UseVisualStyleBackColor = true;
            countLabel.Name = "InquiryCountLabel";
            countLabel.Text = "Showing 0 of 0 inquiries";
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
            Controls.Add(resultLabel);
            Controls.Add(searchPanel);
            Controls.Add(toolbarPanel);
            Name = "InquiryPage";
            Size = new System.Drawing.Size(1000, 640);
            toolbarPanel.ResumeLayout(false);
            toolbarPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)recordsGrid).EndInit();
            ResumeLayout(false);
        }
    }
}
