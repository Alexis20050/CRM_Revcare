using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace CRM_Revcare_
{
    // Non-generic, database-free base for the six Visual Studio designer surfaces.
    public class TransactionPage : UserControl
    {
        private ITransactionController controller;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataGridView Grid => FindControl<DataGridView>("Records");

        internal TControl FindControl<TControl>(string name) where TControl : Control =>
            (TControl)Controls.Find(name, true).Single();

        internal void Attach(ITransactionController value) => controller = value;
        public void NewRecord() => controller?.NewRecord();
        public void RefreshRecords() => controller?.RefreshRecords();
        public void SaveRecord() => controller?.SaveRecord();
        public void DeleteSelected() => controller?.DeleteSelected();
        public void ApplyListFilter() => controller?.ApplyListFilter();
        public void ApplySyncedRecords(IEnumerable records) => controller?.ApplySyncedRecords(records);
    }

    internal interface ITransactionController
    {
        void NewRecord();
        void RefreshRecords();
        void SaveRecord();
        void DeleteSelected();
        void ApplyListFilter();
        void ApplySyncedRecords(IEnumerable records);
    }
}
