using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;
using crm.domain.entities;

namespace CRM_Revcare_
{
    internal partial class TenantJobTasksDialogForm : Form
    {
        private readonly TenantApiClient api;
        private readonly string jobOrderId;
        private readonly Func<string, Task> openWorkOrderAction;
        private List<JsonObject> allTasks = new();
        private List<JsonObject> jobTasks = new();
        private JsonObject currentJob;

        public TenantJobTasksDialogForm(TenantApiClient api, string jobOrderId, Func<string, Task> openWorkOrder = null)
        {
            InitializeComponent();
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.jobOrderId = jobOrderId ?? throw new ArgumentNullException(nameof(jobOrderId));
            this.openWorkOrderAction = openWorkOrder;

            tasksGrid.AutoGenerateColumns = false;
            GridRowNumbering.Enable(tasksGrid);
            TaskIdCol.DataPropertyName = "jobTaskId";
            ServiceNameCol.DataPropertyName = "serviceId";
            StatusCol.DataPropertyName = "status";
            ReviewStatusCol.DataPropertyName = "reviewStatus";
            AssignedToCol.DataPropertyName = "assignedTo";
            RemarksCol.DataPropertyName = "remarks";

            tasksGrid.SelectionChanged += (_, _) => UpdateButtonStates();
            btnStartTask.Click += async (_, _) => await ChangeSelectedTaskStatus("Ongoing");
            btnFinishTask.Click += async (_, _) => await ChangeSelectedTaskStatus("Done");
            btnSubmitReview.Click += async (_, _) => await SubmitSelectedTaskForReview();
            btnRefresh.Click += async (_, _) => await LoadTasksAsync();
            btnPrintWorkOrder.Click += async (_, _) =>
            {
                if (openWorkOrderAction != null)
                {
                    await openWorkOrderAction(jobOrderId);
                }
            };
            btnClose.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

            Shown += async (_, _) => await LoadTasksAsync();
        }

        private JsonObject SelectedTask =>
            tasksGrid.SelectedRows.Count > 0 && tasksGrid.SelectedRows[0].DataBoundItem is JsonObject obj ? obj : null;

        private void UpdateButtonStates()
        {
            var task = SelectedTask;
            bool hasSel = task != null;
            bool canUpdate = api.Has("Tasks.Update") || api.Has("Tasks.UpdateAssigned");
            bool canQa = api.Has("Jobs.QA");

            string taskStatus = task?["status"]?.ToString() ?? "";
            string reviewStatus = task?["reviewStatus"]?.ToString() ?? "Not Submitted";
            bool unlocked = reviewStatus is not ("Submitted" or "Approved");

            btnStartTask.Enabled = hasSel && canUpdate && unlocked && taskStatus == "Pending";
            btnFinishTask.Enabled = hasSel && canUpdate && unlocked && taskStatus == "Ongoing";
            btnSubmitReview.Enabled = hasSel && canUpdate && unlocked && taskStatus == "Done";
            btnPrintWorkOrder.Enabled = true;
        }

        private async Task LoadTasksAsync()
        {
            try
            {
                var data = await api.Send("GET", "crm/workshop");
                allTasks = data.AsArray().Select(n => (JsonObject)n).ToList();

                // Find job metadata
                currentJob = allTasks.FirstOrDefault(t => t["jobOrderId"]?.ToString() == jobOrderId);
                jobTasks = allTasks.Where(t => t["jobOrderId"]?.ToString() == jobOrderId && (t["jobTaskId"]?.GetValue<long>() ?? 0) > 0).ToList();

                string formattedJob = int.TryParse(jobOrderId, out int jid) ? RecordReference.JobOrder(jid) : $"Job #{jobOrderId}";
                lblTitle.Text = $"{formattedJob} — Assigned Tasks ({jobTasks.Count} service(s))";
                string cust = currentJob?["customer"]?.ToString() ?? "N/A";
                string vehicle = currentJob?["vehicleDetails"]?.ToString() ?? "N/A";
                string stage = currentJob?["jobStatus"]?.ToString() ?? "In Queue";

                lblCustomer.Text = $"Customer: {cust}";
                lblVehicle.Text = $"Vehicle: {vehicle}";
                lblStatus.Text = $"Status: {stage}";

                tasksGrid.DataSource = jobTasks;
                tasksGrid.ClearSelection();
                tasksGrid.CurrentCell = null;
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load tasks: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ChangeSelectedTaskStatus(string newStatus)
        {
            var task = SelectedTask;
            if (task == null) return;
            try
            {
                await api.Send("PUT", "crm/job-tasks/" + task["jobTaskId"], new JsonObject
                {
                    ["status"] = newStatus,
                    ["remarks"] = task["remarks"]?.DeepClone() ?? JsonValue.Create(""),
                    ["rowVersion"] = task["rowVersion"].DeepClone()
                });
                await LoadTasksAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to update task: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SubmitSelectedTaskForReview()
        {
            var task = SelectedTask;
            if (task == null) return;
            using var dialog = new TenantTaskReviewForm();
            dialog.Bind("Submit task for review",
                new JsonObject { ["rowVersion"] = task["rowVersion"].DeepClone(), ["result"] = "Submitted" },
                new() { new TenantField("notes", "Review notes", Required: false) },
                async body => { await api.Send("POST", "crm/job-tasks/" + task["jobTaskId"] + "/submit", body); });
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                await LoadTasksAsync();
            }
        }
    }
}
