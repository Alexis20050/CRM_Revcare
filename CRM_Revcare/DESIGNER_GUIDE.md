# Edit the UI in Visual Studio

Build the solution, then right-click a screen's main .cs file and choose **View Designer** (Shift+F7). Drag controls from the Toolbox; press F4 to edit properties.

- **AdminLoginForm.cs**: login screen.
- **CrmTransactionsForm.cs**: header, sidebar, dividers, page container, refresh and logout bar.
- **Views/CustomerPage.cs**: customer registry, search, edit panel, deactivation.
- **Views/InquiryPage.cs**
- **Views/ComplaintPage.cs**
- **Views/FeedbackPage.cs**
- **Views/JobOrderPage.cs**
- **Views/JobTaskPage.cs**
- **Views/PaymentPage.cs**

Open each page separately to move its labels, inputs, buttons, and grid. Select a grid and choose **Edit Columns** to change captions, widths, visibility, and formatting. Docked containers resize through Dock, Size, Padding, and Anchor properties. Document Outline helps select nested controls.

Visual Studio saves layout in each screen's .Designer.cs and .resx. Edit through the Designer rather than manually editing those files. Keep control names and column DataPropertyName values intact because bindings use them. Adding a new data field also requires model and save/load code.

CrmCollectionController.cs handles CRUD; the main form's code-behind binds fields and handles navigation and refresh. Opening a Designer does not query the database.

## Transaction references

Tasks, payments, feedback, and complaints select a job from a Designer-owned ComboBox. Captions show the job number, customer/vehicle reference, and status; only the integer JobOrderId is saved. Tasks, payments, and feedback require a selection. Complaints may use "No job order" for a general complaint.

Opening a job dropdown reloads its choices. Job-list refreshes and cloud refreshes also update the choices while retaining the selected ID and unsaved inputs. Deleted or missing references remain visibly unavailable rather than silently changing to another job. Saving checks that the selected job still exists in the same database transaction. Linked complaints and feedback prevent job deletion through this application; tasks and payments also have database foreign keys. Complaint/feedback protection is application-level, so direct SQL writes are not covered by those checks.

Primary IDs remain read-only and database-generated. With the customer integration, Customers is added as a supporting section with auto-generated CustomerId, FullName, ContactNumber, Email, IsActive, and CreatedAt. Customer records are deactivated rather than deleted to preserve historical links. Job Orders require an existing active customer selection. Inquiries may link to an existing customer or allow unregistered entry (with customer details displayed read-only). Job Tasks, Payments, and Feedbacks derive their customer from the selected Job Order. Complaints derive their customer from the linked Job Order or allow selecting a customer directly for general complaints. Former unverified external customer IDs on Complaints and Feedbacks are preserved as LegacyCustomerId, with foreign key constraints added to verified CustomerId links. Historical records are never automatically matched or rewritten.

## Validation

The application builds in Release. CRUD, filters, login, and automatic refresh were checked using temporary LocalDB databases. The smoke-test executables accept --current-model to build these disposable schemas from the current model.

The missing CreatedAt columns were subsequently resolved by migration 20261118000100_AddCoreTransactionCreatedAt, applied to MonsterASP. Existing records received the UTC migration timestamp because their original creation times were unknown. The default migration-based core transaction checks now pass, and the EF snapshot matches the current model.
