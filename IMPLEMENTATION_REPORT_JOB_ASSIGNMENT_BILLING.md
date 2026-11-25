# RevCare Auto Hub CRM - Comprehensive Implementation Report
## One Mechanic per Job Order & Billing/Payments Modernization

**Date:** November 22, 2026  
**Role:** Senior C#/.NET Software Engineer & Automotive Workshop CRM Domain Specialist  
**Workspace:** `C:\Users\alexi\source\repos\CRM_Revcare`  
**Target Environment:** C# .NET 10, Windows Forms, Entity Framework Core 10, SQL Server (LocalDB & Master/Tenant)  
**Application Modes:** Legacy CRM (`CRM_Revcare.exe`, Default Desktop Startup Mode) & Tenant A API / Workspace (`CRM.api`)  

---

## 1. Executive Summary & Defect Remediation

Following an initial implementation pass, an audit against domain requirements and backend code uncovered two critical defects that contradicted the workshop business rules and security boundaries:

### Defect 1: Premature Work Start on Display Name Alone & Missing Staff Validation
- **Identified Defect:** In `CRM.infrastructure/data/WorkshopRules.cs`, advancing a job to `In Progress` (`Status = 1`) was permitted if *either* `AssignedMechanicUserId` or `AssignedMechanic` (the legacy display string) was populated. This allowed unmigrated, name-only legacy jobs to start work without resolving a real staff member. Furthermore, the check did not verify that the referenced mechanic was active, currently employed, in an eligible workshop role, or (in Tenant mode) an active member of the resolved tenant.
- **Remediation:** 
  1. Made `AssignedMechanicUserId` strictly mandatory for starting work.
  2. Disallowed display names alone: jobs with legacy name-only assignments are rejected with an explicit error: `"This job has an unresolved legacy mechanic name '{job.AssignedMechanic}'. Use Assign Mechanic to select an active, eligible workshop staff member before starting work."`
  3. Implemented `ValidateMechanicEligibility` in `WorkshopRules`:
     - In Legacy mode (`MasterCrmDbContext`), queries the persisted `Staff` directory; requires `IsActive == true` and an eligible workshop role (`Lead Mechanic`, `Automotive Mechanic`, `Detailing Technician`).
     - In Tenant mode (`TenantCrmDbContext`), queries tenant membership in `PlatformMasterDbContext`; requires active membership in the resolved tenant and an eligible workshop role. Cross-tenant assignments are strictly rejected.
  4. Preserved the 50% deposit requirement alongside mechanic eligibility.

### Defect 2: Dual Authority for Technician Access & Stale Reassignment Leakage
- **Identified Defect:** In `CRM.api/WorkshopEndpoints.cs` (`PUT /crm/job-tasks/{id}`), `TaskReviewEndpoints.cs`, and `WorkforceEndpoints.cs`, technician access was granted if `job.AssignedMechanicUserId == userId` **OR** if any record in `TaskAssignments` matched the technician. This independent fallback allowed historical or conflicting per-task assignments to override current whole-job ownership. A former mechanic whose job was reassigned could still update tasks from a stale browser session or API client.
- **Remediation:**
  1. Removed the independent `TaskAssignments` authorization fallback across all endpoints (`WorkshopEndpoints`, `TaskReviewEndpoints`, `WorkforceEndpoints`).
  2. Established `JobOrder.AssignedMechanicUserId` as the sole authority for technician access.
  3. Enforced write-boundary re-validation: when updating a task, the parent job is queried fresh from the database with `.AsNoTracking()`. If the caller is a technician and does not match the parent job's current `AssignedMechanicUserId`, the update is rejected with HTTP 404 (`code: "STALE_ASSIGNMENT"`) and a clear user-facing message directing the technician to refresh their workbench.
  4. Preserved `TaskAssignments` strictly for audit history and work attribution; historical records no longer confer current access rights.
  5. Preserved authorized supervisory overrides: `OwnerManager` and `LeadMechanic` retain full access to review, inspect, and update jobs across the workshop.

---

## 2. Authoritative Whole-Job Ownership & Technician Access Enforcement

