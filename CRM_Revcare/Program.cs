#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show($"Application error: {ex?.Message}\n\n{ex?.StackTrace}", "RevCare Auto Hub CRM", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show($"Application error: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "RevCare Auto Hub CRM", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            // Operational sessions must pass through backend authorization.
            // Direct Legacy SQL access is restricted to explicitly isolated demo databases.
            bool useTenantApi = !Array.Exists(args, value =>
                string.Equals(value, "--legacy", StringComparison.OrdinalIgnoreCase));

            if (!useTenantApi)
            {
                try
                {
                    var target = CrmDatabase.ResolveTarget(args);
                    if (!string.Equals(target.ServerDisplayName, @"(localdb)\MSSQLLocalDB", StringComparison.OrdinalIgnoreCase)
                        || !target.DatabaseName.StartsWith("RevCare_Demo_", StringComparison.Ordinal)
                        || !Guid.TryParseExact(target.DatabaseName["RevCare_Demo_".Length..], "N", out _))
                        throw new InvalidOperationException("Direct Legacy workspace access is restricted to a disposable RevCare_Demo_<GUID> LocalDB database. Use central login for operational access.");
                }
                catch (Exception ex)
                {
                    CrmDiagnostics.LogException("Startup Target Resolution", ex);
                    MessageBox.Show(
                        $"Unable to start RevCare Auto Hub CRM:\n\n{ex.Message}",
                        "Database Configuration Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            if (useTenantApi)
            {
                string apiAddress = Environment.GetEnvironmentVariable("REVCARE_API_BASE_URL") ?? "https://localhost:7105";
                try
                {
                    while (true)
                    {
                        using var api = new TenantApiClient(apiAddress);
                        string chosenUsername = "";
                        string chosenPassword = "";
                        bool isSuperAdmin = false;
                        List<TenantResolutionInfo>? accessibleTenants = null;

                        using var tenantLogin = new AdminLoginForm(async (username, password) =>
                        {
                            var resolution = await api.ResolveTenantsAsync(username, password);
                            chosenUsername = username;
                            chosenPassword = password;
                            isSuperAdmin = resolution.IsSuperAdmin;
                            accessibleTenants = resolution.Tenants;
                            return isSuperAdmin || (accessibleTenants != null && accessibleTenants.Count > 0);
                        });

                        if (tenantLogin.ShowDialog() != DialogResult.OK) break;

                        // Case 1: Super Admin with 0 tenant memberships -> direct to Master Admin workspace
                        if (isSuperAdmin && (accessibleTenants == null || accessibleTenants.Count == 0))
                        {
                            AuthenticateBeforeWorkspace(api, chosenUsername, chosenPassword, null);
                            using var adminForm = new PlatformAdminForm(api);
                            Application.Run(adminForm);
                            if (!adminForm.LogoutRequested) break;
                            continue;
                        }

                        // Case 2: Exactly 1 tenant and NOT Super Admin -> auto-select that tenant
                        if (!isSuperAdmin && accessibleTenants != null && accessibleTenants.Count == 1)
                        {
                            var t = accessibleTenants[0];
                            api.CompanyName = t.DisplayName;
                            AuthenticateBeforeWorkspace(api, chosenUsername, chosenPassword, t.TenantId);
                            using var workspace = new TenantWorkspaceForm(api);
                            Application.Run(workspace);
                            if (!workspace.LogoutRequested) break;
                            continue;
                        }

                        // Case 3: Multiple workspaces available (SuperAdmin + tenant, or multiple tenants) -> Show selector
                        using var selector = new TenantSelectorDialogForm(isSuperAdmin, accessibleTenants ?? new List<TenantResolutionInfo>());
                        if (selector.ShowDialog() != DialogResult.OK) break;

                        if (selector.SelectedWorkspace is TenantSelectorDialogForm.WorkspaceChoice choice)
                        {
                            if (choice.IsSuperAdmin)
                            {
                                AuthenticateBeforeWorkspace(api, chosenUsername, chosenPassword, null);
                                using var adminForm = new PlatformAdminForm(api);
                                Application.Run(adminForm);
                                if (!adminForm.LogoutRequested) break;
                            }
                            else
                            {
                                api.CompanyName = choice.DisplayName;
                                AuthenticateBeforeWorkspace(api, chosenUsername, chosenPassword, choice.TenantId);
                                using var workspace = new TenantWorkspaceForm(api);
                                Application.Run(workspace);
                                if (!workspace.LogoutRequested) break;
                            }
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "RevCare Auto Hub CRM", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                return;
            }

            while (true)
            {
                using var login = new AdminLoginForm();
                if (login.ShowDialog() != DialogResult.OK) break;

                try
                {
                    using var main = new CrmTransactionsForm();
                    main.WindowState = FormWindowState.Maximized;
                    main.Shown += (s, e) =>
                    {
                        main.Activate();
                        main.BringToFront();
                    };
                    Application.Run(main);
                    if (!main.LogoutRequested) break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open CRM main window:\n\n{ex.Message}\n\n{ex.StackTrace}", "RevCare Auto Hub CRM - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
            }
        }

        private static void AuthenticateBeforeWorkspace(TenantApiClient api, string username, string password, string? tenantId)
        {
            // The login dialog has closed and no main message loop is running yet.
            // Run network work without capturing a WinForms context, then continue
            // creating forms on the original STA thread, including after sign-out.
            Task.Run(() => api.LoginAsync(username, password, tenantId)).GetAwaiter().GetResult();
        }
    }
}
