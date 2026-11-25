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
    public sealed partial class InquiryDialogForm : Form
    {
        private bool saving;
        private readonly Inquiry targetInquiry;
        private readonly Func<MasterCrmDbContext> factory;
        private readonly Dictionary<int, Customer> customerMap = new();
        private Customer selectedCustomer;

        public InquiryDialogForm() : this(new Inquiry(), CrmDatabase.CreateContext, Array.Empty<Customer>()) { }

        public InquiryDialogForm(Inquiry inquiry, Func<MasterCrmDbContext> factory, IEnumerable<Customer> customers)
        {
            this.targetInquiry = inquiry ?? new Inquiry();
            this.factory = factory ?? CrmDatabase.CreateContext;
            InitializeComponent();
            FormClosing += (s, e) => { if (saving && DialogResult != DialogResult.OK) e.Cancel = true; };

            formTitle.Text = targetInquiry.InquiryId == 0 ? "Add New Inquiry" : $"Edit Inquiry {RecordReference.Inquiry(targetInquiry.InquiryId)}";

            var customerList = customers?.ToList() ?? new List<Customer>();
            if (customerList.Count == 0)
            {
                try { using var db = this.factory(); customerList = db.Customers.AsNoTracking().ToList(); } catch { }
            }

            foreach (var c in customerList) customerMap[c.CustomerId] = c;

            void UpdateConvertButtonState()
            {
                bool isGuest = selectedCustomer == null;
                convertButton.Text = isGuest ? "★ Register & Convert Lead" : "✔ Mark as Converted";
            }

            void SetSelectedCustomer(Customer c)
            {
                selectedCustomer = c;
                if (c != null)
                {
                    customerText.Text = $"{c.FullName} — {c.ContactNumber} ({RecordReference.Customer(c.CustomerId)})" + (c.IsActive ? "" : " (inactive)");
                    nameText.Text = c.FullName;
                    contactText.Text = c.ContactNumber;
                    nameText.ReadOnly = true;
                    contactText.ReadOnly = true;
                }
                else
                {
                    customerText.Text = "— Walk-in / Unregistered lead —";
                    nameText.ReadOnly = false;
                    contactText.ReadOnly = false;
                }
                UpdateConvertButtonState();
            }

            browseCustomerButton.Click += (s, e) =>
            {
                using var picker = new CustomerPickerDialogForm(customerList, selectedCustomer?.CustomerId, allowNone: true);
                if (picker.ShowDialog(this) == DialogResult.OK)
                {
                    SetSelectedCustomer(picker.SelectedCustomer);
                }
            };
            clearCustomerButton.Click += (s, e) =>
            {
                SetSelectedCustomer(null);
                nameText.Clear();
                contactText.Clear();
            };
            customerText.DoubleClick += (s, e) => browseCustomerButton.PerformClick();

            // Bind values
            if (targetInquiry.CustomerId is int cid && customerMap.TryGetValue(cid, out var currentCustomer))
            {
                SetSelectedCustomer(currentCustomer);
            }
            else
            {
                SetSelectedCustomer(null);
                nameText.Text = targetInquiry.CustomerName ?? "";
                contactText.Text = targetInquiry.ContactNumber ?? "";
            }

            SelectComboItem(platformCombo, targetInquiry.InquiryPlatform, 0);
            SelectComboItem(statusCombo, targetInquiry.Status, 0);
            stateCombo.SelectedIndex = targetInquiry.IsActive ? 0 : 1;
            messageText.Text = targetInquiry.MessageDetails ?? "";

            saveButton.Click += async (s, e) =>
            {
                if (saving) return;
                saving = true;
                saveButton.Enabled = false;
                try { await SaveInquiry(); }
                finally { saving = false; if (!saveButton.IsDisposed) saveButton.Enabled = true; }
            };
            convertButton.Click += (s, e) => RegisterAndConvertLead();
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

        private async System.Threading.Tasks.Task SaveInquiry()
        {
            errorLabel.Text = "";

            string name = selectedCustomer != null ? selectedCustomer.FullName : nameText.Text.Trim();
            string contact = selectedCustomer != null ? selectedCustomer.ContactNumber : contactText.Text.Trim();
            string platform = platformCombo.Text.Trim();
            string status = statusCombo.Text.Trim();
            string message = messageText.Text.Trim();
            bool isActive = stateCombo.Text == "Active";

            if (selectedCustomer == null && string.IsNullOrWhiteSpace(name))
            {
                errorLabel.Text = "Customer Name is required.";
                nameText.Focus();
                return;
            }
            if (selectedCustomer == null && string.IsNullOrWhiteSpace(contact))
            {
                errorLabel.Text = "Contact Number is required.";
                contactText.Focus();
                return;
            }
            if (selectedCustomer == null)
            {
                var valResult = CRM.infrastructure.services.ContactNumberHelper.Validate(contact, allowExistingPlaceholder: true);
                if (!valResult.IsValid)
                {
                    errorLabel.Text = valResult.ErrorMessage;
                    contactText.Focus();
                    return;
                }
                contact = CRM.infrastructure.services.ContactNumberHelper.Normalize(contact);
            }
            if (string.IsNullOrWhiteSpace(platform))
            {
                errorLabel.Text = "Please select an Inquiry Platform from the dropdown.";
                platformCombo.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(status))
            {
                errorLabel.Text = "Please select a Status from the dropdown.";
                statusCombo.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                errorLabel.Text = "Message / Inquiry details are required.";
                messageText.Focus();
                return;
            }

            try
            {
                using var db = factory();
                var strategy = db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var tx = db.Database.BeginTransaction();
                    bool isNew = targetInquiry.InquiryId == 0;
                    var entity = isNew ? targetInquiry : db.Inquiries.Find(targetInquiry.InquiryId);
                    if (entity == null) throw new InvalidOperationException("Inquiry record no longer exists.");

                    entity.CustomerId = selectedCustomer?.CustomerId;
                    entity.CustomerName = selectedCustomer != null ? "" : name;
                    entity.ContactNumber = selectedCustomer != null ? "" : contact;
                    entity.InquiryPlatform = platform;
                    entity.MessageDetails = message;
                    entity.Status = status;
                    entity.IsActive = isActive;

                    if (!isNew) db.Entry(entity).Property("RowVersion").OriginalValue = targetInquiry.RowVersion;
                    if (isNew) db.Inquiries.Add(entity);
                    else db.Inquiries.Update(entity);

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

        private void RegisterAndConvertLead()
        {
            errorLabel.Text = "";

            string name = selectedCustomer != null ? selectedCustomer.FullName : nameText.Text.Trim();
            string contact = selectedCustomer != null ? selectedCustomer.ContactNumber : contactText.Text.Trim();
            string platform = platformCombo.Text.Trim();
            string message = messageText.Text.Trim();
            bool isActive = stateCombo.Text == "Active";

            if (string.IsNullOrWhiteSpace(name))
            {
                errorLabel.Text = "Customer Name is required to register and convert.";
                nameText.Focus();
                return;
            }
            string digits = new string(contact.Where(char.IsDigit).ToArray());
            if (digits.Length < 7 || digits.Length > 15)
            {
                errorLabel.Text = "Contact Number must contain between 7 and 15 digits.";
                contactText.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(platform))
            {
                errorLabel.Text = "Please select an Inquiry Platform from the dropdown.";
                platformCombo.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(message))
            {
                errorLabel.Text = "Message / Inquiry details are required.";
                messageText.Focus();
                return;
            }

            try
            {
                using var db = factory();
                var strategy = db.Database.CreateExecutionStrategy();
                Customer resolvedCustomer = selectedCustomer;

                strategy.Execute(() =>
                {
                    using var tx = db.Database.BeginTransaction();

                    if (resolvedCustomer == null)
                    {
                        var existing = db.Customers.FirstOrDefault(c => c.ContactNumber == contact || c.FullName.ToLower() == name.ToLower());
                        if (existing != null)
                        {
                            resolvedCustomer = existing;
                        }
                        else
                        {
                            resolvedCustomer = new Customer
                            {
                                FullName = name,
                                ContactNumber = contact,
                                Email = name.ToLower().Replace(" ", ".") + "@example.com",
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            db.Customers.Add(resolvedCustomer);
                            db.SaveChanges();
                        }
                    }

                    bool isNew = targetInquiry.InquiryId == 0;
                    var entity = isNew ? targetInquiry : db.Inquiries.Find(targetInquiry.InquiryId);
                    if (entity == null) throw new InvalidOperationException("Inquiry record no longer exists.");

                    entity.CustomerId = resolvedCustomer.CustomerId;
                    entity.CustomerName = "";
                    entity.ContactNumber = "";
                    entity.InquiryPlatform = platform;
                    entity.MessageDetails = message;
                    entity.Status = "Converted";
                    entity.IsActive = isActive;

                    if (isNew) db.Inquiries.Add(entity);
                    else db.Inquiries.Update(entity);

                    db.SaveChanges();
                    tx.Commit();
                });

                MessageBox.Show(this,
                    $"Customer '{resolvedCustomer.FullName}' ({resolvedCustomer.ContactNumber}) has been confirmed and Inquiry status marked as 'Converted'.",
                    "Lead Converted Successfully",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                errorLabel.Text = ex.Message;
            }
        }

        private sealed record CustomerChoice(int Id, string Caption, Customer Customer)
        {
            public override string ToString() => Caption;
        }
    }
}
