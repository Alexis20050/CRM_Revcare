using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public partial class JobTasksDialogForm : Form
    {
        private readonly int jobOrderId;
        private readonly Func<MasterCrmDbContext> dbFactory;
        private readonly Dictionary<int, Customer> customerCache = new();
        private JobOrder currentJob;
        private Customer currentCustomer;
        private List<JobTask> currentTasks = new();

        public JobTasksDialogForm(int jobOrderId, Func<MasterCrmDbContext> factory, IEnumerable<Customer> customers = null)
        {
            InitializeComponent();
            this.jobOrderId = jobOrderId;
            this.dbFactory = factory ?? throw new ArgumentNullException(nameof(factory));

            if (customers != null)
            {
                foreach (var c in customers)
                {
                    customerCache[c.CustomerId] = c;
                }
            }

            tasksGrid.AutoGenerateColumns = false;
            GridRowNumbering.Enable(tasksGrid);
            tasksGrid.EnableHeadersVisualStyles = false;
            tasksGrid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            tasksGrid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.FromArgb(30, 41, 59);
            tasksGrid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            tasksGrid.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(224, 242, 254);
            tasksGrid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(15, 23, 42);
            tasksGrid.RowTemplate.Height = 32;
            tasksGrid.GridColor = System.Drawing.Color.FromArgb(226, 232, 240);

            TaskIdCol.DataPropertyName = "JobTaskId";
            ServiceNameCol.DataPropertyName = "ServiceId";
            StatusCol.DataPropertyName = "Status";
            RemarksCol.DataPropertyName = "Remarks";

            string taskSortCol = null;
            bool taskSortAsc = true;
            tasksGrid.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= tasksGrid.Columns.Count) return;
                var col = tasksGrid.Columns[e.ColumnIndex];
                string prop = col.DataPropertyName;
                if (string.IsNullOrEmpty(prop)) return;

                if (taskSortCol == prop) taskSortAsc = !taskSortAsc;
                else { taskSortCol = prop; taskSortAsc = true; }

                foreach (DataGridViewColumn c in tasksGrid.Columns)
                {
                    c.SortMode = DataGridViewColumnSortMode.Programmatic;
                    c.HeaderCell.SortGlyphDirection = (c == col)
                        ? (taskSortAsc ? SortOrder.Ascending : SortOrder.Descending)
                        : SortOrder.None;
                }

                currentTasks = taskSortCol switch
                {
                    "JobTaskId" => taskSortAsc ? currentTasks.OrderBy(t => t.JobTaskId).ToList() : currentTasks.OrderByDescending(t => t.JobTaskId).ToList(),
                    "ServiceId" => taskSortAsc ? currentTasks.OrderBy(t => t.ServiceId).ToList() : currentTasks.OrderByDescending(t => t.ServiceId).ToList(),
                    "Status" => taskSortAsc ? currentTasks.OrderBy(t => t.Status).ToList() : currentTasks.OrderByDescending(t => t.Status).ToList(),
                    "Remarks" => taskSortAsc ? currentTasks.OrderBy(t => t.Remarks ?? "").ToList() : currentTasks.OrderByDescending(t => t.Remarks ?? "").ToList(),
                    _ => currentTasks
                };

                tasksGrid.DataSource = null;
                tasksGrid.DataSource = currentTasks;
            };

            tasksGrid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && tasksGrid.Columns[e.ColumnIndex].DataPropertyName == "JobTaskId" && e.Value is int tid)
                {
                    e.Value = RecordReference.JobTask(tid);
                    e.FormattingApplied = true;
                }
                else if (e.ColumnIndex >= 0 && tasksGrid.Columns[e.ColumnIndex].DataPropertyName == "ServiceId" && e.Value is string sName)
                {
                    e.Value = CRM.infrastructure.services.JobServices.FormatServiceWithPrice(sName);
                    e.FormattingApplied = true;
                }
                else if (e.ColumnIndex >= 0 && tasksGrid.Columns[e.ColumnIndex].DataPropertyName == "Status" && e.Value is string stVal)
                {
                    e.FormattingApplied = true;
                    switch (stVal)
                    {
                        case "Pending":
                            e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(180, 83, 9);
                            break;
                        case "Ongoing":
                            e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(29, 78, 216);
                            e.CellStyle.Font = new System.Drawing.Font(tasksGrid.Font, System.Drawing.FontStyle.Bold);
                            break;
                        case "Done":
                            e.CellStyle.ForeColor = System.Drawing.Color.FromArgb(21, 128, 61);
                            e.CellStyle.Font = new System.Drawing.Font(tasksGrid.Font, System.Drawing.FontStyle.Bold);
                            break;
                    }
                }
            };

            tasksGrid.DataBindingComplete += (s, e) =>
            {
                tasksGrid.ClearSelection();
                tasksGrid.CurrentCell = null;
            };

            tasksGrid.SelectionChanged += (s, e) =>
            {
                bool hasSel = tasksGrid.SelectedRows.Count > 0;
                bool isClosed = currentJob != null && (currentJob.Status is "Completed" or "Released" or "Cancelled" || currentJob.IsArchived);
                btnMarkDone.Enabled = hasSel && !isClosed;
                btnStartTask.Enabled = hasSel && !isClosed;
            };

            tasksGrid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < tasksGrid.Rows.Count)
                {
                    tasksGrid.Rows[e.RowIndex].Selected = true;
                }
            };

            tasksGrid.CellEndEdit += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < tasksGrid.Rows.Count)
                {
                    if (tasksGrid.Rows[e.RowIndex].DataBoundItem is JobTask task)
                    {
                        try
                        {
                            using var db = dbFactory();
                            var strategy = db.Database.CreateExecutionStrategy();
                            strategy.Execute(() =>
                            {
                                var dbTask = db.JobTasks.FirstOrDefault(t => t.JobTaskId == task.JobTaskId && t.JobOrderId == jobOrderId);
                                if (dbTask != null)
                                {
                                    dbTask.Remarks = task.Remarks ?? "";
                                    db.SaveChanges();
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, "Could not save remarks: " + ex.Message, "Save Remarks", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            };

            btnMarkAllDone.Click += (s, e) => UpdateAllTasksStatus("Done");
            btnMarkDone.Click += (s, e) => UpdateSelectedTaskStatus("Done");
            btnStartTask.Click += (s, e) => UpdateSelectedTaskStatus("Ongoing");
            btnPrintMechanicCopy.Click += (s, e) => OpenMechanicCopy();
            btnRefresh.Click += (s, e) => LoadJobAndTasks();
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            Shown += (s, e) => LoadJobAndTasks();
        }

        private void LoadJobAndTasks()
        {
            try
            {
                using var db = dbFactory();
                currentJob = db.JobOrders.AsNoTracking().FirstOrDefault(j => j.JobOrderId == jobOrderId);
                if (currentJob == null)
                {
                    MessageBox.Show(this, $"{RecordReference.JobOrder(jobOrderId)} not found.", "Job Tasks", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Close();
                    return;
                }

                currentCustomer = null;
                if (currentJob.CustomerId.HasValue)
                {
                    if (!customerCache.TryGetValue(currentJob.CustomerId.Value, out currentCustomer))
                    {
                        currentCustomer = db.Customers.AsNoTracking().FirstOrDefault(c => c.CustomerId == currentJob.CustomerId.Value);
                        if (currentCustomer != null) customerCache[currentCustomer.CustomerId] = currentCustomer;
                    }
                }

                currentTasks = db.JobTasks.AsNoTracking().Where(t => t.JobOrderId == jobOrderId).OrderBy(t => t.JobTaskId).ToList();

                // Populate Header
                int doneCount = currentTasks.Count(t => t.Status == "Done");
                decimal totalServices = currentTasks.Sum(t => CRM.infrastructure.services.JobServices.GetServicePrice(t.ServiceId));
                lblTitle.Text = $"{RecordReference.JobOrder(currentJob.JobOrderId)} — Services ({doneCount}/{currentTasks.Count} done — Total: ₱{totalServices:N2})";
                string custName = currentCustomer != null ? currentCustomer.FullName : "No Customer Assigned";
                string custPhone = currentCustomer != null ? currentCustomer.ContactNumber : "N/A";
                lblCustomer.Text = $"Customer: {custName} | Contact: {custPhone}";
                string mechName = !string.IsNullOrWhiteSpace(currentJob.AssignedMechanic) && currentJob.AssignedMechanic != "Unassigned"
                    ? currentJob.AssignedMechanic
                    : "Unassigned";
                lblVehicle.Text = $"Vehicle: {currentJob.VehicleDisplay} | Estimate: {currentJob.EstimateNumber ?? "None"} | Mechanic: {mechName}";
                lblStatus.Text = $"Status: {currentJob.Status} | Total Cost: ₱{(currentJob.TotalEstimatedCost ?? totalServices):N2}";

                string intake = !string.IsNullOrWhiteSpace(currentJob.InquiryNotes)
                    ? currentJob.InquiryNotes
                    : "No specific intake instructions noted.";
                lblIntakeNotes.Text = $"Intake Instructions: {intake}";

                // Bind Tasks Grid
                tasksGrid.DataSource = currentTasks;
                bool isClosed = currentJob != null && (currentJob.Status is "Completed" or "Released" or "Cancelled" || currentJob.IsArchived);
                RemarksCol.ReadOnly = isClosed;

                tasksGrid.ClearSelection();
                tasksGrid.CurrentCell = null;
                btnMarkDone.Enabled = false;
                btnStartTask.Enabled = false;
                btnMarkAllDone.Enabled = !isClosed && currentTasks.Count > 0;
                btnPrintMechanicCopy.Enabled = true;
                if (currentTasks.Count == 0)
                    lblIntakeNotes.Text = "No services assigned. Add requested services through Job Orders.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load job tasks: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool EnsureJobInProgress(MasterCrmDbContext db, JobOrder job)
        {
            if (job.Status != "In Queue") return true;

            decimal totalCost = job.TotalEstimatedCost ?? 0m;
            decimal requiredDeposit = decimal.Round(totalCost * 0.5m, 2, MidpointRounding.AwayFromZero);
            decimal paid = db.Payments.Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided).Sum(p => (decimal?)p.AmountPaid) ?? 0m;

            if (totalCost <= 0)
            {
                MessageBox.Show(this, "Please set the total estimated cost in Job Orders first.", "Cannot Start Work", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (paid < requiredDeposit)
            {
                MessageBox.Show(this, $"Work cannot start yet. A 50% deposit of ₱{requiredDeposit:N2} is required (Paid: ₱{paid:N2}). Record payment first.", "Deposit Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool hasMechanic = !string.IsNullOrWhiteSpace(job.AssignedMechanicUserId)
                || (!string.IsNullOrWhiteSpace(job.AssignedMechanic) && job.AssignedMechanic != "Unassigned");

            if (!hasMechanic)
            {
                var ask = MessageBox.Show(this,
                    "A mechanic must be assigned before work can start.\n\nWould you like to assign an active mechanic now?",
                    "Assign Mechanic Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (ask == DialogResult.Yes)
                {
                    var cust = job.CustomerId.HasValue && customerCache.TryGetValue(job.CustomerId.Value, out var c) ? c : null;
                    using var assignDialog = new AssignMechanicDialogForm(job, cust, dbFactory);
                    if (assignDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        var freshJob = db.JobOrders.Find(job.JobOrderId);
                        if (freshJob != null)
                        {
                            job.AssignedMechanic = freshJob.AssignedMechanic;
                            job.AssignedMechanicUserId = freshJob.AssignedMechanicUserId;
                            hasMechanic = true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (hasMechanic)
            {
                var strategy = db.Database.CreateExecutionStrategy();
                strategy.Execute(() =>
                {
                    using var innerDb = dbFactory();
                    var j = innerDb.JobOrders.Find(job.JobOrderId);
                    if (j != null)
                    {
                        j.Status = "In Progress";
                        if (j.StartedAt < DateTimePicker.MinimumDateTime || j.StartedAt > DateTimePicker.MaximumDateTime)
                            j.StartedAt = DateTime.Now;
                        innerDb.SaveChanges();
                    }
                });
                job.Status = "In Progress";
                return true;
            }

            return false;
        }

        private void UpdateAllTasksStatus(string targetStatus)
        {
            try
            {
                using var db = dbFactory();
                var job = db.JobOrders.FirstOrDefault(j => j.JobOrderId == jobOrderId);
                if (job == null) return;

                if (job.Status is "Completed" or "Released" or "Cancelled" || job.IsArchived)
                {
                    MessageBox.Show(this, $"Job is {job.Status}. Tasks cannot be modified.", "Job Tasks", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (targetStatus == "Done")
                {
                    if (!EnsureJobInProgress(db, job)) return;

                    var activeTasks = db.JobTasks.Where(t => t.JobOrderId == jobOrderId && !t.IsArchived && t.Status != "Withdrawn" && t.Status != "Cancelled").ToList();
                    int pendingCount = activeTasks.Count(t => t.Status == "Pending");
                    if (pendingCount > 0)
                    {
                        MessageBox.Show(this,
                            $"Cannot mark all tasks Done: {pendingCount} task(s) are still Pending (not started).\n\nWorkshop integrity requires that tasks must be started (marked Ongoing) before being completed.",
                            "Pending Tasks Exist", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var ongoingTasks = activeTasks.Where(t => t.Status == "Ongoing").ToList();
                    if (ongoingTasks.Count == 0)
                    {
                        MessageBox.Show(this, "All active tasks for this job are already completed.", "Job Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var confirm = MessageBox.Show(this,
                        $"Mark all {ongoingTasks.Count} ongoing task(s) as Done?\n\nConfirm that workshop repair/inspection for each service is completed.",
                        "Confirm Bulk Task Completion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (confirm != DialogResult.Yes) return;

                    var strategy = db.Database.CreateExecutionStrategy();
                    strategy.Execute(() =>
                    {
                        foreach (var t in ongoingTasks)
                        {
                            t.Status = "Done";
                        }
                        db.SaveChanges();
                    });
                }
                LoadJobAndTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to update all tasks: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateSelectedTaskStatus(string targetStatus)
        {
            if (tasksGrid.SelectedRows.Count == 0 && tasksGrid.Rows.Count > 0)
            {
                tasksGrid.Rows[0].Selected = true;
            }
            if (tasksGrid.SelectedRows.Count == 0 || tasksGrid.SelectedRows[0].DataBoundItem is not JobTask selectedTask)
            {
                MessageBox.Show(this, "Please select a task from the list.", "Select Task", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using var db = dbFactory();
                var job = db.JobOrders.FirstOrDefault(j => j.JobOrderId == jobOrderId);
                if (job == null) return;

                if (job.Status is "Completed" or "Released" or "Cancelled" || job.IsArchived)
                {
                    MessageBox.Show(this, $"Job is {job.Status}. Tasks cannot be modified.", "Job Tasks", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (selectedTask.IsArchived || selectedTask.Status is "Withdrawn" or "Cancelled")
                {
                    MessageBox.Show(this, $"Task #{selectedTask.JobTaskId} is {selectedTask.Status} and cannot be modified.", "Task Status", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (selectedTask.Status is "Done" or "Completed" && targetStatus != selectedTask.Status)
                {
                    MessageBox.Show(this, $"Completed task #{selectedTask.JobTaskId} cannot be moved back to {targetStatus}.", "Reversing Completed Task Prohibited", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (targetStatus == "Done" && selectedTask.Status != "Ongoing")
                {
                    MessageBox.Show(this, $"Task #{selectedTask.JobTaskId} is currently {selectedTask.Status}. Tasks must be started (marked Ongoing) before being marked Done.", "Task Execution State", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (targetStatus is "Ongoing" or "Done")
                {
                    if (!EnsureJobInProgress(db, job)) return;
                }

                var strategy = db.Database.CreateExecutionStrategy();
                strategy.Execute(() =>
                {
                    var dbTask = db.JobTasks.FirstOrDefault(t => t.JobTaskId == selectedTask.JobTaskId && t.JobOrderId == jobOrderId);
                    if (dbTask != null)
                    {
                        dbTask.Status = targetStatus;
                        db.SaveChanges();
                    }
                });
                LoadJobAndTasks();
                tasksGrid.ClearSelection();
                tasksGrid.CurrentCell = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to update task: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void OpenMechanicCopy()
        {
            if (currentJob == null) return;
            try
            {
                var data = new MechanicWorkOrderData
                {
                    JobOrderId = currentJob.JobOrderId,
                    EstimateReference = currentJob.EstimateNumber,
                    CustomerName = currentCustomer?.FullName ?? "Unlinked Customer",
                    VehicleDetails = currentJob.VehicleDisplay,
                    IntakeNotes = currentJob.InquiryNotes ?? "",
                    JobStatus = currentJob.Status ?? "In Progress",
                    PrintDate = DateTime.UtcNow,
                    Tasks = currentTasks.Select(t => new MechanicTaskItem
                    {
                        TaskId = t.JobTaskId,
                        ServiceName = t.ServiceId,
                        Status = t.Status,
                        Notes = t.Remarks
                    }).ToList()
                };
                var business = LegacyBusinessConfig.Load();
                var html = ReportTemplateBuilder.BuildMechanicCopyHtml(data, business);
                ReportPreviewDialogForm.ShowPreview(this, $"Mechanic's Work Order Copy - {RecordReference.JobOrder(currentJob.JobOrderId)}", html);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to generate mechanic work order copy: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
