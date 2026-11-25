#nullable enable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public sealed class AppointmentHistoryDialogForm : Form
    {
        private readonly int appointmentId;
        private readonly Func<MasterCrmDbContext> factory;
        private DataGridView grid;
        private Label lblHeader;

        public AppointmentHistoryDialogForm(Func<MasterCrmDbContext> factory, int appointmentId)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.appointmentId = appointmentId;

            InitializeUi();
            LoadHistory();
        }

        private void InitializeUi()
        {
            Text = $"Appointment History — {RecordReference.Appointment(appointmentId)}";
            Size = new Size(820, 450);
            MinimumSize = new Size(700, 350);
            StartPosition = FormStartPosition.CenterParent;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(246, 248, 250),
                Padding = new Padding(16, 12, 16, 12)
            };

            lblHeader = new Label
            {
                Text = $"Audit & Lifecycle History for {RecordReference.Appointment(appointmentId)}",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                AutoSize = true
            };
            topPanel.Controls.Add(lblHeader);

            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                Padding = new Padding(16, 8, 16, 8)
            };
            var btnClose = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Right,
                Width = 90
            };
            bottomPanel.Controls.Add(btnClose);
            AcceptButton = btnClose;

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoGenerateColumns = false
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "OccurredAtUtc", HeaderText = "Date / Time (Local)", Width = 145 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Action", DataPropertyName = "Action", Width = 110 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PreviousScheduledStart", HeaderText = "Previous Schedule", Width = 135 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "NewScheduledStart", HeaderText = "New Schedule", Width = 135 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Reason", HeaderText = "Reason / Notes", DataPropertyName = "Reason", Width = 230 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Actor", HeaderText = "Actor", DataPropertyName = "Actor", Width = 100 });

            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= grid.Rows.Count) return;
                var h = grid.Rows[e.RowIndex].DataBoundItem as AppointmentHistory;
                if (h == null) return;

                string colName = grid.Columns[e.ColumnIndex].Name;
                if (colName == "OccurredAtUtc")
                {
                    e.Value = h.OccurredAtUtc.ToLocalTime().ToString("yyyy-MM-dd hh:mm tt");
                    e.FormattingApplied = true;
                }
                else if (colName == "PreviousScheduledStart")
                {
                    e.Value = h.PreviousScheduledStart.HasValue ? h.PreviousScheduledStart.Value.ToString("yyyy-MM-dd hh:mm tt") : "—";
                    e.FormattingApplied = true;
                }
                else if (colName == "NewScheduledStart")
                {
                    e.Value = h.NewScheduledStart.HasValue ? h.NewScheduledStart.Value.ToString("yyyy-MM-dd hh:mm tt") : "—";
                    e.FormattingApplied = true;
                }
            };

            Controls.Add(grid);
            Controls.Add(bottomPanel);
            Controls.Add(topPanel);
        }

        private void LoadHistory()
        {
            try
            {
                using var db = factory();
                var history = db.AppointmentHistories
                    .Where(h => h.AppointmentId == appointmentId)
                    .OrderByDescending(h => h.OccurredAtUtc)
                    .ToList();

                grid.DataSource = history;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not load history: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
