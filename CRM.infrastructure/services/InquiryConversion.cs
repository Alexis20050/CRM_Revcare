using System.Data;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM.infrastructure.services;

public static class InquiryConversion
{
    public static int Convert(Func<DbContext> factory, int inquiryId, int? customerId, string vehicle)
    {
        using var db = factory();
        return db.Database.CreateExecutionStrategy().Execute(() =>
        {
            using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
            var inquiry = db.Set<Inquiry>().FromSqlInterpolated($"SELECT * FROM Inquiries WITH (UPDLOCK,HOLDLOCK) WHERE InquiryId = {inquiryId}").Single();
            if (inquiry.ConvertedJobOrderId is int existing) return existing;
            if (!inquiry.IsActive) throw new InvalidOperationException("Reactivate the inquiry before converting it.");
            customerId ??= inquiry.CustomerId;
            if (customerId == null)
            {
                if (string.IsNullOrWhiteSpace(inquiry.CustomerName) || string.IsNullOrWhiteSpace(inquiry.ContactNumber))
                    throw new InvalidOperationException("Select a registered customer or complete the inquiry's customer name and contact number.");
                if (db.Set<Customer>().Any(c => c.FullName == inquiry.CustomerName || c.ContactNumber == inquiry.ContactNumber))
                    throw new InvalidOperationException("A possible customer match exists. Select that customer explicitly before converting.");
                var customer = new Customer { FullName = inquiry.CustomerName.Trim(), ContactNumber = inquiry.ContactNumber.Trim() };
                db.Set<Customer>().Add(customer); db.SaveChanges();
                customerId = customer.CustomerId;
            }
            if (string.IsNullOrWhiteSpace(vehicle)) vehicle = inquiry.VehicleDetails;
            if (string.IsNullOrWhiteSpace(vehicle)) throw new InvalidOperationException("Enter vehicle details before converting.");
            var job = new JobOrder { CustomerId = customerId, EstimateId = "Inquiry #" + inquiryId,
                VehicleDetails = vehicle.Trim(), InquiryNotes = inquiry.MessageDetails, StartedAt = DateTime.Now };
            db.Set<JobOrder>().Add(job); db.SaveChanges();
            inquiry.CustomerId = customerId;
            inquiry.ConvertedJobOrderId = job.JobOrderId;
            inquiry.Status = "Converted";
            db.SaveChanges(); tx.Commit();
            return job.JobOrderId;
        });
    }
}
