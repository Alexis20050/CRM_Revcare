#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public sealed record MechanicOption(string UserId, string DisplayName, string Role, int UnfinishedJobs)
    {
        public string WorkloadText => $"{UnfinishedJobs} active job{(UnfinishedJobs == 1 ? "" : "s")}";
    }

    public partial class AssignMechanicDialogForm : Form
    {
        private readonly JobOrder job;
        private readonly Func<MasterCrmDbContext> factory;
        private readonly Customer? customer;
        private List<MechanicOption> allMechanics = new();
        private bool isReassigning;
        private WorkshopJobCategory jobCategory = WorkshopJobCategory.Unspecified;

        public AssignMechanicDialogForm(JobOrder job, Customer? customer, Func<MasterCrmDbContext> factory)
        {
            this.job = job ?? throw new ArgumentNullException(nameof(job));
            this.customer = customer;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

            InitializeComponent();

            isReassigning = (!string.IsNullOrWhiteSpace(job.AssignedMechanicUserId) && job.AssignedMechanicUserId != "Unassigned")
                || (!string.IsNullOrWhiteSpace(job.AssignedMechanic) && job.AssignedMechanic != "Unassigned");

            try
            {
                using var db = factory();
                var tasks = db.JobTasks.AsNoTracking().Where(t => t.JobOrderId == job.JobOrderId).ToList();
                jobCategory = MechanicEligibilityService.DetermineJobCategory(job, tasks);
            }
            catch
            {
                jobCategory = MechanicEligibilityService.DetermineJobCategory(job);
            }

            string categoryText = jobCategory switch
            {
                WorkshopJobCategory.Mechanical => "Mechanical Service",
                WorkshopJobCategory.Detailing => "Detailing Service",
                WorkshopJobCategory.Mixed => "Mixed (Mechanical & Detailing)",
                _ => "General Workshop"
            };

            lblJobInfo.Text = $"{RecordReference.JobOrder(job.JobOrderId)} ({job.EstimateNumber}) — Status: {JobOrderWorkflow.Normalize(job.Status)} — Category: {categoryText}";
            lblCustomerInfo.Text = $"Customer: {customer?.FullName ?? "Walk-in Customer"} ({customer?.ContactNumber ?? "No phone"})";
            lblVehicleInfo.Text = $"Vehicle: {job.VehicleDisplay}";
            lblCurrentMechanic.Text = $"Current Assigned Mechanic: {job.AssignedMechanicDisplay}";

            if (isReassigning)
            {
                titleLabel.Text = $"Reassign Mechanic — {RecordReference.JobOrder(job.JobOrderId)}";
                btnAssign.Text = "Confirm Reassignment";
                lblReason.Text = "Reason for Reassignment (Required):";
                reasonBox.Visible = true;
                lblReason.Visible = true;
            }
            else
            {
                titleLabel.Text = $"Assign Mechanic — {RecordReference.JobOrder(job.JobOrderId)}";
                btnAssign.Text = "Assign Mechanic";
                lblReason.Text = "Assignment Note (Optional):";
                reasonBox.PlaceholderText = "Optional assignment instructions or notes...";
            }

            LoadMechanics();

            searchBox.TextChanged += (s, e) => FilterMechanics();
            clearSearchButton.Click += (s, e) => { searchBox.Clear(); FilterMechanics(); };

            mechanicsGrid.SelectionChanged += (s, e) => UpdateButtonsState();
            reasonBox.TextChanged += (s, e) => UpdateButtonsState();

            btnAssign.Click += (s, e) => ExecuteAssignment();
            UpdateButtonsState();
        }

        private void LoadMechanics()
        {
            try
            {
                using var db = factory();

                // Calculate active workloads across existing jobs using stable staff IDs
                var workloads = db.JobOrders.AsNoTracking()
                    .Where(j => !string.IsNullOrEmpty(j.AssignedMechanicUserId) && j.Status != "Completed" && j.Status != "Released")
                    .GroupBy(j => j.AssignedMechanicUserId!)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Persisted active qualified workshop roster from database
                var staffEntities = db.Staff.AsNoTracking()
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayName)
                    .ToList();

                // Strict filtering: ONLY active staff with eligible workshop roles matching job specialty
                var eligibleStaff = staffEntities.Where(s =>
                {
                    var roleType = MechanicEligibilityService.NormalizeRole(s.Role);
                    if (!MechanicEligibilityService.IsEligibleWorkshopRole(roleType)) return false;
                    return MechanicEligibilityService.IsRoleCompatibleWithJob(roleType, jobCategory, out _);
                }).ToList();

                if (eligibleStaff.Count == 0)
                {
                    allMechanics = new List<MechanicOption>();
                    FilterMechanics();
                    statusLabel.Text = jobCategory switch
                    {
                        WorkshopJobCategory.Detailing => "No eligible Detailing Technicians or Lead Mechanics found.",
                        WorkshopJobCategory.Mechanical => "No eligible Automotive Mechanics or Lead Mechanics found.",
                        WorkshopJobCategory.Mixed => "No qualified Lead Mechanics found for mixed-specialty job.",
                        _ => "No eligible workshop mechanics found. Register qualified workshop staff first."
                    };
                    btnAssign.Enabled = false;
                    return;
                }

                allMechanics = eligibleStaff.Select(s => new MechanicOption(
                    s.StaffId,
                    s.DisplayName,
                    s.Role,
                    workloads.GetValueOrDefault(s.StaffId, 0)
                )).OrderBy(m => m.UnfinishedJobs).ThenBy(m => m.DisplayName).ToList();

                FilterMechanics();
            }
            catch (Exception ex)
            {
                statusLabel.Text = "Could not load staff: " + ex.Message;
            }
        }

        private void FilterMechanics()
        {
            string q = searchBox.Text.Trim();
            var filtered = string.IsNullOrWhiteSpace(q)
                ? allMechanics
                : allMechanics.Where(m => m.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                          m.Role.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            mechanicsGrid.DataSource = filtered;
            mechanicsGrid.ClearSelection();
            mechanicsGrid.CurrentCell = null;
        }

        private void UpdateButtonsState()
        {
            var selected = GetSelectedMechanic();
            bool hasSelection = selected != null;
            bool isCurrent = selected != null && (!string.IsNullOrEmpty(job.AssignedMechanicUserId)
                ? selected.UserId.Equals(job.AssignedMechanicUserId, StringComparison.OrdinalIgnoreCase)
                : selected.DisplayName.Equals(job.AssignedMechanic, StringComparison.OrdinalIgnoreCase));
            bool reasonOk = !isReassigning || !string.IsNullOrWhiteSpace(reasonBox.Text);

            if (job.Status is "Completed" or "Released")
            {
                btnAssign.Enabled = false;
                statusLabel.Text = "Completed or released jobs cannot be reassigned.";
                return;
            }

            if (allMechanics.Count == 0)
            {
                btnAssign.Enabled = false;
                statusLabel.Text = "No eligible workshop staff found. Register staff before assigning jobs.";
                return;
            }

            if (!hasSelection)
            {
                btnAssign.Enabled = false;
                statusLabel.Text = "Select an eligible mechanic from the list.";
                return;
            }

            if (isCurrent)
            {
                btnAssign.Enabled = false;
                statusLabel.Text = "This mechanic is already assigned to this job.";
                return;
            }

            if (isReassigning && !reasonOk)
            {
                btnAssign.Enabled = false;
                statusLabel.Text = "Enter a reason before confirming reassignment.";
                return;
            }

            btnAssign.Enabled = true;
            statusLabel.Text = "";
        }

        private MechanicOption? GetSelectedMechanic()
        {
            if (mechanicsGrid.SelectedRows.Count > 0 && mechanicsGrid.SelectedRows[0].DataBoundItem is MechanicOption opt)
                return opt;
            return null;
        }

        private void ExecuteAssignment()
        {
            var selected = GetSelectedMechanic();
            if (selected == null) return;

            string reason = reasonBox.Text.Trim();
            if (isReassigning && string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show(this, "A reason is required to reassign an active Job Order.", "Reason Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                reasonBox.Focus();
                return;
            }

            try
            {
                using var db = factory();
                var currentJob = db.JobOrders.Find(job.JobOrderId);
                if (currentJob == null)
                {
                    MessageBox.Show(this, "Job order not found. It may have been deleted.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (currentJob.Status is "Completed" or "Released")
                {
                    MessageBox.Show(this, "Completed or released jobs cannot be reassigned through standard controls.", "Action Blocked",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Concurrency verification
                if (job.RowVersion is { Length: > 0 })
                {
                    db.Entry(currentJob).Property(j => j.RowVersion).OriginalValue = job.RowVersion;
                }

                // Centralized eligibility and specialty validation
                var validation = MechanicEligibilityService.ValidateAssignment(db, currentJob, selected.UserId);
                if (!validation.IsValid)
                {
                    MessageBox.Show(this, validation.ErrorMessage ?? "The selected staff member is not eligible for this job assignment.",
                        "Assignment Ineligible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string? previousId = currentJob.AssignedMechanicUserId;
                string previousName = currentJob.AssignedMechanicDisplay;
                currentJob.AssignedMechanicUserId = selected.UserId;
                currentJob.AssignedMechanic = selected.DisplayName;

                // Record audit entry
                db.AuditEntries.Add(new AuditEntry
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    Actor = db.AuditActor,
                    EntityType = "JobOrder",
                    Action = isReassigning ? "ReassignMechanic" : "AssignMechanic",
                    Changes = JsonSerializer.Serialize(new
                    {
                        currentJob.JobOrderId,
                        PreviousMechanicId = previousId,
                        PreviousMechanicName = previousName,
                        NewMechanicId = selected.UserId,
                        NewMechanicName = selected.DisplayName,
                        Reason = reason
                    })
                });

                db.SaveChanges();

                job.AssignedMechanicUserId = selected.UserId;
                job.AssignedMechanic = selected.DisplayName;
                job.RowVersion = currentJob.RowVersion;

                MessageBox.Show(this,
                    $"{RecordReference.JobOrder(job.JobOrderId)} successfully assigned to {selected.DisplayName}.\n\nAll service tasks under this job now inherit this mechanic assignment.",
                    "Mechanic Assigned", MessageBoxButtons.OK, MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (DbUpdateConcurrencyException)
            {
                MessageBox.Show(this, "This job order was modified by another user. Please reload the job list and try again.", "Concurrency Conflict",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to save assignment: " + ex.Message, "Save Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
