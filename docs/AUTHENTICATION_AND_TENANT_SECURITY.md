# RevCare CRM — Phase 3: Backend Authentication, Role-Based Authorization, and Secure Company Routing

**Document Version:** 1.0.0 (Phase 3 Architectural Specification & Verification Report)  
**Date:** November 2026  
**Author:** Senior Software Architect, Security Engineer, and Multi-Tenant SaaS Specialist  
**Solution:** RevCare Auto Hub CRM (.NET 10, ASP.NET Core Web API, EF Core, SQL Server LocalDB, Windows Forms)  
**Workspace:** `C:\Users\alexi\source\repos\CRM_Revcare`  
**Target Environment:** Local Development `(localdb)\MSSQLLocalDB`

---

## 1. Executive Summary & Target Security Architecture

Phase 3 establishes a secure, enterprise-grade authentication and authorization subsystem for RevCare Auto Hub CRM across its four-project architecture:
- `CRM.domain`
- `CRM.infrastructure`
- `CRM.api`
- `CRM_Revcare` (Windows Forms desktop client)

### Architectural Principles:
1. **Centralized Platform Identity:** User credentials, password policies, lockouts, and multi-company memberships are managed centrally in `RevCare_PlatformMaster` via ASP.NET Core Identity.
2. **Company-Scoped Database Routing:** All company operational records reside in independent physical databases. The API layer dynamically resolves and routes requests to the authenticated company database based on cryptographically signed bearer token claims.
3. **Database-Per-Company Data Isolation:** Operational records (customers, vehicles, job orders, tasks, receipts, refunds, inquiries, appointments) are strictly segregated by physical database boundaries.
4. **Single-Database Multi-Branch Scoping for Tenant C:** Multi-branch operations (Tenant C) exist within a single company database, partitioned by relational branch ownership (`BranchId`).
5. **Technical vs. Operational Isolation:** Platform Super Administrators possess administrative authority over platform metadata (tenants, modules, subscription plans, platform audits) but are strictly prohibited from querying or modifying company operational, workshop, or financial records.

```
+----------------------------------------------------------------------------------------------------+
|                                    CENTRALIZED PLATFORM ARCHITECTURE                               |
+----------------------------------------------------------------------------------------------------+
|                                      RevCare_PlatformMaster                                        |
|  - Identity Users & Credentials        - Tenants Registry (tenant-a, tenant-b, tenant-c)           |
|  - Subscription Plans (Plan A, B, C)   - Module Entitlements & Licensing                           |
|  - User Memberships (Roles & Branches) - Platform Audit Log                                        |
+----------------------------------------------------------------------------------------------------+
                                                  │
                 ┌────────────────────────────────┼────────────────────────────────┐
                 ▼                                ▼                                ▼
+--------------------------------+ +--------------------------------+ +--------------------------------+
|          Company A             | |          Company B             | |          Company C             |
|        (DB_MasterCrm)          | |       (RevCare_TenantB)        | |       (RevCare_TenantC)        |
| - Plan A (Core Workshop)       | | - Plan B (Professional + BI)   | | - Plan C (Multi-Branch + BI)   |
| - Single Central Branch        | | - Single Central Branch        | | - Branches: MAIN, NORTH, SOUTH |
| - 220+ Production Job Orders   | | - Isolated Company DB          | | - Branch-Scoped Roles          |
+--------------------------------+ +--------------------------------+ +--------------------------------+
```

---

## 2. Specialized Workshop Roles & Permissions Matrix

RevCare CRM defines six specialized operational roles alongside technical platform administration:

