namespace crm.domain.entities;

public static class JobOrderWorkflow
{
    public static string Normalize(string status) => status == "QA Checking" ? "In Progress" : status;
    public static int Stage(string status) => Normalize(status) switch
    {
        "In Queue" => 0, "In Progress" => 1, "Completed" => 2, "Released" => 3, "Cancelled" => 99, _ => -1
    };
    public static bool IsCancelled(string status) => string.Equals(Normalize(status), "Cancelled", StringComparison.OrdinalIgnoreCase);
}
