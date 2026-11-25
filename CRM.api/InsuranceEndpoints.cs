using System.Security.Claims;
using System.Text.Json;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.api;

public static class InsuranceEndpoints
{
    public static void MapInsuranceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/crm/insurance").RequireAuthorization("TenantOperations");

        // List claims
        group.MapGet("", async (int? customerId, int? jobOrderId, string? status, TenantCrmDbContext db) =>
        {
            var entries = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "InsuranceClaim")
                .OrderBy(a => a.AuditEntryId)
                .ToListAsync();

            var claims = ConsolidateClaims(entries);

            if (customerId.HasValue && customerId.Value > 0)
                claims = claims.Where(c => c.CustomerId == customerId.Value).ToList();
            if (jobOrderId.HasValue && jobOrderId.Value > 0)
                claims = claims.Where(c => c.JobOrderId == jobOrderId.Value).ToList();
            if (!string.IsNullOrWhiteSpace(status))
                claims = claims.Where(c => string.Equals(c.Status, status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

            // Link actual received payments from ledger
            var jobIds = claims.Select(c => c.JobOrderId).Distinct().ToList();
            var insurancePayments = await db.Payments.AsNoTracking()
                .Where(p => jobIds.Contains(p.JobOrderId) && p.PayerType == "Insurance" && !p.IsVoided)
                .ToListAsync();

            var result = claims.Select(c =>
            {
                decimal received = insurancePayments.Where(p => p.JobOrderId == c.JobOrderId).Sum(p => p.AmountPaid);
                return c with { ActualReceivedPayments = received };
            });

            return Results.Ok(result);
        });

        // Get claim by reference
        group.MapGet("/{claimRef}", async (string claimRef, TenantCrmDbContext db) =>
        {
            var entries = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "InsuranceClaim")
                .OrderBy(a => a.AuditEntryId)
                .ToListAsync();

            var claims = ConsolidateClaims(entries);
            var claim = claims.FirstOrDefault(c => string.Equals(c.ClaimReference, claimRef.Trim(), StringComparison.OrdinalIgnoreCase));
            if (claim == null) return Results.NotFound(new { error = $"Insurance claim '{claimRef}' not found." });

            decimal received = await db.Payments.AsNoTracking()
                .Where(p => p.JobOrderId == claim.JobOrderId && p.PayerType == "Insurance" && !p.IsVoided)
                .SumAsync(p => p.AmountPaid);

            return Results.Ok(claim with { ActualReceivedPayments = received });
        });

