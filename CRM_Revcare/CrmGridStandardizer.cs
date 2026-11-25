#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    /// <summary>
    /// Centralized standardizer for CRM DataGridViews.
    /// Enforces fixed, non-scaling columns with manual resizability,
    /// standard header and row heights, horizontal/vertical scrollbars,
    /// session-level column width persistence across operations, and header reset menus.
    /// </summary>
    public static class CrmGridStandardizer
    {
        public const int StandardHeaderHeight = 42;
        public const int StandardRowHeight = 36;

        // Session-level preservation of user-adjusted column widths: "gridKey:columnName" -> width
        private static readonly Dictionary<string, int> SavedColumnWidths = new(StringComparer.OrdinalIgnoreCase);
        private static bool _isApplyingLayout;

        #region Default Module Column Widths (DefaultWidth, MinWidth)

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> FeedbackColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "FeedbackId", (125, 80) },
                { "FeedbackIdColumn", (125, 80) },
                { "JobOrderId", (300, 150) },
                { "JobOrderIdColumn", (300, 150) },
                { "RatingScore", (150, 90) },
                { "RatingScoreColumn", (150, 90) },
                { "PositiveRemarks", (350, 150) },
                { "PositiveRemarksColumn", (350, 150) },
                { "AreasForImprovement", (350, 150) },
                { "AreasForImprovementColumn", (350, 150) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> CustomerColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "CustomerId", (125, 80) },
                { "CustomerIdColumn", (125, 80) },
                { "FullName", (260, 140) },
                { "FullNameColumn", (260, 140) },
                { "ContactNumber", (170, 110) },
                { "ContactNumberColumn", (170, 110) },
                { "Email", (240, 120) },
                { "EmailColumn", (240, 120) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> InquiryColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "InquiryId", (125, 80) },
                { "InquiryIdColumn", (125, 80) },
                { "CustomerName", (220, 120) },
                { "CustomerNameColumn", (220, 120) },
                { "ContactNumber", (160, 110) },
                { "ContactNumberColumn", (160, 110) },
                { "InquiryPlatform", (130, 80) },
                { "InquiryPlatformColumn", (130, 80) },
                { "MessageDetails", (350, 150) },
                { "MessageDetailsColumn", (350, 150) },
                { "Status", (130, 80) },
                { "StatusColumn", (130, 80) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> ComplaintColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "ComplaintId", (125, 80) },
                { "ComplaintIdColumn", (125, 80) },
                { "CustomerId", (220, 120) },
                { "CustomerIdColumn", (220, 120) },
                { "JobOrderId", (160, 100) },
                { "JobOrderIdColumn", (160, 100) },
                { "ComplaintType", (160, 100) },
                { "ComplaintTypeColumn", (160, 100) },
                { "Description", (350, 150) },
                { "DescriptionColumn", (350, 150) },
                { "ResolutionStatus", (130, 80) },
                { "ResolutionStatusColumn", (130, 80) },
                { "ActionTaken", (260, 120) },
                { "ActionTakenColumn", (260, 120) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> JobOrderColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "JobOrderId", (130, 95) },
                { "jobIdColumn", (130, 95) },
                { "EstimateId", (280, 140) },
                { "referenceColumn", (280, 140) },
                { "Status", (140, 100) },
                { "statusColumn", (140, 100) },
                { "StartedAt", (180, 140) },
                { "startedColumn", (180, 140) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> JobTaskColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "JobTaskId", (125, 80) },
                { "JobTaskIdColumn", (125, 80) },
                { "JobOrderId", (130, 90) },
                { "JobOrderIdColumn", (130, 90) },
                { "ServiceId", (240, 130) },
                { "ServiceIdColumn", (240, 130) },
                { "Remarks", (300, 140) },
                { "RemarksColumn", (300, 140) },
                { "Status", (130, 80) },
                { "StatusColumn", (130, 80) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> TaskJobsOverviewColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "JobOrderId", (130, 90) },
                { "EstimateId", (220, 130) },
                { "Customer", (240, 130) },
                { "Vehicle", (180, 110) },
                { "Status", (130, 90) },
                { "Progress", (160, 110) },
                { "StartedAt", (170, 120) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> PaymentColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "PaymentId", (125, 80) },
                { "PaymentIdColumn", (125, 80) },
                { "JobOrderId", (140, 90) },
                { "JobOrderIdColumn", (140, 90) },
                { "PaymentType", (150, 90) },
                { "PaymentTypeColumn", (150, 90) },
                { "AmountPaid", (150, 100) },
                { "AmountPaidColumn", (150, 100) },
                { "PaymentMethod", (160, 100) },
                { "PaymentMethodColumn", (160, 100) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> CancellationReviewColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "RequestId", (110, 70) },
                { "JobOrderId", (100, 65) },
                { "Customer", (180, 100) },
                { "Vehicle", (160, 100) },
                { "TotalPaidAtRequest", (130, 80) },
                { "TotalPaidDisplay", (130, 80) },
                { "ProposedRefund", (130, 80) },
                { "ProposedRefundDisplay", (130, 80) },
                { "ApprovedRefund", (130, 80) },
                { "ApprovedRefundDisplay", (130, 80) },
                { "ReasonCategory", (160, 100) },
                { "Status", (120, 85) },
                { "RequestedBy", (130, 75) },
                { "RequestedAtUtc", (160, 90) },
                { "RequestedAtDisplay", (160, 90) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> RefundHistoryColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "RefundId", (110, 70) },
                { "JobOrderId", (100, 65) },
                { "PaymentId", (110, 70) },
                { "PaymentIdDisplay", (110, 70) },
                { "RecipientName", (180, 95) },
                { "Amount", (130, 85) },
                { "AmountDisplay", (130, 85) },
                { "RefundMethod", (120, 70) },
                { "Reason", (250, 120) },
                { "AuthorizedBy", (130, 80) },
                { "RefundDateUtc", (160, 90) },
                { "RefundDateDisplay", (160, 90) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> JobBalancesColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "JobOrderId", (110, 70) },
                { "Customer", (200, 110) },
                { "Vehicle", (160, 100) },
                { "EstimateCost", (130, 90) },
                { "TotalPaid", (130, 90) },
                { "RemainingBalance", (140, 90) },
                { "PaymentStatus", (140, 90) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> AppointmentColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { GridRowNumbering.RowNumberColumnName, (55, 45) },
                { "No", (55, 45) },
                { "AppointmentId", (125, 80) },
                { "ScheduledStart", (180, 130) },
                { "Customer", (220, 120) },
                { "Vehicle", (180, 110) },
                { "Status", (130, 80) },
                { "RequestedServices", (260, 130) },
                { "JobOrderId", (130, 80) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> MechanicAssignmentColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { "DisplayName", (220, 120) },
                { "colName", (220, 120) },
                { "Role", (180, 100) },
                { "colRole", (180, 100) },
                { "WorkloadText", (160, 90) },
                { "colWorkload", (160, 90) }
            };

        public static readonly IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> JobTasksDialogColumns =
            new Dictionary<string, (int defaultWidth, int minWidth)>(StringComparer.OrdinalIgnoreCase)
            {
                { "NoCol", (55, 45) },
                { "TaskIdCol", (125, 80) },
                { "ServiceNameCol", (280, 140) },
                { "StatusCol", (130, 85) },
                { "RemarksCol", (300, 140) }
            };

        #endregion

        /// <summary>
        /// Resolves the default column definitions for a given module name.
        /// </summary>
        public static IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> ResolveDefinitions(string moduleName)
        {
            return moduleName.Trim().ToLowerInvariant() switch
            {
                "feedback" => FeedbackColumns,
                "customer" or "customers" => CustomerColumns,
                "inquiry" or "inquiries" => InquiryColumns,
                "complaint" or "complaints" => ComplaintColumns,
                "job order" or "joborder" or "joborders" or "order" => JobOrderColumns,
                "job task" or "jobtask" or "jobtasks" or "task" or "tasks" => JobTaskColumns,
                "taskjobs" or "taskjobsoverview" => TaskJobsOverviewColumns,
                "payment" or "payments" or "billing" => PaymentColumns,
                "cancellationreviews" => CancellationReviewColumns,
                "refundhistory" or "refunds" => RefundHistoryColumns,
                "jobbalances" => JobBalancesColumns,
                "appointment" or "appointments" => AppointmentColumns,
                "mechanic" or "mechanics" => MechanicAssignmentColumns,
                "jobtasksdialog" => JobTasksDialogColumns,
                _ => FeedbackColumns
            };
        }

        /// <summary>
        /// Configures standardized grid dimensions, sizing modes, and scrollbars.
        /// </summary>
        public static void ApplyStandardSettings(DataGridView grid)
        {
            if (grid == null) return;

            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersHeight = StandardHeaderHeight;
            grid.AllowUserToResizeColumns = true;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToOrderColumns = false;
            grid.ScrollBars = ScrollBars.Both;
            grid.RowTemplate.Height = StandardRowHeight;
            grid.ShowCellToolTips = true;
        }

        /// <summary>
        /// Applies fixed default column widths, enforces minimum widths, restores saved session widths,
        /// and attaches user-resizing listeners and header context menu.
        /// </summary>
        public static void ApplyColumnLayout(DataGridView grid, string moduleName, IReadOnlyDictionary<string, (int defaultWidth, int minWidth)>? customDefs = null)
        {
            if (grid == null) return;
            ApplyStandardSettings(grid);

            var defs = customDefs ?? ResolveDefinitions(moduleName);

            _isApplyingLayout = true;
            try
            {
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    col.Resizable = DataGridViewTriState.True;

                    string savedKey = $"{moduleName}:{col.Name}";
                    if (SavedColumnWidths.TryGetValue(savedKey, out int userWidth) && userWidth > 0)
                    {
                        col.Width = userWidth;
                    }
                    else if (FindDefinition(defs, col, out var def))
                    {
                        col.Width = def.defaultWidth;
                        col.MinimumWidth = def.minWidth;
                    }
                    else if (col.Width <= 0 || col.Width > 500)
                    {
                        col.Width = 150;
                        col.MinimumWidth = 80;
                    }

                    if (FindDefinition(defs, col, out var minDef))
                    {
                        col.MinimumWidth = minDef.minWidth;
                    }
                }
            }
            finally
            {
                _isApplyingLayout = false;
            }

            // Track manual user resizing
            grid.ColumnWidthChanged -= Grid_ColumnWidthChanged;
            grid.ColumnWidthChanged += Grid_ColumnWidthChanged;
            grid.Tag = moduleName;

            // Header Context Menu for "Reset Column Widths"
            AttachResetContextMenu(grid, moduleName, defs);
        }

        private static void Grid_ColumnWidthChanged(object? sender, DataGridViewColumnEventArgs e)
        {
            if (_isApplyingLayout || e.Column == null || sender is not DataGridView grid) return;
            string? moduleName = grid.Tag as string ?? "DefaultGrid";
            string savedKey = $"{moduleName}:{e.Column.Name}";
            SavedColumnWidths[savedKey] = e.Column.Width;
        }

        private static bool FindDefinition(IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> defs, DataGridViewColumn col, out (int defaultWidth, int minWidth) def)
        {
            if (defs.TryGetValue(col.Name, out def)) return true;
            if (!string.IsNullOrEmpty(col.DataPropertyName) && defs.TryGetValue(col.DataPropertyName, out def)) return true;
            if (!string.IsNullOrEmpty(col.HeaderText) && defs.TryGetValue(col.HeaderText, out def)) return true;
            def = default;
            return false;
        }

        /// <summary>
        /// Restores saved column widths after re-binding without altering user customization.
        /// </summary>
        public static void RestoreColumnWidths(DataGridView grid, string moduleName)
        {
            if (grid == null) return;
            var defs = ResolveDefinitions(moduleName);

            _isApplyingLayout = true;
            try
            {
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                    string savedKey = $"{moduleName}:{col.Name}";
                    if (SavedColumnWidths.TryGetValue(savedKey, out int userWidth) && userWidth > 0)
                    {
                        col.Width = userWidth;
                    }
                    else if (FindDefinition(defs, col, out var def))
                    {
                        if (col.Width <= 0) col.Width = def.defaultWidth;
                        col.MinimumWidth = def.minWidth;
                    }
                }
            }
            finally
            {
                _isApplyingLayout = false;
            }
        }

        /// <summary>
        /// Resets all columns in the grid to their standard default widths.
        /// </summary>
        public static void ResetColumnWidths(DataGridView grid, string moduleName, IReadOnlyDictionary<string, (int defaultWidth, int minWidth)>? customDefs = null)
        {
            if (grid == null) return;
            var defs = customDefs ?? ResolveDefinitions(moduleName);

            _isApplyingLayout = true;
            try
            {
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    string savedKey = $"{moduleName}:{col.Name}";
                    SavedColumnWidths.Remove(savedKey);

                    if (FindDefinition(defs, col, out var def))
                    {
                        col.Width = def.defaultWidth;
                        col.MinimumWidth = def.minWidth;
                    }
                }
            }
            finally
            {
                _isApplyingLayout = false;
            }
        }

        private static void AttachResetContextMenu(DataGridView grid, string moduleName, IReadOnlyDictionary<string, (int defaultWidth, int minWidth)> defs)
        {
            var menu = new ContextMenuStrip();
            var resetItem = new ToolStripMenuItem("Reset Column Widths");
            resetItem.Click += (s, e) => ResetColumnWidths(grid, moduleName, defs);
            menu.Items.Add(resetItem);

            grid.MouseUp -= Grid_MouseUp;
            grid.MouseUp += Grid_MouseUp;

            void Grid_MouseUp(object? s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Right)
                {
                    var hit = grid.HitTest(e.X, e.Y);
                    if (hit.Type == DataGridViewHitTestType.ColumnHeader)
                    {
                        menu.Show(grid, e.Location);
                    }
                }
            }
        }
    }
}
