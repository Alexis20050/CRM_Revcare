using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public sealed record UnresolvedAssignmentItem(
    int JobOrderId,
    string? CurrentStringAssignment,
    string Status,
    string Reason,
    string RecommendedAction
);

public sealed class UnresolvedAssignmentReport
{
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
    public string Context { get; init; } = "";
    public int TotalJobsEvaluated { get; init; }
    public int ReconciledCount { get; init; }
    public List<UnresolvedAssignmentItem> UnresolvedItems { get; init; } = new();
    public bool HasUnresolved => UnresolvedItems.Count > 0;
}

public static class AssignmentReconciliationService
{
    public static UnresolvedAssignmentReport ReconcileLegacy(MasterCrmDbContext db)
    {
        var activeStaff = db.Staff.AsNoTracking()
            .Where(s => s.IsActive)
            .AsEnumerable()
            .Where(s => MechanicEligibilityService.IsEligibleWorkshopRole(s.Role))
            .ToList();
        var jobs = db.JobOrders.ToList();

        int reconciled = 0;
        var unresolved = new List<UnresolvedAssignmentItem>();

        foreach (var job in jobs)
        {
            if (job.Status is "Completed" or "Released")
            {
                // Completed or released jobs cannot be reassigned through standard or backfill operations
                continue;
            }

            if (!string.IsNullOrWhiteSpace(job.AssignedMechanicUserId))
            {
                // Already has stable ID
                continue;
            }

            if (string.IsNullOrWhiteSpace(job.AssignedMechanic) || job.AssignedMechanic == "Unassigned")
            {
                // Unassigned job
                continue;
            }

            string raw = job.AssignedMechanic.Trim();
            var matches = activeStaff.Where(s =>
                s.DisplayName.Equals(raw, StringComparison.OrdinalIgnoreCase) ||
                s.StaffId.Equals(raw, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (matches.Count == 1)
            {
                job.AssignedMechanicUserId = matches[0].StaffId;
                job.AssignedMechanic = matches[0].DisplayName;
                reconciled++;
            }
            else if (matches.Count == 0)
            {
                unresolved.Add(new UnresolvedAssignmentItem(
                    job.JobOrderId,
                    job.AssignedMechanic,
                    job.Status,
                    "No matching active staff member found in directory.",
                    "Register the staff member in the staff directory or explicitly reassign this job."
                ));
            }
            else
            {
                unresolved.Add(new UnresolvedAssignmentItem(
                    job.JobOrderId,
                    job.AssignedMechanic,
                    job.Status,
                    $"Ambiguous match: {matches.Count} active staff members have display name '{raw}'.",
                    "Explicitly select the intended mechanic using Assign/Reassign Mechanic dialog."
                ));
            }
        }

        if (reconciled > 0)
        {
            db.SaveChanges();
        }

        return new UnresolvedAssignmentReport
        {
            Context = "Legacy Master CRM",
            TotalJobsEvaluated = jobs.Count,
            ReconciledCount = reconciled,
            UnresolvedItems = unresolved
        };
    }

    public static async Task<UnresolvedAssignmentReport> ReconcileTenantAsync(TenantCrmDbContext db, PlatformMasterDbContext master, string tenantId)
    {
        var activeMembers = await master.Memberships.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.IsActive &&
                (m.Role == "LeadMechanic" || m.Role == "AutomotiveMechanic" || m.Role == "DetailingTechnician"))
            .ToListAsync();

        var profiles = await db.Staff.AsNoTracking().ToListAsync();
        var staffLookup = (from m in activeMembers
                           join p in profiles on m.UserId equals p.UserId into ps
                           from p in ps.DefaultIfEmpty()
                           select new { m.UserId, DisplayName = p?.DisplayName ?? m.DisplayName ?? m.UserId }).ToList();

        var jobs = await db.JobOrders.Include(j => j.Tasks).ToListAsync();
        var assignments = await db.TaskAssignments.AsNoTracking().ToListAsync();

        int reconciled = 0;
        var unresolved = new List<UnresolvedAssignmentItem>();

        foreach (var job in jobs)
        {
            if (job.Status is "Completed" or "Released")
            {
                // Completed or released jobs cannot be reassigned through standard or backfill operations
                continue;
            }

            var taskIds = job.Tasks.Select(t => t.JobTaskId).ToHashSet();
            var taskAssignees = assignments.Where(a => taskIds.Contains(a.JobTaskId)).Select(a => a.UserId).Distinct().ToList();

            if (taskAssignees.Count > 1)
            {
                // Conflict: old tasks have multiple different assignees
                unresolved.Add(new UnresolvedAssignmentItem(
                    job.JobOrderId,
                    job.AssignedMechanic,
                    job.Status,
                    $"Conflicting task assignees detected: [{string.Join(", ", taskAssignees)}].",
                    "Authorized Lead Mechanic or Owner must explicitly assign one responsible mechanic to the whole job."
                ));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(job.AssignedMechanicUserId))
            {
                // Already assigned at job level
                continue;
            }

            if (taskAssignees.Count == 1)
            {
                string userId = taskAssignees[0];
                var match = staffLookup.FirstOrDefault(s => s.UserId == userId);
                if (match != null)
                {
                    job.AssignedMechanicUserId = match.UserId;
                    job.AssignedMechanic = match.DisplayName;
                    reconciled++;
                    continue;
                }
            }

            if (!string.IsNullOrWhiteSpace(job.AssignedMechanic) && job.AssignedMechanic != "Unassigned")
            {
                string raw = job.AssignedMechanic.Trim();
                var matches = staffLookup.Where(s =>
                    s.DisplayName.Equals(raw, StringComparison.OrdinalIgnoreCase) ||
                    s.UserId.Equals(raw, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                if (matches.Count == 1)
                {
                    job.AssignedMechanicUserId = matches[0].UserId;
                    job.AssignedMechanic = matches[0].DisplayName;
                    reconciled++;
                }
                else if (matches.Count == 0)
                {
                    unresolved.Add(new UnresolvedAssignmentItem(
                        job.JobOrderId,
                        job.AssignedMechanic,
                        job.Status,
                        "No matching active tenant workshop staff found.",
                        "Add staff membership or reassign the job order."
                    ));
                }
                else
                {
                    unresolved.Add(new UnresolvedAssignmentItem(
                        job.JobOrderId,
                        job.AssignedMechanic,
                        job.Status,
                        $"Ambiguous match: {matches.Count} active staff members match '{raw}'.",
                        "Explicitly reassign the job order."
                    ));
                }
            }
        }

        if (reconciled > 0)
        {
            await db.SaveChangesAsync();
        }

        return new UnresolvedAssignmentReport
        {
            Context = $"Tenant [{tenantId}]",
            TotalJobsEvaluated = jobs.Count,
            ReconciledCount = reconciled,
            UnresolvedItems = unresolved
        };
    }
}
