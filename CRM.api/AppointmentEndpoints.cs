#nullable enable
using System.Security.Claims;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api;

public static class AppointmentEndpoints
{
    public static void MapAppointmentEndpoints(this WebApplication app)
    {
        var crm = app.MapGroup("/crm/appointments").RequireAuthorization("TenantOperations");

        crm.MapGet("", async (TenantCrmDbContext db) =>
        {
            var appts = await db.Appointments.AsNoTracking()
                .Include(a => a.Customer)
                .Include(a => a.Vehicle)
                .OrderByDescending(a => a.ScheduledStart)
                .Select(a => new
                {
                    a.AppointmentId,
                    a.CustomerId,
                    CustomerName = a.Customer != null ? a.Customer.FullName : "",
                    CustomerContact = a.Customer != null ? a.Customer.ContactNumber : "",
                    a.VehicleId,
                    VehicleDisplay = a.Vehicle != null ? a.Vehicle.DisplayName : "",
                    a.ScheduledStart,
                    a.Status,
                    a.RequestedServices,
                    a.Notes,
                    a.LinkedJobOrderId,
                    a.CancellationReason,
                    a.CancelledBy,
                    a.CancelledAt,
                    a.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(appts);
        });

        crm.MapGet("/{id:int}", async (int id, TenantCrmDbContext db) =>
        {
            var appt = await db.Appointments.AsNoTracking()
                .Include(a => a.Customer)
                .Include(a => a.Vehicle)
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appt == null) return Results.NotFound();

            return Results.Ok(new
            {
                appt.AppointmentId,
                appt.CustomerId,
                CustomerName = appt.Customer?.FullName ?? "",
                CustomerContact = appt.Customer?.ContactNumber ?? "",
                appt.VehicleId,
                VehicleDisplay = appt.Vehicle?.DisplayName ?? "",
                appt.ScheduledStart,
                appt.Status,
                appt.RequestedServices,
                appt.Notes,
                appt.LinkedJobOrderId,
                appt.CancellationReason,
                appt.CancelledBy,
                appt.CancelledAt,
                appt.CreatedAt
            });
        });

        crm.MapPost("", async (AppointmentCreate request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (request.CustomerId <= 0)
                return Results.BadRequest(new { error = "Valid CustomerId is required." });

            var customer = await db.Customers.FindAsync(request.CustomerId);
            if (customer == null || !customer.IsActive)
                return Results.BadRequest(new { error = "Active customer not found." });

            if (request.ScheduledStart < DateTime.UtcNow.AddMinutes(-30))
                return Results.BadRequest(new { error = "Scheduled start must not be in the past." });

            var appt = new Appointment
            {
                CustomerId = request.CustomerId,
                VehicleId = request.VehicleId > 0 ? request.VehicleId : null,
                ScheduledStart = request.ScheduledStart,
                RequestedServices = request.RequestedServices?.Trim() ?? "",
                Notes = request.Notes?.Trim() ?? "",
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow
            };

            db.Appointments.Add(appt);
            await db.SaveChangesAsync();

            db.AppointmentHistories.Add(new AppointmentHistory
            {
                AppointmentId = appt.AppointmentId,
                Action = "Created",
                NewScheduledStart = appt.ScheduledStart,
                Reason = "Appointment scheduled",
                Actor = user.Identity?.Name ?? db.AuditActor,
                OccurredAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            return Results.Created($"/crm/appointments/{appt.AppointmentId}", new
            {
                appt.AppointmentId,
                appt.CustomerId,
                appt.VehicleId,
                appt.ScheduledStart,
                appt.RequestedServices,
                appt.Notes,
                appt.Status,
                appt.CreatedAt
            });
        });

        crm.MapPut("/{id:int}", async (int id, AppointmentUpdate request, TenantCrmDbContext db) =>
        {
            var appt = await db.Appointments.FindAsync(id);
            if (appt == null) return Results.NotFound();

            if (appt.Status != "Scheduled")
                return Results.BadRequest(new { error = $"Cannot modify a {appt.Status} appointment." });

            if (request.VehicleId.HasValue && request.VehicleId.Value > 0)
                appt.VehicleId = request.VehicleId.Value;

            if (request.RequestedServices != null)
                appt.RequestedServices = request.RequestedServices.Trim();

            if (request.Notes != null)
                appt.Notes = request.Notes.Trim();

            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                appt.AppointmentId,
                appt.CustomerId,
                appt.VehicleId,
                appt.ScheduledStart,
                appt.RequestedServices,
                appt.Notes,
                appt.Status
            });
        });

        crm.MapPost("/{id:int}/reschedule", async (int id, AppointmentReschedule request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            try
            {
                string actor = user.Identity?.Name ?? db.AuditActor;
                AppointmentService.Reschedule(db, id, request.NewScheduledStart, request.Reason, actor);
                var updated = await db.Appointments.FindAsync(id);
                return Results.Ok(new { message = "Appointment rescheduled.", appointmentId = id, status = updated?.Status });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        crm.MapPost("/{id:int}/cancel", async (int id, AppointmentCancel request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            try
            {
                string actor = user.Identity?.Name ?? db.AuditActor;
                AppointmentService.Cancel(db, id, request.ReasonCategory, request.Explanation ?? "", actor);
                var updated = await db.Appointments.FindAsync(id);
                return Results.Ok(new { message = "Appointment cancelled.", appointmentId = id, status = updated?.Status });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        crm.MapPost("/{id:int}/no-show", async (int id, AppointmentNoShow request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            try
            {
                string actor = user.Identity?.Name ?? db.AuditActor;
                AppointmentService.MarkNoShow(db, id, actor, request.Reason ?? "Customer did not arrive for scheduled appointment");
                var updated = await db.Appointments.FindAsync(id);
                return Results.Ok(new { message = "Appointment marked as No Show.", appointmentId = id, status = updated?.Status });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        crm.MapPost("/{id:int}/check-in", async (int id, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var strategy = db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                var target = await db.Appointments
                    .FromSqlInterpolated($"SELECT * FROM Appointments WITH (UPDLOCK, HOLDLOCK) WHERE AppointmentId = {id}")
                    .Include(a => a.Vehicle)
                    .FirstOrDefaultAsync();

                if (target == null) return Results.NotFound(new { error = "Appointment not found." });
                if (target.Status != "Scheduled")
                    return Results.BadRequest(new { error = $"Appointment is in '{target.Status}' status. Only Scheduled appointments can be checked in." });
                if (target.LinkedJobOrderId.HasValue)
                    return Results.Conflict(new { error = $"Job Order #{target.LinkedJobOrderId.Value} was already created for this appointment." });

                // Workflow Consistency: check if a queued Job Order already exists for this visit/customer/vehicle
                JobOrder? existingJob = null;
                if (target.CustomerId.HasValue && target.VehicleId.HasValue)
                {
                    existingJob = await db.JobOrders
                        .Include(j => j.Tasks)
                        .Where(j => j.CustomerId == target.CustomerId.Value &&
                                    j.VehicleId == target.VehicleId.Value &&
                                    j.Status == "In Queue")
                        .OrderByDescending(j => j.JobOrderId)
                        .FirstOrDefaultAsync();
                }

                JobOrder job;
                if (existingJob != null)
                {
                    job = existingJob;
                    job.InquiryNotes = $"{job.InquiryNotes} | Checked in from Appointment #{target.AppointmentId}".Trim();

                    if (!string.IsNullOrWhiteSpace(target.RequestedServices))
                    {
                        var services = target.RequestedServices.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var s in services)
                        {
                            if (!job.Tasks.Any(t => string.Equals(t.ServiceId, s, StringComparison.OrdinalIgnoreCase)))
                            {
                                job.Tasks.Add(new JobTask { ServiceId = s, Status = "Pending" });
                            }
                        }
                    }
                }
                else
                {
                    job = new JobOrder
                    {
                        CustomerId = target.CustomerId,
                        VehicleId = target.VehicleId,
                        VehicleDetails = target.Vehicle?.DisplayName ?? "",
                        InquiryNotes = $"Created from Appointment #{target.AppointmentId}. {target.Notes}".Trim(),
                        Status = "In Queue",
                        StartedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    if (!string.IsNullOrWhiteSpace(target.RequestedServices))
                    {
                        var services = target.RequestedServices.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (services.Length > 0)
                        {
                            decimal total = 0m;
                            foreach (var s in services)
                            {
                                job.Tasks.Add(new JobTask { ServiceId = s, Status = "Pending" });
                                var svc = await db.Services.FirstOrDefaultAsync(x => x.Name == s);
                                if (svc != null) total += svc.Price;
                            }
                            job.TotalEstimatedCost = total;
                        }
                    }

                    db.JobOrders.Add(job);
                }

                await db.SaveChangesAsync();

                target.Status = "Checked In";
                target.LinkedJobOrderId = job.JobOrderId;

                db.AppointmentHistories.Add(new AppointmentHistory
                {
                    AppointmentId = target.AppointmentId,
                    Action = "Checked In",
                    Reason = $"Checked in by {user.Identity?.Name ?? "Staff"}; created Job Order #{job.JobOrderId}",
                    Actor = user.Identity?.Name ?? db.AuditActor,
                    OccurredAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new
                {
                    appointmentId = target.AppointmentId,
                    jobOrderId = job.JobOrderId,
                    status = target.Status,
                    message = $"Appointment checked in successfully. Job Order #{job.JobOrderId} created."
                });
            });
        });

        crm.MapGet("/{id:int}/history", async (int id, TenantCrmDbContext db) =>
        {
            var history = await db.AppointmentHistories.AsNoTracking()
                .Where(h => h.AppointmentId == id)
                .OrderByDescending(h => h.OccurredAtUtc)
                .ToListAsync();

            return Results.Ok(history);
        });

        crm.MapGet("/metrics", (TenantCrmDbContext db) =>
        {
            var (total, scheduled, checkedIn, cancelled, noShow, cancelRate, noShowRate) = AppointmentService.GetAppointmentMetrics(db);
            return Results.Ok(new
            {
                total,
                scheduled,
                checkedIn,
                cancelled,
                noShow,
                cancellationRate = Math.Round(cancelRate, 1),
                noShowRate = Math.Round(noShowRate, 1)
            });
        });
    }
}

public sealed record AppointmentCreate(int CustomerId, int? VehicleId, DateTime ScheduledStart, string? RequestedServices, string? Notes);
public sealed record AppointmentUpdate(int? VehicleId, string? RequestedServices, string? Notes);
public sealed record AppointmentReschedule(DateTime NewScheduledStart, string Reason);
public sealed record AppointmentCancel(string ReasonCategory, string? Explanation = null);
public sealed record AppointmentNoShow(string? Reason = null);