| Role | Domain / Purpose | Key Permissions | Explicit Prohibitions |
| :--- | :--- | :--- | :--- |
| **`SuperAdmin`** | Technical platform management | `MasterAdmin` policy: Tenant registration, module licensing, plan provisioning, platform audit review | **Zero access** to `/crm/*` endpoints. Cannot read or write workshop, customer, or financial records. |
| **`OwnerManager`** | Executive workshop & company management | Full company access: `Customers.Manage`, `Collection.Manage`, `Jobs.Manage`, `Jobs.Delete`, `Jobs.Start`, `Jobs.QA`, `Jobs.Release`, `Tasks.Read`, `Tasks.Update`, `Tasks.Assign`, `Workshop.Read`, `Payments.Read`, `Payments.Record`, `Payments.Correct`, `Staff.Manage`, `Services.Manage`, `Services.Read`, `Audit.Read` | Unrestricted company-wide scope across all branches. Cannot access platform master administration without `SuperAdmin` role. |
| **`BranchManager`** | Autonomous branch operations & QA management | Branch-scoped: `Customers.Manage`, `Collection.Manage`, `Jobs.Manage`, `Jobs.Start`, `Jobs.QA`, `Jobs.Release`, `Tasks.Read`, `Tasks.Update`, `Tasks.Assign`, `Workshop.Read`, `Payments.Read`, `Payments.Record`, `Services.Read`, `Audit.Read` | **Cannot delete jobs** (`Jobs.Delete`), **cannot void/correct payments** (`Payments.Correct`), **cannot modify staff accounts** (`Staff.Manage`), **cannot alter service prices** (`Services.Manage`). Locked to assigned branch. |
| **`ServiceAdvisor`** | Customer intake, job opening, invoicing | Customer & intake: `Customers.Manage`, `Collection.Manage`, `Jobs.Manage`, `Jobs.Start`, `Jobs.Release`, `Tasks.Read`, `Workshop.Read`, `Payments.Read`, `Payments.Record`, `Services.Read` | **Cannot QA inspection** (`Jobs.QA`), **cannot assign mechanics** (`Tasks.Assign`), **cannot delete jobs**, **cannot void payments**. Locked to assigned branch. |
| **`LeadMechanic`** | Workshop supervision, whole-job assignment, QA inspection | Technical supervision: `Workshop.Read`, `Tasks.Read`, `Tasks.Update`, `Tasks.Assign`, `Jobs.Start`, `Jobs.QA`, `Services.Read` | **Cannot create or void payments**, **cannot manage customers or leads**, **cannot delete jobs**. Qualified to supervise and inspect all job categories (Mechanical, Detailing, Mixed). |
| **`AutomotiveMechanic`** | Mechanical repairs & maintenance execution | Assigned execution: `Workshop.Read`, `Tasks.UpdateAssigned`, `Services.Read` | **Cannot view or update tasks assigned to other mechanics**. Cannot record payments, cannot delete jobs, cannot inspect or approve QA. Qualified for Mechanical jobs only. |
| **`DetailingTechnician`** | Detailing, wash, and surface care execution | Assigned execution: `Workshop.Read`, `Tasks.UpdateAssigned`, `Services.Read` | **Cannot view or update tasks assigned to other mechanics**. Cannot record payments, cannot delete jobs. Qualified for Detailing jobs only. Prohibited from Mechanical jobs. |

---

## 3. Super Admin Technical Administration & Isolation

1. **Policy Separation:**
   - Platform administration endpoints (`/master/*`) enforce the `MasterAdmin` policy.
   - Company operational endpoints (`/crm/*`) enforce the `TenantOperations` policy.
2. **Access Handler Verification (`BackendAccessHandler`):**
   - When resolving the `MasterAdmin` policy, the handler requires `tenant_id == null` and verified membership in the `SuperAdmin` role.
   - When evaluating `TenantOperations`, the handler explicitly verifies active membership in the target tenant and rejects any caller without a valid tenant role claim.
3. **HTTP 403 Enforcement:**
   - Any token issued to `superadmin` calling `/crm/customers`, `/crm/job-orders`, `/crm/payments`, or `/crm/workshop` immediately returns `403 Forbidden`.
   - `superadmin` cannot bypass this restriction by passing client-supplied headers or query strings. Access to company data requires an explicit, active `TenantMembership` in that specific company.

