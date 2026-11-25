using System.Security.Claims;

namespace CRM.api;

public static class TenantPermissions
{
    public static readonly string[] Roles = ["OwnerManager", "BranchManager", "ServiceAdvisor", "LeadMechanic", "AutomotiveMechanic", "DetailingTechnician"];

    private static readonly Dictionary<string, string[]> Grants = new()
    {
        ["OwnerManager"] = ["Customers.Manage", "Collection.Manage", "Jobs.Manage", "Jobs.Delete", "Jobs.Start", "Jobs.QA", "Jobs.Release", "Tasks.Read", "Tasks.Update", "Tasks.Assign", "Workshop.Read", "Payments.Read", "Payments.Record", "Payments.Correct", "Reports.Read", "Staff.Manage", "Services.Manage", "Services.Read", "Audit.Read"],
        ["BranchManager"] = ["Customers.Manage", "Collection.Manage", "Jobs.Manage", "Jobs.Start", "Jobs.QA", "Jobs.Release", "Tasks.Read", "Tasks.Update", "Tasks.Assign", "Workshop.Read", "Payments.Read", "Payments.Record", "Services.Read", "Audit.Read"],
        ["ServiceAdvisor"] = ["Customers.Manage", "Collection.Manage", "Jobs.Manage", "Jobs.Start", "Jobs.Release", "Tasks.Read", "Workshop.Read", "Payments.Read", "Payments.Record", "Services.Read"],
        ["LeadMechanic"] = ["Workshop.Read", "Tasks.Read", "Tasks.Update", "Tasks.Assign", "Jobs.Start", "Jobs.QA", "Services.Read"],
        ["AutomotiveMechanic"] = ["Workshop.Read", "Tasks.UpdateAssigned", "Services.Read"],
        ["DetailingTechnician"] = ["Workshop.Read", "Tasks.UpdateAssigned", "Services.Read"]
    };

    public static string ExtractBaseRole(string? roleWithBranch)
    {
        if (string.IsNullOrWhiteSpace(roleWithBranch)) return "";
        int colon = roleWithBranch.IndexOf(':');
        return colon >= 0 ? roleWithBranch[..colon].Trim() : roleWithBranch.Trim();
    }

    public static string? ExtractBranchCode(string? roleWithBranch)
    {
        if (string.IsNullOrWhiteSpace(roleWithBranch)) return null;
        int colon = roleWithBranch.IndexOf(':');
        if (colon < 0 || colon >= roleWithBranch.Length - 1) return null;
        var code = roleWithBranch[(colon + 1)..].Trim();
        return string.IsNullOrEmpty(code) ? null : code;
    }

    public static int? GetUserBranchId(ClaimsPrincipal user)
    {
        var val = user.FindFirstValue("branch_id");
        return int.TryParse(val, out int bid) ? bid : null;
    }

    public static string[] ForRole(string role) => Grants.GetValueOrDefault(ExtractBaseRole(role)) ?? [];
    public static bool Has(ClaimsPrincipal user, string permission) => ForRole(user.FindFirstValue("tenant_role") ?? "").Contains(permission);
    public static bool Technician(ClaimsPrincipal user) => Has(user, "Tasks.UpdateAssigned");

    public static string? RequiredModule(HttpContext http)
    {
        var path = http.Request.Path.Value?.Trim('/').Split('/') ?? [];
        if (path.Length < 2 || path[0] != "crm") return null;

        return path[1] switch
        {
            "customers" => "Customers",
            "vehicles" => "Customers",
            "inquiries" => "Inquiries",
            "complaints" => "Complaints",
            "feedbacks" => "Feedback",
            "appointments" => "Inquiries",
            "insurance" => "Payments",
            "follow-ups" => "Customers",
            "job-orders" => "JobOrders",
            "estimates" => "JobOrders",
            "terms" => "JobOrders",
            "reports" => "Payments",
            "job-tasks" or "workshop" or "assignees" => "JobTasks",
            "payments" => "Payments",
            "services" => "Services",
            "branches" => "Branching",
            "analytics" or "bi" => "BusinessIntelligence",
            _ => null
        };
    }

    // Deny unknown routes. These rules also apply when clients bypass the WinForms menus.
    public static bool Allows(HttpContext http, string role)
    {
        var path = http.Request.Path.Value?.Trim('/').Split('/') ?? [];
        if (path.Length < 2 || path[0] != "crm") return false;
        string method = http.Request.Method;
        string baseRole = ExtractBaseRole(role);

        if (path[1] is "modules" or "branches") return true; // Metadata about tenant modules is accessible to all tenant staff

        string? permission = path[1] switch
        {
            "customers" => "Customers.Manage",
            "vehicles" => "Customers.Manage",
            "inquiries" or "complaints" or "feedbacks" => "Collection.Manage",
            "appointments" => "Collection.Manage",
            "estimates" => "Jobs.Manage",
            "insurance" => method == "GET" ? "Payments.Read" : "Payments.Record",
            "follow-ups" => "Customers.Manage",
            "terms" => method == "GET" ? "Workshop.Read" : "Jobs.Manage",
            "reports" or "analytics" or "bi" => "Reports.Read",
            "job-orders" when path.Length == 4 && path[3] == "assignment" => "Tasks.Assign",
            "job-orders" when path.Length == 4 && path[3] == "status" => "Workshop.Read",
            "job-orders" when path.Length == 4 && path[3] == "terms-acceptance" => "Jobs.Manage",
            "job-orders" when path.Length == 4 && path[3] == "terms-status" => "Workshop.Read",
            "job-orders" => method == "DELETE" ? "Jobs.Delete" : "Jobs.Manage",
            "job-tasks" when path.Length == 4 && path[3] == "assignment" => "Tasks.Assign",
            "job-tasks" when path.Length == 4 && path[3] == "review" => "Jobs.QA",
            "job-tasks" when path.Length == 4 && path[3] is "submit" or "reviews" => baseRole is "AutomotiveMechanic" or "DetailingTechnician" ? "Tasks.UpdateAssigned" : "Tasks.Update",
            "job-tasks" when method == "PUT" => baseRole is "AutomotiveMechanic" or "DetailingTechnician" ? "Tasks.UpdateAssigned" : "Tasks.Update",
            "job-tasks" => "Tasks.Read",
            "workshop" when path.Length == 3 && path[2] == "assignment-history" => "Tasks.Assign",
            "workshop" => "Workshop.Read",
            "payments" => method == "GET" ? "Payments.Read" : method == "POST" && path.Length == 2 ? "Payments.Record" : "Payments.Correct",
            "staff" => "Staff.Manage",
            "assignees" => "Tasks.Assign",
            "services" => method == "GET" ? "Services.Read" : "Services.Manage",
            "audit" => "Audit.Read",
            _ => null
        };
        return permission != null && ForRole(baseRole).Contains(permission);
    }
}
