using System.ComponentModel;

namespace CRM_Revcare_
{
    public sealed class JobOverviewItem
    {
        public int JobOrderId { get; set; }
        public string Customer { get; set; } = "";
        public string EstimateId { get; set; } = "";
        public string Status { get; set; } = "";
        public string StartedAt { get; set; } = "";
        public string Vehicle { get; set; } = "";
        public string Progress { get; set; } = "";

        [Browsable(false)]
        public int DoneTasks { get; set; }

        [Browsable(false)]
        public int TotalTasks { get; set; }

        [Browsable(false)]
        public double ProgressRatio { get; set; }

        [Browsable(false)]
        public bool IsCompletedOrReleased { get; set; }
    }
}
