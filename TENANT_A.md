# Tenant A operation

Run `./Start-TenantA.ps1` from PowerShell at the repository root. This builds and starts a local API process (hidden window) and the Tenant A desktop. Closing the desktop stops that API process. SQL connections remain encrypted and the API loads the Master/Tenant A settings from local .NET user secrets. No database password is sent to the desktop.

The desktop now defaults to API mode. For Visual Studio, run CRM.api's HTTPS profile and the desktop together; the default URL is `https://localhost:7105`. `REVCARE_API_BASE_URL` overrides it. Non-loopback API addresses require HTTPS. The local launcher uses loopback HTTP only; hosting a shared HTTPS API is a separate deployment task.

Sign in as `owner` with the configured application password. The tenant login intentionally does not accept the platform-only `superadmin` account. Create named staff accounts through **Staff**, choosing ServiceAdvisor, LeadMechanic, AutomotiveMechanic, DetailingTechnician or OwnerManager. Assign tasks through **Job Tasks → Assign task**. Technicians see **My Assigned Tasks** only. Newly imported tasks have no assignee until assigned by an Owner or Lead Mechanic.

## Permission boundaries

- Owner/Manager: all Tenant A operations, staff administration, service catalog, audit history and payment corrections/voids.
- Service Advisor: customer records, inquiries/complaints/feedback, job creation, starting/releasing eligible jobs and recording payments. Cannot assign tasks, perform QA, manage staff or void/correct payments.
- Lead Mechanic: workshop view, assignment, task progress, starting eligible jobs and QA/completion. Cannot access customer contact lists or billing.
- Automotive Mechanic and Detailing Technician: only assigned workshop details and task updates.
- Super Admin: platform metadata and administration only. No implicit tenant business access.

The API validates active membership and role on every tenant request. UI visibility is a convenience, not the authorization boundary. Role changes invalidate older role claims on subsequent requests; users must sign in again. A token expires after 30 minutes. Password hashes live only in Master. Service/assignment data lives in Tenant A. Unfinished tasks must be reassigned before disabling staff or changing their role. Staff cannot change their own access.

## Screens and controls

`TenantWorkspaceForm.Designer.cs` owns the sidebar, job/task grids, search and action toolbar. Each module has a dedicated `Tenant*Form.Designer.cs` with standard controls editable in Visual Studio Designer. `TenantEntryForm` supplies shared binding, validation and save handling. No custom drawing is used. Lookup fields select existing records rather than accepting typed foreign keys. Existing records retain rowversions for conflict detection.

Job services create pending tasks and snapshot the selected service name and reference price. Later catalog changes do not change those snapshots or the approved job estimate. Historical tasks without snapshots retain unknown prices rather than inventing historical values. The Owner must enter actual catalog prices; the initial catalog prices are zero.

## Workshop and billing workflow

1. Service Advisor or Owner creates a job with customer, vehicle, estimate and requested services.
2. Record the required 50% deposit, then move the job to In Progress.
3. Owner or Lead Mechanic assigns tasks. Mechanics and detailing technicians see only their assigned work, grouped by job.
4. The technician starts a task, records notes, marks it Done, then submits it for review.
5. When all tasks are submitted or approved, Owner or Lead Mechanic moves the job to QA Checking.
6. Owner or Lead Mechanic approves each task or requests rework with a mandatory reason. Rework returns the task to Ongoing; it must be completed and submitted again.
7. Completion requires every task approved. Release additionally requires no unpaid balance.

Review actions preserve reviewer, timestamp and history. Submitted/approved tasks cannot be silently edited through the ordinary task editor. Historical completed jobs retain their original state; the migration does not fabricate past reviews or timestamps.

The workshop QA queue filters review-ready work. Selecting a job narrows the task list without rebinding the job grid. Mechanics have no QA, billing or staff-administration buttons, and the API enforces the same restrictions.

Payment entry shows estimated cost, total paid, balance and credit. New payments offer buttons to fill the remaining required deposit or outstanding balance. Existing receipt edits do not use these shortcuts. Payment corrections and voids remain Owner-only; voiding preserves history.

## Data transition and rollback

`tools/TenantA.Import` is restricted to `db80735` → `db81189`. Without `--apply` it performs a read-only preflight. With `--apply`, it uses a transaction, preserves business identities and relationships, copies historical audit entries with newly generated audit IDs, and compares every persisted imported value (except generated rowversions). It refuses nonempty destination business tables; rerunning cannot duplicate or replace transactions.

The legacy source is retained unchanged. Use Tenant A for new transactions after cutover; do not continue writing to both copies. The old desktop is available only with the explicit `--legacy` argument for controlled recovery. After any new Tenant A transactions, rollback requires reconciling those changes; switching back blindly would lose their visibility.

Tenant B/C, branching, business intelligence, offline synchronization and external API hosting are not enabled by this release. Avoid running the old synchronization flow against the new databases.

## Verification

- `dotnet run --project tests/Backend.Smoke -c Release`: disposable LocalDB databases; real API authorization, assignments, financial rules and concurrency.
- `dotnet run --project tests/Tenant.Ui -c Release`: fake API; technician menus, sorted record selection and responsive layout.
- `dotnet run --project tests/BusinessRules.Smoke -c Release`: shared workshop rule regressions.

Cloud setup still uses the explicit API `--initialize` command. Normal login does not seed or migrate databases.