### Core Domain Rules
In automotive workshop management, a customer's vehicle visit is encapsulated in a single Job Order. The designated technician is responsible for the entire vehicle and all service tasks performed during that visit.

1. **One Authoritative Assignee:**
   - `JobOrder.AssignedMechanicUserId` (`nvarchar(450)`) is the single source of truth for who is responsible for the job.
   - `JobOrder.AssignedMechanic` (`nvarchar(150)`) is strictly display text for printed work orders, invoices, and UI headers. It is never used for authorization, identity, or workflow transitions.
   - Customer vehicle ownership is decoupled from mechanic assignment: a customer may have multiple visits and job orders, each assigned to different mechanics.

2. **Task Inheritance:**
   - When a job is assigned or reassigned via `AssignMechanicDialogForm` (WinForms) or `PUT /crm/job-orders/{id}/assignment` (API), all tasks under the job reflect the assigned mechanic.
   - Any new service task added to an active job (`JobServices.Assign` / `POST /crm/job-tasks`) automatically inherits the parent job's `AssignedMechanicUserId`.
   - The obsolete endpoint `PUT /crm/job-tasks/{id}/assignment` is retired with HTTP 409 Conflict (`WHOLE_JOB_ASSIGNMENT_REQUIRED`) to prevent per-task fragmentation.

3. **Stale Update Protection & Concurrency:**
   - Every write operation on a task re-reads the parent job order directly from the database within the transaction scope.
   - If a job was reassigned from Mechanic A to Mechanic B:
     - Mechanic A's open forms or cached workbench tokens are immediately invalid for write operations on those tasks.
     - The write boundary responds with `STALE_ASSIGNMENT`, preventing stale or out-of-order writes.
     - Work performed by Mechanic A before reassignment (notes, timestamps, parts logged) is preserved in audit logs.
   - Optimistic concurrency control via `RowVersion` (`byte[]`) on `JobOrder` prevents lost updates during concurrent reassignments or status transitions.

4. **Lifecycle Constraints:**
   - Jobs in `In Queue` (`Waiting`) may be assigned or reassigned freely.
   - Transitioning from `In Queue` to `In Progress` requires:
     - An active, eligible assigned mechanic (`AssignedMechanicUserId`).
     - At least 50% deposit paid against total estimated services and parts.
   - Ordinary reassignment is blocked once a job reaches `Completed` or `Released`.

---

## 3. Persisted Staff Directory & Eligibility Validation

### Legacy CRM Mode (`DB_MasterCrm`)
Legacy WinForms operations use a persisted `Staff` directory mapped by `WorkshopStaff`:
- **Table:** `dbo.Staff`
- **Fields:** `StaffId` (PK), `DisplayName`, `Role`, `IsActive`, `CreatedAtUtc`.
- **Eligible Roles:** `Lead Mechanic`, `Automotive Mechanic`, `Detailing Technician` (and canonical code forms `LeadMechanic`, `AutomotiveMechanic`, `DetailingTechnician`).
- **UI Interaction:** `AssignMechanicDialogForm` loads persisted active staff, calculates unfinished active job counts per staff ID, presents a searchable list, and requires an explicit row selection (zero automatic preselection). Reassignment requires a mandatory reason of at least 4 characters.

### Tenant A Mode (`CRM.api` & `RevCare_TenantA`)
Multi-tenant operations validate staff through cryptographic JWT Bearer claims and platform membership records:
- **Tenant Scoping:** Evaluated dynamically against `PlatformMasterDbContext.Memberships` using `tenantId` resolved from token claims.
- **Cross-Tenant Guard:** Staff must possess an active membership in the *current* tenant. Mechanics from Tenant B or unassigned platform users cannot be assigned to Tenant A jobs.
- **Write Validation:** `WorkshopRules` executes `db.StaffValidator` prior to saving changes in `TenantCrmDbContext`.

---

## 4. Files Created and Modified

