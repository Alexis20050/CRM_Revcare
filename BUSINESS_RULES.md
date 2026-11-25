# Workshop business rules

Job progression is In Queue → In Progress → QA Checking → Completed → Released. New jobs start in queue. Skipping or reversing stages is rejected.

- Enter the estimated cost before accepting payments or progressing work. Starting work requires 50% paid, rounded to cents away from zero. A genuinely zero-cost job requires no deposit.
- Finish every assigned task before completion. Completion records the current local time, matching the existing StartedAt picker. Lead time is elapsed hours from StartedAt to CompletedAt. Historical completion dates are not fabricated.
- Release requires a zero outstanding balance. Payment changes cannot make a released job unpaid.
- Total Paid sums payments. Outstanding Balance is max(estimate minus paid, zero). Customer Credit shows excess paid; the payment editor requires explicit permission for overpayment. Credit is tracked against that job; cross-job credit redemption is not implemented.
- Convert an inquiry from its list action. Select a customer, or create one from complete inquiry details when there is no possible existing match. Vehicle details and inquiry notes are copied into a queued job. Conversion is transactional and repeat calls return the existing job.
- Active feedback rated 1 or 2 creates one urgent Open complaint per feedback record. Repeated edits do not create duplicate tickets.
- New complaints within 30 elapsed days of recorded completion are flagged Under Warranty Service. Unknown historical completion dates are not treated as warranty eligibility. Flags are visible in the complaints grid.
- Deactivate customers instead of deleting them. Existing history remains accessible; inactive customers cannot receive new or reassigned jobs. Jobs with payments or tasks cannot be deleted, and foreign keys preserve linked records.

Rules run through MasterCrmDbContext SaveChanges and SaveChangesAsync, shared by desktop and API. SQL constraints protect basic amounts and references; aggregate workflow rules require these application save paths (direct SQL can bypass application validation). Parent locking serializes relevant payment and lifecycle writes. Conversion is also available at POST /crm/inquiries/{id}/convert with CustomerId and VehicleDetails.

Migration WorkshopBusinessRules preserves unknown estimates and completion dates as NULL, adopts previously added complaint/feedback job constraints, and refuses automatic destructive rollback. Invalid existing references or nonpositive payments must be reviewed before migration.

Verification: tests/BusinessRules.Smoke creates and deletes an isolated LocalDB database. tools/Workshop.Database performs migration preflight and optional --apply against the desktop's configured connection without printing credentials.

The migration was applied to LocalDB DB_MasterCrm, then to MonsterASP db80735 on explicit request. The cloud Customer and WorkshopBusinessRules migrations completed with transaction counts preserved and entity reads verified. Cloud customer records start empty; existing customer references are archived for manual review. The desktop's default connection remains LocalDB unless explicitly configured otherwise.
