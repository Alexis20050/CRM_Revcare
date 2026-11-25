#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public enum WorkshopRoleType
{
    Unknown,
    Ineligible,
    LeadMechanic,
    AutomotiveMechanic,
    DetailingTechnician
}

public enum WorkshopJobCategory
{
    Unspecified,
    Mechanical,
    Detailing,
    Mixed
}

public sealed record MechanicAssignmentValidation(bool IsValid, string? ErrorMessage)
{
    public static MechanicAssignmentValidation Success { get; } = new(true, null);
    public static MechanicAssignmentValidation Failure(string error) => new(false, error);
}

public static class MechanicEligibilityService
{
    public static readonly string[] EligibleWorkshopRoles =
    [
        "Lead Mechanic",
        "Automotive Mechanic",
        "Detailing Technician",
        "LeadMechanic",
        "AutomotiveMechanic",
        "DetailingTechnician"
    ];

    public static readonly string[] ExcludedRoles =
    [
        "Cashier",
        "Service Advisor",
        "ServiceAdvisor",
        "Super Admin",
        "SuperAdmin",
        "Owner",
        "Manager",
        "Owner / Manager",
        "OwnerManager"
    ];

    public static WorkshopRoleType NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return WorkshopRoleType.Unknown;
        string r = role.Trim();

