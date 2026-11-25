namespace CRM_Revcare_
{
    partial class JobOrderPickerDialogForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            topPanel = new System.Windows.Forms.Panel();
            countLabel = new System.Windows.Forms.Label();
            filterLayout = new System.Windows.Forms.TableLayoutPanel();
            searchBox = new System.Windows.Forms.TextBox();
            statusFilter = new System.Windows.Forms.ComboBox();
            clearButton = new System.Windows.Forms.Button();
            subtitleLabel = new System.Windows.Forms.Label();
            titleLabel = new System.Windows.Forms.Label();
            bottomPanel = new System.Windows.Forms.Panel();
            clearJobButton = new System.Windows.Forms.Button();
            cancelButton = new System.Windows.Forms.Button();
            selectButton = new System.Windows.Forms.Button();
            pagingPanel = new System.Windows.Forms.Panel();
            firstPageButton = new System.Windows.Forms.Button();
            prevPageButton = new System.Windows.Forms.Button();
            pageInfoLabel = new System.Windows.Forms.Label();
            nextPageButton = new System.Windows.Forms.Button();
            lastPageButton = new System.Windows.Forms.Button();
            grid = new System.Windows.Forms.DataGridView();
            JobOrderIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            CustomerNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ContactNumberColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            VehicleReferenceColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            StatusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            StartedAtColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            topPanel.SuspendLayout();
            filterLayout.SuspendLayout();
            bottomPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
            SuspendLayout();
            // 
            // topPanel
            // 
            topPanel.Controls.Add(countLabel);
            topPanel.Controls.Add(filterLayout);
            topPanel.Controls.Add(subtitleLabel);
            topPanel.Controls.Add(titleLabel);
            topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            topPanel.Location = new System.Drawing.Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Padding = new System.Windows.Forms.Padding(14, 12, 14, 8);
            topPanel.Size = new System.Drawing.Size(960, 115);
            topPanel.TabIndex = 0;
            // 
            // countLabel
            // 
            countLabel.AutoSize = true;
            countLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            countLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            countLabel.Location = new System.Drawing.Point(14, 92);
            countLabel.Name = "countLabel";
            countLabel.Size = new System.Drawing.Size(140, 15);
            countLabel.TabIndex = 3;
            countLabel.Text = "Showing 0 of 0 job orders";
            // 
            // filterLayout
            // 
            filterLayout.ColumnCount = 3;
            filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            filterLayout.Controls.Add(searchBox, 0, 0);
            filterLayout.Controls.Add(statusFilter, 1, 0);
            filterLayout.Controls.Add(clearButton, 2, 0);
            filterLayout.Dock = System.Windows.Forms.DockStyle.Top;
            filterLayout.Location = new System.Drawing.Point(14, 52);
            filterLayout.Name = "filterLayout";
            filterLayout.RowCount = 1;
            filterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            filterLayout.Size = new System.Drawing.Size(932, 34);
            filterLayout.TabIndex = 2;
            // 
            // searchBox
            // 
            searchBox.Dock = System.Windows.Forms.DockStyle.Fill;
            searchBox.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            searchBox.Location = new System.Drawing.Point(3, 3);
            searchBox.Name = "searchBox";
            searchBox.PlaceholderText = "Search by Job #, customer name, contact number, or vehicle / estimate...";
            searchBox.Size = new System.Drawing.Size(691, 25);
            searchBox.TabIndex = 0;
            // 
            // statusFilter
            // 
            statusFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            statusFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            statusFilter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            statusFilter.FormattingEnabled = true;
            statusFilter.Items.AddRange(new object[] { "All jobs", "Waiting", "In Progress", "Completed", "Released" });
            statusFilter.Location = new System.Drawing.Point(700, 3);
            statusFilter.Name = "statusFilter";
            statusFilter.Size = new System.Drawing.Size(154, 25);
            statusFilter.TabIndex = 1;
            // 
            // clearButton
            // 
            clearButton.Dock = System.Windows.Forms.DockStyle.Fill;
            clearButton.Location = new System.Drawing.Point(860, 3);
            clearButton.Name = "clearButton";
            clearButton.Size = new System.Drawing.Size(69, 28);
            clearButton.TabIndex = 2;
            clearButton.Text = "Clear";
            clearButton.UseVisualStyleBackColor = true;
            // 
            // subtitleLabel
            // 
            subtitleLabel.AutoSize = true;
            subtitleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            subtitleLabel.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            subtitleLabel.Location = new System.Drawing.Point(14, 32);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 5);
            subtitleLabel.Size = new System.Drawing.Size(437, 20);
            subtitleLabel.TabIndex = 1;
            subtitleLabel.Text = "Search and select a Job Order from the list below. Double-click a row to confirm.";
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            titleLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            titleLabel.Location = new System.Drawing.Point(14, 12);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new System.Drawing.Size(142, 20);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "Select a Job Order";
            // 
            // bottomPanel
            // 
            bottomPanel.Controls.Add(clearJobButton);
            bottomPanel.Controls.Add(cancelButton);
            bottomPanel.Controls.Add(selectButton);
            bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            bottomPanel.Location = new System.Drawing.Point(0, 485);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new System.Windows.Forms.Padding(14, 10, 14, 10);
            bottomPanel.Size = new System.Drawing.Size(960, 52);
            bottomPanel.TabIndex = 2;
            // 
            // clearJobButton
            // 
            clearJobButton.Location = new System.Drawing.Point(14, 10);
            clearJobButton.Name = "clearJobButton";
            clearJobButton.Size = new System.Drawing.Size(180, 32);
            clearJobButton.TabIndex = 2;
            clearJobButton.Text = "No Job Order (General)";
            clearJobButton.UseVisualStyleBackColor = true;
            clearJobButton.Visible = false;
            // 
            // cancelButton
            // 
            cancelButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Location = new System.Drawing.Point(740, 10);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(95, 32);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // selectButton
            // 
            selectButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            selectButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            selectButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            selectButton.Location = new System.Drawing.Point(843, 10);
            selectButton.Name = "selectButton";
            selectButton.Size = new System.Drawing.Size(105, 32);
            selectButton.TabIndex = 0;
            selectButton.Text = "Select";
            selectButton.UseVisualStyleBackColor = true;
            // 
            // grid
            // 
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
            grid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            grid.BackgroundColor = System.Drawing.SystemColors.Window;
            grid.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            grid.ColumnHeadersHeight = 35;
            grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { JobOrderIdColumn, CustomerNameColumn, ContactNumberColumn, VehicleReferenceColumn, StatusColumn, StartedAtColumn });
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            grid.DefaultCellStyle = dataGridViewCellStyle3;
            grid.Dock = System.Windows.Forms.DockStyle.Fill;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = System.Drawing.SystemColors.ControlLight;
            grid.Location = new System.Drawing.Point(0, 115);
            grid.MultiSelect = false;
            grid.Name = "grid";
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.RowTemplate.Height = 30;
            grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            grid.Size = new System.Drawing.Size(960, 370);
            grid.TabIndex = 1;
            // 
            // JobOrderIdColumn
            // 
            JobOrderIdColumn.DataPropertyName = "JobOrderId";
            JobOrderIdColumn.FillWeight = 11F;
            JobOrderIdColumn.HeaderText = "Job #";
            JobOrderIdColumn.MinimumWidth = 65;
            JobOrderIdColumn.Name = "JobOrderIdColumn";
            JobOrderIdColumn.ReadOnly = true;
            // 
            // CustomerNameColumn
            // 
            CustomerNameColumn.DataPropertyName = "CustomerName";
            CustomerNameColumn.FillWeight = 23F;
            CustomerNameColumn.HeaderText = "Customer Name";
            CustomerNameColumn.MinimumWidth = 140;
            CustomerNameColumn.Name = "CustomerNameColumn";
            CustomerNameColumn.ReadOnly = true;
            // 
            // ContactNumberColumn
            // 
            ContactNumberColumn.DataPropertyName = "ContactNumber";
            ContactNumberColumn.FillWeight = 16F;
            ContactNumberColumn.HeaderText = "Contact #";
            ContactNumberColumn.MinimumWidth = 110;
            ContactNumberColumn.Name = "ContactNumberColumn";
            ContactNumberColumn.ReadOnly = true;
            // 
            // VehicleReferenceColumn
            // 
            VehicleReferenceColumn.DataPropertyName = "VehicleReference";
            VehicleReferenceColumn.FillWeight = 29F;
            VehicleReferenceColumn.HeaderText = "Vehicle / Estimate";
            VehicleReferenceColumn.MinimumWidth = 180;
            VehicleReferenceColumn.Name = "VehicleReferenceColumn";
            VehicleReferenceColumn.ReadOnly = true;
            // 
            // StatusColumn
            // 
            StatusColumn.DataPropertyName = "Status";
            StatusColumn.FillWeight = 14F;
            StatusColumn.HeaderText = "Status";
            StatusColumn.MinimumWidth = 95;
            StatusColumn.Name = "StatusColumn";
            StatusColumn.ReadOnly = true;
            // 
            // StartedAtColumn
            // 
            StartedAtColumn.DataPropertyName = "StartedAtFormatted";
            StartedAtColumn.FillWeight = 17F;
            StartedAtColumn.HeaderText = "Started Date";
            StartedAtColumn.MinimumWidth = 120;
            StartedAtColumn.Name = "StartedAtColumn";
            StartedAtColumn.ReadOnly = true;
            // 
            // pagingPanel
            // 
            pagingPanel.BackColor = System.Drawing.Color.FromArgb(246, 248, 250);
            pagingPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            pagingPanel.Height = 36;
            pagingPanel.Padding = new System.Windows.Forms.Padding(14, 4, 14, 4);
            var pagingFlow = new System.Windows.Forms.FlowLayoutPanel();
            pagingFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            pagingFlow.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            pagingFlow.WrapContents = false;

            firstPageButton.Text = "« First";
            firstPageButton.Size = new System.Drawing.Size(65, 26);
            firstPageButton.FlatStyle = System.Windows.Forms.FlatStyle.System;

            prevPageButton.Text = "‹ Prev";
            prevPageButton.Size = new System.Drawing.Size(65, 26);
            prevPageButton.FlatStyle = System.Windows.Forms.FlatStyle.System;

            pageInfoLabel.AutoSize = true;
            pageInfoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            pageInfoLabel.Padding = new System.Windows.Forms.Padding(8, 5, 8, 0);
            pageInfoLabel.Text = "Page 1 of 1";

            nextPageButton.Text = "Next ›";
            nextPageButton.Size = new System.Drawing.Size(65, 26);
            nextPageButton.FlatStyle = System.Windows.Forms.FlatStyle.System;

            lastPageButton.Text = "Last »";
            lastPageButton.Size = new System.Drawing.Size(65, 26);
            lastPageButton.FlatStyle = System.Windows.Forms.FlatStyle.System;

            pagingFlow.Controls.Add(firstPageButton);
            pagingFlow.Controls.Add(prevPageButton);
            pagingFlow.Controls.Add(pageInfoLabel);
            pagingFlow.Controls.Add(nextPageButton);
            pagingFlow.Controls.Add(lastPageButton);
            pagingPanel.Controls.Add(pagingFlow);
            // 
            // JobOrderPickerDialogForm
            // 
            AcceptButton = selectButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(960, 537);
            Controls.Add(grid);
            Controls.Add(pagingPanel);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(780, 420);
            Name = "JobOrderPickerDialogForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Select a Job Order";
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            filterLayout.ResumeLayout(false);
            filterLayout.PerformLayout();
            bottomPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)grid).EndInit();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label subtitleLabel;
        private System.Windows.Forms.TableLayoutPanel filterLayout;
        private System.Windows.Forms.TextBox searchBox;
        private System.Windows.Forms.ComboBox statusFilter;
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Label countLabel;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Button selectButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button clearJobButton;
        private System.Windows.Forms.DataGridView grid;
        internal System.Windows.Forms.Panel pagingPanel;
        internal System.Windows.Forms.Button firstPageButton;
        internal System.Windows.Forms.Button prevPageButton;
        internal System.Windows.Forms.Label pageInfoLabel;
        internal System.Windows.Forms.Button nextPageButton;
        internal System.Windows.Forms.Button lastPageButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn JobOrderIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn CustomerNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ContactNumberColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn VehicleReferenceColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn StatusColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn StartedAtColumn;
    }
}
