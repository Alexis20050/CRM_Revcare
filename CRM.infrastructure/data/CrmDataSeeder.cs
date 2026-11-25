using System;
using System.Collections.Generic;
using System.Linq;
using crm.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data
{
    public static partial class CrmDataSeeder
    {
        public static void ResetAndSeed(MasterCrmDbContext db)
        {
            var strategy = db.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                using var tx = db.Database.BeginTransaction();

                // 1. Clear existing data in correct FK dependency order
                db.Feedbacks.RemoveRange(db.Feedbacks);
                db.Complaints.RemoveRange(db.Complaints);
                db.Inquiries.RemoveRange(db.Inquiries);
                db.Payments.RemoveRange(db.Payments);
                db.JobTasks.RemoveRange(db.JobTasks);
                db.JobOrders.RemoveRange(db.JobOrders);
                db.Customers.RemoveRange(db.Customers);
                db.SaveChanges();

                // Reset identity columns to clear, predictable business ID sequences
                try
                {
                    db.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Feedbacks', RESEED, 6000)");
                    db.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Complaints', RESEED, 5000)");
                    db.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Inquiries', RESEED, 4000)");
                    db.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Payments', RESEED, 3000)");
                    db.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('JobTasks', RESEED, 2000)");
                    db.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('JobOrders', RESEED, 1000)");
                    db.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('Customers', RESEED, 100)");
                }
                catch { }

                // Seed workshop staff directory if empty
                EnsureStaffSeeded(db);

                // 2. Customers
                var customers = new List<Customer>
                {
                    new Customer { FullName = "Juan Dela Cruz", ContactNumber = "0917-111-2233", Email = "juan.delacruz@gmail.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-30) },
                    new Customer { FullName = "Maria Santos", ContactNumber = "0928-333-4455", Email = "maria.santos@yahoo.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-28) },
                    new Customer { FullName = "Carlos Reyes", ContactNumber = "0945-777-8899", Email = "carlos.reyes@outlook.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-25) },
                    new Customer { FullName = "Ana Patricia Gomez", ContactNumber = "0919-888-9900", Email = "ana.gomez@gmail.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new Customer { FullName = "Roberto Tan", ContactNumber = "0915-444-5566", Email = "roberto.tan@enterprise.ph", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-18) },
                    new Customer { FullName = "Liza Macaraeg", ContactNumber = "0922-666-7788", Email = "liza.macaraeg@hotmail.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-15) },
                    new Customer { FullName = "Gabriel Bautista", ContactNumber = "0933-222-1144", Email = "gabriel.b@gmail.com", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-10) },
                    new Customer { FullName = "Elena Villanueva", ContactNumber = "0918-555-4321", Email = "elena.v@revcare.ph", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-8) }
                };
                db.Customers.AddRange(customers);
                db.SaveChanges();

                // 3. Job Orders (linked to customers)
                var jobOrders = new List<JobOrder>
                {
                    new JobOrder { CustomerId = customers[0].CustomerId, EstimateId = "EST-2026-001 (Toyota Vios / Brake Overhaul)", Status = "Completed", StartedAt = DateTime.UtcNow.AddDays(-24), CreatedAt = DateTime.UtcNow.AddDays(-25) },
                    new JobOrder { CustomerId = customers[1].CustomerId, EstimateId = "EST-2026-002 (Honda City / PMS 40,000km)", Status = "Completed", StartedAt = DateTime.UtcNow.AddDays(-20), CreatedAt = DateTime.UtcNow.AddDays(-21) },
                    new JobOrder { CustomerId = customers[2].CustomerId, EstimateId = "EST-2026-003 (Mitsubishi Montero / Suspension Noise)", Status = "In Progress", AssignedMechanicUserId = "juan.gomez", AssignedMechanic = "Juan Gomez", StartedAt = DateTime.UtcNow.AddDays(-5), CreatedAt = DateTime.UtcNow.AddDays(-6) },
                    new JobOrder { CustomerId = customers[3].CustomerId, EstimateId = "EST-2026-004 (Nissan Navara / AC Evaporator Cleaning)", Status = "Completed", StartedAt = DateTime.UtcNow.AddDays(-14), CreatedAt = DateTime.UtcNow.AddDays(-15) },
                    new JobOrder { CustomerId = customers[4].CustomerId, EstimateId = "EST-2026-005 (Ford Ranger / Transmission Flush)", Status = "In Queue", StartedAt = DateTime.UtcNow.AddDays(-2), CreatedAt = DateTime.UtcNow.AddDays(-3) },
                    new JobOrder { CustomerId = customers[5].CustomerId, EstimateId = "EST-2026-006 (Hyundai Tucson / Radiator & Coolant)", Status = "Completed", StartedAt = DateTime.UtcNow.AddDays(-10), CreatedAt = DateTime.UtcNow.AddDays(-11) },
                    new JobOrder { CustomerId = customers[6].CustomerId, EstimateId = "EST-2026-007 (Mazda 3 / Wheel Alignment & Camber)", Status = "In Progress", AssignedMechanicUserId = "pedro.santos", AssignedMechanic = "Pedro Santos", StartedAt = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow.AddDays(-2) },
                    new JobOrder { CustomerId = customers[7].CustomerId, EstimateId = "EST-2026-008 (Subaru Forester / Steering Rack Overhaul)", Status = "In Queue", StartedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow.AddHours(-12) }
                };
                db.JobOrders.AddRange(jobOrders);
                db.SaveChanges();

                // 4. Job Tasks (linked to Job Orders)
                var jobTasks = new List<JobTask>
                {
                    new JobTask { JobOrderId = jobOrders[0].JobOrderId, ServiceId = "Front Brake Pad Replacement", Remarks = "Replaced with OEM ceramic pads, disc rotors resurfaced.", Status = "Completed", CreatedAt = DateTime.UtcNow.AddDays(-24) },
                    new JobTask { JobOrderId = jobOrders[0].JobOrderId, ServiceId = "Brake Fluid Bleeding", Remarks = "DOT 4 brake fluid flush completed.", Status = "Completed", CreatedAt = DateTime.UtcNow.AddDays(-24) },
                    new JobTask { JobOrderId = jobOrders[1].JobOrderId, ServiceId = "Periodic Maintenance Service (PMS)", Remarks = "Full synthetic 5W-30 oil change, spark plug inspection, oil filter replaced.", Status = "Completed", CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new JobTask { JobOrderId = jobOrders[1].JobOrderId, ServiceId = "Cabin & Air Filter Replacement", Remarks = "Air intake and cabin carbon filters replaced.", Status = "Completed", CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new JobTask { JobOrderId = jobOrders[2].JobOrderId, ServiceId = "Suspension Bushing & Ball Joint Check", Remarks = "Front right lower control arm bushing torn. Replacement part ordered.", Status = "In Progress", CreatedAt = DateTime.UtcNow.AddDays(-5) },
                    new JobTask { JobOrderId = jobOrders[2].JobOrderId, ServiceId = "Front Shock Absorber Replacement", Remarks = "Awaiting parts delivery from supplier.", Status = "Pending", CreatedAt = DateTime.UtcNow.AddDays(-5) },
                    new JobTask { JobOrderId = jobOrders[3].JobOrderId, ServiceId = "Aircon Evaporator Core Cleaning", Remarks = "Dashboard dismantled, evaporator ultrasonic cleaned and recharged R134a refrigerant.", Status = "Completed", CreatedAt = DateTime.UtcNow.AddDays(-14) },
                    new JobTask { JobOrderId = jobOrders[4].JobOrderId, ServiceId = "Automatic Transmission Fluid Flush", Remarks = "Vehicle queued in Bay 3 for fluid exchange.", Status = "In Queue", CreatedAt = DateTime.UtcNow.AddDays(-2) },
                    new JobTask { JobOrderId = jobOrders[5].JobOrderId, ServiceId = "Radiator Flush & Thermostat Replacement", Remarks = "Old thermostat stuck open. Replaced and filled with 50/50 coolant.", Status = "Completed", CreatedAt = DateTime.UtcNow.AddDays(-10) },
                    new JobTask { JobOrderId = jobOrders[6].JobOrderId, ServiceId = "Computerized 4-Wheel Alignment", Remarks = "Toe and camber adjusted to manufacturer specifications.", Status = "In Progress", CreatedAt = DateTime.UtcNow.AddDays(-1) }
                };
                db.JobTasks.AddRange(jobTasks);

                // 5. Payments (linked to Job Orders)
                var payments = new List<Payment>
                {
                    new Payment { JobOrderId = jobOrders[0].JobOrderId, PaymentType = "Retail Full", AmountPaid = 8500.00m, PaymentMethod = "Credit Card", CreatedAt = DateTime.UtcNow.AddDays(-24) },
                    new Payment { JobOrderId = jobOrders[1].JobOrderId, PaymentType = "Retail Full", AmountPaid = 6200.00m, PaymentMethod = "GCash", CreatedAt = DateTime.UtcNow.AddDays(-20) },
                    new Payment { JobOrderId = jobOrders[2].JobOrderId, PaymentType = "Downpayment", AmountPaid = 5000.00m, PaymentMethod = "Bank Transfer", CreatedAt = DateTime.UtcNow.AddDays(-5) },
                    new Payment { JobOrderId = jobOrders[3].JobOrderId, PaymentType = "Retail Full", AmountPaid = 12500.00m, PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow.AddDays(-14) },
                    new Payment { JobOrderId = jobOrders[5].JobOrderId, PaymentType = "Retail Full", AmountPaid = 4800.00m, PaymentMethod = "GCash", CreatedAt = DateTime.UtcNow.AddDays(-10) },
                    new Payment { JobOrderId = jobOrders[6].JobOrderId, PaymentType = "Downpayment", AmountPaid = 2000.00m, PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow.AddDays(-1) }
                };
                db.Payments.AddRange(payments);

                // 6. Inquiries (some linked to customers, some guest inquiries)
                var inquiries = new List<Inquiry>
                {
                    new Inquiry { CustomerId = customers[0].CustomerId, CustomerName = "", ContactNumber = "", InquiryPlatform = "Messenger", MessageDetails = "Inquiring about 50,000 km PMS package for Toyota Vios and available time slots.", Status = "Converted", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-26) },
                    new Inquiry { CustomerId = customers[2].CustomerId, CustomerName = "", ContactNumber = "", InquiryPlatform = "Call", MessageDetails = "Reported squeaking and knocking sound in the front suspension when passing over speed bumps.", Status = "Converted", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-7) },
                    new Inquiry { CustomerId = null, CustomerName = "Marco Polo", ContactNumber = "0917-888-1212", InquiryPlatform = "Walk-in", MessageDetails = "Walk-in inquiry asking for wheel alignment and nitrogen tire inflation rates.", Status = "Responded", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-4) },
                    new Inquiry { CustomerId = null, CustomerName = "Cynthia Ramos", ContactNumber = "0920-999-3434", InquiryPlatform = "Messenger", MessageDetails = "Do you service European car models like BMW 320i air conditioning system?", Status = "Responded", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                    new Inquiry { CustomerId = null, CustomerName = "Dante Alighieri", ContactNumber = "0949-555-7878", InquiryPlatform = "Email", MessageDetails = "Requesting official quotation for fleet PMS change oil for 5 company utility vans.", Status = "Unanswered", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                    new Inquiry { CustomerId = customers[6].CustomerId, CustomerName = "", ContactNumber = "", InquiryPlatform = "Call", MessageDetails = "Car pulling strongly to the right side on the highway. Requested urgent inspection.", Status = "Converted", IsActive = true, CreatedAt = DateTime.UtcNow.AddHours(-18) }
                };
                db.Inquiries.AddRange(inquiries);

                // 7. Complaints (linked to real Job Orders & Customers)
                var complaints = new List<Complaint>
                {
                    new Complaint
                    {
                        JobOrderId = jobOrders[2].JobOrderId,
                        CustomerId = jobOrders[2].CustomerId, // Linked to customer & job order
                        ComplaintType = "Delay in Delivery",
                        Description = "Job Order #3 suspension parts delivery was delayed by 2 days due to supplier backorder.",
                        ResolutionStatus = "Investigating",
                        ActionTaken = "Followed up with parts warehouse and provided loaner courtesy ride.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new Complaint
                    {
                        JobOrderId = null, // General shop complaint
                        CustomerId = customers[4].CustomerId,
                        ComplaintType = "Pricing Issue",
                        Description = "Customer questioned the diagnostic labor fee before starting the transmission flush estimate.",
                        ResolutionStatus = "Resolved",
                        ActionTaken = "Explained diagnostic procedure; credited diagnostic fee towards final repair bill upon approval.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new Complaint
                    {
                        JobOrderId = jobOrders[5].JobOrderId,
                        CustomerId = jobOrders[5].CustomerId,
                        ComplaintType = "Service Quality",
                        Description = "Slight coolant odor inside cabin on first drive home after radiator service.",
                        ResolutionStatus = "Resolved",
                        ActionTaken = "Re-inspected hose clamps, pressure-tested cooling circuit (no leaks found), wiped engine bay residual spillage.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    }
                };
                db.Complaints.AddRange(complaints);

                // 8. Feedbacks (linked to real Job Orders)
                var feedbacks = new List<Feedback>
                {
                    new Feedback
                    {
                        JobOrderId = jobOrders[0].JobOrderId,
                        RatingScore = 5,
                        PositiveRemarks = "Brakes feel brand new! Honest mechanics and transparent pricing breakdown.",
                        AreasForImprovement = "Wi-Fi in customer lounge was slightly slow.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-23)
                    },
                    new Feedback
                    {
                        JobOrderId = jobOrders[1].JobOrderId,
                        RatingScore = 5,
                        PositiveRemarks = "Fast 1-hour PMS change oil turnaround and complimentary car wash was spotless.",
                        AreasForImprovement = "Waiting area coffee machine was out of cups.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-19)
                    },
                    new Feedback
                    {
                        JobOrderId = jobOrders[3].JobOrderId,
                        RatingScore = 4,
                        PositiveRemarks = "Air conditioning is ice cold now even in heavy traffic noon heat! Knowledgeable advisor.",
                        AreasForImprovement = "Dashboard reassembly left a small smudged fingerprint on the glove compartment.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-13)
                    },
                    new Feedback
                    {
                        JobOrderId = jobOrders[5].JobOrderId,
                        RatingScore = 4,
                        PositiveRemarks = "Quick response when resolving the coolant smell issue. Appreciate the prompt after-service checkup.",
                        AreasForImprovement = "Provide text update notification when vehicle is ready for release.",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-7)
                    }
                };
                db.Feedbacks.AddRange(feedbacks);

                db.SaveChanges();
                tx.Commit();
            });
        }

        public static void EnsureStaffSeeded(MasterCrmDbContext db)
        {
            if (!db.Staff.Any())
            {
                db.Staff.AddRange(
                    new WorkshopStaff { StaffId = "juan.gomez", DisplayName = "Juan Gomez", Role = "Automotive Mechanic", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
                    new WorkshopStaff { StaffId = "pedro.santos", DisplayName = "Pedro Santos", Role = "Lead Mechanic", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
                    new WorkshopStaff { StaffId = "maria.reyes", DisplayName = "Maria Reyes", Role = "Detailing Technician", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
                    new WorkshopStaff { StaffId = "alex.cruz", DisplayName = "Alex Cruz", Role = "Automotive Mechanic", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
                    new WorkshopStaff { StaffId = "carlos.ramos", DisplayName = "Carlos Ramos", Role = "Lead Mechanic", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
                    new WorkshopStaff { StaffId = "danilo.reyes", DisplayName = "Danilo Reyes", Role = "Detailing Technician", IsActive = true, CreatedAtUtc = DateTime.UtcNow }
                );
                db.SaveChanges();
            }
        }
    }
}
