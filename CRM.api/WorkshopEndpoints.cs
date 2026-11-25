using System.Security.Claims;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;
namespace CRM.api;

public static class WorkshopEndpoints
{
    public static void MapWorkshopEndpoints(this WebApplication app)
    {
        var crm = app.MapGroup("/crm").RequireAuthorization("TenantOperations");
        crm.MapGet("/job-orders", async (TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            var query = db.JobOrders.AsNoTracking();
            if (!db.IsLegacyPrototypeTenantA && userBranchId.HasValue)
            {
                query = query.Where(j => j.BranchId == userBranchId.Value);
            }
            return Results.Ok(await query.OrderByDescending(j => j.JobOrderId).ToListAsync());
        });
        crm.MapGet("/job-orders/{id:int}", async (int id, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var job = await db.JobOrders.AsNoTracking().SingleOrDefaultAsync(j => j.JobOrderId == id);
            if (job == null) return Results.NotFound();
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            if (!db.IsLegacyPrototypeTenantA && userBranchId.HasValue && job.BranchId.HasValue && job.BranchId.Value != userBranchId.Value)
            {
                return Results.NotFound();
            }
            return Results.Ok(job);
        });
        crm.MapPost("/job-orders", async (JobOrderWrite request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            var job = new JobOrder { CustomerId = request.CustomerId, VehicleDetails = request.VehicleDetails?.Trim() ?? "",
                InquiryNotes = request.InquiryNotes, TotalEstimatedCost = request.TotalEstimatedCost,
                StartedAt = request.StartedAt, Status = "In Queue",
                BranchId = userBranchId ?? request.BranchId ?? 1 };
            if (string.IsNullOrWhiteSpace(job.VehicleDetails)) return Results.BadRequest(new { error = "Vehicle details are required." });
            JobServices.Assign(db, job, request.Services ?? []);
            db.Add(job);
            await db.SaveChangesAsync();
            return Results.Created($"/crm/job-orders/{job.JobOrderId}", job);
        });
        crm.MapPut("/job-orders/{id:int}", async (int id, JobOrderWrite request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var job = await db.JobOrders.FindAsync(id);
            if (job == null) return Results.NotFound();
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            if (!db.IsLegacyPrototypeTenantA && userBranchId.HasValue && job.BranchId.HasValue && job.BranchId.Value != userBranchId.Value)
            {
                return Results.Forbid();
            }
            if (request.RowVersion is not { Length: > 0 }) return Results.BadRequest(new { error = "RowVersion is required. Reload this job." });
            if (string.IsNullOrWhiteSpace(request.VehicleDetails)) return Results.BadRequest(new { error = "Vehicle details are required." });
            db.Entry(job).Property(j => j.RowVersion).OriginalValue = request.RowVersion;
            job.CustomerId = request.CustomerId;
            job.VehicleDetails = request.VehicleDetails?.Trim() ?? "";
            job.TotalEstimatedCost = request.TotalEstimatedCost;
            job.InquiryNotes = request.InquiryNotes;
            job.StartedAt = request.StartedAt;
            if (job.Status != request.Status && !WorkforceEndpoints.CanTransition(user, request.Status)) return Results.Forbid();
            job.Status = request.Status;
            JobServices.Assign(db, job, request.Services ?? []);
            await db.SaveChangesAsync();
            return Results.Ok(job);
        });
        crm.MapDelete("/job-orders/{id:int}", async (int id, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var job = await db.JobOrders.FindAsync(id);
            if (job == null) return Results.NotFound();
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            if (!db.IsLegacyPrototypeTenantA && userBranchId.HasValue && job.BranchId.HasValue && job.BranchId.Value != userBranchId.Value)
            {
                return Results.Forbid();
            }
            return Results.Conflict(new { message = "Job Orders cannot be permanently deleted. Use the approved archive workflow." });
        });
        crm.MapGet("/job-tasks", async (int? jobOrderId, TenantCrmDbContext db) =>
            Results.Ok(await db.JobTasks.AsNoTracking().Where(t => jobOrderId == null || t.JobOrderId == jobOrderId)
                .OrderByDescending(t => t.JobTaskId).ToListAsync()));
        crm.MapPut("/job-tasks/{id:int}", async (int id, TaskWrite request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var task = await db.JobTasks.FindAsync(id);
            if (task == null) return Results.NotFound();
            var job = await db.JobOrders.AsNoTracking().FirstOrDefaultAsync(j => j.JobOrderId == task.JobOrderId);
            if (job == null) return Results.NotFound();
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            if (!db.IsLegacyPrototypeTenantA && userBranchId.HasValue && job.BranchId.HasValue && job.BranchId.Value != userBranchId.Value)
            {
                return Results.Forbid();
            }
            if (TenantPermissions.Technician(user))
            {
                string userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
                if (job.AssignedMechanicUserId != userId)
                {
                    return Results.NotFound(new { error = "You are not authorized to update this task. It belongs to another mechanic or the job was reassigned.", code = "STALE_ASSIGNMENT" });
                }
            }
            if (request.RowVersion is not { Length: > 0 }) return Results.BadRequest(new { error = "RowVersion is required. Reload this task." });
            db.Entry(task).Property(t => t.RowVersion).OriginalValue = request.RowVersion;
            task.Status = request.Status; task.Remarks = request.Remarks;
            await db.SaveChangesAsync();
            return Results.Ok(task);
        });
        crm.MapGet("/payments", async (int? jobOrderId, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            var query = db.Payments.AsNoTracking();
            if (jobOrderId != null) query = query.Where(p => p.JobOrderId == jobOrderId);
            if (!db.IsLegacyPrototypeTenantA && userBranchId.HasValue)
            {
                query = query.Where(p => p.BranchId == userBranchId.Value);
            }
            return Results.Ok(await query.OrderByDescending(p => p.PaymentId).ToListAsync());
        });
        crm.MapPost("/payments", async (PaymentWrite request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (request.SubmissionId == Guid.Empty) return Results.BadRequest(new { error = "A unique SubmissionId is required." });
            var existing = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.SubmissionId == request.SubmissionId);
            if (existing != null)
                return existing.JobOrderId == request.JobOrderId && existing.AmountPaid == request.AmountPaid
                    && existing.PaymentMethod == request.PaymentMethod && existing.PaymentType == request.PaymentType
                    && existing.AllowCustomerCredit == request.AllowCustomerCredit
                    ? Results.Ok(existing) : Results.Conflict(new { error = "SubmissionId was already used for a different payment." });
            var parentJob = await db.JobOrders.AsNoTracking().FirstOrDefaultAsync(j => j.JobOrderId == request.JobOrderId);
            if (parentJob == null) return Results.BadRequest(new { error = "Select an existing job order." });
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            if (!db.IsLegacyPrototypeTenantA && userBranchId.HasValue && parentJob.BranchId.HasValue && parentJob.BranchId.Value != userBranchId.Value)
            {
                return Results.Forbid();
            }
            var payment = new Payment { JobOrderId = request.JobOrderId, AmountPaid = request.AmountPaid,
                PaymentMethod = request.PaymentMethod, PaymentType = request.PaymentType,
                AllowCustomerCredit = request.AllowCustomerCredit, SubmissionId = request.SubmissionId,
                BranchId = userBranchId ?? parentJob.BranchId ?? 1 };
            db.Add(payment); await db.SaveChangesAsync();
            return Results.Created($"/crm/payments/{payment.PaymentId}", payment);
        });
        crm.MapPut("/payments/{id:int}", async (int id, PaymentCorrection request, TenantCrmDbContext db) =>
        {
            var payment = await db.Payments.FindAsync(id);
            if (payment == null) return Results.NotFound();
            return Results.Conflict(new { error = "Saved receipts are read-only. Void the incorrect receipt with a reason, then record a replacement payment." });
        });
        crm.MapPost("/payments/{id:int}/void", async (int id, VoidPayment request, TenantCrmDbContext db) =>
        {
            var payment = await db.Payments.FindAsync(id);
            if (payment == null) return Results.NotFound();
            if (request.RowVersion is not { Length: > 0 }) return Results.BadRequest(new { error = "RowVersion is required. Reload this payment." });
            db.Entry(payment).Property(p => p.RowVersion).OriginalValue = request.RowVersion;
            payment.IsVoided = true; payment.VoidReason = request.Reason;
            await db.SaveChangesAsync(); return Results.Ok(payment);
        });
        crm.MapGet("/payments/{id:int}/receipt", async (int id, TenantCrmDbContext db, PlatformMasterDbContext master, ClaimsPrincipal user, IConfiguration config) =>
        {
            var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.PaymentId == id);
            if (payment == null) return Results.NotFound();

            string tenantId = user.FindFirst("tenant_id")?.Value
                ?? (await db.DatabaseIdentity.Select(d => d.TenantId).FirstOrDefaultAsync())
                ?? "tenant-a";

            var tenantReg = await master.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.TenantId == tenantId);
            var tenantSection = config.GetSection($"Tenants:{tenantId}");

