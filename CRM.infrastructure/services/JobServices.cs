using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public static class JobServices
{
    public static readonly string[] Offered =
    [
        "Oil and filter change", "Brake inspection", "Wheel alignment", "Exterior detailing",
        "Air-conditioning diagnosis", "Battery replacement & check", "Engine tune-up & spark plugs",
        "Suspension & shock absorbers", "Transmission fluid service", "Tire replacement & balancing"
    ];

    public static readonly (string Name, decimal Price)[] Catalog =
    [
        ("Oil and filter change", 1500.00m),
        ("Brake inspection", 800.00m),
        ("Wheel alignment", 1200.00m),
        ("Exterior detailing", 2500.00m),
        ("Air-conditioning diagnosis", 1000.00m),
        ("Battery replacement & check", 3500.00m),
        ("Engine tune-up & spark plugs", 2800.00m),
        ("Suspension & shock absorbers", 3200.00m),
        ("Transmission fluid service", 2200.00m),
        ("Tire replacement & balancing", 1600.00m)
    ];

    public static readonly string[] OfferedWithPrices = Catalog
        .Select(c => $"{c.Name} — ₱{c.Price:N2}")
        .ToArray();

    public static decimal GetServicePrice(string serviceName)
    {
        var clean = CleanServiceName(serviceName);
        var item = Catalog.FirstOrDefault(c => c.Name.Equals(clean, StringComparison.OrdinalIgnoreCase));
        return item != default ? item.Price : 1000.00m;
    }

    public static string FormatServiceWithPrice(string serviceName)
    {
        var clean = CleanServiceName(serviceName);
        decimal price = GetServicePrice(clean);
        return $"{clean} — ₱{price:N2}";
    }

    public static string CleanServiceName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        string s = input.Trim();
        int dashIdx = s.IndexOf(" — ₱", StringComparison.Ordinal);
        if (dashIdx > 0) return s.Substring(0, dashIdx).Trim();
        int altDash = s.IndexOf(" - ₱", StringComparison.Ordinal);
        if (altDash > 0) return s.Substring(0, altDash).Trim();
        int simpleDash = s.IndexOf(" — PHP", StringComparison.OrdinalIgnoreCase);
        if (simpleDash > 0) return s.Substring(0, simpleDash).Trim();
        return s;
    }

    public static decimal CalculateTotal(IEnumerable<string> services)
    {
        if (services == null) return 0m;
        return services.Sum(s => GetServicePrice(s));
    }

    public static void Assign(DbContext db, JobOrder job, IEnumerable<string> requested)
    {
        var selected = requested
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => CleanServiceName(s))
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var existing = job.JobOrderId == 0 ? new List<JobTask>() : db.Set<JobTask>().AsNoTracking().Where(t => t.JobOrderId == job.JobOrderId).ToList();
        if (existing.Any(t => !selected.Contains(t.ServiceId, StringComparer.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Existing tasks cannot be removed by unchecking a service. Their work history must be preserved.");
        if (selected.Length == 0) throw new InvalidOperationException("Select at least one requested service.");

        foreach (var service in selected.Where(s => !existing.Any(t => t.ServiceId.Equals(s, StringComparison.OrdinalIgnoreCase))))
        {
            if (db is TenantCrmDbContext tenant ? !tenant.Services.Any(s => s.Name == service && s.IsActive) : !Offered.Contains(service)) throw new InvalidOperationException("Select a service from the offered list.");
            if (job.Status is "QA Checking" or "Completed" or "Released")
                throw new InvalidOperationException("New services can only be added while the job is In Queue or In Progress.");
            if (!job.Tasks.Any(t => t.ServiceId.Equals(service, StringComparison.OrdinalIgnoreCase)))
            {
                var task = new JobTask { ServiceId = service, Status = "Pending" };
                job.Tasks.Add(task);
                if (db is TenantCrmDbContext tenantDb)
                {
                    var offering = tenantDb.Services.FirstOrDefault(s => s.Name == service && s.IsActive);
                    decimal refPrice = offering?.Price ?? GetServicePrice(service);
                    tenantDb.TaskWork.Add(new TaskWork { Task = task, ServiceNameAtApproval = service, ReferencePriceAtApproval = refPrice });
                    if (!string.IsNullOrWhiteSpace(job.AssignedMechanicUserId))
                    {
                        tenantDb.TaskAssignments.Add(new TaskAssignment { Task = task, UserId = job.AssignedMechanicUserId });
                    }
                }
            }
        }
    }
}