        // Create new insurance claim
        group.MapPost("", async (InsuranceClaimCreateRequest request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(request.ProviderName))
                return Results.BadRequest(new { error = "Insurance provider name is required." });
            if (string.IsNullOrWhiteSpace(request.ClaimReference))
                return Results.BadRequest(new { error = "Claim or authorization reference is required." });
            if (request.JobOrderId <= 0)
                return Results.BadRequest(new { error = "Valid JobOrderId is required." });

            var job = await db.JobOrders.Include(j => j.Vehicle).FirstOrDefaultAsync(j => j.JobOrderId == request.JobOrderId);
            if (job == null) return Results.NotFound(new { error = $"Job Order #{request.JobOrderId} not found." });

            var existingEntries = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "InsuranceClaim")
                .ToListAsync();
            var existingClaims = ConsolidateClaims(existingEntries);
            if (existingClaims.Any(c => string.Equals(c.ClaimReference, request.ClaimReference.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Results.Conflict(new { error = $"Insurance claim reference '{request.ClaimReference}' already exists." });

            string actorName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? db.AuditActor;

            var claim = new InsuranceClaimRecord(
                ClaimReference: request.ClaimReference.Trim(),
                ProviderName: request.ProviderName.Trim(),
                JobOrderId: request.JobOrderId,
                CustomerId: job.CustomerId ?? 0,
                VehicleId: job.VehicleId ?? 0,
                EstimateId: job.ApprovedEstimateId,
                Status: "Pending",
                RequestedCoverage: request.RequestedCoverage ?? job.TotalEstimatedCost ?? 0m,
                ApprovedCoverage: 0m,
                CustomerResponsibility: request.CustomerDeductible ?? 0m,
                InsurerResponsibility: 0m,
                ActualReceivedPayments: 0m,
                Notes: request.Notes ?? "",
                ApprovalReference: null,
                ReviewedBy: null,
                ReviewedAtUtc: null,
                CreatedAtUtc: DateTime.UtcNow
            );

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actorName,
                EntityType = "InsuranceClaim",
                Action = "Created",
                Changes = JsonSerializer.Serialize(claim)
            });

            await db.SaveChangesAsync();
            return Results.Created($"/crm/insurance/{claim.ClaimReference}", claim);
        });

        // Approve insurance claim
        group.MapPost("/{claimRef}/approve", async (string claimRef, InsuranceApprovalRequest request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var entries = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "InsuranceClaim")
                .OrderBy(a => a.AuditEntryId)
                .ToListAsync();

            var claims = ConsolidateClaims(entries);
            var claim = claims.FirstOrDefault(c => string.Equals(c.ClaimReference, claimRef.Trim(), StringComparison.OrdinalIgnoreCase));
            if (claim == null) return Results.NotFound(new { error = $"Insurance claim '{claimRef}' not found." });

            if (claim.Status == "Approved")
                return Results.BadRequest(new { error = "Claim is already approved." });

            if (request.ApprovedCoverage <= 0m)
                return Results.BadRequest(new { error = "Approved coverage amount must be greater than zero." });

            decimal customerResp = request.CustomerResponsibility >= 0m ? request.CustomerResponsibility : claim.CustomerResponsibility;
            decimal insurerResp = Math.Max(0m, request.ApprovedCoverage - customerResp);

            string actorName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? db.AuditActor;

            var updated = claim with
            {
                Status = "Approved",
                ApprovedCoverage = request.ApprovedCoverage,
                CustomerResponsibility = customerResp,
                InsurerResponsibility = insurerResp,
                ApprovalReference = request.ApprovalReference?.Trim() ?? claimRef,
                ReviewedBy = actorName,
                ReviewedAtUtc = DateTime.UtcNow,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? claim.Notes : $"{claim.Notes} | Approval: {request.Notes.Trim()}".Trim()
            };

            // Non-negotiable rule: Insurance approval NEVER creates a Payment or alters JobOrder.TotalPaid
            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actorName,
                EntityType = "InsuranceClaim",
                Action = "Approved",
                Changes = JsonSerializer.Serialize(updated)
            });

            await db.SaveChangesAsync();

            decimal received = await db.Payments.AsNoTracking()
                .Where(p => p.JobOrderId == updated.JobOrderId && p.PayerType == "Insurance" && !p.IsVoided)
                .SumAsync(p => p.AmountPaid);

            return Results.Ok(new
            {
                claim = updated with { ActualReceivedPayments = received },
                message = $"Insurance claim '{claimRef}' approved for ₱{request.ApprovedCoverage:N2}. Insurer responsibility: ₱{insurerResp:N2}, Customer deductible: ₱{customerResp:N2}. (Note: Actual funds must be recorded via payment receipt upon remittance.)"
            });
        });

        // Decline insurance claim
        group.MapPost("/{claimRef}/decline", async (string claimRef, InsuranceDeclineRequest request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return Results.BadRequest(new { error = "Reason for declining claim is required." });

            var entries = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "InsuranceClaim")
                .OrderBy(a => a.AuditEntryId)
                .ToListAsync();

            var claims = ConsolidateClaims(entries);
            var claim = claims.FirstOrDefault(c => string.Equals(c.ClaimReference, claimRef.Trim(), StringComparison.OrdinalIgnoreCase));
            if (claim == null) return Results.NotFound(new { error = $"Insurance claim '{claimRef}' not found." });

            string actorName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? db.AuditActor;

            var updated = claim with
            {
                Status = "Declined",
                ReviewedBy = actorName,
                ReviewedAtUtc = DateTime.UtcNow,
                Notes = $"{claim.Notes} | Declined: {request.Reason.Trim()}".Trim()
            };

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actorName,
                EntityType = "InsuranceClaim",
                Action = "Declined",
                Changes = JsonSerializer.Serialize(updated)
            });

            await db.SaveChangesAsync();
            return Results.Ok(new { claim = updated, message = $"Insurance claim '{claimRef}' declined: {request.Reason}" });
        });

        // Record received insurance payment
        group.MapPost("/{claimRef}/record-payment", async (string claimRef, InsurancePaymentRecordRequest request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (request.AmountPaid <= 0m)
                return Results.BadRequest(new { error = "Payment amount must be greater than zero." });

            var entries = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "InsuranceClaim")
                .OrderBy(a => a.AuditEntryId)
                .ToListAsync();

            var claims = ConsolidateClaims(entries);
            var claim = claims.FirstOrDefault(c => string.Equals(c.ClaimReference, claimRef.Trim(), StringComparison.OrdinalIgnoreCase));
            if (claim == null) return Results.NotFound(new { error = $"Insurance claim '{claimRef}' not found." });

            if (claim.Status != "Approved")
                return Results.BadRequest(new { error = $"Cannot record payment against a {claim.Status} claim. Claim must be Approved first." });

            var job = await db.JobOrders.Include(j => j.Payments).FirstOrDefaultAsync(j => j.JobOrderId == claim.JobOrderId);
            if (job == null) return Results.NotFound(new { error = $"Job Order #{claim.JobOrderId} not found." });

            decimal currentReceived = job.Payments.Where(p => p.PayerType == "Insurance" && !p.IsVoided).Sum(p => p.AmountPaid);
            if (currentReceived + request.AmountPaid > claim.ApprovedCoverage)
                return Results.Conflict(new { error = $"Payment of ₱{request.AmountPaid:N2} exceeds approved coverage of ₱{claim.ApprovedCoverage:N2} (Already received: ₱{currentReceived:N2})." });

            string actorName = user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.NameIdentifier) ?? db.AuditActor;

            var payment = new Payment
            {
                JobOrderId = claim.JobOrderId,
                AmountPaid = request.AmountPaid,
                PaymentMethod = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Bank Transfer" : request.PaymentMethod.Trim(),
                PaymentType = "Insurance Settlement",
                PayerType = "Insurance",
                PaymentPurpose = $"Claim {claim.ClaimReference} ({claim.ProviderName})",
                SubmissionId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            };

            db.Payments.Add(payment);

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actorName,
                EntityType = "InsuranceClaim",
                Action = "PaymentReceived",
                Changes = JsonSerializer.Serialize(new { claim.ClaimReference, PaymentAmount = request.AmountPaid, NewTotal = currentReceived + request.AmountPaid })
            });

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                message = $"Recorded insurance payment of ₱{request.AmountPaid:N2} for claim '{claim.ClaimReference}'.",
                paymentId = payment.PaymentId,
                receiptReference = payment.ReceiptReference,
                totalInsurerPayments = currentReceived + request.AmountPaid,
                jobOutstandingBalance = job.OutstandingBalance
            });
        });
    }

    private static List<InsuranceClaimRecord> ConsolidateClaims(List<AuditEntry> entries)
    {
        var dict = new Dictionary<string, InsuranceClaimRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Changes)) continue;
            try
            {
                var record = JsonSerializer.Deserialize<InsuranceClaimRecord>(entry.Changes);
                if (record != null && !string.IsNullOrWhiteSpace(record.ClaimReference))
                {
                    dict[record.ClaimReference] = record;
                }
            }
            catch { }
        }
        return dict.Values.OrderByDescending(c => c.CreatedAtUtc).ToList();
    }
}

public sealed record InsuranceClaimRecord(
    string ClaimReference,
    string ProviderName,
    int JobOrderId,
    int CustomerId,
    int VehicleId,
    int? EstimateId,
    string Status, // Pending, Approved, Declined
    decimal RequestedCoverage,
    decimal ApprovedCoverage,
    decimal CustomerResponsibility,
    decimal InsurerResponsibility,
    decimal ActualReceivedPayments,
    string Notes,
    string? ApprovalReference,
    string? ReviewedBy,
    DateTime? ReviewedAtUtc,
    DateTime CreatedAtUtc
);

public sealed record InsuranceClaimCreateRequest(
    string ProviderName,
    string ClaimReference,
    int JobOrderId,
    decimal? RequestedCoverage,
    decimal? CustomerDeductible,
    string? Notes
);

public sealed record InsuranceApprovalRequest(
    decimal ApprovedCoverage,
    decimal CustomerResponsibility,
    string? ApprovalReference,
    string? Notes
);

public sealed record InsuranceDeclineRequest(
    string Reason
);

public sealed record InsurancePaymentRecordRequest(
    decimal AmountPaid,
    string? PaymentMethod,
    string? ReferenceNotes
);
