#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    public static class GridRowNumbering
    {
        public const string RowNumberColumnName = "ColRowNo";

        public static DataGridViewTextBoxColumn CreateColumn()
        {
            return new DataGridViewTextBoxColumn
            {
                Name = RowNumberColumnName,
                HeaderText = "No.",
                ReadOnly = true,
                Width = 55,
                MinimumWidth = 45,
                FillWeight = 8F,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    ForeColor = Color.FromArgb(100, 116, 139)
                },
                HeaderCell = new DataGridViewColumnHeaderCell
                {
                    Value = "No.",
                    Style = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
                }
            };
        }

        public static void Enable(DataGridView grid, Func<int>? getPageOffset = null)
        {
            if (grid == null) return;
            if (!grid.Columns.Contains(RowNumberColumnName))
            {
                var col = CreateColumn();
                grid.Columns.Insert(0, col);
            }

            grid.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && e.ColumnIndex < grid.Columns.Count)
                {
                    if (grid.Columns[e.ColumnIndex].Name == RowNumberColumnName)
                    {
                        int offset = getPageOffset?.Invoke() ?? 0;
                        e.Value = (offset + e.RowIndex + 1).ToString();
                        e.FormattingApplied = true;
                    }
                }
            };
        }
    }
}