        if (r.Equals("Lead Mechanic", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("LeadMechanic", StringComparison.OrdinalIgnoreCase))
        {
            return WorkshopRoleType.LeadMechanic;
        }

        if (r.Equals("Automotive Mechanic", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("AutomotiveMechanic", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("Mechanic", StringComparison.OrdinalIgnoreCase))
        {
            return WorkshopRoleType.AutomotiveMechanic;
        }

        if (r.Equals("Detailing Technician", StringComparison.OrdinalIgnoreCase) ||
            r.Equals("DetailingTechnician", StringComparison.OrdinalIgnoreCase))
        {
            return WorkshopRoleType.DetailingTechnician;
        }

        return WorkshopRoleType.Ineligible;
    }

    public static bool IsEligibleWorkshopRole(string? role)
    {
        var roleType = NormalizeRole(role);
        return roleType is WorkshopRoleType.LeadMechanic
            or WorkshopRoleType.AutomotiveMechanic
            or WorkshopRoleType.DetailingTechnician;
    }

    public static bool IsEligibleWorkshopRole(WorkshopRoleType roleType) =>
        roleType is WorkshopRoleType.LeadMechanic
            or WorkshopRoleType.AutomotiveMechanic
            or WorkshopRoleType.DetailingTechnician;

    public static bool IsActiveEligibleMechanic(WorkshopStaff? staff) =>
        staff != null && staff.IsActive && IsEligibleWorkshopRole(staff.Role);

    public static WorkshopJobCategory ClassifyServiceName(string? serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return WorkshopJobCategory.Unspecified;
        string clean = JobServices.CleanServiceName(serviceName).ToLowerInvariant();

        if (clean.Contains("detail") || clean.Contains("wash") || clean.Contains("wax") ||
            clean.Contains("polish") || clean.Contains("ceramic") || clean.Contains("tint") ||
            clean.Contains("paint protection") || clean.Contains("interior clean"))
        {
            return WorkshopJobCategory.Detailing;
        }

        if (clean.Contains("oil") || clean.Contains("brake") || clean.Contains("alignment") ||
            clean.Contains("air-con") || clean.Contains("air condition") || clean.Contains("battery") ||
            clean.Contains("engine") || clean.Contains("spark") || clean.Contains("suspension") ||
            clean.Contains("shock") || clean.Contains("transmission") || clean.Contains("tire") ||
            clean.Contains("tyre") || clean.Contains("camber") || clean.Contains("clutch") ||
            clean.Contains("radiator") || clean.Contains("alternator") || clean.Contains("tune-up"))
        {
            return WorkshopJobCategory.Mechanical;
        }

        return WorkshopJobCategory.Mechanical;
    }

    public static WorkshopJobCategory DetermineJobCategory(JobOrder job, IEnumerable<JobTask>? tasks = null)
    {
        var taskList = tasks?.ToList() ?? job.Tasks?.ToList();
        if (taskList != null && taskList.Count > 0)
        {
            bool hasMechanical = false;
            bool hasDetailing = false;

            foreach (var t in taskList)
            {
                string name = !string.IsNullOrWhiteSpace(t.ServiceId)
                    ? t.ServiceId
                    : (!string.IsNullOrWhiteSpace(t.ServiceNameSnapshot)
                        ? t.ServiceNameSnapshot
                        : (t.ServiceCatalog?.Name ?? ""));

                var cat = ClassifyServiceName(name);
                if (cat == WorkshopJobCategory.Mechanical) hasMechanical = true;
                if (cat == WorkshopJobCategory.Detailing) hasDetailing = true;
            }

            if (hasMechanical && hasDetailing) return WorkshopJobCategory.Mixed;
            if (hasDetailing) return WorkshopJobCategory.Detailing;
            if (hasMechanical) return WorkshopJobCategory.Mechanical;
        }

        // Secondary check via Estimate reference / Inquiry notes
        if (!string.IsNullOrWhiteSpace(job.EstimateId))
        {
            var estCat = ClassifyServiceName(job.EstimateId);
            if (estCat != WorkshopJobCategory.Unspecified) return estCat;
        }

        if (!string.IsNullOrWhiteSpace(job.InquiryNotes))
        {
            var inqCat = ClassifyServiceName(job.InquiryNotes);
            if (inqCat != WorkshopJobCategory.Unspecified) return inqCat;
        }

        return WorkshopJobCategory.Unspecified;
    }

    public static bool IsRoleCompatibleWithJob(WorkshopRoleType roleType, WorkshopJobCategory category, out string? reason)
    {
        reason = null;
        switch (roleType)
        {
            case WorkshopRoleType.LeadMechanic:
                // Lead Mechanics hold workshop supervision qualifications across both Mechanical and Detailing domains.
                return true;

            case WorkshopRoleType.AutomotiveMechanic:
                if (category == WorkshopJobCategory.Detailing)
                {
                    reason = "Automotive Mechanics are not qualified for detailing jobs. Detailing jobs require a Detailing Technician or Lead Mechanic.";
                    return false;
                }
                if (category == WorkshopJobCategory.Mixed)
                {
                    reason = "Automotive Mechanics are not qualified for detailing tasks in mixed jobs. Mixed jobs require a qualified Lead Mechanic.";
                    return false;
                }
                return true;

            case WorkshopRoleType.DetailingTechnician:
                if (category == WorkshopJobCategory.Mechanical)
                {
                    reason = "Detailing Technicians are not qualified for mechanical repair jobs. Mechanical jobs require an Automotive Mechanic or Lead Mechanic.";
                    return false;
                }
                if (category == WorkshopJobCategory.Mixed)
                {
                    reason = "Detailing Technicians are not qualified for mechanical repair services in mixed jobs. Mixed jobs require a qualified Lead Mechanic.";
                    return false;
                }
                return true;

            default:
                reason = "The staff member does not have an eligible workshop mechanic role (Lead Mechanic, Automotive Mechanic, or Detailing Technician).";
                return false;
        }
    }

    public static MechanicAssignmentValidation ValidateAssignment(
        DbContext context,
        JobOrder job,
        string? mechanicUserId,
        IEnumerable<JobTask>? tasks = null)
    {
        if (string.IsNullOrWhiteSpace(mechanicUserId))
        {
            return MechanicAssignmentValidation.Failure("Assign an active eligible mechanic before saving assignment.");
        }

        // Resolve staff record and role
        string staffDisplayName;
        string staffRole;
        bool isActive;

        if (context is MasterCrmDbContext legacyDb)
        {
            var staff = legacyDb.Staff.AsNoTracking().FirstOrDefault(s => s.StaffId == mechanicUserId);
            if (staff == null)
            {
                return MechanicAssignmentValidation.Failure(
                    "The assigned mechanic was not found in the staff directory. Assign an active eligible mechanic.");
            }
            if (!staff.IsActive)
            {
                return MechanicAssignmentValidation.Failure(
                    $"The assigned mechanic '{staff.DisplayName}' is inactive. Reassign the job to an active, eligible mechanic.");
            }
            staffDisplayName = staff.DisplayName;
            staffRole = staff.Role;
            isActive = staff.IsActive;
        }
        else if (context is TenantCrmDbContext tenantDb)
        {
            if (tenantDb.StaffValidator != null)
            {
                var val = tenantDb.StaffValidator(mechanicUserId);
                if (val == null || !val.Value.Exists)
                {
                    return MechanicAssignmentValidation.Failure(
                        "The assigned mechanic was not found in the tenant staff directory. Assign an active eligible mechanic.");
                }
                if (!val.Value.IsActive)
                {
                    return MechanicAssignmentValidation.Failure(
                        "The assigned mechanic is inactive in this tenant. Reassign the job to an active, eligible mechanic.");
                }
                staffDisplayName = mechanicUserId;
                staffRole = val.Value.Role;
                isActive = val.Value.IsActive;
            }
            else
            {
                string tenantId = tenantDb.DatabaseIdentity.AsNoTracking().Select(d => d.TenantId).FirstOrDefault() ?? "tenant-a";
                try
                {
                    string conn = Environment.GetEnvironmentVariable("REVCARE_PLATFORM_MASTER")
                        ?? "Server=(localdb)\\MSSQLLocalDB;Database=RevCare_PlatformMaster;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30";
                    using var master = new PlatformMasterDbContext(new DbContextOptionsBuilder<PlatformMasterDbContext>()
                        .UseSqlServer(conn, sql => sql.EnableRetryOnFailure(3)).Options);
                    var membership = master.Memberships.AsNoTracking().FirstOrDefault(m => m.TenantId == tenantId && m.UserId == mechanicUserId);
                    if (membership == null)
                    {
                        return MechanicAssignmentValidation.Failure(
                            "The assigned mechanic does not have active membership in the current tenant. Assign an active eligible mechanic.");
                    }
                    if (!membership.IsActive)
                    {
                        return MechanicAssignmentValidation.Failure(
                            "The assigned mechanic is inactive in this tenant. Reassign the job to an active, eligible mechanic.");
                    }
                    staffDisplayName = membership.DisplayName ?? mechanicUserId;
                    staffRole = membership.Role;
                    isActive = membership.IsActive;
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch
                {
                    var localProfile = tenantDb.Staff.AsNoTracking().FirstOrDefault(s => s.UserId == mechanicUserId);
                    if (localProfile == null)
                    {
                        return MechanicAssignmentValidation.Failure(
                            "The assigned mechanic was not found in the tenant staff directory. Assign an active eligible mechanic.");
                    }
                    staffDisplayName = localProfile.DisplayName;
                    staffRole = "AutomotiveMechanic"; // Fallback default
                    isActive = true;
                }
            }
        }
        else
        {
            return MechanicAssignmentValidation.Failure("Unsupported database context for staff validation.");
        }

        // Validate Role Eligibility
        var roleType = NormalizeRole(staffRole);
        if (!IsEligibleWorkshopRole(roleType))
        {
            return MechanicAssignmentValidation.Failure(
                $"The assigned staff member '{staffDisplayName}' has role '{staffRole}', which is not an eligible workshop mechanic role. Only Lead Mechanic, Automotive Mechanic, and Detailing Technician may be assigned.");
        }

        // Validate Service Category & Specialty Compatibility
        IEnumerable<JobTask> jobTasks = tasks ?? job.Tasks;
        if ((jobTasks == null || !jobTasks.Any()) && job.JobOrderId > 0)
        {
            jobTasks = context.Set<JobTask>().AsNoTracking().Where(t => t.JobOrderId == job.JobOrderId).ToList();
        }

        var category = DetermineJobCategory(job, jobTasks);
        if (!IsRoleCompatibleWithJob(roleType, category, out string? specialtyError))
        {
            return MechanicAssignmentValidation.Failure(
                $"Incompatible workshop assignment: {specialtyError} (Assigned: '{staffDisplayName}', Role: '{staffRole}').");
        }

        return MechanicAssignmentValidation.Success;
    }
}