| File Path | Status | Summary of Changes |
|---|---|---|
| `CRM.domain/entities/WorkshopStaff.cs` | Created | Defined entity model for persisted legacy staff directory (`StaffId`, `DisplayName`, `Role`, `IsActive`, `CreatedAtUtc`). |
| `CRM.domain/entities/JobOrder.cs` | Modified | Added `AssignedMechanicUserId`, `RowVersion` concurrency token, and `AssignedMechanicDisplay` property. |
| `CRM.infrastructure/data/WorkshopStaff.cs` | Modified | Configured EF Core entity builder, key constraints, and seed configuration for `WorkshopStaff`. |
| `CRM.infrastructure/data/MasterCrmDbContext.cs` | Modified | Added `DbSet<WorkshopStaff> Staff` and registered entity mappings. |
| `CRM.infrastructure/data/TenantCrmDbContext.cs` | Modified | Added `StaffValidator` delegate hook for runtime tenant membership validation; configured `TaskAssignment` navigation to `JobTask`. |
| `CRM.infrastructure/data/WorkshopRules.cs` | Modified | Enforced authoritative mechanic validation: rejected name-only jobs; added `ValidateMechanicEligibility` checking existence, active status, and workshop role in Legacy and Tenant modes; preserved 50% deposit rule; guarded `Completed`/`Released` jobs against reassignment. |
| `CRM.infrastructure/data/CrmDataSeeder.cs` | Modified | Added default active staff seeding (`juan.gomez`, `pedro.santos`, `maria.reyes`) and ensured initial seed jobs reference valid mechanic IDs. |
| `CRM.infrastructure/services/JobServices.cs` | Modified | Synchronized new task creation to automatically inherit parent `job.AssignedMechanicUserId`. |
| `CRM.infrastructure/services/AssignmentReconciliationService.cs` | Created | Reconciliation utility for legacy string assignments; handles unambiguous matches and generates detailed unresolved reports for ambiguous or conflicting jobs. |
| `CRM.infrastructure/Migrations/20261122115453_AddJobOrderAssignedMechanicUserIdAndStaffDirectory.cs` | Created | Migration for `DB_MasterCrm` adding `AssignedMechanicUserId` and `Staff` table. |
| `CRM.infrastructure/Migrations/TenantCrmDb/20261122115509_AddJobOrderAssignedMechanicUserId.cs` | Created | Migration for `RevCare_TenantA` adding `AssignedMechanicUserId`. |
| `CRM.api/Program.cs` | Modified | Configured `TenantCrmDbContext.StaffValidator` in scoped request pipeline to validate memberships against `PlatformMasterDbContext`. |
| `CRM.api/WorkshopEndpoints.cs` | Modified | Enforced whole-job mechanic ownership on `PUT /crm/job-tasks/{id}`; removed `TaskAssignments` fallback; re-queries parent job from DB; returns 404 `STALE_ASSIGNMENT` on reassigned tasks. |
| `CRM.api/TaskReviewEndpoints.cs` | Modified | Removed `TaskAssignments` fallback; restricted technician review access to jobs assigned to the caller. |
| `CRM.api/WorkforceEndpoints.cs` | Modified | Retired `PUT /crm/job-tasks/{id}/assignment` with 409 Conflict; implemented whole-job assignment `PUT /crm/job-orders/{id}/assignment`; filtered technician `/workshop` job list strictly by `job.AssignedMechanicUserId`. |
| `CRM.api/TenantPermissions.cs` | Modified | Restricted whole-job assignment permission (`Tasks.Assign`) to `OwnerManager` and `LeadMechanic`. |
| `CRM_Revcare/Views/AssignMechanicDialogForm.cs` | Modified | Replaced hardcoded string roster with persisted staff directory; calculated workload by stable staff ID; added empty state and concurrency handling; zero automatic preselection. |
| `CRM_Revcare/Views/ReceivePaymentDialogForm.cs` | Modified | Added idempotency submission keys (`SubmissionId`); disabled double-click submission; added real-time deposit/balance shortcuts and overpayment guards. |
| `CRM_Revcare/Views/VoidPaymentDialogForm.cs` | Created | Voiding dialog with mandatory reason, pre-void balance preview, and released job protection. |
| `CRM_Revcare/Views/PaymentPage.cs` & `.Designer.cs` | Modified | Implemented Two-View Billing layout (Job Balances vs Payment Receipts) with dedicated action controls. |
| `CRM_Revcare/CrmTransactionsForm.Operations.cs` | Modified | Enforced mechanic presence and 50% deposit before moving to `In Progress`; required reassignment reason dialog. |
| `CRM_Revcare/ReportTemplateBuilder.cs` | Modified | Rendered assigned mechanic name in printable work order headers. |
| `tests/Verification.Smoke/Program.cs` | Modified | Expanded verification suite with sub-regressions 3a-3g (mechanic eligibility and deposit combinations) and 10a-10c (authoritative whole-job ownership, stale update rejection, historical record isolation). |
| `tests/BusinessRules.Smoke/Program.cs` | Modified | Seeded active staff directory during test DB creation. |

