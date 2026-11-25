using System.Windows.Forms;

namespace CRM_Revcare_
{
    public partial class TransactionDialogForm : Form
    {
        public TransactionDialogForm() { InitializeComponent(); }

        // The fields remain owned by their Designer page. The dialog borrows them
        // while open so all existing validation and lookup bindings are retained.
        internal Panel EditorHost => editorHost;
        internal Panel MessageHost => messageHost;
    }
}
