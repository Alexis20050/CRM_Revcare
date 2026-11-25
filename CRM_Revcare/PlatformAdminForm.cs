#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_Revcare_;

internal sealed partial class PlatformAdminForm : Form
{
    private readonly TenantApiClient api;
    private bool busy;
    internal bool LogoutRequested { get; private set; }

    public PlatformAdminForm() : this(new TenantApiClient("https://localhost:7105")) { }

    public PlatformAdminForm(TenantApiClient api)
    {
        this.api = api;
        InitializeComponent();

        subtitle.Text = $"Platform Administration | Super Admin: {api.Username}";

        signOut.Click += (s, e) => { LogoutRequested = true; Close(); };

        btnRegisterTenant.Click += async (s, e) => await ShowRegisterTenantDialog();
        btnToggleStatus.Click += async (s, e) => await ShowUpdateStatusDialog();
        btnHealth.Click += async (s, e) => await TestSelectedTenantHealth();
        btnProvisionOwner.Click += async (s, e) => await ShowProvisionOwnerDialog();
        btnRefreshTenants.Click += async (s, e) => await LoadTenantsAsync();

        tenantPickerForModules.SelectedIndexChanged += async (s, e) => await LoadTenantModulesAsync();
        btnEnableModule.Click += async (s, e) => await ChangeModuleEntitlement(true);
        btnDisableModule.Click += async (s, e) => await ChangeModuleEntitlement(false);
        btnRefreshModules.Click += async (s, e) => await LoadTenantModulesAsync();

        auditTenantFilter.SelectedIndexChanged += async (s, e) => await LoadAuditAsync();
        btnRefreshAudit.Click += async (s, e) => await LoadAuditAsync();

        Shown += async (s, e) => await InitializeDataAsync();
    }

    private async Task InitializeDataAsync()
    {
        await LoadTenantsAsync();
        await LoadAuditAsync();
    }

