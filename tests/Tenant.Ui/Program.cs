using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using CRM_Revcare_;

class Program
{
    [STAThread] static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, _) => { };
        Application.EnableVisualStyles();
        using var api = new TenantApiClient("https://localhost", new Stub());
        var startup = typeof(TenantWorkspaceForm).Assembly.GetType("CRM_Revcare_.Program")!;
        var entry = startup.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static)!;
        if (entry.ReturnType != typeof(void) || entry.GetCustomAttribute<STAThreadAttribute>() == null)
            throw new Exception("WinForms entry point must stay synchronous and STA.");
        int startupThread = Environment.CurrentManagedThreadId;
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
        try
        {
            // Exercise delayed login with a WinForms context, including repeat login after sign-out.
            for (int attempt = 0; attempt < 2; attempt++)
            {
                startup.GetMethod("AuthenticateBeforeWorkspace", BindingFlags.NonPublic | BindingFlags.Static)!
                    .Invoke(null, new object[] { api, "mechanic", "test", "tenant-a" });
                if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA || Environment.CurrentManagedThreadId != startupThread)
                    throw new Exception("Authentication moved startup off its STA thread.");
            }
        }
        finally { SynchronizationContext.SetSynchronizationContext(previousContext); }
        Console.WriteLine("PASS: Delayed authentication and repeat login preserve the STA startup thread.");
        using var workspace = new TenantWorkspaceForm(api);
        workspace.Show();
        Application.DoEvents();
        var navigation = Field<FlowLayoutPanel>(workspace, "navigation");
        var menus = navigation.Controls.OfType<RadioButton>().Where(b => b.Visible).ToList();
        if (menus.Count != 1 || menus[0].Text != "My Assigned Tasks") throw new Exception("Technician navigation exposes forbidden modules.");
        if (Field<Button>(workspace, "approve").Visible || Field<Button>(workspace, "rework").Visible) throw new Exception("Technician QA approval buttons must be hidden.");
        var grid = Field<DataGridView>(workspace, "records");
        for (int i = 0; i < 50 && grid.Rows.Count < 2; i++) { Application.DoEvents(); Thread.Sleep(10); }
        if (grid.Rows.Count != 2) throw new Exception("Assigned rows missing.");
        grid.Sort(grid.Columns["jobTaskId"], System.ComponentModel.ListSortDirection.Descending);
        grid.CurrentCell = grid.Rows[0].Cells["jobTaskId"];
        var selected = (JsonObject)workspace.GetType().GetProperty("Selected", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(workspace);
        if (selected["jobTaskId"].GetValue<int>() != 2) throw new Exception("Sorted grid selected the wrong task.");
        foreach (var size in new[] { new Size(1000, 650), new Size(1260, 760), new Size(1600, 1000) })
        {
            workspace.Size = size; Application.DoEvents();
            var toolbar = Field<FlowLayoutPanel>(workspace, "actions");
            if (grid.PointToScreen(Point.Empty).Y < toolbar.PointToScreen(new Point(0, toolbar.Height)).Y || grid.Height < 100) throw new Exception("Workspace grid is hidden by toolbar.");
        }
        Console.WriteLine("PASS: Technician navigation, assigned rows, sorted selection and layout at three sizes.");
        using var editor = new TenantCustomerForm();
        editor.Bind("Add Customer", new JsonObject(), new() { new("fullName", "Name", Required: true) }, _ => Task.CompletedTask);
        editor.Show(); Application.DoEvents();
        if (!editor.Controls.Find("fullName", true).Single().Visible) throw new Exception("Editor not visible.");
        editor.Close();
        using (var bitmap = new Bitmap(workspace.Width, workspace.Height))
        {
            workspace.DrawToBitmap(bitmap, new Rectangle(Point.Empty, workspace.Size));
            bitmap.Save("tests/Tenant.Ui/workshop.png");
        }
        workspace.Close();
        foreach (var form in new TenantEntryForm[] { new TenantJobOrderForm(), new TenantTaskForm(), new TenantPaymentForm(), new TenantStaffForm(), new TenantInquiryForm(), new TenantComplaintForm(), new TenantFeedbackForm(), new TenantServiceForm(), new TenantTaskReviewForm() })
            using (form) { form.Show(); Application.DoEvents(); form.Close(); }
        using (var selector = new TenantSelectorDialogForm(true, new[] { new TenantResolutionInfo("tenant-a", "Tenant A", "OwnerManager", "Ready", true) }))
        {
            selector.Show(); Application.DoEvents(); selector.Close();
        }
        using (var adminForm = new PlatformAdminForm(api))
        {
            adminForm.Show(); Application.DoEvents(); adminForm.Close();
        }
        Console.WriteLine("PASS: Designer forms and administration dialogs open cleanly without errors.");
        Environment.Exit(0);
    }
    static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);
    sealed class Stub : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri.AbsolutePath.StartsWith("/auth/"))
                await Task.Delay(20, cancellationToken).ConfigureAwait(false);
            string body = request.RequestUri.AbsolutePath switch
            {
                "/auth/login" => "{\"accessToken\":\"test\"}",
                "/auth/me" => "{\"role\":\"AutomotiveMechanic\",\"permissions\":[\"Workshop.Read\",\"Tasks.UpdateAssigned\"],\"enabledModules\":[\"JobTasks\"]}",
                "/crm/workshop" => "[{\"jobTaskId\":1,\"jobOrderId\":1,\"serviceId\":\"Oil change\",\"status\":\"Pending\"},{\"jobTaskId\":2,\"jobOrderId\":1,\"serviceId\":\"Brake inspection\",\"status\":\"Pending\"}]",
                "/master/tenants" => "[{\"tenantId\":\"tenant-a\",\"displayName\":\"Tenant A\",\"connectionKey\":\"TenantA\",\"status\":\"Ready\",\"isActive\":true}]",
                "/master/tenants/tenant-a/modules" => "[]",
                "/master/audit" => "[]",
                _ => throw new Exception("Unexpected endpoint: " + request.RequestUri.AbsolutePath)
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
