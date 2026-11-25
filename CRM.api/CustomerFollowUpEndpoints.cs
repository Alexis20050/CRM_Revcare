using System.Security.Claims;
using System.Text.Json;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api;

public static class CustomerFollowUpEndpoints
{
    public static void MapCustomerFollowUpEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/crm/follow-ups").RequireAuthorization("TenantOperations");

        // List follow-ups
        group.MapGet("", async (int? customerId, int? jobOrderId, string? status, TenantCrmDbContext db) =>
        {
            var entries = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "CustomerFollowUp")
                .OrderBy(a => a.AuditEntryId)
                .ToListAsync();

            var followUps = ConsolidateFollowUps(entries);

            if (customerId.HasValue && customerId.Value > 0)
                followUps = followUps.Where(f => f.CustomerId == customerId.Value).ToList();
            if (jobOrderId.HasValue && jobOrderId.Value > 0)
                followUps = followUps.Where(f => f.JobOrderId == jobOrderId.Value).ToList();
            if (!string.IsNullOrWhiteSpace(status))
                followUps = followUps.Where(f => string.Equals(f.Status, status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

            return Results.Ok(followUps);
        });

        // Get follow-up by ID
        group.MapGet("/{id:guid}", async (Guid id, TenantCrmDbContext db) =>
        {
            var entries = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "CustomerFollowUp")
                .OrderBy(a => a.AuditEntryId)
                .ToListAsync();

            var followUps = ConsolidateFollowUps(entries);
            var item = followUps.FirstOrDefault(f => f.FollowUpId == id);
            if (item == null) return Results.NotFound(new { error = $"Follow-up record #{id} not found." });

            return Results.Ok(item);
        });

        // Create new follow-up
        group.MapPost("", async (CreateFollowUpRequest request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (request.CustomerId <= 0)
                return Results.BadRequest(new { error = "Valid CustomerId is required." });
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Results.BadRequest(new { error = "Follow-up reason is required (e.g. Post-Service Check, Warranty Check, Maintenance Reminder)." });

            var customer = await db.Customers.FindAsync(request.CustomerId);
            if (customer == null) return Results.NotFound(new { error = $"Customer #{request.CustomerId} not found." });

            string actorName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? db.AuditActor;

            var followUp = new CustomerFollowUpRecord(
                FollowUpId: Guid.NewGuid(),
                CustomerId: request.CustomerId,
                CustomerName: customer.FullName,
                ContactNumber: customer.ContactNumber,
                VehicleId: request.VehicleId,
                JobOrderId: request.JobOrderId,
                Reason: request.Reason.Trim(),
                ScheduledDate: request.ScheduledDate ?? DateTime.UtcNow.AddDays(3),
                Status: "Pending", // Pending, Contacted, Completed, Declined
                ResponsibleStaff: request.ResponsibleStaff ?? actorName,
                OutcomeNotes: null,
                CompletedAtUtc: null,
                RecordedBy: actorName,
                CreatedAtUtc: DateTime.UtcNow
            );

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actorName,
                EntityType = "CustomerFollowUp",
                Action = "Created",
                Changes = JsonSerializer.Serialize(followUp)
            });

            await db.SaveChangesAsync();
            return Results.Created($"/crm/follow-ups/{followUp.FollowUpId}", followUp);
        });

        // Update follow-up status and outcome
        group.MapPut("/{id:guid}/status", async (Guid id, UpdateFollowUpStatusRequest request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(request.Status))
                return Results.BadRequest(new { error = "Status is required (Contacted, Completed, Declined)." });

            var validStatuses = new[] { "Pending", "Contacted", "Completed", "Declined" };
            if (!validStatuses.Contains(request.Status.Trim(), StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = $"Invalid status. Must be one of: {string.Join(", ", validStatuses)}." });

            var entries = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "CustomerFollowUp")
                .OrderBy(a => a.AuditEntryId)
                .ToListAsync();

            var followUps = ConsolidateFollowUps(entries);
            var item = followUps.FirstOrDefault(f => f.FollowUpId == id);
            if (item == null) return Results.NotFound(new { error = $"Follow-up record #{id} not found." });

            string actorName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? db.AuditActor;

            var updated = item with
            {
                Status = request.Status.Trim(),
                OutcomeNotes = request.OutcomeNotes ?? item.OutcomeNotes,
                CompletedAtUtc = request.Status.Trim().Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                                 request.Status.Trim().Equals("Declined", StringComparison.OrdinalIgnoreCase)
                                 ? (item.CompletedAtUtc ?? DateTime.UtcNow) : null
            };

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actorName,
                EntityType = "CustomerFollowUp",
                Action = "StatusUpdated",
                Changes = JsonSerializer.Serialize(updated)
            });

            await db.SaveChangesAsync();
            return Results.Ok(updated);
        });

        // Customer 360 follow-up endpoint
        app.MapGet("/crm/customers/{id:int}/follow-ups", async (int id, TenantCrmDbContext db) =>
        {
            var entries = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "CustomerFollowUp")
                .OrderBy(a => a.AuditEntryId)
                .ToListAsync();

            var followUps = ConsolidateFollowUps(entries)
                .Where(f => f.CustomerId == id)
                .OrderByDescending(f => f.ScheduledDate)
                .ToList();

            return Results.Ok(followUps);
        }).RequireAuthorization("TenantOperations");
    }

    private static List<CustomerFollowUpRecord> ConsolidateFollowUps(List<AuditEntry> entries)
    {
        var dict = new Dictionary<Guid, CustomerFollowUpRecord>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Changes)) continue;
            try
            {
                var record = JsonSerializer.Deserialize<CustomerFollowUpRecord>(entry.Changes);
                if (record != null && record.FollowUpId != Guid.Empty)
                {
                    dict[record.FollowUpId] = record;
                }
            }
            catch { }
        }
        return dict.Values.OrderByDescending(f => f.ScheduledDate).ToList();
    }
}

public sealed record CustomerFollowUpRecord(
    Guid FollowUpId,
    int CustomerId,
    string CustomerName,
    string ContactNumber,
    int? VehicleId,
    int? JobOrderId,
    string Reason,
    DateTime ScheduledDate,
    string Status,
    string ResponsibleStaff,
    string? OutcomeNotes,
    DateTime? CompletedAtUtc,
    string RecordedBy,
    DateTime CreatedAtUtc
);

public sealed record CreateFollowUpRequest(
    int CustomerId,
    int? VehicleId,
    int? JobOrderId,
    string Reason,
    DateTime? ScheduledDate,
    string? ResponsibleStaff,
    string? Notes
);

public sealed record UpdateFollowUpStatusRequest(
    string Status,
    string? OutcomeNotes
);
