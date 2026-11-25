using System;
using System.Linq;
using System.Threading.Tasks;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace BusinessRules.Smoke;

public static class MultiCompanyIsolationTests
{
    public static async Task RunAsync(Action<bool, string> check, Action<Action, string> reject)
    {
        Console.WriteLine("\n[STAGE: Multi-Company Schema Unification & Data Isolation Tests]");

        string suffix = Guid.NewGuid().ToString("N")[..8];
        string connA = $@"Server=(localdb)\MSSQLLocalDB;Database=Disposable_CompanyA_Iso_{suffix};Trusted_Connection=True;TrustServerCertificate=True";
        string connB = $@"Server=(localdb)\MSSQLLocalDB;Database=Disposable_CompanyB_Iso_{suffix};Trusted_Connection=True;TrustServerCertificate=True";
        string connC = $@"Server=(localdb)\MSSQLLocalDB;Database=Disposable_CompanyC_Iso_{suffix};Trusted_Connection=True;TrustServerCertificate=True";

        var optsA = new DbContextOptionsBuilder<MasterCrmDbContext>()
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory, MasterCrmModelCacheKeyFactory>()
            .UseSqlServer(connA).Options;
        var optsB = new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(connB).Options;
        var optsC = new DbContextOptionsBuilder<TenantCrmDbContext>().UseSqlServer(connC).Options;

        await using var dbA = new MasterCrmDbContext(optsA);
        await using var dbB = new TenantCrmDbContext(optsB);
        await using var dbC = new TenantCrmDbContext(optsC);

        try
        {
            // 1. Apply Migrations to All Three Disposable Databases
            await dbA.Database.MigrateAsync();
            await dbB.Database.MigrateAsync();
            await dbC.Database.MigrateAsync();

            check(!dbA.Database.HasPendingModelChanges(), "Phase 2: MasterCrmDbContext migration matches model");
            check(!dbB.Database.HasPendingModelChanges(), "Phase 2: TenantCrmDbContext (Company B) migration matches model");
            check(!dbC.Database.HasPendingModelChanges(), "Phase 2: TenantCrmDbContext (Company C) migration matches model");

            // 2. Validate Tenant Database Identity Singletons
            var identityA = await dbA.DatabaseIdentity.FirstOrDefaultAsync();
            check(identityA != null && identityA.TenantId == "tenant-a", "Phase 2: Company A identity is 'tenant-a'");

            // Configure Tenant B and Tenant C identities
            var identityB = await dbB.DatabaseIdentity.FirstOrDefaultAsync();
            if (identityB == null)
            {
                dbB.DatabaseIdentity.Add(new TenantDatabaseIdentity { Id = 1, TenantId = "tenant-b" });
                await dbB.SaveChangesAsync();
            }
            else
            {
                identityB.TenantId = "tenant-b";
                await dbB.SaveChangesAsync();
            }

            var identityC = await dbC.DatabaseIdentity.FirstOrDefaultAsync();
            if (identityC == null)
            {
                dbC.DatabaseIdentity.Add(new TenantDatabaseIdentity { Id = 1, TenantId = "tenant-c" });
                await dbC.SaveChangesAsync();
            }
            else
            {
                identityC.TenantId = "tenant-c";
                await dbC.SaveChangesAsync();
            }

            // 3. Verify Identity Check Constraint (Id must be 1)
            bool constraintThrew = false;
            try
            {
                using var testCtx = new MasterCrmDbContext(optsA);
                testCtx.Database.ExecuteSqlRaw("INSERT INTO [TenantDatabaseIdentity] ([Id], [TenantId]) VALUES (2, 'invalid-tenant');");
            }
            catch (Exception)
            {
                constraintThrew = true;
            }
            check(constraintThrew, "Phase 2: TenantDatabaseIdentity singleton constraint blocks Id != 1");

            // 4. Populate Overlapping Primary Keys across Company A, B, and C
            var branchA = await dbA.Branches.FirstAsync(b => b.BranchCode == "MAIN");
            var branchB = await dbB.Branches.FirstAsync(b => b.BranchCode == "MAIN");
            var branchC = await dbC.Branches.FirstAsync(b => b.BranchCode == "MAIN");

            // Company A Customer & Job (PK 1)
            var custA = new Customer { FullName = "Company A Customer", ContactNumber = "09170000001", Email = "cust.a@revcare.com" };
            dbA.Customers.Add(custA);
            await dbA.SaveChangesAsync();
            check(custA.CustomerId == 1, "Phase 2: Company A customer assigned PK 1");

            var vehA = new Vehicle { CustomerId = custA.CustomerId, Make = "Toyota", Model = "Fortuner 2024", PlateNumber = "A-AAA-01" };
            dbA.Vehicles.Add(vehA);
            await dbA.SaveChangesAsync();

            var jobA = new JobOrder
            {
                CustomerId = custA.CustomerId,
                VehicleId = vehA.VehicleId,
                EstimateId = "EST-A-000001",
                TotalEstimatedCost = 5000m,
                Status = "In Queue",
                BranchId = branchA.BranchId,
                StartedAt = DateTime.Now
            };
            dbA.JobOrders.Add(jobA);
            await dbA.SaveChangesAsync();
            check(jobA.JobOrderId == 1, "Phase 2: Company A job order assigned PK 1");

            // Company B Customer & Job (PK 1)
            var custB = new Customer { FullName = "Company B Customer", ContactNumber = "09180000002", Email = "cust.b@companyb.com" };
            dbB.Customers.Add(custB);
            await dbB.SaveChangesAsync();
            check(custB.CustomerId == 1, "Phase 2: Company B customer assigned PK 1");

            var vehB = new Vehicle { CustomerId = custB.CustomerId, Make = "Honda", Model = "Civic RS 2025", PlateNumber = "B-BBB-02" };
            dbB.Vehicles.Add(vehB);
            await dbB.SaveChangesAsync();

            var jobB = new JobOrder
            {
                CustomerId = custB.CustomerId,
                VehicleId = vehB.VehicleId,
                EstimateId = "EST-B-000001",
                TotalEstimatedCost = 12000m,
                Status = "In Queue",
                BranchId = branchB.BranchId,
                StartedAt = DateTime.Now
            };
            dbB.JobOrders.Add(jobB);
            await dbB.SaveChangesAsync();
            check(jobB.JobOrderId == 1, "Phase 2: Company B job order assigned PK 1");

            // Company C Customer & Job (PK 1)
            var custC = new Customer { FullName = "Company C Customer", ContactNumber = "09190000003", Email = "cust.c@companyc.com" };
            dbC.Customers.Add(custC);
            await dbC.SaveChangesAsync();
            check(custC.CustomerId == 1, "Phase 2: Company C customer assigned PK 1");

            var vehC = new Vehicle { CustomerId = custC.CustomerId, Make = "Mitsubishi", Model = "Montero 2026", PlateNumber = "C-CCC-03" };
            dbC.Vehicles.Add(vehC);
            await dbC.SaveChangesAsync();

            var jobC = new JobOrder
            {
                CustomerId = custC.CustomerId,
                VehicleId = vehC.VehicleId,
                EstimateId = "EST-C-000001",
                TotalEstimatedCost = 25000m,
                Status = "In Queue",
                BranchId = branchC.BranchId,
                StartedAt = DateTime.Now
            };
            dbC.JobOrders.Add(jobC);
            await dbC.SaveChangesAsync();
            check(jobC.JobOrderId == 1, "Phase 2: Company C job order assigned PK 1");

            // 5. Verify Zero Cross-Company Data Leakage
            var readCustA = await dbA.Customers.FindAsync(1);
            var readCustB = await dbB.Customers.FindAsync(1);
            var readCustC = await dbC.Customers.FindAsync(1);

            check(readCustA?.FullName == "Company A Customer", "Phase 2: Company A query returns Company A record exclusively");
            check(readCustB?.FullName == "Company B Customer", "Phase 2: Company B query returns Company B record exclusively");
            check(readCustC?.FullName == "Company C Customer", "Phase 2: Company C query returns Company C record exclusively");

            var readJobA = await dbA.JobOrders.Include(j => j.Vehicle).FirstAsync(j => j.JobOrderId == 1);
            var readJobB = await dbB.JobOrders.Include(j => j.Vehicle).FirstAsync(j => j.JobOrderId == 1);
            var readJobC = await dbC.JobOrders.Include(j => j.Vehicle).FirstAsync(j => j.JobOrderId == 1);

            check(readJobA.Vehicle?.PlateNumber == "A-AAA-01", "Phase 2: Company A job linked to Company A vehicle");
            check(readJobB.Vehicle?.PlateNumber == "B-BBB-02", "Phase 2: Company B job linked to Company B vehicle");
            check(readJobC.Vehicle?.PlateNumber == "C-CCC-03", "Phase 2: Company C job linked to Company C vehicle");

            check(await dbA.Customers.CountAsync() == 1, "Phase 2: Company A table count isolated to 1 customer");
            check(await dbB.Customers.CountAsync() == 1, "Phase 2: Company B table count isolated to 1 customer");
            check(await dbC.Customers.CountAsync() == 1, "Phase 2: Company C table count isolated to 1 customer");

            // 6. Test Tenant C Multi-Branching within Single Database
            var branchNorth = new TenantBranch { BranchCode = "NORTH", BranchName = "RevCare North Hub", Address = "North Highway", CreatedAtUtc = DateTime.UtcNow };
            var branchSouth = new TenantBranch { BranchCode = "SOUTH", BranchName = "RevCare South Hub", Address = "South Highway", CreatedAtUtc = DateTime.UtcNow };
            dbC.Branches.AddRange(branchNorth, branchSouth);
            await dbC.SaveChangesAsync();

            var jobNorth = new JobOrder
            {
                CustomerId = custC.CustomerId,
                VehicleId = vehC.VehicleId,
                EstimateId = "EST-C-NORTH-01",
                TotalEstimatedCost = 8000m,
                Status = "In Queue",
                BranchId = branchNorth.BranchId,
                StartedAt = DateTime.Now
            };
            var jobSouth = new JobOrder
            {
                CustomerId = custC.CustomerId,
                VehicleId = vehC.VehicleId,
                EstimateId = "EST-C-SOUTH-01",
                TotalEstimatedCost = 15000m,
                Status = "In Queue",
                BranchId = branchSouth.BranchId,
                StartedAt = DateTime.Now
            };
            dbC.JobOrders.AddRange(jobNorth, jobSouth);
            await dbC.SaveChangesAsync();

            var northJobs = await dbC.JobOrders.Where(j => j.BranchId == branchNorth.BranchId).ToListAsync();
            var southJobs = await dbC.JobOrders.Where(j => j.BranchId == branchSouth.BranchId).ToListAsync();
            check(northJobs.Count == 1 && northJobs[0].EstimateId == "EST-C-NORTH-01", "Phase 2: Tenant C Branch NORTH orders isolated");
            check(southJobs.Count == 1 && southJobs[0].EstimateId == "EST-C-SOUTH-01", "Phase 2: Tenant C Branch SOUTH orders isolated");

            // 7. Test 3NF Operational Schema Unification in Tenant B & Tenant C
            // Verify Estimates & Lines in Company B
            var estB = new Estimate
            {
                EstimateNumber = "EST-B-2026-001",
                CustomerId = custB.CustomerId,
                VehicleId = vehB.VehicleId,
                Status = "Approved",
                TotalAmount = 4500m,
                Lines =
                {
                    new EstimateLine { Description = "Synthetic Oil Change", Quantity = 1, UnitPrice = 2500m, LineTotal = 2500m },
                    new EstimateLine { Description = "Oil Filter Element", Quantity = 1, UnitPrice = 2000m, LineTotal = 2000m }
                }
            };
            dbB.Estimates.Add(estB);
            await dbB.SaveChangesAsync();

            var savedEstB = await dbB.Estimates.Include(e => e.Lines).FirstOrDefaultAsync(e => e.EstimateNumber == "EST-B-2026-001");
            check(savedEstB != null && savedEstB.Lines.Count == 2, "Phase 2: Tenant B supports 3NF Estimates and EstimateLines");
            check(savedEstB?.Lines.Sum(l => l.LineTotal) == 4500m, "Phase 2: Tenant B estimate lines total verified");

            // Verify Appointment and AppointmentHistory in Company B
            var apptB = new Appointment
            {
                CustomerId = custB.CustomerId,
                VehicleId = vehB.VehicleId,
                ScheduledStart = DateTime.UtcNow.AddDays(1),
                Status = "Scheduled",
                RequestedServices = "Synthetic Oil Change",
                Notes = "First service for new vehicle"
            };
            dbB.Appointments.Add(apptB);
            await dbB.SaveChangesAsync();

            var apptHistB = new AppointmentHistory
            {
                AppointmentId = apptB.AppointmentId,
                Action = "Scheduled",
                Reason = "Online customer booking",
                Actor = "System Booking Service"
            };
            dbB.AppointmentHistories.Add(apptHistB);
            await dbB.SaveChangesAsync();

            var savedApptB = await dbB.Appointments.Include(a => a.Vehicle).FirstOrDefaultAsync(a => a.AppointmentId == apptB.AppointmentId);
            check(savedApptB != null && savedApptB.Vehicle?.Make == "Honda", "Phase 2: Tenant B supports Appointments linked to 3NF Vehicles");

            // Verify Cancellation Requests and Refunds in Company B
            var cancelReqB = new JobCancellationRequest
            {
                JobOrderId = jobB.JobOrderId,
                ReasonCategory = "Customer Relocation",
                ReasonExplanation = "Customer moving to another city",
                RequestedBy = "cust.b@companyb.com",
                Status = "Approved",
                TotalPaidAtRequest = 6000m,
                ProposedRefund = 6000m,
                ApprovedRefund = 6000m,
                ReviewJustification = "Customer cancelled before parts acquisition",
                ReviewedBy = "Manager B"
            };
            dbB.JobCancellationRequests.Add(cancelReqB);
            await dbB.SaveChangesAsync();

            var refundB = new Refund
            {
                JobOrderId = jobB.JobOrderId,
                CancellationRequestId = cancelReqB.RequestId,
                Amount = 6000m,
                RefundMethod = "Bank Transfer",
                RecipientName = "Company B Customer",
                Reason = "Approved relocation cancellation",
                AuthorizedBy = "Manager B"
            };
            dbB.Refunds.Add(refundB);
            await dbB.SaveChangesAsync();

            var savedRefundB = await dbB.Refunds.Include(r => r.CancellationRequest).FirstOrDefaultAsync(r => r.RefundId == refundB.RefundId);
            check(savedRefundB != null && savedRefundB.CancellationRequest?.ApprovedRefund == 6000m, "Phase 2: Tenant B supports JobCancellationRequests and Refunds");

            // Verify Soft Archiving Columns on JobOrder & JobTask in Company B
            jobB.IsArchived = true;
            jobB.ArchivedAtUtc = DateTime.UtcNow;
            jobB.ArchivedBy = "Manager B";
            jobB.ArchiveReason = "Historical archived job";
            await dbB.SaveChangesAsync();

            var archivedJobB = await dbB.JobOrders.AsNoTracking().FirstOrDefaultAsync(j => j.JobOrderId == jobB.JobOrderId);
            check(archivedJobB != null && archivedJobB.IsArchived && archivedJobB.ArchivedBy == "Manager B", "Phase 2: Tenant B supports Job Order soft-archiving metadata");

            Console.WriteLine("[Multi-Company Isolation Tests Completed Successfully]");
        }
        finally
        {
            // Drop Disposable Databases Cleanly
            await dbA.Database.EnsureDeletedAsync();
            await dbB.Database.EnsureDeletedAsync();
            await dbC.Database.EnsureDeletedAsync();
        }
    }
}
