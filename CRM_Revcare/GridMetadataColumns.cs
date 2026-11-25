using System;
using System.Windows.Forms;

namespace CRM_Revcare_;

internal static class GridMetadataColumns
{
    internal static void Hide(DataGridView grid)
    {
        foreach (DataGridViewColumn column in grid.Columns)
        {
            string property = string.IsNullOrEmpty(column.DataPropertyName) ? column.Name : column.DataPropertyName;
            bool active = property.Equals("IsActive", StringComparison.OrdinalIgnoreCase)
                || column.HeaderText.Equals("Active", StringComparison.OrdinalIgnoreCase);
            bool created = property.StartsWith("CreatedAt", StringComparison.OrdinalIgnoreCase)
                || column.HeaderText.Equals("Created At", StringComparison.OrdinalIgnoreCase);
            // Payment date is business information, not a generic creation metadata column.
            if (active || (created && !column.HeaderText.StartsWith("Payment Date", StringComparison.OrdinalIgnoreCase)))
                column.Visible = false;
        }
    }
}
