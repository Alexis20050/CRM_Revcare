using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

using (var readOnlyCloud = new MasterCrmDbContext(new DbContextOptionsBuilder<MasterCrmDbContext>()
    .UseSqlServer("Server=127.0.0.1,1;Database=db80735;Integrated Security=True;Connect Timeout=1;TrustServerCertificate=True").Options))
{
    try { readOnlyCloud.SaveChanges(); throw new Exception("Cloud sync save was not blocked."); }
    catch (InvalidOperationException ex) when (ex.Message.Contains("read-only")) { Console.WriteLine("PASS: compatibility sync save blocked before connecting"); }
    try { await readOnlyCloud.SaveChangesAsync(); throw new Exception("Cloud async save was not blocked."); }
    catch (InvalidOperationException ex) when (ex.Message.Contains("read-only")) { Console.WriteLine("PASS: compatibility async save blocked before connecting"); }
}

var options = new DbContextOptionsBuilder<MasterCrmDbContext>().UseSqlServer(
    @"Server=(localdb)\MSSQLLocalDB;Database=RevCare_Rules_" + Guid.NewGuid().ToString("N") + ";Trusted_Connection=True;TrustServerCertificate=True").Options;
MasterCrmDbContext Create() => new(options);
using var db = Create();
try
{
    db.Database.Migrate();
    db.Staff.AddRange(
        new WorkshopStaff { StaffId = "juan.gomez", DisplayName = "Juan Gomez", Role = "Automotive Mechanic", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
        new WorkshopStaff { StaffId = "pedro.santos", DisplayName = "Pedro Santos", Role = "Lead Mechanic", IsActive = true, CreatedAtUtc = DateTime.UtcNow }
    );
    db.SaveChanges();
    var customer = new Customer { FullName = "Rules test", ContactNumber = "09171234567" };
    db.Add(customer); db.SaveChanges();
    var serviceJob = new JobOrder { CustomerId = customer.CustomerId, EstimateId = "Services test", TotalEstimatedCost = 1000, StartedAt = DateTime.Now };
    JobServices.Assign(db, serviceJob, JobServices.Offered.Take(2));
    db.Add(serviceJob); db.SaveChanges();
    Check(db.JobTasks.Count(t => t.JobOrderId == serviceJob.JobOrderId && t.Status == "Pending") == 2, "Requested services create linked pending tasks atomically");
    Check(serviceJob.EstimateNumber == $"EST-{serviceJob.JobOrderId:D6}", "Estimate reference uses the saved database identity");
    Check(new JobOrder { EstimateId = "EST-2026-0030 / Customer Name / Toyota Vios" }.VehicleDisplay == "Toyota Vios (legacy reference)", "Structured legacy vehicle reference remains visible");
    Check(new JobOrder { EstimateId = "CUSTOMER/UNKNOWN" }.VehicleDisplay == "Vehicle details missing", "Ambiguous references do not invent vehicle details");
    Reject(() => { using var c = Create(); c.JobTasks.First(t => t.JobOrderId == serviceJob.JobOrderId).Status = "Ongoing"; c.SaveChanges(); }, "Queued jobs cannot start tasks");
    Reject(() => { using var c = Create(); c.JobTasks.First(t => t.JobOrderId == serviceJob.JobOrderId).Status = "Done"; c.SaveChanges(); }, "Queued jobs cannot complete tasks");
    using (var c = Create())
    {
        var saved = c.JobOrders.Find(serviceJob.JobOrderId);
        JobServices.Assign(c, saved, JobServices.Offered.Take(2)); c.Update(saved); c.SaveChanges();
        Check(c.JobTasks.Count(t => t.JobOrderId == saved.JobOrderId) == 2, "Resaving services does not duplicate tasks");
        JobServices.Assign(c, saved, JobServices.Offered.Take(3)); c.Update(saved); c.SaveChanges();
        Check(c.JobTasks.Count(t => t.JobOrderId == saved.JobOrderId) == 3, "Adding a service creates only its new task");
        Reject(() => JobServices.Assign(c, saved, JobServices.Offered.Take(1)), "Existing service history cannot be removed");
        saved.Status = "Completed";
        Reject(() => JobServices.Assign(c, saved, JobServices.Offered.Take(4)), "Closed jobs cannot accept new services");
    }
    Reject(() => JobServices.Assign(db, new JobOrder(), Array.Empty<string>()), "New order requires a requested service");
    var job = new JobOrder { CustomerId = customer.CustomerId, EstimateId = "Test", TotalEstimatedCost = 1000, StartedAt = DateTime.Now.AddHours(-2) };
    db.Add(job); db.SaveChanges();
    int id = job.JobOrderId;
    void Stage(string status) { using var c = Create(); c.FinalInspectionConfirmed = status == "Completed"; c.JobOrders.Find(id).Status = status; c.SaveChanges(); }
    void Pay(decimal amount, bool credit = false) { using var c = Create(); c.Add(new Payment { JobOrderId = id, AmountPaid = amount, AllowCustomerCredit = credit }); c.SaveChanges(); }
    Reject(() => Stage("Completed"), "Skipped stages rejected");
    Reject(() => Stage("In Progress"), "50 percent deposit enforced");
    Reject(() => { using var c = Create(); c.Add(new Payment { JobOrderId = id, AmountPaid = 100, PaymentMethod = "Unknown" }); c.SaveChanges(); }, "Unknown payment method rejected");
    Reject(() => { using var c = Create(); c.Add(new Payment { JobOrderId = id, AmountPaid = 100, IsVoided = true, VoidReason = "Invalid creation" }); c.SaveChanges(); }, "New payment cannot start voided");
    Pay(499); Reject(() => Stage("In Progress"), "Partial deposit below 50 percent rejected");
    Pay(1);
    CRM.infrastructure.data.WorkshopRules.RequireAssignedMechanic = true;
    Reject(() => Stage("In Progress"), "Unassigned job blocked from starting work");
    CRM.infrastructure.data.WorkshopRules.RequireAssignedMechanic = false;
    using (var c = Create()) { var j = c.JobOrders.Find(id); j.AssignedMechanicUserId = "juan.gomez"; j.AssignedMechanic = "Juan Gomez"; c.SaveChanges(); }
    Stage("In Progress");
    var task = new JobTask { JobOrderId = id, ServiceId = "Inspection" }; db.Add(task); db.SaveChanges();
    Reject(() => Stage("QA Checking"), "Separate QA stage is retired");
    Reject(() => Stage("Completed"), "Pending task blocks completion");
    task.Status = "Done"; db.SaveChanges();
    Reject(() => { using var c = Create(); c.JobOrders.Find(id).Status = "Completed"; c.SaveChanges(); }, "Completion requires inspection confirmation");
    // Simulate a historical row created before the four-stage rollout.
    db.Database.ExecuteSqlInterpolated($"UPDATE JobOrders SET Status = 'QA Checking' WHERE JobOrderId = {id}");
    Stage("Completed");
    Reject(() => { using var c = Create(); var j = c.JobOrders.Find(id); j.AssignedMechanicUserId = "pedro.santos"; j.AssignedMechanic = "Pedro Santos"; c.SaveChanges(); }, "Completed job cannot be reassigned");
    using (var c = Create())
    {
        var completed = c.JobOrders.Find(id);
        Check(completed.CompletedAt.HasValue && completed.TurnaroundHours >= 2, "Completion timestamp and lead time");
        Check(completed.Status == "Completed" && c.AuditEntries.Any(a => a.Action == "FinalInspectionApproved"), "Historical QA job completes with an audited inspection confirmation");
    }
    Reject(() => Stage("Released"), "Outstanding balance blocks release");
    Reject(() => Pay(501), "Overpayment rejected");
    Pay(500); Stage("Released");
    Reject(() => { using var c = Create(); c.Payments.First(p => p.JobOrderId == id).AmountPaid += 1; c.SaveChanges(); }, "Saved receipt amount is immutable");
    Reject(() => { using var c = Create(); c.Payments.First(p => p.JobOrderId == id).PaymentMethod = "Card"; c.SaveChanges(); }, "Saved receipt method is immutable");
    Pay(50, true);
    using (var c = Create()) Check(c.JobOrders.Find(id).CustomerCredit == 50 && c.JobOrders.Find(id).OutstandingBalance == 0, "Flagged excess becomes customer credit");
    Reject(() => { using var c = Create(); c.Remove(c.Payments.OrderBy(p => p.PaymentId).First()); c.SaveChanges(); }, "Cannot unpay a released job");
    var feedback = new Feedback { JobOrderId = id, RatingScore = 2, AreasForImprovement = "Inspect again" };
    db.Add(feedback); await db.SaveChangesAsync();
    Check(db.Complaints.Single().IsUrgent && db.Complaints.Single().UnderWarrantyService && db.Complaints.Single().ResolutionStatus == "Open", "Low feedback opens urgent warranty complaint");
    feedback.PositiveRemarks = "Updated"; db.SaveChanges();
    Check(db.Complaints.Count() == 1, "Feedback edits do not duplicate escalation");
    customer.IsActive = false; db.SaveChanges();
    Reject(() => { using var c = Create(); c.Add(new JobOrder { CustomerId = customer.CustomerId }); c.SaveChanges(); }, "Inactive customer cannot open a job");
    Reject(() => { using var c = Create(); c.Remove(c.Customers.Find(customer.CustomerId)); c.SaveChanges(); }, "Customer history cannot be deleted");
    Reject(() => { using var c = Create(); c.Remove(c.JobOrders.Find(id)); c.SaveChanges(); }, "Financial job cannot be deleted");
    var inquiry = new Inquiry { CustomerName = "New lead", ContactNumber = "09179998888", MessageDetails = "Repair brakes", VehicleDetails = "Toyota / ABC123" };
    db.Add(inquiry); db.SaveChanges();
    int converted = InquiryConversion.Convert(Create, inquiry.InquiryId, null, "");
    Check(converted == InquiryConversion.Convert(Create, inquiry.InquiryId, null, ""), "Conversion is idempotent");
    using (var c = Create())
    {
        var j = c.JobOrders.Find(converted);
        Check(j.InquiryNotes == "Repair brakes" && j.VehicleDetails == "Toyota / ABC123" && c.Inquiries.Find(inquiry.InquiryId).Status == "Converted", "Conversion preserves intake details and links the customer");
    }
    using (var a = Create())
    using (var b = Create())
    {
        var first = a.Customers.Find(customer.CustomerId);
        var stale = b.Customers.Find(customer.CustomerId);
        first.Email = "first@example.com"; a.SaveChanges();
        stale.Email = "stale@example.com";
        try { b.SaveChanges(); throw new Exception("Stale update was accepted"); }
        catch (DbUpdateConcurrencyException) { Console.WriteLine("PASS: Stale edits are rejected"); }
    }
    using (var c = Create())
    {
        var credit = c.Payments.Single(p => p.AllowCustomerCredit);
        credit.IsVoided = true; credit.VoidReason = "Customer credit recorded in error";
        await c.SaveChangesAsync();
        Check(c.Payments.Count() == 4 && c.JobOrders.Find(id).OutstandingBalance == 0, "Void preserves history and recalculates billing");
        Check(credit.VoidedAtUtc.HasValue && c.AuditEntries.Any(a => a.EntityType == "Payment"), "Voiding stores timestamp and audit history");
    }

    // --- 3NF Normalization & Catalog Price Isolation Checks ---
    using (var c = Create())
    {
        var vehicle = new Vehicle { PlateNumber = "XYZ-9999", Make = "Toyota", Model = "Corolla", CustomerId = customer.CustomerId };
        c.Vehicles.Add(vehicle);
        var svc = new WorkshopService { Name = "Full Brake Overhaul", CurrentPrice = 3500m, IsActive = true };
        c.Services.Add(svc);
        c.SaveChanges();

        var est = new Estimate { CustomerId = customer.CustomerId, VehicleId = vehicle.VehicleId, Status = "Draft", TotalAmount = 3500m };
        est.Lines.Add(new EstimateLine { ServiceCatalogId = svc.ServiceId, Description = svc.Name, Quantity = 1, UnitPrice = 3500m, LineTotal = 3500m });
        c.Estimates.Add(est);
        c.SaveChanges();

        Check(c.Vehicles.Any(v => v.PlateNumber == "XYZ-9999"), "3NF: Vehicle entity created and persisted");
        Check(c.Estimates.Include(e => e.Lines).Any(e => e.Lines.Count == 1 && e.TotalAmount == 3500m), "3NF: Estimate with lines persisted");

        // Catalog price change does not change existing task or estimate line prices
        svc.CurrentPrice = 4500m;
        c.SaveChanges();
        var reloadedEst = c.Estimates.Include(e => e.Lines).Single(e => e.EstimateId == est.EstimateId);
        Check(reloadedEst.Lines.First().UnitPrice == 3500m, "3NF: Service catalog price changes do not overwrite past agreed prices");
    }

    // --- Workshop Appointment Workflow Checks ---
    using (var c = Create())
    {
        var appt = new Appointment
        {
            CustomerId = customer.CustomerId,
            ScheduledStart = DateTime.Today.AddDays(1).AddHours(9),
            ScheduledEnd = DateTime.Today.AddDays(1).AddHours(10),
            RequestedServices = "Full Brake Overhaul",
            Status = "Scheduled"
        };
        c.Appointments.Add(appt);
        c.SaveChanges();

        // Reschedule
        appt.ScheduledStart = DateTime.Today.AddDays(2).AddHours(14);
        c.SaveChanges();
        Check(c.AuditEntries.Any(a => a.EntityType == "Appointment" && a.Action == "Rescheduled"), "Appointment: Rescheduling recorded in audit history");

        // Cancel requires reason
        Reject(() => { using var c2 = Create(); var a2 = c2.Appointments.Find(appt.AppointmentId); a2.Status = "Cancelled"; a2.CancellationReason = ""; c2.SaveChanges(); }, "Appointment: Cancellation requires reason");

        // Check-in creates job order idempotently
        var activeCust = new Customer { FullName = "Active Appointment Customer", ContactNumber = "09172223333", IsActive = true };
        c.Customers.Add(activeCust);
        c.SaveChanges();

        var apptForJob = new Appointment
        {
            CustomerId = activeCust.CustomerId,
            ScheduledStart = DateTime.Today.AddHours(2),
            RequestedServices = "Full Brake Overhaul",
            Status = "Scheduled"
        };
        c.Appointments.Add(apptForJob);
        c.SaveChanges();

        var newJob = new JobOrder
        {
            CustomerId = activeCust.CustomerId,
            VehicleDetails = "Toyota Corolla",
            TotalEstimatedCost = 3500m,
            Status = "In Queue",
            EstimateId = "EST-APPT-001"
        };
        c.JobOrders.Add(newJob);
        c.SaveChanges();

        apptForJob.Status = "Checked In";
        apptForJob.LinkedJobOrderId = newJob.JobOrderId;
        c.SaveChanges();
        Check(c.Appointments.Find(apptForJob.AppointmentId).Status == "Checked In", "Appointment: Checked In successfully");
        Reject(() => { using var c2 = Create(); var a2 = c2.Appointments.Find(apptForJob.AppointmentId); a2.LinkedJobOrderId = 99999; c2.SaveChanges(); }, "Appointment: Cannot re-link checked in appointment to duplicate job");
    }

    // --- Job Order Cancellation Checks ---
    using (var c = Create())
    {
        var activeCustomer = c.Customers.First(cust => cust.IsActive);

        // 1. Cancel completed/released job is rejected
        Reject(() => { using var c2 = Create(); var j2 = c2.JobOrders.Find(id); j2.Status = "Cancelled"; j2.CancellationReason = "Customer changed mind"; c2.SaveChanges(); }, "Cancellation: Completed or released jobs cannot be cancelled");

        // 2. In-queue job with payments/deposit is blocked requiring refund review
        var queueJobWithDeposit = new JobOrder { CustomerId = activeCustomer.CustomerId, EstimateId = "Deposit-Job", TotalEstimatedCost = 1000, Status = "In Queue" };
        queueJobWithDeposit.Tasks.Add(new JobTask { ServiceId = "Diagnostic", Status = "Pending" });
        c.JobOrders.Add(queueJobWithDeposit);
        c.SaveChanges();

        c.Payments.Add(new Payment { JobOrderId = queueJobWithDeposit.JobOrderId, AmountPaid = 500 });
        c.SaveChanges();

        Reject(() => { using var c2 = Create(); var j2 = c2.JobOrders.Find(queueJobWithDeposit.JobOrderId); j2.Status = "Cancelled"; j2.CancellationReason = "Decline after deposit"; c2.SaveChanges(); }, "Cancellation: In-queue jobs with deposit require refund review and are blocked from direct cancellation");

        // 3. Waiting job without payment: allowed with reason
        var cancelJob = new JobOrder { CustomerId = activeCustomer.CustomerId, EstimateId = "Cancel-Test", TotalEstimatedCost = 500, Status = "In Queue" };
        cancelJob.Tasks.Add(new JobTask { ServiceId = "Diagnostic", Status = "Pending" });
        c.JobOrders.Add(cancelJob);
        c.SaveChanges();

        Reject(() => { using var c2 = Create(); var j = c2.JobOrders.Find(cancelJob.JobOrderId); j.Status = "Cancelled"; j.CancellationReason = ""; c2.SaveChanges(); }, "Cancellation: Cancellation requires non-empty reason");

        cancelJob.Status = "Cancelled";
        cancelJob.CancellationReason = "Customer declined repair estimate";
        cancelJob.CancelledBy = "service.advisor";
        cancelJob.CancelledAtUtc = DateTime.UtcNow;
        c.SaveChanges();

        var reloaded = c.JobOrders.Include(j => j.Tasks).Single(j => j.JobOrderId == cancelJob.JobOrderId);
        Check(reloaded.Status == "Cancelled" && reloaded.Tasks.All(t => t.Status == "Cancelled"), "Cancellation: Unfinished tasks marked Cancelled");

        // 4. Cancelled job cannot progress
        Reject(() => { using var c2 = Create(); var j = c2.JobOrders.Find(cancelJob.JobOrderId); j.Status = "In Progress"; c2.SaveChanges(); }, "Cancellation: Cancelled job cannot transition to In Progress");
    }

    // --- Mechanic Eligibility & Specialty Compatibility Checks ---
    using (var c = Create())
    {
        var activeCustomer = c.Customers.First(cust => cust.IsActive);

        // Add roster members with various roles
        var cashier = new WorkshopStaff { StaffId = "cashier.test", DisplayName = "Cashier Test", Role = "Cashier", IsActive = true };
        var advisor = new WorkshopStaff { StaffId = "advisor.test", DisplayName = "Advisor Test", Role = "Service Advisor", IsActive = true };
        var inactiveMech = new WorkshopStaff { StaffId = "inactive.mech.test", DisplayName = "Inactive Mech", Role = "Automotive Mechanic", IsActive = false };
        var autoMech = new WorkshopStaff { StaffId = "auto.mech.test", DisplayName = "Auto Mech Test", Role = "Automotive Mechanic", IsActive = true };
        var detailTech = new WorkshopStaff { StaffId = "detail.tech.test", DisplayName = "Detail Tech Test", Role = "Detailing Technician", IsActive = true };
        var leadMech = new WorkshopStaff { StaffId = "lead.mech.test", DisplayName = "Lead Mech Test", Role = "Lead Mechanic", IsActive = true };

        c.Staff.AddRange(cashier, advisor, inactiveMech, autoMech, detailTech, leadMech);
        c.SaveChanges();

        // 1. Ineligible staff (Cashier, Advisor, Inactive) rejected upon assignment to a job
        var mechJob = new JobOrder { CustomerId = activeCustomer.CustomerId, EstimateId = "EST-MECH-01", TotalEstimatedCost = 2000m, Status = "In Queue" };
        mechJob.Tasks.Add(new JobTask { ServiceId = "Brake inspection", Status = "Pending" });
        c.JobOrders.Add(mechJob);
        c.SaveChanges();

        Reject(() => { using var c2 = Create(); var j = c2.JobOrders.Find(mechJob.JobOrderId); j.AssignedMechanicUserId = cashier.StaffId; c2.SaveChanges(); }, "Eligibility: Cashier rejected on job assignment");
        Reject(() => { using var c2 = Create(); var j = c2.JobOrders.Find(mechJob.JobOrderId); j.AssignedMechanicUserId = advisor.StaffId; c2.SaveChanges(); }, "Eligibility: Service Advisor rejected on job assignment");
        Reject(() => { using var c2 = Create(); var j = c2.JobOrders.Find(mechJob.JobOrderId); j.AssignedMechanicUserId = inactiveMech.StaffId; c2.SaveChanges(); }, "Eligibility: Inactive mechanic rejected on job assignment");

        // 2. Specialty compatibility: Detailing Technician rejected on Mechanical job
        Reject(() => { using var c2 = Create(); var j = c2.JobOrders.Find(mechJob.JobOrderId); j.AssignedMechanicUserId = detailTech.StaffId; c2.SaveChanges(); }, "Specialty: Detailing Technician rejected on Mechanical job");

        // 3. Specialty compatibility: Automotive Mechanic accepted on Mechanical job
        using (var c2 = Create())
        {
            var j = c2.JobOrders.Find(mechJob.JobOrderId);
            j.AssignedMechanicUserId = autoMech.StaffId;
            j.AssignedMechanic = autoMech.DisplayName;
            c2.SaveChanges();
            Check(j.AssignedMechanicUserId == autoMech.StaffId, "Specialty: Automotive Mechanic accepted on Mechanical job");
        }

        // 4. Detailing job checks
        var detailJob = new JobOrder { CustomerId = activeCustomer.CustomerId, EstimateId = "EST-DETAIL-01", TotalEstimatedCost = 2500m, Status = "In Queue" };
        detailJob.Tasks.Add(new JobTask { ServiceId = "Exterior detailing", Status = "Pending" });
        c.JobOrders.Add(detailJob);
        c.SaveChanges();

        // Automotive Mechanic rejected on Detailing job
        Reject(() => { using var c2 = Create(); var j = c2.JobOrders.Find(detailJob.JobOrderId); j.AssignedMechanicUserId = autoMech.StaffId; c2.SaveChanges(); }, "Specialty: Automotive Mechanic rejected on Detailing job");

        // Detailing Technician accepted on Detailing job
        using (var c2 = Create())
        {
            var j = c2.JobOrders.Find(detailJob.JobOrderId);
            j.AssignedMechanicUserId = detailTech.StaffId;
            j.AssignedMechanic = detailTech.DisplayName;
            c2.SaveChanges();
            Check(j.AssignedMechanicUserId == detailTech.StaffId, "Specialty: Detailing Technician accepted on Detailing job");
        }

        // 5. Lead Mechanic accepted on Mechanical, Detailing, and Mixed jobs
        using (var c2 = Create())
        {
            var jMech = c2.JobOrders.Find(mechJob.JobOrderId);
            jMech.AssignedMechanicUserId = leadMech.StaffId;
            jMech.AssignedMechanic = leadMech.DisplayName;
            c2.SaveChanges();
            Check(jMech.AssignedMechanicUserId == leadMech.StaffId, "Specialty: Lead Mechanic accepted on Mechanical job");

            var jDetail = c2.JobOrders.Find(detailJob.JobOrderId);
            jDetail.AssignedMechanicUserId = leadMech.StaffId;
            jDetail.AssignedMechanic = leadMech.DisplayName;
            c2.SaveChanges();
            Check(jDetail.AssignedMechanicUserId == leadMech.StaffId, "Specialty: Lead Mechanic accepted on Detailing job");
        }

        // Mixed job requires Lead Mechanic
        var mixedJob = new JobOrder { CustomerId = activeCustomer.CustomerId, EstimateId = "EST-MIXED-01", TotalEstimatedCost = 4000m, Status = "In Queue" };
        mixedJob.Tasks.Add(new JobTask { ServiceId = "Oil and filter change", Status = "Pending" });
        mixedJob.Tasks.Add(new JobTask { ServiceId = "Exterior detailing", Status = "Pending" });
        c.JobOrders.Add(mixedJob);
        c.SaveChanges();

        Reject(() => { using var c2 = Create(); var j = c2.JobOrders.Find(mixedJob.JobOrderId); j.AssignedMechanicUserId = autoMech.StaffId; c2.SaveChanges(); }, "Specialty: Automotive Mechanic rejected on Mixed job");
        Reject(() => { using var c2 = Create(); var j = c2.JobOrders.Find(mixedJob.JobOrderId); j.AssignedMechanicUserId = detailTech.StaffId; c2.SaveChanges(); }, "Specialty: Detailing Technician rejected on Mixed job");

        using (var c2 = Create())
        {
            var j = c2.JobOrders.Find(mixedJob.JobOrderId);
            j.AssignedMechanicUserId = leadMech.StaffId;
            j.AssignedMechanic = leadMech.DisplayName;
            c2.SaveChanges();
            Check(j.AssignedMechanicUserId == leadMech.StaffId, "Specialty: Lead Mechanic accepted on Mixed job");
        }
    }

    // --- PLAN 1: Job Order Cancellation & Refund Management ---
    using (var c = Create())
    {
        var cust = c.Customers.First(cust => cust.IsActive);

        // 1. Unpaid In-Queue job: direct cancellation allowed
        var directCancelJob = new JobOrder { CustomerId = cust.CustomerId, EstimateId = "EST-DIR-01", TotalEstimatedCost = 1500m, Status = "In Queue" };
        directCancelJob.Tasks.Add(new JobTask { ServiceId = "Diagnostic", Status = "Pending" });
        c.JobOrders.Add(directCancelJob);
        c.SaveChanges();

        var directElig = CancellationRefundService.EvaluateEligibility(c, directCancelJob);
        Check(directElig.CanDirectCancel && !directElig.RequiresReview, "Plan 1: Unpaid In-Queue job is eligible for direct cancellation");

        CancellationRefundService.DirectCancelUnpaidJob(c, directCancelJob.JobOrderId, "Customer requested cancellation", "Customer requested cancellation", "Advisor Test");
        var reloadedDirect = c.JobOrders.Find(directCancelJob.JobOrderId)!;
        Check(reloadedDirect.Status == "Cancelled" && reloadedDirect.CancelledBy == "Advisor Test", "Plan 1: Direct cancellation completes immediately");

        // 2. Paid In-Queue job: direct cancellation rejected; requires management review
        var paidJob = new JobOrder { CustomerId = cust.CustomerId, EstimateId = "EST-PAID-01", TotalEstimatedCost = 2000m, Status = "In Queue" };
        paidJob.Tasks.Add(new JobTask { ServiceId = "Oil and filter change", Status = "Pending" });
        c.JobOrders.Add(paidJob);
        c.SaveChanges();

        c.Payments.Add(new Payment { JobOrderId = paidJob.JobOrderId, AmountPaid = 1000m, PaymentMethod = "Cash" });
        c.SaveChanges();

        var paidElig = CancellationRefundService.EvaluateEligibility(c, paidJob);
        Check(!paidElig.CanDirectCancel && paidElig.RequiresReview, "Plan 1: Paid In-Queue job requires management review");

        Reject(() => CancellationRefundService.DirectCancelUnpaidJob(c, paidJob.JobOrderId, "Customer cancelled", "explanation", "Advisor Test"), "Plan 1: Direct cancellation rejected on paid job");

        // 3. Submit cancellation request
        var req = CancellationRefundService.SubmitCancellationRequest(c, paidJob.JobOrderId, "Scheduling conflict", "Customer will be out of town for 2 weeks", "Advisor Test", 0m, 1000m);
        Check(req.Status == "Pending Review" && req.TotalPaidAtRequest == 1000m && req.ProposedRefund == 1000m, "Plan 1: Cancellation request submitted with correct amounts");

        // 4. Workshop actions suspended while cancellation request is pending review
        Reject(() => { using var c2 = Create(); c2.JobOrders.Find(paidJob.JobOrderId)!.Status = "In Progress"; c2.SaveChanges(); }, "Plan 1: Workshop stage progression suspended while cancellation review pending");
        Reject(() => { using var c2 = Create(); c2.JobTasks.Add(new JobTask { JobOrderId = paidJob.JobOrderId, ServiceId = "Battery service" }); c2.SaveChanges(); }, "Plan 1: Task addition suspended while cancellation review pending");
        Reject(() => { using var c2 = Create(); c2.Payments.Add(new Payment { JobOrderId = paidJob.JobOrderId, AmountPaid = 500m }); c2.SaveChanges(); }, "Plan 1: New payment suspended while cancellation review pending");

        // 5. Duplicate cancellation request rejected
        Reject(() => CancellationRefundService.SubmitCancellationRequest(c, paidJob.JobOrderId, "Duplicate request", "Another reason", "Advisor Test"), "Plan 1: Duplicate cancellation request rejected");

        // 6. Management review decision: Approve with refund
        CancellationRefundService.ReviewCancellationRequest(c, req.RequestId, "Approve", "Authorized full refund per customer request", "Manager Test", 1000m);
        var reloadedReq = c.JobCancellationRequests.Find(req.RequestId)!;
        Check(reloadedReq.Status == "Approved" || reloadedReq.Status == "Pending Refund", "Plan 1: Management review decision approved");

        // 7. Record Refund: capped at paid amount, submission ID uniqueness enforced
        var subGuid = Guid.NewGuid();
        var refund = CancellationRefundService.RecordRefund(c, paidJob.JobOrderId, req.RequestId, null, 1000m, "Cash", cust.FullName, "Full deposit refund for cancelled job", "Manager Test", subGuid);
        Check(refund.Amount == 1000m && refund.RefundMethod == "Cash", "Plan 1: Refund recorded successfully");

        // Cannot exceed remaining refundable amount
        Reject(() => CancellationRefundService.RecordRefund(c, paidJob.JobOrderId, req.RequestId, null, 1m, "Cash", cust.FullName, "Excess refund", "Manager Test", Guid.NewGuid()), "Plan 1: Excess refund rejected");

        // Duplicate submission ID rejected
        Reject(() => CancellationRefundService.RecordRefund(c, paidJob.JobOrderId, req.RequestId, null, 1m, "Cash", cust.FullName, "Duplicate subId", "Manager Test", subGuid), "Plan 1: Duplicate refund submission ID rejected");

        // Reload job and check final status
        var finalJob = c.JobOrders.Find(paidJob.JobOrderId)!;
        Check(finalJob.Status == "Cancelled", "Plan 1: Job transitioned to Cancelled upon full refund and review completion");

        // Optimization: Cannot void a payment that has linked refunds
        var refundedPayment = c.Payments.First(p => p.JobOrderId == paidJob.JobOrderId);
        Reject(() => { using var cVoid = Create(); var p = cVoid.Payments.Find(refundedPayment.PaymentId)!; p.IsVoided = true; p.VoidReason = "Attempt void refunded payment"; cVoid.SaveChanges(); }, "Optimization: Payment with linked refund cannot be voided");

        // Optimization: Stale approval / unapproved request blocks refund
        var staleJob = new JobOrder { CustomerId = cust.CustomerId, EstimateId = "EST-STALE-01", TotalEstimatedCost = 3000m, Status = "In Queue" };
        c.JobOrders.Add(staleJob); c.SaveChanges();
        c.Payments.Add(new Payment { JobOrderId = staleJob.JobOrderId, AmountPaid = 1500m, PaymentMethod = "GCash" }); c.SaveChanges();
        var staleReq = CancellationRefundService.SubmitCancellationRequest(c, staleJob.JobOrderId, "Customer request", "Change of mind", "Advisor Test", 0m, 1500m);
        CancellationRefundService.ReviewCancellationRequest(c, staleReq.RequestId, "Reject", "Denied per manager policy", "Manager Test", 0m);
        Reject(() => CancellationRefundService.RecordRefund(c, staleJob.JobOrderId, staleReq.RequestId, null, 500m, "Cash", cust.FullName, "Refund on rejected request", "Manager Test", Guid.NewGuid()), "Optimization: Refund rejected against unapproved/stale request");

        // Optimization: Refund exceeding approved cancellation request limit is rejected
        var partReqJob = new JobOrder { CustomerId = cust.CustomerId, EstimateId = "EST-PART-01", TotalEstimatedCost = 4000m, Status = "In Queue" };
        c.JobOrders.Add(partReqJob); c.SaveChanges();
        c.Payments.Add(new Payment { JobOrderId = partReqJob.JobOrderId, AmountPaid = 2000m, PaymentMethod = "Cash" }); c.SaveChanges();
        var partReq = CancellationRefundService.SubmitCancellationRequest(c, partReqJob.JobOrderId, "Customer request", "Parts unavailable", "Advisor Test", 500m, 1500m);
        CancellationRefundService.ReviewCancellationRequest(c, partReq.RequestId, "Approve", "Approved 1000 refund, 500 inspection fee kept", "Manager Test", 1000m);
        Reject(() => CancellationRefundService.RecordRefund(c, partReqJob.JobOrderId, partReq.RequestId, null, 1500m, "Cash", cust.FullName, "Exceeding approved limit", "Manager Test", Guid.NewGuid()), "Optimization: Refund exceeding approved cancellation limit rejected");
        var validPartRefund = CancellationRefundService.RecordRefund(c, partReqJob.JobOrderId, partReq.RequestId, null, 1000m, "Cash", cust.FullName, "Approved partial refund", "Manager Test", Guid.NewGuid());
        Check(validPartRefund.Amount == 1000m, "Optimization: Partial approved refund successfully recorded");
    }

    // --- PLAN 2: Appointment Rescheduling & Cancellation ---
    using (var c = Create())
    {
        var cust = c.Customers.First(cust => cust.IsActive);
        var appt = new Appointment
        {
            CustomerId = cust.CustomerId,
            ScheduledStart = DateTime.Today.AddDays(1).AddHours(10),
            ScheduledEnd = DateTime.Today.AddDays(1).AddHours(11),
            RequestedServices = "Brake inspection",
            Status = "Scheduled"
        };
        c.Appointments.Add(appt);
        c.SaveChanges();

        // 1. Rescheduling updates date and writes to AppointmentHistory
        DateTime newTime = DateTime.Today.AddDays(3).AddHours(14);
        AppointmentService.Reschedule(c, appt.AppointmentId, newTime, "Customer requested afternoon slot", "service.advisor");
        var reloadedAppt = c.Appointments.Find(appt.AppointmentId)!;
        Check(reloadedAppt.ScheduledStart == newTime, "Plan 2: Appointment rescheduled date updated");
        Check(c.AppointmentHistories.Any(h => h.AppointmentId == appt.AppointmentId && h.Action == "Rescheduled" && h.NewScheduledStart == newTime), "Plan 2: AppointmentHistory recorded for reschedule");

        // Short reason rejected
        Reject(() => AppointmentService.Reschedule(c, appt.AppointmentId, DateTime.Today.AddDays(4), "no", "service.advisor"), "Plan 2: Reschedule rejects short reason");

        // 2. Cancellation updates status, records history and audit
        AppointmentService.Cancel(c, appt.AppointmentId, "Customer requested cancellation", "Customer sold vehicle", "service.advisor");
        var cancelledAppt = c.Appointments.Find(appt.AppointmentId)!;
        Check(cancelledAppt.Status == "Cancelled" && cancelledAppt.CancellationReason.Contains("sold vehicle"), "Plan 2: Appointment cancelled with reason");
        Check(c.AppointmentHistories.Any(h => h.AppointmentId == appt.AppointmentId && h.Action == "Cancelled"), "Plan 2: AppointmentHistory recorded for cancellation");

        // Cancelled appointment cannot be rescheduled or cancelled again
        Reject(() => AppointmentService.Reschedule(c, appt.AppointmentId, DateTime.Today.AddDays(5), "Try reschedule", "service.advisor"), "Plan 2: Cancelled appointment cannot be rescheduled");
        Reject(() => AppointmentService.Cancel(c, appt.AppointmentId, "Customer requested cancellation", "Already cancelled", "service.advisor"), "Plan 2: Cancelled appointment cannot be re-cancelled");

        // 3. Appointment with linked Job Order cannot be cancelled via AppointmentService (must use Job Order cancellation)
        var existingJob = c.JobOrders.First();
        var apptWithJob = new Appointment
        {
            CustomerId = cust.CustomerId,
            ScheduledStart = DateTime.Today.AddDays(1).AddHours(8),
            RequestedServices = "Diagnostic",
            Status = "Scheduled",
            LinkedJobOrderId = existingJob.JobOrderId
        };
        c.Appointments.Add(apptWithJob);
        c.SaveChanges();

        Reject(() => AppointmentService.Cancel(c, apptWithJob.AppointmentId, "Scheduling conflict", "Cancel directly", "service.advisor"), "Plan 2: Appointment with linked Job Order blocks direct cancellation");

        // 4. No-show tracking
        var apptNoShow = new Appointment
        {
            CustomerId = cust.CustomerId,
            ScheduledStart = DateTime.Today.AddHours(-1),
            RequestedServices = "Battery check",
            Status = "Scheduled"
        };
        c.Appointments.Add(apptNoShow);
        c.SaveChanges();

        AppointmentService.MarkNoShow(c, apptNoShow.AppointmentId, "service.advisor");
        var reloadedNoShow = c.Appointments.Find(apptNoShow.AppointmentId)!;
        Check(reloadedNoShow.Status == "No Show", "Plan 2: Appointment marked No Show");
        Check(c.AppointmentHistories.Any(h => h.AppointmentId == apptNoShow.AppointmentId && h.Action == "No Show"), "Plan 2: AppointmentHistory recorded for No Show");
    }

    // --- PLAN 3: Contact Number Formatting & Validation ---
    {
        // 1. Normalization
        Check(ContactNumberHelper.Normalize("+639171234567") == "09171234567", "Plan 3: +639 normalized to 09");
        Check(ContactNumberHelper.Normalize("639171234567") == "09171234567", "Plan 3: 639 normalized to 09");
        Check(ContactNumberHelper.Normalize("09171234567") == "09171234567", "Plan 3: 09 mobile preserved");
        Check(ContactNumberHelper.Normalize("09XX-001-0099") == "09XX-001-0099", "Plan 3: Historical placeholder preserved intact");

        // 2. Fictional / Placeholder detection
        Check(ContactNumberHelper.IsFictionalOrPlaceholder("09XX-001-0099"), "Plan 3: 09XX placeholder detected");
        Check(!ContactNumberHelper.IsFictionalOrPlaceholder("09171234567"), "Plan 3: Real mobile not flagged as placeholder");

        // 3. Validation
        var valGood = ContactNumberHelper.Validate("09171234567");
        Check(valGood.IsValid, "Plan 3: Valid 09 mobile passes validation");

        var valPlus = ContactNumberHelper.Validate("+639171234567");
        Check(valPlus.IsValid, "Plan 3: Valid +639 mobile passes validation");

        var valLetters = ContactNumberHelper.Validate("0917ABC4567");
        Check(!valLetters.IsValid, "Plan 3: Contact number with letters rejected");

        var valNewPlaceholder = ContactNumberHelper.Validate("09XX-123-4567", allowExistingPlaceholder: false);
        Check(!valNewPlaceholder.IsValid, "Plan 3: Assigning new placeholder rejected");

        var valExistingPlaceholder = ContactNumberHelper.Validate("09XX-123-4567", allowExistingPlaceholder: true);
        Check(valExistingPlaceholder.IsValid, "Plan 3: Existing placeholder allowed on unchanged record");

        var valTooShort = ContactNumberHelper.Validate("12345");
        Check(!valTooShort.IsValid, "Plan 3: Too short contact number rejected");

        // 4. Matching for search
        Check(ContactNumberHelper.Matches("0917-123-4567", "09171234567"), "Plan 3: Unformatted search query matches formatted contact");
        Check(ContactNumberHelper.Matches("09171234567", "1234"), "Plan 3: Substring search matches contact");

        // 5. Retention outreach eligibility
        Check(ContactNumberHelper.IsEligibleForOutreach("09171234567", isActive: true, isOptedOut: false), "Plan 3: Active valid contact eligible for outreach");
        Check(!ContactNumberHelper.IsEligibleForOutreach("09XX-001-0099", isActive: true, isOptedOut: false), "Plan 3: Placeholder contact excluded from outreach");
        Check(!ContactNumberHelper.IsEligibleForOutreach("09171234567", isActive: false, isOptedOut: false), "Plan 3: Inactive customer contact excluded from outreach");

        // 6. Classification & International/Local cross-matching
        Check(ContactNumberHelper.Classify("09171234567") == ContactNumberHelper.ClassificationValid, "Optimization: Local 09 mobile classified as Valid");
        Check(ContactNumberHelper.Classify("+639171234567") == ContactNumberHelper.ClassificationValid, "Optimization: International +639 mobile classified as Valid");
        Check(ContactNumberHelper.Classify("09XX-001-0099") == ContactNumberHelper.ClassificationDemo, "Optimization: 09XX placeholder classified as Demo");
        Check(ContactNumberHelper.Classify("12345") == ContactNumberHelper.ClassificationInvalid, "Optimization: Malformed phone classified as Invalid");
        Check(ContactNumberHelper.Classify("") == ContactNumberHelper.ClassificationMissing, "Optimization: Empty phone classified as Missing");
        Check(ContactNumberHelper.Matches("09171234567", "+639171234567"), "Optimization: Local contact matches +639 query");
        Check(ContactNumberHelper.Matches("+639171234567", "09171234567"), "Optimization: +639 contact matches local 09 query");
    }

    BusinessRules.Smoke.GridSortingAndLayoutTests.Run(Check);

    await BusinessRules.Smoke.JobOrderArchivingTests.RunAsync(db, Create, Check, Reject);

    await BusinessRules.Smoke.WorkshopIntegrityTests.RunAsync(db, Create, Check, Reject);

    if (!args.Contains("--tenant-a-only"))
        await BusinessRules.Smoke.MultiCompanyIsolationTests.RunAsync(Check, Reject);

    Check(!db.Database.HasPendingModelChanges(), "Migration matches model");
    Console.WriteLine("All business-rule checks passed.");
}
finally { db.Database.EnsureDeleted(); }

static void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS: " + name); }
static void Reject(Action action, string name)
{
    try { action(); } catch (InvalidOperationException) { Console.WriteLine("PASS: " + name); return; }
    throw new Exception("Expected rejection: " + name);
}
