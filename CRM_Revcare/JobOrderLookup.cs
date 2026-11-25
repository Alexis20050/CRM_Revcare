using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    // Data binding only: supports both TextBox (Enterprise Lookup) and ComboBox (legacy dropdown).
    internal sealed class JobOrderLookup
    {
        private readonly TextBox textBoxInput;
        private readonly ComboBox comboBoxInput;
        private readonly bool required;
        private readonly Func<MasterCrmDbContext> factory;
        private readonly Label result;
        private int? selectedJobId;
        private List<JobOrder> jobs = new();
        private Dictionary<int, Customer> customers = new();
        public event Action Changed;
        public int? SelectedJobId => textBoxInput != null ? selectedJobId : (SelectedId > 0 ? SelectedId : null);
        public JobOrder SelectedJob => jobs.FirstOrDefault(j => j.JobOrderId == (SelectedJobId ?? 0));
        public string CustomerDescription
        {
            get
            {
                var job = SelectedJob;
                if (job == null) return "Select a job order";
                return CustomerFor(job);
            }
        }
        private string CustomerFor(JobOrder job) => job.CustomerId is int id && customers.TryGetValue(id, out var c)
            ? $"{c.FullName} | {c.ContactNumber}" : "Customer not linked - review Job Orders";

        private string FormatChoice(JobOrder j)
        {
            string cust = j.CustomerId is int id && customers.TryGetValue(id, out var c) ? c.FullName : "Unlinked";
            string phone = j.CustomerId is int cid && customers.TryGetValue(cid, out var cc) ? cc.ContactNumber : "";
            string vehicleRef = CRM.infrastructure.services.HistoricalCompatibility.Reference(j) + " / " + j.VehicleDisplay;
            string phonePart = string.IsNullOrWhiteSpace(phone) ? "" : $" ({phone})";
            return $"{RecordReference.JobOrder(j.JobOrderId)} — {cust}{phonePart} | {vehicleRef} [{j.Status}]";
        }

        private static string CleanReference(string estimateRef, string customerName)
        {
            if (string.IsNullOrWhiteSpace(estimateRef)) return "-";
            string text = estimateRef.Trim();
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                text = text.Replace(" / " + customerName + " / ", " / ")
                           .Replace(" / " + customerName, "")
                           .Replace(customerName + " / ", "");
            }
            return text.Trim(' ', '/', '-');
        }

        public void ReplaceCustomers(IEnumerable<Customer> rows)
        {
            customers = rows.ToDictionary(c => c.CustomerId);
            Replace(jobs.ToArray());
        }

        public JobOrderLookup(TextBox input, bool required, Func<MasterCrmDbContext> factory, Label result)
        {
            this.textBoxInput = input;
            this.required = required;
            this.factory = factory;
            this.result = result;
            input.ReadOnly = true;
            input.BackColor = System.Drawing.Color.White;
            input.PlaceholderText = required ? "Click [ 🔍 Browse ] to select job order..." : "No job order (click Browse to link)";
            input.DoubleClick += (s, e) => OpenPickerDialog(input.FindForm());
            input.Click += (s, e) => { if (selectedJobId == null) OpenPickerDialog(input.FindForm()); };
            input.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2 || e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    OpenPickerDialog(input.FindForm());
                }
                else if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.Back)
                {
                    e.Handled = true;
                    Clear();
                }
            };
        }

        public JobOrderLookup(ComboBox input, bool required, Func<MasterCrmDbContext> factory, Label result)
        {
            this.comboBoxInput = input;
            this.required = required;
            this.factory = factory;
            this.result = result;
            input.DisplayMember = nameof(Choice.Caption);
            input.ValueMember = nameof(Choice.Id);
            input.DropDownWidth = Math.Max(input.Width, 620);
            Replace(Array.Empty<JobOrder>());
            input.SelectedIndexChanged += (s, e) => Changed?.Invoke();
            input.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2)
                {
                    e.Handled = true;
                    OpenPickerDialog(input.FindForm());
                }
            };
            input.DropDown += (s, e) =>
            {
                try
                {
                    using var db = factory();
                    customers = db.Customers.AsNoTracking().ToDictionary(c => c.CustomerId);
                    Replace(db.JobOrders.AsNoTracking().ToList());
                }
                catch
                {
                    result.Text = "Could not refresh job choices. Previous selection kept; try Refresh Cloud.";
                }
            };
        }

        public void Clear()
        {
            selectedJobId = null;
            if (textBoxInput != null) textBoxInput.Text = "";
            if (comboBoxInput != null && comboBoxInput.Items.Count > 0) comboBoxInput.SelectedIndex = 0;
            Changed?.Invoke();
        }

        public bool OpenPickerDialog(IWin32Window owner = null)
        {
            using var dlg = new JobOrderPickerDialogForm(jobs, customers.Values, SelectedJobId, !required);
            Control ctrl = (Control)textBoxInput ?? comboBoxInput;
            if (dlg.ShowDialog(owner ?? ctrl?.FindForm()) == DialogResult.OK)
            {
                Select(dlg.SelectedJobOrderId);
                return true;
            }
            return false;
        }

        public void AttachButtons(Button browseButton, Button clearButton = null)
        {
            if (browseButton != null)
            {
                browseButton.Click += (s, e) => OpenPickerDialog(browseButton.FindForm());
            }
            if (clearButton != null)
            {
                clearButton.Click += (s, e) => Clear();
            }
        }

        public void AttachBrowseButton(Button button) => AttachButtons(button);

        private int SelectedId => comboBoxInput != null && comboBoxInput.SelectedItem is Choice choice ? choice.Id : (selectedJobId ?? 0);
        public string Describe(int id)
        {
            if (comboBoxInput != null)
            {
                var choice = comboBoxInput.Items.Cast<Choice>().FirstOrDefault(c => c.Id == id && c.Id > 0);
                if (choice != null) return choice.Caption;
            }
            var job = jobs.FirstOrDefault(j => j.JobOrderId == id);
            if (job != null) return FormatChoice(job);
            return RecordReference.JobOrder(id);
        }
        public void Select(int? id)
        {
            int value = id ?? 0;
            selectedJobId = value > 0 ? value : null;
            if (textBoxInput != null)
            {
                if (selectedJobId.HasValue)
                {
                    var job = jobs.FirstOrDefault(j => j.JobOrderId == selectedJobId.Value);
                    textBoxInput.Text = job != null ? FormatChoice(job) : RecordReference.JobOrder(selectedJobId.Value);
                }
                else
                {
                    textBoxInput.Text = "";
                }
                Changed?.Invoke();
                return;
            }

            if (comboBoxInput != null)
            {
                var match = comboBoxInput.Items.Cast<Choice>().FirstOrDefault(c => c.Id == value);
                if (match == null)
                {
                    match = new Choice(value, $"{RecordReference.JobOrder(value)} — unavailable; refresh or choose another job");
                    comboBoxInput.Items.Add(match);
                }
                comboBoxInput.SelectedItem = match;
            }
        }
        public void Replace(IEnumerable<JobOrder> rows)
        {
            jobs = rows.ToList();
            if (textBoxInput != null)
            {
                if (selectedJobId.HasValue)
                {
                    var job = jobs.FirstOrDefault(j => j.JobOrderId == selectedJobId.Value);
                    textBoxInput.Text = job != null ? FormatChoice(job) : RecordReference.JobOrder(selectedJobId.Value);
                }
                return;
            }

            if (comboBoxInput != null)
            {
                int selected = SelectedId;
                var choices = jobs.OrderByDescending(j => j.JobOrderId)
                    .Select(j => new Choice(j.JobOrderId, FormatChoice(j))).ToArray();
                comboBoxInput.BeginUpdate();
                try
                {
                    comboBoxInput.Items.Clear();
                    comboBoxInput.Items.Add(new Choice(0, required ? "Select a job order..." : "No job order (general complaint)"));
                    comboBoxInput.Items.AddRange(choices);
                    Select(selected);
                }
                finally { comboBoxInput.EndUpdate(); }
            }
        }
        public int? Read(MasterCrmDbContext db)
        {
            int id = SelectedId;
            if (id == 0 && !required) return null;
            if (id <= 0 || !db.JobOrders.Any(j => j.JobOrderId == id))
            {
                Control ctrl = (Control)textBoxInput ?? comboBoxInput;
                ctrl?.Focus();
                throw new InvalidOperationException("Select an existing job order. Click [ 🔍 Browse ] to choose one.");
            }
            return id;
        }
        private sealed record Choice(int Id, string Caption);
    }
}
