using CRM_Revcare_;
class Program
{
    [STAThread] static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, _) => { };
        Application.EnableVisualStyles();
        using var host = new Form { Size = new Size(1300, 850) };
        var page = new JobTaskPage { Dock = DockStyle.Fill };
        host.Controls.Add(page);
        var jobs = (DataGridView)page.Controls.Find("JobsOverview", true).Single();
        jobs.DataSource = new[] { new { JobOrderId = 31, Customer = "Nin Edano", Vehicle = "Montero", Progress = "1 / 4 done", Status = "In Queue" } };
        page.Grid.DataSource = new[] { new { JobTaskId = 1, JobOrderId = 31, ServiceId = "Oil change", Remarks = "Inspect engine", Status = "Pending" } };
        host.Show(); Application.DoEvents();
        foreach (var size in new[] { new Size(1300, 850), new Size(1000, 700), new Size(1600, 1000) })
        {
            host.Size = size; Application.DoEvents();
            var workspace = page.Controls.Find("TaskWorkspace", true).Single();
            var toolbar = page.Controls.Find("EntryActions", true).Single();
            if (workspace.Top < toolbar.Bottom) throw new Exception("Toolbar overlaps the job workspace.");
            if (!jobs.Visible || jobs.Height < 200 || page.Grid.Visible) throw new Exception("Main workspace must show only the expanded job list.");
            if (!((SplitContainer)workspace).Panel2Collapsed) throw new Exception("Embedded task pane must be collapsed.");
            if (!page.Controls.Find("ViewTasksPopUp", true).Single().Visible) throw new Exception("View Tasks action must remain accessible.");
            var popupAction = page.Controls.Find("ViewTasksPopUp", true).Single();
            if (jobs.RectangleToScreen(jobs.ClientRectangle).IntersectsWith(popupAction.RectangleToScreen(popupAction.ClientRectangle)))
                throw new Exception("Job grid overlaps the popup action.");
            var cell = jobs.GetCellDisplayRectangle(0, 0, true);
            var screenCell = jobs.RectangleToScreen(cell);
            if (!workspace.RectangleToScreen(workspace.ClientRectangle).Contains(screenCell))
                throw new Exception("First job row is clipped outside the workspace.");
            Console.WriteLine($"PASS: Expanded job list and popup action at {size.Width}x{size.Height}");
        }
        using var bitmap = new Bitmap(host.Width, host.Height);
        host.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.Size));
        bitmap.Save("tests/Layout.Smoke/layout.png");
        page.Hide();
        foreach (Control module in new Control[] { new InquiryPage(), new JobOrderPage() })
        {
            using (module)
            {
                module.Dock = DockStyle.Fill;
                host.Controls.Add(module);
                module.BringToFront();
                Application.DoEvents();
                using var capture = new Bitmap(host.Width, host.Height);
                host.DrawToBitmap(capture, new Rectangle(Point.Empty, host.Size));
                capture.Save($"tests/Layout.Smoke/{module.GetType().Name}.png");
            }
        }
        host.Close();
        Environment.Exit(0);
    }
    static IEnumerable<Control> All(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(All(c)));
}
