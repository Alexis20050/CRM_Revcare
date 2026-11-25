# Transaction protection

- Low-rating escalation is owned by the shared domain rules. The extra dialog-created complaint was removed.
- Payments are voided through Edit Payment using the Void checkbox and a required reason. Voided rows remain in history and are excluded from totals. Released jobs cannot be made unpaid. Voided payments cannot be edited.
- SQL rowversion values protect transaction/customer records. Desktop editors retain the version opened by the user. API PUT requests must return the RowVersion received when reading the record.
- Payment submissions have unique request IDs, retained across retries. The shared controller retries with fresh contexts and recognizes an already-committed payment.
- SaveChangesAsync uses asynchronous database saving/commit. Existing synchronous domain-validation reads run on a worker during that path; they are not yet rewritten as individual asynchronous queries. Desktop save buttons await writes and prevent duplicate clicks.
- AuditEntries records domain changes with actor and UTC timestamp in the same transaction. Desktop activity is attributed to the sole admin login; API operations without an authenticated identity are labelled unattributed application.
- Completion and warranty calculations use the workshop's fixed Manila civil time for compatibility with existing StartedAt/CompletedAt records. Start dates are protected after completion. New void/audit timestamps use UTC.
- The payment editor displays estimate, total paid, outstanding balance, and 50% deposit.

TransactionProtection was applied to MonsterASP db80735 with existing record counts preserved. The business-rule test suite includes stale edits, void balances and audit persistence.
