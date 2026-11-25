#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    partial class AssignMechanicDialogForm
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
            headerPanel = new Panel();
            titleLabel = new Label();
            subtitleLabel = new Label();
            contextPanel = new Panel();
            lblJobInfo = new Label();
            lblCustomerInfo = new Label();
            lblVehicleInfo = new Label();
            lblCurrentMechanic = new Label();
            lblPolicyNotice = new Label();
            searchPanel = new Panel();
            lblSearch = new Label();
            searchBox = new TextBox();
            clearSearchButton = new Button();
            mechanicsGrid = new DataGridView();
            colName = new DataGridViewTextBoxColumn();
            colRole = new DataGridViewTextBoxColumn();
            colWorkload = new DataGridViewTextBoxColumn();
            reasonPanel = new Panel();
            lblReason = new Label();
            reasonBox = new TextBox();
            actionsPanel = new FlowLayoutPanel();
            btnAssign = new Button();
            btnCancel = new Button();
            statusLabel = new Label();

            headerPanel.SuspendLayout();
            contextPanel.SuspendLayout();
            searchPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mechanicsGrid).BeginInit();
            reasonPanel.SuspendLayout();
            actionsPanel.SuspendLayout();
            SuspendLayout();

            // headerPanel
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 65;
            headerPanel.BackColor = Color.FromArgb(241, 245, 249);
            headerPanel.Padding = new Padding(16, 10, 16, 10);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);

            // titleLabel
            titleLabel.Text = "Assign Workshop Mechanic";
            titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            titleLabel.Location = new Point(14, 8);
            titleLabel.AutoSize = true;

            // subtitleLabel
            subtitleLabel.Text = "One mechanic is responsible for the entire Job Order and all service tasks under it.";
            subtitleLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            subtitleLabel.ForeColor = Color.FromArgb(71, 85, 105);
            subtitleLabel.Location = new Point(15, 34);
            subtitleLabel.AutoSize = true;

            // contextPanel
            contextPanel.Dock = DockStyle.Top;
            contextPanel.Height = 110;
            contextPanel.Padding = new Padding(16, 8, 16, 8);
            contextPanel.BackColor = Color.FromArgb(248, 250, 252);
            contextPanel.Controls.Add(lblJobInfo);
            contextPanel.Controls.Add(lblCustomerInfo);
            contextPanel.Controls.Add(lblVehicleInfo);
            contextPanel.Controls.Add(lblCurrentMechanic);
            contextPanel.Controls.Add(lblPolicyNotice);

            lblJobInfo.Location = new Point(16, 8);
            lblJobInfo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblJobInfo.AutoSize = true;

            lblCustomerInfo.Location = new Point(16, 28);
            lblCustomerInfo.AutoSize = true;

            lblVehicleInfo.Location = new Point(320, 28);
            lblVehicleInfo.AutoSize = true;

            lblCurrentMechanic.Location = new Point(16, 50);
            lblCurrentMechanic.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblCurrentMechanic.ForeColor = Color.FromArgb(15, 23, 42);
            lblCurrentMechanic.AutoSize = true;

            lblPolicyNotice.Location = new Point(16, 75);
            lblPolicyNotice.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblPolicyNotice.ForeColor = Color.FromArgb(100, 116, 139);
            lblPolicyNotice.Text = "Only active Lead Mechanics, Automotive Mechanics, and Detailing Technicians with matching specialties may be assigned.";
            lblPolicyNotice.AutoSize = true;

            // searchPanel
            searchPanel.Dock = DockStyle.Top;
            searchPanel.Height = 44;
            searchPanel.Padding = new Padding(16, 8, 16, 4);
            searchPanel.Controls.Add(lblSearch);
            searchPanel.Controls.Add(searchBox);
            searchPanel.Controls.Add(clearSearchButton);

            lblSearch.Text = "Search Mechanic:";
            lblSearch.Location = new Point(16, 12);
            lblSearch.AutoSize = true;

            searchBox.Location = new Point(135, 9);
            searchBox.Size = new Size(260, 23);
            searchBox.PlaceholderText = "Filter by mechanic name or role...";

            clearSearchButton.Text = "Clear";
            clearSearchButton.Location = new Point(402, 8);
            clearSearchButton.Size = new Size(60, 25);
            clearSearchButton.UseVisualStyleBackColor = true;

            // mechanicsGrid
            mechanicsGrid.Dock = DockStyle.Fill;
            mechanicsGrid.ReadOnly = true;
            mechanicsGrid.AllowUserToAddRows = false;
            mechanicsGrid.AllowUserToDeleteRows = false;
            mechanicsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            mechanicsGrid.MultiSelect = false;
            mechanicsGrid.AutoGenerateColumns = false;
            mechanicsGrid.RowHeadersVisible = false;
            mechanicsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            mechanicsGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            mechanicsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            mechanicsGrid.ColumnHeadersHeight = 42;
            mechanicsGrid.AllowUserToResizeColumns = true;
            mechanicsGrid.AllowUserToResizeRows = false;
            mechanicsGrid.AllowUserToOrderColumns = false;
            mechanicsGrid.ScrollBars = ScrollBars.Both;
            mechanicsGrid.RowTemplate.Height = 36;
            mechanicsGrid.ShowCellToolTips = true;

            colName.DataPropertyName = "DisplayName";
            colName.HeaderText = "Mechanic Name";
            colName.Width = 220;
            colName.MinimumWidth = 120;
            colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            colRole.DataPropertyName = "Role";
            colRole.HeaderText = "Specialty / Role";
            colRole.Width = 180;
            colRole.MinimumWidth = 100;
            colRole.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            colWorkload.DataPropertyName = "WorkloadText";
            colWorkload.HeaderText = "Active Unfinished Jobs";
            colWorkload.Width = 160;
            colWorkload.MinimumWidth = 90;
            colWorkload.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

            mechanicsGrid.Columns.AddRange(new DataGridViewColumn[] { colName, colRole, colWorkload });

            // reasonPanel
            reasonPanel.Dock = DockStyle.Bottom;
            reasonPanel.Height = 70;
            reasonPanel.Padding = new Padding(16, 6, 16, 6);
            reasonPanel.Controls.Add(lblReason);
            reasonPanel.Controls.Add(reasonBox);

            lblReason.Text = "Reassignment Reason (Required if changing mechanic):";
            lblReason.Location = new Point(16, 6);
            lblReason.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblReason.AutoSize = true;

            reasonBox.Location = new Point(16, 26);
            reasonBox.Size = new Size(580, 36);
            reasonBox.Multiline = true;
            reasonBox.PlaceholderText = "Explain why this job is being reassigned (e.g. shift change, specialty required)...";

            // actionsPanel
            actionsPanel.Dock = DockStyle.Bottom;
            actionsPanel.Height = 50;
            actionsPanel.FlowDirection = FlowDirection.RightToLeft;
            actionsPanel.Padding = new Padding(16, 8, 16, 8);
            actionsPanel.Controls.Add(btnCancel);
            actionsPanel.Controls.Add(btnAssign);
            actionsPanel.Controls.Add(statusLabel);

            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(85, 32);
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.UseVisualStyleBackColor = true;

            btnAssign.Text = "Assign Mechanic";
            btnAssign.Size = new Size(150, 32);
            btnAssign.BackColor = Color.FromArgb(37, 99, 235);
            btnAssign.ForeColor = Color.White;
            btnAssign.UseVisualStyleBackColor = false;

            statusLabel.AutoSize = true;
            statusLabel.ForeColor = Color.Firebrick;
            statusLabel.Padding = new Padding(0, 8, 16, 0);

            // AssignMechanicDialogForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(620, 520);
            MinimumSize = new Size(550, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Text = "Assign Mechanic to Job Order";

            Controls.Add(mechanicsGrid);
            Controls.Add(reasonPanel);
            Controls.Add(searchPanel);
            Controls.Add(contextPanel);
            Controls.Add(headerPanel);
            Controls.Add(actionsPanel);

            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            contextPanel.ResumeLayout(false);
            contextPanel.PerformLayout();
            searchPanel.ResumeLayout(false);
            searchPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)mechanicsGrid).EndInit();
            reasonPanel.ResumeLayout(false);
            reasonPanel.PerformLayout();
            actionsPanel.ResumeLayout(false);
            actionsPanel.PerformLayout();
            ResumeLayout(false);
        }

        private Panel headerPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Panel contextPanel;
        private Label lblJobInfo;
        private Label lblCustomerInfo;
        private Label lblVehicleInfo;
        private Label lblCurrentMechanic;
        private Label lblPolicyNotice;
        private Panel searchPanel;
        private Label lblSearch;
        private TextBox searchBox;
        private Button clearSearchButton;
        private DataGridView mechanicsGrid;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colRole;
        private DataGridViewTextBoxColumn colWorkload;
        private Panel reasonPanel;
        private Label lblReason;
        private TextBox reasonBox;
        private FlowLayoutPanel actionsPanel;
        private Button btnAssign;
        private Button btnCancel;
        private Label statusLabel;
    }
}