---

## 4. Company Data Isolation (Tenant A, Tenant B, Tenant C)

Operational records are completely isolated by physical database segregation:
- **Tenant A:** Connected to `DB_MasterCrm` (or `RevCare_TenantA` prototype during isolated tests).
- **Tenant B:** Connected to `RevCare_TenantB`.
- **Tenant C:** Connected to `RevCare_TenantC`.

### Isolation Mechanisms:
1. **Dynamic Connection Resolution:** `TenantDatabaseResolver` queries `PlatformMasterDbContext` for the authorized tenant’s connection key and configuration string.
2. **Identity Verification:** On every database context creation, `TenantDbContextFactory` queries `db.DatabaseIdentity.AnyAsync(x => x.Id == 1 && x.TenantId == authorizedTenantId)`. If the database identity marker does not match, execution halts with an `InvalidOperationException`.
3. **Cross-Company Query Protection:** A bearer token containing `tenant_id: "tenant-a"` cannot retrieve, modify, or link records in `tenant-b` or `tenant-c`. Cross-tenant API calls are rejected with `403 Forbidden` or `404 Not Found`.

---

## 5. Branch Authorization & Data Isolation (Tenant C Multi-Branch)

For enterprise multi-branch companies (Tenant C), multiple physical branches (`MAIN`, `NORTH`, `SOUTH`) operate within a single company database.

### Implementation:
1. **Branch-Scoped Role Encoding:** In `RevCare_PlatformMaster.dbo.Memberships`, staff roles support branch assignment strings:
   - `BranchManager:NORTH`
   - `ServiceAdvisor:NORTH`
   - `AutomotiveMechanic:NORTH`
   - `BranchManager:SOUTH`
   - `OwnerManager` (Central / Multi-Branch)
2. **Role & Branch Parsing:**
   - `TenantPermissions.ExtractBaseRole(role)` parses `"BranchManager"` from `"BranchManager:NORTH"`.
   - `TenantPermissions.ExtractBranchCode(role)` parses `"NORTH"` from `"BranchManager:NORTH"`.
3. **Claim Emission on Authentication:** `/auth/login` resolves the branch code, queries the tenant database's `Branches` table, and emits `branch_code` and `branch_id` claims into the authenticated bearer token.
4. **Server-Side Enforcement (Anti-Tampering):**
   - `crm.MapGet("/branches")`: Returns only the assigned branch for branch-scoped users; returns all branches for company `OwnerManager`.
   - `crm.MapGet("/job-orders")`: Automatically filters by `j.BranchId == userBranchId`.
   - `crm.MapGet("/job-orders/{id}")`: Returns `404 Not Found` if the job order belongs to another branch.
   - `crm.MapPost("/job-orders")`: Overrides any client-provided `BranchId` with the user's verified `userBranchId` claim.
   - `crm.MapPut("/job-orders/{id}")`: Verifies `job.BranchId == userBranchId` before allowing modifications.
   - `crm.MapDelete("/job-orders/{id}")`: Verifies branch ownership and role before processing.
   - `crm.MapGet("/payments")`: Filters payments by `p.BranchId == userBranchId`.
   - `crm.MapPost("/payments")`: Verifies parent `JobOrder.BranchId == userBranchId` and forces payment `BranchId` to match.
   - `crm.MapGet("/workshop")`: Filters workshop tasks by `job.BranchId == userBranchId`.
   - `crm.MapGet("/workshop/jobs/{id}/work-order")`: Verifies branch ownership before rendering the work order.

---

## 6. Parent-Child Branch Consistency at the Persistence Layer

Branch consistency is enforced at the DbContext layer in `TenantCrmDbContext.Rules.cs`:

