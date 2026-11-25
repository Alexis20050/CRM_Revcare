using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public partial class CrmTransactionsForm
    {
        private readonly List<CustomerLookup> customerLookups = new();
        private Dictionary<int, Customer> customerRecords = new();
        private CustomerLookup BindCustomer(TextBox input, TransactionPage page, Func<MasterCrmDbContext> factory)
        {
            var lookup = new CustomerLookup(input, factory, page.FindControl<Label>("Result"));
            customerLookups.Add(lookup);
            return lookup;
        }
        private void UpdateCustomers(IEnumerable<Customer> rows)
        {
            var list = rows.ToList();
            customerRecords = list.ToDictionary(c => c.CustomerId);
            foreach (var lookup in customerLookups) lookup.Replace(list);
            foreach (var lookup in jobLookups) lookup.ReplaceCustomers(list);
            refreshTaskJobs?.Invoke();
            inquiriesPage.Grid.Invalidate();
            complaintsPage.Grid.Invalidate();
            feedbackPage.Grid.Invalidate();
            ordersPage.Grid.Invalidate();
            tasksPage.Grid.Invalidate();
            paymentsPage.Grid.Invalidate();
        }
        private CrmCollectionController<Customer> BuildCustomers(Func<MasterCrmDbContext> factory, Func<string, bool> confirm)
        {
            var tab = new CrmCollectionController<Customer>(customersPage, "Customer", x => x.CustomerId, factory, confirm);
            UseEntryDialog(tab, customersPage.editorPanel, customersPage.resultLabel, customersPage.saveButton, "Customer");
            customersPage.Grid.CellFormatting += (s, e) =>
            {
                if (e.ColumnIndex < 0) return;
                string prop = customersPage.Grid.Columns[e.ColumnIndex].DataPropertyName;
                if (prop == "CreatedAt" && e.Value is DateTime dt)
                {
                    e.Value = dt.ToString("yyyy-MM-dd HH:mm");
                    e.FormattingApplied = true;
                }
                else if (prop == "CustomerId" && e.Value is int cid)
                {
                    e.Value = RecordReference.Customer(cid);
                    e.FormattingApplied = true;
                }
                else if (prop == "ContactNumber" && e.Value is string rawPhone)
                {
                    string classification = CRM.infrastructure.services.ContactNumberHelper.Classify(rawPhone);
                    if (classification == CRM.infrastructure.services.ContactNumberHelper.ClassificationDemo)
                    {
                        e.Value = $"{rawPhone} [Demo]";
                        e.FormattingApplied = true;
                    }
                    else if (classification == CRM.infrastructure.services.ContactNumberHelper.ClassificationInvalid)
                    {
                        e.Value = $"{rawPhone} [Invalid]";
                        e.FormattingApplied = true;
                    }
                    else if (classification == CRM.infrastructure.services.ContactNumberHelper.ClassificationMissing)
                    {
                        e.Value = "[Missing]";
                        e.FormattingApplied = true;
                    }
                }
            };

            void UpdateContactFormatLabel(string text)
            {
                string classification = CRM.infrastructure.services.ContactNumberHelper.Classify(text);
                if (string.IsNullOrWhiteSpace(text))
                {
                    customersPage.contactFormatLabel.Text = "Format: 09XX-XXX-XXXX or +63 9XX XXX XXXX (Required)";
                    customersPage.contactFormatLabel.ForeColor = System.Drawing.SystemColors.GrayText;
                }
                else if (classification == CRM.infrastructure.services.ContactNumberHelper.ClassificationValid)
                {
                    customersPage.contactFormatLabel.Text = "✓ Valid mobile format";
                    customersPage.contactFormatLabel.ForeColor = System.Drawing.Color.FromArgb(22, 101, 52);
                }
                else if (classification == CRM.infrastructure.services.ContactNumberHelper.ClassificationDemo)
                {
                    customersPage.contactFormatLabel.Text = "ℹ Demo placeholder (legacy/fictional format)";
                    customersPage.contactFormatLabel.ForeColor = System.Drawing.Color.FromArgb(146, 64, 14);
                }
                else
                {
                    customersPage.contactFormatLabel.Text = "⚠ Invalid format: Expected 11-digit local (09...) or +639...";
                    customersPage.contactFormatLabel.ForeColor = System.Drawing.Color.FromArgb(185, 28, 28);
                }
            }

            customersPage.contact.TextChanged += (s, e) => UpdateContactFormatLabel(customersPage.contact.Text);

            void OpenSelectedCustomerProfile()
            {
                if (customersPage.Grid.CurrentRow?.DataBoundItem is Customer c && c.CustomerId > 0)
                {
                    using var profileDialog = new CustomerProfileDialogForm(c.CustomerId, factory);
                    profileDialog.ShowDialog(customersPage.FindForm());
                }
            }

            customersPage.Grid.SelectionChanged += (s, e) =>
            {
                bool hasCust = customersPage.Grid.CurrentRow?.DataBoundItem is Customer c && c.CustomerId > 0;
                customersPage.btnViewProfile.Enabled = hasCust;
            };

            customersPage.btnViewProfile.Click += (s, e) => OpenSelectedCustomerProfile();
            customersPage.Grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OpenSelectedCustomerProfile();
            };

            tab.ConfigureEditor(row =>
            {
                customersPage.fullName.Text = row.FullName;
                customersPage.contact.Text = row.ContactNumber;
                customersPage.email.Text = row.Email;
                customersPage.active.Checked = row.IsActive;
                UpdateContactFormatLabel(customersPage.contact.Text);
            }, (row, db) =>
            {
                string name = Required(customersPage.fullName, "Full name");
                string contact = Required(customersPage.contact, "Contact number");
                string email = customersPage.email.Text.Trim();
                if (email.Length > 0 && (!MailAddress.TryCreate(email, out var address) || address.Address != email))
                    throw new InvalidOperationException("Enter a valid email address or leave it blank.");
                bool isExistingPlaceholder = row.CustomerId != 0 && contact == row.ContactNumber && CRM.infrastructure.services.ContactNumberHelper.IsFictionalOrPlaceholder(contact);
                var valResult = CRM.infrastructure.services.ContactNumberHelper.Validate(contact, allowExistingPlaceholder: isExistingPlaceholder);
                if (!valResult.IsValid)
                    throw new InvalidOperationException(valResult.ErrorMessage);
                contact = CRM.infrastructure.services.ContactNumberHelper.Normalize(contact);
                string digits = CRM.infrastructure.services.ContactNumberHelper.ExtractDigits(contact);
                var candidates = db.Customers.AsNoTracking().Where(c => c.CustomerId != row.CustomerId).ToList()
                    .Where(c => c.FullName.Equals(name, StringComparison.OrdinalIgnoreCase)
                        || (digits.Length >= 7 && CRM.infrastructure.services.ContactNumberHelper.ExtractDigits(c.ContactNumber) == digits)).Take(5).ToList();
                if (candidates.Count > 0 && !confirm("Possible existing customers:\n" +
                    string.Join("\n", candidates.Select(c => $"{RecordReference.Customer(c.CustomerId)}: {c.FullName} / {c.ContactNumber}")) +
                    "\nSave as a separate customer? Choose No to use an existing record."))
                    throw new InvalidOperationException("Customer not saved. Select an existing customer or review the details.");
                row.FullName = name; row.ContactNumber = contact; row.Email = email;
                row.IsActive = customersPage.active.Checked;
            });
            tab.ConfigureListTools(c =>
            {
                string query = customersPage.search.Text.Trim();
                return query.Length == 0 || RecordReference.Matches(RecordReference.CustomerPrefix, c.CustomerId, query)
                    || c.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || CRM.infrastructure.services.ContactNumberHelper.Matches(c.ContactNumber, query)
                    || c.Email.Contains(query, StringComparison.OrdinalIgnoreCase);
            }, (visible, total) =>
            {
                string query = customersPage.search.Text.Trim();
                customersPage.countLabel.Text = visible == 0 && total > 0
                    ? $"No matching records found for \"{query}\"."
                    : total == 0 ? "No customers registered."
                    : $"Showing {visible} of {total} customers.";
            });

            var searchDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
            searchDebounceTimer.Tick += (s, e) =>
            {
                searchDebounceTimer.Stop();
                tab.ApplyListFilter();
            };
            customersPage.search.TextChanged += (s, e) =>
            {
                searchDebounceTimer.Stop();
                searchDebounceTimer.Start();
            };
            customersPage.searchButton.Click += (s, e) =>
            {
                searchDebounceTimer.Stop();
                tab.ApplyListFilter();
            };
            customersPage.clearSearch.Click += (s, e) =>
            {
                searchDebounceTimer.Stop();
                customersPage.search.Clear();
                tab.ApplyListFilter();
            };
            tab.RecordsLoaded += UpdateCustomers;
            return tab;
        }
    }
}
