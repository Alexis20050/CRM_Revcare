using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;

namespace CRM_Revcare_
{
    public partial class CustomerPickerDialogForm : Form
    {
        private readonly List<CustomerPickerRow> allRows = new();
        private readonly BindingList<CustomerPickerRow> visibleRows = new();
        private List<CustomerPickerRow> currentFiltered = new();
        private int currentPage = 1;
        private const int PageSize = 10;
        private int totalPages = 1;

        public Customer SelectedCustomer { get; private set; }
        public int? SelectedCustomerId => SelectedCustomer?.CustomerId;

        public CustomerPickerDialogForm() : this(Array.Empty<Customer>(), null, false) { }

        public CustomerPickerDialogForm(IEnumerable<Customer> customers, int? preselectedCustomerId = null, bool allowNone = false)
        {
            InitializeComponent();

            clearCustomerButton.Visible = allowNone;

            foreach (var c in (customers ?? Array.Empty<Customer>()).OrderBy(x => x.FullName))
            {
                allRows.Add(new CustomerPickerRow
                {
                    CustomerId = c.CustomerId,
                    FullName = c.FullName ?? "",
                    ContactNumber = c.ContactNumber ?? "",
                    Email = c.Email ?? "",
                    StatusText = c.IsActive ? "Active" : "Inactive",
                    OriginalCustomer = c
                });
            }

            grid.AutoGenerateColumns = false;
            grid.DataSource = visibleRows;
            StatusColumn.Visible = false;
            GridRowNumbering.Enable(grid, () => (currentPage - 1) * PageSize);

            grid.SelectionChanged += (s, e) =>
            {
                selectButton.Enabled = grid.CurrentRow != null && grid.SelectedRows.Count > 0;
            };

            searchBox.TextChanged += (s, e) => ApplyFilter();
            clearButton.Click += (s, e) => searchBox.Clear();

            firstPageButton.Click += (s, e) => { currentPage = 1; RenderPage(); };
            prevPageButton.Click += (s, e) => { if (currentPage > 1) { currentPage--; RenderPage(); } };
            nextPageButton.Click += (s, e) => { if (currentPage < totalPages) { currentPage++; RenderPage(); } };
            lastPageButton.Click += (s, e) => { currentPage = totalPages; RenderPage(); };

            grid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) ConfirmSelection();
            };

            grid.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    ConfirmSelection();
                }
            };

            selectButton.Click += (s, e) => ConfirmSelection();

            clearCustomerButton.Click += (s, e) =>
            {
                SelectedCustomer = null;
                DialogResult = DialogResult.OK;
                Close();
            };

            ApplyFilter();

            if (preselectedCustomerId.HasValue)
            {
                int matchIndex = currentFiltered.FindIndex(r => r.CustomerId == preselectedCustomerId.Value);
                if (matchIndex >= 0)
                {
                    currentPage = (matchIndex / PageSize) + 1;
                    RenderPage();
                    for (int i = 0; i < visibleRows.Count; i++)
                    {
                        if (visibleRows[i].CustomerId == preselectedCustomerId.Value)
                        {
                            grid.ClearSelection();
                            grid.Rows[i].Selected = true;
                            if (grid.Columns.Count > 0) grid.CurrentCell = grid.Rows[i].Cells[0];
                            grid.FirstDisplayedScrollingRowIndex = i;
                            selectButton.Enabled = true;
                            break;
                        }
                    }
                }
            }
        }

        private void ApplyFilter()
        {
            string query = searchBox.Text.Trim();

            currentFiltered = allRows.Where(r =>
            {
                if (string.IsNullOrWhiteSpace(query)) return true;
                return RecordReference.Matches(RecordReference.CustomerPrefix, r.CustomerId, query)
                    || r.CustomerCode.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || r.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || r.ContactNumber.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || r.Email.Contains(query, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            currentPage = 1;
            RenderPage();
        }

        private void RenderPage()
        {
            totalPages = Math.Max(1, (int)Math.Ceiling((double)currentFiltered.Count / PageSize));
            if (currentPage > totalPages) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;

            var pageItems = currentFiltered.Skip((currentPage - 1) * PageSize).Take(PageSize).ToList();

            visibleRows.Clear();
            foreach (var row in pageItems)
            {
                visibleRows.Add(row);
            }

            pageInfoLabel.Text = $"Page {currentPage} of {totalPages} ({currentFiltered.Count} customers)";
            countLabel.Text = currentFiltered.Count == 0
                ? "No matching records found. Clear or change the search query."
                : $"Showing {visibleRows.Count} on this page (Page {currentPage} of {totalPages}, Total: {currentFiltered.Count}).";
            firstPageButton.Enabled = currentPage > 1;
            prevPageButton.Enabled = currentPage > 1;
            nextPageButton.Enabled = currentPage < totalPages;
            lastPageButton.Enabled = currentPage < totalPages;
            grid.ClearSelection();
            grid.CurrentCell = null;
            selectButton.Enabled = false;
        }

        private void ConfirmSelection()
        {
            if (grid.SelectedRows.Count == 0 || !(grid.SelectedRows[0].DataBoundItem is CustomerPickerRow row))
            {
                MessageBox.Show("Please select a customer from the list.", "Select Customer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SelectedCustomer = row.OriginalCustomer;
            DialogResult = DialogResult.OK;
            Close();
        }

        private sealed class CustomerPickerRow
        {
            public int CustomerId { get; set; }
            public string CustomerCode => RecordReference.Customer(CustomerId);
            public string FullName { get; set; } = "";
            public string ContactNumber { get; set; } = "";
            public string Email { get; set; } = "";
            public string StatusText { get; set; } = "Active";
            public Customer OriginalCustomer { get; set; }
        }
    }
}
