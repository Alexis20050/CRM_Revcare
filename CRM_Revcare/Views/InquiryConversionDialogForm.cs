using System;
using System.Linq;
using System.Windows.Forms;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public partial class InquiryConversionDialogForm : Form
    {
        public InquiryConversionDialogForm() { InitializeComponent(); }
        internal InquiryConversionDialogForm(int inquiryId, Func<MasterCrmDbContext> factory) : this()
        {
            var lookup = new CustomerLookup(customer, factory, result);
            lookup.AttachButtons(browseCustomerButton, clearCustomerButton);
            Shown += (s, e) =>
            {
                try
                {
                    using var db = factory();
                    var inquiry = db.Inquiries.AsNoTracking().Single(i => i.InquiryId == inquiryId);
                    lookup.Replace(db.Customers.AsNoTracking().ToList());
                    lookup.Select(inquiry.CustomerId);
                    vehicle.Text = inquiry.VehicleDetails;
                }
                catch { result.Text = "Could not load inquiry details. Check the connection."; }
            };
            convert.Click += (s, e) =>
            {
                convert.Enabled = false;
                try
                {
                    InquiryConversion.Convert(factory, inquiryId, lookup.SelectedId, vehicle.Text);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (InvalidOperationException ex) { result.Text = ex.Message; }
                catch { result.Text = "Could not convert the inquiry. Check the connection and try again."; }
                finally { if (!IsDisposed) convert.Enabled = true; }
            };
        }
    }
}
