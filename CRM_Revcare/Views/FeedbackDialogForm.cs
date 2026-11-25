using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public sealed partial class FeedbackDialogForm : Form
    {
        private bool saving;
        private readonly Feedback targetFeedback;
        private readonly Func<MasterCrmDbContext> factory;
        private readonly Dictionary<int, Customer> customerMap = new();
        private readonly Dictionary<int, JobOrder> jobMap = new();
        private JobOrder selectedJob;

        public FeedbackDialogForm() : this(new Feedback(), CrmDatabase.CreateContext, Array.Empty<Customer>(), Array.Empty<JobOrder>()) { }

        public FeedbackDialogForm(Feedback feedback, Func<MasterCrmDbContext> factory, IEnumerable<Customer> customers, IEnumerable<JobOrder> jobs)
        {
            this.targetFeedback = feedback ?? new Feedback();
            this.factory = factory ?? CrmDatabase.CreateContext;
            InitializeComponent();
            FormClosing += (s, e) => { if (saving && DialogResult != DialogResult.OK) e.Cancel = true; };

            formTitle.Text = targetFeedback.FeedbackId == 0 ? "Add New Feedback" : $"Edit Feedback {RecordReference.Feedback(targetFeedback.FeedbackId)}";

            var customerList = customers?.ToList() ?? new List<Customer>();
            if (customerList.Count == 0)
            {
                try { using var db = this.factory(); customerList = db.Customers.AsNoTracking().ToList(); } catch { }
            }
            var jobList = jobs?.ToList() ?? new List<JobOrder>();
            if (jobList.Count == 0)
            {
                try { using var db = this.factory(); jobList = db.JobOrders.AsNoTracking().ToList(); } catch { }
            }

            foreach (var c in customerList) customerMap[c.CustomerId] = c;
            foreach (var j in jobList) jobMap[j.JobOrderId] = j;

            string DescribeCustomer(JobOrder j) =>
                j.CustomerId is int cid && customerMap.TryGetValue(cid, out var c)
                    ? $"{c.FullName} — {c.ContactNumber}"
                    : "No verified customer on this job";

            static string CleanRef(string r, string name)
            {
                if (string.IsNullOrWhiteSpace(r)) return "-";
                string t = r.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    t = t.Replace(" / " + name + " / ", " / ").Replace(" / " + name, "").Replace(name + " / ", "");
                }
                return t.Trim(' ', '/', '-');
            }

            string FormatJobCaption(JobOrder j)
            {
                string cust = j.CustomerId is int cid && customerMap.TryGetValue(cid, out var c) ? c.FullName : "Unlinked";
                string phone = j.CustomerId is int cid2 && customerMap.TryGetValue(cid2, out var c2) ? $" ({c2.ContactNumber})" : "";
                string vRef = CleanRef(j.EstimateId, cust);
                return $"{RecordReference.JobOrder(j.JobOrderId)} — {cust}{phone} | {vRef} [{j.Status}]";
            }

            void SetSelectedJob(JobOrder j)
            {
                selectedJob = j;
                if (j != null)
                {
                    jobText.Text = FormatJobCaption(j);
                    customerText.Text = DescribeCustomer(j);
                }
                else
                {
                    jobText.Text = "";
                    customerText.Text = "Click [ 🔍 Browse ] above to select a job order";
                }
            }

            browseJobButton.Click += (s, e) =>
            {
                using var picker = new JobOrderPickerDialogForm(jobList, customerList, selectedJob?.JobOrderId, allowNone: false);
                if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedJob != null)
                {
                    SetSelectedJob(picker.SelectedJob);
                }
            };
            clearJobButton.Click += (s, e) => SetSelectedJob(null);
            jobText.DoubleClick += (s, e) => browseJobButton.PerformClick();

            // Initial selection
            if (targetFeedback.JobOrderId > 0 && jobMap.TryGetValue(targetFeedback.JobOrderId, out var curJob))
            {
                SetSelectedJob(curJob);
            }
            else
            {
                SetSelectedJob(null);
            }

            // Rating dropdown (drop down only!)
            int score = targetFeedback.RatingScore >= 1 && targetFeedback.RatingScore <= 5 ? targetFeedback.RatingScore : 5;
            ratingCombo.SelectedIndex = 5 - score; // index 0 = 5 stars, index 4 = 1 star

            stateCombo.SelectedIndex = targetFeedback.IsActive ? 0 : 1;
            positiveText.Text = targetFeedback.PositiveRemarks ?? "";
            improvementsText.Text = targetFeedback.AreasForImprovement ?? "";

            saveButton.Click += async (s, e) =>
            {
                if (saving) return;
                saving = true;
                saveButton.Enabled = false;
                try { await SaveFeedback(); }
                finally { saving = false; if (!saveButton.IsDisposed) saveButton.Enabled = true; }
            };
        }

        private async System.Threading.Tasks.Task SaveFeedback()
        {
            errorLabel.Text = "";

            if (selectedJob == null || selectedJob.JobOrderId <= 0)
            {
                errorLabel.Text = "Please select a valid Job Order. Click [ 🔍 Browse ] to choose one.";
                browseJobButton.Focus();
                return;
            }

            int ratingScore = 5 - ratingCombo.SelectedIndex;
            if (ratingScore < 1 || ratingScore > 5) ratingScore = 5;

            string positive = positiveText.Text.Trim();
            string improvements = improvementsText.Text.Trim();
            bool isActive = stateCombo.Text == "Active";

            try
            {
                using var db = factory();

                // Prevent duplicate reviews for the same Job Order
                bool duplicate = db.Feedbacks.Any(f => f.JobOrderId == selectedJob.JobOrderId && f.FeedbackId != targetFeedback.FeedbackId);
                if (duplicate)
                {
                    errorLabel.Text = $"Job Order #{selectedJob.JobOrderId} already has feedback recorded. Please edit the existing record.";
                    browseJobButton.Focus();
                    return;
                }

                var strategy = db.Database.CreateExecutionStrategy();
                int savedFeedbackId = 0;
                await strategy.ExecuteAsync(async () =>
                {
                    using var tx = db.Database.BeginTransaction();
                    bool isNew = targetFeedback.FeedbackId == 0;
                    var entity = isNew ? targetFeedback : db.Feedbacks.Find(targetFeedback.FeedbackId);
                    if (entity == null) throw new InvalidOperationException("Feedback record no longer exists.");

                    entity.JobOrderId = selectedJob.JobOrderId;
                    entity.LegacyCustomerId = selectedJob.CustomerId ?? 0;
                    entity.RatingScore = ratingScore;
                    entity.PositiveRemarks = positive;
                    entity.AreasForImprovement = improvements;
                    entity.IsActive = isActive;

                    if (!isNew) db.Entry(entity).Property("RowVersion").OriginalValue = targetFeedback.RowVersion;
                    if (isNew) db.Feedbacks.Add(entity);
                    else db.Feedbacks.Update(entity);

                    await db.SaveChangesAsync();
                    await tx.CommitAsync();
                    savedFeedbackId = entity.FeedbackId;
                });

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                errorLabel.Text = ex is DbUpdateConcurrencyException ? "This record changed. Cancel, refresh and reopen it." : ex is InvalidOperationException ? ex.Message : "Could not save. Check the connection and linked records, then refresh and try again.";
            }
        }

        private sealed record JobChoice(int Id, string Caption, JobOrder Job)
        {
            public override string ToString() => Caption;
        }
    }
}
