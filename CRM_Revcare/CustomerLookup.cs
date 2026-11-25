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
    internal sealed class CustomerLookup
    {
        private readonly TextBox textBoxInput;
        private readonly ComboBox comboBoxInput;
        private readonly Func<MasterCrmDbContext> factory;
        private readonly Label result;
        private Dictionary<int, Customer> known = new();
        private int? selectedId;
        public event Action Changed;
        public Customer SelectedCustomer { get; private set; }
        public int? SelectedId => selectedId ?? SelectedCustomer?.CustomerId ?? (comboBoxInput?.SelectedItem is Choice c && c.Id > 0 ? c.Id : null);

        public CustomerLookup(TextBox input, Func<MasterCrmDbContext> factory, Label result)
        {
            this.textBoxInput = input;
            this.factory = factory;
            this.result = result;
            input.ReadOnly = true;
            input.BackColor = Color.White;
            input.PlaceholderText = "Click [ 🔍 Browse ] to select customer...";
            input.DoubleClick += (s, e) => OpenPickerDialog(input.FindForm());
            input.Click += (s, e) => { if (SelectedCustomer == null) OpenPickerDialog(input.FindForm()); };
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

        public CustomerLookup(ComboBox input, Func<MasterCrmDbContext> factory, Label result)
        {
            this.comboBoxInput = input;
            this.factory = factory;
            this.result = result;
            input.DisplayMember = nameof(Choice.Caption);
            input.ValueMember = nameof(Choice.Id);
            input.SelectedIndexChanged += (s, e) =>
            {
                var choice = input.SelectedItem as Choice;
                selectedId = choice?.Id > 0 ? choice.Id : null;
                SelectedCustomer = choice?.Customer;
                Changed?.Invoke();
            };
            Replace(Array.Empty<Customer>());
            input.DropDown += (s, e) =>
            {
                try { using var db = factory(); Replace(db.Customers.AsNoTracking().ToList()); }
                catch { result.Text = "Could not load customers. Refresh Cloud and try again."; }
            };
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

        public void Clear()
        {
            selectedId = null;
            SelectedCustomer = null;
            if (textBoxInput != null) textBoxInput.Text = "";
            if (comboBoxInput != null && comboBoxInput.Items.Count > 0) comboBoxInput.SelectedIndex = 0;
            Changed?.Invoke();
        }

        public void OpenPickerDialog(IWin32Window owner = null)
        {
            try
            {
                using var db = factory();
                Replace(db.Customers.AsNoTracking().ToList());
            }
            catch { }

            using var picker = new CustomerPickerDialogForm(known.Values, SelectedId, allowNone: true);
            var parentForm = owner ?? textBoxInput?.FindForm() ?? comboBoxInput?.FindForm();
            if (picker.ShowDialog(parentForm) == DialogResult.OK)
            {
                Select(picker.SelectedCustomerId);
                Changed?.Invoke();
            }
        }

        public void Select(int? id)
        {
            int key = id ?? 0;
            selectedId = key > 0 ? key : null;
            if (key > 0)
            {
                if (!known.TryGetValue(key, out var customer))
                {
                    try
                    {
                        using var db = factory();
                        customer = db.Customers.AsNoTracking().FirstOrDefault(c => c.CustomerId == key);
                        if (customer != null) known[key] = customer;
                    }
                    catch { }
                }

                if (customer != null)
                {
                    SelectedCustomer = customer;
                    if (textBoxInput != null)
                    {
                        textBoxInput.Text = $"{customer.FullName} — {customer.ContactNumber} ({RecordReference.Customer(customer.CustomerId)})" + (customer.IsActive ? "" : " [Inactive]");
                        textBoxInput.ForeColor = customer.IsActive ? SystemColors.WindowText : Color.DarkRed;
                    }
                }
                else
                {
                    SelectedCustomer = null;
                    if (textBoxInput != null)
                    {
                        textBoxInput.Text = RecordReference.Customer(key);
                        textBoxInput.ForeColor = SystemColors.WindowText;
                    }
                }
            }
            else
            {
                selectedId = null;
                SelectedCustomer = null;
                if (textBoxInput != null)
                {
                    textBoxInput.Text = "";
                    textBoxInput.ForeColor = SystemColors.WindowText;
                }
            }

            if (comboBoxInput != null)
            {
                var choice = comboBoxInput.Items.Cast<Choice>().FirstOrDefault(c => c.Id == key);
                if (choice == null)
                {
                    choice = known.TryGetValue(key, out var cust)
                        ? new Choice(key, $"{cust.FullName} — {cust.ContactNumber} ({RecordReference.Customer(cust.CustomerId)})" + (cust.IsActive ? "" : " (inactive)"), cust)
                        : new Choice(key, $"Unavailable customer — refresh choices");
                    comboBoxInput.Items.Add(choice);
                }
                comboBoxInput.SelectedItem = choice;
            }
        }

        public void Replace(IEnumerable<Customer> rows)
        {
            var selected = SelectedId;
            known = rows.ToDictionary(c => c.CustomerId);
            if (selected.HasValue)
            {
                Select(selected.Value);
            }
            if (comboBoxInput != null)
            {
                var choices = known.Values.Where(c => c.IsActive || c.CustomerId == selected).OrderBy(c => c.FullName)
                    .Select(c => new Choice(c.CustomerId, $"{c.FullName} — {c.ContactNumber} ({RecordReference.Customer(c.CustomerId)})" + (c.IsActive ? "" : " (inactive)"), c)).ToArray();
                comboBoxInput.BeginUpdate();
                try
                {
                    comboBoxInput.Items.Clear();
                    comboBoxInput.Items.Add(new Choice(0, "— Select a customer —"));
                    comboBoxInput.Items.AddRange(choices);
                    Select(selected);
                }
                finally { comboBoxInput.EndUpdate(); }
            }
        }

        public int? Read(MasterCrmDbContext db, bool required, int? previousId)
        {
            var id = SelectedId ?? (previousId.HasValue && previousId.Value > 0 ? previousId : null);
            if (id == null)
            {
                if (required)
                {
                    Control target = (Control)textBoxInput ?? comboBoxInput;
                    target?.Focus();
                    throw new InvalidOperationException("Select a customer. Click [ 🔍 Browse ] to choose one.");
                }
                return null;
            }
            var customer = db.Customers.Find(id.Value);
            if (customer == null || (!customer.IsActive && id != previousId))
                throw new InvalidOperationException("Select an existing active customer. Inactive customers remain on historical records only.");
            return id;
        }

        private sealed record Choice(int Id, string Caption, Customer Customer = null);
    }
}
