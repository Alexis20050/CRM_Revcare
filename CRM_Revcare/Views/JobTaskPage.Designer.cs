namespace CRM_Revcare_
{
    partial class JobTaskPage
    {
        private System.Windows.Forms.DataGridViewTextBoxColumn JobTaskIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn JobOrderIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ServiceIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn RemarksColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn StatusColumn;
        internal System.Windows.Forms.TextBox customerInfo;
        internal System.Windows.Forms.Label customerInfoLabel;
        internal System.Windows.Forms.FlowLayoutPanel entryActions;
        internal System.Windows.Forms.Button addRecordButton;
        internal System.Windows.Forms.Button editRecordButton;
        internal System.Windows.Forms.Button printMechanicCopyButton;
        internal System.Windows.Forms.Button btnViewTasksPopUp;
        internal System.Windows.Forms.ComboBox jobFilter;
        internal System.Windows.Forms.Button clearFilter;
        internal System.Windows.Forms.Label taskCount;
        internal System.Windows.Forms.Label workflowHint;
        internal System.Windows.Forms.DataGridView jobsGrid;
        internal System.Windows.Forms.Label selectedJobLabel;
        internal System.Windows.Forms.SplitContainer workspaceSplit;
        internal System.Windows.Forms.GroupBox jobsSection;
        internal System.Windows.Forms.GroupBox tasksSection;
        internal System.Windows.Forms.Label workspaceTitle;
        internal System.Windows.Forms.FlowLayoutPanel taskActions;
        internal System.Windows.Forms.Button findJob;
        internal System.Windows.Forms.Button newestJob;
        internal System.Windows.Forms.TextBox taskSearch;
        internal System.Windows.Forms.Button searchTasksButton;
        internal System.Windows.Forms.TextBox jobDetails;
        internal System.Windows.Forms.TextBox serviceDetails;
        internal System.Windows.Forms.Panel toolbar;
        internal System.Windows.Forms.Label titleLabel;
        internal System.Windows.Forms.TableLayoutPanel filterLayout;
        internal System.Windows.Forms.ComboBox statusFilter;
        internal System.Windows.Forms.Panel pagingPanel;
        internal System.Windows.Forms.Button btnFirst;
        internal System.Windows.Forms.Button btnPrev;
        internal System.Windows.Forms.Label lblPageInfo;
        internal System.Windows.Forms.Button btnNext;
        internal System.Windows.Forms.Button btnLast;
        internal System.Windows.Forms.Label lblShow;
        internal System.Windows.Forms.ComboBox cmbPageSize;
        internal System.Windows.Forms.Label sortLabel;
        internal System.Windows.Forms.ComboBox sortCombo;
        internal System.Windows.Forms.Label workspaceSubtitle;
        internal System.Windows.Forms.Panel listPanel;
        internal System.Windows.Forms.Panel editorPanel;
        internal System.Windows.Forms.DataGridView recordsGrid;
        internal System.Windows.Forms.Label modeLabel;
        internal System.Windows.Forms.Label idLabel;
        internal System.Windows.Forms.TextBox recordIdBox;
        internal System.Windows.Forms.Button newButton;
        internal System.Windows.Forms.Button saveButton;
        internal System.Windows.Forms.Button deleteButton;
        internal System.Windows.Forms.Button refreshButton;
        internal System.Windows.Forms.Button btnMarkDone;
        internal System.Windows.Forms.Button btnStartTask;
        internal System.Windows.Forms.Button btnMarkAllDone;
        internal System.Windows.Forms.Label resultLabel;
        internal System.Windows.Forms.Panel actionsPanel;
        internal System.Windows.Forms.ComboBox order;
        internal System.Windows.Forms.Button browseJobButton;
        internal System.Windows.Forms.Label orderLabel;
        internal System.Windows.Forms.ComboBox service;
        internal System.Windows.Forms.Label serviceLabel;
        internal System.Windows.Forms.TextBox remarks;
        internal System.Windows.Forms.Label remarksLabel;
        internal System.Windows.Forms.ComboBox status;
        internal System.Windows.Forms.Label statusLabel;
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            listPanel = new System.Windows.Forms.Panel();
            editorPanel = new System.Windows.Forms.Panel();
            recordsGrid = new System.Windows.Forms.DataGridView();
            modeLabel = new System.Windows.Forms.Label();
            idLabel = new System.Windows.Forms.Label();
            recordIdBox = new System.Windows.Forms.TextBox();
            newButton = new System.Windows.Forms.Button();
            saveButton = new System.Windows.Forms.Button();
            deleteButton = new System.Windows.Forms.Button();
            refreshButton = new System.Windows.Forms.Button();
            resultLabel = new System.Windows.Forms.Label();
            actionsPanel = new System.Windows.Forms.Panel();
            order = new System.Windows.Forms.ComboBox();
            orderLabel = new System.Windows.Forms.Label();
            service = new System.Windows.Forms.ComboBox();
            serviceLabel = new System.Windows.Forms.Label();
            remarks = new System.Windows.Forms.TextBox();
            remarksLabel = new System.Windows.Forms.Label();
            status = new System.Windows.Forms.ComboBox();
            statusLabel = new System.Windows.Forms.Label();
            entryActions = new System.Windows.Forms.FlowLayoutPanel();
            addRecordButton = new System.Windows.Forms.Button();
            editRecordButton = new System.Windows.Forms.Button();
            printMechanicCopyButton = new System.Windows.Forms.Button();
            btnViewTasksPopUp = new System.Windows.Forms.Button();
            newestJob = new System.Windows.Forms.Button();
            findJob = new System.Windows.Forms.Button();
            jobFilter = new System.Windows.Forms.ComboBox();
            toolbar = new System.Windows.Forms.Panel();
            titleLabel = new System.Windows.Forms.Label();
            filterLayout = new System.Windows.Forms.TableLayoutPanel();
            taskSearch = new System.Windows.Forms.TextBox();
            searchTasksButton = new System.Windows.Forms.Button();
            statusFilter = new System.Windows.Forms.ComboBox();
            clearFilter = new System.Windows.Forms.Button();
            taskCount = new System.Windows.Forms.Label();
            workspaceSplit = new System.Windows.Forms.SplitContainer();
            jobsGrid = new System.Windows.Forms.DataGridView();
            selectedJobLabel = new System.Windows.Forms.Label();
            pagingPanel = new System.Windows.Forms.Panel();
            btnFirst = new System.Windows.Forms.Button();
            btnPrev = new System.Windows.Forms.Button();
            lblPageInfo = new System.Windows.Forms.Label();
            btnNext = new System.Windows.Forms.Button();
            btnLast = new System.Windows.Forms.Button();
            lblShow = new System.Windows.Forms.Label();
            cmbPageSize = new System.Windows.Forms.ComboBox();
            jobsSection = new System.Windows.Forms.GroupBox();
            tasksSection = new System.Windows.Forms.GroupBox();
            workspaceTitle = new System.Windows.Forms.Label();
            workspaceSubtitle = new System.Windows.Forms.Label();
            taskActions = new System.Windows.Forms.FlowLayoutPanel();
            browseJobButton = new System.Windows.Forms.Button();
            customerInfo = new System.Windows.Forms.TextBox();
            customerInfoLabel = new System.Windows.Forms.Label();
            jobDetails = new System.Windows.Forms.TextBox();
            serviceDetails = new System.Windows.Forms.TextBox();
            workflowHint = new System.Windows.Forms.Label();
            sortLabel = new System.Windows.Forms.Label();
            sortCombo = new System.Windows.Forms.ComboBox();

            ((System.ComponentModel.ISupportInitialize)recordsGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)jobsGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)workspaceSplit).BeginInit();
            workspaceSplit.Panel1.SuspendLayout();
            workspaceSplit.Panel2.SuspendLayout();
            workspaceSplit.SuspendLayout();
            listPanel.SuspendLayout();
            editorPanel.SuspendLayout();
            actionsPanel.SuspendLayout();
            entryActions.SuspendLayout();
            toolbar.SuspendLayout();
            filterLayout.SuspendLayout();
            pagingPanel.SuspendLayout();
            SuspendLayout();

            // 1. TOP ACTION BUTTONS BAR (Matches Job Orders format)
            entryActions.Name = "EntryActions";
            entryActions.Dock = System.Windows.Forms.DockStyle.Top;
            entryActions.Height = 44;
            entryActions.Padding = new System.Windows.Forms.Padding(3);
            entryActions.AutoScroll = false;
            entryActions.WrapContents = false;

            btnViewTasksPopUp.Name = "ViewTasksPopUp";
            btnViewTasksPopUp.Text = "View Tasks / Update";
            btnViewTasksPopUp.Size = new System.Drawing.Size(165, 32);
            btnViewTasksPopUp.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnViewTasksPopUp.UseVisualStyleBackColor = true;

            editRecordButton.Name = "EditRecord";
            editRecordButton.Text = "Edit Selected";
            editRecordButton.Size = new System.Drawing.Size(125, 32);
            editRecordButton.Visible = false;

            printMechanicCopyButton.Name = "PrintMechanicCopy";
            printMechanicCopyButton.Text = "🖹 Mechanic's Copy";
            printMechanicCopyButton.Size = new System.Drawing.Size(145, 32);
            printMechanicCopyButton.UseVisualStyleBackColor = true;
            printMechanicCopyButton.Enabled = false;

            newestJob.Name = "ShowNewestJob";
            newestJob.Text = "⚡ Newest job";
            newestJob.Size = new System.Drawing.Size(110, 32);
            newestJob.UseVisualStyleBackColor = true;

            findJob.Name = "FindTaskJob";
            findJob.Visible = false;
            jobFilter.Name = "TaskJobFilter";
            jobFilter.Visible = false;

            entryActions.Controls.Add(btnViewTasksPopUp);
            btnMarkDone = new System.Windows.Forms.Button();
            btnMarkDone.Name = "MarkTaskDone";
            btnMarkDone.Text = "✓ Mark Done";
            btnMarkDone.Size = new System.Drawing.Size(115, 32);
            btnMarkDone.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnMarkDone.BackColor = System.Drawing.Color.FromArgb(22, 101, 52);
            btnMarkDone.ForeColor = System.Drawing.Color.White;
            btnMarkDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkDone.Enabled = false;

            btnStartTask = new System.Windows.Forms.Button();
            btnStartTask.Name = "StartTask";
            btnStartTask.Text = "▶ Start Task";
            btnStartTask.Size = new System.Drawing.Size(105, 32);
            btnStartTask.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnStartTask.BackColor = System.Drawing.Color.FromArgb(29, 78, 216);
            btnStartTask.ForeColor = System.Drawing.Color.White;
            btnStartTask.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnStartTask.Enabled = false;

            btnMarkAllDone = new System.Windows.Forms.Button();
            btnMarkAllDone.Name = "MarkAllDone";
            btnMarkAllDone.Text = "⚡ Mark ALL Done";
            btnMarkAllDone.Size = new System.Drawing.Size(145, 32);
            btnMarkAllDone.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnMarkAllDone.BackColor = System.Drawing.Color.FromArgb(15, 118, 110);
            btnMarkAllDone.ForeColor = System.Drawing.Color.White;
            btnMarkAllDone.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnMarkAllDone.Enabled = false;

            entryActions.Controls.Add(btnMarkDone);
            entryActions.Controls.Add(btnStartTask);
            entryActions.Controls.Add(btnMarkAllDone);
            entryActions.Controls.Add(editRecordButton);
            entryActions.Controls.Add(printMechanicCopyButton);
            entryActions.Controls.Add(newestJob);
            entryActions.Controls.Add(findJob);
            entryActions.Controls.Add(jobFilter);

            // 2. TOOLBAR (Title, Filter layout, Count - Matches Job Orders format)
            toolbar.Name = "TaskOrderListTools";
            toolbar.Dock = System.Windows.Forms.DockStyle.Top;
            toolbar.Height = 112;

            titleLabel.Name = "TitleLabel";
            titleLabel.Text = "Job Tasks";
            titleLabel.Dock = System.Windows.Forms.DockStyle.Top;
            titleLabel.Height = 30;
            titleLabel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            titleLabel.ForeColor = System.Drawing.Color.FromArgb(15, 23, 42);

            filterLayout.Name = "FilterLayout";
            filterLayout.Dock = System.Windows.Forms.DockStyle.Top;
            filterLayout.MaximumSize = new System.Drawing.Size(760, 0);
            filterLayout.Height = 38;
            filterLayout.ColumnCount = 4;
            filterLayout.RowCount = 1;
            filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 155F));
            filterLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 65F));

            taskSearch.Name = "TaskSearch";
            taskSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            taskSearch.PlaceholderText = "Search job #, reference, customer or vehicle";
            taskSearch.Font = new System.Drawing.Font("Segoe UI", 9.5F);

            searchTasksButton.Name = "SearchTasksButton";
            searchTasksButton.Dock = System.Windows.Forms.DockStyle.Fill;
            searchTasksButton.Text = "Search";
            searchTasksButton.UseVisualStyleBackColor = true;

            statusFilter.Name = "TaskStatusFilter";
            statusFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            statusFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            statusFilter.Items.AddRange(new object[] { "All jobs", "Waiting", "In Progress", "Completed", "Released" });
            statusFilter.SelectedIndex = 0;

            clearFilter.Name = "AllTasks";
            clearFilter.Dock = System.Windows.Forms.DockStyle.Fill;
            clearFilter.Text = "Clear";
            clearFilter.UseVisualStyleBackColor = true;

            filterLayout.Controls.Add(taskSearch, 0, 0);
            filterLayout.Controls.Add(searchTasksButton, 1, 0);
            filterLayout.Controls.Add(statusFilter, 2, 0);
            filterLayout.Controls.Add(clearFilter, 3, 0);

            taskCount.Name = "TaskCount";
            taskCount.Dock = System.Windows.Forms.DockStyle.Bottom;
            taskCount.Height = 28;
            taskCount.Text = "Showing 0 of 0 job orders. Select a row to view or edit.";
            taskCount.Font = new System.Drawing.Font("Segoe UI", 9F);
            taskCount.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);

            toolbar.Controls.Add(filterLayout);
            toolbar.Controls.Add(titleLabel);
            toolbar.Controls.Add(taskCount);

            // 3. PAGINATION PANEL (Matches Job Orders format: First, Prev, Page Info, Next, Last, Show)
            pagingPanel.Name = "PagingPanel";
            pagingPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            pagingPanel.Height = 36;
            pagingPanel.BackColor = System.Drawing.Color.FromArgb(246, 248, 250);
            pagingPanel.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);

            var pageFlow = new System.Windows.Forms.FlowLayoutPanel
            {
                Dock = System.Windows.Forms.DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };

            btnFirst.Name = "btnFirst";
            btnFirst.Text = "« First";
            btnFirst.Size = new System.Drawing.Size(60, 26);
            btnFirst.FlatStyle = System.Windows.Forms.FlatStyle.System;
            btnFirst.Cursor = System.Windows.Forms.Cursors.Hand;

            btnPrev.Name = "btnPrev";
            btnPrev.Text = "‹ Prev";
            btnPrev.Size = new System.Drawing.Size(60, 26);
            btnPrev.FlatStyle = System.Windows.Forms.FlatStyle.System;
            btnPrev.Cursor = System.Windows.Forms.Cursors.Hand;

            lblPageInfo.Name = "lblPageInfo";
            lblPageInfo.AutoSize = true;
            lblPageInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            lblPageInfo.Padding = new System.Windows.Forms.Padding(8, 5, 8, 0);
            lblPageInfo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            lblPageInfo.Text = "Page 1 of 1 (0 records)";

            btnNext.Name = "btnNext";
            btnNext.Text = "Next ›";
            btnNext.Size = new System.Drawing.Size(60, 26);
            btnNext.FlatStyle = System.Windows.Forms.FlatStyle.System;
            btnNext.Cursor = System.Windows.Forms.Cursors.Hand;

            btnLast.Name = "btnLast";
            btnLast.Text = "Last »";
            btnLast.Size = new System.Drawing.Size(60, 26);
            btnLast.FlatStyle = System.Windows.Forms.FlatStyle.System;
            btnLast.Cursor = System.Windows.Forms.Cursors.Hand;

            lblShow.Name = "lblShow";
            lblShow.Text = "Show:";
            lblShow.AutoSize = true;
            lblShow.Padding = new System.Windows.Forms.Padding(12, 5, 4, 0);
            lblShow.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);

            cmbPageSize.Name = "cmbPageSize";
            cmbPageSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbPageSize.Width = 60;
            cmbPageSize.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            cmbPageSize.Items.AddRange(new object[] { 10, 20, 50, 100 });
            cmbPageSize.SelectedItem = 20;

            pageFlow.Controls.Add(btnFirst);
            pageFlow.Controls.Add(btnPrev);
            pageFlow.Controls.Add(lblPageInfo);
            pageFlow.Controls.Add(btnNext);
            pageFlow.Controls.Add(btnLast);
            pageFlow.Controls.Add(lblShow);
            pageFlow.Controls.Add(cmbPageSize);
            pagingPanel.Controls.Add(pageFlow);

            // 4. SELECTION HINT LABEL (Matches Job Orders format: "Select a row to edit or view details.")
            selectedJobLabel.Name = "SelectedJobSummary";
            selectedJobLabel.Text = "Select a row to edit or view details.";
            selectedJobLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            selectedJobLabel.Height = 28;
            selectedJobLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
            selectedJobLabel.ForeColor = System.Drawing.Color.FromArgb(71, 85, 105);
            selectedJobLabel.BackColor = System.Drawing.Color.White;
            selectedJobLabel.Padding = new System.Windows.Forms.Padding(4, 4, 4, 2);

            // 5. BOTTOM ACTIONS PANEL (Matches Job Orders format: Delete/Action on left, Refresh on right)
            actionsPanel.Name = "ActionsPanel";
            actionsPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            actionsPanel.Height = 46;

            refreshButton.Name = "RefreshRecords";
            refreshButton.Text = "Refresh List";
            refreshButton.Location = new System.Drawing.Point(180, 6);
            refreshButton.Size = new System.Drawing.Size(115, 32);
            refreshButton.UseVisualStyleBackColor = true;

            deleteButton.Name = "DeleteRecord";
            deleteButton.Text = "📦 Archive / Withdraw Task";
            deleteButton.Location = new System.Drawing.Point(0, 6);
            deleteButton.Size = new System.Drawing.Size(170, 32);
            deleteButton.Enabled = false;
            deleteButton.Visible = false;

            actionsPanel.Controls.Add(deleteButton);
            actionsPanel.Controls.Add(refreshButton);

            // 6. MAIN DATA GRID (jobsGrid: Full width, zebra striping, 32px height, bold headers)
            jobsGrid.Name = "JobsOverview";
            jobsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            jobsGrid.ReadOnly = true;
            jobsGrid.AllowUserToAddRows = false;
            jobsGrid.AllowUserToDeleteRows = false;
            jobsGrid.MultiSelect = false;
            jobsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            jobsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.None;
            jobsGrid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.None;
            jobsGrid.AllowUserToResizeColumns = true;
            jobsGrid.AllowUserToResizeRows = false;
            jobsGrid.AllowUserToOrderColumns = false;
            jobsGrid.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            jobsGrid.RowHeadersVisible = false;
            jobsGrid.BackgroundColor = System.Drawing.Color.White;
            jobsGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            jobsGrid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            jobsGrid.GridColor = System.Drawing.Color.FromArgb(226, 232, 240);
            jobsGrid.EnableHeadersVisualStyles = false;
            jobsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            jobsGrid.ColumnHeadersHeight = 42;
            jobsGrid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            jobsGrid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            jobsGrid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            jobsGrid.RowTemplate.Height = 36;
            jobsGrid.ShowCellToolTips = true;
            jobsGrid.DefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            jobsGrid.DefaultCellStyle.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
            jobsGrid.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(224, 242, 254);
            jobsGrid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            jobsGrid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(243, 244, 246);

            // 7. WORKSPACE SPLIT CONTAINER
            workspaceSplit.Name = "TaskWorkspace";
            workspaceSplit.Dock = System.Windows.Forms.DockStyle.Fill;
            workspaceSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
            workspaceSplit.SplitterDistance = 450;
            workspaceSplit.SplitterWidth = 4;
            workspaceSplit.Panel2Collapsed = true;
            workspaceSplit.IsSplitterFixed = true;

            // Panel1 controls: Grid in Fill, then bottom bars
            workspaceSplit.Panel1.Controls.Add(jobsGrid);
            workspaceSplit.Panel1.Controls.Add(selectedJobLabel);
            workspaceSplit.Panel1.Controls.Add(pagingPanel);
            workspaceSplit.Panel1.Controls.Add(actionsPanel);

            // Panel2 controls: Task details (preserved for background tests / legacy compatibility)
            listPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            recordsGrid.Name = "Records";
            recordsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            recordsGrid.ReadOnly = true;
            recordsGrid.AllowUserToAddRows = false;
            recordsGrid.AllowUserToDeleteRows = false;
            recordsGrid.MultiSelect = false;
            recordsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
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

            JobTaskIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "JobTaskId", DataPropertyName = "JobTaskId", HeaderText = "Job Task Id", Width = 125, MinimumWidth = 80, AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None, ReadOnly = true };
            JobOrderIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "JobOrderId", DataPropertyName = "JobOrderId", HeaderText = "Job Order Id", Width = 130, MinimumWidth = 90, AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None, ReadOnly = true };
            ServiceIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "ServiceId", DataPropertyName = "ServiceId", HeaderText = "Service / Task", Width = 240, MinimumWidth = 130, AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None, ReadOnly = true };
            RemarksColumn = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Remarks", DataPropertyName = "Remarks", HeaderText = "Remarks", Width = 300, MinimumWidth = 140, AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None, ReadOnly = true };
            StatusColumn = new System.Windows.Forms.DataGridViewTextBoxColumn { Name = "Status", DataPropertyName = "Status", HeaderText = "Status", Width = 130, MinimumWidth = 80, AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None, ReadOnly = true };
            recordsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { JobTaskIdColumn, JobOrderIdColumn, ServiceIdColumn, RemarksColumn, StatusColumn });

            listPanel.Controls.Add(recordsGrid);
            tasksSection.Name = "TasksSection";
            tasksSection.Dock = System.Windows.Forms.DockStyle.Fill;
            tasksSection.Controls.Add(listPanel);
            workspaceSplit.Panel2.Controls.Add(tasksSection);

            // Editor panel (hidden dialog template)
            editorPanel.Dock = System.Windows.Forms.DockStyle.Right;
            editorPanel.Width = 350;
            editorPanel.Visible = false;

            modeLabel.Name = "EditorMode";
            idLabel.Name = "idLabel";
            recordIdBox.Name = "RecordId";
            newButton.Name = "ClearEditor";
            newButton.Visible = false;
            saveButton.Name = "SaveRecord";
            saveButton.Text = "Save / Update Task";
            resultLabel.Name = "Result";
            resultLabel.Text = "Fill the fields and save, or select a row to edit.";
            order.Name = "JobOrderId";
            orderLabel.Name = "orderLabel";
            service.Name = "ServiceId";
            serviceLabel.Name = "serviceLabel";
            remarks.Name = "Remarks";
            remarksLabel.Name = "remarksLabel";
            status.Name = "Status";
            statusLabel.Name = "statusLabel";
            browseJobButton.Name = "browseJobButton";
            customerInfo.Name = "CustomerInfo";
            customerInfoLabel.Name = "customerInfoLabel";
            jobDetails.Name = "AssignedJobDetails";
            serviceDetails.Name = "AssignedServiceDetails";
            workflowHint.Name = "TaskWorkflowHint";

            editorPanel.Controls.Add(modeLabel);
            editorPanel.Controls.Add(idLabel);
            editorPanel.Controls.Add(recordIdBox);
            editorPanel.Controls.Add(newButton);
            editorPanel.Controls.Add(orderLabel);
            editorPanel.Controls.Add(order);
            editorPanel.Controls.Add(serviceLabel);
            editorPanel.Controls.Add(service);
            editorPanel.Controls.Add(remarksLabel);
            editorPanel.Controls.Add(remarks);
            editorPanel.Controls.Add(statusLabel);
            editorPanel.Controls.Add(status);
            editorPanel.Controls.Add(workflowHint);
            editorPanel.Controls.Add(jobDetails);
            editorPanel.Controls.Add(serviceDetails);
            editorPanel.Controls.Add(customerInfo);
            editorPanel.Controls.Add(saveButton);
            editorPanel.Controls.Add(resultLabel);

            // Extra legacy controls preserved
            jobsSection.Name = "jobsSection";
            workspaceTitle.Name = "WorkspaceTitle";
            workspaceSubtitle.Name = "WorkspaceSubtitle";
            taskActions.Name = "TaskActions";
            sortLabel.Name = "SortLabel";
            sortCombo.Name = "SortCombo";

            // Main UserControl assembly (Exact order matching JobOrderPage)
            Controls.Add(workspaceSplit);
            Controls.Add(toolbar);
            Controls.Add(entryActions);
            Controls.Add(editorPanel);

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Name = "JobTaskTab";
            Size = new System.Drawing.Size(1150, 655);
            Padding = new System.Windows.Forms.Padding(8);

            ((System.ComponentModel.ISupportInitialize)recordsGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)jobsGrid).EndInit();
            workspaceSplit.Panel1.ResumeLayout(false);
            workspaceSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)workspaceSplit).EndInit();
            workspaceSplit.ResumeLayout(false);
            listPanel.ResumeLayout(false);
            editorPanel.ResumeLayout(false);
            editorPanel.PerformLayout();
            actionsPanel.ResumeLayout(false);
            entryActions.ResumeLayout(false);
            toolbar.ResumeLayout(false);
            filterLayout.ResumeLayout(false);
            filterLayout.PerformLayout();
            pagingPanel.ResumeLayout(false);
            pagingPanel.PerformLayout();
            ResumeLayout(false);
        }
    }
}
