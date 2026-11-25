using System;
using System.Collections.Generic;
using System.Linq;
using crm.domain.entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.data
{
    public static partial class CrmDataSeeder
    {
        public static void SeedRealistic200Records(MasterCrmDbContext db)
        {
            EnsureStaffSeeded(db);
            Console.WriteLine($"[Seeder] Starting counts -> Customers: {db.Customers.Count()}, Jobs: {db.JobOrders.Count()}, Tasks: {db.JobTasks.Count()}, Payments: {db.Payments.Count()}, Inquiries: {db.Inquiries.Count()}, Complaints: {db.Complaints.Count()}, Feedbacks: {db.Feedbacks.Count()}");

            var staffList = db.Staff.AsNoTracking().Where(s => s.IsActive).ToList();
            if (staffList.Count == 0) staffList = db.Staff.AsNoTracking().ToList();

            var rnd = new Random(42);

                string[] firstNames =
                [
                    "Mateo", "Sophia", "Gabriel", "Isabella", "Lucas", "Angelica", "Daniel", "Catherine",
                    "Miguel", "Beatriz", "Joshua", "Patricia", "Rafael", "Jasmine", "Christian", "Carmela",
                    "Mark", "Christine", "Anthony", "Stephanie", "Emmanuel", "Nicole", "John Paul", "Katrina",
                    "Dominic", "Andrea", "Paolo", "Camille", "Carlo", "Vanessa", "Kenneth", "Maria Elena",
                    "Jerome", "Kristine", "Vincent", "Bianca", "Rommel", "Danica", "Arvin", "Rochelle",
                    "Alden", "Bea", "Gerald", "Sarah", "Enrique", "Liza", "Richard", "Marian",
                    "Dingdong", "Regine", "Ogie", "Kim", "Piolo", "Judy Ann", "Ryan", "Carmina",
                    "Zoren", "Cassie", "Marga", "Kristoff"
                ];

                string[] lastNames =
                [
                    "Santos", "Reyes", "Cruz", "Bautista", "Ocampo", "Garcia", "Mendoza", "Torres",
                    "Ramos", "Flores", "Gonzales", "Lopez", "Fernandez", "De Leon", "Del Rosario", "Villanueva",
                    "Castillo", "Rivera", "Aquino", "Castro", "Sanchez", "Navarro", "Mercado", "Salazar",
                    "Pineda", "Valencia", "Dizon", "Morales", "Soriano", "Alonzo", "Manalo", "Pascual",
                    "Valdez", "Padilla", "Tan", "Sy", "Co", "Gokongwei", "Lim", "Chua",
                    "Villanueva", "Magpantay", "Custodio", "Aguilar", "Tolentino", "Espiritu", "Dimaculangan", "Macaraeg"
                ];

                string[] emailDomains = ["gmail.com", "yahoo.com", "outlook.com", "icloud.com", "revcare-client.ph"];

                string[] mobilePrefixes = ["0917", "0918", "0920", "0922", "0927", "0933", "0945", "0956", "0966", "0977", "0998"];

                string[] vehicles =
                [
                    "Toyota Vios 1.3 XLE (NBD-4821)", "Toyota Fortuner 2.8 V 4x4 (CAK-9124)", "Toyota Innova 2.8 E Diesel (NFR-3105)",
                    "Toyota Hilux Conquest 4x4 (NDB-8812)", "Toyota Corolla Cross 1.8 HEV (NBP-7741)", "Toyota Wigo 1.0 G (DAR-4512)",
                    "Toyota Rush 1.5 G (NBC-2319)", "Toyota Hiace Commuter Deluxe (NBE-6623)", "Toyota Raize 1.0 Turbo (NDB-1294)",
                    "Honda Civic RS Turbo (NFD-8921)", "Honda City 1.5 RS (NBE-1045)", "Honda CR-V 2.0 S (CBL-7823)",
                    "Honda HR-V 1.5 V Turbo (NDB-3342)", "Honda BR-V 1.5 VX (NAE-5512)", "Honda Brio 1.2 RS (NBA-9081)",
                    "Mitsubishi Montero Sport GT 4x4 (NBF-6612)", "Mitsubishi Xpander Cross 1.5 (NCR-4412)", "Mitsubishi Strada Athlete 4x4 (CBM-8831)",
                    "Mitsubishi Mirage G4 GLS (NBD-1123)", "Mitsubishi L300 FB Exceed (NBA-7744)",
                    "Nissan Navara PRO-4X (NDB-9912)", "Nissan Terra 2.5 VL 4x4 (NBE-4451)", "Nissan Almera 1.0 Turbo VE (NCB-2214)",
                    "Nissan Kicks e-POWER VL (DAR-8821)", "Nissan Urvan NV350 (NBF-1243)",
                    "Ford Ranger Wildtrak 4x4 (NBF-7712)", "Ford Everest Titanium+ 4x4 (NDB-6621)", "Ford Territory Titanium (NCB-9012)",
                    "Isuzu D-Max 3.0 4x4 (CAK-5521)", "Isuzu mu-X 3.0 LS-E (NBF-3314)", "Isuzu Traviz L (NCR-9912)",
                    "Hyundai Tucson 2.0 CRDi (NBD-7712)", "Hyundai Staria 2.2 Premium (CBM-1124)", "Hyundai Creta 1.5 GLS (NAE-7721)",
                    "Hyundai Stargazer 1.5 X (NDB-4412)", "Suzuki Jimny AllGrip GLX (NBE-9912)", "Suzuki Ertiga Hybrid GLX (NCR-3312)",
                    "Suzuki Swift 1.2 GL (NBA-4421)", "Suzuki XL7 1.5 GLX (CBL-9912)", "Mazda CX-5 2.5 AWD (NBF-8812)",
                    "Subaru Forester 2.0i-S EyeSight (NBE-7712)", "Kia Seltos 1.4 Turbo (NCB-6612)"
                ];

                (string Name, decimal Price)[] catalog =
                [
                    ("Oil and filter change", 1500.00m),
                    ("Brake inspection", 800.00m),
                    ("Wheel alignment", 1200.00m),
                    ("Exterior detailing", 2500.00m),
                    ("Air-conditioning diagnosis", 1000.00m),
                    ("Battery replacement & check", 3500.00m),
                    ("Engine tune-up & spark plugs", 2800.00m),
                    ("Suspension & shock absorbers", 3200.00m),
                    ("Transmission fluid service", 2200.00m),
                    ("Tire replacement & balancing", 1600.00m)
                ];

                string[] inquiryPlatforms = ["Facebook Messenger", "WhatsApp", "Phone Call", "Walk-in", "Google Business"];
                string[] inquiryMessages =
                [
                    "How much for 40,000km periodic maintenance service for Toyota Fortuner?",
                    "Do you accept walk-in computerized 4-wheel alignment on Saturday mornings?",
                    "Inquiring about aircon cleaning cost; weak airflow and slight musty smell.",
                    "Squeaking sound coming from front wheels when braking downhill.",
                    "Quote for automatic transmission fluid drain and refill using OEM fluid.",
                    "Need battery replacement. Do you offer on-site home service or testing?",
                    "Check engine light turned on after long provincial drive. OBD-II scanning fee?",
                    "How long does 3-step exterior detailing and ceramic paint protection take?",
                    "Knocking sound in front suspension when passing over road humps.",
                    "Need tire replacement and high-speed balancing for 4 tires (225/65 R17).",
                    "Do you carry genuine spark plugs and cabin filters for Honda Civic RS?",
                    "Inquiring about full brake system overhaul including disc rotor resurfacing.",
                    "Engine vibration noticeable while idling in Drive with AC turned on.",
                    "Can I get a formal quotation for company fleet maintenance of 5 vehicles?",
                    "Radiator coolant temperature slightly higher than normal in heavy traffic."
                ];

                (string Type, string Desc, string Action)[] complaintTemplates =
                [
                    ("Service Quality", "Aircon cooling was slow to reach comfortable temperature after evaporator cleaning.", "Re-evacuated system, checked expansion valve, and topped up refrigerant. Cooling now optimal at 6.5C."),
                    ("Parts Delay", "Special order shock absorber bushings took 3 additional days to arrive from supplier.", "Provided complimentary labor discount and expedited final installation upon parts arrival."),
                    ("Billing & Pricing", "Customer requested itemized breakdown of shop consumables listed on the final billing.", "Service advisor walked through itemized supply costs; customer acknowledged and approved."),
                    ("Waiting Time", "Initial intake assessment took longer than estimated 20 minutes due to bay congestion.", "Assigned dedicated service advisor and offered complimentary lounge coffee and snack."),
                    ("Cleanliness & Care", "Small grease fingerprint found on steering wheel cover after suspension test drive.", "Detailing technician performed full interior touch-up and steering sanitization."),
                    ("Communication", "Customer was not notified immediately when brake rotor resurfacing was recommended.", "Standardized customer update protocol via instant SMS before undertaking additional scope."),
                    ("Service Quality", "Slight wheel vibration felt at highway speeds above 100 km/h after tire rotation.", "Re-calibrated high-speed dynamic wheel balancer and re-weighted front tires to zero tolerance."),
                    ("Parts Delay", "OEM transmission filter gasket was temporarily out of stock at primary distributor.", "Sourced genuine part from secondary certified warehouse within 24 hours."),
                    ("Billing & Pricing", "Customer asked if 10% senior citizen discount applies to parts as well as labor.", "Explained statutory guidelines and applied full legal senior discount on eligible items.")
                ];

                (int Score, string Pos, string Imp)[] feedbackTemplates =
                [
                    (5, "Very professional technicians and comfortable airconditioned waiting lounge.", "Expand customer parking slots during peak weekend morning hours."),
                    (5, "Fast PMS turnover, car feels very smooth and engine response improved significantly.", "Add automated text notification when vehicle enters final quality inspection."),
                    (4, "Detailed explanation of worn brake pads with actual measurements shown before replacement.", "Provide complimentary bottled water in the reception area."),
                    (5, "Computerized alignment fixed the pulling issue completely. Very honest pricing.", "None, keep up the consistent high quality service!"),
                    (4, "Polite service advisor and clean shop bays. Work finished ahead of estimated time.", "Credit card terminal connection was slightly slow; backup terminal recommended."),
                    (5, "Excellent detailing work, car looks like it just rolled out of the showroom.", "Offer loyalty reward points for returning maintenance customers."),
                    (3, "Decent PMS service but weekend waiting room was quite crowded.", "Implement online queue reservation system to stagger morning walk-ins."),
                    (5, "Transparent quotation, no hidden add-ons or unnecessary replacement pressure.", "Keep the complimentary vehicle wash voucher program active."),
                    (4, "Knowledgeable lead mechanic diagnosed the knocking sound accurately within 10 minutes.", "Install digital status board showing real-time workshop bay progress.")
                ];

                // 1. SEED CUSTOMERS TO 200
                int currentCustomers = db.Customers.Count();
                int customersNeeded = Math.Max(0, 200 - currentCustomers);
                if (customersNeeded > 0)
                {
                    var newCusts = new List<Customer>();
                    var existingNames = new HashSet<string>(db.Customers.Select(c => c.FullName), StringComparer.OrdinalIgnoreCase);
                    var existingPhones = new HashSet<string>(db.Customers.Select(c => c.ContactNumber));

                    for (int i = 0; i < customersNeeded; i++)
                    {
                        string fn = firstNames[rnd.Next(firstNames.Length)];
                        string ln = lastNames[rnd.Next(lastNames.Length)];
                        string fullName = $"{fn} {ln}";
                        int counter = 2;
                        while (existingNames.Contains(fullName))
                        {
                            fullName = $"{fn} {ln} {counter++}";
                        }
                        existingNames.Add(fullName);

                        string prefix = mobilePrefixes[rnd.Next(mobilePrefixes.Length)];
                        string phone = $"{prefix}-{rnd.Next(100, 999)}-{rnd.Next(1000, 9999)}";
                        while (existingPhones.Contains(phone))
                        {
                            phone = $"{prefix}-{rnd.Next(100, 999)}-{rnd.Next(1000, 9999)}";
                        }
                        existingPhones.Add(phone);

                        string cleanFn = fn.ToLowerInvariant().Replace(" ", "");
                        string cleanLn = ln.ToLowerInvariant().Replace(" ", "");
                        string email = $"{cleanFn}.{cleanLn}{rnd.Next(10, 99)}@{emailDomains[rnd.Next(emailDomains.Length)]}";

                        newCusts.Add(new Customer
                        {
                            FullName = fullName,
                            ContactNumber = phone,
                            Email = email,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow.AddDays(-rnd.Next(5, 120))
                        });
                    }
                    db.Customers.AddRange(newCusts);
                    db.SaveChanges();
                }

                var allCustomers = db.Customers.AsNoTracking().ToList();

                // 2. SEED JOB ORDERS TO 200
                int currentJobs = db.JobOrders.Count();
                int jobsNeeded = Math.Max(0, 200 - currentJobs);
                if (jobsNeeded > 0)
                {
                    var newJobs = new List<JobOrder>();
                    for (int i = 0; i < jobsNeeded; i++)
                    {
                        var cust = allCustomers[rnd.Next(allCustomers.Count)];
                        string vehicle = vehicles[rnd.Next(vehicles.Length)];

                        // Select 1 to 3 distinct catalog services
                        int serviceCount = rnd.Next(1, 4);
                        var pickedServices = catalog.OrderBy(_ => rnd.Next()).Take(serviceCount).ToList();
                        decimal totalCost = pickedServices.Sum(s => s.Price);

                        // Realistic status distribution
                        int roll = rnd.Next(100);
                        string status = roll < 60 ? "Completed" : roll < 85 ? "Released" : roll < 95 ? "In Progress" : "In Queue";

                        var mech = staffList[rnd.Next(staffList.Count)];
                        DateTime created = DateTime.UtcNow.AddDays(-rnd.Next(2, 90));
                        DateTime started = created.AddHours(rnd.Next(1, 6));
                        DateTime? completed = (status == "Completed" || status == "Released") ? started.AddHours(rnd.Next(3, 24)) : null;

                        int estNum = currentJobs + i + 1;
                        string estRef = $"EST-{estNum:D6} / {cust.FullName} / {vehicle}";

                        // Initial creation in In Queue status with Pending tasks
                        var job = new JobOrder
                        {
                            CustomerId = cust.CustomerId,
                            VehicleDetails = vehicle,
                            EstimateId = estRef,
                            TotalEstimatedCost = totalCost,
                            Status = "In Queue",
                            AssignedMechanicUserId = mech.StaffId,
                            AssignedMechanic = mech.DisplayName,
                            InquiryNotes = $"Customer intake request: {string.Join(", ", pickedServices.Select(s => s.Name))}.",
                            StartedAt = started,
                            CreatedAt = created
                        };

                        // Add matching child tasks with Pending status initially
                        foreach (var s in pickedServices)
                        {
                            string remark = s.Name switch
                            {
                                "Oil and filter change" => "Drained 4.5L synthetic 5W-30 engine oil. Replaced OEM oil filter.",
                                "Brake inspection" => "Front pads measured at 8mm, rear at 7mm. Caliper sliders lubricated.",
                                "Wheel alignment" => "Toe and camber set to manufacturer specification. Steering centered.",
                                "Exterior detailing" => "Clay bar decontamination and 2-step compound polish applied.",
                                "Air-conditioning diagnosis" => "System vacuum tested, no leaks found. Recharged 550g R134a refrigerant.",
                                "Battery replacement & check" => "Replaced with Motolite Gold 12V 70Ah maintenance-free battery.",
                                "Engine tune-up & spark plugs" => "Throttle body cleaned and installed 4x Iridium spark plugs.",
                                "Suspension & shock absorbers" => "Inspected ball joints and tie rod ends. Bushings in good condition.",
                                "Transmission fluid service" => "Flushed automatic transmission fluid via fluid exchange machine.",
                                "Tire replacement & balancing" => "Balanced wheel rims with 15g clip-on weights, inflated to 33 PSI.",
                                _ => "Service inspection completed according to workshop checklist."
                            };

                            job.Tasks.Add(new JobTask
                            {
                                ServiceId = s.Name,
                                Remarks = remark,
                                Status = "Pending",
                                CreatedAt = created
                            });
                        }

                        newJobs.Add(job);
                    }
                    db.JobOrders.AddRange(newJobs);
                    db.SaveChanges();

                    // Advance jobs according to realistic workflow rules while tracking payment counts
                    string[] payMethods = ["Cash", "GCash", "Credit Card", "Debit Card", "Bank Transfer", "Check"];
                    int curPay = db.Payments.Count();
                    
                    var advanceJobs = new List<(JobOrder Job, string TargetStatus, decimal Total, decimal Half, decimal Balance)>();
                    for (int i = 0; i < newJobs.Count; i++)
                    {
                        var job = newJobs[i];
                        int remainingPayments = 200 - (curPay + advanceJobs.Count);
                        if (remainingPayments <= 0) break;

                        string targetStatus = (remainingPayments >= 2 && i % 3 != 0) ? "Released" : (remainingPayments >= 1 && i % 2 == 0) ? "Completed" : "In Progress";
                        decimal total = job.TotalEstimatedCost ?? 2000m;
                        decimal half = decimal.Round(total * 0.5m, 2, MidpointRounding.AwayFromZero);
                        decimal balance = total - half;
                        advanceJobs.Add((job, targetStatus, total, half, balance));
                    }

                    // Step 1: Add downpayments for all jobs advancing past In Queue
                    var downpayments = new List<Payment>();
                    foreach (var item in advanceJobs)
                    {
                        downpayments.Add(new Payment
                        {
                            JobOrderId = item.Job.JobOrderId,
                            AmountPaid = item.Half,
                            PaymentType = "Downpayment",
                            PaymentMethod = payMethods[rnd.Next(payMethods.Length)],
                            SubmissionId = Guid.NewGuid(),
                            IsVoided = false,
                            CreatedAt = item.Job.StartedAt
                        });
                    }
                    if (downpayments.Count > 0)
                    {
                        db.Payments.AddRange(downpayments);
                        db.SaveChanges();
                    }

                    // Step 2: Set status to In Progress (now valid because downpayment >= 50% exists)
                    foreach (var item in advanceJobs)
                    {
                        item.Job.Status = "In Progress";
                    }
                    if (advanceJobs.Count > 0)
                    {
                        db.SaveChanges();
                    }

                    // Step 3: Advance tasks (now valid because Job is In Progress)
                    foreach (var item in advanceJobs)
                    {
                        foreach (var t in item.Job.Tasks)
                        {
                            t.Status = item.TargetStatus == "In Progress" && rnd.Next(2) == 0 ? "Ongoing" : "Done";
                        }
                    }
                    if (advanceJobs.Count > 0)
                    {
                        db.SaveChanges();
                    }

                    // Step 4: Advance Completed & Released jobs (inspection confirmed, all tasks Done)
                    var completedOrReleased = advanceJobs.Where(x => x.TargetStatus is "Completed" or "Released").ToList();
                    foreach (var item in completedOrReleased)
                    {
                        foreach (var t in item.Job.Tasks) t.Status = "Done";
                        item.Job.Status = "Completed";
                        item.Job.CompletedAt = item.Job.StartedAt.AddHours(rnd.Next(4, 24));
                    }
                    if (completedOrReleased.Count > 0)
                    {
                        db.FinalInspectionConfirmed = true;
                        db.SaveChanges();
                    }

                    // Step 5: For Released jobs, record final balance payment, then release
                    var releasedJobs = advanceJobs.Where(x => x.TargetStatus == "Released").ToList();
                    var finalPayments = new List<Payment>();
                    foreach (var item in releasedJobs)
                    {
                        if (item.Balance > 0 && (db.Payments.Count() + finalPayments.Count) < 200)
                        {
                            finalPayments.Add(new Payment
                            {
                                JobOrderId = item.Job.JobOrderId,
                                AmountPaid = item.Balance,
                                PaymentType = "Final Balance",
                                PaymentMethod = payMethods[rnd.Next(payMethods.Length)],
                                SubmissionId = Guid.NewGuid(),
                                IsVoided = false,
                                CreatedAt = item.Job.CompletedAt!.Value
                            });
                        }
                    }
                    if (finalPayments.Count > 0)
                    {
                        db.Payments.AddRange(finalPayments);
                        db.SaveChanges();
                    }

                    foreach (var item in releasedJobs)
                    {
                        item.Job.Status = "Released";
                    }
                    if (releasedJobs.Count > 0)
                    {
                        db.SaveChanges();
                    }
                }

                var allJobs = db.JobOrders.Include(j => j.Payments).AsNoTracking().ToList();

                // 3. SEED PAYMENTS TO 200 (if still needed)
                int currentPayments = db.Payments.Count();
                int paymentsNeeded = Math.Max(0, 200 - currentPayments);
                if (paymentsNeeded > 0)
                {
                    string[] payMethods = ["Cash", "GCash", "Credit Card", "Debit Card", "Bank Transfer", "Check"];
                    var eligibleJobs = allJobs.Where(j => j.TotalEstimatedCost.HasValue && j.TotalEstimatedCost.Value > 0).ToList();
                    var batchPayments = new List<Payment>();

                    int pIdx = 0;
                    for (int i = 0; i < paymentsNeeded; i++)
                    {
                        var j = eligibleJobs[pIdx % eligibleJobs.Count];
                        pIdx++;

                        decimal total = j.TotalEstimatedCost!.Value;
                        decimal half = decimal.Round(total * 0.5m, 2, MidpointRounding.AwayFromZero);

                        batchPayments.Add(new Payment
                        {
                            JobOrderId = j.JobOrderId,
                            AmountPaid = half,
                            PaymentType = "Progress Payment",
                            PaymentMethod = payMethods[rnd.Next(payMethods.Length)],
                            SubmissionId = Guid.NewGuid(),
                            IsVoided = false,
                            CreatedAt = j.StartedAt.AddHours(rnd.Next(1, 12))
                        });
                    }
                    db.Payments.AddRange(batchPayments);
                    db.SaveChanges();
                }

                // 4. SEED INQUIRIES TO 200
                int currentInquiries = db.Inquiries.Count();
                int inquiriesNeeded = Math.Max(0, 200 - currentInquiries);
                if (inquiriesNeeded > 0)
                {
                    var newInquiries = new List<Inquiry>();
                    string[] inqStatuses = ["New", "Contacted", "Estimated", "Closed"];

                    for (int i = 0; i < inquiriesNeeded; i++)
                    {
                        string fn = firstNames[rnd.Next(firstNames.Length)];
                        string ln = lastNames[rnd.Next(lastNames.Length)];
                        string platform = inquiryPlatforms[rnd.Next(inquiryPlatforms.Length)];
                        string msg = inquiryMessages[rnd.Next(inquiryMessages.Length)];
                        string stat = inqStatuses[rnd.Next(inqStatuses.Length)];

                        string prefix = mobilePrefixes[rnd.Next(mobilePrefixes.Length)];
                        string phone = $"{prefix}-{rnd.Next(100, 999)}-{rnd.Next(1000, 9999)}";

                        newInquiries.Add(new Inquiry
                        {
                            CustomerName = $"{fn} {ln}",
                            ContactNumber = phone,
                            InquiryPlatform = platform,
                            MessageDetails = msg,
                            Status = stat,
                            CreatedAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 75))
                        });
                    }
                    db.Inquiries.AddRange(newInquiries);
                    db.SaveChanges();
                }

                // 5. SEED COMPLAINTS TO 200
                int currentComplaints = db.Complaints.Count();
                int complaintsNeeded = Math.Max(0, 200 - currentComplaints);
                if (complaintsNeeded > 0)
                {
                    var newComplaints = new List<Complaint>();
                    string[] compStatuses = ["Resolved", "Closed", "Under Investigation", "Open"];

                    for (int i = 0; i < complaintsNeeded; i++)
                    {
                        var t = complaintTemplates[rnd.Next(complaintTemplates.Length)];
                        var cust = allCustomers[rnd.Next(allCustomers.Count)];
                        var j = rnd.Next(2) == 0 ? allJobs[rnd.Next(allJobs.Count)] : null;

                        string resStatus = compStatuses[rnd.Next(compStatuses.Length)];
                        string action = (resStatus == "Resolved" || resStatus == "Closed") ? t.Action : (rnd.Next(2) == 0 ? "Investigating workshop logs with lead technician." : "");

                        newComplaints.Add(new Complaint
                        {
                            CustomerId = cust.CustomerId,
                            JobOrderId = j?.JobOrderId,
                            ComplaintType = t.Type,
                            Description = t.Desc,
                            ResolutionStatus = resStatus,
                            ActionTaken = action,
                            IsActive = true,
                            IsUrgent = t.Type == "Service Quality" && rnd.Next(3) == 0,
                            UnderWarrantyService = rnd.Next(3) == 0,
                            CreatedAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 90))
                        });
                    }
                    db.Complaints.AddRange(newComplaints);
                    db.SaveChanges();
                }

                // 6. SEED FEEDBACKS TO 200
                int currentFeedbacks = db.Feedbacks.Count();
                int feedbacksNeeded = Math.Max(0, 200 - currentFeedbacks);
                if (feedbacksNeeded > 0)
                {
                    var newFeedbacks = new List<Feedback>();
                    var finishedJobs = allJobs.Where(j => j.Status == "Completed" || j.Status == "Released").ToList();
                    if (finishedJobs.Count == 0) finishedJobs = allJobs;

                    for (int i = 0; i < feedbacksNeeded; i++)
                    {
                        var j = finishedJobs[rnd.Next(finishedJobs.Count)];
                        var t = feedbackTemplates[rnd.Next(feedbackTemplates.Length)];

                        newFeedbacks.Add(new Feedback
                        {
                            JobOrderId = j.JobOrderId,
                            LegacyCustomerId = j.CustomerId ?? allCustomers[0].CustomerId,
                            RatingScore = t.Score,
                            PositiveRemarks = t.Pos,
                            AreasForImprovement = t.Imp,
                            IsActive = true,
                            CreatedAt = (j.CompletedAt ?? j.StartedAt).AddHours(rnd.Next(2, 48))
                        });
                    }
                    db.Feedbacks.AddRange(newFeedbacks);
                    db.SaveChanges();
                }

            Console.WriteLine($"[Seeder] Final counts -> Customers: {db.Customers.Count()}, Jobs: {db.JobOrders.Count()}, Tasks: {db.JobTasks.Count()}, Payments: {db.Payments.Count()}, Inquiries: {db.Inquiries.Count()}, Complaints: {db.Complaints.Count()}, Feedbacks: {db.Feedbacks.Count()}");
        }
    }
}
