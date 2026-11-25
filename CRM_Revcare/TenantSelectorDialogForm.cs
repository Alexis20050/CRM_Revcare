#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CRM_Revcare_;

internal sealed partial class TenantSelectorDialogForm : Form
{
    public object? SelectedWorkspace { get; private set; }

    public sealed record WorkspaceChoice(string? TenantId, string DisplayName, string Subtitle, bool IsSuperAdmin)
    {
        public override string ToString() => DisplayName;
    }

    public TenantSelectorDialogForm() : this(false, Array.Empty<TenantResolutionInfo>()) { }

    public TenantSelectorDialogForm(bool isSuperAdmin, IEnumerable<TenantResolutionInfo> tenants)
    {
        InitializeComponent();

        if (isSuperAdmin)
        {
            workspaceList.Items.Add(new WorkspaceChoice(null, "🛡️ Platform Administration (Super Admin)", "Manage tenants, module entitlements, and health", true));
        }

        foreach (var t in tenants)
        {
            string statusTag = t.Status == "Ready" ? "" : $" [{t.Status.ToUpperInvariant()}]";
            workspaceList.Items.Add(new WorkspaceChoice(
                t.TenantId,
                $"🏢 {t.DisplayName} ({t.Role}){statusTag}",
                $"Tenant ID: {t.TenantId} | Status: {t.Status}",
                false
            ));
        }

        if (workspaceList.Items.Count > 0)
            workspaceList.SelectedIndex = 0;

        workspaceList.DoubleClick += (s, e) => ConfirmSelection();
        enterButton.Click += (s, e) => ConfirmSelection();
    }

    private void ConfirmSelection()
    {
        if (workspaceList.SelectedItem is WorkspaceChoice choice)
        {
            SelectedWorkspace = choice;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
