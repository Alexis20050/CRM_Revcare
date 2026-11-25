# Entry windows

Job Orders, Job Tasks, Payments, and Customers have an Add button and an Edit Selected button above the list. Double-clicking a row also opens its editor.

Save uses the existing validation and EF persistence logic. Successful saves close the window and reload the list. Validation or database failures keep it open with an error message. Cancel, Escape, and the close button discard unsaved input.

The transaction fields remain in each page's Designer file: JobOrderPage, JobTaskPage, PaymentPage, and CustomerPage. The shared window frame is TransactionDialogForm.cs (View Designer). At runtime it temporarily hosts the page's editor panel, then returns the controls when it closes. This retains the same field bindings and avoids duplicate save logic.

Customer deactivation and list refresh stay on the main Customer page. The other sections retain their existing delete and refresh controls.

Verification: tests/Dialogs.Smoke uses a disposable LocalDB database to exercise all four Add flows, validation, payment editing, cancellation, and reopening. It does not modify the cloud database.