    private async Task LoadTenantsAsync()
    {
        if (busy || IsDisposed) return;
        busy = true;
        try
        {
            var arr = await api.GetMasterTenantsAsync();
            if (IsDisposed) return;
            var list = new List<TenantRegistrationRow>();
            foreach (var item in arr)
            {
                if (item == null) continue;
                list.Add(new TenantRegistrationRow(
                    item["tenantId"]?.GetValue<string>() ?? "",
                    item["displayName"]?.GetValue<string>() ?? "",
                    item["connectionKey"]?.GetValue<string>() ?? "",
                    item["status"]?.GetValue<string>() ?? "",
                    item["isActive"]?.GetValue<bool>() ?? true,
                    item["createdAtUtc"] != null ? DateTime.Parse(item["createdAtUtc"]!.ToString()).ToLocalTime() : DateTime.MinValue
                ));
            }

            tenantsGrid.DataSource = list.ToList();
            GridMetadataColumns.Hide(tenantsGrid);

            // Populate tenant picker for modules
            string? prevSelected = (tenantPickerForModules.SelectedItem as TenantPickerItem)?.TenantId;
            tenantPickerForModules.Items.Clear();
            auditTenantFilter.Items.Clear();
            auditTenantFilter.Items.Add("All Tenants");

            foreach (var t in list)
            {
                tenantPickerForModules.Items.Add(new TenantPickerItem(t.TenantId, $"{t.DisplayName} ({t.TenantId}) [{t.Status}]"));
                auditTenantFilter.Items.Add(t.TenantId);
            }
            if (tenantPickerForModules.Items.Count > 0)
            {
                int idx = 0;
                if (!string.IsNullOrEmpty(prevSelected))
                {
                    for (int i = 0; i < tenantPickerForModules.Items.Count; i++)
                    {
                        if ((tenantPickerForModules.Items[i] as TenantPickerItem)?.TenantId == prevSelected)
                        {
                            idx = i;
                            break;
                        }
                    }
                }
                tenantPickerForModules.SelectedIndex = idx;
            }
            if (auditTenantFilter.SelectedIndex < 0) auditTenantFilter.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            if (!IsDisposed) MessageBox.Show(this, "Failed to load tenants: " + ex.Message, "Platform Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { busy = false; }
    }

    private async Task LoadTenantModulesAsync()
    {
        if (IsDisposed || tenantPickerForModules.SelectedItem is not TenantPickerItem item) return;
        selectedTenantLabel.Text = $"Modules for: {item.DisplayName}";

        try
        {
            var modules = await api.GetTenantModulesAsync(item.TenantId);
            if (IsDisposed) return;
            var view = modules.Select(m => new
            {
                ModuleId = m.ModuleId,
                DisplayName = m.DisplayName,
                Implemented = m.IsImplemented ? "Yes" : "Coming Soon",
                Dependencies = string.IsNullOrWhiteSpace(m.RequiredDependencies) ? "None" : m.RequiredDependencies,
                Enabled = m.IsEnabled ? "✅ Enabled" : "⛔ Disabled",
                Reason = m.Reason,
                UpdatedBy = m.UpdatedBy,
                LastUpdated = m.UpdatedAtUtc?.ToLocalTime().ToString("g") ?? "-"
            }).ToList();

            modulesGrid.DataSource = view;
        }
        catch (Exception ex)
        {
            if (!IsDisposed) MessageBox.Show(this, "Failed to load module entitlements: " + ex.Message, "Platform Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ChangeModuleEntitlement(bool enable)
    {
        if (tenantPickerForModules.SelectedItem is not TenantPickerItem item) return;
        if (modulesGrid.CurrentRow == null)
        {
            MessageBox.Show(this, "Select a module first.", "Module Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string moduleId = modulesGrid.CurrentRow.Cells["ModuleId"]?.Value?.ToString() ?? "";
        string displayName = modulesGrid.CurrentRow.Cells["DisplayName"]?.Value?.ToString() ?? moduleId;
        string implemented = modulesGrid.CurrentRow.Cells["Implemented"]?.Value?.ToString() ?? "";

        if (enable && implemented == "Coming Soon")
        {
            MessageBox.Show(this, $"Module '{displayName}' is marked as unimplemented (Coming Soon) and cannot be enabled.", "Unimplemented Module", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string actionVerb = enable ? "enable" : "disable";
        using var prompt = new EntitlementReasonPromptForm(displayName, enable);
        if (prompt.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await api.UpdateTenantModuleAsync(item.TenantId, moduleId, enable, prompt.Reason);
            MessageBox.Show(this, $"Module '{displayName}' was successfully {actionVerb}d for tenant '{item.DisplayName}'.", "Module Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadTenantModulesAsync();
            await LoadAuditAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Entitlement Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task ShowRegisterTenantDialog()
    {
        using var dlg = new RegisterTenantDialogForm();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await api.RegisterMasterTenantAsync(dlg.TenantId, dlg.DisplayName, dlg.ConnectionKey);
            MessageBox.Show(this, $"Tenant '{dlg.DisplayName}' registered with status 'Provisioning'. Operational database access remains blocked until database is ready.", "Tenant Registered", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadTenantsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Registration failed: " + ex.Message, "Platform Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ShowUpdateStatusDialog()
    {
        if (tenantsGrid.CurrentRow?.DataBoundItem is not TenantRegistrationRow row)
        {
            MessageBox.Show(this, "Select a tenant from the grid first.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new UpdateTenantStatusDialogForm(row.TenantId, row.DisplayName, row.Status, row.IsActive);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await api.UpdateMasterTenantStatusAsync(row.TenantId, dlg.SelectedStatus, dlg.SelectedIsActive);
            MessageBox.Show(this, $"Tenant '{row.DisplayName}' status updated to {dlg.SelectedStatus} (Active: {dlg.SelectedIsActive}).", "Status Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadTenantsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Status update failed: " + ex.Message, "Platform Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task TestSelectedTenantHealth()
    {
        if (tenantsGrid.CurrentRow?.DataBoundItem is not TenantRegistrationRow row)
        {
            MessageBox.Show(this, "Select a tenant from the grid first.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var health = await api.GetMasterTenantHealthAsync(row.TenantId);
            bool isConfigured = health?["isConfigured"]?.GetValue<bool>() ?? false;
            bool isReachable = health?["isReachable"]?.GetValue<bool>() ?? false;
            bool identityMatches = health?["identityMatches"]?.GetValue<bool>() ?? false;
            string diag = health?["diagnostics"]?.GetValue<string>() ?? "";

            string message = $"Tenant: {row.DisplayName} ({row.TenantId})\n" +
                             $"Status: {row.Status} | Active: {row.IsActive}\n" +
                             $"Connection Key: {row.ConnectionKey}\n\n" +
                             $"• Connection string configured: {(isConfigured ? "YES" : "NO")}\n" +
                             $"• Database reachable: {(isReachable ? "YES" : "NO")}\n" +
                             $"• Tenant database identity matches: {(identityMatches ? "YES" : "NO")}\n" +
                             (string.IsNullOrEmpty(diag) ? "" : $"\nDiagnostics: {diag}");

            MessageBoxIcon icon = isReachable && identityMatches ? MessageBoxIcon.Information : MessageBoxIcon.Warning;
            MessageBox.Show(this, message, "Tenant Database Health Check", MessageBoxButtons.OK, icon);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Health check failed: " + ex.Message, "Platform Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ShowProvisionOwnerDialog()
    {
        if (tenantsGrid.CurrentRow?.DataBoundItem is not TenantRegistrationRow row)
        {
            MessageBox.Show(this, "Select a tenant from the grid first.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new ProvisionOwnerDialogForm(row.TenantId, row.DisplayName);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var res = await api.ProvisionMasterTenantOwnerAsync(row.TenantId, dlg.Username, dlg.Password, dlg.OwnerDisplayName);
            string profileStatus = res?["profileProvisioningStatus"]?.GetValue<string>() ?? "Synced";
            string reason = res?["provisioningFailureReason"]?.GetValue<string>() ?? "";

            string msg = $"Owner account '{dlg.Username}' successfully provisioned in Master with OwnerManager role.\n\n" +
                         $"Operational Profile Status: {profileStatus}" +
                         (string.IsNullOrEmpty(reason) ? "" : $"\nNote: {reason}");

            MessageBox.Show(this, msg, "Owner Provisioned", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Owner provisioning failed: " + ex.Message, "Platform Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadAuditAsync()
    {
        try
        {
            string? sel = auditTenantFilter.SelectedItem?.ToString();
            string? tenantFilter = sel == "All Tenants" ? null : sel;
            var arr = await api.GetMasterAuditAsync(tenantFilter);

            var list = new List<AuditLogRow>();
            foreach (var item in arr)
            {
                if (item == null) continue;
                list.Add(new AuditLogRow(
                    item["id"]?.GetValue<long>() ?? 0,
                    item["tenantId"]?.GetValue<string>() ?? "",
                    item["moduleId"]?.GetValue<string>() ?? "",
                    item["action"]?.GetValue<string>() ?? "",
                    item["reason"]?.GetValue<string>() ?? "",
                    item["actor"]?.GetValue<string>() ?? "",
                    item["occurredAtUtc"] != null ? DateTime.Parse(item["occurredAtUtc"]!.ToString()).ToLocalTime().ToString("g") : ""
                ));
            }

            auditGrid.DataSource = list;
        }
        catch { }
    }

    private sealed record TenantRegistrationRow(string TenantId, string DisplayName, string ConnectionKey, string Status, bool IsActive, DateTime CreatedAtLocal);
    private sealed record AuditLogRow(long Id, string TenantId, string ModuleId, string Action, string Reason, string Actor, string Timestamp);
    private sealed record TenantPickerItem(string TenantId, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}

internal sealed class EntitlementReasonPromptForm : Form
{
    private readonly TextBox reasonBox;
    public string Reason => reasonBox.Text.Trim();

    public EntitlementReasonPromptForm(string moduleName, bool enabling)
    {
        Text = (enabling ? "Enable " : "Disable ") + moduleName;
        Size = new Size(460, 260);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        BackColor = Color.White;

        var lbl = new Label
        {
            Text = $"Reason for {(enabling ? "enabling" : "disabling")} '{moduleName}' (mandatory for audit):",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Location = new Point(20, 16),
            Size = new Size(405, 36)
        };

        reasonBox = new TextBox
        {
            Location = new Point(20, 56),
            Size = new Size(405, 90),
            Multiline = true,
            Font = new Font("Segoe UI", 9.5F),
            ScrollBars = ScrollBars.Vertical
        };

        var okBtn = new Button
        {
            Text = enabling ? "Enable Module" : "Disable Module",
            DialogResult = DialogResult.OK,
            Location = new Point(200, 165),
            Size = new Size(130, 34),
            BackColor = enabling ? Color.FromArgb(0, 130, 70) : Color.FromArgb(200, 60, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        okBtn.FlatAppearance.BorderSize = 0;
        okBtn.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(reasonBox.Text))
            {
                MessageBox.Show(this, "A reason is mandatory for module entitlement changes.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        };

        var cancelBtn = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(340, 165),
            Size = new Size(85, 34)
        };

        Controls.Add(lbl);
        Controls.Add(reasonBox);
        Controls.Add(okBtn);
        Controls.Add(cancelBtn);
        AcceptButton = okBtn;
        CancelButton = cancelBtn;
    }
}

internal sealed class RegisterTenantDialogForm : Form
{
    private readonly TextBox idBox = new();
    private readonly TextBox nameBox = new();
    private readonly TextBox connBox = new();

    public string TenantId => idBox.Text.Trim();
    public string DisplayName => nameBox.Text.Trim();
    public string ConnectionKey => connBox.Text.Trim();

    public RegisterTenantDialogForm()
    {
        Text = "Register New Tenant";
        Size = new Size(440, 310);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        int y = 16;
        void AddRow(string labelText, TextBox tb, string placeholder)
        {
            var lbl = new Label { Text = labelText, Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            tb.Location = new Point(20, y + 20);
            tb.Size = new Size(385, 25);
            tb.PlaceholderText = placeholder;
            Controls.Add(lbl);
            Controls.Add(tb);
            y += 56;
        }

        AddRow("Tenant ID (e.g. tenant-b):", idBox, "tenant-b");
        AddRow("Business Display Name:", nameBox, "RevCare Tenant B");
        AddRow("Database Connection Key reference:", connBox, "TenantB");

        var note = new Label
        {
            Text = "Note: Tenant is created with status 'Provisioning'. Operational database access remains safely blocked until database is ready.",
            Location = new Point(20, y),
            Size = new Size(385, 36),
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 8.5F)
        };
        Controls.Add(note);
        y += 44;

        var ok = new Button { Text = "Register Tenant", DialogResult = DialogResult.OK, Location = new Point(190, y), Size = new Size(130, 34), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        ok.FlatAppearance.BorderSize = 0;
        ok.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(idBox.Text) || string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(connBox.Text))
            {
                MessageBox.Show(this, "All fields are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(330, y), Size = new Size(75, 34) };
        Controls.Add(ok); Controls.Add(cancel);
        AcceptButton = ok; CancelButton = cancel;
    }
}

internal sealed class UpdateTenantStatusDialogForm : Form
{
    private readonly ComboBox statusCombo = new();
    private readonly CheckBox activeCheck = new();

    public string SelectedStatus => statusCombo.SelectedItem?.ToString() ?? "Ready";
    public bool SelectedIsActive => activeCheck.Checked;

    public UpdateTenantStatusDialogForm(string tenantId, string displayName, string currentStatus, bool currentIsActive)
    {
        Text = $"Update Status — {displayName}";
        Size = new Size(400, 240);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        var lbl = new Label { Text = $"Update Provisioning/Access Status for '{tenantId}':", Location = new Point(20, 16), Size = new Size(350, 25), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        statusCombo.Location = new Point(20, 45);
        statusCombo.Size = new Size(345, 26);
        statusCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        statusCombo.Items.AddRange(["Ready", "Provisioning", "Suspended"]);
        statusCombo.SelectedItem = currentStatus;

        activeCheck.Text = "Tenant Account Active (uncheck to deactivate access)";
        activeCheck.Location = new Point(20, 85);
        activeCheck.Size = new Size(350, 26);
        activeCheck.Checked = currentIsActive;

        var ok = new Button { Text = "Save Status", DialogResult = DialogResult.OK, Location = new Point(175, 140), Size = new Size(110, 34), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        ok.FlatAppearance.BorderSize = 0;
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(295, 140), Size = new Size(70, 34) };

        Controls.Add(lbl); Controls.Add(statusCombo); Controls.Add(activeCheck); Controls.Add(ok); Controls.Add(cancel);
        AcceptButton = ok; CancelButton = cancel;
    }
}

internal sealed class ProvisionOwnerDialogForm : Form
{
    private readonly TextBox userBox = new();
    private readonly TextBox passBox = new();
    private readonly TextBox nameBox = new();

    public string Username => userBox.Text.Trim();
    public string Password => passBox.Text;
    public string OwnerDisplayName => nameBox.Text.Trim();

    public ProvisionOwnerDialogForm(string tenantId, string displayName)
    {
        Text = $"Provision Initial Owner — {displayName}";
        Size = new Size(440, 290);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 9F);

        int y = 16;
        void AddRow(string labelText, TextBox tb, bool isPass)
        {
            var lbl = new Label { Text = labelText, Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            tb.Location = new Point(20, y + 20);
            tb.Size = new Size(385, 25);
            if (isPass) tb.UseSystemPasswordChar = true;
            Controls.Add(lbl); Controls.Add(tb);
            y += 54;
        }

        AddRow("Owner Username:", userBox, false);
        AddRow("Initial Password (min 12 chars):", passBox, true);
        AddRow("Display Name (Full Name):", nameBox, false);

        var ok = new Button { Text = "Provision Owner", DialogResult = DialogResult.OK, Location = new Point(185, y + 10), Size = new Size(135, 34), BackColor = Color.FromArgb(130, 80, 180), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        ok.FlatAppearance.BorderSize = 0;
        ok.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(userBox.Text) || passBox.Text.Length < 12)
            {
                MessageBox.Show(this, "Username is required and password must be at least 12 characters.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(330, y + 10), Size = new Size(75, 34) };
        Controls.Add(ok); Controls.Add(cancel);
        AcceptButton = ok; CancelButton = cancel;
    }
}
