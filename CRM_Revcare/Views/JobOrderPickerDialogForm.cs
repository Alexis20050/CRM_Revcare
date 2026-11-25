using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;

namespace CRM_Revcare_
{
    public partial class JobOrderPickerDialogForm : Form
    {
        private readonly List<JobOrderPickerRow> allRows = new();
        private readonly BindingList<JobOrderPickerRow> visibleRows = new();
        private List<JobOrderPickerRow> currentFiltered = new();
        private const int PageSize = 10;
        private int currentPage = 1;
        private int totalPages = 1;

        public JobOrder SelectedJob { get; private set; }
        public int? SelectedJobOrderId => SelectedJob?.JobOrderId;

        public JobOrderPickerDialogForm() : this(Array.Empty<JobOrder>(), Array.Empty<Customer>(), null, false) { }

        public JobOrderPickerDialogForm(IEnumerable<JobOrder> jobs, IEnumerable<Customer> customers, int? preselectedJobId = null, bool allowNone = false)
        {
            InitializeComponent();

            clearJobButton.Visible = allowNone;
            statusFilter.SelectedIndex = 0;

            var custDict = customers?.ToDictionary(c => c.CustomerId) ?? new Dictionary<int, Customer>();

            foreach (var j in (jobs ?? Array.Empty<JobOrder>()).OrderByDescending(x => x.JobOrderId))
            {
                Customer cust = null;
                if (j.CustomerId is int cid) custDict.TryGetValue(cid, out cust);

                allRows.Add(new JobOrderPickerRow
                {
                    JobOrderId = j.JobOrderId,
                    CustomerName = cust?.FullName ?? "Unlinked Customer",
                    ContactNumber = cust?.ContactNumber ?? "-",
                    VehicleReference = j.EstimateNumber + " / " + j.VehicleDisplay,
                    Status = JobOrderWorkflow.Normalize(j.Status) ?? "In Queue",
                    StartedAt = j.StartedAt,
                    OriginalJob = j
                });
            }

            grid.AutoGenerateColumns = false;
            grid.DataSource = visibleRows;
            GridRowNumbering.Enable(grid, () => (currentPage - 1) * PageSize);
            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string prop = grid.Columns[e.ColumnIndex].DataPropertyName;
                if (prop == "JobOrderId" && e.Value is int jid)
                {
                    e.Value = RecordReference.JobOrder(jid);
                    e.FormattingApplied = true;
                }
            };
            grid.SelectionChanged += (s, e) =>
            {
                selectButton.Enabled = grid.CurrentRow != null && grid.SelectedRows.Count > 0;
            };

            // Paging events
            firstPageButton.Click += (s, e) => { currentPage = 1; RenderPage(); };
            prevPageButton.Click += (s, e) => { if (currentPage > 1) { currentPage--; RenderPage(); } };
            nextPageButton.Click += (s, e) => { if (currentPage < totalPages) { currentPage++; RenderPage(); } };
            lastPageButton.Click += (s, e) => { currentPage = totalPages; RenderPage(); };

            // Events
            searchBox.TextChanged += (s, e) => ApplyFilter();
            statusFilter.SelectedIndexChanged += (s, e) => ApplyFilter();
            clearButton.Click += (s, e) =>
            {
                searchBox.Clear();
                statusFilter.SelectedIndex = 0;
            };

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

            clearJobButton.Click += (s, e) =>
            {
                SelectedJob = null;
                DialogResult = DialogResult.OK;
                Close();
            };

            ApplyFilter();

            // Pre-select row if requested
            if (preselectedJobId.HasValue && preselectedJobId.Value > 0)
            {
                int matchIndex = currentFiltered.FindIndex(r => r.JobOrderId == preselectedJobId.Value);
                if (matchIndex >= 0)
                {
                    currentPage = (matchIndex / PageSize) + 1;
                    RenderPage();
                    for (int i = 0; i < visibleRows.Count; i++)
                    {
                        if (visibleRows[i].JobOrderId == preselectedJobId.Value)
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
            string selectedStatus = JobWorkflowLabels.FilterStatus(statusFilter.SelectedIndex);

            currentFiltered = allRows.Where(r =>
            {
                if (!string.IsNullOrEmpty(selectedStatus) && !string.Equals(crm.domain.entities.JobOrderWorkflow.Normalize(r.Status), selectedStatus, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (string.IsNullOrEmpty(query)) return true;

                return JobSearch.Matches(r.OriginalJob, new crm.domain.entities.Customer
                    { FullName = r.CustomerName, ContactNumber = r.ContactNumber }, query);
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
            foreach (var item in pageItems)
            {
                visibleRows.Add(item);
            }

            pageInfoLabel.Text = $"Page {currentPage} of {totalPages} ({currentFiltered.Count} jobs)";
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
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].DataBoundItem is not JobOrderPickerRow selectedRow)
            {
                MessageBox.Show(this, "Please select a Job Order from the table.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SelectedJob = selectedRow.OriginalJob;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static string CleanReference(string estimateRef, string customerName)
        {
            if (string.IsNullOrWhiteSpace(estimateRef)) return "-";
            string text = estimateRef.Trim();
            if (!string.IsNullOrWhiteSpace(customerName))
            {
                // Remove repeated " / CustomerName / " or " - CustomerName" if present in EstimateId
                text = text.Replace(" / " + customerName + " / ", " / ")
                           .Replace(" / " + customerName, "")
                           .Replace(customerName + " / ", "");
            }
            return text.Trim(' ', '/', '-');
        }

        public sealed class JobOrderPickerRow
        {
            public int JobOrderId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string ContactNumber { get; set; } = string.Empty;
            public string VehicleReference { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public DateTime StartedAt { get; set; }
            public string StartedAtFormatted => StartedAt.ToString("yyyy-MM-dd HH:mm");
            public JobOrder OriginalJob { get; set; }
        }
    }
}
