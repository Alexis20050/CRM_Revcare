namespace CRM_Revcare_
{
    partial class PlatformAdminForm
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
            header = new System.Windows.Forms.Panel();
            title = new System.Windows.Forms.Label();
            subtitle = new System.Windows.Forms.Label();
            signOut = new System.Windows.Forms.Button();
            tabs = new System.Windows.Forms.TabControl();
            tenantsTab = new System.Windows.Forms.TabPage();
            tenantsGrid = new System.Windows.Forms.DataGridView();
            tenantToolbar = new System.Windows.Forms.FlowLayoutPanel();
            btnRegisterTenant = new System.Windows.Forms.Button();
            btnToggleStatus = new System.Windows.Forms.Button();
            btnHealth = new System.Windows.Forms.Button();
            btnProvisionOwner = new System.Windows.Forms.Button();
            btnRefreshTenants = new System.Windows.Forms.Button();
            modulesTab = new System.Windows.Forms.TabPage();
            split = new System.Windows.Forms.SplitContainer();
            leftPanel = new System.Windows.Forms.Panel();
            tenantPickerForModules = new System.Windows.Forms.ListBox();
            leftTitle = new System.Windows.Forms.Label();
            rightPanel = new System.Windows.Forms.Panel();
            modulesGrid = new System.Windows.Forms.DataGridView();
            moduleToolbar = new System.Windows.Forms.FlowLayoutPanel();
            selectedTenantLabel = new System.Windows.Forms.Label();
            btnEnableModule = new System.Windows.Forms.Button();
            btnDisableModule = new System.Windows.Forms.Button();
            btnRefreshModules = new System.Windows.Forms.Button();
            auditTab = new System.Windows.Forms.TabPage();
            auditGrid = new System.Windows.Forms.DataGridView();
            auditToolbar = new System.Windows.Forms.FlowLayoutPanel();
            auditFilterLabel = new System.Windows.Forms.Label();
            auditTenantFilter = new System.Windows.Forms.ComboBox();
            btnRefreshAudit = new System.Windows.Forms.Button();
            header.SuspendLayout();
            tabs.SuspendLayout();
            tenantsTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)tenantsGrid).BeginInit();
            tenantToolbar.SuspendLayout();
            modulesTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)split).BeginInit();
            split.Panel1.SuspendLayout();
            split.Panel2.SuspendLayout();
            split.SuspendLayout();
            leftPanel.SuspendLayout();
            rightPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)modulesGrid).BeginInit();
            moduleToolbar.SuspendLayout();
            auditTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)auditGrid).BeginInit();
            auditToolbar.SuspendLayout();
            SuspendLayout();
            // 
            // header
            // 
            header.BackColor = System.Drawing.Color.FromArgb(28, 38, 56);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(signOut);
            header.Dock = System.Windows.Forms.DockStyle.Top;
            header.Location = new System.Drawing.Point(0, 0);
            header.Name = "header";
            header.Padding = new System.Windows.Forms.Padding(20, 10, 20, 10);
            header.Size = new System.Drawing.Size(1084, 65);
            header.TabIndex = 0;
            // 
            // title
            // 
            title.AutoSize = true;
            title.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            title.ForeColor = System.Drawing.Color.White;
            title.Location = new System.Drawing.Point(20, 10);
            title.Name = "title";
            title.Size = new System.Drawing.Size(206, 25);
            title.TabIndex = 0;
            title.Text = "RevCare Auto Hub CRM";
            // 
            // subtitle
            // 
            subtitle.AutoSize = true;
            subtitle.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            subtitle.ForeColor = System.Drawing.Color.FromArgb(180, 200, 220);
            subtitle.Location = new System.Drawing.Point(22, 35);
            subtitle.Name = "subtitle";
            subtitle.Size = new System.Drawing.Size(236, 17);
            subtitle.TabIndex = 1;
            subtitle.Text = "Platform Administration | Super Admin";
            // 
            // signOut
            // 
            signOut.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            signOut.BackColor = System.Drawing.Color.FromArgb(215, 60, 60);
            signOut.FlatAppearance.BorderSize = 0;
            signOut.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            signOut.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            signOut.ForeColor = System.Drawing.Color.White;
            signOut.Location = new System.Drawing.Point(959, 16);
            signOut.Name = "signOut";
            signOut.Size = new System.Drawing.Size(95, 32);
            signOut.TabIndex = 2;
            signOut.Text = "Sign Out";
            signOut.UseVisualStyleBackColor = false;
            // 
            // tabs
            // 
            tabs.Controls.Add(tenantsTab);
            tabs.Controls.Add(modulesTab);
            tabs.Controls.Add(auditTab);
            tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            tabs.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            tabs.Location = new System.Drawing.Point(0, 65);
            tabs.Name = "tabs";
            tabs.Padding = new System.Drawing.Point(14, 6);
            tabs.SelectedIndex = 0;
            tabs.Size = new System.Drawing.Size(1084, 616);
            tabs.TabIndex = 1;
            // 
            // tenantsTab
            // 
            tenantsTab.BackColor = System.Drawing.Color.White;
            tenantsTab.Controls.Add(tenantsGrid);
            tenantsTab.Controls.Add(tenantToolbar);
            tenantsTab.Location = new System.Drawing.Point(4, 32);
            tenantsTab.Name = "tenantsTab";
            tenantsTab.Padding = new System.Windows.Forms.Padding(15);
            tenantsTab.Size = new System.Drawing.Size(1076, 580);
            tenantsTab.TabIndex = 0;
            tenantsTab.Text = "🏢 Tenants & Provisioning";
            // 
            // tenantsGrid
            // 
            tenantsGrid.AllowUserToAddRows = false;
            tenantsGrid.AllowUserToDeleteRows = false;
            tenantsGrid.AllowUserToResizeRows = false;
            tenantsGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            tenantsGrid.BackgroundColor = System.Drawing.Color.White;
            tenantsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            tenantsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            tenantsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            tenantsGrid.Location = new System.Drawing.Point(15, 60);
            tenantsGrid.MultiSelect = false;
            tenantsGrid.Name = "tenantsGrid";
            tenantsGrid.ReadOnly = true;
            tenantsGrid.RowHeadersVisible = false;
            tenantsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            tenantsGrid.Size = new System.Drawing.Size(1046, 505);
            tenantsGrid.TabIndex = 1;
            // 
            // tenantToolbar
            // 
            tenantToolbar.Controls.Add(btnRegisterTenant);
            tenantToolbar.Controls.Add(btnToggleStatus);
            tenantToolbar.Controls.Add(btnHealth);
            tenantToolbar.Controls.Add(btnProvisionOwner);
            tenantToolbar.Controls.Add(btnRefreshTenants);
            tenantToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            tenantToolbar.Location = new System.Drawing.Point(15, 15);
            tenantToolbar.Name = "tenantToolbar";
            tenantToolbar.Padding = new System.Windows.Forms.Padding(0, 0, 0, 8);
            tenantToolbar.Size = new System.Drawing.Size(1046, 45);
            tenantToolbar.TabIndex = 0;
            // 
            // btnRegisterTenant
            // 
            btnRegisterTenant.AutoSize = true;
            btnRegisterTenant.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnRegisterTenant.FlatAppearance.BorderSize = 0;
            btnRegisterTenant.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnRegisterTenant.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnRegisterTenant.ForeColor = System.Drawing.Color.White;
            btnRegisterTenant.Location = new System.Drawing.Point(0, 0);
            btnRegisterTenant.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnRegisterTenant.Name = "btnRegisterTenant";
            btnRegisterTenant.Size = new System.Drawing.Size(135, 32);
            btnRegisterTenant.TabIndex = 0;
            btnRegisterTenant.Text = "➕ Register Tenant...";
            btnRegisterTenant.UseVisualStyleBackColor = false;
            // 
            // btnToggleStatus
            // 
            btnToggleStatus.AutoSize = true;
            btnToggleStatus.BackColor = System.Drawing.Color.FromArgb(60, 140, 60);
            btnToggleStatus.FlatAppearance.BorderSize = 0;
            btnToggleStatus.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnToggleStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnToggleStatus.ForeColor = System.Drawing.Color.White;
            btnToggleStatus.Location = new System.Drawing.Point(143, 0);
            btnToggleStatus.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnToggleStatus.Name = "btnToggleStatus";
            btnToggleStatus.Size = new System.Drawing.Size(125, 32);
            btnToggleStatus.TabIndex = 1;
            btnToggleStatus.Text = "🔄 Update Status...";
            btnToggleStatus.UseVisualStyleBackColor = false;
            // 
            // btnHealth
            // 
            btnHealth.AutoSize = true;
            btnHealth.BackColor = System.Drawing.Color.FromArgb(90, 100, 120);
            btnHealth.FlatAppearance.BorderSize = 0;
            btnHealth.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnHealth.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnHealth.ForeColor = System.Drawing.Color.White;
            btnHealth.Location = new System.Drawing.Point(276, 0);
            btnHealth.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnHealth.Name = "btnHealth";
            btnHealth.Size = new System.Drawing.Size(110, 32);
            btnHealth.TabIndex = 2;
            btnHealth.Text = "🩺 Test Health...";
            btnHealth.UseVisualStyleBackColor = false;
            // 
            // btnProvisionOwner
            // 
            btnProvisionOwner.AutoSize = true;
            btnProvisionOwner.BackColor = System.Drawing.Color.FromArgb(130, 80, 180);
            btnProvisionOwner.FlatAppearance.BorderSize = 0;
            btnProvisionOwner.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnProvisionOwner.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnProvisionOwner.ForeColor = System.Drawing.Color.White;
            btnProvisionOwner.Location = new System.Drawing.Point(394, 0);
            btnProvisionOwner.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnProvisionOwner.Name = "btnProvisionOwner";
            btnProvisionOwner.Size = new System.Drawing.Size(175, 32);
            btnProvisionOwner.TabIndex = 3;
            btnProvisionOwner.Text = "👤 Provision Initial Owner...";
            btnProvisionOwner.UseVisualStyleBackColor = false;
            // 
            // btnRefreshTenants
            // 
            btnRefreshTenants.AutoSize = true;
            btnRefreshTenants.BackColor = System.Drawing.Color.Gray;
            btnRefreshTenants.FlatAppearance.BorderSize = 0;
            btnRefreshTenants.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnRefreshTenants.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnRefreshTenants.ForeColor = System.Drawing.Color.White;
            btnRefreshTenants.Location = new System.Drawing.Point(577, 0);
            btnRefreshTenants.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnRefreshTenants.Name = "btnRefreshTenants";
            btnRefreshTenants.Size = new System.Drawing.Size(85, 32);
            btnRefreshTenants.TabIndex = 4;
            btnRefreshTenants.Text = "🔄 Refresh";
            btnRefreshTenants.UseVisualStyleBackColor = false;
            // 
            // modulesTab
            // 
            modulesTab.BackColor = System.Drawing.Color.White;
            modulesTab.Controls.Add(split);
            modulesTab.Location = new System.Drawing.Point(4, 32);
            modulesTab.Name = "modulesTab";
            modulesTab.Padding = new System.Windows.Forms.Padding(15);
            modulesTab.Size = new System.Drawing.Size(1076, 580);
            modulesTab.TabIndex = 1;
            modulesTab.Text = "📦 Module Catalog & Entitlements";
            // 
            // split
            // 
            split.Dock = System.Windows.Forms.DockStyle.Fill;
            split.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            split.Location = new System.Drawing.Point(15, 15);
            split.Name = "split";
            // 
            // split.Panel1
            // 
            split.Panel1.Controls.Add(leftPanel);
            // 
            // split.Panel2
            // 
            split.Panel2.Controls.Add(rightPanel);
            split.Size = new System.Drawing.Size(1046, 550);
            split.SplitterDistance = 250;
            split.TabIndex = 0;
            // 
            // leftPanel
            // 
            leftPanel.Controls.Add(tenantPickerForModules);
            leftPanel.Controls.Add(leftTitle);
            leftPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            leftPanel.Location = new System.Drawing.Point(0, 0);
            leftPanel.Name = "leftPanel";
            leftPanel.Padding = new System.Windows.Forms.Padding(5);
            leftPanel.Size = new System.Drawing.Size(250, 550);
            leftPanel.TabIndex = 0;
            // 
            // tenantPickerForModules
            // 
            tenantPickerForModules.Dock = System.Windows.Forms.DockStyle.Fill;
            tenantPickerForModules.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            tenantPickerForModules.IntegralHeight = false;
            tenantPickerForModules.ItemHeight = 17;
            tenantPickerForModules.Location = new System.Drawing.Point(5, 30);
            tenantPickerForModules.Name = "tenantPickerForModules";
            tenantPickerForModules.Size = new System.Drawing.Size(240, 515);
            tenantPickerForModules.TabIndex = 1;
            // 
            // leftTitle
            // 
            leftTitle.Dock = System.Windows.Forms.DockStyle.Top;
            leftTitle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            leftTitle.Location = new System.Drawing.Point(5, 5);
            leftTitle.Name = "leftTitle";
            leftTitle.Size = new System.Drawing.Size(240, 25);
            leftTitle.TabIndex = 0;
            leftTitle.Text = "Select Tenant:";
            // 
            // rightPanel
            // 
            rightPanel.Controls.Add(modulesGrid);
            rightPanel.Controls.Add(moduleToolbar);
            rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            rightPanel.Location = new System.Drawing.Point(0, 0);
            rightPanel.Name = "rightPanel";
            rightPanel.Padding = new System.Windows.Forms.Padding(5);
            rightPanel.Size = new System.Drawing.Size(792, 550);
            rightPanel.TabIndex = 0;
            // 
            // modulesGrid
            // 
            modulesGrid.AllowUserToAddRows = false;
            modulesGrid.AllowUserToDeleteRows = false;
            modulesGrid.AllowUserToResizeRows = false;
            modulesGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            modulesGrid.BackgroundColor = System.Drawing.Color.White;
            modulesGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            modulesGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            modulesGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            modulesGrid.Location = new System.Drawing.Point(5, 50);
            modulesGrid.MultiSelect = false;
            modulesGrid.Name = "modulesGrid";
            modulesGrid.ReadOnly = true;
            modulesGrid.RowHeadersVisible = false;
            modulesGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            modulesGrid.Size = new System.Drawing.Size(782, 495);
            modulesGrid.TabIndex = 1;
            // 
            // moduleToolbar
            // 
            moduleToolbar.Controls.Add(selectedTenantLabel);
            moduleToolbar.Controls.Add(btnEnableModule);
            moduleToolbar.Controls.Add(btnDisableModule);
            moduleToolbar.Controls.Add(btnRefreshModules);
            moduleToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            moduleToolbar.Location = new System.Drawing.Point(5, 5);
            moduleToolbar.Name = "moduleToolbar";
            moduleToolbar.Size = new System.Drawing.Size(782, 45);
            moduleToolbar.TabIndex = 0;
            // 
            // selectedTenantLabel
            // 
            selectedTenantLabel.AutoSize = true;
            selectedTenantLabel.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            selectedTenantLabel.Location = new System.Drawing.Point(0, 8);
            selectedTenantLabel.Margin = new System.Windows.Forms.Padding(0, 8, 15, 0);
            selectedTenantLabel.Name = "selectedTenantLabel";
            selectedTenantLabel.Size = new System.Drawing.Size(188, 19);
            selectedTenantLabel.TabIndex = 0;
            selectedTenantLabel.Text = "Modules for: (select tenant)";
            // 
            // btnEnableModule
            // 
            btnEnableModule.AutoSize = true;
            btnEnableModule.BackColor = System.Drawing.Color.FromArgb(0, 130, 70);
            btnEnableModule.FlatAppearance.BorderSize = 0;
            btnEnableModule.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnEnableModule.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnEnableModule.ForeColor = System.Drawing.Color.White;
            btnEnableModule.Location = new System.Drawing.Point(203, 0);
            btnEnableModule.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnEnableModule.Name = "btnEnableModule";
            btnEnableModule.Size = new System.Drawing.Size(125, 32);
            btnEnableModule.TabIndex = 1;
            btnEnableModule.Text = "✅ Enable Module...";
            btnEnableModule.UseVisualStyleBackColor = false;
            // 
            // btnDisableModule
            // 
            btnDisableModule.AutoSize = true;
            btnDisableModule.BackColor = System.Drawing.Color.FromArgb(200, 60, 60);
            btnDisableModule.FlatAppearance.BorderSize = 0;
            btnDisableModule.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnDisableModule.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnDisableModule.ForeColor = System.Drawing.Color.White;
            btnDisableModule.Location = new System.Drawing.Point(336, 0);
            btnDisableModule.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnDisableModule.Name = "btnDisableModule";
            btnDisableModule.Size = new System.Drawing.Size(130, 32);
            btnDisableModule.TabIndex = 2;
            btnDisableModule.Text = "⛔ Disable Module...";
            btnDisableModule.UseVisualStyleBackColor = false;
            // 
            // btnRefreshModules
            // 
            btnRefreshModules.AutoSize = true;
            btnRefreshModules.BackColor = System.Drawing.Color.Gray;
            btnRefreshModules.FlatAppearance.BorderSize = 0;
            btnRefreshModules.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnRefreshModules.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnRefreshModules.ForeColor = System.Drawing.Color.White;
            btnRefreshModules.Location = new System.Drawing.Point(474, 0);
            btnRefreshModules.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnRefreshModules.Name = "btnRefreshModules";
            btnRefreshModules.Size = new System.Drawing.Size(85, 32);
            btnRefreshModules.TabIndex = 3;
            btnRefreshModules.Text = "🔄 Refresh";
            btnRefreshModules.UseVisualStyleBackColor = false;
            // 
            // auditTab
            // 
            auditTab.BackColor = System.Drawing.Color.White;
            auditTab.Controls.Add(auditGrid);
            auditTab.Controls.Add(auditToolbar);
            auditTab.Location = new System.Drawing.Point(4, 32);
            auditTab.Name = "auditTab";
            auditTab.Padding = new System.Windows.Forms.Padding(15);
            auditTab.Size = new System.Drawing.Size(1076, 580);
            auditTab.TabIndex = 2;
            auditTab.Text = "📋 Platform Audit Log";
            // 
            // auditGrid
            // 
            auditGrid.AllowUserToAddRows = false;
            auditGrid.AllowUserToDeleteRows = false;
            auditGrid.AllowUserToResizeRows = false;
            auditGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            auditGrid.BackgroundColor = System.Drawing.Color.White;
            auditGrid.BorderStyle = System.Windows.Forms.BorderStyle.None;
            auditGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            auditGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            auditGrid.Location = new System.Drawing.Point(15, 60);
            auditGrid.MultiSelect = false;
            auditGrid.Name = "auditGrid";
            auditGrid.ReadOnly = true;
            auditGrid.RowHeadersVisible = false;
            auditGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            auditGrid.Size = new System.Drawing.Size(1046, 505);
            auditGrid.TabIndex = 1;
            // 
            // auditToolbar
            // 
            auditToolbar.Controls.Add(auditFilterLabel);
            auditToolbar.Controls.Add(auditTenantFilter);
            auditToolbar.Controls.Add(btnRefreshAudit);
            auditToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            auditToolbar.Location = new System.Drawing.Point(15, 15);
            auditToolbar.Name = "auditToolbar";
            auditToolbar.Size = new System.Drawing.Size(1046, 45);
            auditToolbar.TabIndex = 0;
            // 
            // auditFilterLabel
            // 
            auditFilterLabel.AutoSize = true;
            auditFilterLabel.Location = new System.Drawing.Point(0, 8);
            auditFilterLabel.Margin = new System.Windows.Forms.Padding(0, 8, 5, 0);
            auditFilterLabel.Name = "auditFilterLabel";
            auditFilterLabel.Size = new System.Drawing.Size(78, 17);
            auditFilterLabel.TabIndex = 0;
            auditFilterLabel.Text = "Filter Tenant:";
            // 
            // auditTenantFilter
            // 
            auditTenantFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            auditTenantFilter.Location = new System.Drawing.Point(83, 5);
            auditTenantFilter.Margin = new System.Windows.Forms.Padding(0, 5, 15, 0);
            auditTenantFilter.Name = "auditTenantFilter";
            auditTenantFilter.Size = new System.Drawing.Size(180, 25);
            auditTenantFilter.TabIndex = 1;
            // 
            // btnRefreshAudit
            // 
            btnRefreshAudit.AutoSize = true;
            btnRefreshAudit.BackColor = System.Drawing.Color.Gray;
            btnRefreshAudit.FlatAppearance.BorderSize = 0;
            btnRefreshAudit.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnRefreshAudit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            btnRefreshAudit.ForeColor = System.Drawing.Color.White;
            btnRefreshAudit.Location = new System.Drawing.Point(278, 0);
            btnRefreshAudit.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
            btnRefreshAudit.Name = "btnRefreshAudit";
            btnRefreshAudit.Size = new System.Drawing.Size(105, 32);
            btnRefreshAudit.TabIndex = 2;
            btnRefreshAudit.Text = "🔄 Refresh Log";
            btnRefreshAudit.UseVisualStyleBackColor = false;
            // 
            // PlatformAdminForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            ClientSize = new System.Drawing.Size(1084, 681);
            Controls.Add(tabs);
            Controls.Add(header);
            Font = new System.Drawing.Font("Segoe UI", 9F);
            MinimumSize = new System.Drawing.Size(900, 600);
            Name = "PlatformAdminForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "RevCare Auto Hub CRM — Platform Administration";
            header.ResumeLayout(false);
            header.PerformLayout();
            tabs.ResumeLayout(false);
            tenantsTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)tenantsGrid).EndInit();
            tenantToolbar.ResumeLayout(false);
            tenantToolbar.PerformLayout();
            modulesTab.ResumeLayout(false);
            split.Panel1.ResumeLayout(false);
            split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)split).EndInit();
            split.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)modulesGrid).EndInit();
            moduleToolbar.ResumeLayout(false);
            moduleToolbar.PerformLayout();
            auditTab.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)auditGrid).EndInit();
            auditToolbar.ResumeLayout(false);
            auditToolbar.PerformLayout();
            ResumeLayout(false);
        }

        private System.Windows.Forms.Panel header;
        private System.Windows.Forms.Label title;
        private System.Windows.Forms.Label subtitle;
        private System.Windows.Forms.Button signOut;
        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tenantsTab;
        private System.Windows.Forms.DataGridView tenantsGrid;
        private System.Windows.Forms.FlowLayoutPanel tenantToolbar;
        private System.Windows.Forms.Button btnRegisterTenant;
        private System.Windows.Forms.Button btnToggleStatus;
        private System.Windows.Forms.Button btnHealth;
        private System.Windows.Forms.Button btnProvisionOwner;
        private System.Windows.Forms.Button btnRefreshTenants;
        private System.Windows.Forms.TabPage modulesTab;
        private System.Windows.Forms.SplitContainer split;
        private System.Windows.Forms.Panel leftPanel;
        private System.Windows.Forms.Label leftTitle;
        private System.Windows.Forms.ListBox tenantPickerForModules;
        private System.Windows.Forms.Panel rightPanel;
        private System.Windows.Forms.FlowLayoutPanel moduleToolbar;
        private System.Windows.Forms.Label selectedTenantLabel;
        private System.Windows.Forms.Button btnEnableModule;
        private System.Windows.Forms.Button btnDisableModule;
        private System.Windows.Forms.Button btnRefreshModules;
        private System.Windows.Forms.DataGridView modulesGrid;
        private System.Windows.Forms.TabPage auditTab;
        private System.Windows.Forms.FlowLayoutPanel auditToolbar;
        private System.Windows.Forms.Label auditFilterLabel;
        private System.Windows.Forms.ComboBox auditTenantFilter;
        private System.Windows.Forms.Button btnRefreshAudit;
        private System.Windows.Forms.DataGridView auditGrid;
    }
}
