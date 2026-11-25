#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public static class AppointmentService
{
    public static readonly string[] CancellationReasons =
    [
        "Customer requested cancellation",
        "Customer unavailable",
        "Scheduling conflict",
        "Vehicle unavailable",
        "Workshop unavailable",
        "Duplicate appointment",
        "Customer declined service",
        "Other"
    ];

    public static void Reschedule(
        MasterCrmDbContext db,
        int appointmentId,
        DateTime newScheduledStart,
        string reason,
        string actor)
    {
        var appt = db.Appointments.FromSqlInterpolated($"SELECT * FROM Appointments WITH (UPDLOCK,HOLDLOCK) WHERE AppointmentId = {appointmentId}").FirstOrDefault()
            ?? db.Appointments.Find(appointmentId)
            ?? throw new InvalidOperationException($"Appointment #{appointmentId} not found.");

        if (appt.Status != "Scheduled")
            throw new InvalidOperationException($"Cannot reschedule a {appt.Status} appointment. Only Scheduled appointments can be rescheduled.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 4)
            throw new InvalidOperationException("A rescheduling reason of at least 4 characters is required.");

        DateTime previousStart = appt.ScheduledStart;
        appt.ScheduledStart = newScheduledStart;

        var history = new AppointmentHistory
        {
            AppointmentId = appointmentId,
            Action = "Rescheduled",
            PreviousScheduledStart = previousStart,
            NewScheduledStart = newScheduledStart,
            Reason = reason.Trim(),
            Actor = string.IsNullOrWhiteSpace(actor) ? db.AuditActor : actor.Trim(),
            OccurredAtUtc = DateTime.UtcNow
        };
        db.AppointmentHistories.Add(history);

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = history.Actor,
            EntityType = "Appointment",
            Action = "Rescheduled",
            Changes = System.Text.Json.JsonSerializer.Serialize(new
            {
                appointmentId,
                PreviousStart = previousStart,
                NewStart = newScheduledStart,
                Reason = history.Reason
            })
        });

        db.SaveChanges();
    }

    public static void Cancel(
        MasterCrmDbContext db,
        int appointmentId,
        string reasonCategory,
        string explanation,
        string actor)
    {
        var appt = db.Appointments.FromSqlInterpolated($"SELECT * FROM Appointments WITH (UPDLOCK,HOLDLOCK) WHERE AppointmentId = {appointmentId}").FirstOrDefault()
            ?? db.Appointments.Find(appointmentId)
            ?? throw new InvalidOperationException($"Appointment #{appointmentId} not found.");

        if (appt.Status != "Scheduled")
            throw new InvalidOperationException($"Cannot cancel a {appt.Status} appointment. Only Scheduled appointments can be cancelled.");

        if (appt.LinkedJobOrderId.HasValue)
        {
            throw new InvalidOperationException(
                $"A Job Order ({RecordReference.JobOrder(appt.LinkedJobOrderId.Value)}) has already been created for this appointment. Use the Job Order Cancellation workflow to review completed work and financial obligations.");
        }

        string fullReason = string.IsNullOrWhiteSpace(explanation)
            ? reasonCategory
            : $"{reasonCategory}: {explanation.Trim()}";

        if (fullReason.Trim().Length < 4)
            throw new InvalidOperationException("A cancellation reason of at least 4 characters is required.");

        appt.Status = "Cancelled";
        appt.CancellationReason = fullReason.Trim();
        appt.CancelledBy = string.IsNullOrWhiteSpace(actor) ? db.AuditActor : actor.Trim();
        appt.CancelledAt = DateTime.UtcNow;

        var history = new AppointmentHistory
        {
            AppointmentId = appointmentId,
            Action = "Cancelled",
            Reason = fullReason.Trim(),
            Actor = appt.CancelledBy,
            OccurredAtUtc = DateTime.UtcNow
        };
        db.AppointmentHistories.Add(history);

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = appt.CancelledBy,
            EntityType = "Appointment",
            Action = "Cancelled",
            Changes = System.Text.Json.JsonSerializer.Serialize(new
            {
                appointmentId,
                Reason = fullReason,
                CancelledBy = appt.CancelledBy
            })
        });

        db.SaveChanges();
    }

    public static void MarkNoShow(
        MasterCrmDbContext db,
        int appointmentId,
        string actor,
        string reason = "Customer did not arrive for scheduled appointment")
    {
        var appt = db.Appointments.FromSqlInterpolated($"SELECT * FROM Appointments WITH (UPDLOCK,HOLDLOCK) WHERE AppointmentId = {appointmentId}").FirstOrDefault()
            ?? db.Appointments.Find(appointmentId)
            ?? throw new InvalidOperationException($"Appointment #{appointmentId} not found.");

        if (appt.Status != "Scheduled")
            throw new InvalidOperationException($"Cannot mark a {appt.Status} appointment as No Show.");

        appt.Status = "No Show";

        var history = new AppointmentHistory
        {
            AppointmentId = appointmentId,
            Action = "No Show",
            Reason = reason,
            Actor = string.IsNullOrWhiteSpace(actor) ? db.AuditActor : actor.Trim(),
            OccurredAtUtc = DateTime.UtcNow
        };
        db.AppointmentHistories.Add(history);

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = history.Actor,
            EntityType = "Appointment",
            Action = "NoShow",
            Changes = System.Text.Json.JsonSerializer.Serialize(new { appointmentId, Reason = reason })
        });

        db.SaveChanges();
    }

    // --- Business Intelligence & Retention Metrics ---
    public static (int Total, int Scheduled, int CheckedIn, int Cancelled, int NoShow, double CancellationRate, double NoShowRate) GetAppointmentMetrics(MasterCrmDbContext db)
    {
        var appts = db.Appointments.AsNoTracking().ToList();
        int total = appts.Count;
        if (total == 0) return (0, 0, 0, 0, 0, 0, 0);

        int scheduled = appts.Count(a => a.Status == "Scheduled");
        int checkedIn = appts.Count(a => a.Status == "Checked In");
        int cancelled = appts.Count(a => a.Status == "Cancelled");
        int noShow = appts.Count(a => a.Status == "No Show");

        double cancelRate = (double)cancelled / total * 100.0;
        double noShowRate = (double)noShow / total * 100.0;

        return (total, scheduled, checkedIn, cancelled, noShow, cancelRate, noShowRate);
    }

    // --- TenantCrmDbContext Overloads ---
    public static void Reschedule(
        TenantCrmDbContext db,
        int appointmentId,
        DateTime newScheduledStart,
        string reason,
        string actor)
    {
        var appt = db.Appointments.FromSqlInterpolated($"SELECT * FROM Appointments WITH (UPDLOCK,HOLDLOCK) WHERE AppointmentId = {appointmentId}").FirstOrDefault()
            ?? db.Appointments.Find(appointmentId)
            ?? throw new InvalidOperationException($"Appointment #{appointmentId} not found.");

        if (appt.Status != "Scheduled")
            throw new InvalidOperationException($"Cannot reschedule a {appt.Status} appointment. Only Scheduled appointments can be rescheduled.");

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 4)
            throw new InvalidOperationException("A rescheduling reason of at least 4 characters is required.");

        DateTime previousStart = appt.ScheduledStart;
        appt.ScheduledStart = newScheduledStart;

        var history = new AppointmentHistory
        {
            AppointmentId = appointmentId,
            Action = "Rescheduled",
            PreviousScheduledStart = previousStart,
            NewScheduledStart = newScheduledStart,
            Reason = reason.Trim(),
            Actor = string.IsNullOrWhiteSpace(actor) ? db.AuditActor : actor.Trim(),
            OccurredAtUtc = DateTime.UtcNow
        };
        db.AppointmentHistories.Add(history);

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = history.Actor,
            EntityType = "Appointment",
            Action = "Rescheduled",
            Changes = System.Text.Json.JsonSerializer.Serialize(new
            {
                appointmentId,
                PreviousStart = previousStart,
                NewStart = newScheduledStart,
                Reason = history.Reason
            })
        });

        db.SaveChanges();
    }

    public static void Cancel(
        TenantCrmDbContext db,
        int appointmentId,
        string reasonCategory,
        string explanation,
        string actor)
    {
        var appt = db.Appointments.FromSqlInterpolated($"SELECT * FROM Appointments WITH (UPDLOCK,HOLDLOCK) WHERE AppointmentId = {appointmentId}").FirstOrDefault()
            ?? db.Appointments.Find(appointmentId)
            ?? throw new InvalidOperationException($"Appointment #{appointmentId} not found.");

        if (appt.Status != "Scheduled")
            throw new InvalidOperationException($"Cannot cancel a {appt.Status} appointment. Only Scheduled appointments can be cancelled.");

        if (appt.LinkedJobOrderId.HasValue)
        {
            throw new InvalidOperationException(
                $"A Job Order ({RecordReference.JobOrder(appt.LinkedJobOrderId.Value)}) has already been created for this appointment. Use the Job Order Cancellation workflow to review completed work and financial obligations.");
        }

        string fullReason = string.IsNullOrWhiteSpace(explanation)
            ? reasonCategory
            : $"{reasonCategory}: {explanation.Trim()}";

        if (fullReason.Trim().Length < 4)
            throw new InvalidOperationException("A cancellation reason of at least 4 characters is required.");

        appt.Status = "Cancelled";
        appt.CancellationReason = fullReason.Trim();
        appt.CancelledBy = string.IsNullOrWhiteSpace(actor) ? db.AuditActor : actor.Trim();
        appt.CancelledAt = DateTime.UtcNow;

        var history = new AppointmentHistory
        {
            AppointmentId = appointmentId,
            Action = "Cancelled",
            Reason = fullReason.Trim(),
            Actor = appt.CancelledBy,
            OccurredAtUtc = DateTime.UtcNow
        };
        db.AppointmentHistories.Add(history);

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = appt.CancelledBy,
            EntityType = "Appointment",
            Action = "Cancelled",
            Changes = System.Text.Json.JsonSerializer.Serialize(new
            {
                appointmentId,
                Reason = fullReason,
                CancelledBy = appt.CancelledBy
            })
        });

        db.SaveChanges();
    }

    public static void MarkNoShow(
        TenantCrmDbContext db,
        int appointmentId,
        string actor,
        string reason = "Customer did not arrive for scheduled appointment")
    {
        var appt = db.Appointments.FromSqlInterpolated($"SELECT * FROM Appointments WITH (UPDLOCK,HOLDLOCK) WHERE AppointmentId = {appointmentId}").FirstOrDefault()
            ?? db.Appointments.Find(appointmentId)
            ?? throw new InvalidOperationException($"Appointment #{appointmentId} not found.");

        if (appt.Status != "Scheduled")
            throw new InvalidOperationException($"Cannot mark a {appt.Status} appointment as No Show.");

        appt.Status = "No Show";

        var history = new AppointmentHistory
        {
            AppointmentId = appointmentId,
            Action = "No Show",
            Reason = reason,
            Actor = string.IsNullOrWhiteSpace(actor) ? db.AuditActor : actor.Trim(),
            OccurredAtUtc = DateTime.UtcNow
        };
        db.AppointmentHistories.Add(history);

        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Actor = history.Actor,
            EntityType = "Appointment",
            Action = "NoShow",
            Changes = System.Text.Json.JsonSerializer.Serialize(new { appointmentId, Reason = reason })
        });

        db.SaveChanges();
    }

    public static (int Total, int Scheduled, int CheckedIn, int Cancelled, int NoShow, double CancellationRate, double NoShowRate) GetAppointmentMetrics(TenantCrmDbContext db)
    {
        var appts = db.Appointments.AsNoTracking().ToList();
        int total = appts.Count;
        if (total == 0) return (0, 0, 0, 0, 0, 0, 0);

        int scheduled = appts.Count(a => a.Status == "Scheduled");
        int checkedIn = appts.Count(a => a.Status == "Checked In");
        int cancelled = appts.Count(a => a.Status == "Cancelled");
        int noShow = appts.Count(a => a.Status == "No Show");

        double cancelRate = (double)cancelled / total * 100.0;
        double noShowRate = (double)noShow / total * 100.0;

        return (total, scheduled, checkedIn, cancelled, noShow, cancelRate, noShowRate);
    }
}