            string bName = tenantSection["Name"] ?? tenantReg?.DisplayName ?? "RevCare Auto Hub";
            string? bTagline = tenantSection["Tagline"];
            string? bAddress = tenantSection["Address"];
            string? bContact = tenantSection["Contact"];

            var businessDetails = new
            {
                name = bName,
                tagline = bTagline,
                address = bAddress,
                contact = bContact
            };

            var job = await db.JobOrders.AsNoTracking().Include(j => j.Payments).FirstOrDefaultAsync(j => j.JobOrderId == payment.JobOrderId);
            var customerName = job != null
                ? await db.Customers.AsNoTracking().Where(c => c.CustomerId == job.CustomerId).Select(c => c.FullName).FirstOrDefaultAsync() ?? "Customer"
                : "Customer";

            string subStr = payment.SubmissionId.ToString();
            var audit = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "Payment" && a.Action == "Added" && a.Changes.Contains(subStr))
                .OrderByDescending(a => a.AuditEntryId)
                .FirstOrDefaultAsync();

            string recordedBy = "Not recorded";
            if (audit != null && !string.IsNullOrWhiteSpace(audit.Actor) && audit.Actor != "Unattributed application")
            {
                var staff = await db.Staff.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == audit.Actor);
                recordedBy = staff?.DisplayName ?? audit.Actor;
            }

            var validPayments = job?.Payments.Where(p => !p.IsVoided).ToList() ?? new List<Payment>();
            decimal totalPaid = validPayments.Sum(p => p.AmountPaid);
            decimal? totalEstimatedCost = job?.TotalEstimatedCost;
            decimal? outstandingBalance = totalEstimatedCost.HasValue ? Math.Max(0m, totalEstimatedCost.Value - totalPaid) : null;
            decimal? customerCredit = totalEstimatedCost.HasValue ? Math.Max(0m, totalPaid - totalEstimatedCost.Value) : null;

            return Results.Ok(new
            {
                paymentId = payment.PaymentId,
                receiptReference = payment.ReceiptReference,
                paymentDate = payment.CreatedAt,
                customerName,
                jobOrderId = payment.JobOrderId,
                vehicleDetails = job?.VehicleDisplay ?? "Vehicle Details",
                paymentType = payment.PaymentType,
                paymentMethod = payment.PaymentMethod,
                amountReceived = payment.AmountPaid,
                totalEstimatedCost,
                totalPaid,
                outstandingBalance,
                customerCredit,
                recordedBy,
                isVoided = payment.IsVoided,
                voidReason = payment.VoidReason ?? "",
                voidedAtUtc = payment.VoidedAtUtc,
                calculatedAtUtc = DateTime.UtcNow,
                businessDetails
            });
        });
    }
}
public sealed record JobOrderWrite(int CustomerId, string VehicleDetails, decimal? TotalEstimatedCost,
    DateTime StartedAt, string Status, string InquiryNotes, string[] Services, byte[] RowVersion, int? BranchId = null);
public sealed record TaskWrite(string Status, string Remarks, byte[] RowVersion);
public sealed record PaymentWrite(int JobOrderId, decimal AmountPaid, string PaymentType, string PaymentMethod, bool AllowCustomerCredit, Guid SubmissionId, int? BranchId = null);
public sealed record PaymentCorrection(decimal AmountPaid, string PaymentType, string PaymentMethod, bool AllowCustomerCredit, byte[] RowVersion);
public sealed record VoidPayment(string Reason, byte[] RowVersion);
