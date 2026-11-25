#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public sealed class AppointmentManagerDialogForm : Form
    {
        private readonly Func<MasterCrmDbContext> factory;
        private readonly int? preselectedInquiryId;
        private readonly int? preselectedCustomerId;

        private DataGridView grid;
        private TextBox searchBox;
        private Button btnNew;
        private Button btnReschedule;
        private Button btnCheckIn;
        private Button btnCancel;
        private Button btnNoShow;
        private Button btnHistory;
        private Button btnRefresh;
        private Label lblCount;
        private Label lblDetails;

        private List<Appointment> loadedAppointments = new();

        public AppointmentManagerDialogForm(Func<MasterCrmDbContext> factory, int? inquiryId = null, int? customerId = null)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.preselectedInquiryId = inquiryId;
            this.preselectedCustomerId = customerId;

            InitializeUi();
            LoadAppointments();
        }

        private void InitializeUi()
        {
            Text = "Workshop Appointments & Scheduling";
            Size = new Size(1000, 620);
            MinimumSize = new Size(900, 540);
            StartPosition = FormStartPosition.CenterParent;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.FromArgb(246, 248, 250),
                Padding = new Padding(16, 10, 16, 10)
            };

            var title = new Label
            {
                Text = "Workshop Appointments & Scheduling",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(16, 10),
                AutoSize = true
            };

            var subtitle = new Label
            {
                Text = "Manage customer appointments directly within Intake. Check-in seamlessly creates Job Orders.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.DimGray,
                Location = new Point(16, 34),
                AutoSize = true
            };

            searchBox = new TextBox
            {
                Location = new Point(16, 56),
                Size = new Size(240, 24),
                PlaceholderText = "Search customer, vehicle, status..."
            };
            searchBox.TextChanged += (s, e) => ApplyFilter();

            btnNew = new Button
            {
                Text = "+ Schedule Appointment",
                Location = new Point(270, 53),
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(36, 137, 203),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Click += (s, e) => OpenScheduleDialog();

            btnReschedule = new Button
            {
                Text = "Reschedule",
                Location = new Point(440, 53),
                Size = new Size(100, 28),
                Enabled = false
            };
            btnReschedule.Click += (s, e) => RescheduleSelected();

            btnCheckIn = new Button
            {
                Text = "✔ Check In / Create Job",
                Location = new Point(550, 53),
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 164, 79),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnCheckIn.FlatAppearance.BorderSize = 0;
            btnCheckIn.Click += (s, e) => CheckInSelected();

            btnCancel = new Button
            {
                Text = "Cancel Appt",
                Location = new Point(720, 53),
                Size = new Size(100, 28),
                Enabled = false
            };
            btnCancel.Click += (s, e) => CancelSelected();

            btnNoShow = new Button
            {
                Text = "Mark No Show",
                Location = new Point(805, 53),
                Size = new Size(95, 28),
                Enabled = false
            };
            btnNoShow.Click += (s, e) => MarkNoShowSelected();

            btnHistory = new Button
            {
                Text = "History",
                Location = new Point(905, 53),
                Size = new Size(75, 28),
                Enabled = false
            };
            btnHistory.Click += (s, e) => ViewHistorySelected();

            topPanel.Controls.Add(title);
            topPanel.Controls.Add(subtitle);
            topPanel.Controls.Add(searchBox);
            topPanel.Controls.Add(btnNew);
            topPanel.Controls.Add(btnReschedule);
            topPanel.Controls.Add(btnCheckIn);
            topPanel.Controls.Add(btnCancel);
            topPanel.Controls.Add(btnNoShow);
            topPanel.Controls.Add(btnHistory);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(246, 248, 250),
                Padding = new Padding(16, 8, 16, 8)
            };
            lblCount = new Label { Dock = DockStyle.Left, AutoSize = true, Font = new Font("Segoe UI", 9F) };
            lblDetails = new Label { Dock = DockStyle.Right, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Italic), ForeColor = Color.DimGray };
            bottomPanel.Controls.Add(lblCount);
            bottomPanel.Controls.Add(lblDetails);

            grid = new DataGridView
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
            CrmGridStandardizer.ApplyStandardSettings(grid);

            GridRowNumbering.Enable(grid);

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AppointmentId", HeaderText = "Appt #", DataPropertyName = "AppointmentId", Width = 125, MinimumWidth = 80, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScheduledStart", HeaderText = "Scheduled Start", DataPropertyName = "ScheduledStart", Width = 180, MinimumWidth = 130, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "Customer", Width = 220, MinimumWidth = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Vehicle", HeaderText = "Vehicle", Width = 180, MinimumWidth = 110, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", DataPropertyName = "Status", Width = 130, MinimumWidth = 80, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RequestedServices", HeaderText = "Requested Services", DataPropertyName = "RequestedServices", Width = 260, MinimumWidth = 130, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LinkedJobOrderId", HeaderText = "Job Order #", Width = 130, MinimumWidth = 80, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            CrmGridStandardizer.ApplyColumnLayout(grid, "Appointments");

            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
                var appt = grid.Rows[e.RowIndex].DataBoundItem as Appointment;
                if (appt == null) return;

                if (grid.Columns[e.ColumnIndex].Name == "AppointmentId")
                {
                    e.Value = RecordReference.Appointment(appt.AppointmentId);
                    e.FormattingApplied = true;
                }
                else if (grid.Columns[e.ColumnIndex].Name == "Customer")
                {
                    e.Value = appt.Customer?.FullName ?? (appt.Inquiry?.CustomerName ?? "Walk-in");
                    e.FormattingApplied = true;
                }
                else if (grid.Columns[e.ColumnIndex].Name == "Vehicle")
                {
                    e.Value = appt.Vehicle?.DisplayName ?? (appt.Inquiry?.VehicleDetails ?? "To be inspected");
                    e.FormattingApplied = true;
                }
                else if (grid.Columns[e.ColumnIndex].Name == "LinkedJobOrderId")
                {
                    e.Value = appt.LinkedJobOrderId.HasValue ? RecordReference.JobOrder(appt.LinkedJobOrderId.Value) : "—";
                    e.FormattingApplied = true;
                }
                else if (grid.Columns[e.ColumnIndex].Name == "ScheduledStart" && e.Value is DateTime dt)
                {
                    e.Value = dt.ToString("yyyy-MM-dd hh:mm tt");
                    e.FormattingApplied = true;
                }
            };

            grid.SelectionChanged += (s, e) => UpdateActionButtons();
            grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ViewHistorySelected(); };

            Controls.Add(grid);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
        }

        private Appointment? SelectedAppointment =>
            grid.SelectedRows.Count > 0 ? grid.SelectedRows[0].DataBoundItem as Appointment : null;

        private void UpdateActionButtons()
        {
            var appt = SelectedAppointment;
            bool hasSelection = appt != null;
            bool isScheduled = hasSelection && appt!.Status == "Scheduled";

            btnReschedule.Enabled = isScheduled;
            btnCheckIn.Enabled = isScheduled;
            btnCancel.Enabled = isScheduled;
            btnNoShow.Enabled = isScheduled;
            btnHistory.Enabled = hasSelection;

            lblDetails.Text = appt != null
                ? $"Selected: {RecordReference.Appointment(appt.AppointmentId)} ({appt.Status})" + (appt.LinkedJobOrderId.HasValue ? $" -> Linked {RecordReference.JobOrder(appt.LinkedJobOrderId.Value)}" : "")
                : "Select an appointment row to view or execute actions.";
        }

        private void LoadAppointments()
        {
            try
            {
                using var db = factory();
                loadedAppointments = db.Appointments
                    .Include(a => a.Customer)
                    .Include(a => a.Vehicle)
                    .Include(a => a.Inquiry)
                    .OrderByDescending(a => a.ScheduledStart)
                    .ToList();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load appointments: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilter()
        {
            string q = searchBox.Text.Trim();
            var filtered = loadedAppointments.Where(a =>
            {
                if (string.IsNullOrWhiteSpace(q)) return true;
                return RecordReference.Matches(RecordReference.AppointmentPrefix, a.AppointmentId, q)
                    || (a.LinkedJobOrderId.HasValue && RecordReference.Matches(RecordReference.JobOrderPrefix, a.LinkedJobOrderId.Value, q))
                    || (a.Customer?.FullName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (a.Vehicle?.DisplayName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (a.Status ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (a.RequestedServices ?? "").Contains(q, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            grid.DataSource = filtered;
            grid.ClearSelection();
            grid.CurrentCell = null;
            UpdateActionButtons();

            lblCount.Text = $"{filtered.Count} appointment(s) displayed.";
        }

        private void OpenScheduleDialog()
        {
            using var dlg = new ScheduleAppointmentForm(factory, preselectedInquiryId, preselectedCustomerId);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                LoadAppointments();
            }
        }

        private void ViewHistorySelected()
        {
            var appt = SelectedAppointment;
            if (appt == null) return;

            using var dlg = new AppointmentHistoryDialogForm(factory, appt.AppointmentId);
            dlg.ShowDialog(this);
        }

        private void RescheduleSelected()
        {
            var appt = SelectedAppointment;
            if (appt == null || appt.Status != "Scheduled") return;

            using var rescheduleForm = new Form
            {
                Text = $"Reschedule {RecordReference.Appointment(appt.AppointmentId)}",
                Size = new Size(420, 290),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lbl = new Label { Text = "Select New Scheduled Date & Time:", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var picker = new DateTimePicker
            {
                Location = new Point(20, 42),
                Size = new Size(360, 26),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "yyyy-MM-dd hh:mm tt",
                Value = appt.ScheduledStart > DateTime.Now ? appt.ScheduledStart : DateTime.Now.AddDays(1)
            };

            var lblReason = new Label { Text = "Reason for Rescheduling (Required, min 4 chars):", Location = new Point(20, 80), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var txtReason = new TextBox
            {
                Location = new Point(20, 106),
                Size = new Size(360, 60),
                Multiline = true,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "e.g., Customer requested later date due to parts availability..."
            };

            var btnSave = new Button { Text = "Save", Location = new Point(210, 185), Size = new Size(80, 30), DialogResult = DialogResult.OK };
            var btnClose = new Button { Text = "Cancel", Location = new Point(300, 185), Size = new Size(80, 30), DialogResult = DialogResult.Cancel };

            rescheduleForm.Controls.AddRange(new Control[] { lbl, picker, lblReason, txtReason, btnSave, btnClose });
            rescheduleForm.AcceptButton = btnSave;
            rescheduleForm.CancelButton = btnClose;

            if (rescheduleForm.ShowDialog(this) == DialogResult.OK)
            {
                string reason = txtReason.Text.Trim();
                if (reason.Length < 4)
                {
                    MessageBox.Show(this, "A rescheduling reason of at least 4 characters is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var db = factory();
                    CRM.infrastructure.services.AppointmentService.Reschedule(db, appt.AppointmentId, picker.Value, reason, db.AuditActor);
                    MessageBox.Show(this, $"Appointment {RecordReference.Appointment(appt.AppointmentId)} successfully rescheduled.", "Rescheduled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadAppointments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Reschedule Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CheckInSelected()
        {
            var appt = SelectedAppointment;
            if (appt == null || appt.Status != "Scheduled") return;

            if (MessageBox.Show(this,
                $"Check in {RecordReference.Appointment(appt.AppointmentId)}?\nThis will create a new Job Order for {appt.Customer?.FullName ?? "this customer"}.",
                "Confirm Check In", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using var db = factory();
                var strategy = db.Database.CreateExecutionStrategy();
                string createdJobRef = string.Empty;

                strategy.Execute(() =>
                {
                    using var tx = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
                    var target = db.Appointments
                        .FromSqlInterpolated($"SELECT * FROM Appointments WITH (UPDLOCK, HOLDLOCK) WHERE AppointmentId = {appt.AppointmentId}")
                        .Include(a => a.Vehicle)
                        .FirstOrDefault();

                    if (target == null || target.Status != "Scheduled")
                    {
                        throw new InvalidOperationException("Appointment is no longer in Scheduled status.");
                    }

                    if (target.LinkedJobOrderId.HasValue)
                    {
                        throw new InvalidOperationException($"Job Order #{target.LinkedJobOrderId.Value} was already created for this appointment.");
                    }

                    // Create Job Order
                    var job = new JobOrder
                    {
                        CustomerId = target.CustomerId,
                        VehicleId = target.VehicleId,
                        VehicleDetails = target.Vehicle?.DisplayName ?? appt.Vehicle?.DisplayName ?? "",
                        InquiryNotes = $"Created from Appointment #{target.AppointmentId}. {target.Notes}",
                        Status = "In Queue",
                        StartedAt = DateTime.Now,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Assign requested service if specified
                    if (!string.IsNullOrWhiteSpace(target.RequestedServices))
                    {
                        var services = target.RequestedServices.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        if (services.Length > 0)
                        {
                            CRM.infrastructure.services.JobServices.Assign(db, job, services);
                            decimal estCost = CRM.infrastructure.services.JobServices.CalculateTotal(services);
                            job.TotalEstimatedCost = estCost;
                        }
                    }

                    db.JobOrders.Add(job);

                    target.Status = "Checked In";
                    target.LinkedJobOrder = job;

                    // Record Appointment History
                    db.AppointmentHistories.Add(new AppointmentHistory
                    {
                        Appointment = target,
                        Action = "Checked In",
                        Reason = "Checked in and created Job Order",
                        Actor = db.AuditActor,
                        OccurredAtUtc = DateTime.UtcNow
                    });

                    db.SaveChanges();
                    tx.Commit();

                    createdJobRef = RecordReference.JobOrder(job.JobOrderId);
                });

                MessageBox.Show(this, $"Check in successful! Created Job Order {createdJobRef}.", "Checked In", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to check in appointment:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelSelected()
        {
            var appt = SelectedAppointment;
            if (appt == null || appt.Status != "Scheduled") return;

            // If a Job Order is linked, redirect to Job Cancellation workflow
            if (appt.LinkedJobOrderId.HasValue)
            {
                string jobRef = RecordReference.JobOrder(appt.LinkedJobOrderId.Value);
                if (MessageBox.Show(this,
                    $"A Job Order ({jobRef}) has already been created for this appointment.\n\n" +
                    "Direct appointment cancellation is prohibited to ensure incurred costs, tasks, and payments are formally reviewed.\n\n" +
                    $"Would you like to open the Job Cancellation & Refund review dialog for {jobRef}?",
                    "Linked Job Order Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    using var db = factory();
                    var linkedJob = db.JobOrders.Find(appt.LinkedJobOrderId.Value);
                    if (linkedJob != null)
                    {
                        using var cancelJobDlg = new CancelJobDialogForm(linkedJob, factory);
                        cancelJobDlg.ShowDialog(this);
                        LoadAppointments();
                    }
                }
                return;
            }

            using var reasonForm = new Form
            {
                Text = $"Cancel Appointment — {RecordReference.Appointment(appt.AppointmentId)}",
                Size = new Size(460, 310),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lblCat = new Label { Text = "Reason Category (Required):", Location = new Point(20, 16), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var cmbCategory = new ComboBox
            {
                Location = new Point(20, 40),
                Size = new Size(400, 26),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCategory.Items.AddRange(CRM.infrastructure.services.AppointmentService.CancellationReasons);
            cmbCategory.SelectedIndex = 0;

            var lblExp = new Label { Text = "Explanation / Details (Required, min 4 chars):", Location = new Point(20, 78), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var txtReason = new TextBox
            {
                Location = new Point(20, 102),
                Size = new Size(400, 80),
                Multiline = true,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "Provide specific customer reason or notes for cancellation..."
            };

            var btnConfirm = new Button
            {
                Text = "Cancel Appointment",
                Location = new Point(240, 205),
                Size = new Size(180, 32),
                DialogResult = DialogResult.OK,
                BackColor = Color.Firebrick,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            var btnClose = new Button { Text = "Close", Location = new Point(140, 205), Size = new Size(90, 32), DialogResult = DialogResult.Cancel };

            reasonForm.Controls.AddRange(new Control[] { lblCat, cmbCategory, lblExp, txtReason, btnConfirm, btnClose });
            reasonForm.AcceptButton = btnConfirm;
            reasonForm.CancelButton = btnClose;

            if (reasonForm.ShowDialog(this) == DialogResult.OK)
            {
                string category = cmbCategory.SelectedItem?.ToString() ?? "Other";
                string explanation = txtReason.Text.Trim();
                if (explanation.Length < 4)
                {
                    MessageBox.Show(this, "A detailed cancellation explanation of at least 4 characters is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var db = factory();
                    CRM.infrastructure.services.AppointmentService.Cancel(db, appt.AppointmentId, category, explanation, db.AuditActor);
                    MessageBox.Show(this, $"Appointment {RecordReference.Appointment(appt.AppointmentId)} cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadAppointments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Cancellation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MarkNoShowSelected()
        {
            var appt = SelectedAppointment;
            if (appt == null || appt.Status != "Scheduled") return;

            if (MessageBox.Show(this, $"Mark Appointment {RecordReference.Appointment(appt.AppointmentId)} as No Show?", "Confirm No Show", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            try
            {
                using var db = factory();
                CRM.infrastructure.services.AppointmentService.MarkNoShow(db, appt.AppointmentId, db.AuditActor);
                MessageBox.Show(this, $"Appointment {RecordReference.Appointment(appt.AppointmentId)} marked as No Show.", "No Show", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public sealed class ScheduleAppointmentForm : Form
    {
        private readonly Func<MasterCrmDbContext> factory;
        private readonly int? inquiryId;
        private readonly int? initialCustomerId;

        private Customer? selectedCustomer;
        private Vehicle? selectedVehicle;

        private TextBox txtCustomer;
        private TextBox txtVehicle;
        private DateTimePicker dtStart;
        private CheckedListBox clbServices;
        private TextBox txtNotes;
        private Button btnBrowseCust;
        private Button btnBrowseVeh;
        private Label lblError;

        public ScheduleAppointmentForm(Func<MasterCrmDbContext> factory, int? inquiryId, int? customerId)
        {
            this.factory = factory;
            this.inquiryId = inquiryId;
            this.initialCustomerId = customerId;

            InitializeUi();
            LoadInitialData();
        }

        private void InitializeUi()
        {
            Text = "Schedule New Workshop Appointment";
            Size = new Size(580, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblTitle = new Label { Text = "Schedule Workshop Appointment", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Location = new Point(20, 16), AutoSize = true };

            var lblCust = new Label { Text = "Customer (Searchable):", Location = new Point(20, 55), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtCustomer = new TextBox { Location = new Point(20, 75), Size = new Size(420, 24), ReadOnly = true };
            btnBrowseCust = new Button { Text = "Browse...", Location = new Point(450, 74), Size = new Size(90, 26) };
            btnBrowseCust.Click += (s, e) => BrowseCustomer();

            var lblVeh = new Label { Text = "Vehicle (Searchable / Optional):", Location = new Point(20, 110), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtVehicle = new TextBox { Location = new Point(20, 130), Size = new Size(420, 24), ReadOnly = true };
            btnBrowseVeh = new Button { Text = "Pick / Add...", Location = new Point(450, 129), Size = new Size(90, 26) };
            btnBrowseVeh.Click += (s, e) => BrowseVehicle();

            var lblDate = new Label { Text = "Scheduled Date & Time:", Location = new Point(20, 165), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            dtStart = new DateTimePicker { Location = new Point(20, 185), Size = new Size(260, 24), Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd hh:mm tt", Value = DateTime.Now.AddDays(1).Date.AddHours(9) };

            var lblServ = new Label { Text = "Requested Services:", Location = new Point(20, 220), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            clbServices = new CheckedListBox { Location = new Point(20, 240), Size = new Size(520, 90), CheckOnClick = true };
            clbServices.Items.AddRange(CRM.infrastructure.services.JobServices.OfferedWithPrices);

            var lblNotes = new Label { Text = "Intake Notes / Instructions:", Location = new Point(20, 340), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            txtNotes = new TextBox { Location = new Point(20, 360), Size = new Size(520, 50), Multiline = true };

            lblError = new Label { Location = new Point(20, 420), Size = new Size(520, 24), ForeColor = Color.Firebrick, Font = new Font("Segoe UI", 9F) };

            var btnSave = new Button { Text = "Save Appointment", Location = new Point(380, 445), Size = new Size(160, 32), BackColor = Color.FromArgb(36, 137, 203), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveAppointment();

            var btnCancel = new Button { Text = "Cancel", Location = new Point(280, 445), Size = new Size(90, 32), DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[] { lblTitle, lblCust, txtCustomer, btnBrowseCust, lblVeh, txtVehicle, btnBrowseVeh, lblDate, dtStart, lblServ, clbServices, lblNotes, txtNotes, lblError, btnSave, btnCancel });
            CancelButton = btnCancel;
        }

        private void LoadInitialData()
        {
            if (initialCustomerId.HasValue)
            {
                using var db = factory();
                selectedCustomer = db.Customers.Find(initialCustomerId.Value);
                if (selectedCustomer != null)
                {
                    txtCustomer.Text = $"{selectedCustomer.FullName} ({selectedCustomer.ContactNumber})";
                    selectedVehicle = db.Vehicles.FirstOrDefault(v => v.CustomerId == selectedCustomer.CustomerId);
                    if (selectedVehicle != null) txtVehicle.Text = selectedVehicle.DisplayName;
                }
            }
        }

        private void BrowseCustomer()
        {
            using var db = factory();
            var customers = db.Customers.Where(c => c.IsActive).ToList();
            using var picker = new CustomerPickerDialogForm(customers, selectedCustomer?.CustomerId, allowNone: false);
            if (picker.ShowDialog(this) == DialogResult.OK && picker.SelectedCustomer != null)
            {
                selectedCustomer = picker.SelectedCustomer;
                txtCustomer.Text = $"{selectedCustomer.FullName} ({selectedCustomer.ContactNumber})";

                var veh = db.Vehicles.FirstOrDefault(v => v.CustomerId == selectedCustomer.CustomerId);
                if (veh != null)
                {
                    selectedVehicle = veh;
                    txtVehicle.Text = veh.DisplayName;
                }
            }
        }

        private void BrowseVehicle()
        {
            using var db = factory();
            var vehicles = selectedCustomer != null
                ? db.Vehicles.Where(v => v.CustomerId == selectedCustomer.CustomerId).ToList()
                : db.Vehicles.ToList();

            if (vehicles.Count == 0)
            {
                string make = Microsoft.VisualBasic.Interaction.InputBox("Enter Vehicle Make (e.g. Toyota):", "New Vehicle", "Toyota");
                if (string.IsNullOrWhiteSpace(make)) return;
                string model = Microsoft.VisualBasic.Interaction.InputBox("Enter Vehicle Model (e.g. Vios):", "New Vehicle", "Vios");
                if (string.IsNullOrWhiteSpace(model)) return;
                string plate = Microsoft.VisualBasic.Interaction.InputBox("Enter Plate Number:", "New Vehicle", "");

                var newVeh = new Vehicle
                {
                    CustomerId = selectedCustomer?.CustomerId,
                    Make = make.Trim(),
                    Model = model.Trim(),
                    PlateNumber = plate.Trim(),
                    CreatedAt = DateTime.UtcNow
                };
                db.Vehicles.Add(newVeh);
                db.SaveChanges();
                selectedVehicle = newVeh;
                txtVehicle.Text = newVeh.DisplayName;
                return;
            }

            var choices = vehicles.Select(v => v.DisplayName).ToArray();
            var selectedIdx = 0;
            selectedVehicle = vehicles[selectedIdx];
            txtVehicle.Text = selectedVehicle.DisplayName;
        }

        private void SaveAppointment()
        {
            lblError.Text = "";
            if (dtStart.Value < DateTime.Now.AddHours(-1))
            {
                lblError.Text = "Scheduled start time cannot be in the past.";
                return;
            }

            var chosenServices = new List<string>();
            foreach (var item in clbServices.CheckedItems)
            {
                chosenServices.Add(CRM.infrastructure.services.JobServices.CleanServiceName(item?.ToString() ?? ""));
            }

            try
            {
                using var db = factory();
                var appt = new Appointment
                {
                    CustomerId = selectedCustomer?.CustomerId,
                    VehicleId = selectedVehicle?.VehicleId,
                    InquiryId = inquiryId,
                    ScheduledStart = dtStart.Value,
                    RequestedServices = string.Join(", ", chosenServices),
                    Notes = txtNotes.Text.Trim(),
                    Status = "Scheduled",
                    CreatedAt = DateTime.UtcNow
                };

                db.Appointments.Add(appt);
                db.SaveChanges();

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
        }
    }
}
