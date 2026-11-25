# Payments

Choose Receive Payment, select the job/customer, and read the bill summary before entering the amount actually received. Selecting a job does not automatically enter an amount.

Use deposit due fills the amount still needed to reach the required 50% deposit. Pay remaining balance fills the unpaid bill. Both subtract valid existing payments. The preview shows the balance after this payment or the excess credit; overpayment requires explicitly enabling customer credit.

Customer payment automatically records Downpayment, Progress Payment, or Final Balance based on existing payments and the amount received. Customer Deductible and Insurer Share remain explicit categories.

View / Correct edits an existing receipt and excludes that receipt from the other-payment total. The job cannot be reassigned during correction.

Void Selected Payment opens a separate void workflow: financial fields are locked, a reason is required, and confirmation is requested. Voided receipts remain visible with their reason, cannot be edited, and do not count toward the amount paid.

The displayed totals come from the job lookup snapshot. The database revalidates the current balance when saving. Refresh Cloud to reload changes made by other users.

Controls remain in PaymentPage.Designer.cs and the shared TransactionDialogForm Designer. No schema migration was needed.