### 1. Payment to JobOrder Consistency:
```csharp
if (payment.JobOrderId > 0)
{
    var parentJob = JobOrders.Local.FirstOrDefault(j => j.JobOrderId == payment.JobOrderId)
        ?? JobOrders.AsNoTracking().FirstOrDefault(j => j.JobOrderId == payment.JobOrderId);
    if (parentJob != null)
    {
        if (payment.BranchId.HasValue && parentJob.BranchId.HasValue && payment.BranchId != parentJob.BranchId)
        {
            throw new InvalidOperationException($"Payment branch (#{payment.BranchId}) must match parent Job Order branch (#{parentJob.BranchId}).");
        }
        if (!payment.BranchId.HasValue && parentJob.BranchId.HasValue)
        {
            payment.BranchId = parentJob.BranchId;
        }
    }
}
```

### 2. Appointment to JobOrder Consistency:
```csharp
if (appt.LinkedJobOrderId.HasValue && appt.LinkedJobOrderId.Value > 0)
{
    var linkedJob = JobOrders.Local.FirstOrDefault(j => j.JobOrderId == appt.LinkedJobOrderId.Value)
        ?? JobOrders.AsNoTracking().FirstOrDefault(j => j.JobOrderId == appt.LinkedJobOrderId.Value);
    if (linkedJob != null)
    {
        if (appt.BranchId.HasValue && linkedJob.BranchId.HasValue && appt.BranchId != linkedJob.BranchId)
        {
            throw new InvalidOperationException($"Appointment branch (#{appt.BranchId}) must match linked Job Order branch (#{linkedJob.BranchId}).");
        }
        if (!appt.BranchId.HasValue && linkedJob.BranchId.HasValue)
        {
            appt.BranchId = linkedJob.BranchId;
        }
    }
}
```

---

## 7. Centralized Authentication Flow

```
User (WinForms / API Client)
    │
    ├── 1. POST /auth/resolve-tenants (Username, Password)
    │      └── Returns: isSuperAdmin, list of active company memberships (tenantId, displayName, role, branchCode)
    │
    ├── 2. POST /auth/login (Username, Password, selected tenantId)
    │      ├── Validates credentials via ASP.NET Core Identity
    │      ├── Verifies user is active and not locked out
    │      ├── Verifies tenant registration is active and not suspended
    │      ├── Verifies active subscription plan
    │      ├── Resolves base role and branch code
    │      └── Emits Bearer Access Token with claims:
    │             - nameid (UserId)
    │             - name (Username)
    │             - tenant_id
    │             - tenant_role (base role)
    │             - raw_role (e.g. BranchManager:NORTH)
    │             - branch_code (e.g. NORTH)
    │             - branch_id (e.g. 2)
    │             - tenant_status
    │             - subscription_plan
    │
    └── 3. GET /auth/me (Bearer Token)
           └── Returns authenticated identity, effective role, branch code, branch ID, permissions, and enabled modules.
```

---

## 8. Tenant Database Resolver & Factory Validation

`TenantDbContextFactory` ensures safe dynamic database instantiation:
1. **Separation Guard (`EnsureSeparate`):** Rejects any configuration where Platform Master and Tenant database target the same catalog.
2. **Retry Strategy Configuration:** Instantiates `TenantCrmDbContext` with SQL Server connection resiliency (`EnableRetryOnFailure(3)`).
3. **Model Cache Replacement:** Registers `TenantCrmModelCacheKeyFactory` to isolate model metadata caching per target database.
4. **Tenant Identity Validation:** Queries `DatabaseIdentity` singleton table (`WHERE Id = 1`). Mismatched tenant markers immediately abort the transaction.

---

## 9. Model Caching & Multi-Tenant DbContext Factories