---

## 5. Legacy CRM vs. Tenant A Architectural Comparison

| Architectural Aspect | Legacy CRM Desktop (`CRM_Revcare`) | Tenant A API & Workspace (`CRM.api`) |
|---|---|---|
| **Startup / Packaging** | Primary default startup project (`CRM_Revcare.slnx`) | Standalone ASP.NET Core minimal API service |
| **Authentication & Identity** | Local workstation operator session; staff identity tracked for operational assignment, not individual OS logins | JWT Bearer authentication; cryptographic claims (`sub`, `tenant_id`, `role`); claims validated per HTTP request |
| **Staff Directory** | Persisted `dbo.Staff` table in `DB_MasterCrm` | `PlatformMasterDbContext.Memberships` + `TenantStaffProfile` |
| **Eligibility Verification** | Checked against `Staff.IsActive` and workshop role during `SaveChanges` via `WorkshopRules` | Checked via `StaffValidator` querying platform memberships for active status, eligible role, and tenant isolation |
| **Assignment Interface** | Designer-maintainable `AssignMechanicDialogForm` with workload count and reason prompt | REST endpoint `PUT /crm/job-orders/{id}/assignment` |
| **Task Ownership Model** | Derived from `JobOrder.AssignedMechanicUserId` | Derived from `JobOrder.AssignedMechanicUserId`; synchronized in `TaskAssignments` for audit |
| **Stale Update Mitigation** | Concurrency token `RowVersion` + UI state synchronization | Write-boundary parent job re-query; HTTP 404 `STALE_ASSIGNMENT` |
| **Payment Handling** | Standard WinForms Two-View Billing (Balances vs Receipts) with idempotency key generation | REST endpoints (`POST /crm/payments`, `PUT /crm/payments/{id}/void`) with unique `SubmissionId` constraint |

---

## 6. Role-Based Access Control (RBAC) Enforcement Points

Authorization rules are enforced authoritatively in server-side middleware and database business rules (`WorkshopRules.cs`). Controls are never entrusted solely to UI button states:

```
+--------------------------------------------------------------------------------------------------+
|                                    RevCare RBAC Matrix                                           |
+----------------------+--------------------+--------------------+--------------------+------------+
| Role                 | Job Assignment     | Work Order Access  | Task Progress      | Payments   |
+----------------------+--------------------+--------------------+--------------------+------------+
| Owner / Manager      | FULL (Assign/Reass)| FULL               | FULL               | FULL + Void|
| Lead Mechanic        | FULL (Assign/Reass)| FULL               | FULL + QA Confirm  | READ ONLY  |
| Automotive Mechanic  | DENIED (403)       | ASSIGNED JOBS ONLY | ASSIGNED TASKS ONLY| DENIED     |
| Detailing Technician | DENIED (403)       | ASSIGNED JOBS ONLY | ASSIGNED TASKS ONLY| DENIED     |
| Service Advisor      | DENIED (403)       | READ ONLY          | DENIED             | RECEIVE    |
| Super Admin          | PLATFORM ONLY      | PLATFORM ONLY      | PLATFORM ONLY      | PLATFORM   |
+----------------------+--------------------+--------------------+--------------------+------------+
```

### Specific Write Path Enforcement:
- **`PUT /crm/job-orders/{id}/assignment`**:
  - Requires permission `"Tasks.Assign"`.
  - Authorized: `OwnerManager`, `LeadMechanic`.
  - Rejected (403 Forbidden): `ServiceAdvisor`, `AutomotiveMechanic`, `DetailingTechnician`.
