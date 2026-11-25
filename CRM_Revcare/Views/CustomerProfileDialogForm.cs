#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using CRM.infrastructure.services;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public sealed class CustomerProfileDialogForm : Form
    {
        private readonly int customerId;
        private readonly Func<MasterCrmDbContext> factory;

        private Label lblName = null!;
        private Label lblRef = null!;
        private Label lblContact = null!;
        private Label lblEmail = null!;
        private Label lblStatus = null!;
        private Label lblSince = null!;
        private Label lblFinancialSummary = null!;

        private TabControl tabs = null!;
        private DataGridView gridVehicles = null!;
        private DataGridView gridJobs = null!;
        private DataGridView gridPayments = null!;
        private DataGridView gridAppointments = null!;
        private DataGridView gridFeedbackComplaints = null!;

        public CustomerProfileDialogForm(int customerId, Func<MasterCrmDbContext> factory)
        {
            this.customerId = customerId;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));

            InitializeUi();
            LoadProfileData();
        }

        private void InitializeUi()
        {
            Text = $"Customer 360 Profile & History — {RecordReference.Customer(customerId)}";
            Size = new Size(1020, 680);
            MinimumSize = new Size(880, 580);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9F);

            // Header card
            var headerCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 125,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(20, 12, 20, 12)
            };

            lblRef = new Label
            {
                Location = new Point(20, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            lblName = new Label
            {
                Location = new Point(20, 32),
                AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };

            lblStatus = new Label
            {
                Location = new Point(360, 36),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Padding = new Padding(6, 2, 6, 2),
                BackColor = Color.FromArgb(220, 252, 231),
                ForeColor = Color.FromArgb(22, 101, 52)
            };

            lblContact = new Label
            {
                Location = new Point(20, 68),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            lblEmail = new Label
            {
                Location = new Point(280, 68),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            lblSince = new Label
            {
                Location = new Point(540, 68),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            lblFinancialSummary = new Label
            {
                Location = new Point(20, 96),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };

            headerCard.Controls.AddRange(new Control[] { lblRef, lblName, lblStatus, lblContact, lblEmail, lblSince, lblFinancialSummary });

            // Bottom bar
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(16, 6, 16, 6)
            };
            var btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Right,
                Width = 90
            };
            bottomBar.Controls.Add(btnClose);
            AcceptButton = btnClose;

            // Tabs
            tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(12, 6)
            };

            // Tab 1: Job Orders
            var tabJobs = new TabPage("🔧 Job Orders");
            gridJobs = CreateReadOnlyGrid();
            gridJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "JobOrderId", HeaderText = "Job Order #", Width = 95 });
            gridJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstimateNumber", HeaderText = "Estimate #", Width = 100 });
            gridJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vehicle", HeaderText = "Vehicle", Width = 150 });
            gridJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 95 });
            gridJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalEstimatedCost", HeaderText = "Total Cost", Width = 105 });
            gridJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPaid", HeaderText = "Paid", Width = 105 });
            gridJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "OutstandingBalance", HeaderText = "Balance", Width = 105 });
            gridJobs.Columns.Add(new DataGridViewTextBoxColumn { Name = "StartedAt", HeaderText = "Started", Width = 125 });

            gridJobs.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                string col = gridJobs.Columns[e.ColumnIndex].Name;
                if (col == "JobOrderId" && e.Value is int jid)
                {
                    e.Value = RecordReference.JobOrder(jid);
                    e.FormattingApplied = true;
                }
                else if (col is "TotalEstimatedCost" or "TotalPaid" or "OutstandingBalance" && e.Value is decimal val)
                {
                    e.Value = $"₱{val:N2}";
                    e.FormattingApplied = true;
                }
                else if (col == "StartedAt" && e.Value is DateTime dt)
                {
                    e.Value = dt.ToString("yyyy-MM-dd HH:mm");
                    e.FormattingApplied = true;
                }
            };
            tabJobs.Controls.Add(gridJobs);

            // Tab 2: Payments & Refunds
            var tabPayments = new TabPage("💳 Payments & Refunds");
            gridPayments = CreateReadOnlyGrid();
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Transaction", Width = 95 });
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", HeaderText = "Reference #", Width = 110 });
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "JobOrderId", HeaderText = "Job Order #", Width = 95 });
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", HeaderText = "Amount", Width = 110 });
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Method", HeaderText = "Method", Width = 95 });
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", Width = 130 });
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 105 });
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reason", HeaderText = "Notes / Reason", Width = 230 });
            tabPayments.Controls.Add(gridPayments);

            // Tab 3: Registered Vehicles
            var tabVehicles = new TabPage("🚗 Vehicles");
            gridVehicles = CreateReadOnlyGrid();
            gridVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "VehicleId", HeaderText = "Vehicle Ref", Width = 100 });
            gridVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlateNumber", HeaderText = "Plate Number", Width = 120 });
            gridVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Make", HeaderText = "Make", Width = 120 });
            gridVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Model", HeaderText = "Model", Width = 120 });
            gridVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Year", HeaderText = "Year", Width = 80 });
            gridVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Color", HeaderText = "Color", Width = 90 });
            gridVehicles.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vin", HeaderText = "VIN / Chassis", Width = 170 });
            gridVehicles.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                if (gridVehicles.Columns[e.ColumnIndex].Name == "VehicleId" && e.Value is int vid)
                {
                    e.Value = RecordReference.Vehicle(vid);
                    e.FormattingApplied = true;
                }
            };
            tabVehicles.Controls.Add(gridVehicles);

            // Tab 4: Appointments & Inquiries
            var tabAppts = new TabPage("📅 Appointments & Inquiries");
            gridAppointments = CreateReadOnlyGrid();
            gridAppointments.Columns.Add(new DataGridViewTextBoxColumn { Name = "RecordType", HeaderText = "Type", Width = 105 });
            gridAppointments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", HeaderText = "Reference #", Width = 110 });
            gridAppointments.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScheduledOrDate", HeaderText = "Date / Schedule", Width = 140 });
            gridAppointments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 100 });
            gridAppointments.Columns.Add(new DataGridViewTextBoxColumn { Name = "Details", HeaderText = "Services / Intake Details", Width = 310 });
            gridAppointments.Columns.Add(new DataGridViewTextBoxColumn { Name = "LinkedJob", HeaderText = "Linked Job", Width = 100 });
            tabAppts.Controls.Add(gridAppointments);

            // Tab 5: Feedback & Complaints
            var tabFeedback = new TabPage("⭐ Feedback & Complaints");
            gridFeedbackComplaints = CreateReadOnlyGrid();
            gridFeedbackComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", Width = 95 });
            gridFeedbackComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reference", HeaderText = "Reference #", Width = 110 });
            gridFeedbackComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "RatingOrType", HeaderText = "Rating / Type", Width = 130 });
            gridFeedbackComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 90 });
            gridFeedbackComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Date", Width = 120 });
            gridFeedbackComplaints.Columns.Add(new DataGridViewTextBoxColumn { Name = "Remarks", HeaderText = "Customer Remarks / Resolution", Width = 320 });
            tabFeedback.Controls.Add(gridFeedbackComplaints);

            tabs.TabPages.AddRange(new[] { tabJobs, tabPayments, tabVehicles, tabAppts, tabFeedback });

            Controls.Add(tabs);
            Controls.Add(bottomBar);
            Controls.Add(headerCard);
        }

        private static DataGridView CreateReadOnlyGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoGenerateColumns = false
            };
            GridRowNumbering.Enable(g);
            return g;
        }

        private void LoadProfileData()
        {
            try
            {
                using var db = factory();
                var cust = db.Customers.Find(customerId);
                if (cust == null)
                {
                    MessageBox.Show(this, "Customer not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                lblRef.Text = RecordReference.Customer(cust.CustomerId);
                lblName.Text = cust.FullName;
                string classification = ContactNumberHelper.Classify(cust.ContactNumber);
                lblContact.Text = $"📞 {cust.ContactNumber} [{classification}]";
                lblEmail.Text = string.IsNullOrWhiteSpace(cust.Email) ? "✉ No Email" : $"✉ {cust.Email}";
                lblSince.Text = $"Registered: {cust.CreatedAt:yyyy-MM-dd}";

                if (cust.IsActive)
                {
                    lblStatus.Text = "Active Customer";
                    lblStatus.BackColor = Color.FromArgb(220, 252, 231);
                    lblStatus.ForeColor = Color.FromArgb(22, 101, 52);
                }
                else
                {
                    lblStatus.Text = "Inactive Customer";
                    lblStatus.BackColor = Color.FromArgb(254, 226, 226);
                    lblStatus.ForeColor = Color.FromArgb(153, 27, 27);
                }

                // 1. Registered Vehicles
                var vehicles = db.Vehicles.Where(v => v.CustomerId == customerId).ToList();
                gridVehicles.DataSource = vehicles;

                // 2. Job Orders
                var jobs = db.JobOrders.Include(j => j.Payments).Where(j => j.CustomerId == customerId).OrderByDescending(j => j.JobOrderId).ToList();
                gridJobs.DataSource = jobs.Select(j => new
                {
                    j.JobOrderId,
                    j.EstimateNumber,
                    Vehicle = j.VehicleDisplay,
                    j.Status,
                    TotalEstimatedCost = j.TotalEstimatedCost ?? 0m,
                    TotalPaid = j.TotalPaid,
                    OutstandingBalance = j.Status == "Cancelled" ? 0m : (j.OutstandingBalance ?? 0m),
                    j.StartedAt
                }).ToList();

                // 3. Payments & Refunds (including Insurance Claims and Settlements)
                var jobIds = jobs.Select(j => j.JobOrderId).ToList();
                var payments = db.Payments.Where(p => jobIds.Contains(p.JobOrderId)).ToList();
                var refunds = db.Refunds.Where(r => jobIds.Contains(r.JobOrderId)).ToList();

                // Insurance claims from audit
                var insuranceAudits = db.AuditEntries
                    .Where(a => a.EntityType == "InsuranceClaim")
                    .ToList();
                var insuranceItems = new List<dynamic>();
                foreach (var a in insuranceAudits)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(a.Changes);
                        if (doc.RootElement.TryGetProperty("JobOrderId", out var jProp) && jobIds.Contains(jProp.GetInt32()))
                        {
                            string claimRef = doc.RootElement.GetProperty("ClaimReference").GetString() ?? "";
                            string provider = doc.RootElement.GetProperty("ProviderName").GetString() ?? "Insurance";
                            string status = doc.RootElement.GetProperty("Status").GetString() ?? "Pending";
                            decimal appCoverage = doc.RootElement.TryGetProperty("ApprovedCoverage", out var ac) ? ac.GetDecimal() : 0m;
                            decimal insResp = doc.RootElement.TryGetProperty("InsurerResponsibility", out var ir) ? ir.GetDecimal() : 0m;
                            decimal custResp = doc.RootElement.TryGetProperty("CustomerResponsibility", out var cr) ? cr.GetDecimal() : 0m;

                            insuranceItems.Add(new
                            {
                                Type = "Insurance Claim",
                                Reference = claimRef,
                                JobOrderId = RecordReference.JobOrder(jProp.GetInt32()),
                                Amount = $"₱{appCoverage:N2}",
                                Method = provider,
                                Date = a.OccurredAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                                Status = $"Claim: {status}",
                                Reason = $"Insurer: ₱{insResp:N2} | Deductible: ₱{custResp:N2} (Approval not payment)"
                            });
                        }
                    }
                    catch { }
                }

                var financialTxList = payments.Select(p => new
                {
                    Type = "Payment",
                    Reference = RecordReference.Receipt(p.PaymentId),
                    JobOrderId = RecordReference.JobOrder(p.JobOrderId),
                    Amount = $"₱{p.AmountPaid:N2}",
                    Method = p.PaymentMethod + (p.PayerType == "Insurance" ? " (Insurance)" : ""),
                    Date = p.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Status = p.IsVoided ? "Voided" : "Valid",
                    Reason = p.IsVoided ? $"Void Reason: {p.VoidReason}" : p.PaymentType
                }).Concat(refunds.Select(r => new
                {
                    Type = "Refund",
                    Reference = RecordReference.Refund(r.RefundId),
                    JobOrderId = RecordReference.JobOrder(r.JobOrderId),
                    Amount = $"-₱{r.Amount:N2}",
                    Method = r.RefundMethod,
                    Date = r.RefundDateUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    Status = "Refunded",
                    Reason = $"Recipient: {r.RecipientName} | Reason: {r.Reason} (Auth: {r.AuthorizedBy})"
                })).Concat(insuranceItems.Cast<dynamic>()).OrderByDescending(t => t.Date).ToList();

                gridPayments.DataSource = financialTxList;

                // Financial Summary
                decimal totalBilled = jobs.Sum(j => j.TotalEstimatedCost ?? 0m);
                decimal totalValidPaid = payments.Where(p => !p.IsVoided).Sum(p => p.AmountPaid);
                decimal totalRefunded = refunds.Sum(r => r.Amount);
                decimal netPaid = Math.Max(0m, totalValidPaid - totalRefunded);
                decimal outstanding = jobs.Where(j => j.Status != "Cancelled").Sum(j => j.OutstandingBalance ?? 0m);

                lblFinancialSummary.Text = $"Lifetime Activity:  {jobs.Count} Jobs  |  Total Billed: ₱{totalBilled:N2}  |  Paid: ₱{totalValidPaid:N2}  |  Refunded: ₱{totalRefunded:N2}  |  Net Collections: ₱{netPaid:N2}  |  Open Balance: ₱{outstanding:N2}";

                // 4. Appointments, Inquiries & Follow-ups
                var appts = db.Appointments.Where(a => a.CustomerId == customerId).ToList();
                var inqs = db.Inquiries.Where(i => i.CustomerId == customerId).ToList();

                var followUpAudits = db.AuditEntries
                    .Where(a => a.EntityType == "CustomerFollowUp")
                    .ToList();
                var followUpItems = new List<dynamic>();
                foreach (var a in followUpAudits)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(a.Changes);
                        if (doc.RootElement.TryGetProperty("CustomerId", out var cProp) && cProp.GetInt32() == customerId)
                        {
                            string reason = doc.RootElement.GetProperty("Reason").GetString() ?? "Follow-Up";
                            string status = doc.RootElement.GetProperty("Status").GetString() ?? "Pending";
                            string dateStr = doc.RootElement.GetProperty("ScheduledDate").GetString() ?? "";
                            DateTime.TryParse(dateStr, out var sDate);
                            int? jid = doc.RootElement.TryGetProperty("JobOrderId", out var jp) && jp.GetInt32() > 0 ? jp.GetInt32() : null;
                            string outcome = doc.RootElement.TryGetProperty("OutcomeNotes", out var op) ? (op.GetString() ?? "") : "";

                            followUpItems.Add(new
                            {
                                RecordType = "Follow-Up",
                                Reference = "FLW-" + doc.RootElement.GetProperty("FollowUpId").GetString()?[..8],
                                ScheduledOrDate = sDate != default ? sDate.ToString("yyyy-MM-dd") : a.OccurredAtUtc.ToString("yyyy-MM-dd"),
                                Status = status,
                                Details = $"{reason}" + (string.IsNullOrWhiteSpace(outcome) ? "" : $" | Outcome: {outcome}"),
                                LinkedJob = jid.HasValue ? RecordReference.JobOrder(jid.Value) : "—"
                            });
                        }
                    }
                    catch { }
                }

                var interactionList = appts.Select(a => new
                {
                    RecordType = "Appointment",
                    Reference = RecordReference.Appointment(a.AppointmentId),
                    ScheduledOrDate = a.ScheduledStart.ToString("yyyy-MM-dd hh:mm tt"),
                    a.Status,
                    Details = a.RequestedServices + (string.IsNullOrWhiteSpace(a.CancellationReason) ? "" : $" (Cancelled: {a.CancellationReason})"),
                    LinkedJob = a.LinkedJobOrderId.HasValue ? RecordReference.JobOrder(a.LinkedJobOrderId.Value) : "—"
                }).Concat(inqs.Select(i => new
                {
                    RecordType = "Inquiry",
                    Reference = RecordReference.Inquiry(i.InquiryId),
                    ScheduledOrDate = i.CreatedAt.ToString("yyyy-MM-dd"),
                    i.Status,
                    Details = $"{i.InquiryPlatform}: {i.MessageDetails}",
                    LinkedJob = "—"
                })).Concat(followUpItems.Cast<dynamic>()).OrderByDescending(x => x.ScheduledOrDate).ToList();

                gridAppointments.DataSource = interactionList;

                // 5. Feedback & Complaints
                var feedbackList = db.Feedbacks.Where(f => jobIds.Contains(f.JobOrderId)).ToList();
                var complaintList = db.Complaints.Where(c => (c.CustomerId == customerId) || (c.JobOrderId.HasValue && jobIds.Contains(c.JobOrderId.Value))).ToList();

                var sentimentList = feedbackList.Select(f => new
                {
                    Category = "Feedback",
                    Reference = RecordReference.Feedback(f.FeedbackId),
                    RatingOrType = $"{f.RatingScore} / 5 Stars",
                    Status = "Submitted",
                    Date = f.CreatedAt.ToString("yyyy-MM-dd"),
                    Remarks = $"Positive: {f.PositiveRemarks} | Improvements: {f.AreasForImprovement}"
                }).Concat(complaintList.Select(c => new
                {
                    Category = "Complaint",
                    Reference = RecordReference.Complaint(c.ComplaintId),
                    RatingOrType = c.ComplaintType + (c.IsUrgent ? " [URGENT]" : ""),
                    Status = c.ResolutionStatus,
                    Date = c.CreatedAt.ToString("yyyy-MM-dd"),
                    Remarks = $"Description: {c.Description} | Resolution: {c.ActionTaken}"
                })).OrderByDescending(s => s.Date).ToList();

                gridFeedbackComplaints.DataSource = sentimentList;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error loading customer profile: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}

