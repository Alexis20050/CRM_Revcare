using System;
using crm.domain.entities;

namespace CRM_Revcare_;

internal static class JobSearch
{
    internal static bool Matches(JobOrder job, Customer customer, string query)
    {
        query = query?.Trim() ?? "";
        return query.Length == 0 || RecordReference.Matches(RecordReference.JobOrderPrefix, job.JobOrderId, query)
            || $"{customer?.FullName} {customer?.ContactNumber} {job.EstimateNumber} {job.EstimateId} {job.VehicleDisplay} {job.Status}"
                .Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
