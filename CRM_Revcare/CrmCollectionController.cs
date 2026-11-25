using System;
using System.Collections.Generic;
using System.Data;
using System.Collections;
using System.Linq;
using System.Windows.Forms;
using crm.domain.entities;
using CRM.infrastructure.common;
using CRM.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    // Database behavior attached to controls owned by the WinForms Designer.
    internal sealed class CrmCollectionController<T> : ITransactionController where T : class, new()
    {
        private readonly TransactionPage page;
        private readonly Func<MasterCrmDbContext> createContext;
        private readonly Func<T, int> key;
        private readonly Func<string, bool> confirmDelete;
        private readonly Label mode;
        private readonly TextBox recordId;
        private readonly Label result;
        private readonly Button deleteButton;
        private readonly string recordName;
        private Action<T> loadInputs;
        private Action<T, MasterCrmDbContext> readInputs;
        private int selectedId;
        private byte[] editVersion;
        private Guid submissionId;
        private bool binding;
        private List<T> cachedRecords = new List<T>();
        private List<T> visibleRecords = new List<T>();
        private Func<T, bool> listFilter = row => true;
        private Action<int, int> updateListCount;
        private string sortProperty;
        private bool sortAscending = true;

        public int PageSize { get; set; } = 20;
        public int CurrentPage { get; private set; } = 1;
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)visibleRecords.Count / Math.Max(1, PageSize)));

        private readonly Panel pagingPanel;
        private readonly Button btnFirst;
        private readonly Button btnPrev;
        private readonly Label lblPageInfo;
        private readonly Button btnNext;
        private readonly Button btnLast;
        private readonly ComboBox cmbPageSize;

        public Func<T, bool> OpenVoidEditor { get; set; }
        public Func<T, bool> OpenArchiveEditor { get; set; }
        public Func<T, bool> OpenDialogEditor { get; set; }
        public Func<string?> PreValidate { get; set; }
        public int SelectedId => selectedId;
        public T SelectedRecord => selectedId > 0 && (Grid.SelectedRows.Count > 0 || Grid.CurrentRow != null)
            ? (Grid.SelectedRows.Count > 0 ? Grid.SelectedRows[0].DataBoundItem : Grid.CurrentRow?.DataBoundItem) as T
            : null;
        public event Action RecordSaved;
        public int LastSavedId { get; private set; }
        public bool IsNewRecord => selectedId == 0;
        public void LoadEditor(T row)
        {
            selectedId = key(row);
            editVersion = (byte[])typeof(T).GetProperty("RowVersion")?.GetValue(row);
            submissionId = row is crm.domain.entities.Payment p ? p.SubmissionId : Guid.NewGuid();
            var idLabel = FindOptional<Label>(page, "idLabel") ?? FindOptional<Label>(page, "RecordIdLabel");
            if (selectedId == 0)
            {
                if (recordId != null)
                {
                    recordId.Visible = false;
                    recordId.Text = "";
                }
                if (idLabel != null) idLabel.Visible = false;
                if (mode != null) mode.Text = "New " + recordName;
            }
            else
            {
                string formattedRef = RecordReference.FormatForRecord(recordName, selectedId);
                if (recordId != null)
                {
                    recordId.Visible = true;
                    recordId.ReadOnly = true;
                    recordId.Text = formattedRef;
                }
                if (idLabel != null) idLabel.Visible = true;
                if (mode != null) mode.Text = "Edit " + recordName + " (" + formattedRef + ")";
            }
            loadInputs?.Invoke(row);
            result.Text = "Complete the fields and save. Cancel closes without saving.";
        }

        private static TControl FindOptional<TControl>(Control root, string name) where TControl : Control
        {
            var found = root.Controls.Find(name, true);
            return found.Length > 0 ? found[0] as TControl : null;
        }

        public CrmCollectionController(TransactionPage page, string recordName, Func<T, int> key,
            Func<MasterCrmDbContext> createContext, Func<string, bool> confirmDelete)
        {
            this.page = page;
            this.recordName = recordName;
            this.key = key;
            this.createContext = createContext;
            this.confirmDelete = confirmDelete;
            Grid = page.Grid;
            mode = FindOptional<Label>(page, "EditorMode");
            recordId = FindOptional<TextBox>(page, "RecordId");
            result = FindOptional<Label>(page, "Result") ?? new Label();
            deleteButton = FindOptional<Button>(page, "DeleteRecord");

            pagingPanel = new Panel
            {
                Name = "PagingPanel",
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = System.Drawing.Color.FromArgb(246, 248, 250),
                Padding = new Padding(8, 4, 8, 4)
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };

            btnFirst = new Button
            {
                Text = "« First",
                Size = new System.Drawing.Size(60, 26),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnPrev = new Button
            {
                Text = "‹ Prev",
                Size = new System.Drawing.Size(60, 26),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            lblPageInfo = new Label
            {
                AutoSize = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Padding = new Padding(8, 5, 8, 0),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular),
                Text = "Page 1 of 1"
            };
            btnNext = new Button
            {
                Text = "Next ›",
                Size = new System.Drawing.Size(60, 26),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnLast = new Button
            {
                Text = "Last »",
                Size = new System.Drawing.Size(60, 26),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };

            var lblShow = new Label
            {
                Text = "Show:",
                AutoSize = true,
                Padding = new Padding(12, 5, 4, 0),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular)
            };
            cmbPageSize = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 60,
                Font = new System.Drawing.Font("Segoe UI", 8.5F)
            };
            cmbPageSize.Items.AddRange(new object[] { 10, 20, 50, 100 });
            cmbPageSize.SelectedItem = 20;

            flow.Controls.Add(btnFirst);
            flow.Controls.Add(btnPrev);
            flow.Controls.Add(lblPageInfo);
            flow.Controls.Add(btnNext);
            flow.Controls.Add(btnLast);
            flow.Controls.Add(lblShow);
            flow.Controls.Add(cmbPageSize);
            pagingPanel.Controls.Add(flow);

            btnFirst.Click += (s, e) => { CurrentPage = 1; RenderPage(); };
            btnPrev.Click += (s, e) => { if (CurrentPage > 1) { CurrentPage--; RenderPage(); } };
            btnNext.Click += (s, e) => { if (CurrentPage < TotalPages) { CurrentPage++; RenderPage(); } };
            btnLast.Click += (s, e) => { CurrentPage = TotalPages; RenderPage(); };
            cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                if (cmbPageSize.SelectedItem is int size && size > 0)
                {
                    PageSize = size;
                    CurrentPage = 1;
                    RenderPage();
                }
            };

            AttachPagingBar();
            page.HandleCreated += (s, e) => AttachPagingBar();
            CrmGridStandardizer.ApplyColumnLayout(Grid, recordName);
            GridRowNumbering.Enable(Grid, () => (CurrentPage - 1) * PageSize);
            CrmGridStandardizer.RestoreColumnWidths(Grid, recordName);

            var newButton = FindOptional<Button>(page, "NewRecord");
            if (newButton != null) newButton.Click += (s, e) =>
            {
                if (OpenDialogEditor != null) OpenAddDialog();
                else NewRecord();
            };

            var saveButton = FindOptional<Button>(page, "SaveRecord");
            if (saveButton != null) saveButton.Click += async (s, e) =>
            {
                saveButton.Enabled = false;
                var owner = saveButton.FindForm();
                var editor = saveButton.Parent;
                FormClosingEventHandler preventClose = (_, args) => args.Cancel = owner?.DialogResult != DialogResult.OK;
                if (owner != null) owner.FormClosing += preventClose;
                if (editor != null) editor.Enabled = false;
                try { await SaveRecordAsync(); }
                finally
                {
                    if (owner != null) owner.FormClosing -= preventClose;
                    if (editor != null && !editor.IsDisposed) editor.Enabled = true;
                    if (!saveButton.IsDisposed) saveButton.Enabled = true;
                    if (owner != null && !owner.IsDisposed && owner.DialogResult == DialogResult.OK) owner.Close();
                }
            };

            var refreshButton = FindOptional<Button>(page, "RefreshRecords");
            if (refreshButton != null) refreshButton.Click += (s, e) => RefreshRecords();

            var editButton = FindOptional<Button>(page, "EditRecord");
            if (editButton != null)
            {
                editButton.Enabled = false;
                editButton.Click += (s, e) => EditSelected();
            }

            if (deleteButton != null)
            {
                deleteButton.Enabled = false;
                deleteButton.Click += (s, e) => DeleteSelected();
            }

            Grid.TabStop = false;
            Grid.SelectionChanged += (s, e) => SelectRecord();
            Grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelected(); };
            Grid.ColumnHeaderMouseClick += (s, e) =>
            {
                if (e.ColumnIndex < 0 || e.ColumnIndex >= Grid.Columns.Count) return;
                var col = Grid.Columns[e.ColumnIndex];
                if (col.Name == GridRowNumbering.RowNumberColumnName) return;
                string propName = col.DataPropertyName;
                if (string.IsNullOrEmpty(propName)) return;

                if (visibleRecords.Count == 0 || binding) return;

                var prop = typeof(T).GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (prop == null) return;

                if (sortProperty == propName)
                    sortAscending = !sortAscending;
                else
                {
                    sortProperty = propName;
                    sortAscending = true;
                }

                foreach (DataGridViewColumn c in Grid.Columns)
                {
                    if (c.Name == GridRowNumbering.RowNumberColumnName || string.IsNullOrEmpty(c.DataPropertyName))
                        c.SortMode = DataGridViewColumnSortMode.NotSortable;
                    else
                        c.SortMode = DataGridViewColumnSortMode.Programmatic;

                    c.HeaderCell.SortGlyphDirection = (c == col)
                        ? (sortAscending ? SortOrder.Ascending : SortOrder.Descending)
                        : SortOrder.None;
                }

                var comparer = new CrmRecordComparer<T>(prop, sortAscending, key);
                visibleRecords.Sort(comparer);

                int pages = TotalPages;
                if (CurrentPage > pages) CurrentPage = pages;
                if (CurrentPage < 1) CurrentPage = 1;

                RenderPage();
            };
            Grid.Enter += (s, e) =>
            {
                if (selectedId == 0)
                {
                    Grid.ClearSelection();
                    Grid.CurrentCell = null;
                }
            };
            page.VisibleChanged += (s, e) =>
            {
                if (page.Visible && selectedId == 0)
                {
                    Grid.ClearSelection();
                    Grid.CurrentCell = null;
                }
            };
            Grid.DataBindingComplete += (s, e) =>
            {
                if (selectedId == 0)
                {
                    Grid.ClearSelection();
                    Grid.CurrentCell = null;
                }
            };
            page.Attach(this);
        }

        private void AttachPagingBar()
        {
            var parentContainer = Grid.Parent ?? page;
            if (parentContainer != null && !parentContainer.Controls.Contains(pagingPanel))
            {
                parentContainer.Controls.Add(pagingPanel);
                if (result != null && parentContainer.Controls.Contains(result))
                {
                    parentContainer.Controls.SetChildIndex(result, 0);
                    parentContainer.Controls.SetChildIndex(pagingPanel, 1);
                }
                Grid.BringToFront();
            }
        }

        public DataGridView Grid { get; }
        public event Action<System.Collections.Generic.IReadOnlyList<T>> RecordsLoaded;
        public void ConfigureListTools(Func<T, bool> filter, Action<int, int> updateCount)
        {
            listFilter = filter;
            updateListCount = updateCount;
            updateListCount(0, 0);
        }
        public void ConfigureEditor(Action<T> load, Action<T, MasterCrmDbContext> read)
        {
            loadInputs = load;
            readInputs = read;
            NewRecord();
        }
        void ITransactionController.ApplySyncedRecords(IEnumerable records) => ApplySyncedRecords(records.Cast<T>());
        public void ApplyListFilter()
        {
            selectedId = 0;
            visibleRecords = cachedRecords.Where(listFilter).ToList();
            if (!string.IsNullOrEmpty(sortProperty))
            {
                var prop = typeof(T).GetProperty(sortProperty, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var comparer = new CrmRecordComparer<T>(prop, sortAscending, key);
                    visibleRecords.Sort(comparer);
                }
            }
            updateListCount?.Invoke(visibleRecords.Count, cachedRecords.Count);
            CurrentPage = 1;
            RenderPage();
        }

        private List<T> FilterRecords(IEnumerable<T> records)
        {
            cachedRecords = records.OrderByDescending(key).ToList();
            RecordsLoaded?.Invoke(cachedRecords);
            visibleRecords = cachedRecords.Where(listFilter).ToList();
            if (!string.IsNullOrEmpty(sortProperty))
            {
                var prop = typeof(T).GetProperty(sortProperty, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    var comparer = new CrmRecordComparer<T>(prop, sortAscending, key);
                    visibleRecords.Sort(comparer);
                }
            }
            updateListCount?.Invoke(visibleRecords.Count, cachedRecords.Count);
            return visibleRecords;
        }

        public void RenderPage()
        {
            if (binding) return;
            binding = true;
            try
            {
                AttachPagingBar();
                int total = visibleRecords.Count;
                int pages = Math.Max(1, (int)Math.Ceiling((double)total / PageSize));
                if (CurrentPage > pages) CurrentPage = pages;
                if (CurrentPage < 1) CurrentPage = 1;

                var pageSlice = visibleRecords.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
                Grid.DataSource = pageSlice;
                GridMetadataColumns.Hide(Grid);
                CrmGridStandardizer.RestoreColumnWidths(Grid, recordName);

                lblPageInfo.Text = $"Page {CurrentPage} of {pages} ({total} records)";
                btnFirst.Enabled = CurrentPage > 1;
                btnPrev.Enabled = CurrentPage > 1;
                btnNext.Enabled = CurrentPage < pages;
                btnLast.Enabled = CurrentPage < pages;

                bool found = false;
                if (selectedId != 0)
                {
                    Grid.ClearSelection();
                    foreach (DataGridViewRow row in Grid.Rows)
                    {
                        if (row.DataBoundItem is T item && key(item) == selectedId)
                        {
                            row.Selected = true;
                            if (Grid.Columns.Count > 0) Grid.CurrentCell = row.Cells[0];
                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                {
                    Grid.ClearSelection();
                    Grid.CurrentCell = null;
                    if (selectedId != 0)
                    {
                        selectedId = 0;
                        editVersion = null;
                        if (recordId != null) recordId.Text = "Assigned on save";
                        if (mode != null) mode.Text = "New " + recordName.ToLowerInvariant();
                        if (deleteButton != null) deleteButton.Enabled = false;
                        var editBtn = FindOptional<Button>(page, "EditRecord");
                        if (editBtn != null) editBtn.Enabled = false;
                        loadInputs?.Invoke(new T());
                    }
                }
            }
            finally { binding = false; }
        }

        public void OpenAddDialog()
        {
            if (OpenDialogEditor != null)
            {
                var row = new T();
                if (OpenDialogEditor(row)) RefreshRecords();
            }
        }

        public void NewRecord()
        {
            Grid.ClearSelection();
            Grid.CurrentCell = null;
            selectedId = 0;
            submissionId = Guid.NewGuid();
            var idLabel = FindOptional<Label>(page, "idLabel") ?? FindOptional<Label>(page, "RecordIdLabel");
            if (recordId != null)
            {
                recordId.Visible = false;
                recordId.Text = "";
            }
            if (idLabel != null) idLabel.Visible = false;
            if (deleteButton != null) deleteButton.Enabled = false;
            var editButton = FindOptional<Button>(page, "EditRecord");
            if (editButton != null) editButton.Enabled = false;
            if (mode != null) mode.Text = "New " + recordName;
            loadInputs?.Invoke(new T());
            result.Text = OpenDialogEditor != null
                ? "Click + Add " + recordName + " to create a new record, or select a row to edit."
                : "Fill the fields and save, or select a row to edit.";
        }

        public void EditSelected()
        {
            if (SelectedRecord == null)
            {
                result.Text = "Select a row to edit.";
                return;
            }
            if (OpenDialogEditor != null)
            {
                if (OpenDialogEditor(SelectedRecord)) RefreshRecords();
            }
        }

        private void SelectRecord()
        {
            var editBtn = FindOptional<Button>(page, "EditRecord");
            if (binding) return;
            if (Grid.SelectedRows.Count == 0)
            {
                selectedId = 0;
                if (deleteButton != null) deleteButton.Enabled = false;
                if (editBtn != null) editBtn.Enabled = false;
                return;
            }
            var row = Grid.SelectedRows[0].DataBoundItem as T;
            if (row == null)
            {
                selectedId = 0;
                if (deleteButton != null) deleteButton.Enabled = false;
                if (editBtn != null) editBtn.Enabled = false;
                return;
            }
            selectedId = key(row);
            string formattedRef = RecordReference.FormatForRecord(recordName, selectedId);
            if (recordId != null)
            {
                recordId.Visible = true;
                recordId.ReadOnly = true;
                recordId.Text = formattedRef;
            }
            var idLabel = FindOptional<Label>(page, "idLabel") ?? FindOptional<Label>(page, "RecordIdLabel");
            if (idLabel != null) idLabel.Visible = true;
            loadInputs?.Invoke(row);
            if (mode != null) mode.Text = "Editing " + recordName + " (" + formattedRef + ")";
            if (deleteButton != null) deleteButton.Enabled = true;
            if (editBtn != null) editBtn.Enabled = true;
            result.Text = OpenDialogEditor != null
                ? "Row selected. Double-click or click Edit Selected to edit."
                : "Save updates this selected record. Use New / Clear to create another.";
        }

        private void LoadRecords()
        {
            using var dbContext = createContext();
            // Materialize before rebinding: a database failure must not clear the current list/editor.
            var rows = dbContext.Set<T>().AsNoTracking().ToList();
            FilterRecords(rows);
            CurrentPage = 1;
            selectedId = 0;
            RenderPage();
            NewRecord();
            result.Text = rows.Count + " record(s). Select a row to edit.";
        }

        public void RefreshRecords() => Run(LoadRecords);

        public void ApplySyncedRecords(IEnumerable<T> records)
        {
            FilterRecords(records);
            if (selectedId != 0)
            {
                int matchIdx = visibleRecords.FindIndex(x => key(x) == selectedId);
                if (matchIdx >= 0)
                {
                    CurrentPage = (matchIdx / PageSize) + 1;
                }
            }
            RenderPage();
            result.Text = selectedId != 0 && !cachedRecords.Any(x => key(x) == selectedId)
                ? "The edited record was deleted elsewhere. Your entries were kept; use New / Clear to start again."
                : cachedRecords.Count + " record(s) synced. Editor entries were preserved.";
        }

        public void SaveRecord() => Run(() =>
        {
            using var strategyContext = createContext();
            var strategy = strategyContext.Database.CreateExecutionStrategy();
            string saveResult = null;
            strategy.Execute(() =>
            {
                using var dbContext = createContext();
                using var tx = dbContext.Database.BeginTransaction(IsolationLevel.Serializable);
                bool updating = selectedId != 0;
                if (!updating && typeof(T) == typeof(crm.domain.entities.Payment) && dbContext.Payments.Any(p => p.SubmissionId == submissionId))
                { saveResult = "Payment already saved."; return; }
                var row = updating ? dbContext.Set<T>().Find(selectedId) : new T();
                if (row == null) throw new InvalidOperationException("This record was deleted. Refresh the list before continuing.");
                if (updating && editVersion != null) dbContext.Entry(row).Property("RowVersion").OriginalValue = editVersion;
                if (!updating && row is crm.domain.entities.Payment payment) payment.SubmissionId = submissionId;
                readInputs(row, dbContext);
                if (updating) dbContext.Update(row); else dbContext.Add(row);
                dbContext.SaveChanges();
                tx.Commit();
                LastSavedId = key(row);
                saveResult = recordName + " #" + key(row) + (updating ? " updated." : " created.");
            });
            if (RecordSaved != null) RecordSaved.Invoke();
            else AfterSave(saveResult);
        });

        private async System.Threading.Tasks.Task SaveRecordAsync()
        {
            var saveBtn = FindOptional<Button>(page, "SaveRecord");
            if (PreValidate != null)
            {
                var preError = PreValidate();
                if (!string.IsNullOrEmpty(preError))
                {
                    result.Text = preError;
                    var parentForm = result?.FindForm();
                    if (parentForm != null && (parentForm is TransactionDialogForm || parentForm.Modal))
                    {
                        MessageBox.Show(parentForm, preError, "Validation Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }
            }
            if (saveBtn != null) saveBtn.Enabled = false;
            try
            {
                using var strategyContext = createContext();
                string message = "";
                await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    using var db = createContext();
                    await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    bool updating = selectedId != 0;
                    if (!updating && typeof(T) == typeof(crm.domain.entities.Payment) && await db.Payments.AnyAsync(p => p.SubmissionId == submissionId))
                    { message = "Payment already saved."; return; }
                    var row = updating ? await db.Set<T>().FindAsync(selectedId) : new T();
                    if (row == null) throw new InvalidOperationException("Record was deleted. Refresh before editing.");
                    if (updating && editVersion != null) db.Entry(row).Property("RowVersion").OriginalValue = editVersion;
                    if (!updating && row is crm.domain.entities.Payment payment) payment.SubmissionId = submissionId;
                    readInputs(row, db);
                    if (updating) db.Update(row); else db.Add(row);
                    await db.SaveChangesAsync();
                    await tx.CommitAsync();
                    LastSavedId = key(row);
                    string generatedRef = RecordReference.FormatForRecord(recordName, key(row));
                    message = $"{recordName} saved successfully: {generatedRef}.";
                });
                if (RecordSaved != null) RecordSaved.Invoke(); else AfterSave(message);
            }
            catch (DbUpdateConcurrencyException) { result.Text = "Someone changed this record. Cancel, refresh, and reopen it before saving."; }
            catch (InvalidOperationException ex)
            {
                result.Text = ex.Message;
                var form = result?.FindForm();
                if (form != null && (form is TransactionDialogForm || form.Modal))
                {
                    MessageBox.Show(form, ex.Message, "RevCare Auto Hub", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch { result.Text = "Could not save. Check the connection and linked records, then try again."; }
            finally
            {
                if (saveBtn != null) saveBtn.Enabled = true;
            }
        }

        public void DeleteSelected() => Run(() =>
        {
            if (selectedId == 0) throw new InvalidOperationException("Select a record to delete.");
            if (typeof(T) == typeof(crm.domain.entities.Payment))
            {
                if (SelectedRecord != null && OpenVoidEditor?.Invoke(SelectedRecord) == true) RefreshRecords();
                return;
            }
            if (typeof(T) == typeof(crm.domain.entities.JobOrder) || typeof(T) == typeof(crm.domain.entities.JobTask))
            {
                if (SelectedRecord != null && OpenArchiveEditor?.Invoke(SelectedRecord) == true) RefreshRecords();
                else if (OpenArchiveEditor == null) throw new InvalidOperationException($"Permanent deletion of {recordName} is prohibited. Use Archive Record instead.");
                return;
            }
            bool customerRecord = typeof(T) == typeof(crm.domain.entities.Customer);
            if (!confirmDelete((customerRecord ? "Deactivate the selected customer?" : "Permanently delete " + recordName.ToLowerInvariant() + " #" + selectedId + "?"))) return;
            using var dbContext = createContext();
            var strategy = dbContext.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                using var tx = dbContext.Database.BeginTransaction(IsolationLevel.Serializable);
                var row = dbContext.Set<T>().Find(selectedId);
                if (row == null) throw new InvalidOperationException("This record was already deleted. Refresh the list.");
                if (row is crm.domain.entities.JobOrder job)
                {
                    throw new InvalidOperationException($"Permanent deletion of Job Order #{job.JobOrderId} is prohibited. Use Archive Record instead.");
                }
                if (row is crm.domain.entities.JobTask task)
                {
                    throw new InvalidOperationException($"Permanent deletion of Job Task #{task.JobTaskId} is prohibited. Use Archive Record / Service Withdrawal instead.");
                }
                if (row is crm.domain.entities.Payment payment)
                {
                    if (string.IsNullOrWhiteSpace(payment.VoidReason)) throw new InvalidOperationException("Edit this payment, enter a void reason, and check Void payment before saving.");
                    payment.IsVoided = true;
                    dbContext.Update(row);
                }
                else if (row is crm.domain.entities.Customer customer)
                {
                    customer.IsActive = false;
                    dbContext.Update(row);
                }
                else dbContext.Remove(row);
                dbContext.SaveChanges();
                tx.Commit();
            });
            AfterSave(recordName + (customerRecord ? " deactivated; history retained." : " deleted."));
        });

        private void AfterSave(string message)
        {
            NewRecord();
            try { LoadRecords(); result.Text = message; }
            catch (Exception) { result.Text = message + " The list could not reload. Click Refresh List; the change is already saved."; }
        }

        private void Run(Action action)
        {
            try { action(); }
            catch (DbUpdateConcurrencyException) { result.Text = "This record changed or was deleted. Refresh the list and try again."; }
            catch (DbUpdateException) { result.Text = "Could not save the change. Check references and linked records. Delete related tasks/payments before deleting a job order."; }
            catch (Exception ex)
            {
                result.Text = ex is InvalidOperationException || ex is ArgumentException
                    ? ex.Message : "Could not access the database. Check the connection and migrations, then click Refresh List.";
                if (ex is InvalidOperationException && result?.FindForm() is Form form && (form is TransactionDialogForm || form.Modal))
                {
                    MessageBox.Show(form, ex.Message, "RevCare Auto Hub", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}