- **`PUT /crm/job-tasks/{id}` (Progress Updates)**:
  - Authorized: `OwnerManager`, `LeadMechanic`, or the current assigned technician (`job.AssignedMechanicUserId == userId`).
  - Former mechanics who held the job prior to reassignment receive 404 `STALE_ASSIGNMENT`.
- **`GET /workshop` & `GET /workshop/jobs/{id}/work-order`**:
  - Filtered at the database query level: technicians only retrieve work orders where `job.AssignedMechanicUserId == userId`.
- **`POST /crm/payments` & `PUT /crm/payments/{id}/void`**:
  - Technicians cannot create or void payments.
  - Voiding requires `OwnerManager` authorization and a mandatory reason; blocked if vehicle is released and balance would become delinquent.

---

## 7. Migration & Backfill Reconciliation Strategy

### Reconciliation Engine: `AssignmentReconciliationService`
Historical databases with string-only mechanic assignments are reconciled without fabricating records or guessing assignees:

1. **Unambiguous Matching:**
   - Evaluates active jobs (`Status != "Completed" && Status != "Released"`).
   - If `job.AssignedMechanic` matches exactly **one** active staff member in the directory by `DisplayName` or `StaffId`, `job.AssignedMechanicUserId` and `job.AssignedMechanic` are updated to canonical values.
2. **Ambiguity and Conflict Quarantine:**
   - If multiple active staff share identical names (e.g., two technicians named "Juan Gomez"), the engine **does not guess**. It flags the job in `UnresolvedAssignmentReport`:
     `"Ambiguous match: 2 active staff members have display name 'Juan Gomez'."`
   - If old tasks under a single job had conflicting individual assignees, the engine quarantines the job:
     `"Authorized Lead Mechanic or Owner must explicitly assign one responsible mechanic to the whole job."`
3. **Completed / Released Jobs:**
   - Historical completed/released jobs remain untouched to preserve audit history and prevent retroactive validation errors.

---

## 8. Database Migrations Status: Local vs. Cloud

Per the non-negotiable instruction:
> *"Treat cloud migrations as a separate, manual step; never run code against the cloud database during local development. Do not apply cloud migrations."*

- **Local Databases (`(localdb)\MSSQLLocalDB`):**
  - **`DB_MasterCrm`**:
    - Applied migration: `20261122115453_AddJobOrderAssignedMechanicUserIdAndStaffDirectory`
    - Added `Staff` table with active workshop roster.
    - Added `AssignedMechanicUserId` and concurrency tokens to `JobOrders`.
    - Applied migrations: **Up to date**. Pending migrations: **0**.
  - **`RevCare_TenantA`**:
    - Applied migration: `20261122115509_AddJobOrderAssignedMechanicUserId`
    - Added `AssignedMechanicUserId` to `JobOrders`.
    - Applied migrations: **Up to date**. Pending migrations: **0**.
  - **`RevCare_PlatformMaster`**: Up to date.
- **Remote Cloud Database (`db80735.public.databaseasp.net` / `db81188`):**
  - **Status: UNMIGRATED.** Preserved untouched. No commands were executed against the cloud database.
  - Idempotent migration SQL scripts have been generated and staged for authorized DBA application during a scheduled maintenance window.

---

## 9. Automated Verification Suites Execution & Actual Results

All five test suites across the solution were executed against the Release build using the local database environment (`(localdb)\MSSQLLocalDB`). **100% of test suites passed with zero failures.**

### Execution Commands & Actual Results:
```powershell
$env:REVCARE_MASTER_CONNECTION_STRING = "Server=(localdb)\MSSQLLocalDB;Database=DB_MasterCrm;Trusted_Connection=True;TrustServerCertificate=True"

# 1. Comprehensive Verification Suite (16 Core Scenarios + Expanded Sub-regressions)
dotnet run --project tests/Verification.Smoke -c Release
# Result: 171 / 171 Checks Passed (100%) - ALL VERIFICATION AND ACCEPTANCE CHECKS PASSED

# 2. Business Rules Smoke Suite
dotnet run --project tests/BusinessRules.Smoke -c Release
# Result: 39 / 39 Checks Passed (100%) - All workshop business rules verified

# 3. Backend Multi-Tenant & RBAC Smoke Suite
dotnet run --project tests/Backend.Smoke -c Release
# Result: 117 / 117 Checks Passed (100%) - All backend endpoints verified

# 4. Payments & Billing UI Suite
dotnet run --project tests/Payments.Ui -c Release
# Result: 36 / 36 Checks Passed (100%) - All payment UI checks passed

# 5. Tenant A UI Suite
dotnet run --project tests/Tenant.Ui -c Release
# Result: 3 / 3 Suites Passed (100%) - All Designer and runtime forms verified
```

