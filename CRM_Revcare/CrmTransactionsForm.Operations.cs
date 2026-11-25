using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public partial class CrmTransactionsForm
    {
        private static readonly string[] JobOrderStages = ["In Queue", "In Progress", "Completed", "Released"];
        private Action<int> showJobTasks;
        private Action refreshTaskJobs;
        private static string BillingStatus(JobOrder job) => job == null ? "Select a job" :
            job.TotalEstimatedCost == null ? "Estimate needed" :
            job.OutstandingBalance == 0 ? (job.CustomerCredit > 0 ? "Fully Paid - Customer Credit" : "Fully Paid") :
            job.TotalPaid > 0 ? "Partially Paid" : "Unpaid";

        private CrmCollectionController<JobOrder> BuildJobOrders(Func<MasterCrmDbContext> factory, Func<string, bool> confirm)
        {
            var tab = new CrmCollectionController<JobOrder>(ordersPage, "Job Order", x => x.JobOrderId, factory, confirm);
            UseEntryDialog(tab, ordersPage.editorPanel, ordersPage.resultLabel, ordersPage.saveButton, "Job Order");
            var customerLookup = BindCustomer(ordersPage.customer, ordersPage, factory);
            customerLookup.AttachButtons(ordersPage.browseCustomerButton, ordersPage.clearCustomerButton);

            ordersPage.assignedMechanic.Visible = true;
            ordersPage.assignedMechanicLabel.Visible = true;
            ordersPage.assignMechanicButtonLeft.Visible = true;
            ordersPage.assignMechanicDialogButton.Visible = true;
            ordersPage.assignMechanicButton.Visible = true;
            if (ordersPage.assignedMechanicColumn != null)
            {
                ordersPage.assignedMechanicColumn.Visible = true;
            }

            ordersPage.estimatedCost.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                if (char.IsDigit(e.KeyChar)) return;
                if (e.KeyChar == '.' && !ordersPage.estimatedCost.Text.Contains('.')) return;
                e.Handled = true;
            };
            ordersPage.estimatedCost.Leave += (s, e) =>
            {
                if (decimal.TryParse(ordersPage.estimatedCost.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var v) ||
                    decimal.TryParse(ordersPage.estimatedCost.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out v))
                {
                    if (v > 0) ordersPage.estimatedCost.Text = v.ToString("N2");
                }
            };
            ordersPage.requestedServices.Items.Clear();
            ordersPage.requestedServices.Items.AddRange(CRM.infrastructure.services.JobServices.OfferedWithPrices);
            ordersPage.requestedServicesLabel.Text = "Requested Services & Prices (determines job order cost):";

            bool updatingServicesTotal = false;
            void RecalculateServicesTotal()
            {
                if (updatingServicesTotal || ordersPage.estimatedCost.ReadOnly) return;
                decimal total = 0m;
                foreach (var item in ordersPage.requestedServices.CheckedItems)
                {
                    total += CRM.infrastructure.services.JobServices.GetServicePrice(item?.ToString() ?? "");
                }
                if (total > 0 || ordersPage.requestedServices.CheckedItems.Count == 0)
                {
                    ordersPage.estimatedCost.Text = total > 0 ? total.ToString("N2") : "";
                    ordersPage.requestedServicesLabel.Text = total > 0
                        ? $"Requested Services & Prices (Total: ₱{total:N2}):"
                        : "Requested Services & Prices (determines job order cost):";
                }
            }

            ordersPage.requestedServices.ItemCheck += (s, e) =>
            {
                if (updatingServicesTotal) return;
                ordersPage.BeginInvoke(new Action(() => RecalculateServicesTotal()));
            };

            var estimate = ordersPage.estimate;
            var status = ordersPage.status;
            var started = ordersPage.started;
            JobOrder editingOrder = new();
            int unfinishedTasks = 0;
            void UpdateStartEligibility()
            {
                bool validCost = decimal.TryParse(ordersPage.estimatedCost.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var cost)
                    || decimal.TryParse(ordersPage.estimatedCost.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out cost);
                validCost = validCost && cost >= 0 && decimal.Round(cost, 2) == cost;
                decimal required = validCost ? decimal.Round(cost * .5m, 2, MidpointRounding.AwayFromZero) : 0;
                decimal paid = editingOrder.TotalPaid;
                bool starting = editingOrder.Status == "In Queue" && status.Text == "In Progress";
                bool releasing = status.Text == "Released";
                bool inspecting = status.Text != editingOrder.Status && status.Text == "Completed";
                bool hasMechanic = !string.IsNullOrWhiteSpace(editingOrder.AssignedMechanicUserId)
                    || (!string.IsNullOrWhiteSpace(editingOrder.AssignedMechanic) && editingOrder.AssignedMechanic != "Unassigned")
                    || (!string.IsNullOrWhiteSpace(ordersPage.assignedMechanic.Text) && ordersPage.assignedMechanic.Text != "Unassigned");
                bool blocked = (starting && (!validCost || paid < required))
                    || (releasing && (!validCost || paid < cost)) || (inspecting && (unfinishedTasks > 0 || !ordersPage.inspectionPassed.Checked));
                ordersPage.inspectionPassed.Visible = inspecting;
                ordersPage.saveButton.Enabled = !blocked;
                ordersPage.saveButton.Text = status.Text == editingOrder.Status ? "Save Details" : status.Text switch
                {
                    "In Progress" => "Start Work",
                    "Completed" => "Confirm Inspection Passed",
                    "Released" => "Release Vehicle",
                    _ => "Save Job Order"
                };
                ordersPage.paymentRequirement.ForeColor = blocked ? Color.Firebrick : SystemColors.ControlText;
                if (inspecting)
                {
                    ordersPage.paymentRequirement.Text = unfinishedTasks > 0
                        ? $"{unfinishedTasks} task(s) still need work. Finish them in Job Tasks, then reopen this job."
                        : "Tick the final inspection confirmation below after inspecting the repairs.";
                    ordersPage.resultLabel.Text = ordersPage.paymentRequirement.Text;
                    return;
                }
                if (releasing)
                {
                    ordersPage.paymentRequirement.Text = !validCost
                        ? "Set a valid estimated cost before releasing this vehicle."
                        : $"Estimated cost: PHP {cost:N2}\nValid payments: PHP {paid:N2}\nOutstanding balance: PHP {Math.Max(0, cost - paid):N2}\n" +
                            (paid < cost ? "Keep Completed. Settle the balance in Payments, then reopen this job to release it."
                                         : "Fully paid. This vehicle can be released.");
                    ordersPage.resultLabel.Text = !validCost ? "Release blocked: set a valid estimated cost."
                        : paid < cost ? $"Release blocked: PHP {cost - paid:N2} unpaid. Record payment in Payments, then refresh payment status."
                        : "Fully paid. Click Release Vehicle to save the release.";
                    return;
                }
                if (starting)
                {
                    if (!hasMechanic)
                    {
                        ordersPage.paymentRequirement.Text = (validCost && paid >= required)
                            ? "A mechanic must be assigned before work can start. Click 'Assign Mechanic' or click 'Start Work' to assign now."
                            : "A mechanic must be assigned before work can start. Click 'Assign Mechanic' first.";
                        ordersPage.resultLabel.Text = "Assign a mechanic before starting work. Use Assign Mechanic button or click Start Work.";
                        ordersPage.saveButton.Enabled = validCost && paid >= required;
                        return;
                    }
                    ordersPage.paymentRequirement.Text = !validCost
                        ? "Set a valid estimated cost before starting work."
                        : $"50% deposit required: PHP {required:N2}\nValid payments: PHP {paid:N2}\nRemaining deposit: PHP {Math.Max(0, required - paid):N2}\n" +
                          (paid < required ? $"₱{(required - paid):N2} more deposit required before work can start." : "Deposit requirement met. You can move to In Progress.");
                    ordersPage.resultLabel.Text = (validCost && paid >= required)
                        ? "Deposit requirement met. You can move to In Progress."
                        : $"₱{Math.Max(0, required - paid):N2} more deposit required before work can start.";
                    return;
                }
                ordersPage.paymentRequirement.Text = editingOrder.Status != "In Queue" ? "" : !validCost
                    ? "Set a valid estimated cost before starting work."
                    : $"50% deposit required: PHP {required:N2}\nValid payments: PHP {paid:N2}\nRemaining deposit: PHP {Math.Max(0, required - paid):N2}\n" +
                        (paid < required ? "Keep In Queue, record a payment, then reopen this job." : "Deposit requirement met. You can move to In Progress.");
            }
            status.SelectedIndexChanged += (s, e) => UpdateStartEligibility();
            ordersPage.inspectionPassed.CheckedChanged += (s, e) => UpdateStartEligibility();
            ordersPage.estimatedCost.TextChanged += (s, e) => UpdateStartEligibility();
            ordersPage.refreshPaymentStatus.Click += (_, _) =>
            {
                try
                {
                    using var db = factory();
                    editingOrder.Payments = db.Payments.AsNoTracking().Where(p => p.JobOrderId == editingOrder.JobOrderId).ToList();
                    unfinishedTasks = db.JobTasks.Count(t => t.JobOrderId == editingOrder.JobOrderId && t.Status != "Done");
                    UpdateStartEligibility();
                }
                catch
                {
                    ordersPage.saveButton.Enabled = false;
                    ordersPage.resultLabel.Text = "Could not refresh payments. Check the connection and try Refresh payment status again.";
                }
            };
            ConfigureJobOrderList(tab);
            ordersPage.viewTasksButton.Enabled = false;
            ordersPage.printMechanicCopyButton.Enabled = false;
            ordersPage.assignMechanicButton.Enabled = false;
            ordersPage.cancelJobButton.Enabled = false;
            ordersPage.Grid.SelectionChanged += (s, e) =>
            {
                bool hasJob = tab.SelectedRecord != null;
                bool canAssign = hasJob && tab.SelectedRecord is JobOrder selJob && selJob.Status != "Completed" && selJob.Status != "Released" && selJob.Status != "Cancelled";
                bool canCancel = hasJob && tab.SelectedRecord is JobOrder cJob && cJob.Status != "Completed" && cJob.Status != "Released" && cJob.Status != "Cancelled";
                ordersPage.viewTasksButton.Enabled = hasJob;
                ordersPage.printMechanicCopyButton.Enabled = hasJob;
                ordersPage.assignMechanicButton.Enabled = canAssign;
                ordersPage.cancelJobButton.Enabled = canCancel;
                ordersPage.archiveJobButton.Enabled = hasJob;
            };
            ordersPage.viewTasksButton.Click += (s, e) =>
            {
                if (tab.SelectedRecord is JobOrder selected) showJobTasks?.Invoke(selected.JobOrderId);
                else ordersPage.resultLabel.Text = "Select a job order to view its services.";
            };
            ordersPage.printMechanicCopyButton.Click += (s, e) =>
            {
                if (tab.SelectedRecord is JobOrder selected) OpenMechanicCopy(selected, factory);
                else ordersPage.resultLabel.Text = "Select a job order to preview its mechanic's copy.";
            };
            void TriggerAssignMechanic()
            {
                var jobToAssign = (editingOrder != null && editingOrder.JobOrderId > 0) ? editingOrder : (tab.SelectedRecord as JobOrder);
                if (jobToAssign != null && jobToAssign.JobOrderId > 0)
                {
                    if (jobToAssign.Status == "Completed" || jobToAssign.Status == "Released" || jobToAssign.Status == "Cancelled")
                    {
                        ordersPage.resultLabel.Text = "Completed, released, or cancelled jobs cannot be reassigned.";
                        return;
                    }
                    var cust = jobToAssign.CustomerId.HasValue && customerRecords.TryGetValue(jobToAssign.CustomerId.Value, out var c) ? c : null;
                    using var dialog = new AssignMechanicDialogForm(jobToAssign, cust, factory);
                    if (dialog.ShowDialog(ordersPage.FindForm()) == DialogResult.OK)
                    {
                        if (editingOrder != null && editingOrder.JobOrderId == jobToAssign.JobOrderId)
                        {
                            editingOrder.AssignedMechanic = jobToAssign.AssignedMechanic;
                            editingOrder.AssignedMechanicUserId = jobToAssign.AssignedMechanicUserId;
                            ordersPage.assignedMechanic.Text = jobToAssign.AssignedMechanicDisplay;
                            ordersPage.assignMechanicDialogButton.Text = $"🔧 Assigned: {jobToAssign.AssignedMechanicDisplay}";
                            ordersPage.assignMechanicButtonLeft.Text = "🔧 Change";
                            UpdateStartEligibility();
                        }
                        tab.RefreshRecords();
                        ordersPage.resultLabel.Text = $"Assigned mechanic for {RecordReference.JobOrder(jobToAssign.JobOrderId)}: {jobToAssign.AssignedMechanicDisplay}";
                    }
                }
                else
                {
                    ordersPage.resultLabel.Text = "Select or save a job order first to assign a mechanic.";
                }
            }

            ordersPage.assignMechanicButton.Click += (s, e) => TriggerAssignMechanic();
            ordersPage.assignMechanicDialogButton.Click += (s, e) => TriggerAssignMechanic();
            ordersPage.assignMechanicButtonLeft.Click += (s, e) => TriggerAssignMechanic();

            tab.PreValidate = () =>
            {
                if (customerLookup.SelectedId == null || customerLookup.SelectedId <= 0)
                {
                    ordersPage.customer.Focus();
                    return "Customer is required. Please select an active customer.";
                }
                if (status.Text == "In Progress" && editingOrder.Status == "In Queue")
                {
                    bool hasMech = !string.IsNullOrWhiteSpace(editingOrder.AssignedMechanicUserId)
                        || (!string.IsNullOrWhiteSpace(editingOrder.AssignedMechanic) && editingOrder.AssignedMechanic != "Unassigned")
                        || (!string.IsNullOrWhiteSpace(ordersPage.assignedMechanic.Text) && ordersPage.assignedMechanic.Text != "Unassigned");
                    if (!hasMech)
                    {
                        var ask = MessageBox.Show(ordersPage.FindForm(),
                            "A mechanic must be assigned before work can start.\n\nWould you like to assign an active mechanic now?",
                            "Assign Mechanic Required",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);
                        if (ask == DialogResult.Yes)
                        {
                            TriggerAssignMechanic();
                            hasMech = !string.IsNullOrWhiteSpace(editingOrder.AssignedMechanicUserId)
                                || (!string.IsNullOrWhiteSpace(editingOrder.AssignedMechanic) && editingOrder.AssignedMechanic != "Unassigned");
                            if (!hasMech)
                            {
                                return "Work cannot start without an assigned mechanic.";
                            }
                        }
                        else
                        {
                            return "Work cannot start without an assigned mechanic.";
                        }
                    }
                }
                return null;
            };
            ordersPage.cancelJobButton.Click += async (s, e) =>
            {
                if (tab.SelectedRecord is JobOrder selected && selected.JobOrderId > 0)
                {
                    using var dialog = new CancelJobDialogForm(selected, factory);
                    if (dialog.ShowDialog(ordersPage.FindForm()) == DialogResult.OK)
                    {
                        await SyncAsync();
                        ordersPage.resultLabel.Text = $"{RecordReference.JobOrder(selected.JobOrderId)} cancelled successfully.";
                    }
                }
                else
                {
                    ordersPage.resultLabel.Text = "Select a job order to cancel.";
                }
            };

            ordersPage.archiveJobButton.Click += (s, e) =>
            {
                if (tab.SelectedRecord is JobOrder selected && selected.JobOrderId > 0)
                {
                    using var dialog = new ArchiveJobOrderDialogForm(selected, factory);
                    if (dialog.ShowDialog(ordersPage.FindForm()) == DialogResult.OK)
                    {
                        tab.RefreshRecords();
                        ordersPage.resultLabel.Text = $"{RecordReference.JobOrder(selected.JobOrderId)} archived successfully.";
                    }
                }
                else
                {
                    ordersPage.resultLabel.Text = "Select a job order to archive.";
                }
            };

            ordersPage.viewArchivedJobsButton.Click += (s, e) =>
            {
                using var dialog = new ArchivedRecordsDialogForm(factory, () => tab.RefreshRecords());
                dialog.ShowDialog(ordersPage.FindForm());
            };

            tab.OpenArchiveEditor = job =>
            {
                using var dialog = new ArchiveJobOrderDialogForm(job, factory);
                return dialog.ShowDialog(ordersPage.FindForm()) == DialogResult.OK;
            };

            status.SelectedIndexChanged += (s, e) =>
            {
                if (status.SelectedItem == null) return;
                string sel = status.SelectedItem.ToString();
                ordersPage.resultLabel.Text = sel switch
                {
                    "In Queue" => "Status: 'In Queue' (Intake stage. Set vehicle details, customer, and estimated cost).",
                    "In Progress" => "Status: 'In Progress' (Requires Total Estimated Cost & at least 50% downpayment).",
                    "Completed" => "Confirm the final inspection passed. The vehicle can be released after full payment.",
                    "Released" => "Status: 'Released' (Requires full payment of total estimated balance).",
                    _ => "Fill the fields and save, or advance to the next stage when ready."
                };
                UpdateStartEligibility();
            };

            ordersPage.Grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0) return;
                string prop = ordersPage.Grid.Columns[e.ColumnIndex].DataPropertyName;
                if (prop == "JobOrderId" && e.Value is int jid)
                {
                    e.Value = RecordReference.JobOrder(jid);
                    e.FormattingApplied = true;
                }
                else if (prop == "Status" && e.Value is string workflowStatus)
                {
                    e.Value = JobOrderWorkflow.Normalize(workflowStatus);
                    e.FormattingApplied = true;
                }
                if (prop == "CustomerId")
                {
                    e.Value = e.Value is int id && customerRecords.TryGetValue(id, out var c)
                        ? $"{c.FullName} ({c.ContactNumber})" : "Unlinked - review customer";
                    e.FormattingApplied = true;
                }
                else if (prop == "EstimateId" && e.Value is string estRef)
                {
                    // Clean display: eliminate duplicate customer and vehicle strings from grid cell
                    int slashIdx = estRef.IndexOf(" / ");
                    if (slashIdx > 0)
                    {
                        e.Value = estRef.Substring(0, slashIdx).Trim();
                        e.FormattingApplied = true;
                    }
                }
                else if (prop is "TotalEstimatedCost" or "TotalPaid" or "OutstandingBalance" or "CustomerCredit" && e.Value is decimal amt)
                {
                    e.Value = $"₱{amt:N2}";
                    e.FormattingApplied = true;
                }
            };

            tab.ConfigureEditor(row =>
            {
                updatingServicesTotal = true;
                editingOrder = row;
                bool historical = HistoricalCompatibility.IsProtected(row);
                ordersPage.estimatedCost.ReadOnly = historical;
                ordersPage.requestedServices.Enabled = !historical;
                ordersPage.requestedServices.Items.Clear();
                ordersPage.requestedServices.Items.AddRange(historical ? JobServices.Offered : JobServices.OfferedWithPrices);
                ordersPage.estimateLabel.Text = historical ? "Legacy reference (approval unverified)" : "Recorded quotation reference";
                ordersPage.inspectionPassed.Checked = false;
                unfinishedTasks = 0;
                ordersPage.requestedServices.Items.Clear();
                ordersPage.requestedServices.Items.AddRange(CRM.infrastructure.services.JobServices.OfferedWithPrices);
                ordersPage.requestedServicesLabel.Text = "Requested Services & Prices (determines job order cost):";
                if (row.JobOrderId != 0)
                {
                    using var lookupDb = factory();
                    editingOrder.Payments = lookupDb.Payments.AsNoTracking().Where(p => p.JobOrderId == row.JobOrderId).ToList();
                    unfinishedTasks = lookupDb.JobTasks.Count(t => t.JobOrderId == row.JobOrderId && t.Status != "Done");
                    foreach (var serviceName in lookupDb.JobTasks.Where(t => t.JobOrderId == row.JobOrderId).Select(t => t.ServiceId).Distinct().ToList())
                    {
                        string clean = CRM.infrastructure.services.JobServices.CleanServiceName(serviceName);
                        int index = -1;
                        for (int i = 0; i < ordersPage.requestedServices.Items.Count; i++)
                        {
                            if (CRM.infrastructure.services.JobServices.CleanServiceName(ordersPage.requestedServices.Items[i]?.ToString()).Equals(clean, StringComparison.OrdinalIgnoreCase))
                            {
                                index = i;
                                break;
                            }
                        }
                        if (index < 0) index = ordersPage.requestedServices.Items.Add(historical ? clean : CRM.infrastructure.services.JobServices.FormatServiceWithPrice(clean));
                        ordersPage.requestedServices.SetItemChecked(index, true);
                    }
                }
                customerLookup.Select(row.CustomerId);
                ordersPage.vehicleDetails.Text = row.VehicleDetails;
                ordersPage.assignedMechanic.Text = row.JobOrderId == 0 ? "Unassigned" : row.AssignedMechanicDisplay;
                ordersPage.assignMechanicDialogButton.Visible = row.JobOrderId > 0;
                ordersPage.assignMechanicDialogButton.Text = string.IsNullOrWhiteSpace(row.AssignedMechanic) || row.AssignedMechanic == "Unassigned"
                    ? "🔧 Assign Mechanic"
                    : $"🔧 Assigned: {row.AssignedMechanic}";
                ordersPage.assignMechanicButtonLeft.Visible = row.JobOrderId > 0;
                ordersPage.assignMechanicButtonLeft.Text = string.IsNullOrWhiteSpace(row.AssignedMechanic) || row.AssignedMechanic == "Unassigned"
                    ? "🔧 Assign"
                    : "🔧 Change";
                decimal serviceSum = 0m;
                foreach (var item in ordersPage.requestedServices.CheckedItems)
                {
                    serviceSum += CRM.infrastructure.services.JobServices.GetServicePrice(item?.ToString() ?? "");
                }
                if (row.TotalEstimatedCost.HasValue)
                {
                    ordersPage.estimatedCost.Text = row.TotalEstimatedCost.Value.ToString("F2");
                }
                else if (!historical && serviceSum > 0)
                {
                    ordersPage.estimatedCost.Text = serviceSum.ToString("N2");
                }
                else
                {
                    ordersPage.estimatedCost.Text = "";
                }
                if (historical)
                {
                    ordersPage.requestedServicesLabel.Text = "Recorded work; historical itemization unverified";
                }
                else if (serviceSum > 0)
                {
                    ordersPage.requestedServicesLabel.Text = $"Requested Services & Prices (Total: ₱{serviceSum:N2}):";
                }
                ordersPage.inquiryNotes.Text = row.InquiryNotes;
                DateTime validStartedAt = (row.JobOrderId == 0 || row.StartedAt < DateTimePicker.MinimumDateTime || row.StartedAt > DateTimePicker.MaximumDateTime)
                    ? (row.CreatedAt >= DateTimePicker.MinimumDateTime && row.CreatedAt <= DateTimePicker.MaximumDateTime ? row.CreatedAt : DateTime.Now)
                    : row.StartedAt;
                started.Value = validStartedAt;
                updatingServicesTotal = false;

                estimate.Text = HistoricalCompatibility.Reference(row);

                string currentStatus = string.IsNullOrWhiteSpace(row.Status) ? "In Queue" : JobOrderWorkflow.Normalize(row.Status);
                status.Items.Clear();
                if (row.JobOrderId == 0)
                {
                    status.Items.Add("In Queue");
                    status.SelectedIndex = 0;
                }
                else
                {
                    int stageIdx = Array.IndexOf(JobOrderStages, currentStatus);
                    if (stageIdx < 0)
                    {
                        status.Items.Add(currentStatus);
                        status.Items.Add("In Queue");
                        status.SelectedItem = currentStatus;
                    }
                    else
                    {
                        status.Items.Add(JobOrderStages[stageIdx]);
                        if (stageIdx + 1 < JobOrderStages.Length)
                        {
                            status.Items.Add(JobOrderStages[stageIdx + 1]);
                        }
                        status.SelectedItem = currentStatus;
                    }
                }
            }, (row, db) =>
            {
                if (HistoricalCompatibility.IsProtected(row)) { /* Retain the persisted authoritative amount, including null/zero. */ }
                else if (string.IsNullOrWhiteSpace(ordersPage.estimatedCost.Text)) row.TotalEstimatedCost = null;
                else if (decimal.TryParse(ordersPage.estimatedCost.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var cost) ||
                         decimal.TryParse(ordersPage.estimatedCost.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out cost))
                {
                    if (cost < 0 || decimal.Round(cost, 2) != cost)
                        throw new InvalidOperationException("Estimated cost must be nonnegative with at most two decimal places.");
                    row.TotalEstimatedCost = cost;
                }
                else throw new InvalidOperationException("Enter a valid estimated cost.");

                row.VehicleDetails = ordersPage.vehicleDetails.Text.Trim();
                row.InquiryNotes = ordersPage.inquiryNotes.Text.Trim();
                row.CustomerId = customerLookup.Read(db, true, row.CustomerId);
                // Estimate number is derived from the database identity; preserve legacy references.
                row.StartedAt = started.Value;

                string targetStatus = Required(status, "Status");
                int nextStage = Array.IndexOf(JobOrderStages, targetStatus);
                int prevStage = row.JobOrderId == 0 ? 0 : JobOrderWorkflow.Stage(row.Status);
                db.FinalInspectionConfirmed = ordersPage.inspectionPassed.Checked;

                if (nextStage < 0 || (row.JobOrderId == 0 && nextStage != 0) || (row.JobOrderId != 0 && nextStage != prevStage && nextStage != prevStage + 1))
                {
                    throw new InvalidOperationException("Move jobs one step at a time: Waiting, In Progress, Completed, Released.");
                }

                if (row.JobOrderId != 0 && nextStage > prevStage)
                {
                    if (row.TotalEstimatedCost == null)
                    {
                        throw new InvalidOperationException("Please enter a Total Estimated Cost before advancing the job status.");
                    }

                    decimal totalPaid = db.Payments.Where(p => p.JobOrderId == row.JobOrderId && !p.IsVoided).Sum(p => (decimal?)p.AmountPaid) ?? 0;

                    if (nextStage == 1) // In Progress
                    {
                        decimal minDeposit = decimal.Round(row.TotalEstimatedCost.Value * 0.5m, 2, MidpointRounding.AwayFromZero);
                        if (totalPaid < minDeposit)
                        {
                             throw new InvalidOperationException($"Cannot advance to 'In Progress'. A 50% downpayment of ₱{minDeposit:N2} is required before work starts. Current payments: ₱{totalPaid:N2}. Record a payment in the Payments tab first.");
                        }
                        // Assigned mechanic requirement removed for now per user request.
                    }
                    else if (nextStage == 2) // Completed
                    {
                        int pendingTasks = db.JobTasks.Count(t => t.JobOrderId == row.JobOrderId && t.Status != "Done");
                        if (pendingTasks > 0)
                        {
                            throw new InvalidOperationException($"Cannot mark as 'Completed'. There are {pendingTasks} unfinished task(s) for this job. Mark all tasks as 'Done' in the Tasks tab first.");
                        }
                    }
                    else if (nextStage == 3) // Released
                    {
                        if (totalPaid < row.TotalEstimatedCost.Value)
                        {
                            decimal balance = row.TotalEstimatedCost.Value - totalPaid;
                            throw new InvalidOperationException($"Cannot release vehicle. An outstanding balance of ₱{balance:N2} remains unpaid. Settle the balance in the Payments tab first.");
                        }
                    }
                }

                row.Status = targetStatus;
                if (!HistoricalCompatibility.IsProtected(row))
                    CRM.infrastructure.services.JobServices.Assign(db, row, ordersPage.requestedServices.CheckedItems.Cast<string>());
            });
            return tab;
        }

        private void ConfigureJobOrderList(CrmCollectionController<JobOrder> tab)
        {
            var search = ordersPage.search;
            var statusFilter = ordersPage.statusFilter;
            var clear = ordersPage.clear;
            var count = ordersPage.count;
            tab.ConfigureListTools( row =>
            {
                string query = search.Text.Trim();
                string custName = row.CustomerId is int cid && customerRecords.TryGetValue(cid, out var c) ? c.FullName : "";
                string custPhone = row.CustomerId is int cid2 && customerRecords.TryGetValue(cid2, out var c2) ? c2.ContactNumber : "";
                return !row.IsArchived && (statusFilter.SelectedIndex <= 0 || JobOrderWorkflow.Normalize(row.Status) == JobWorkflowLabels.FilterStatus(statusFilter.SelectedIndex))
                    && (query.Length == 0 || RecordReference.Matches(RecordReference.JobOrderPrefix, row.JobOrderId, query)
                        || (row.EstimateId ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)
                        || custName.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || custPhone.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (row.AssignedMechanic ?? "").Contains(query, StringComparison.OrdinalIgnoreCase));
            }, (visible, total) => count.Text = $"Showing {visible} of {total} job orders. Select a row to view or edit.");
            search.TextChanged += (s, e) => tab.ApplyListFilter();
            ordersPage.searchButton.Click += (s, e) => tab.ApplyListFilter();
            statusFilter.SelectedIndexChanged += (s, e) => tab.ApplyListFilter();
            clear.Click += (s, e) => { search.Clear(); statusFilter.SelectedIndex = 0; tab.ApplyListFilter(); };
        }

        private CrmCollectionController<JobTask> BuildJobTasks(Func<MasterCrmDbContext> factory, Func<string, bool> confirm)
        {
            var tab = new CrmCollectionController<JobTask>(tasksPage, "Task", x => x.JobTaskId, factory, confirm);
            UseEntryDialog(tab, tasksPage.editorPanel, tasksPage.resultLabel, tasksPage.saveButton, "Job Task");
            var filterLookup = BindJobLookup(tasksPage.jobFilter, true, tasksPage, factory);
            bool allTasks = false, rebuilding = false;
            var taskRows = new System.Collections.Generic.List<JobTask>();
            bool MatchesJob(JobOrder j, string query) => JobSearch.Matches(j,
                customerRecords.TryGetValue(j.CustomerId ?? 0, out var customer) ? customer : null, query);
            string currentSortColumn = "JobOrderId";
            bool currentSortAscending = false;
            bool syncingSort = false;

            int StatusWorkflowRank(string st) => st switch
            {
                "In Queue" => 1,
                "In Progress" => 2,
                "QA Checking" => 3,
                "Completed" => 4,
                "Released" => 5,
                _ => 99
            };

            void ApplySortGlyphs()
            {
                foreach (DataGridViewColumn col in tasksPage.jobsGrid.Columns)
                {
                    col.SortMode = DataGridViewColumnSortMode.Programmatic;
                    if (col.Name == currentSortColumn)
                    {
                        col.HeaderCell.SortGlyphDirection = currentSortAscending ? SortOrder.Ascending : SortOrder.Descending;
                    }
                    else
                    {
                        col.HeaderCell.SortGlyphDirection = SortOrder.None;
                    }
                }
            }

            void UpdateSortComboSelection()
            {
                if (tasksPage.sortCombo == null) return;
                syncingSort = true;
                try
                {
                    if (currentSortColumn == "JobOrderId")
                        tasksPage.sortCombo.SelectedIndex = currentSortAscending ? 1 : 0;
                    else if (currentSortColumn == "Customer")
                        tasksPage.sortCombo.SelectedIndex = currentSortAscending ? 2 : 3;
                    else if (currentSortColumn == "Vehicle")
                        tasksPage.sortCombo.SelectedIndex = currentSortAscending ? 4 : 5;
                    else if (currentSortColumn == "Progress")
                        tasksPage.sortCombo.SelectedIndex = currentSortAscending ? 7 : 6;
                    else if (currentSortColumn == "Status")
                        tasksPage.sortCombo.SelectedIndex = 8;
                }
                finally { syncingSort = false; }
            }

            int currentPage = 1;
            int pageSize = 20;
            CrmGridStandardizer.ApplyColumnLayout(tasksPage.jobsGrid, "TaskJobs");
            GridRowNumbering.Enable(tasksPage.jobsGrid, () => (currentPage - 1) * pageSize);
            CrmGridStandardizer.RestoreColumnWidths(tasksPage.jobsGrid, "TaskJobs");

            void RefreshJobOverview()
            {
                if (rebuilding) return;
                int firstVisible = tasksPage.jobsGrid.FirstDisplayedScrollingRowIndex;
                rebuilding = true;
                try
                {
                    int? selected = filterLookup.SelectedJobId;
                    string query = tasksPage.taskSearch.Text.Trim();
                    int statusIdx = tasksPage.statusFilter != null ? tasksPage.statusFilter.SelectedIndex : 0;
                    string filterStatus = statusIdx switch
                    {
                        1 => "In Queue",
                        2 => "In Progress",
                        3 => "Completed",
                        4 => "Released",
                        _ => ""
                    };

                    var rawList = jobRecords.Where(j => {
                        if (j.IsArchived) return false;
                        if (!string.IsNullOrEmpty(filterStatus) && JobOrderWorkflow.Normalize(j.Status) != filterStatus)
                            return false;
                        return MatchesJob(j, query) ||
                            (j.EstimateNumber ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            taskRows.Any(t => t.JobOrderId == j.JobOrderId && $"{t.ServiceId} {t.Remarks} {t.Status}".Contains(query, StringComparison.OrdinalIgnoreCase));
                    })
                    .Select(j => {
                        int total = taskRows.Count(t => t.JobOrderId == j.JobOrderId);
                        int done = taskRows.Count(t => t.JobOrderId == j.JobOrderId && t.Status == "Done");
                        string cust = customerRecords.TryGetValue(j.CustomerId ?? 0, out var c) ? $"{c.FullName} ({c.ContactNumber})" : "Unlinked";
                        return new JobOverviewItem
                        {
                            JobOrderId = j.JobOrderId,
                            Customer = cust,
                            EstimateId = HistoricalCompatibility.Reference(j),
                            Status = JobOrderWorkflow.Normalize(j.Status),
                            StartedAt = j.StartedAt < DateTimePicker.MinimumDateTime ? "—" : j.StartedAt.ToString("dd MMM yyyy, hh:mm tt"),
                            Vehicle = j.VehicleDisplay,
                            Progress = $"{done} / {total} done",
                            DoneTasks = done,
                            TotalTasks = total,
                            ProgressRatio = total == 0 ? 0 : (double)done / total,
                            IsCompletedOrReleased = j.Status == "Completed" || j.Status == "Released"
                        };
                    }).ToList();

                    var sorted = currentSortColumn switch
                    {
                        "JobOrderId" => currentSortAscending
                            ? rawList.OrderBy(x => x.JobOrderId)
                            : rawList.OrderBy(x => x.IsCompletedOrReleased).ThenByDescending(x => x.JobOrderId),
                        "Customer" => currentSortAscending
                            ? rawList.OrderBy(x => x.Customer, StringComparer.OrdinalIgnoreCase).ThenByDescending(x => x.JobOrderId)
                            : rawList.OrderByDescending(x => x.Customer, StringComparer.OrdinalIgnoreCase).ThenByDescending(x => x.JobOrderId),
                        "EstimateId" => currentSortAscending
                            ? rawList.OrderBy(x => x.EstimateId, StringComparer.OrdinalIgnoreCase).ThenByDescending(x => x.JobOrderId)
                            : rawList.OrderByDescending(x => x.EstimateId, StringComparer.OrdinalIgnoreCase).ThenByDescending(x => x.JobOrderId),
                        "Vehicle" => currentSortAscending
                            ? rawList.OrderBy(x => x.Vehicle, StringComparer.OrdinalIgnoreCase).ThenByDescending(x => x.JobOrderId)
                            : rawList.OrderByDescending(x => x.Vehicle, StringComparer.OrdinalIgnoreCase).ThenByDescending(x => x.JobOrderId),
                        "Progress" => currentSortAscending
                            ? rawList.OrderBy(x => x.ProgressRatio).ThenBy(x => x.DoneTasks).ThenByDescending(x => x.JobOrderId)
                            : rawList.OrderByDescending(x => x.ProgressRatio).ThenByDescending(x => x.DoneTasks).ThenByDescending(x => x.JobOrderId),
                        "Status" => currentSortAscending
                            ? rawList.OrderBy(x => StatusWorkflowRank(x.Status)).ThenByDescending(x => x.JobOrderId)
                            : rawList.OrderByDescending(x => StatusWorkflowRank(x.Status)).ThenByDescending(x => x.JobOrderId),
                        _ => currentSortAscending
                            ? rawList.OrderBy(x => x.JobOrderId)
                            : rawList.OrderBy(x => x.IsCompletedOrReleased).ThenByDescending(x => x.JobOrderId)
                    };

                    var sortedList = sorted.ToList();
                    int totalPages = Math.Max(1, (int)Math.Ceiling((double)sortedList.Count / pageSize));
                    if (currentPage > totalPages) currentPage = totalPages;
                    if (currentPage < 1) currentPage = 1;

                    var pagedList = sortedList.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
                    tasksPage.jobsGrid.DataSource = pagedList;
                    CrmGridStandardizer.RestoreColumnWidths(tasksPage.jobsGrid, "TaskJobs");

                    if (tasksPage.jobsGrid.Columns["JobOrderId"] != null)
                    {
                        tasksPage.jobsGrid.Columns["JobOrderId"].HeaderText = "Job Order #";
                        tasksPage.jobsGrid.Columns["JobOrderId"].FillWeight = 40;
                        tasksPage.jobsGrid.Columns["JobOrderId"].MinimumWidth = 60;
                        tasksPage.jobsGrid.Columns["JobOrderId"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                    if (tasksPage.jobsGrid.Columns["Customer"] != null)
                    {
                        tasksPage.jobsGrid.Columns["Customer"].HeaderText = "Customer";
                        tasksPage.jobsGrid.Columns["Customer"].FillWeight = 140;
                        tasksPage.jobsGrid.Columns["Customer"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }
                    if (tasksPage.jobsGrid.Columns["EstimateId"] != null)
                    {
                        tasksPage.jobsGrid.Columns["EstimateId"].HeaderText = "Recorded Reference";
                        tasksPage.jobsGrid.Columns["EstimateId"].FillWeight = 90;
                        tasksPage.jobsGrid.Columns["EstimateId"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }
                    if (tasksPage.jobsGrid.Columns["Status"] != null)
                    {
                        tasksPage.jobsGrid.Columns["Status"].HeaderText = "Status";
                        tasksPage.jobsGrid.Columns["Status"].FillWeight = 75;
                        tasksPage.jobsGrid.Columns["Status"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                    if (tasksPage.jobsGrid.Columns["StartedAt"] != null)
                    {
                        tasksPage.jobsGrid.Columns["StartedAt"].HeaderText = "Started At";
                        tasksPage.jobsGrid.Columns["StartedAt"].FillWeight = 110;
                        tasksPage.jobsGrid.Columns["StartedAt"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                    if (tasksPage.jobsGrid.Columns["Vehicle"] != null)
                    {
                        tasksPage.jobsGrid.Columns["Vehicle"].HeaderText = "Vehicle";
                        tasksPage.jobsGrid.Columns["Vehicle"].FillWeight = 130;
                        tasksPage.jobsGrid.Columns["Vehicle"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                    }
                    if (tasksPage.jobsGrid.Columns["Progress"] != null)
                    {
                        tasksPage.jobsGrid.Columns["Progress"].HeaderText = "Progress";
                        tasksPage.jobsGrid.Columns["Progress"].FillWeight = 75;
                        tasksPage.jobsGrid.Columns["Progress"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                    }
                    ApplySortGlyphs();

                    tasksPage.taskCount.Text = $"Showing {sortedList.Count} of {jobRecords.Count} job orders. Select a row to view or edit.";
                    tasksPage.lblPageInfo.Text = $"Page {currentPage} of {totalPages} ({sortedList.Count} records)";
                    tasksPage.btnFirst.Enabled = currentPage > 1;
                    tasksPage.btnPrev.Enabled = currentPage > 1;
                    tasksPage.btnNext.Enabled = currentPage < totalPages;
                    tasksPage.btnLast.Enabled = currentPage < totalPages;

                    tasksPage.jobsGrid.ClearSelection();
                    tasksPage.jobsGrid.CurrentCell = null;
                    foreach (DataGridViewRow row in tasksPage.jobsGrid.Rows)
                        if ((int)row.Cells["JobOrderId"].Value == selected)
                        {
                            row.Selected = true;
                            tasksPage.jobsGrid.CurrentCell = row.Cells["JobOrderId"];
                        }
                    if (firstVisible >= 0 && tasksPage.jobsGrid.Rows.Count > 0)
                        tasksPage.jobsGrid.FirstDisplayedScrollingRowIndex = Math.Min(firstVisible, tasksPage.jobsGrid.Rows.Count - 1);
                }
                finally { rebuilding = false; }
            }
            refreshTaskJobs = RefreshJobOverview;
            tasksPage.refreshButton.Click += (s, e) => ordersPage.RefreshRecords();
            tasksPage.btnFirst.Click += (s, e) => { currentPage = 1; RefreshJobOverview(); };
            tasksPage.btnPrev.Click += (s, e) => { if (currentPage > 1) { currentPage--; RefreshJobOverview(); } };
            tasksPage.btnNext.Click += (s, e) => { currentPage++; RefreshJobOverview(); };
            tasksPage.btnLast.Click += (s, e) => { currentPage = 999999; RefreshJobOverview(); };
            tasksPage.cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                if (tasksPage.cmbPageSize.SelectedItem is int sz && sz > 0)
                {
                    pageSize = sz;
                    currentPage = 1;
                    RefreshJobOverview();
                }
            };
            tasksPage.searchTasksButton.Click += (s, e) => { currentPage = 1; RefreshJobOverview(); };
            tasksPage.taskSearch.TextChanged += (s, e) => { currentPage = 1; RefreshJobOverview(); };
            if (tasksPage.statusFilter != null)
                tasksPage.statusFilter.SelectedIndexChanged += (s, e) => { currentPage = 1; RefreshJobOverview(); };
            tasksPage.clearFilter.Click += (s, e) =>
            {
                tasksPage.taskSearch.Clear();
                if (tasksPage.statusFilter != null) tasksPage.statusFilter.SelectedIndex = 0;
                currentPage = 1;
                RefreshJobOverview();
            };
            tab.RecordsLoaded += rows =>
            {
                // Filtering tasks reuses these same records; it must not rebind the job grid.
                if (taskRows.SequenceEqual(rows)) return;
                taskRows = rows.ToList();
                RefreshJobOverview();
            };
            tasksPage.selectedJobLabel.Text = "Select a job to view its service tasks.";
            tasksPage.editRecordButton.Enabled = false;
            tasksPage.printMechanicCopyButton.Enabled = false;
            if (tasksPage.btnViewTasksPopUp != null) tasksPage.btnViewTasksPopUp.Enabled = false;

            if (tasksPage.sortCombo != null)
            {
                tasksPage.sortCombo.SelectedIndexChanged += (s, e) =>
                {
                    if (syncingSort) return;
                    switch (tasksPage.sortCombo.SelectedIndex)
                    {
                        case 0: // Job # (Newest first)
                            currentSortColumn = "JobOrderId";
                            currentSortAscending = false;
                            break;
                        case 1: // Job # (Oldest first)
                            currentSortColumn = "JobOrderId";
                            currentSortAscending = true;
                            break;
                        case 2: // Customer (A → Z)
                            currentSortColumn = "Customer";
                            currentSortAscending = true;
                            break;
                        case 3: // Customer (Z → A)
                            currentSortColumn = "Customer";
                            currentSortAscending = false;
                            break;
                        case 4: // Vehicle (A → Z)
                            currentSortColumn = "Vehicle";
                            currentSortAscending = true;
                            break;
                        case 5: // Vehicle (Z → A)
                            currentSortColumn = "Vehicle";
                            currentSortAscending = false;
                            break;
                        case 6: // Progress (Highest first)
                            currentSortColumn = "Progress";
                            currentSortAscending = false;
                            break;
                        case 7: // Progress (Lowest first)
                            currentSortColumn = "Progress";
                            currentSortAscending = true;
                            break;
                        case 8: // Status (Workflow order)
                            currentSortColumn = "Status";
                            currentSortAscending = true;
                            break;
                    }
                    RefreshJobOverview();
                };
            }

            tasksPage.jobsGrid.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= tasksPage.jobsGrid.Columns.Count) return;
                var clickedCol = tasksPage.jobsGrid.Columns[e.ColumnIndex];
                string colName = clickedCol.Name;

                if (currentSortColumn == colName)
                {
                    currentSortAscending = !currentSortAscending;
                }
                else
                {
                    currentSortColumn = colName;
                    currentSortAscending = (colName != "JobOrderId");
                }

                UpdateSortComboSelection();
                RefreshJobOverview();
            };

            tasksPage.jobsGrid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                var col = tasksPage.jobsGrid.Columns[e.ColumnIndex];
                if (col.Name == "Status" && e.Value is string statusVal)
                {
                    e.FormattingApplied = true;
                    switch (statusVal)
                    {
                        case "In Queue":
                            e.CellStyle.ForeColor = Color.FromArgb(180, 83, 9);
                            break;
                        case "In Progress":
                            e.CellStyle.ForeColor = Color.FromArgb(29, 78, 216);
                            e.CellStyle.Font = new Font(tasksPage.jobsGrid.Font, FontStyle.Bold);
                            break;
                        case "QA Checking":
                            e.CellStyle.ForeColor = Color.FromArgb(126, 34, 206);
                            e.CellStyle.Font = new Font(tasksPage.jobsGrid.Font, FontStyle.Bold);
                            break;
                        case "Completed":
                            e.CellStyle.ForeColor = Color.FromArgb(21, 128, 61);
                            e.CellStyle.Font = new Font(tasksPage.jobsGrid.Font, FontStyle.Bold);
                            break;
                        case "Released":
                            e.CellStyle.ForeColor = Color.FromArgb(71, 85, 105);
                            break;
                    }
                }
                else if (col.Name == "Progress" && e.Value is string progVal)
                {
                    if (tasksPage.jobsGrid.Rows[e.RowIndex].DataBoundItem is JobOverviewItem item)
                    {
                        if (item.TotalTasks > 0 && item.DoneTasks == item.TotalTasks)
                        {
                            e.Value = $"✓ {progVal}";
                            e.CellStyle.ForeColor = Color.FromArgb(21, 128, 61);
                            e.CellStyle.Font = new Font(tasksPage.jobsGrid.Font, FontStyle.Bold);
                            e.FormattingApplied = true;
                        }
                        else if (item.DoneTasks > 0)
                        {
                            e.CellStyle.ForeColor = Color.FromArgb(29, 78, 216);
                            e.FormattingApplied = true;
                        }
                        else
                        {
                            e.CellStyle.ForeColor = Color.FromArgb(100, 116, 139);
                            e.FormattingApplied = true;
                        }
                    }
                }
                else if (col.Name == "JobOrderId" && e.Value is int jid)
                {
                    e.Value = RecordReference.JobOrder(jid);
                    e.CellStyle.Font = new Font(tasksPage.jobsGrid.Font, FontStyle.Bold);
                    e.FormattingApplied = true;
                }
            };

            void OpenTasksPopUp(int jid)
            {
                using var dlg = new JobTasksDialogForm(jid, factory, customerRecords.Values);
                dlg.ShowDialog(this);
                ordersPage.RefreshRecords();
                tasksPage.RefreshRecords();
            }

            void UpdateTaskButtonsState()
            {
                bool hasTask = tab.SelectedRecord != null;
                bool hasJob = filterLookup.SelectedJobId.HasValue;
                tasksPage.editRecordButton.Enabled = hasTask;
                tasksPage.printMechanicCopyButton.Enabled = hasJob;
                if (tasksPage.btnMarkDone != null) tasksPage.btnMarkDone.Enabled = hasTask;
                if (tasksPage.btnStartTask != null) tasksPage.btnStartTask.Enabled = hasTask;
                if (tasksPage.btnMarkAllDone != null) tasksPage.btnMarkAllDone.Enabled = hasJob;
                if (tasksPage.btnViewTasksPopUp != null)
                {
                    tasksPage.btnViewTasksPopUp.Enabled = hasJob;
                    if (hasJob)
                    {
                        tasksPage.btnViewTasksPopUp.BackColor = Color.FromArgb(13, 110, 253);
                        tasksPage.btnViewTasksPopUp.ForeColor = Color.White;
                        tasksPage.btnViewTasksPopUp.FlatStyle = FlatStyle.Flat;
                    }
                    else
                    {
                        tasksPage.btnViewTasksPopUp.BackColor = SystemColors.Control;
                        tasksPage.btnViewTasksPopUp.ForeColor = SystemColors.GrayText;
                        tasksPage.btnViewTasksPopUp.FlatStyle = FlatStyle.System;
                    }
                }
            }

            async System.Threading.Tasks.Task UpdateTaskProgressAsync(int? specificTaskId, string targetStatus)
            {
                int? jid = filterLookup.SelectedJobId;
                if (!jid.HasValue && tab.SelectedRecord != null) jid = tab.SelectedRecord.JobOrderId;
                if (!jid.HasValue)
                {
                    tasksPage.resultLabel.Text = "Select a job order first.";
                    return;
                }

                try
                {
                    using var db = factory();
                    var job = await db.JobOrders.FirstOrDefaultAsync(j => j.JobOrderId == jid.Value);
                    if (job == null) return;

                    if (job.Status is "Completed" or "Released" or "Cancelled")
                    {
                        MessageBox.Show(tasksPage.FindForm(), $"Job is {job.Status}. Tasks cannot be modified.", "Job Tasks", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (job.Status == "In Queue" && targetStatus is "Ongoing" or "Done")
                    {
                        decimal totalCost = job.TotalEstimatedCost ?? 0m;
                        decimal requiredDeposit = decimal.Round(totalCost * 0.5m, 2, MidpointRounding.AwayFromZero);
                        decimal paid = db.Payments.Where(p => p.JobOrderId == job.JobOrderId && !p.IsVoided).Sum(p => (decimal?)p.AmountPaid) ?? 0m;

                        if (totalCost <= 0)
                        {
                            MessageBox.Show(tasksPage.FindForm(), "Please set the total estimated cost in Job Orders first.", "Cannot Start Work", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        if (paid < requiredDeposit)
                        {
                            MessageBox.Show(tasksPage.FindForm(), $"Work cannot start yet. A 50% deposit of ₱{requiredDeposit:N2} is required (Paid: ₱{paid:N2}). Record payment first.", "Deposit Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        bool hasMechanic = !string.IsNullOrWhiteSpace(job.AssignedMechanicUserId)
                            || (!string.IsNullOrWhiteSpace(job.AssignedMechanic) && job.AssignedMechanic != "Unassigned");

                        if (!hasMechanic)
                        {
                            var ask = MessageBox.Show(tasksPage.FindForm(),
                                "A mechanic must be assigned before work can start.\n\nWould you like to assign an active mechanic now?",
                                "Assign Mechanic Required",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (ask == DialogResult.Yes)
                            {
                                var cust = job.CustomerId.HasValue && customerRecords.TryGetValue(job.CustomerId.Value, out var c) ? c : null;
                                using var assignDialog = new AssignMechanicDialogForm(job, cust, factory);
                                if (assignDialog.ShowDialog(tasksPage.FindForm()) == DialogResult.OK)
                                {
                                    var freshJob = db.JobOrders.Find(job.JobOrderId);
                                    if (freshJob != null)
                                    {
                                        job.AssignedMechanic = freshJob.AssignedMechanic;
                                        job.AssignedMechanicUserId = freshJob.AssignedMechanicUserId;
                                        hasMechanic = true;
                                    }
                                }
                                else return;
                            }
                            else return;
                        }

                        if (hasMechanic)
                        {
                            job.Status = "In Progress";
                            if (job.StartedAt < DateTimePicker.MinimumDateTime || job.StartedAt > DateTimePicker.MaximumDateTime)
                                job.StartedAt = DateTime.Now;
                        }
                    }

                    if (specificTaskId.HasValue)
                    {
                        var t = await db.JobTasks.FirstOrDefaultAsync(x => x.JobTaskId == specificTaskId.Value);
                        if (t != null)
                        {
                            if (t.IsArchived || t.Status is "Withdrawn" or "Cancelled")
                            {
                                MessageBox.Show(tasksPage.FindForm(), $"Task #{t.JobTaskId} is {t.Status} and cannot be modified.", "Task Status", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            if (t.Status is "Done" or "Completed" && targetStatus != t.Status)
                            {
                                MessageBox.Show(tasksPage.FindForm(), $"Completed task #{t.JobTaskId} cannot be moved back to {targetStatus}.", "Reversing Completed Task Prohibited", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            if (targetStatus == "Done" && t.Status != "Ongoing")
                            {
                                MessageBox.Show(tasksPage.FindForm(), $"Task #{t.JobTaskId} is currently {t.Status}. A task must be started (marked Ongoing) before it can be marked as Done.", "Task Execution State", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            t.Status = targetStatus;
                        }
                    }
                    else
                    {
                        if (targetStatus == "Done")
                        {
                            var allJobTasks = await db.JobTasks.Where(x => x.JobOrderId == jid.Value && !x.IsArchived && x.Status != "Withdrawn" && x.Status != "Cancelled").ToListAsync();
                            int pendingCount = allJobTasks.Count(x => x.Status == "Pending");
                            if (pendingCount > 0)
                            {
                                MessageBox.Show(tasksPage.FindForm(),
                                    $"Cannot mark all tasks Done: {pendingCount} task(s) are still Pending (not started).\n\nWorkshop integrity requires that tasks must be started (marked Ongoing) before being completed.",
                                    "Pending Tasks Exist", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            var ongoingTasks = allJobTasks.Where(x => x.Status == "Ongoing").ToList();
                            if (ongoingTasks.Count == 0)
                            {
                                MessageBox.Show(tasksPage.FindForm(), "All active tasks for this job are already completed.", "Job Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            var confirm = MessageBox.Show(tasksPage.FindForm(),
                                $"Mark all {ongoingTasks.Count} ongoing task(s) as Done?\n\nConfirm that workshop repair/inspection for each service is completed.",
                                "Confirm Bulk Task Completion", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (confirm != DialogResult.Yes) return;

                            foreach (var t in ongoingTasks) t.Status = "Done";
                        }
                    }

                    await db.SaveChangesAsync();
                    ordersPage.RefreshRecords();
                    tasksPage.RefreshRecords();
                    tasksPage.resultLabel.Text = specificTaskId.HasValue
                        ? $"Task #{specificTaskId.Value} updated to {targetStatus}."
                        : $"All ongoing tasks for {RecordReference.JobOrder(jid.Value)} marked as Done.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(tasksPage.FindForm(), "Failed to update task progress: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (tasksPage.btnMarkDone != null)
            {
                tasksPage.btnMarkDone.Click += async (s, e) =>
                {
                    if (tab.SelectedRecord is JobTask task)
                        await UpdateTaskProgressAsync(task.JobTaskId, "Done");
                    else if (tasksPage.Grid.SelectedRows.Count > 0 && tasksPage.Grid.SelectedRows[0].DataBoundItem is JobTask selTask)
                        await UpdateTaskProgressAsync(selTask.JobTaskId, "Done");
                };
            }

            if (tasksPage.btnStartTask != null)
            {
                tasksPage.btnStartTask.Click += async (s, e) =>
                {
                    if (tab.SelectedRecord is JobTask task)
                        await UpdateTaskProgressAsync(task.JobTaskId, "Ongoing");
                    else if (tasksPage.Grid.SelectedRows.Count > 0 && tasksPage.Grid.SelectedRows[0].DataBoundItem is JobTask selTask)
                        await UpdateTaskProgressAsync(selTask.JobTaskId, "Ongoing");
                };
            }

            if (tasksPage.btnMarkAllDone != null)
            {
                tasksPage.btnMarkAllDone.Click += async (s, e) =>
                {
                    await UpdateTaskProgressAsync(null, "Done");
                };
            }

            tasksPage.Grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < tasksPage.Grid.Rows.Count)
                {
                    tasksPage.Grid.Rows[e.RowIndex].Selected = true;
                    UpdateTaskButtonsState();
                }
            };

            tab.OpenArchiveEditor = task =>
            {
                var ask = MessageBox.Show(tasksPage.FindForm(),
                    $"Withdraw and archive {RecordReference.JobTask(task.JobTaskId)} ({task.ServiceId})?\n\nThis will mark the task as Withdrawn and preserve history.",
                    "Archive Task", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (ask == DialogResult.Yes)
                {
                    var result = JobOrderArchivingService.ArchiveJobTaskAsync(factory, task.JobTaskId, "Withdrawn from service scope", "System Administrator").GetAwaiter().GetResult();
                    if (!result.IsPermitted)
                    {
                        MessageBox.Show(tasksPage.FindForm(), result.Message, "Archive Task Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    return true;
                }
                return false;
            };

            if (tasksPage.btnViewTasksPopUp != null)
            {
                tasksPage.btnViewTasksPopUp.Click += (s, e) =>
                {
                    if (filterLookup.SelectedJobId is int jid) OpenTasksPopUp(jid);
                    else tasksPage.resultLabel.Text = "Select a job order to view its tasks in pop-up.";
                };
            }

            tasksPage.jobsGrid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < tasksPage.jobsGrid.Rows.Count &&
                    tasksPage.jobsGrid.Rows[e.RowIndex].Cells["JobOrderId"].Value is int jid)
                {
                    OpenTasksPopUp(jid);
                }
            };

            tasksPage.Grid.SelectionChanged += (s, e) => UpdateTaskButtonsState();
            tasksPage.Grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0) return;
                string prop = tasksPage.Grid.Columns[e.ColumnIndex].DataPropertyName;
                if (prop == "JobTaskId" && e.Value is int tid)
                {
                    e.Value = RecordReference.JobTask(tid);
                    e.FormattingApplied = true;
                }
                else if (prop == "JobOrderId" && e.Value is int jid)
                {
                    e.Value = RecordReference.JobOrder(jid);
                    e.FormattingApplied = true;
                }
                else if (prop == "ServiceId" && e.Value is string sName)
                {
                    e.Value = CRM.infrastructure.services.JobServices.FormatServiceWithPrice(sName);
                    e.FormattingApplied = true;
                }
            };
            tasksPage.jobsGrid.SelectionChanged += (s, e) =>
            {
                if (!rebuilding && tasksPage.jobsGrid.SelectedRows.Count > 0)
                    filterLookup.Select((int)tasksPage.jobsGrid.SelectedRows[0].Cells["JobOrderId"].Value);
                UpdateTaskButtonsState();
            };
            tab.ConfigureListTools(t => !t.IsArchived && (filterLookup.SelectedJobId.HasValue ? t.JobOrderId == filterLookup.SelectedJobId.Value : allTasks)
                && (string.IsNullOrWhiteSpace(tasksPage.taskSearch.Text)
                    || RecordReference.Matches(RecordReference.JobTaskPrefix, t.JobTaskId, tasksPage.taskSearch.Text.Trim())
                    || jobRecords.Any(j => j.JobOrderId == t.JobOrderId && MatchesJob(j, tasksPage.taskSearch.Text.Trim()))
                    || ($"{t.ServiceId} {t.Remarks} {t.Status}").Contains(tasksPage.taskSearch.Text.Trim(), StringComparison.OrdinalIgnoreCase)),
                (visible, total) => tasksPage.taskCount.Text = $"{tasksPage.jobsGrid.Rows.Count} workshop jobs. Select a job, then click View Tasks / Update.");
            filterLookup.Changed += tab.ApplyListFilter;
            filterLookup.Changed += () =>
            {
                tasksPage.Grid.Columns["JobOrderId"].Visible = !filterLookup.SelectedJobId.HasValue;
                int? jid = filterLookup.SelectedJobId;
                int taskCountForJob = jid.HasValue ? taskRows.Count(t => t.JobOrderId == jid.Value) : 0;
                if (jid is int id)
                {
                    string desc = filterLookup.Describe(id);
                    tasksPage.selectedJobLabel.Text = taskCountForJob == 0
                        ? $"🚗 {desc} — This job has zero service tasks. Requested services must be added through Job Orders."
                        : $"🚗 {desc}";
                    tasksPage.selectedJobLabel.ForeColor = Color.FromArgb(15, 23, 42);
                    tasksPage.selectedJobLabel.BackColor = Color.FromArgb(240, 249, 255);
                }
                else
                {
                    tasksPage.selectedJobLabel.Text = allTasks ? "📋 All workshop tasks" : "ℹ️ Select a job order above to view its service tasks.";
                    tasksPage.selectedJobLabel.ForeColor = Color.FromArgb(100, 116, 139);
                    tasksPage.selectedJobLabel.BackColor = Color.FromArgb(241, 245, 249);
                }

                tasksPage.Grid.ClearSelection();
                tasksPage.Grid.CurrentCell = null;
                UpdateTaskButtonsState();

                if (rebuilding) return;
                int? selected = filterLookup.SelectedJobId;
                if (tasksPage.jobsGrid.SelectedRows.Count == 1 &&
                    (int)tasksPage.jobsGrid.SelectedRows[0].Cells["JobOrderId"].Value == selected) return;
                rebuilding = true;
                try
                {
                    tasksPage.jobsGrid.ClearSelection();
                    tasksPage.jobsGrid.CurrentCell = null;
                    foreach (DataGridViewRow row in tasksPage.jobsGrid.Rows)
                        if ((int)row.Cells["JobOrderId"].Value == selected)
                        {
                            row.Selected = true;
                            if (tasksPage.jobsGrid.Columns.Count > 0) tasksPage.jobsGrid.CurrentCell = row.Cells[0];
                            if (!row.Displayed) tasksPage.jobsGrid.FirstDisplayedScrollingRowIndex = row.Index;
                            break;
                        }
                }
                finally { rebuilding = false; }
            };
            tasksPage.printMechanicCopyButton.Click += (s, e) =>
            {
                if (filterLookup.SelectedJobId is int jid && jobRecords.FirstOrDefault(j => j.JobOrderId == jid) is JobOrder job)
                {
                    OpenMechanicCopy(job, factory);
                }
                else
                {
                    tasksPage.resultLabel.Text = "Select a job order to preview its mechanic's copy.";
                }
            };
            void RevealJob(int id)
            {
                tasksPage.taskSearch.Clear();
                filterLookup.Select(id);
                // Explicit navigation must reveal the row even when it was already selected.
                foreach (DataGridViewRow row in tasksPage.jobsGrid.Rows)
                    if ((int)row.Cells["JobOrderId"].Value == id)
                    {
                        rebuilding = true;
                        try
                        {
                            tasksPage.jobsGrid.ClearSelection();
                            tasksPage.jobsGrid.CurrentCell = row.Cells["JobOrderId"];
                            row.Selected = true;
                            tasksPage.jobsGrid.FirstDisplayedScrollingRowIndex = row.Index;
                        }
                        finally { rebuilding = false; }
                        break;
                    }
            }
            tasksPage.findJob.Click += (s, e) =>
            {
                if (filterLookup.OpenPickerDialog(this) && filterLookup.SelectedJobId is int id) RevealJob(id);
            };
            tasksPage.newestJob.Click += (s, e) =>
            {
                ordersPage.RefreshRecords();
                tasksPage.RefreshRecords();
                if (jobRecords.Count > 0) RevealJob(jobRecords.Max(j => j.JobOrderId));
            };
            void ExecuteTaskSearch()
            {
                allTasks = true;
                filterLookup.Select(null);
                tab.ApplyListFilter();
                RefreshJobOverview();
                if (tasksPage.jobsGrid.Rows.Count > 0) tasksPage.jobsGrid.FirstDisplayedScrollingRowIndex = 0;
                tasksPage.taskCount.Text = $"{tasksPage.jobsGrid.Rows.Count} matching jobs. Open a job to update its tasks.";
            }
            tasksPage.taskSearch.TextChanged += (s, e) => ExecuteTaskSearch();
            if (tasksPage.searchTasksButton != null)
            {
                tasksPage.searchTasksButton.Click += (s, e) => ExecuteTaskSearch();
            }
            tasksPage.clearFilter.Click += (s, e) =>
            {
                allTasks = true; tasksPage.taskSearch.Clear(); filterLookup.Select(null);
                if (tasksPage.jobsGrid.Rows.Count > 0) tasksPage.jobsGrid.FirstDisplayedScrollingRowIndex = 0;
            };
            showJobTasks = id =>
            {
                tasksPage.RefreshRecords();
                RevealJob(id);
                taskMenu.Checked = true;
            };
            var order = tasksPage.order;
            var jobLookup = BindJobLookup(order, true, tasksPage, factory);
            jobLookup.AttachBrowseButton(tasksPage.browseJobButton);
            jobLookup.Changed += () =>
            {
                tasksPage.customerInfo.Text = jobLookup.CustomerDescription;
                tasksPage.customerInfo.Select(0, 0);
                tasksPage.jobDetails.Text = jobLookup.SelectedJobId is int id ? jobLookup.Describe(id) : "";
                tasksPage.jobDetails.Select(0, 0);
            };
            var service = tasksPage.service;
            var remarks = tasksPage.remarks;
            var status = tasksPage.status;
            tab.ConfigureEditor(row =>
            {
                jobLookup.Select(row.JobOrderId);
                service.Text = row.ServiceId; remarks.Text = row.Remarks; SelectValue(status, row.Status);
                tasksPage.serviceDetails.Text = row.ServiceId;
                tasksPage.serviceDetails.Select(0, 0);
                remarks.Select(0, 0);
                order.Enabled = false;
                service.Enabled = false;
                tasksPage.browseJobButton.Enabled = false;
                var parent = jobLookup.SelectedJob;
                bool queued = parent?.Status == "In Queue";
                bool closed = parent?.Status is "Completed" or "Released";
                status.Items.Clear();
                if (queued) status.Items.Add(string.IsNullOrWhiteSpace(row.Status) ? "Pending" : row.Status);
                else status.Items.AddRange(new object[] { "Pending", "Ongoing", "Done" });
                SelectValue(status, row.Status);
                status.Enabled = parent != null && !queued && !closed;
                remarks.ReadOnly = closed;
                tasksPage.saveButton.Enabled = row.JobTaskId > 0 && parent != null && !closed;
                tasksPage.workflowHint.Text = queued
                    ? "Work is on hold. Record the required 50% downpayment in Payments, then move this job to In Progress in Job Orders. You can still edit notes."
                    : closed ? "This job is completed or released. Task history is read-only."
                    : "Update the task status and mechanic notes, then save.";
            }, (row, db) =>
            {
                if (row.JobTaskId == 0) throw new InvalidOperationException("Assign services in Job Orders. Tasks are created automatically.");
                row.JobOrderId = jobLookup.Read(db).Value;
                row.Remarks = remarks.Text.Trim(); row.Status = Required(status, "Status");
            });
            return tab;
        }

        private CrmCollectionController<Payment> BuildPayments(Func<MasterCrmDbContext> factory, Func<string, bool> confirm)
        {
            var tab = new CrmCollectionController<Payment>(paymentsPage, "Payment", x => x.PaymentId, factory, confirm);
            UseEntryDialog(tab, paymentsPage.editorPanel, paymentsPage.resultLabel, paymentsPage.saveButton, "Payment");
            var order = paymentsPage.order;
            var jobLookup = BindJobLookup(order, true, paymentsPage, factory);
            jobLookup.AttachBrowseButton(paymentsPage.browseJobButton);

            Payment editing = new();
            bool loading = false, voidMode = false;
            decimal balanceBefore = 0, depositDue = 0;
            var type = paymentsPage.type;
            var amount = paymentsPage.amount;
            amount.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                if (char.IsDigit(e.KeyChar)) return;
                if (e.KeyChar == '.' && !amount.Text.Contains('.')) return;
                e.Handled = true;
            };
            amount.Leave += (s, e) =>
            {
                if (decimal.TryParse(amount.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out var v) ||
                    decimal.TryParse(amount.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out v))
                {
                    if (v > 0) amount.Text = v.ToString("N2");
                }
            };
            var method = paymentsPage.method;
            var normalEditor = tab.OpenDialogEditor;
            tab.OpenVoidEditor = row =>
            {
                if (row.IsVoided) { paymentsPage.resultLabel.Text = "This payment is already voided."; return false; }
                var billedJob = jobRecords.FirstOrDefault(j => j.JobOrderId == row.JobOrderId);
                var cust = billedJob?.CustomerId.HasValue == true && customerRecords.TryGetValue(billedJob.CustomerId.Value, out var c) ? c : null;
                using var voidDialog = new VoidPaymentDialogForm(row, billedJob, cust, factory, () =>
                {
                    tab.RefreshRecords();
                });
                return voidDialog.ShowDialog(paymentsPage.FindForm()) == DialogResult.OK;
            };

            void UpdatePaymentJobSummary()
            {
                if (loading) return;
                var job = jobLookup.SelectedJob;
                paymentsPage.customerInfo.Text = jobLookup.CustomerDescription;
                bool known = job?.TotalEstimatedCost != null;
                paymentsPage.billingStatus.Text = "Saved job status: " + BillingStatus(job);
                paymentsPage.billingStatus.ForeColor = known && job.OutstandingBalance == 0 ? Color.DarkGreen : Color.Black;
                balanceBefore = 0; depositDue = 0;
                if (known)
                {
                    decimal otherPaid = job.Payments.Where(p => !p.IsVoided && p.PaymentId != editing.PaymentId).Sum(p => p.AmountPaid);
                    balanceBefore = Math.Max(0, job.TotalEstimatedCost.Value - otherPaid);
                    depositDue = Math.Max(0, decimal.Round(job.TotalEstimatedCost.Value * .5m, 2, MidpointRounding.AwayFromZero) - otherPaid);
                    paymentsPage.billingSummary.Text = editing.PaymentId == 0
                        ? $"Job total: PHP {job.TotalEstimatedCost:N2}\nAlready paid: PHP {otherPaid:N2}\nRemaining balance: PHP {balanceBefore:N2}\nStill needed for 50% deposit: PHP {depositDue:N2}"
                        : $"Job total: PHP {job.TotalEstimatedCost:N2}\nOriginal receipt: PHP {editing.AmountPaid:N2}\nOther valid receipts: PHP {otherPaid:N2}\nAmount covered by this receipt: up to PHP {balanceBefore:N2}";
                }
                else paymentsPage.billingSummary.Text = job == null ? "Choose a job to see its bill." : "This job has no estimated cost. Set it in Job Orders first.";
                bool valid = decimal.TryParse(amount.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) && value > 0 && decimal.Round(value, 2) == value;
                paymentsPage.preview.Text = !known ? "" : voidMode ? $"Balance after void: PHP {balanceBefore:N2}" :
                    !valid ? "Enter the amount actually received." :
                    value > balanceBefore ? $"Excess customer credit: PHP {value - balanceBefore:N2}" :
                    $"Balance after saving: PHP {balanceBefore - value:N2}" + (value == balanceBefore ? " - Fully Paid" : " - Partially Paid");
                paymentsPage.fillBalanceButton.Enabled = editing.PaymentId == 0 && known && balanceBefore > 0 && !voidMode && !editing.IsVoided;
                paymentsPage.depositButton.Enabled = editing.PaymentId == 0 && known && depositDue > 0 && !voidMode && !editing.IsVoided;
                paymentsPage.resultLabel.Text = editing.IsVoided ? "Voided receipt: retained for history."
                    : voidMode ? paymentsPage.preview.Text
                    : editing.PaymentId != 0 ? "Correction replaces this receipt; it does not record additional money. " + paymentsPage.preview.Text
                    : paymentsPage.preview.Text;
                paymentsPage.saveButton.Enabled = !editing.IsVoided && (voidMode
                    ? !string.IsNullOrWhiteSpace(paymentsPage.voidReason.Text)
                    : editing.PaymentId == 0 && known && valid && (value <= balanceBefore || paymentsPage.allowCredit.Checked));
                if (editing.PaymentId != 0 && !voidMode)
                {
                    paymentsPage.preview.Text = "Saved receipt - read only";
                    paymentsPage.resultLabel.Text = "Additional money: use Receive Payment. Incorrect receipt: close, select Void Payment, enter a reason, then record a replacement.";
                    paymentsPage.billingSummary.Text = $"Receipt amount: PHP {editing.AmountPaid:N2}\nJob total: PHP {job?.TotalEstimatedCost:N2}\nTotal valid payments: PHP {job?.TotalPaid:N2}\nCurrent balance: PHP {job?.OutstandingBalance:N2}";
                }
            }
            void RefreshPaymentBill()
            {
                try
                {
                    if (jobLookup.SelectedJob is JobOrder selected)
                    {
                        using var db = factory();
                        var current = db.JobOrders.AsNoTracking().SingleOrDefault(j => j.JobOrderId == selected.JobOrderId);
                        selected.TotalEstimatedCost = current?.TotalEstimatedCost;
                        selected.Payments = db.Payments.AsNoTracking().Where(p => p.JobOrderId == selected.JobOrderId).ToList();
                    }
                    UpdatePaymentJobSummary();
                }
                catch
                {
                    if (jobLookup.SelectedJob is JobOrder selected) selected.TotalEstimatedCost = null;
                    UpdatePaymentJobSummary();
                    paymentsPage.resultLabel.Text = "Could not load the current bill. Close and reopen this payment after checking the connection.";
                }
            }
            jobLookup.Changed += () => { if (!loading) RefreshPaymentBill(); };
            amount.TextChanged += (s, e) => UpdatePaymentJobSummary();
            paymentsPage.allowCredit.CheckedChanged += (s, e) => UpdatePaymentJobSummary();
            paymentsPage.voidReason.TextChanged += (s, e) => UpdatePaymentJobSummary();
            paymentsPage.fillBalanceButton.Click += (s, e) => amount.Text = balanceBefore.ToString("F2");
            paymentsPage.depositButton.Click += (s, e) => amount.Text = depositDue.ToString("F2");
            paymentsPage.Grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex >= 0 && e.RowIndex >= 0 && paymentsPage.Grid.Columns[e.ColumnIndex].Name == "BillingStatus"
                    && paymentsPage.Grid.Rows[e.RowIndex].DataBoundItem is Payment receipt)
                {
                    var billedJob = jobRecords.FirstOrDefault(j => j.JobOrderId == receipt.JobOrderId);
                    e.Value = BillingStatus(billedJob);
                    e.CellStyle.ForeColor = billedJob?.TotalEstimatedCost != null && billedJob.OutstandingBalance == 0 ? Color.DarkGreen : Color.Black;
                    e.FormattingApplied = true;
                }
                if (e.ColumnIndex >= 0)
                {
                    string prop = paymentsPage.Grid.Columns[e.ColumnIndex].DataPropertyName;
                    if (prop == "PaymentId" && e.Value is int pid)
                    {
                        e.Value = RecordReference.Receipt(pid);
                        e.FormattingApplied = true;
                    }
                    else if (prop == "JobOrderId" && e.Value is int jid)
                    {
                        e.Value = RecordReference.JobOrder(jid);
                        e.FormattingApplied = true;
                    }
                    else if (prop == "AmountPaid" && e.Value is decimal val)
                    {
                        e.Value = $"₱{val:N2}";
                        e.FormattingApplied = true;
                    }
                }
            };
            tab.ConfigureEditor(row =>
            {
                loading = true;
                editing = row;
                jobLookup.Select(row.JobOrderId);
                paymentsPage.voidPayment.Checked = voidMode;
                paymentsPage.voidReason.Text = row.VoidReason;
                paymentsPage.voidReason.Visible = voidMode || row.IsVoided;
                paymentsPage.allowCredit.Checked = row.AllowCustomerCredit;
                paymentsPage.allowCredit.Visible = row.PaymentId == 0;
                // Ordinary receipt categories are derived again from the corrected amount.
                SelectValue(type, row.PaymentId == 0 ? "Customer payment" : row.PaymentType);
                amount.Text = row.PaymentId == 0 ? "" : row.AmountPaid.ToString("F2");
                SelectValue(method, row.PaymentMethod);
                order.Enabled = row.PaymentId == 0;
                paymentsPage.browseJobButton.Enabled = row.PaymentId == 0;
                paymentsPage.clearJobButton.Enabled = row.PaymentId == 0;
                paymentsPage.amountLabel.Text = row.PaymentId == 0 ? "New money received (PHP)" : "Saved receipt amount (PHP)";
                amount.ReadOnly = row.PaymentId != 0;
                type.Enabled = method.Enabled = row.PaymentId == 0;
                paymentsPage.saveButton.Visible = row.PaymentId == 0 || voidMode;
                paymentsPage.saveButton.Text = voidMode ? "Confirm Void Payment" : row.PaymentId == 0 ? "Record Payment" : "Save Correction";
                paymentsPage.modeLabel.Text = voidMode ? "Void payment - history is retained" : row.IsVoided ? "Voided payment (read-only)" : row.PaymentId == 0 ? "Receive a new payment" : "Saved receipt - read only";
                loading = false;
                RefreshPaymentBill();
            }, (row, db) =>
            {
                if (voidMode)
                {
                    if (!confirm("Void this payment? It will remain in history and be excluded from the amount paid.")) throw new InvalidOperationException("Voiding cancelled.");
                    row.IsVoided = true;
                    row.VoidReason = Required(paymentsPage.voidReason, "Void reason");
                    return;
                }
                if (!decimal.TryParse(amount.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var paid)
                    || paid <= 0 || paid > 9999999999999999.99m || decimal.Round(paid, 2) != paid)
                    throw new InvalidOperationException("Enter a positive amount with at most two decimal places.");
                int id = jobLookup.Read(db).Value;
                if (row.PaymentId != 0 && row.JobOrderId != id)
                    throw new InvalidOperationException("A saved receipt cannot be moved to another job. Void it and record a new payment for the correct job.");
                var job = db.JobOrders.Find(id);
                if (job?.TotalEstimatedCost == null) throw new InvalidOperationException("Set this job's estimated cost before accepting payment.");
                decimal otherPaid = db.Payments.Where(p => p.JobOrderId == id && !p.IsVoided && p.PaymentId != row.PaymentId).Sum(p => (decimal?)p.AmountPaid) ?? 0;
                row.JobOrderId = id;
                row.AllowCustomerCredit = paymentsPage.allowCredit.Checked;
                row.AmountPaid = paid;
                row.PaymentType = type.Text == "Customer payment"
                    ? paid >= Math.Max(0, job.TotalEstimatedCost.Value - otherPaid) ? "Final Balance" : otherPaid == 0 ? "Downpayment" : "Progress Payment"
                    : Required(type, "Payment category");
                row.PaymentMethod = Required(method, "Payment method");
            });

            ConfigurePaymentList(tab);
            paymentsPage.previewReceiptButton.Enabled = false;
            paymentsPage.printReceiptButton.Enabled = false;
            paymentsPage.Grid.SelectionChanged += (s, e) =>
            {
                bool hasPayment = tab.SelectedRecord is Payment p && p.PaymentId > 0;
                paymentsPage.previewReceiptButton.Enabled = hasPayment;
                paymentsPage.printReceiptButton.Enabled = hasPayment;
            };
            paymentsPage.previewReceiptButton.Click += (s, e) =>
            {
                if (tab.SelectedRecord is Payment p && p.PaymentId > 0)
                {
                    OpenPaymentReceipt(p, false, factory);
                }
                else
                {
                    paymentsPage.resultLabel.Text = "Select a payment to preview its receipt.";
                }
            };
            paymentsPage.printReceiptButton.Click += (s, e) =>
            {
                if (tab.SelectedRecord is Payment p && p.PaymentId > 0)
                {
                    OpenPaymentReceipt(p, false, factory);
                }
                else
                {
                    paymentsPage.resultLabel.Text = "Select a payment to preview and print its receipt.";
                }
            };

            // Job Balances View Management
            System.Collections.Generic.List<JobBalanceRow> allBalances = new();
            CrmGridStandardizer.ApplyColumnLayout(paymentsPage.jobBalancesGrid, "JobBalances");
            GridRowNumbering.Enable(paymentsPage.jobBalancesGrid);
            CrmGridStandardizer.RestoreColumnWidths(paymentsPage.jobBalancesGrid, "JobBalances");
            paymentsPage.jobBalancesGrid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string prop = paymentsPage.jobBalancesGrid.Columns[e.ColumnIndex].DataPropertyName;
                if (prop == "JobOrderId" && e.Value is int jid)
                {
                    e.Value = RecordReference.JobOrder(jid);
                    e.FormattingApplied = true;
                }
                else if (prop is "TotalEstimatedCost" or "TotalPaid" or "OutstandingBalance" && e.Value is decimal amt)
                {
                    e.Value = $"₱{amt:N2}";
                    e.FormattingApplied = true;
                }
            };

            void ApplyJobBalancesFilter()
            {
                string query = paymentsPage.jobBalancesSearch.Text.Trim();
                int statusIdx = paymentsPage.jobBalancesStatusFilter.SelectedIndex;

                var filtered = allBalances.Where(b =>
                {
                    if (statusIdx == 1 && b.JobStatus != "In Queue") return false;
                    if (statusIdx == 2 && b.JobStatus != "In Progress") return false;
                    if (statusIdx == 3 && b.JobStatus != "Completed") return false;
                    if (statusIdx == 4 && b.JobStatus != "Released") return false;

                    return query.Length == 0 ||
                        RecordReference.Matches(RecordReference.JobOrderPrefix, b.JobOrderId, query) ||
                        b.Customer.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        b.Vehicle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        b.PaymentStatus.Contains(query, StringComparison.OrdinalIgnoreCase);
                }).ToList();

                paymentsPage.jobBalancesGrid.DataSource = filtered;
                paymentsPage.jobBalancesCountLabel.Text = filtered.Count == 0 && allBalances.Count > 0
                    ? $"No matching job balances found for \"{query}\"."
                    : allBalances.Count == 0 ? "No job orders recorded yet."
                    : $"Showing {filtered.Count} of {allBalances.Count} job order balance(s).";

                paymentsPage.jobBalancesGrid.ClearSelection();
                paymentsPage.jobBalancesGrid.CurrentCell = null;
                paymentsPage.btnReceivePaymentForJob.Enabled = false;
                paymentsPage.btnViewJobHistory.Enabled = false;
                paymentsPage.btnOpenJobOrder.Enabled = false;
            }

            void RefreshJobBalances()
            {
                try
                {
                    using var db = factory();
                    var freshJobs = db.JobOrders.Include(j => j.Payments).AsNoTracking().ToList();
                    var refundsByJob = db.Refunds.AsNoTracking().GroupBy(r => r.JobOrderId)
                        .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

                    allBalances = freshJobs.OrderByDescending(j => j.JobOrderId).Select(j =>
                    {
                        string customerName = j.CustomerId.HasValue && customerRecords.TryGetValue(j.CustomerId.Value, out var c) ? c.FullName : "Walk-in Customer";
                        decimal totalRefunded = refundsByJob.TryGetValue(j.JobOrderId, out var refAmt) ? refAmt : 0m;
                        decimal validPaid = j.TotalPaid;
                        decimal netPaid = Math.Max(0m, validPaid - totalRefunded);
                        bool isCancelled = j.Status == "Cancelled";

                        string billing;
                        if (isCancelled)
                        {
                            if (totalRefunded > 0 && netPaid == 0)
                                billing = "Cancelled — Fully Refunded";
                            else if (totalRefunded > 0)
                                billing = $"Cancelled — Partial Refund (₱{totalRefunded:N2})";
                            else if (validPaid > 0)
                                billing = "Cancelled — Pending Refund Review";
                            else
                                billing = "Cancelled — No Payments";
                        }
                        else
                        {
                            billing = BillingStatus(j);
                        }

                        decimal? outstanding = isCancelled ? 0m : j.OutstandingBalance;

                        return new JobBalanceRow
                        {
                            JobOrderId = j.JobOrderId,
                            Customer = customerName,
                            Vehicle = j.VehicleDisplay,
                            TotalEstimatedCost = j.TotalEstimatedCost,
                            TotalPaid = j.TotalPaid,
                            TotalRefunded = totalRefunded,
                            OutstandingBalance = outstanding,
                            PaymentStatus = billing,
                            JobStatus = JobOrderWorkflow.Normalize(j.Status)
                        };
                    }).ToList();

                    ApplyJobBalancesFilter();
                }
                catch
                {
                    paymentsPage.jobBalancesCountLabel.Text = "Could not load job balances.";
                }
            }

            // Cancellation Reviews View Management
            System.Collections.Generic.List<CancellationReviewRow> allCancellationReviews = new();
            CrmGridStandardizer.ApplyColumnLayout(paymentsPage.cancellationReviewsGrid, "CancellationReviews");
            GridRowNumbering.Enable(paymentsPage.cancellationReviewsGrid);
            CrmGridStandardizer.RestoreColumnWidths(paymentsPage.cancellationReviewsGrid, "CancellationReviews");
            paymentsPage.cancellationReviewsGrid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string prop = paymentsPage.cancellationReviewsGrid.Columns[e.ColumnIndex].DataPropertyName;
                if (prop == "RequestId" && e.Value is int reqId)
                {
                    e.Value = RecordReference.CancellationRequest(reqId);
                    e.FormattingApplied = true;
                }
                else if (prop == "JobOrderId" && e.Value is int jid)
                {
                    e.Value = RecordReference.JobOrder(jid);
                    e.FormattingApplied = true;
                }
            };

            void ApplyCancellationReviewsFilter()
            {
                string query = paymentsPage.cancellationSearch.Text.Trim();
                int statusIdx = paymentsPage.cancellationStatusFilter.SelectedIndex;
                string filterStatus = statusIdx > 0 ? paymentsPage.cancellationStatusFilter.Items[statusIdx]?.ToString() ?? "" : "";

                var filtered = allCancellationReviews.Where(r =>
                {
                    if (!string.IsNullOrEmpty(filterStatus) && !r.Status.Equals(filterStatus, StringComparison.OrdinalIgnoreCase))
                        return false;

                    return query.Length == 0 ||
                        RecordReference.Matches(RecordReference.CancellationRequestPrefix, r.RequestId, query) ||
                        RecordReference.Matches(RecordReference.JobOrderPrefix, r.JobOrderId, query) ||
                        r.Customer.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Vehicle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.ReasonCategory.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Status.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.RequestedBy.Contains(query, StringComparison.OrdinalIgnoreCase);
                }).ToList();

                paymentsPage.cancellationReviewsGrid.DataSource = filtered;
                paymentsPage.cancellationCountLabel.Text = filtered.Count == 0 && allCancellationReviews.Count > 0
                    ? $"No matching cancellation requests found for \"{query}\"."
                    : allCancellationReviews.Count == 0 ? "No cancellation requests recorded."
                    : $"Showing {filtered.Count} of {allCancellationReviews.Count} cancellation request(s).";

                paymentsPage.cancellationReviewsGrid.ClearSelection();
                paymentsPage.cancellationReviewsGrid.CurrentCell = null;
                paymentsPage.btnOpenCancellationReview.Enabled = false;
                paymentsPage.btnProcessRefundForRequest.Enabled = false;
            }

            void RefreshCancellationReviews()
            {
                try
                {
                    using var db = factory();
                    var list = db.JobCancellationRequests.Include(r => r.JobOrder).AsNoTracking()
                        .OrderByDescending(r => r.RequestId)
                        .ToList();

                    allCancellationReviews = list.Select(r =>
                    {
                        var job = r.JobOrder;
                        string custName = job?.CustomerId is int cid && customerRecords.TryGetValue(cid, out var c) ? c.FullName : "Customer";
                        return new CancellationReviewRow
                        {
                            RequestId = r.RequestId,
                            JobOrderId = r.JobOrderId,
                            Customer = custName,
                            Vehicle = job?.VehicleDisplay ?? "Vehicle",
                            TotalPaidAtRequest = r.TotalPaidAtRequest,
                            ProposedRefund = r.ProposedRefund,
                            ApprovedRefund = r.ApprovedRefund,
                            ReasonCategory = r.ReasonCategory,
                            Status = r.Status,
                            RequestedBy = r.RequestedBy,
                            RequestedAtUtc = r.RequestedAtUtc,
                            Entity = r
                        };
                    }).ToList();

                    ApplyCancellationReviewsFilter();
                }
                catch
                {
                    paymentsPage.cancellationCountLabel.Text = "Could not load cancellation reviews.";
                }
            }

            paymentsPage.cancellationReviewsGrid.SelectionChanged += (s, e) =>
            {
                bool hasSelected = paymentsPage.cancellationReviewsGrid.SelectedRows.Count > 0 &&
                                   paymentsPage.cancellationReviewsGrid.SelectedRows[0].DataBoundItem is CancellationReviewRow;
                paymentsPage.btnOpenCancellationReview.Enabled = hasSelected;

                if (hasSelected && paymentsPage.cancellationReviewsGrid.SelectedRows[0].DataBoundItem is CancellationReviewRow row)
                {
                    paymentsPage.btnProcessRefundForRequest.Enabled = (row.Status == "Approved" || row.Status == "Pending Refund") && (row.ApprovedRefund ?? 0m) > 0m;
                }
                else
                {
                    paymentsPage.btnProcessRefundForRequest.Enabled = false;
                }
            };

            void OpenSelectedCancellationReview()
            {
                if (paymentsPage.cancellationReviewsGrid.SelectedRows.Count > 0 &&
                    paymentsPage.cancellationReviewsGrid.SelectedRows[0].DataBoundItem is CancellationReviewRow row)
                {
                    using var dlg = new JobCancellationReviewDialogForm(row.RequestId, factory);
                    dlg.ShowDialog(paymentsPage.FindForm());
                    RefreshCancellationReviews();
                    RefreshJobBalances();
                    RefreshRefunds();
                    tab.RefreshRecords();
                }
            }

            paymentsPage.btnOpenCancellationReview.Click += (s, e) => OpenSelectedCancellationReview();
            paymentsPage.cancellationReviewsGrid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OpenSelectedCancellationReview();
            };

            paymentsPage.btnProcessRefundForRequest.Click += (s, e) =>
            {
                if (paymentsPage.cancellationReviewsGrid.SelectedRows.Count > 0 &&
                    paymentsPage.cancellationReviewsGrid.SelectedRows[0].DataBoundItem is CancellationReviewRow row)
                {
                    using var db = factory();
                    var job = db.JobOrders.Include(j => j.Payments).FirstOrDefault(j => j.JobOrderId == row.JobOrderId);
                    var customer = job?.CustomerId is int cid && customerRecords.TryGetValue(cid, out var c) ? c : (job?.CustomerId.HasValue == true ? db.Customers.Find(job.CustomerId.Value) : null);

                    using var refundDlg = new RecordRefundDialogForm(row.JobOrderId, row.RequestId, null, customer, factory);
                    if (refundDlg.ShowDialog(paymentsPage.FindForm()) == DialogResult.OK)
                    {
                        RefreshCancellationReviews();
                        RefreshRefunds();
                        RefreshJobBalances();
                        tab.RefreshRecords();
                    }
                }
            };

            paymentsPage.cancellationSearch.TextChanged += (s, e) => ApplyCancellationReviewsFilter();
            paymentsPage.cancellationSearchButton.Click += (s, e) => ApplyCancellationReviewsFilter();
            paymentsPage.cancellationClearButton.Click += (s, e) =>
            {
                paymentsPage.cancellationSearch.Clear();
                paymentsPage.cancellationStatusFilter.SelectedIndex = 0;
                ApplyCancellationReviewsFilter();
            };
            paymentsPage.cancellationStatusFilter.SelectedIndexChanged += (s, e) => ApplyCancellationReviewsFilter();

            // Refunds View Management
            System.Collections.Generic.List<RefundHistoryRow> allRefunds = new();
            CrmGridStandardizer.ApplyColumnLayout(paymentsPage.refundsGrid, "RefundHistory");
            GridRowNumbering.Enable(paymentsPage.refundsGrid);
            CrmGridStandardizer.RestoreColumnWidths(paymentsPage.refundsGrid, "RefundHistory");
            paymentsPage.refundsGrid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string prop = paymentsPage.refundsGrid.Columns[e.ColumnIndex].DataPropertyName;
                if (prop == "RefundId" && e.Value is int refId)
                {
                    e.Value = RecordReference.Refund(refId);
                    e.FormattingApplied = true;
                }
                else if (prop == "JobOrderId" && e.Value is int jid)
                {
                    e.Value = RecordReference.JobOrder(jid);
                    e.FormattingApplied = true;
                }
            };

            void ApplyRefundsFilter()
            {
                string query = paymentsPage.refundSearch.Text.Trim();
                int methodIdx = paymentsPage.refundMethodFilter.SelectedIndex;
                string filterMethod = methodIdx > 0 ? paymentsPage.refundMethodFilter.Items[methodIdx]?.ToString() ?? "" : "";

                var filtered = allRefunds.Where(r =>
                {
                    if (!string.IsNullOrEmpty(filterMethod) && !r.RefundMethod.Equals(filterMethod, StringComparison.OrdinalIgnoreCase))
                        return false;

                    return query.Length == 0 ||
                        RecordReference.Matches(RecordReference.RefundPrefix, r.RefundId, query) ||
                        RecordReference.Matches(RecordReference.JobOrderPrefix, r.JobOrderId, query) ||
                        r.RecipientName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Reason.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.AuthorizedBy.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.RefundMethod.Contains(query, StringComparison.OrdinalIgnoreCase);
                }).ToList();

                paymentsPage.refundsGrid.DataSource = filtered;
                paymentsPage.refundCountLabel.Text = filtered.Count == 0 && allRefunds.Count > 0
                    ? $"No matching refunds found for \"{query}\"."
                    : allRefunds.Count == 0 ? "No refunds recorded yet."
                    : $"Showing {filtered.Count} of {allRefunds.Count} refund(s).";

                paymentsPage.refundsGrid.ClearSelection();
                paymentsPage.refundsGrid.CurrentCell = null;
                paymentsPage.btnOpenRefundJob.Enabled = false;
            }

            void RefreshRefunds()
            {
                try
                {
                    using var db = factory();
                    var list = db.Refunds.AsNoTracking()
                        .OrderByDescending(r => r.RefundId)
                        .ToList();

                    allRefunds = list.Select(r => new RefundHistoryRow
                    {
                        RefundId = r.RefundId,
                        JobOrderId = r.JobOrderId,
                        PaymentId = r.PaymentId,
                        RecipientName = r.RecipientName,
                        Amount = r.Amount,
                        RefundMethod = r.RefundMethod,
                        Reason = r.Reason,
                        AuthorizedBy = r.AuthorizedBy,
                        RefundDateUtc = r.RefundDateUtc
                    }).ToList();

                    ApplyRefundsFilter();
                }
                catch
                {
                    paymentsPage.refundCountLabel.Text = "Could not load refunds.";
                }
            }

            paymentsPage.refundsGrid.SelectionChanged += (s, e) =>
            {
                bool hasSelected = paymentsPage.refundsGrid.SelectedRows.Count > 0 &&
                                   paymentsPage.refundsGrid.SelectedRows[0].DataBoundItem is RefundHistoryRow;
                paymentsPage.btnOpenRefundJob.Enabled = hasSelected;
            };

            paymentsPage.btnOpenRefundJob.Click += (s, e) =>
            {
                if (paymentsPage.refundsGrid.SelectedRows.Count > 0 &&
                    paymentsPage.refundsGrid.SelectedRows[0].DataBoundItem is RefundHistoryRow row)
                {
                    orderMenu.Checked = true;
                    foreach (DataGridViewRow r in ordersPage.Grid.Rows)
                    {
                        if (r.DataBoundItem is JobOrder j && j.JobOrderId == row.JobOrderId)
                        {
                            r.Selected = true;
                            ordersPage.Grid.CurrentCell = r.Cells[0];
                            break;
                        }
                    }
                }
            };

            paymentsPage.refundSearch.TextChanged += (s, e) => ApplyRefundsFilter();
            paymentsPage.refundSearchButton.Click += (s, e) => ApplyRefundsFilter();
            paymentsPage.refundClearButton.Click += (s, e) =>
            {
                paymentsPage.refundSearch.Clear();
                paymentsPage.refundMethodFilter.SelectedIndex = 0;
                ApplyRefundsFilter();
            };
            paymentsPage.refundMethodFilter.SelectedIndexChanged += (s, e) => ApplyRefundsFilter();

            paymentsPage.btnViewCancellationReviews.Click += (s, e) => RefreshCancellationReviews();
            paymentsPage.btnViewRefunds.Click += (s, e) => RefreshRefunds();

            paymentsPage.jobBalancesGrid.SelectionChanged += (s, e) =>
            {
                bool hasSelected = paymentsPage.jobBalancesGrid.SelectedRows.Count > 0 &&
                                   paymentsPage.jobBalancesGrid.SelectedRows[0].DataBoundItem is JobBalanceRow;
                paymentsPage.btnReceivePaymentForJob.Enabled = hasSelected;
                paymentsPage.btnViewJobHistory.Enabled = hasSelected;
                paymentsPage.btnOpenJobOrder.Enabled = hasSelected;
            };

            paymentsPage.btnReceivePaymentForJob.Click += (s, e) =>
            {
                if (paymentsPage.jobBalancesGrid.SelectedRows.Count > 0 &&
                    paymentsPage.jobBalancesGrid.SelectedRows[0].DataBoundItem is JobBalanceRow row)
                {
                    using var dialog = new ReceivePaymentDialogForm(row.JobOrderId, factory, () =>
                    {
                        tab.RefreshRecords();
                        RefreshJobBalances();
                    });
                    dialog.ShowDialog(paymentsPage.FindForm());
                }
            };

            paymentsPage.btnViewJobHistory.Click += (s, e) =>
            {
                if (paymentsPage.jobBalancesGrid.SelectedRows.Count > 0 &&
                    paymentsPage.jobBalancesGrid.SelectedRows[0].DataBoundItem is JobBalanceRow row)
                {
                    paymentsPage.SwitchToReceiptsView();
                    paymentsPage.paymentSearch.Text = row.JobOrderId.ToString();
                    tab.ApplyListFilter();
                }
            };

            paymentsPage.btnOpenJobOrder.Click += (s, e) =>
            {
                if (paymentsPage.jobBalancesGrid.SelectedRows.Count > 0 &&
                    paymentsPage.jobBalancesGrid.SelectedRows[0].DataBoundItem is JobBalanceRow row)
                {
                    orderMenu.Checked = true;
                    foreach (DataGridViewRow r in ordersPage.Grid.Rows)
                    {
                        if (r.DataBoundItem is JobOrder j && j.JobOrderId == row.JobOrderId)
                        {
                            r.Selected = true;
                            ordersPage.Grid.CurrentCell = r.Cells[0];
                            break;
                        }
                    }
                }
            };

            paymentsPage.jobBalancesSearch.TextChanged += (s, e) => ApplyJobBalancesFilter();
            paymentsPage.jobBalancesSearchButton.Click += (s, e) => ApplyJobBalancesFilter();
            paymentsPage.jobBalancesClearButton.Click += (s, e) =>
            {
                paymentsPage.jobBalancesSearch.Clear();
                paymentsPage.jobBalancesStatusFilter.SelectedIndex = 0;
                ApplyJobBalancesFilter();
            };
            paymentsPage.jobBalancesStatusFilter.SelectedIndexChanged += (s, e) => ApplyJobBalancesFilter();

            tab.RecordsLoaded += _ => RefreshJobBalances();

            return tab;
        }

        private void ConfigurePaymentList(CrmCollectionController<Payment> tab)
        {
            var search = paymentsPage.paymentSearch;
            var clear = paymentsPage.clearSearchButton;
            var paymentStatus = paymentsPage.paymentStatusFilter;
            var billingStatus = paymentsPage.billingStatusFilter;
            var count = paymentsPage.paymentCountLabel;

            tab.ConfigureListTools(row =>
            {
                string query = search.Text.Trim();
                var billedJob = jobRecords.FirstOrDefault(j => j.JobOrderId == row.JobOrderId);
                string custName = billedJob?.CustomerId is int cid && customerRecords.TryGetValue(cid, out var c) ? c.FullName : "";
                string custPhone = billedJob?.CustomerId is int cid2 && customerRecords.TryGetValue(cid2, out var c2) ? c2.ContactNumber : "";
                string estRef = billedJob?.EstimateNumber ?? "";
                bool matchesReceipt = RecordReference.Matches(RecordReference.ReceiptPrefix, row.PaymentId, query);
                string jobBilling = BillingStatus(billedJob);

                if (paymentStatus.SelectedIndex == 1 && row.IsVoided) return false;
                if (paymentStatus.SelectedIndex == 2 && !row.IsVoided) return false;

                if (billingStatus.SelectedIndex == 1 && !jobBilling.StartsWith("Unpaid", StringComparison.OrdinalIgnoreCase)) return false;
                if (billingStatus.SelectedIndex == 2 && !jobBilling.StartsWith("Partially Paid", StringComparison.OrdinalIgnoreCase)) return false;
                if (billingStatus.SelectedIndex == 3 && !jobBilling.StartsWith("Fully Paid", StringComparison.OrdinalIgnoreCase)) return false;

                return query.Length == 0 ||
                    matchesReceipt ||
                    RecordReference.Matches(RecordReference.JobOrderPrefix, row.JobOrderId, query) ||
                    custName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    custPhone.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    estRef.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (row.PaymentType ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (row.PaymentMethod ?? "").Contains(query, StringComparison.OrdinalIgnoreCase);
            }, (visible, total) =>
            {
                string query = search.Text.Trim();
                count.Text = visible == 0 && total > 0
                    ? $"No matching records found for \"{query}\"."
                    : total == 0 ? "No payments recorded yet."
                    : $"Showing {visible} of {total} payments.";
            });

            var debounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
            debounceTimer.Tick += (s, e) =>
            {
                debounceTimer.Stop();
                tab.ApplyListFilter();
            };
            search.TextChanged += (s, e) =>
            {
                debounceTimer.Stop();
                debounceTimer.Start();
            };
            paymentsPage.searchButton.Click += (s, e) =>
            {
                debounceTimer.Stop();
                tab.ApplyListFilter();
            };
            paymentStatus.SelectedIndexChanged += (s, e) => tab.ApplyListFilter();
            billingStatus.SelectedIndexChanged += (s, e) => tab.ApplyListFilter();
            clear.Click += (s, e) =>
            {
                debounceTimer.Stop();
                search.Clear();
                paymentStatus.SelectedIndex = 0;
                billingStatus.SelectedIndex = 0;
                tab.ApplyListFilter();
            };
        }

        private void OpenMechanicCopy(JobOrder job, Func<MasterCrmDbContext> factory)
        {
            if (job == null) return;
            using var db = factory();
            var tasks = db.JobTasks.AsNoTracking().Where(t => t.JobOrderId == job.JobOrderId).OrderBy(t => t.JobTaskId).ToList();
            var customer = job.CustomerId is int cid && customerRecords.TryGetValue(cid, out var c) ? c : db.Customers.Find(job.CustomerId ?? 0);
            var data = new MechanicWorkOrderData
            {
                JobOrderId = job.JobOrderId,
                EstimateReference = HistoricalCompatibility.Reference(job),
                CustomerName = customer?.FullName ?? "Unlinked Customer",
                VehicleDetails = job.VehicleDisplay,
                IntakeNotes = job.InquiryNotes ?? "",
                JobStatus = job.Status ?? "In Progress",
                AssignedMechanic = job.AssignedMechanicDisplay,
                PrintDate = DateTime.UtcNow,
                Tasks = tasks.Select(t => new MechanicTaskItem
                {
                    TaskId = t.JobTaskId,
                    ServiceName = t.ServiceId,
                    AssignedTechnician = job.AssignedMechanicDisplay,
                    Status = t.Status,
                    Notes = t.Remarks
                }).ToList()
            };
            var business = LegacyBusinessConfig.Load();
            string html = ReportTemplateBuilder.BuildMechanicCopyHtml(data, business);
            ReportPreviewDialogForm.ShowPreview(this, $"Mechanic's Job Order Copy - {RecordReference.JobOrder(job.JobOrderId)}", html);
        }

        private void OpenPaymentReceipt(Payment payment, bool directPrint, Func<MasterCrmDbContext> factory)
        {
            if (payment == null || payment.PaymentId == 0) return;
            using var db = factory();
            var freshPayment = db.Payments.AsNoTracking().FirstOrDefault(p => p.PaymentId == payment.PaymentId);
            if (freshPayment == null) return;

            if (freshPayment.CreatedAt == default)
            {
                MessageBox.Show(this, "The receipt cannot be displayed because the payment record is missing a valid transaction timestamp.", "Payment Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var job = db.JobOrders.Include(j => j.Payments).FirstOrDefault(j => j.JobOrderId == freshPayment.JobOrderId);
            var customer = job?.CustomerId is int cid && customerRecords.TryGetValue(cid, out var c) ? c : db.Customers.Find(job?.CustomerId ?? 0);

            string subStr = freshPayment.SubmissionId.ToString();
            var audit = db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "Payment" && a.Action == "Added" && a.Changes.Contains(subStr))
                .OrderByDescending(a => a.AuditEntryId)
                .FirstOrDefault();
            string recordedBy = (audit != null && !string.IsNullOrWhiteSpace(audit.Actor) && audit.Actor != "Unattributed application")
                ? audit.Actor
                : "Not recorded";

            var validPayments = job?.Payments.Where(p => !p.IsVoided).ToList() ?? new System.Collections.Generic.List<Payment>();
            decimal totalPaid = validPayments.Sum(p => p.AmountPaid);
            decimal? totalEstimatedCost = job?.TotalEstimatedCost;
            decimal? balance = totalEstimatedCost.HasValue ? Math.Max(0m, totalEstimatedCost.Value - totalPaid) : null;
            decimal? credit = totalEstimatedCost.HasValue ? Math.Max(0m, totalPaid - totalEstimatedCost.Value) : null;

            var data = new PaymentReceiptData
            {
                PaymentId = freshPayment.PaymentId,
                PaymentDate = freshPayment.CreatedAt,
                CustomerName = customer?.FullName ?? "Customer",
                JobOrderId = freshPayment.JobOrderId,
                VehicleDetails = job?.VehicleDisplay ?? "Vehicle Details",
                PaymentType = freshPayment.PaymentType,
                PaymentMethod = freshPayment.PaymentMethod,
                AmountReceived = freshPayment.AmountPaid,
                TotalEstimatedCost = totalEstimatedCost,
                HistoricalQualification = HistoricalCompatibility.Qualification(job),
                TotalPaid = totalPaid,
                OutstandingBalance = balance,
                CustomerCredit = credit,
                RecordedBy = recordedBy,
                PrintedBy = "Desktop Operator",
                IsVoided = freshPayment.IsVoided,
                VoidReason = freshPayment.VoidReason,
                VoidedAtUtc = freshPayment.VoidedAtUtc,
                CalculatedAtUtc = DateTime.UtcNow
            };
            var business = LegacyBusinessConfig.Load();
            string html = ReportTemplateBuilder.BuildPaymentReceiptHtml(data, business);
            ReportPreviewDialogForm.ShowPreview(this, $"Payment Receipt - {data.ReceiptReference}", html);
        }
    }

    public sealed class CancellationReviewRow
    {
        public int RequestId { get; set; }
        public int JobOrderId { get; set; }
        public string Customer { get; set; } = "";
        public string Vehicle { get; set; } = "";
        public decimal TotalPaidAtRequest { get; set; }
        public decimal ProposedRefund { get; set; }
        public decimal? ApprovedRefund { get; set; }
        public string ReasonCategory { get; set; } = "";
        public string Status { get; set; } = "";
        public string RequestedBy { get; set; } = "";
        public DateTime RequestedAtUtc { get; set; }
        public crm.domain.entities.JobCancellationRequest Entity { get; set; } = null!;
    }

    public sealed class RefundHistoryRow
    {
        public int RefundId { get; set; }
        public int JobOrderId { get; set; }
        public int? PaymentId { get; set; }
        public string RecipientName { get; set; } = "";
        public decimal Amount { get; set; }
        public string RefundMethod { get; set; } = "";
        public string Reason { get; set; } = "";
        public string AuthorizedBy { get; set; } = "";
        public DateTime RefundDateUtc { get; set; }
    }
}