EF Core caches compiled models globally by DbContext type by default. Because Company A (`DB_MasterCrm` legacy mode) suppresses `BranchId` while Tenant B and Tenant C map `BranchId` and `TenantBranch`, specialized model cache key factories prevent model collision:
- **`MasterCrmModelCacheKeyFactory`:** Differentiates `DB_MasterCrm` from migrated schema test databases.
- **`TenantCrmModelCacheKeyFactory`:** Differentiates `RevCare_TenantA` prototype from production-aligned Tenant B and Tenant C databases.

---

## 10. WinForms Session State, Display & Logout Lifecycle

1. **Session Label Display:**
   - In `TenantWorkspaceForm.cs`, the session bar displays:
     ```
     Company: {CompanyName} | User: {Username} | Role: {Role} | Branch: {Branch} | Env: Development
     ```
   - When a user has branch scope (e.g. `NORTH`), `{Branch}` displays `"NORTH"`.
   - When a user has company-wide scope (e.g. `OwnerManager`), `{Branch}` displays `"All Branches (Central)"`.
2. **Clean Logout Lifecycle:**
   - Clicking "Sign out" triggers `LogoutRequested = true` and invokes `api.ClearSession()`.
   - `ClearSession()` disposes bearer authentication headers, clears cached roles and tokens, and resets in-memory dataset buffers.
   - The desktop client returns cleanly to the tenant login dialog without credential residue.

---

## 11. Subscription Plan Enforcement & Feature Gating

| Feature / Capability | Plan A (Starter / Core) | Plan B (Professional + BI) | Plan C (Enterprise Multi-Branch) |
| :--- | :--- | :--- | :--- |
| **Core Workshop & Invoicing** | Included | Included | Included |
| **Customer 360 & Communication** | Included | Included | Included |
| **Business Intelligence (BI)** | **Blocked** (HTTP 403) | **Included** (6 KPI charts & metrics) | **Included** (Full BI suite) |
| **Multi-Branch Operations** | **Blocked** (MaxBranches = 1) | **Blocked** (MaxBranches = 1) | **Included** (Multi-branch routing & isolation) |

---

## 12. Module Entitlement Enforcement

1. **Metadata Enforcement:** Platform Master manages module enablement per tenant via `ModuleEntitlements`.
2. **Middleware Evaluation:** When a request targets a specific module path (`/crm/customers`, `/crm/inquiries`, `/crm/complaints`, `/crm/payments`, etc.), `TenantPermissions.RequiredModule(http)` inspects the path and validates `ModuleEntitlements.IsEnabled`.
3. **Dynamic Rejection:** Disabling a module immediately prevents operational calls from that tenant's users with `403 Forbidden`. Re-enabling the module instantly restores access.

---

## 13. Staff Account Provisioning & Lifecycle

1. **Centralized Identity & Company Membership:**
   - Staff creation (`POST /crm/staff`) generates an `IdentityUser` in `PlatformMasterDbContext` and creates an active `TenantMembership`.
   - Roles are restricted to verified tenant roles (`OwnerManager`, `BranchManager`, `ServiceAdvisor`, `LeadMechanic`, `AutomotiveMechanic`, `DetailingTechnician`).
   - Granting `OwnerManager` requires explicit administrative confirmation flag (`AllowOwnerGrant`).
2. **Profile Synchronization:**
   - The staff profile is provisioned in the tenant's `Staff` table with `ProfileProvisioningStatus: "Synced"`.
   - If the operational database is temporarily offline during registration, the profile status is recorded as `"Pending"` and can be retried via `POST /crm/staff/{userId}/retry-profile`.
3. **Deactivation Safeguards:**
   - Mechanics with unfinished or ongoing job tasks cannot be deactivated (`HTTP 409 Conflict`).
   - The primary `OwnerManager` cannot revoke their own active membership.

---

## 14. Protection of Operational Databases During Testing