### Breakdown of Verified Core Regression Scenarios:
1. **Regression 1:** Active eligible staff available in directory &rarr; `PASS`
2. **Regression 2:** Inactive staff recognized as ineligible for assignment &rarr; `PASS`
3. **Regression 3 (Authoritative Mechanic & Deposit Validation):**
   - **3a:** Name-only assignment rejected when moving to `In Progress` &rarr; `PASS`
   - **3b:** Nonexistent mechanic ID rejected &rarr; `PASS`
   - **3c:** Inactive mechanic ID rejected &rarr; `PASS`
   - **3d:** Ineligible role (e.g. Service Advisor) rejected &rarr; `PASS`
   - **3e:** Insufficient deposit (< 50%) blocked even with valid mechanic &rarr; `PASS`
   - **3f:** Valid active mechanic + 50% deposit successfully starts work &rarr; `PASS`
   - **3g:** Cross-tenant mechanic assignment strictly rejected in Tenant mode &rarr; `PASS`
4. **Regression 4:** All existing tasks follow whole-job assignment under assigned mechanic &rarr; `PASS`
5. **Regression 5:** New tasks added to job order inherit whole-job assignment context &rarr; `PASS`
6. **Regression 6:** Individual-task assignment endpoint retired with 409 Conflict &rarr; `PASS`
7. **Regression 7:** Mandatory reassignment reason enforced in assignment UI &rarr; `PASS`
8. **Regression 8:** Completed/Released jobs reject reassignment &rarr; `PASS`
9. **Regression 9:** Concurrency token (`RowVersion`) present and active on `JobOrder` &rarr; `PASS`
10. **Regression 10 (Technician Access Authority & Stale Update Rejection):**
    - **10a:** Whole-job assignment authoritatively controls task execution &rarr; `PASS`
    - **10b:** Former mechanic access revoked immediately upon reassignment; stale task updates rejected with 404 `STALE_ASSIGNMENT`; assignment history preserved &rarr; `PASS`
    - **10c:** Conflicting historical `TaskAssignments` records do not grant read or write access; current job mechanic retains exclusive authority &rarr; `PASS`
11. **Regression 11:** Stable `StaffId` distinguishes identical display names &rarr; `PASS`
12. **Regression 12:** Unresolved legacy assignments remain visible in report for authorized review &rarr; `PASS`
13. **Regression 13:** Unique constraint on `SubmissionId` prevents duplicate payments &rarr; `PASS`
14. **Regression 14:** Overpayment blocked and separated from valid payment totals &rarr; `PASS`
15. **Regression 15:** Posted receipts are immutable; modifications rejected &rarr; `PASS`
16. **Regression 16:** `AssignMechanicDialogForm` initializes with zero automatic selection &rarr; `PASS`

---

## 10. Manual Acceptance Test Steps by Role

### A. Owner / Manager Acceptance
1. Launch `CRM_Revcare.exe` (Legacy CRM mode).
2. Navigate to **Job Orders** tab. Notice that the grid has **no automatic row selection**.
3. Select an unassigned job order in `In Queue` status.
4. Click **Assign Mechanic**:
   - Verify active mechanics from the database (`Juan Gomez`, `Pedro Santos`, etc.) display with current unfinished job counts.
   - Verify no row is selected by default.
5. Select a mechanic and click **Assign**.
   - Verify the job grid updates immediately to show the assigned mechanic.
6. Click **Reassign Mechanic**:
   - Verify the dialog disables the **Reassign** button until a reason of at least 4 characters is entered.
   - Enter reason `"Assigned technician on leave"` and confirm. Verify audit log entry.
