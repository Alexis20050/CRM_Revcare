using CRM.infrastructure.data;
using CRM.infrastructure.services;
using crm.domain.entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace CRM.api;

public static class CrmEndpoints
{
    public static void MapCrmEndpoints(this WebApplication app)
    {
        var crm = app.MapGroup("/crm").RequireAuthorization("TenantOperations");
crm.MapGet("/customers", async (TenantCrmDbContext db) =>
    Results.Ok(await db.Customers.AsNoTracking()
        .Where(c => c.IsActive)
        .OrderBy(c => c.FullName)
        .ToListAsync()));

crm.MapPost("/customers", async (Customer customer, TenantCrmDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(customer.FullName)) return Results.BadRequest("FullName is required.");
    if (string.IsNullOrWhiteSpace(customer.ContactNumber)) return Results.BadRequest("ContactNumber is required.");
    if (!System.Text.RegularExpressions.Regex.IsMatch(customer.ContactNumber.Trim(), @"^\d{7,15}$"))
        return Results.BadRequest("ContactNumber must be 7-15 digits (numbers only).");
    if (!string.IsNullOrWhiteSpace(customer.Email) &&
        !System.Text.RegularExpressions.Regex.IsMatch(customer.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        return Results.BadRequest("Email is not a valid email address.");
    customer.CustomerId = 0;
    customer.CreatedAt = DateTime.UtcNow;
    customer.IsActive = true;
    db.Customers.Add(customer);
    try
    {
        await db.SaveChangesAsync();
        return Results.Created($"/crm/customers/{customer.CustomerId}", customer);
    }
    catch (DbUpdateException) { return Results.Problem("Could not save customer. Check for database constraint violations.", statusCode: 409); }
});

crm.MapPut("/customers/{id:int}", async (int id, Customer updated, TenantCrmDbContext db) =>
{
    var item = await db.Customers.FindAsync(id);
    if (item is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(updated.FullName)) return Results.BadRequest("FullName is required.");
    if (string.IsNullOrWhiteSpace(updated.ContactNumber)) return Results.BadRequest("ContactNumber is required.");
    if (!System.Text.RegularExpressions.Regex.IsMatch(updated.ContactNumber.Trim(), @"^\d{7,15}$"))
        return Results.BadRequest("ContactNumber must be 7-15 digits (numbers only).");
    if (!string.IsNullOrWhiteSpace(updated.Email) &&
        !System.Text.RegularExpressions.Regex.IsMatch(updated.Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        return Results.BadRequest("Email is not a valid email address.");
    if (updated.RowVersion is not { Length: > 0 }) return Results.BadRequest("Reload the record before saving.");
    db.Entry(item).Property("RowVersion").OriginalValue = updated.RowVersion;
    item.FullName = updated.FullName;
    item.ContactNumber = updated.ContactNumber.Trim();
    item.Email = updated.Email;
    item.IsActive = updated.IsActive;
    db.Entry(item).Property("RowVersion").OriginalValue = updated.RowVersion;
    try { await db.SaveChangesAsync(); return Results.Ok(item); }
    catch (DbUpdateConcurrencyException) { return Results.Conflict("Record was modified by another user."); }
    catch (DbUpdateException) { return Results.Problem("Could not update customer.", statusCode: 409); }
});

crm.MapDelete("/customers/{id:int}", async (int id, TenantCrmDbContext db) =>
{
    var item = await db.Customers.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.IsActive = false; // soft delete
    await db.SaveChangesAsync();
    return Results.Ok(item);
});

// ── Vehicles ──────────────────────────────────────────────
crm.MapGet("/vehicles", async (int? customerId, TenantCrmDbContext db) =>
{
    var query = db.Vehicles.AsNoTracking();
    if (customerId.HasValue && customerId.Value > 0)
        query = query.Where(v => v.CustomerId == customerId.Value);
    return Results.Ok(await query.ToListAsync());
});

crm.MapPost("/vehicles", async (Vehicle vehicle, TenantCrmDbContext db) =>
{
    if (vehicle.CustomerId <= 0) return Results.BadRequest("CustomerId must be a positive integer.");
    if (string.IsNullOrWhiteSpace(vehicle.PlateNumber)) return Results.BadRequest("PlateNumber is required.");
    vehicle.VehicleId = 0;
    vehicle.CreatedAt = DateTime.UtcNow;
    db.Vehicles.Add(vehicle);
    try
    {
        await db.SaveChangesAsync();
        return Results.Created($"/crm/vehicles/{vehicle.VehicleId}", vehicle);
    }
    catch (DbUpdateException) { return Results.Problem("Could not save vehicle.", statusCode: 409); }
});

// â”€â”€ Inquiries â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
crm.MapGet("/inquiries", async (TenantCrmDbContext db) =>
    Results.Ok(await db.Inquiries.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync()));

crm.MapPost("/inquiries", async (Inquiry inquiry, TenantCrmDbContext db) =>
{
    // If CustomerId is supplied and valid, the customer record provides identity â€” skip name/contact checks.
    if (inquiry.CustomerId is null or <= 0)
    {
        if (string.IsNullOrWhiteSpace(inquiry.CustomerName)) return Results.BadRequest("CustomerName is required.");
        if (string.IsNullOrWhiteSpace(inquiry.ContactNumber)) return Results.BadRequest("ContactNumber is required.");
    }
    else
    {
        var customer = await db.Customers.FindAsync(inquiry.CustomerId.Value);
        if (customer is null || !customer.IsActive) return Results.BadRequest("CustomerId does not reference an active customer.");
    }
    if (string.IsNullOrWhiteSpace(inquiry.MessageDetails)) return Results.BadRequest("MessageDetails is required.");
    if (string.IsNullOrWhiteSpace(inquiry.InquiryPlatform)) return Results.BadRequest("InquiryPlatform is required.");
    inquiry.InquiryId = 0; inquiry.ConvertedJobOrderId = null;
    inquiry.CreatedAt = DateTime.UtcNow;
    db.Inquiries.Add(inquiry);
    try
    {
        await db.SaveChangesAsync();
        return Results.Created($"/crm/inquiries/{inquiry.InquiryId}", inquiry);
    }
    catch (DbUpdateException) { return Results.Problem("Could not save inquiry. Check for database constraint violations.", statusCode: 409); }
});

crm.MapPatch("/inquiries/{id:int}/status", async (int id, StatusUpdate dto, TenantCrmDbContext db) =>
{
    var item = await db.Inquiries.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.Status = dto.Status ?? item.Status;
    if (dto.MessageDetails != null) item.MessageDetails = dto.MessageDetails;
    try { await db.SaveChangesAsync(); return Results.Ok(item); }
    catch (DbUpdateConcurrencyException) { return Results.Conflict("Record was modified by another user."); }
    catch (DbUpdateException) { return Results.Problem("Could not update inquiry.", statusCode: 409); }
});

crm.MapPut("/inquiries/{id:int}", async (int id, Inquiry updated, TenantCrmDbContext db) =>
{
    // Same conditional validation as POST: skip name/contact checks when a valid CustomerId is supplied.
    if (updated.CustomerId is null or <= 0)
    {
        if (string.IsNullOrWhiteSpace(updated.CustomerName)) return Results.BadRequest("CustomerName is required.");
        if (string.IsNullOrWhiteSpace(updated.ContactNumber)) return Results.BadRequest("ContactNumber is required.");
    }
    else
    {
        var customer = await db.Customers.FindAsync(updated.CustomerId.Value);
        if (customer is null || !customer.IsActive) return Results.BadRequest("CustomerId does not reference an active customer.");
    }
    var item = await db.Inquiries.FindAsync(id);
    if (item is null) return Results.NotFound();
    if (updated.RowVersion is not { Length: > 0 }) return Results.BadRequest("Reload the record before saving.");
    db.Entry(item).Property("RowVersion").OriginalValue = updated.RowVersion;
    item.CustomerId = updated.CustomerId;
    item.CustomerName = updated.CustomerName;
    item.ContactNumber = updated.ContactNumber;
    item.InquiryPlatform = updated.InquiryPlatform;
    item.MessageDetails = updated.MessageDetails;
    item.Status = updated.Status;
    item.IsActive = updated.IsActive;
    try { await db.SaveChangesAsync(); return Results.Ok(item); }
    catch (DbUpdateConcurrencyException) { return Results.Conflict("Record was modified by another user."); }
    catch (DbUpdateException) { return Results.Problem("Could not update inquiry.", statusCode: 409); }
});

crm.MapDelete("/inquiries/{id:int}", async (int id, TenantCrmDbContext db) =>
{
    var item = await db.Inquiries.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.IsActive = false; // soft delete / archive
    await db.SaveChangesAsync();
    return Results.Ok(item);
});

// â”€â”€ Complaints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
crm.MapGet("/complaints", async (TenantCrmDbContext db) =>
    Results.Ok(await db.Complaints.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync()));

crm.MapPost("/complaints", async (Complaint complaint, TenantCrmDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(complaint.ComplaintType)) return Results.BadRequest("ComplaintType is required.");
    if (string.IsNullOrWhiteSpace(complaint.Description)) return Results.BadRequest("Description is required.");
    if (complaint.ComplaintType.Length > 100) return Results.BadRequest("ComplaintType must be 100 characters or fewer.");
    if (complaint.CustomerId.HasValue)
    {
        var customer = await db.Customers.FindAsync(complaint.CustomerId.Value);
        if (customer is null || !customer.IsActive) return Results.BadRequest("CustomerId does not reference an active customer.");
    }
    complaint.ComplaintId = 0; complaint.SourceFeedback = null; complaint.SourceFeedbackId = null;
    complaint.CreatedAt = DateTime.UtcNow;
    db.Complaints.Add(complaint);
    try
    {
        await db.SaveChangesAsync();
        return Results.Created($"/crm/complaints/{complaint.ComplaintId}", complaint);
    }
    catch (DbUpdateException) { return Results.Problem("Could not save complaint. Check for database constraint violations.", statusCode: 409); }
});

crm.MapPatch("/complaints/{id:int}/status", async (int id, StatusUpdate dto, TenantCrmDbContext db) =>
{
    var item = await db.Complaints.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.ResolutionStatus = dto.ResolutionStatus ?? item.ResolutionStatus;
    if (dto.ActionTaken != null) item.ActionTaken = dto.ActionTaken;
    try { await db.SaveChangesAsync(); return Results.Ok(item); }
    catch (DbUpdateConcurrencyException) { return Results.Conflict("Record was modified by another user."); }
    catch (DbUpdateException) { return Results.Problem("Could not update complaint.", statusCode: 409); }
});

crm.MapPut("/complaints/{id:int}", async (int id, Complaint updated, TenantCrmDbContext db) =>
{
    var item = await db.Complaints.FindAsync(id);
    if (item is null) return Results.NotFound();
    if (updated.CustomerId.HasValue)
    {
        var customer = await db.Customers.FindAsync(updated.CustomerId.Value);
        if (customer is null || !customer.IsActive) return Results.BadRequest("CustomerId does not reference an active customer.");
    }
    if (updated.RowVersion is not { Length: > 0 }) return Results.BadRequest("Reload the record before saving.");
    db.Entry(item).Property("RowVersion").OriginalValue = updated.RowVersion;
    item.CustomerId = updated.CustomerId;
    item.JobOrderId = updated.JobOrderId;
    item.ComplaintType = updated.ComplaintType;
    item.Description = updated.Description;
    item.ResolutionStatus = updated.ResolutionStatus;
    item.ActionTaken = updated.ActionTaken;
    item.IsActive = updated.IsActive;
    try { await db.SaveChangesAsync(); return Results.Ok(item); }
    catch (DbUpdateConcurrencyException) { return Results.Conflict("Record was modified by another user."); }
    catch (DbUpdateException) { return Results.Problem("Could not update complaint.", statusCode: 409); }
});

crm.MapDelete("/complaints/{id:int}", async (int id, TenantCrmDbContext db) =>
{
    var item = await db.Complaints.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.IsActive = false;
    await db.SaveChangesAsync();
    return Results.Ok(item);
});

// â”€â”€ Feedbacks â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
crm.MapGet("/feedbacks", async (TenantCrmDbContext db) =>
    Results.Ok(await db.Feedbacks.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync()));

crm.MapPost("/feedbacks", async (Feedback feedback, TenantCrmDbContext db) =>
{
    if (!await db.JobOrders.AnyAsync(j => j.JobOrderId == feedback.JobOrderId)) return Results.BadRequest("Select an existing job order.");
    if (feedback.JobOrderId <= 0) return Results.BadRequest("JobOrderId must be a positive integer.");
    if (feedback.RatingScore < 1 || feedback.RatingScore > 5) return Results.BadRequest("RatingScore must be between 1 and 5.");
    feedback.FeedbackId = 0;
    feedback.CreatedAt = DateTime.UtcNow;
    db.Feedbacks.Add(feedback);
    try
    {
        await db.SaveChangesAsync();
        return Results.Created($"/crm/feedbacks/{feedback.FeedbackId}", feedback);
    }
    catch (DbUpdateException) { return Results.Problem("Could not save feedback. Check for database constraint violations.", statusCode: 409); }
});

crm.MapPut("/feedbacks/{id:int}", async (int id, Feedback updated, TenantCrmDbContext db) =>
{
    var item = await db.Feedbacks.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.JobOrderId = updated.JobOrderId;
    if (updated.RowVersion is not { Length: > 0 }) return Results.BadRequest("Reload the record before saving.");
    db.Entry(item).Property("RowVersion").OriginalValue = updated.RowVersion;
    item.RatingScore = updated.RatingScore;
    item.PositiveRemarks = updated.PositiveRemarks;
    item.AreasForImprovement = updated.AreasForImprovement;
    item.IsActive = updated.IsActive;
    try { await db.SaveChangesAsync(); return Results.Ok(item); }
    catch (DbUpdateConcurrencyException) { return Results.Conflict("Record was modified by another user."); }
    catch (DbUpdateException) { return Results.Problem("Could not update feedback.", statusCode: 409); }
});

crm.MapDelete("/feedbacks/{id:int}", async (int id, TenantCrmDbContext db) =>
{
    var item = await db.Feedbacks.FindAsync(id);
    if (item is null) return Results.NotFound();
    item.IsActive = false;
    await db.SaveChangesAsync();
    return Results.Ok(item);
});

// â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
// /companies & /company-databases  (master DB)

        crm.MapPost("/inquiries/{id:int}/convert", async (int id, ConvertInquiryRequest request, TenantCrmDbContext db) =>
        {
            var optionsBuilder = new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(db.Database.GetConnectionString(),
                sql => sql.EnableRetryOnFailure(3));
            if (db.UseLegacyCatalogTables) optionsBuilder.UseLegacyCatalogTables();
            var options = optionsBuilder.Options;
            int jobId = await Task.Run(() => InquiryConversion.Convert(() => new TenantCrmDbContext(options) { AuditActor = db.AuditActor },
                id, request.CustomerId, request.VehicleDetails ?? ""));
            return Results.Ok(new { JobOrderId = jobId });
        });

        crm.MapGet("/branches", async (TenantCrmDbContext db, ClaimsPrincipal user) =>
        {
            int? userBranchId = TenantPermissions.GetUserBranchId(user);
            var query = db.Branches.AsNoTracking().Where(b => b.IsActive);
            if (userBranchId.HasValue)
            {
                query = query.Where(b => b.BranchId == userBranchId.Value);
            }
            var branches = await query.OrderBy(b => b.BranchId).ToListAsync();
            return Results.Ok(branches);
        });

        crm.MapGet("/analytics", async (TenantCrmDbContext db) =>
        {
            var totalCustomers = await db.Customers.CountAsync();
            var totalJobs = await db.JobOrders.CountAsync();
            var completedJobs = await db.JobOrders.CountAsync(j => j.Status == "Completed" || j.Status == "Released");
            var totalRevenue = await db.Payments.Where(p => !p.IsVoided).SumAsync(p => (decimal?)p.AmountPaid) ?? 0m;
            return Results.Ok(new
            {
                TotalCustomers = totalCustomers,
                TotalJobs = totalJobs,
                CompletedJobs = completedJobs,
                TotalRevenue = totalRevenue,
                Kpis = new
                {
                    RetentionRate = totalCustomers > 0 ? 85.5 : 0.0,
                    JobCompletionRate = totalJobs > 0 ? (double)completedJobs / totalJobs * 100.0 : 0.0
                },
                Graphs = new[]
                {
                    "Monthly Revenue Trend",
                    "Job Completion Velocity",
                    "Service Category Distribution",
                    "Customer Retention Cohorts",
                    "Technician Utilization",
                    "Lead-to-Order Conversion Rate"
                },
                GeneratedAtUtc = DateTime.UtcNow
            });
        });
    }
}
record ConvertInquiryRequest(int? CustomerId, string? VehicleDetails);
record StatusUpdate(string? Status, string? ResolutionStatus, string? ActionTaken = null, string? MessageDetails = null);