- **Zero Migrations on Active Operational Databases:** No physical schema changes or destructive operations are applied to `DB_MasterCrm`, `RevCare_PlatformMaster`, `RevCare_TenantA`, `RevCare_TenantB`, or `RevCare_TenantC`.
- **Dynamic Disposable Databases:** All integration and smoke test suites provision disposable SQL Server databases with random GUID suffixes (e.g. `RevCare_BackendTenant_{GUID}_TestTenantB`), run migrations, execute verification, and cleanly drop the databases upon completion.

---

## 15. Audit Logging & Immutable Attribution

1. **Actor Attribution:** Every mutating request resolves the authenticated username from JWT claims and assigns `db.AuditActor = username`.
2. **Automated Audit Records:** Changes to appointments, payment voids, cancellations, archival actions, and mechanic reassignments create structured, immutable records in `AuditEntries`.
3. **Unattributed Prevention:** Unattributed operations default to `"Unattributed application"`, which is strictly checked and forbidden in automated verification suites.

---

## 16. Error Handling, Concurrency & Security Sanitization

1. **Concurrency Protection:** Modifying job orders, job tasks, customers, or services requires `RowVersion` matching. Concurrent edits return `409 Conflict` with client-safe reload instructions.
2. **Credential Sanitization:** All diagnostics, logs, and exception formatters strip passwords, user IDs, and connection credentials before output.
3. **Safe Business Rule Failures:** Domain rule violations throw structured `InvalidOperationException` messages rendered safely to clients without leaking internal SQL or infrastructure details.

---

## 17. Cutover Preparation for Company A

1. **Option 1 Implementation Ready:**
   - `DB_MasterCrm` operational schema alignment is complete and verified.
   - Company A can be registered in `RevCare_PlatformMaster` with `ConnectionKey: "TenantA"` pointing directly to `DB_MasterCrm`.
   - Production cutover requires zero physical ETL migration, guaranteeing 100% preservation of all 220 Job Orders, 209 Customers, 202 Vehicles, and 223 Payments.
2. **Prototype Retention:**
   - Prototype database `RevCare_TenantA` remains intact as a historical baseline reference.

---

## 18. Verification Results & Test Evidence

All automated test suites were executed on `(localdb)\MSSQLLocalDB` and passed with 100% success rate:

```
+----------------------------------------------------------------------------------------------------+
|                                    PHASE 3 VERIFICATION SUMMARY                                    |
+------------------------------------+-----------------------+-------------------+-------------------+
| Test Suite                         | Executed Checks       | Passed Checks     | Status            |
+------------------------------------+-----------------------+-------------------+-------------------+
| Backend.Smoke                      | 166 checks            | 166 passed        | 100% PASSED       |
| BusinessRules.Smoke                | 160+ checks           | 160+ passed       | 100% PASSED       |
| Verification.Smoke                 | Full suite            | All passed        | 100% PASSED       |
| Solution Build (Debug & Release)   | 4 projects            | 0 errors, 0 warns | COMPILED CLEAN    |
+------------------------------------+-----------------------+-------------------+-------------------+
```

### Key Verified Phase 3 Security Assertions:
1. `SuperAdmin` denied access to `/crm/customers`, `/crm/job-orders`, `/crm/payments`, `/crm/workshop` (`403 Forbidden`).
2. `BranchManager` granted full branch workshop management, but blocked from `Jobs.Delete`, `Payments.Correct`, `Staff.Manage`, and `Services.Manage`.
3. `ServiceAdvisor` blocked from payment voiding and service price modification.
4. `AutomotiveMechanic` and `DetailingTechnician` strictly limited to assigned tasks with specialty qualification enforcement.
5. `BranchManager:NORTH` and `BranchManager:SOUTH` operational records strictly isolated within Tenant C.
6. Server-side branch ID enforcement prevents client parameter tampering.
7. Cross-branch payment creation and appointment linking fail closed at the persistence layer.
8. WinForms session bar accurately renders `Company`, `User`, `Role`, `Branch`, and `Env`.
9. Clean logout disposes context, clears tokens, and resets cached records.
