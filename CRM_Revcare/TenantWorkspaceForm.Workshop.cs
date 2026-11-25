using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CRM_Revcare_;

internal partial class TenantWorkspaceForm
{
    private string selectedJobId;
    private bool bindingJobs;
    private static string[] DisplayColumns(string path) => path switch
    {
        "workshop" => new[] { "jobTaskId", "serviceId", "remarks", "status", "reviewStatus", "assignedTo", "reviewNotes", "startedAtUtc", "finishedAtUtc" },
        "job-orders" => new[] { "jobOrderId", "customer", "vehicleDisplay", "estimateNumber", "status", "assignedMechanic", "totalEstimatedCost", "totalPaid", "outstandingBalance", "startedAt" },
        "payments" => new[] { "paymentId", "customer", "jobOrderId", "paymentType", "amountPaid", "paymentMethod", "billingStatus", "jobBalance", "isVoided", "createdAt" },
        "staff" => new[] { "userName", "displayName", "role", "profileProvisioningStatus", "provisioningFailureReason", "isActive" },
        "customers" => new[] { "customerId", "fullName", "contactNumber", "email" },
        "inquiries" => new[] { "inquiryId", "customer", "customerName", "inquiryPlatform", "messageDetails", "status" },
        "appointments" => new[] { "appointmentId", "customerName", "vehicleDisplay", "scheduledStart", "status", "requestedServices", "notes", "linkedJobOrderId" },
        "estimates" => new[] { "estimateId", "estimateNumber", "customerName", "vehicleDisplay", "status", "totalAmount", "approvedAt", "createdAt" },
        "complaints" => new[] { "complaintId", "jobOrderId", "complaintType", "description", "resolutionStatus", "actionTaken", "isUrgent", "underWarrantyService" },
        "feedbacks" => new[] { "feedbackId", "jobOrderId", "ratingScore", "positiveRemarks", "areasForImprovement" },
        _ => null
    };
    private void ConfigureNavigation()
    {
        RadioButton[] buttons = { customersMenu, inquiriesMenu, appointmentsMenu, ordersMenu, estimatesMenu, tasksMenu, servicesMenu, assignmentsMenu, paymentsMenu, complaintsMenu, feedbackMenu, reportsMenu, staffMenu, staffHistoryMenu, auditMenu };
        string[] paths = { "customers", "inquiries", "appointments", "job-orders", "estimates", "workshop", "services", "workshop/assignment-history", "payments", "complaints", "feedbacks", "reports/operational-summary", "staff", "staff/access-history", "audit" };
        for (int i = 0; i < paths.Length; i++)
        {
            var module = modules.FirstOrDefault(m => m.Path == paths[i]);
            var button = buttons[i]; button.Tag = module; button.Visible = module != null;
            navigation.Controls[navigation.Controls.IndexOf(button) + 1].Visible = module != null;
            if (module == null) continue;
            button.Text = module.Name;
            button.CheckedChanged += async (_, _) =>
            {
                if (!button.Checked) return;
                selectedModule = (Module)button.Tag; selectedJobId = null; selectedRecordId = null; rows.Clear(); search.Clear();
                await LoadAsync();
                if (selectedRecordId == null)
                {
                    records.CurrentCell = null;
                    records.ClearSelection();
                    UpdateRecordActionButtons();
                    UpdateTaskButtons();
                }
            };
        }

        headerFrontOffice.Visible = customersMenu.Visible || inquiriesMenu.Visible || appointmentsMenu.Visible;
        headerWorkshop.Visible = !api.Has("Tasks.UpdateAssigned") && (ordersMenu.Visible || estimatesMenu.Visible || tasksMenu.Visible || servicesMenu.Visible || assignmentsMenu.Visible);
        headerBillingCare.Visible = paymentsMenu.Visible || complaintsMenu.Visible || feedbackMenu.Visible || reportsMenu.Visible;
        headerAdmin.Visible = staffMenu.Visible || staffHistoryMenu.Visible || auditMenu.Visible;
    }
    private void ConfigureWorkshopActions()
    {
        qaOnly.CheckedChanged += (_, _) => { selectedJobId = null; selectedRecordId = null; Filter(); };
        records.SelectionChanged += (_, _) => UpdateTaskButtons();
        startTask.Click += async (_, _) => await Run(() => ChangeTask("Ongoing"));
        finishTask.Click += async (_, _) => await Run(() => ChangeTask("Done"));
        submit.Click += async (_, _) => await Run(() => ReviewTask("Submitted"));
        approve.Click += async (_, _) => await Run(() => ReviewTask("Approved"));
        rework.Click += async (_, _) => await Run(() => ReviewTask("Rework"));
        allJobs.Click += (_, _) =>
        {
            selectedJobId = null;
            bindingJobs = true;
            jobsOverview.ClearSelection();
            jobsOverview.CurrentCell = null;
            bindingJobs = false;
            Filter(false);
            jobSummary.Text = "Select a job to view its service tasks.";
        };

        async Task OpenWorkshopTasksPopUp(string jid)
        {
            using var dlg = new TenantJobTasksDialogForm(api, jid, async id => await OpenWorkOrderAsync());
            dlg.ShowDialog(this);
            await Reload();
        }

        viewTasksPopUp.Click += async (_, _) =>
        {
            if (selectedJobId != null) await OpenWorkshopTasksPopUp(selectedJobId);
        };

        jobsOverview.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < jobsOverview.Rows.Count &&
                jobsOverview.Rows[e.RowIndex].DataBoundItem is DataRowView row)
            {
                string jid = row["Job"].ToString();
                await OpenWorkshopTasksPopUp(jid);
            }
        };

        jobsOverview.SelectionChanged += (_, _) =>
        {
            if (bindingJobs || Current?.Path != "workshop") return;
            if (jobsOverview.SelectedRows.Count == 0)
            {
                selectedJobId = null;
                jobSummary.Text = "Select a job to view its service tasks.";
                Filter(false);
                return;
            }
            var row = jobsOverview.SelectedRows[0].DataBoundItem as DataRowView;
            if (row == null)
            {
                selectedJobId = null;
                jobSummary.Text = "Select a job to view its service tasks.";
                Filter(false);
                return;
            }
            selectedJobId = row["Job"].ToString();
            Filter(false); // Never rebind the upper grid in its own selection event.
        };
        jobsOverview.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.RowIndex < jobsOverview.Rows.Count)
            {
                if (jobsOverview.Rows[e.RowIndex].DataBoundItem is DataRowView row)
                {
                    selectedJobId = row["Job"].ToString();
                    Filter(false);
                }
            }
        };
        ConfigureWorkshopVisibility();
    }
    private void ConfigureWorkshopVisibility()
    {
        bool work = Current?.Path == "workshop";
        bool update = api.Has("Tasks.Update") || api.Has("Tasks.UpdateAssigned");
        startTask.Visible = finishTask.Visible = submit.Visible = work && update;
        approve.Visible = rework.Visible = work && api.Has("Jobs.QA");
        qaOnly.Visible = work && api.Has("Jobs.QA");
        allJobs.Visible = work;
        viewTasksPopUp.Visible = work;
    }
    private void UpdateTaskButtons()
    {
        if (Current?.Path != "workshop") return;
        var row = Selected;
        bool active = row?["jobStatus"]?.ToString() is "In Progress" or "QA Checking";
        string review = row?["reviewStatus"]?.ToString() ?? "Not Submitted";
        bool unlocked = review is not ("Submitted" or "Approved");
        startTask.Enabled = active && unlocked && row?["status"]?.ToString() == "Pending";
        finishTask.Enabled = active && unlocked && row?["status"]?.ToString() == "Ongoing";
        submit.Enabled = active && unlocked && row?["status"]?.ToString() == "Done";
        approve.Enabled = active && review == "Submitted";
        rework.Enabled = active && review is "Submitted" or "Approved";
    }
    private void BindJobOverview(List<JsonObject> matching)
    {
        bindingJobs = true;
        try
        {
            var table = new DataTable();
            foreach (string name in new[] { "Job", "Customer", "Vehicle", "Progress", "Status", "Review" }) table.Columns.Add(name);
            foreach (var group in matching.GroupBy(r => r["jobOrderId"]?.ToString()))
            {
                var row = group.First();
                int count = row["jobTaskCount"]?.GetValue<int>() ?? group.Count(t => (t["jobTaskId"]?.GetValue<long>() ?? 0) > 0);
                int done = row["jobDoneCount"]?.GetValue<int>() ?? group.Count(t => t["status"]?.ToString() == "Done");
                int submitted = row["jobSubmittedCount"]?.GetValue<int>() ?? 0;
                string stage = row["jobStatus"]?.ToString() ?? "";
                string review = stage is "Completed" or "Released" ? "Closed" : count > 0 && submitted == count ? "Ready for QA" : group.Any(t => t["reviewStatus"]?.ToString() == "Rework") ? "Rework required" : "Work in progress";
                table.Rows.Add(group.Key, row["customer"]?.ToString() ?? "", row["vehicleDetails"]?.ToString() ?? "", $"{done} / {count} done", stage, review);
            }
            jobsOverview.DataSource = table;
            jobsOverview.ClearSelection();
            jobsOverview.CurrentCell = null;
            if (selectedJobId != null)
            {
                foreach (DataGridViewRow row in jobsOverview.Rows)
                    if (row.Cells["Job"].Value?.ToString() == selectedJobId) { jobsOverview.CurrentCell = row.Cells[0]; row.Selected = true; break; }
            }
            if (selectedJobId == null)
            {
                jobSummary.Text = "Select a job to view its service tasks.";
            }
            else
            {
                var selJobTasks = matching.Where(r => r["jobOrderId"]?.ToString() == selectedJobId && (r["jobTaskId"]?.GetValue<long>() ?? 0) > 0).ToList();
                if (selJobTasks.Count == 0)
                    jobSummary.Text = $"Job #{selectedJobId} has zero service tasks. Requested services must be added through Job Orders.";
                else
                    jobSummary.Text = $"Tasks for Job #{selectedJobId} ({selJobTasks.Count} task(s))";
            }
        }
        finally { bindingJobs = false; }
    }
    private async Task ChangeTask(string statusValue)
    {
        var row = RequireSelection();
        await api.Send("PUT", "crm/job-tasks/" + row["jobTaskId"], new JsonObject { ["status"] = statusValue, ["remarks"] = row["remarks"]?.DeepClone() ?? JsonValue.Create(""), ["rowVersion"] = row["rowVersion"].DeepClone() });
        await Reload();
    }
    private async Task ReviewTask(string result)
    {
        var row = RequireSelection();
        using var dialog = new TenantTaskReviewForm();
        dialog.Bind(result == "Submitted" ? "Submit task for review" : result == "Approved" ? "Approve QA" : "Return task for rework",
            new JsonObject { ["rowVersion"] = row["rowVersion"].DeepClone(), ["result"] = result },
            new() { new TenantField("notes", result == "Rework" ? "Rework reason" : "Review notes", Required: result == "Rework") },
            async body => { await api.Send("POST", "crm/job-tasks/" + row["jobTaskId"] + (result == "Submitted" ? "/submit" : "/review"), body); });
        if (dialog.ShowDialog(this) == DialogResult.OK) await Reload();
    }
}
