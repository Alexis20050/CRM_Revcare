namespace CRM_Revcare_
{
    partial class FeedbackPage
    {
        private System.Windows.Forms.DataGridViewTextBoxColumn FeedbackIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn JobOrderIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn RatingScoreColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn PositiveRemarksColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn AreasForImprovementColumn;
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
            FeedbackIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            JobOrderIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            RatingScoreColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            PositiveRemarksColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            AreasForImprovementColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
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
            newButton.Text = "+ Add Feedback";
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
            resultLabel.Size = new System.Drawing.Size(115, 15);
            resultLabel.TabIndex = 4;
            resultLabel.Text = "Loading feedback...";
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
            FeedbackIdColumn,
            JobOrderIdColumn,
            RatingScoreColumn,
            PositiveRemarksColumn,
            AreasForImprovementColumn,
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
            // FeedbackIdColumn
            // 
            FeedbackIdColumn.DataPropertyName = "FeedbackId";
            FeedbackIdColumn.Width = 125;
            FeedbackIdColumn.MinimumWidth = 80;
            FeedbackIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            FeedbackIdColumn.HeaderText = "Feedback Id";
            FeedbackIdColumn.Name = "FeedbackId";
            FeedbackIdColumn.ReadOnly = true;
            // 
            // JobOrderIdColumn
            // 
            JobOrderIdColumn.DataPropertyName = "JobOrderId";
            JobOrderIdColumn.Width = 300;
            JobOrderIdColumn.MinimumWidth = 150;
            JobOrderIdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            JobOrderIdColumn.HeaderText = "Job Order";
            JobOrderIdColumn.Name = "JobOrderId";
            JobOrderIdColumn.ReadOnly = true;
            // 
            // RatingScoreColumn
            // 
            RatingScoreColumn.DataPropertyName = "RatingScore";
            RatingScoreColumn.Width = 150;
            RatingScoreColumn.MinimumWidth = 90;
            RatingScoreColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            RatingScoreColumn.HeaderText = "Rating";
            RatingScoreColumn.Name = "RatingScore";
            RatingScoreColumn.ReadOnly = true;
            // 
            // PositiveRemarksColumn
            // 
            PositiveRemarksColumn.DataPropertyName = "PositiveRemarks";
            PositiveRemarksColumn.Width = 350;
            PositiveRemarksColumn.MinimumWidth = 150;
            PositiveRemarksColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            PositiveRemarksColumn.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            PositiveRemarksColumn.HeaderText = "Positive Remarks";
            PositiveRemarksColumn.Name = "PositiveRemarks";
            PositiveRemarksColumn.ReadOnly = true;
            // 
            // AreasForImprovementColumn
            // 
            AreasForImprovementColumn.DataPropertyName = "AreasForImprovement";
            AreasForImprovementColumn.Width = 350;
            AreasForImprovementColumn.MinimumWidth = 150;
            AreasForImprovementColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            AreasForImprovementColumn.DefaultCellStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            AreasForImprovementColumn.HeaderText = "Areas For Improvement";
            AreasForImprovementColumn.Name = "AreasForImprovement";
            AreasForImprovementColumn.ReadOnly = true;
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
            // FeedbackPage
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
            search.Name = "FeedbackSearch";
            search.Location = new System.Drawing.Point(62, 7);
            search.Size = new System.Drawing.Size(320, 24);
            search.PlaceholderText = "Search feedback #, job #, rating, remarks, improvements...";
            searchButton.Name = "SearchFeedbackButton";
            searchButton.Text = "Search";
            searchButton.Location = new System.Drawing.Point(388, 6);
            searchButton.Size = new System.Drawing.Size(75, 26);
            searchButton.UseVisualStyleBackColor = true;
            clearButton.Name = "ClearFeedbackSearch";
            clearButton.Text = "Clear";
            clearButton.Location = new System.Drawing.Point(469, 6);
            clearButton.Size = new System.Drawing.Size(65, 26);
            clearButton.UseVisualStyleBackColor = true;
            countLabel.Name = "FeedbackCountLabel";
            countLabel.Text = "Showing 0 of 0 feedback records";
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
            Name = "FeedbackPage";
            Size = new System.Drawing.Size(1000, 640);
            toolbarPanel.ResumeLayout(false);
            toolbarPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)recordsGrid).EndInit();
            ResumeLayout(false);
        }
    }
}
