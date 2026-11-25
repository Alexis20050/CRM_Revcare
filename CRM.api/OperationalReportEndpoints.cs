#nullable enable
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api;

public static class OperationalReportEndpoints
{
    public static void MapOperationalReportEndpoints(this WebApplication app)
    {
        var crm = app.MapGroup("/crm/reports").RequireAuthorization("TenantOperations");

        crm.MapGet("/operational-summary", async (TenantCrmDbContext db) =>
        {
            var jobs = await db.JobOrders.AsNoTracking().ToListAsync();
            var tasks = await db.JobTasks.AsNoTracking().ToListAsync();
            var payments = await db.Payments.AsNoTracking().Where(p => !p.IsVoided).ToListAsync();
            var refunds = await db.Refunds.AsNoTracking().ToListAsync();
            var customers = await db.Customers.AsNoTracking().Where(c => c.IsActive).ToListAsync();
            var appts = await db.Appointments.AsNoTracking().ToListAsync();
            var feedbacks = await db.Feedbacks.AsNoTracking().ToListAsync();
            var complaints = await db.Complaints.AsNoTracking().ToListAsync();

            var activeJobs = jobs.Where(j => j.Status != "Released" && j.Status != "Cancelled" && !j.IsArchived).ToList();
            var completedJobs = jobs.Where(j => j.Status is "Completed" or "Released").ToList();

            var stageCounts = jobs
                .Where(j => !j.IsArchived)
                .GroupBy(j => j.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            decimal totalBilled = activeJobs.Sum(j => j.TotalEstimatedCost ?? 0m);
            decimal totalPaid = payments.Sum(p => p.AmountPaid);
            decimal totalRefunded = refunds.Sum(r => r.Amount);
            decimal netCollections = Math.Max(0m, totalPaid - totalRefunded);

            decimal outstandingBalance = 0m;
            foreach (var job in activeJobs)
            {
                decimal cost = job.TotalEstimatedCost ?? 0m;
                decimal jobPaid = payments.Where(p => p.JobOrderId == job.JobOrderId).Sum(p => p.AmountPaid);
                decimal jobRefund = refunds.Where(r => r.JobOrderId == job.JobOrderId).Sum(r => r.Amount);
                decimal net = Math.Max(0m, jobPaid - jobRefund);
                if (cost > net) outstandingBalance += (cost - net);
            }

            int totalTasks = tasks.Count(t => t.Status != "Cancelled");
            int doneTasks = tasks.Count(t => t.Status is "Done" or "Completed");
            decimal taskPct = totalTasks > 0 ? Math.Round((decimal)doneTasks / totalTasks * 100m, 1) : 0m;

            double avgTurnaround = 0.0;
            var turnaroundList = completedJobs.Where(j => j.TurnaroundHours.HasValue).Select(j => j.TurnaroundHours!.Value).ToList();
            if (turnaroundList.Count > 0) avgTurnaround = Math.Round(turnaroundList.Average(), 1);

            double avgCsat = feedbacks.Count > 0 ? Math.Round(feedbacks.Average(f => (double)f.RatingScore), 1) : 5.0;
            int openComplaints = complaints.Count(c => c.ResolutionStatus != "Resolved");

            return Results.Ok(new
            {
                generatedAtUtc = DateTime.UtcNow,
                overview = new
                {
                    totalCustomers = customers.Count,
                    totalJobOrders = jobs.Count,
                    activeJobOrders = activeJobs.Count,
                    completedJobOrders = completedJobs.Count,
                    jobsByStage = stageCounts
                },
                workshopExecution = new
                {
                    totalTasks,
                    completedTasks = doneTasks,
                    pendingOrOngoingTasks = totalTasks - doneTasks,
                    taskCompletionPercentage = taskPct,
                    averageTurnaroundHours = avgTurnaround
                },
                financialSummary = new
                {
                    totalEstimatedActive = totalBilled,
                    grossCollections = totalPaid,
                    refundsIssued = totalRefunded,
                    netCollections,
                    outstandingReceivables = outstandingBalance
                },
                appointments = new
                {
                    total = appts.Count,
                    scheduled = appts.Count(a => a.Status == "Scheduled"),
                    checkedIn = appts.Count(a => a.Status == "Checked In"),
                    cancelled = appts.Count(a => a.Status == "Cancelled"),
                    noShow = appts.Count(a => a.Status == "No Show")
                },
                customerExperience = new
                {
                    feedbackCount = feedbacks.Count,
                    averageCsatScore = avgCsat,
                    openComplaints,
                    totalComplaints = complaints.Count
                }
            });
        });

        crm.MapGet("/financial-summary", async (TenantCrmDbContext db) =>
        {
            var payments = await db.Payments.AsNoTracking().ToListAsync();
            var refunds = await db.Refunds.AsNoTracking().ToListAsync();

            var validPayments = payments.Where(p => !p.IsVoided).ToList();
            var voidedPayments = payments.Where(p => p.IsVoided).ToList();

            var methodTotals = validPayments
                .GroupBy(p => p.PaymentMethod)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.AmountPaid));

            var typeTotals = validPayments
                .GroupBy(p => p.PaymentType)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.AmountPaid));

            decimal gross = validPayments.Sum(p => p.AmountPaid);
            decimal refunded = refunds.Sum(r => r.Amount);
            decimal net = Math.Max(0m, gross - refunded);

            return Results.Ok(new
            {
                grossCollections = gross,
                refundsTotal = refunded,
                netRevenue = net,
                validPaymentCount = validPayments.Count,
                voidedPaymentCount = voidedPayments.Count,
                collectionsByMethod = methodTotals,
                collectionsByType = typeTotals,
                refundCount = refunds.Count
            });
        });

        crm.MapGet("/workshop-performance", async (TenantCrmDbContext db) =>
        {
            var jobs = await db.JobOrders.AsNoTracking().ToListAsync();
            var tasks = await db.JobTasks.AsNoTracking().ToListAsync();

            var mechanicWorkload = jobs
                .Where(j => !string.IsNullOrWhiteSpace(j.AssignedMechanic))
                .GroupBy(j => j.AssignedMechanic!)
                .ToDictionary(g => g.Key, g => new
                {
                    totalJobsAssigned = g.Count(),
                    activeJobs = g.Count(j => j.Status is "In Queue" or "In Progress" or "QA Checking"),
                    completedJobs = g.Count(j => j.Status is "Completed" or "Released")
                });

            var taskStatusBreakdown = tasks
                .GroupBy(t => t.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            return Results.Ok(new
            {
                mechanicWorkload,
                taskStatusBreakdown,
                activeTaskCount = tasks.Count(t => t.Status is "Pending" or "Ongoing"),
                completedTaskCount = tasks.Count(t => t.Status is "Done" or "Completed")
            });
        });
    }
}
