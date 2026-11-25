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
    public sealed partial class ComplaintDialogForm : Form
    {
        private bool saving;
        private readonly Complaint targetComplaint;
        private readonly Func<MasterCrmDbContext> factory;
        private readonly Dictionary<int, Customer> customerMap = new();
        private readonly Dictionary<int, JobOrder> jobMap = new();
        private JobOrder selectedJob;
        private Customer selectedCustomer;

        public ComplaintDialogForm() : this(new Complaint(), CrmDatabase.CreateContext, Array.Empty<Customer>(), Array.Empty<JobOrder>()) { }

        public ComplaintDialogForm(Complaint complaint, Func<MasterCrmDbContext> factory, IEnumerable<Customer> customers, IEnumerable<JobOrder> jobs)
        {
            this.targetComplaint = complaint ?? new Complaint();
            this.factory = factory ?? CrmDatabase.CreateContext;
            InitializeComponent();
            FormClosing += (s, e) => { if (saving && DialogResult != DialogResult.OK) e.Cancel = true; };

            formTitle.Text = targetComplaint.ComplaintId == 0 ? "Add New Complaint" : $"Edit Complaint {RecordReference.Complaint(targetComplaint.ComplaintId)}";

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

            void SetSelectedCustomer(Customer c)
            {
                selectedCustomer = c;
                if (selectedJob != null) return;
                if (c != null)
                {
                    customerText.Text = $"{c.FullName} — {c.ContactNumber} ({RecordReference.Customer(c.CustomerId)})" + (c.IsActive ? "" : " (inactive)");
                }
                else
                {
                    customerText.Text = "— No customer linked (Anonymous complaint) —";
                }
            }

            void SetSelectedJob(JobOrder j)
            {
                selectedJob = j;
                if (j != null)
                {
                    jobText.Text = FormatJobCaption(j);
                    customerText.Enabled = false;
                    browseCustomerButton.Enabled = false;
                    clearCustomerButton.Enabled = false;
                    customerText.Text = "(Customer is set from the linked Job Order above)";

                    DateTime? compDate = j.CompletedAt ?? j.StartedAt;
                    bool isRecent = compDate.HasValue && (DateTime.Now - compDate.Value).TotalDays <= 30;
                    string warrantyTag = isRecent ? "  ⚠️ [UNDER 30-DAY SERVICE WARRANTY]" : "";
                    customerInfoText.Text = DescribeCustomer(j) + warrantyTag;
                    if (targetComplaint.ComplaintId == 0)
                    {
                        warrantyCheck.Checked = isRecent;
                    }
                }
                else
                {
                    jobText.Text = "No job order (General complaint)";
                    customerInfoText.Text = "General complaint - select a customer below (optional)";
                    customerText.Enabled = true;
                    browseCustomerButton.Enabled = true;
                    clearCustomerButton.Enabled = true;
                    SetSelectedCustomer(selectedCustomer);
                }
            }

            browseJobButton.Click += (s, e) =>
            {
                using var picker = new JobOrderPickerDialogForm(jobList, customerList, selectedJob?.JobOrderId, allowNone: true);
                if (picker.ShowDialog(this) == DialogResult.OK)
                {
                    SetSelectedJob(picker.SelectedJob);
                }
            };
            clearJobButton.Click += (s, e) => SetSelectedJob(null);
            jobText.DoubleClick += (s, e) => browseJobButton.PerformClick();

            browseCustomerButton.Click += (s, e) =>
            {
                using var picker = new CustomerPickerDialogForm(customerList, selectedCustomer?.CustomerId, allowNone: true);
                if (picker.ShowDialog(this) == DialogResult.OK)
                {
                    SetSelectedCustomer(picker.SelectedCustomer);
                }
            };
            clearCustomerButton.Click += (s, e) => SetSelectedCustomer(null);
            customerText.DoubleClick += (s, e) =>
            {
                if (browseCustomerButton.Enabled) browseCustomerButton.PerformClick();
            };

            // Initial selection
            if (targetComplaint.JobOrderId is int jid && jobMap.TryGetValue(jid, out var curJob))
            {
                SetSelectedJob(curJob);
            }
            else
            {
                Customer initCust = null;
                if (targetComplaint.CustomerId is int cid && customerMap.TryGetValue(cid, out var curCust))
                    initCust = curCust;
                SetSelectedCustomer(initCust);
                SetSelectedJob(null);
            }

            SelectComboItem(typeCombo, targetComplaint.ComplaintType, 0);
            SelectComboItem(statusCombo, targetComplaint.ResolutionStatus, 0);
            stateCombo.SelectedIndex = targetComplaint.IsActive ? 0 : 1;
            urgentCheck.Checked = targetComplaint.IsUrgent;
            warrantyCheck.Checked = targetComplaint.UnderWarrantyService;
            descText.Text = targetComplaint.Description ?? "";
            actionText.Text = targetComplaint.ActionTaken ?? "";

            saveButton.Click += async (s, e) =>
            {
                if (saving) return;
                saving = true;
                saveButton.Enabled = false;
                try { await SaveComplaint(); }
                finally { saving = false; if (!saveButton.IsDisposed) saveButton.Enabled = true; }
            };
        }

        private static void SelectComboItem(ComboBox combo, string value, int defaultIndex)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                int idx = combo.FindStringExact(value);
                if (idx >= 0) { combo.SelectedIndex = idx; return; }
                combo.Items.Add(value);
                combo.SelectedItem = value;
                return;
            }
            if (combo.Items.Count > defaultIndex) combo.SelectedIndex = defaultIndex;
        }

        private async System.Threading.Tasks.Task SaveComplaint()
        {
            errorLabel.Text = "";

            string type = typeCombo.Text.Trim();
            string status = statusCombo.Text.Trim();
            string desc = descText.Text.Trim();
            string action = actionText.Text.Trim();
            bool isActive = stateCombo.Text == "Active";

            if (string.IsNullOrWhiteSpace(type))
            {
                errorLabel.Text = "Please select a Complaint Type from the dropdown.";
                typeCombo.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(status))
            {
                errorLabel.Text = "Please select a Resolution Status from the dropdown.";
                statusCombo.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(desc))
            {
                errorLabel.Text = "Description is required.";
                descText.Focus();
                return;
            }
            if ((status.Equals("Resolved", StringComparison.OrdinalIgnoreCase) || status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                && string.IsNullOrWhiteSpace(action))
            {
                errorLabel.Text = $"Please enter the Action Taken before marking this complaint as {status}.";
                actionText.Focus();
                return;
            }

            try
            {
                using var db = factory();
                var strategy = db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var tx = db.Database.BeginTransaction();
                    bool isNew = targetComplaint.ComplaintId == 0;
                    var entity = isNew ? targetComplaint : db.Complaints.Find(targetComplaint.ComplaintId);
                    if (entity == null) throw new InvalidOperationException("Complaint record no longer exists.");

                    entity.JobOrderId = selectedJob?.JobOrderId;
                    entity.CustomerId = selectedJob != null ? (selectedJob.CustomerId ?? selectedCustomer?.CustomerId) : selectedCustomer?.CustomerId;
                    entity.ComplaintType = type;
                    entity.Description = desc;
                    entity.ResolutionStatus = status;
                    entity.ActionTaken = action;
                    entity.IsActive = isActive;
                    entity.IsUrgent = urgentCheck.Checked;
                    entity.UnderWarrantyService = warrantyCheck.Checked;

                    if (!isNew) db.Entry(entity).Property("RowVersion").OriginalValue = targetComplaint.RowVersion;
                    if (isNew) db.Complaints.Add(entity);
                    else db.Complaints.Update(entity);

                    await db.SaveChangesAsync();
                    await tx.CommitAsync();
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

        private sealed record CustomerChoice(int Id, string Caption, Customer Customer)
        {
            public override string ToString() => Caption;
        }
    }
}
