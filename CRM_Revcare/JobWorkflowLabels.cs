namespace CRM_Revcare_;

internal static class JobWorkflowLabels
{
    internal static string FilterStatus(int index) => index switch
    {
        1 => "In Queue", 2 => "In Progress", 3 => "Completed",
        4 => "Released", _ => null
    };

    internal static string NextAction(string status) => status switch
    {
        "In Queue" => "Start Work",
        "In Progress" => "Complete Job",
        "QA Checking" => "Complete Job",
        "Completed" => "Release Vehicle",
        "Released" => "Already Released",
        _ => "Select a Job"
    };

    internal static string NextPermission(string status) => status switch
    {
        "In Queue" => "Jobs.Start", "In Progress" or "QA Checking" => "Jobs.QA",
        "Completed" => "Jobs.Release", _ => null
    };
}
