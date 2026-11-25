#nullable enable
using System.Security.Claims;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace CRM.api;

public static class EstimateEndpoints
{
    public static void MapEstimateEndpoints(this WebApplication app)
    {
        var crm = app.MapGroup("/crm").RequireAuthorization("TenantOperations");

        // --- Estimates ---
        crm.MapGet("/estimates", async (TenantCrmDbContext db) =>
        {
            var estimates = await db.Estimates.AsNoTracking()
                .Include(e => e.Customer)
                .Include(e => e.Vehicle)
                .Include(e => e.Lines)
                .OrderByDescending(e => e.EstimateId)
                .Select(e => new
                {
                    e.EstimateId,
                    e.EstimateNumber,
                    e.CustomerId,
                    CustomerName = e.Customer != null ? e.Customer.FullName : "",
                    CustomerContact = e.Customer != null ? e.Customer.ContactNumber : "",
                    e.VehicleId,
                    VehicleDisplay = e.Vehicle != null ? e.Vehicle.DisplayName : "",
                    e.Status,
                    e.TotalAmount,
                    e.ApprovedAt,
                    e.Notes,
                    LineCount = e.Lines.Count,
                    e.CreatedAt
                })
                .ToListAsync();

            return Results.Ok(estimates);
        });

        crm.MapGet("/estimates/{id:int}", async (int id, TenantCrmDbContext db) =>
        {
            var estimate = await db.Estimates.AsNoTracking()
                .Include(e => e.Customer)
                .Include(e => e.Vehicle)
                .Include(e => e.Lines)
                .FirstOrDefaultAsync(e => e.EstimateId == id);

            if (estimate == null) return Results.NotFound();

            return Results.Ok(new
            {
                estimate.EstimateId,
                estimate.EstimateNumber,
                estimate.CustomerId,
                CustomerName = estimate.Customer?.FullName ?? "",
                CustomerContact = estimate.Customer?.ContactNumber ?? "",
                estimate.VehicleId,
                VehicleDisplay = estimate.Vehicle?.DisplayName ?? "",
                estimate.Status,
                estimate.TotalAmount,
                estimate.ApprovedAt,
                estimate.Notes,
                estimate.CreatedAt,
                estimate.RowVersion,
                Lines = estimate.Lines.Select(l => new
                {
                    l.EstimateLineId,
                    l.ServiceCatalogId,
                    l.Description,
                    l.Quantity,
                    l.UnitPrice,
                    l.LineTotal
                })
            });
        });

        crm.MapPost("/estimates", async (EstimateCreate request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (request.CustomerId <= 0)
                return Results.BadRequest(new { error = "Valid CustomerId is required." });

            var customer = await db.Customers.FindAsync(request.CustomerId);
            if (customer == null || !customer.IsActive)
                return Results.BadRequest(new { error = "Active customer not found." });

            var estimate = new Estimate
            {
                CustomerId = request.CustomerId,
                VehicleId = request.VehicleId > 0 ? request.VehicleId : null,
                Notes = request.Notes?.Trim() ?? "",
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            };

            decimal total = 0m;
            if (request.Lines != null && request.Lines.Count > 0)
            {
                foreach (var line in request.Lines)
                {
                    decimal lineTotal = line.Quantity * line.UnitPrice;
                    total += lineTotal;
                    estimate.Lines.Add(new EstimateLine
                    {
                        ServiceCatalogId = line.ServiceCatalogId > 0 ? line.ServiceCatalogId : null,
                        Description = line.Description.Trim(),
                        Quantity = line.Quantity > 0 ? line.Quantity : 1m,
                        UnitPrice = line.UnitPrice,
                        LineTotal = lineTotal
                    });
                }
            }

            estimate.TotalAmount = total;
            db.Estimates.Add(estimate);
            await db.SaveChangesAsync();

            estimate.EstimateNumber = $"EST-{estimate.EstimateId:D6}";
            await db.SaveChangesAsync();

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = user.Identity?.Name ?? db.AuditActor,
                EntityType = "Estimate",
                Action = "Created",
                Changes = System.Text.Json.JsonSerializer.Serialize(new { estimate.EstimateId, estimate.EstimateNumber, estimate.TotalAmount })
            });
            await db.SaveChangesAsync();

            return Results.Created($"/crm/estimates/{estimate.EstimateId}", new
            {
                estimate.EstimateId,
                estimate.EstimateNumber,
                estimate.CustomerId,
                estimate.Status,
                estimate.TotalAmount,
                estimate.Notes,
                estimate.CreatedAt,
                Lines = estimate.Lines.Select(l => new
                {
                    l.EstimateLineId,
                    l.Description,
                    l.Quantity,
                    l.UnitPrice,
                    l.LineTotal
                })
            });
        });

        crm.MapPost("/estimates/{id:int}/approve", async (int id, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var estimate = await db.Estimates.Include(e => e.Lines).FirstOrDefaultAsync(e => e.EstimateId == id);
            if (estimate == null) return Results.NotFound();

            if (HistoricalCompatibility.IsProtected(estimate))
                return Results.BadRequest(new { error = "Historical reconstructed estimates cannot be modified or re-approved." });

            if (estimate.Status != "Draft")
                return Results.BadRequest(new { error = $"Cannot approve an estimate with status '{estimate.Status}'." });

            estimate.Status = "Approved";
            estimate.ApprovedAt = DateTime.UtcNow;

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = user.Identity?.Name ?? db.AuditActor,
                EntityType = "Estimate",
                Action = "Approved",
                Changes = System.Text.Json.JsonSerializer.Serialize(new { estimate.EstimateId, estimate.TotalAmount, ApprovedAt = estimate.ApprovedAt })
            });

            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                message = $"Estimate {estimate.EstimateNumber} approved.",
                estimate = new
                {
                    estimate.EstimateId,
                    estimate.EstimateNumber,
                    estimate.Status,
                    estimate.TotalAmount,
                    estimate.ApprovedAt
                }
            });
        });

        crm.MapPost("/estimates/{id:int}/convert", async (int id, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var strategy = db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                var estimate = await db.Estimates
                    .FromSqlInterpolated($"SELECT * FROM Estimates WITH (UPDLOCK, HOLDLOCK) WHERE EstimateId = {id}")
                    .Include(e => e.Lines)
                    .Include(e => e.Vehicle)
                    .FirstOrDefaultAsync();

                if (estimate == null) return Results.NotFound();

                if (estimate.Status == "Converted")
                    return Results.Conflict(new { error = $"Estimate {estimate.EstimateNumber} has already been converted." });

                if (estimate.Status != "Approved")
                    return Results.BadRequest(new { error = "Only Approved estimates can be converted to Job Orders." });

                string actor = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? db.AuditActor;

                // Workflow Consistency: Check if an appointment check-in already created a Job Order in Queue for this customer/vehicle
                JobOrder? existingJob = null;
                if (estimate.CustomerId.HasValue && estimate.VehicleId.HasValue)
                {
                    existingJob = await db.JobOrders
                        .Include(j => j.Tasks)
                        .Where(j => j.CustomerId == estimate.CustomerId.Value &&
                                    j.VehicleId == estimate.VehicleId.Value &&
                                    j.Status == "In Queue" &&
                                    j.ApprovedEstimateId == null)
                        .OrderByDescending(j => j.JobOrderId)
                        .FirstOrDefaultAsync();
                }

                JobOrder job;
                bool wasExisting = false;

                if (existingJob != null)
                {
                    // Associate with existing Job Order created by appointment check-in, avoiding unintended duplicate Job Orders
                    job = existingJob;
                    wasExisting = true;
                    job.ApprovedEstimateId = estimate.EstimateId;
                    job.EstimateId = estimate.EstimateNumber;
                    job.TotalEstimatedCost = estimate.TotalAmount;
                    job.InquiryNotes = string.IsNullOrWhiteSpace(job.InquiryNotes)
                        ? $"Linked Approved Estimate {estimate.EstimateNumber}. {estimate.Notes}".Trim()
                        : $"{job.InquiryNotes} | Linked Estimate {estimate.EstimateNumber}".Trim();

                    // Synchronize tasks from estimate lines
                    foreach (var line in estimate.Lines)
                    {
                        if (!job.Tasks.Any(t => string.Equals(t.ServiceId, line.Description, StringComparison.OrdinalIgnoreCase)))
                        {
                            job.Tasks.Add(new JobTask
                            {
                                ServiceId = line.Description,
                                Status = "Pending",
                                Remarks = $"Est. price ₱{line.UnitPrice:N2}"
                            });
                        }
                    }
                }
                else
                {
                    // Direct walk-in or new visit: create a new Job Order in Queue
                    job = new JobOrder
                    {
                        CustomerId = estimate.CustomerId,
                        VehicleId = estimate.VehicleId,
                        VehicleDetails = estimate.Vehicle?.DisplayName ?? "",
                        ApprovedEstimateId = estimate.EstimateId,
                        EstimateId = estimate.EstimateNumber,
                        TotalEstimatedCost = estimate.TotalAmount,
                        Status = "In Queue",
                        InquiryNotes = $"Created from Estimate {estimate.EstimateNumber}. {estimate.Notes}".Trim(),
                        StartedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    foreach (var line in estimate.Lines)
                    {
                        job.Tasks.Add(new JobTask
                        {
                            ServiceId = line.Description,
                            Status = "Pending",
                            Remarks = $"Est. price ₱{line.UnitPrice:N2}"
                        });
                    }

                    db.JobOrders.Add(job);
                }

                estimate.Status = "Converted";

                db.AuditEntries.Add(new AuditEntry
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    Actor = actor,
                    EntityType = "Estimate",
                    Action = wasExisting ? "AssociatedWithExistingJobOrder" : "ConvertedToJobOrder",
                    Changes = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        estimate.EstimateId,
                        JobOrderId = job.JobOrderId,
                        WasExistingJobOrder = wasExisting
                    })
                });

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                return Results.Ok(new
                {
                    estimateId = estimate.EstimateId,
                    jobOrderId = job.JobOrderId,
                    wasExistingJobOrder = wasExisting,
                    message = wasExisting
                        ? $"Estimate {estimate.EstimateNumber} successfully associated with existing queued Job Order #{job.JobOrderId}."
                        : $"Estimate {estimate.EstimateNumber} converted to new Job Order #{job.JobOrderId}."
                });
            });
        });

        // --- Managed Terms and Conditions ---
        crm.MapGet("/terms", async (TenantCrmDbContext db) =>
        {
            var activeTerms = await GetActiveTermsAsync(db);
            return Results.Ok(activeTerms);
        });

        crm.MapGet("/terms/versions", async (TenantCrmDbContext db) =>
        {
            var termsList = await GetAllTermsVersionsAsync(db);
            return Results.Ok(termsList);
        });

        crm.MapPost("/terms", async (TermsPublishRequest request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(request.Version))
                return Results.BadRequest(new { error = "Terms version string is required (e.g. v1.1, v2.0)." });
            if (string.IsNullOrWhiteSpace(request.Title))
                return Results.BadRequest(new { error = "Terms title is required." });
            if (request.Terms == null || request.Terms.Count == 0)
                return Results.BadRequest(new { error = "At least one terms clause is required." });

            string actor = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? db.AuditActor;

            var existingVersions = await GetAllTermsVersionsAsync(db);
            if (existingVersions.Any(v => string.Equals(v.Version, request.Version.Trim(), StringComparison.OrdinalIgnoreCase)))
                return Results.Conflict(new { error = $"Terms version '{request.Version}' already exists." });

            var newTerms = new ManagedTermsRecord(
                Version: request.Version.Trim(),
                Title: request.Title.Trim(),
                Terms: request.Terms.Select(t => t.Trim()).ToList(),
                EffectiveDateUtc: request.EffectiveDateUtc ?? DateTime.UtcNow,
                IsActive: request.MakeActive,
                CreatedBy: actor,
                CreatedAtUtc: DateTime.UtcNow
            );

            // If making active, deactivate previously active versions
            if (request.MakeActive)
            {
                foreach (var v in existingVersions.Where(v => v.IsActive))
                {
                    var deactivated = v with { IsActive = false };
                    db.AuditEntries.Add(new AuditEntry
                    {
                        OccurredAtUtc = DateTime.UtcNow,
                        Actor = actor,
                        EntityType = "TermsVersion",
                        Action = "Deactivated",
                        Changes = System.Text.Json.JsonSerializer.Serialize(deactivated)
                    });
                }
            }

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actor,
                EntityType = "TermsVersion",
                Action = "Published",
                Changes = System.Text.Json.JsonSerializer.Serialize(newTerms)
            });

            await db.SaveChangesAsync();
            return Results.Created($"/crm/terms/{newTerms.Version}", newTerms);
        });

        crm.MapPut("/terms/{version}/activate", async (string version, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var existingVersions = await GetAllTermsVersionsAsync(db);
            var target = existingVersions.FirstOrDefault(v => string.Equals(v.Version, version.Trim(), StringComparison.OrdinalIgnoreCase));
            if (target == null) return Results.NotFound(new { error = $"Terms version '{version}' not found." });

            string actor = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? db.AuditActor;

            foreach (var v in existingVersions.Where(v => v.IsActive))
            {
                var deactivated = v with { IsActive = false };
                db.AuditEntries.Add(new AuditEntry
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    Actor = actor,
                    EntityType = "TermsVersion",
                    Action = "Deactivated",
                    Changes = System.Text.Json.JsonSerializer.Serialize(deactivated)
                });
            }

            var activated = target with { IsActive = true };
            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actor,
                EntityType = "TermsVersion",
                Action = "Activated",
                Changes = System.Text.Json.JsonSerializer.Serialize(activated)
            });

            await db.SaveChangesAsync();
            return Results.Ok(new { message = $"Terms version '{version}' is now active.", activeTerms = activated });
        });

        crm.MapPost("/job-orders/{id:int}/terms-acceptance", async (int id, TermsAcceptanceRequest request, TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            var job = await db.JobOrders.FirstOrDefaultAsync(j => j.JobOrderId == id);
            if (job == null) return Results.NotFound();

            string actor = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? "Staff";

            // Resolve terms version (either explicit request or active version)
            string version;
            if (!string.IsNullOrWhiteSpace(request.TermsVersion))
            {
                version = request.TermsVersion.Trim();
            }
            else
            {
                var active = await GetActiveTermsAsync(db);
                version = active.Version;
            }

            string tag = $"[Terms {version} Accepted]";
            if (!job.InquiryNotes.Contains(tag, StringComparison.OrdinalIgnoreCase))
            {
                job.InquiryNotes = string.IsNullOrWhiteSpace(job.InquiryNotes) ? tag : $"{job.InquiryNotes} {tag}";
            }

            string? linkedCustomerName = job.CustomerId.HasValue
                ? (await db.Customers.FindAsync(job.CustomerId.Value))?.FullName
                : null;
            string customerName = request.CustomerName ?? linkedCustomerName ?? "Customer";

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAtUtc = DateTime.UtcNow,
                Actor = actor,
                EntityType = "JobOrder",
                Action = "TermsAcceptance",
                Changes = System.Text.Json.JsonSerializer.Serialize(new
                {
                    jobOrderId = id,
                    version,
                    customerName,
                    customerAcknowledged = request.CustomerAcknowledged,
                    acceptanceMethod = request.AcceptanceMethod ?? "Digital Intake Form",
                    actor,
                    acceptedAtUtc = DateTime.UtcNow,
                    notes = request.Notes
                })
            });

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                jobOrderId = id,
                termsAccepted = true,
                version,
                customerName,
                acceptanceMethod = request.AcceptanceMethod ?? "Digital Intake Form",
                acceptedBy = actor,
                acceptedAtUtc = DateTime.UtcNow,
                message = $"Terms and conditions ({version}) recorded for Job Order #{id}."
            });
        });

        crm.MapGet("/job-orders/{id:int}/terms-status", async (int id, TenantCrmDbContext db) =>
        {
            var job = await db.JobOrders.AsNoTracking().FirstOrDefaultAsync(j => j.JobOrderId == id);
            if (job == null) return Results.NotFound();

            var audit = await db.AuditEntries.AsNoTracking()
                .Where(a => a.EntityType == "JobOrder" && a.Action == "TermsAcceptance")
                .OrderByDescending(a => a.OccurredAtUtc)
                .ToListAsync();

            var matched = audit.FirstOrDefault(a => a.Changes.Contains($"\"jobOrderId\":{id}") || a.Changes.Contains($"\"jobOrderId\": {id}"));

            bool accepted = matched != null || job.InquiryNotes.Contains("[Terms", StringComparison.OrdinalIgnoreCase);

            string? parsedVersion = null;
            if (matched != null)
            {
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(matched.Changes);
                    if (doc.RootElement.TryGetProperty("version", out var prop)) parsedVersion = prop.GetString();
                }
                catch { }
            }

            return Results.Ok(new
            {
                jobOrderId = id,
                termsAccepted = accepted,
                version = parsedVersion ?? (accepted ? "v1.0" : null),
                details = matched != null ? matched.Changes : (accepted ? "Recorded in job intake notes." : "Terms acceptance pending.")
            });
        });
    }

    private static async Task<ManagedTermsRecord> GetActiveTermsAsync(TenantCrmDbContext db)
    {
        var versions = await GetAllTermsVersionsAsync(db);
        var active = versions.FirstOrDefault(v => v.IsActive);
        if (active != null) return active;

        // Default standard terms v1.0 fallback
        return GetDefaultTermsV1();
    }

    private static async Task<List<ManagedTermsRecord>> GetAllTermsVersionsAsync(TenantCrmDbContext db)
    {
        var entries = await db.AuditEntries.AsNoTracking()
            .Where(a => a.EntityType == "TermsVersion")
            .OrderBy(a => a.AuditEntryId)
            .ToListAsync();

        var dict = new Dictionary<string, ManagedTermsRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Changes)) continue;
            try
            {
                var record = System.Text.Json.JsonSerializer.Deserialize<ManagedTermsRecord>(entry.Changes);
                if (record != null && !string.IsNullOrWhiteSpace(record.Version))
                {
                    dict[record.Version] = record;
                }
            }
            catch { }
        }

        if (dict.Count == 0)
        {
            var def = GetDefaultTermsV1();
            dict[def.Version] = def;
        }

        return dict.Values.OrderByDescending(v => v.EffectiveDateUtc).ToList();
    }

    private static ManagedTermsRecord GetDefaultTermsV1() => new(
        Version: "v1.0",
        Title: "RevCare Auto Hub Standard Workshop Terms & Conditions",
        Terms: new List<string>
        {
            "1. Service Authorization: The customer authorizes RevCare Auto Hub to inspect, test, and perform the agreed repair and maintenance services on the specified vehicle.",
            "2. 50% Deposit Requirement: A mandatory 50% deposit of total estimated cost is required before mechanical work commences.",
            "3. Additional Work & Discovered Defects: Any additional defect discovered during disassembly requires explicit customer approval prior to execution.",
            "4. Vehicle Storage & Release: Full settlement of outstanding balances is required prior to vehicle release. Vehicles not retrieved within 48 hours of completion notice are subject to workshop storage charges.",
            "5. Replaced Parts: All replaced parts will be returned to the customer upon release, except parts required for warranty claims.",
            "6. Limitation of Liability: Workshop is not liable for loss or damage to personal items left in the vehicle."
        },
        EffectiveDateUtc: new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc),
        IsActive: true,
        CreatedBy: "System",
        CreatedAtUtc: new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc)
    );
}

public sealed record EstimateCreate(int CustomerId, int? VehicleId, string? Notes, List<EstimateLineCreate>? Lines);
public sealed record EstimateLineCreate(int? ServiceCatalogId, string Description, decimal Quantity, decimal UnitPrice);
public sealed record TermsAcceptanceRequest(string? TermsVersion, bool CustomerAcknowledged = true, string? Notes = null, string? CustomerName = null, string? AcceptanceMethod = null);

public sealed record ManagedTermsRecord(
    string Version,
    string Title,
    List<string> Terms,
    DateTime EffectiveDateUtc,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAtUtc
);

public sealed record TermsPublishRequest(
    string Version,
    string Title,
    List<string> Terms,
    DateTime? EffectiveDateUtc,
    bool MakeActive = true
);
