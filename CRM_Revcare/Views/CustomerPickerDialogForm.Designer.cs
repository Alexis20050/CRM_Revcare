namespace CRM_Revcare_
{
    partial class CustomerPickerDialogForm
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
            clearButton = new System.Windows.Forms.Button();
            subtitleLabel = new System.Windows.Forms.Label();
            titleLabel = new System.Windows.Forms.Label();
            bottomPanel = new System.Windows.Forms.Panel();
            clearCustomerButton = new System.Windows.Forms.Button();
            cancelButton = new System.Windows.Forms.Button();
            selectButton = new System.Windows.Forms.Button();
            pagingPanel = new System.Windows.Forms.Panel();
            firstPageButton = new System.Windows.Forms.Button();
            prevPageButton = new System.Windows.Forms.Button();
            pageInfoLabel = new System.Windows.Forms.Label();
            nextPageButton = new System.Windows.Forms.Button();
            lastPageButton = new System.Windows.Forms.Button();
            grid = new System.Windows.Forms.DataGridView();
            CustomerIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            FullNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ContactNumberColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            EmailColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            StatusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            topPanel.Size = new System.Drawing.Size(820, 115);
            topPanel.TabIndex = 0;
            // 
            // countLabel
            // 
            countLabel.AutoSize = true;
            countLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            countLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            countLabel.Location = new System.Drawing.Point(14, 92);
            countLabel.Name = "countLabel";
            countLabel.Size = new System.Drawing.Size(155, 15);
            countLabel.TabIndex = 3;
            countLabel.Text = "Showing 0 of 0 customers";
            // 
            // filterLayout
            // 
            filterLayout.ColumnCount = 2;
            filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            filterLayout.Controls.Add(searchBox, 0, 0);
            filterLayout.Controls.Add(clearButton, 1, 0);
            filterLayout.Dock = System.Windows.Forms.DockStyle.Top;
            filterLayout.Location = new System.Drawing.Point(14, 52);
            filterLayout.Name = "filterLayout";
            filterLayout.RowCount = 1;
            filterLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            filterLayout.Size = new System.Drawing.Size(792, 34);
            filterLayout.TabIndex = 2;
            // 
            // searchBox
            // 
            searchBox.Dock = System.Windows.Forms.DockStyle.Fill;
            searchBox.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            searchBox.Location = new System.Drawing.Point(3, 3);
            searchBox.Name = "searchBox";
            searchBox.PlaceholderText = "Search by customer name, contact number, or email...";
            searchBox.Size = new System.Drawing.Size(706, 25);
            searchBox.TabIndex = 0;
            // 
            // clearButton
            // 
            clearButton.Dock = System.Windows.Forms.DockStyle.Fill;
            clearButton.Location = new System.Drawing.Point(715, 3);
            clearButton.Name = "clearButton";
            clearButton.Size = new System.Drawing.Size(74, 28);
            clearButton.TabIndex = 1;
            clearButton.Text = "Clear";
            clearButton.UseVisualStyleBackColor = true;
            // 
            // subtitleLabel
            // 
            subtitleLabel.AutoSize = true;
            subtitleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            subtitleLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            subtitleLabel.Location = new System.Drawing.Point(14, 29);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 6);
            subtitleLabel.Size = new System.Drawing.Size(377, 23);
            subtitleLabel.TabIndex = 1;
            subtitleLabel.Text = "Search and select a customer. Double-click or press Enter to confirm.";
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            titleLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            titleLabel.Location = new System.Drawing.Point(14, 12);
            titleLabel.Name = "titleLabel";
            titleLabel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
            titleLabel.Size = new System.Drawing.Size(155, 25);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "Select Customer";
            // 
            // bottomPanel
            // 
            bottomPanel.Controls.Add(clearCustomerButton);
            bottomPanel.Controls.Add(cancelButton);
            bottomPanel.Controls.Add(selectButton);
            bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            bottomPanel.Location = new System.Drawing.Point(0, 468);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new System.Windows.Forms.Padding(14, 10, 14, 10);
            bottomPanel.Size = new System.Drawing.Size(820, 52);
            bottomPanel.TabIndex = 2;
            // 
            // clearCustomerButton
            // 
            clearCustomerButton.Dock = System.Windows.Forms.DockStyle.Left;
            clearCustomerButton.Location = new System.Drawing.Point(14, 10);
            clearCustomerButton.Name = "clearCustomerButton";
            clearCustomerButton.Size = new System.Drawing.Size(130, 32);
            clearCustomerButton.TabIndex = 2;
            clearCustomerButton.Text = "No Customer";
            clearCustomerButton.UseVisualStyleBackColor = true;
            clearCustomerButton.Visible = false;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Dock = System.Windows.Forms.DockStyle.Right;
            cancelButton.Location = new System.Drawing.Point(576, 10);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(100, 32);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // selectButton
            // 
            selectButton.Dock = System.Windows.Forms.DockStyle.Right;
            selectButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            selectButton.Location = new System.Drawing.Point(676, 10);
            selectButton.Name = "selectButton";
            selectButton.Size = new System.Drawing.Size(130, 32);
            selectButton.TabIndex = 0;
            selectButton.Text = "Select Customer";
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
            grid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(238, 242, 246);
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(238, 242, 246);
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { CustomerIdColumn, FullNameColumn, ContactNumberColumn, EmailColumn, StatusColumn });
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle3.Padding = new System.Windows.Forms.Padding(4);
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(193, 222, 255);
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            grid.DefaultCellStyle = dataGridViewCellStyle3;
            grid.Dock = System.Windows.Forms.DockStyle.Fill;
            grid.EnableHeadersVisualStyles = false;
            grid.Location = new System.Drawing.Point(0, 115);
            grid.MultiSelect = false;
            grid.Name = "grid";
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.RowTemplate.Height = 32;
            grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            grid.Size = new System.Drawing.Size(820, 353);
            grid.TabIndex = 1;
            // 
            // CustomerIdColumn
            // 
            CustomerIdColumn.DataPropertyName = "CustomerCode";
            CustomerIdColumn.FillWeight = 18F;
            CustomerIdColumn.HeaderText = "Account #";
            CustomerIdColumn.MinimumWidth = 85;
            CustomerIdColumn.Name = "CustomerIdColumn";
            CustomerIdColumn.ReadOnly = true;
            // 
            // FullNameColumn
            // 
            FullNameColumn.DataPropertyName = "FullName";
            FullNameColumn.FillWeight = 35F;
            FullNameColumn.HeaderText = "Customer Name";
            FullNameColumn.MinimumWidth = 160;
            FullNameColumn.Name = "FullNameColumn";
            FullNameColumn.ReadOnly = true;
            // 
            // ContactNumberColumn
            // 
            ContactNumberColumn.DataPropertyName = "ContactNumber";
            ContactNumberColumn.FillWeight = 25F;
            ContactNumberColumn.HeaderText = "Contact Number";
            ContactNumberColumn.MinimumWidth = 120;
            ContactNumberColumn.Name = "ContactNumberColumn";
            ContactNumberColumn.ReadOnly = true;
            // 
            // EmailColumn
            // 
            EmailColumn.DataPropertyName = "Email";
            EmailColumn.FillWeight = 30F;
            EmailColumn.HeaderText = "Email";
            EmailColumn.MinimumWidth = 140;
            EmailColumn.Name = "EmailColumn";
            EmailColumn.ReadOnly = true;
            // 
            // StatusColumn
            // 
            StatusColumn.DataPropertyName = "StatusText";
            StatusColumn.FillWeight = 15F;
            StatusColumn.HeaderText = "Status";
            StatusColumn.MinimumWidth = 75;
            StatusColumn.Name = "StatusColumn";
            StatusColumn.ReadOnly = true;
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
            // CustomerPickerDialogForm
            // 
            AcceptButton = selectButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(820, 520);
            Controls.Add(grid);
            Controls.Add(pagingPanel);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(700, 450);
            Name = "CustomerPickerDialogForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Select Customer";
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
        private System.Windows.Forms.Button clearButton;
        private System.Windows.Forms.Label countLabel;
        private System.Windows.Forms.Panel bottomPanel;
        private System.Windows.Forms.Button selectButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button clearCustomerButton;
        private System.Windows.Forms.DataGridView grid;
        internal System.Windows.Forms.Panel pagingPanel;
        internal System.Windows.Forms.Button firstPageButton;
        internal System.Windows.Forms.Button prevPageButton;
        internal System.Windows.Forms.Label pageInfoLabel;
        internal System.Windows.Forms.Button nextPageButton;
        internal System.Windows.Forms.Button lastPageButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn CustomerIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn FullNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ContactNumberColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn EmailColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn StatusColumn;
    }
}
