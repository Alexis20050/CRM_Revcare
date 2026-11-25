#nullable enable
using System;

namespace crm.domain.entities;

public class AppointmentHistory
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    // Action: Scheduled, Rescheduled, Cancelled, No Show, Checked In
    public string Action { get; set; } = "Rescheduled";
    public DateTime? PreviousScheduledStart { get; set; }
    public DateTime? NewScheduledStart { get; set; }
    public string Reason { get; set; } = "";
    public string Actor { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
