using System;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    public partial class CrmTransactionsForm
    {
        private void UseEntryDialog<T>(CrmCollectionController<T> controller, Panel editor, Label result,
            Button save, string title) where T : class, new()
        {
            controller.OpenDialogEditor = row =>
            {
                using var dialog = new TransactionDialogForm();
                controller.LoadEditor(row);
                string refStr = controller.SelectedId > 0 ? $" ({crm.domain.entities.RecordReference.FormatForRecord(title, controller.SelectedId)})" : "";
                dialog.Text = controller.IsNewRecord ? "Add " + title : "Edit " + title + refStr;
                if (title == "Payment")
                    dialog.Text = paymentsPage.voidPayment.Checked ? $"Void Payment{refStr}" :
                        controller.IsNewRecord ? "Receive Payment" : $"View Receipt{refStr}";

                if (title == "Job Order")
                {
                    dialog.ClientSize = new System.Drawing.Size(880, 640);
                    dialog.MinimumSize = new System.Drawing.Size(840, 600);
                }
                else if (title == "Customer")
                {
                    dialog.ClientSize = new System.Drawing.Size(600, 390);
                    dialog.MinimumSize = new System.Drawing.Size(560, 360);
                }
                else if (title == "Payment")
                {
                    dialog.ClientSize = new System.Drawing.Size(800, 520);
                    dialog.MinimumSize = new System.Drawing.Size(760, 480);
                }
                else
                {
                    dialog.ClientSize = new System.Drawing.Size(720, 540);
                    dialog.MinimumSize = new System.Drawing.Size(660, 480);
                }
                dialog.AcceptButton = save;
                var editorParent = editor.Parent;
                var resultParent = result.Parent;
                int editorIndex = editorParent.Controls.GetChildIndex(editor);
                int resultIndex = resultParent.Controls.GetChildIndex(result);
                var editorDock = editor.Dock;
                var resultDock = result.Dock;
                var originalSize = editor.Size;
                var originalResultSize = result.Size;
                void Saved() { dialog.DialogResult = DialogResult.OK; dialog.Close(); }
                controller.RecordSaved += Saved;
                try
                {
                    dialog.EditorHost.Controls.Add(editor);
                    editor.Dock = DockStyle.Fill;
                    editor.Visible = true;
                    dialog.MessageHost.Controls.Add(result);
                    result.Dock = DockStyle.Fill;
                    bool saved = dialog.ShowDialog(this) == DialogResult.OK;
                    if (saved && title == "Payment") ordersPage.RefreshRecords();
                    if (saved && title == "Job Order")
                    {
                        ordersPage.RefreshRecords();
                        showJobTasks?.Invoke(controller.LastSavedId);
                    }
                    return saved;
                }
                finally
                {
                    controller.RecordSaved -= Saved;
                    editor.Visible = false;
                    editorParent.Controls.Add(editor);
                    editor.Dock = editorDock;
                    editor.Size = originalSize;
                    editorParent.Controls.SetChildIndex(editor, editorIndex);
                    resultParent.Controls.Add(result);
                    result.Dock = resultDock;
                    result.Size = originalResultSize;
                    resultParent.Controls.SetChildIndex(result, resultIndex);
                    controller.NewRecord();
                }
            };
        }
    }
}