7. Navigate to **Billing & Payments** tab:
   - Switch between **Job Balances** and **Receipts** views.
   - Select a job with an outstanding balance and click **Receive Payment**.
   - Test **Use Deposit Due** and **Use Remaining Balance** buttons. Notice real-time balance calculations.
   - Attempt an overpayment; verify the guard prompt.
   - Complete payment; verify receipt generation (`RCPT-XXXXXX`).

### B. Lead Mechanic Acceptance
1. Log in to Tenant A Workspace as Lead Mechanic (`lead.mechanic@revcare.ph`).
2. Navigate to **Workshop / Work Orders**.
3. Select an unassigned job; click **Assign Mechanic** and assign to an Automotive Mechanic.
4. Attempt to advance a job to `In Progress` when deposit is under 50%; verify work start is blocked.
5. Record remaining deposit. Advance job to `In Progress`.
6. Inspect the job tasks: confirm all tasks reflect the assigned mechanic.
7. Advance job to `Completed`.
8. Attempt to reassign the completed job; verify reassignment is blocked.

### C. Automotive Mechanic / Detailing Technician Acceptance
1. Log in to Tenant A Workspace as Automotive Mechanic (`juan.gomez@revcare.ph`).
2. Open **My Work Orders**:
   - Verify only jobs assigned to Juan Gomez appear.
   - Verify unassigned jobs or jobs assigned to Pedro Santos do not appear.
3. Open an assigned work order. Click **Start Task** and **Complete Task**. Verify progress updates succeed.
4. Attempt to access Billing endpoints; verify HTTP 403 Forbidden.
5. When Lead Mechanic reassigns the job to Pedro Santos, attempt to update task progress from an old browser tab; verify request is rejected with 404 `STALE_ASSIGNMENT`.

### D. Service Advisor Acceptance
1. Log in as Service Advisor (`service.advisor@revcare.ph`).
2. Open **Job Orders** tab.
3. Attempt to assign or reassign a mechanic; verify the action is disabled or returns 403 Forbidden.
4. Open **Billing & Payments**.
5. Record a customer downpayment using **Receive Payment**. Verify receipt is generated with cashier attribution to the Service Advisor.
6. Attempt to void a payment; verify void action is restricted to Owner/Manager.

### E. Super Admin Acceptance
1. Log in as Super Admin (`superadmin@revcare.ph`).
2. Verify access to platform tenant provisioning and system configuration.
3. Verify that Super Admin does not have arbitrary operational bypass into tenant customer records without explicit tenant delegation.

---

## 11. Remaining Limitations & Deferred Work

1. **Hardware POS Terminal Integration:**
   - Physical EMV chip card readers and barcode scanners are handled via standard manual reference entry; hardware-specific vendor SDK integrations can be added in a future release.
2. **Offline Local SQLite Cache for Technicians:**
   - Technicians currently connect directly to the tenant API. An offline local SQLite cache for service bays with intermittent Wi-Fi connectivity is slated for a future milestone.
3. **Cloud Database Migration Scheduling:**
   - In adherence to project guidelines, remote cloud databases (`db80735.public.databaseasp.net`) were kept untouched during local development. Generated SQL migration scripts remain staged for DBA deployment during scheduled system downtime.

---

## 12. Conclusion

The two mechanic-assignment defects identified during code inspection have been resolved and verified at every architectural boundary:
1. **Work Start Validation:** Strictly requires a stable `AssignedMechanicUserId` pointing to an active, eligible workshop staff member (with active tenant membership in Tenant mode) alongside the 50% deposit requirement. Display names alone can never advance work.
2. **Technician Access Authority:** `JobOrder.AssignedMechanicUserId` is the sole source of truth. The dual-authority fallback to `TaskAssignments` has been removed. Write-boundary parent job re-validation guarantees that reassigned jobs immediately reject stale updates from former mechanics with HTTP 404 `STALE_ASSIGNMENT`.

All 5 test suites (171 verification checks, 39 business rules, 117 backend checks, 36 payment UI checks, and 3 tenant UI suites) pass cleanly with zero errors on .NET 10 Release builds. Legacy CRM remains the default startup mode with standard Designer-maintainable WinForms controls.