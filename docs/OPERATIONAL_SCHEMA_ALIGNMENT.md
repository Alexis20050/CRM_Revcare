# RevCare CRM — Phase 2: Operational Schema Unification and Alignment Specification

**Document Version:** 1.0.0 (Phase 2 Architectural Specification & Implementation Report)  
**Date:** November 2026  
**Author:** Senior Software Architect, Database Architect, and Multi-Tenant Security Specialist  
**Solution:** RevCare Auto Hub CRM (.NET 10, ASP.NET Core Web API, EF Core, SQL Server LocalDB, Windows Forms)  
**Workspace:** `C:\Users\alexi\source\repos\CRM_Revcare`  
**Target Environment:** Local Development `(localdb)\MSSQLLocalDB`

---

## 1. Executive Summary & Approved Architectural Direction

RevCare CRM has transitioned from an isolated single-tenant workshop desktop system into a **Multi-Company, Multi-Tenant SaaS Architecture** built upon shared application binaries, unified domain models, shared transactional business rules, and strictly isolated physical company databases.

### 1.1 Approved Architectural Direction: Option 1
Following the Phase 1 architectural evaluation, **Option 1** was approved:
- The existing production database `DB_MasterCrm` is registered as the authoritative operational database for **Company A / Tenant A** ("RevCare Auto Hub").
- No destructive or lossy ETL migration of Company A’s existing production records into `RevCare_TenantA` is performed.
- The physical database name `DB_MasterCrm` remains unchanged.
- The prototype database `RevCare_TenantA` is maintained as a read-only baseline snapshot protected by immutable backup (`RevCare_TenantA_Baseline.bak`), retired from active runtime routing upon final cutover.
- Company B (`RevCare_TenantB`) and Company C (`RevCare_TenantC`) operate on the aligned unified schema, with Company C supporting multiple branches (`MAIN`, `NORTH`, `SOUTH`) within a single company database.

### 1.2 Phase 2 Scope & Safety Boundary Adherence
- **LOCAL DEVELOPMENT ONLY:** Executed entirely against SQL Server instance `(localdb)\MSSQLLocalDB`.
- **ABSOLUTE SAFETY ENFORCEMENT:** Zero physical migrations were applied to the 5 active operational databases (`DB_MasterCrm`, `RevCare_PlatformMaster`, `RevCare_TenantA`, `RevCare_TenantB`, `RevCare_TenantC`).
- **DISPOSABLE DATABASE VALIDATION:** All additive and alignment migrations were tested exclusively on dynamically provisioned disposable test databases (`Disposable_CompanyA_MigrationTest`, `Disposable_CompanyB_MigrationTest`, `Disposable_CompanyC_MigrationTest`, `Disposable_TenantA_BackupTest`).
- **DATA PRESERVATION:** Primary keys, historical transaction pricing, payment allocations, audit entries, and customer reference IDs remain 100% unaltered.

---

## 2. Architectural Database Model Comparison

Prior to Phase 2, `MasterCrmDbContext` (Legacy CRM) and `TenantCrmDbContext` (Multi-Tenant prototype) had diverged due to iterative feature development:

```
+----------------------------------------------------------------------------------------------------+
|                                    ARCHITECTURAL DIVERGENCE (PRE-PHASE 2)                          |
+----------------------------------------------------+-----------------------------------------------+
| MasterCrmDbContext (CRM Legacy Superset)           | TenantCrmDbContext (Prototype Subset)         |
+----------------------------------------------------+-----------------------------------------------+
| - Complete 3NF Vehicles (PlateNumber, VIN, etc.)   | - Flat VehicleDetails string only             |
| - 3NF Estimates & EstimateLines                    | - Ignored Estimates, EstimateLines            |
| - Appointments & Rescheduling Histories            | - Ignored Appointments, AppointmentHistories  |
| - Job Cancellation Requests & Refunds Lifecycle    | - Ignored JobCancellationRequests, Refunds    |
| - Soft Archiving metadata on JobOrders & Tasks     | - Ignored Archiving metadata                  |
| - Single-shop focus (no Branches table)            | - Multi-branch support via Branches table     |
| - No TenantDatabaseIdentity singleton table        | - TenantDatabaseIdentity singleton table      |
| - WorkshopStaff using string StaffId               | - TenantStaff using GUID UserId               |
+----------------------------------------------------+-----------------------------------------------+
```

Through Phase 2 unification, both contexts share common entity contracts (`crm.domain.entities`) and enforce identical database structures, while dynamic model caching isolates legacy unmigrated databases from runtime errors.

---

## 3. Concrete Differences Matrix

The following matrix documents the schema comparison before and after Phase 2 alignment:

| Table / Entity | `DB_MasterCrm` (Company A Baseline) | `TenantCrmDbContext` (Pre-Phase 2) | Unified Target Schema (Phase 2 Aligned) | Alignment Action Taken |
| :--- | :--- | :--- | :--- | :--- |
| **`Customers`** | Normalized (PK `CustomerId`) | Normalized (PK `CustomerId`) | Normalized (PK `CustomerId`) | Fully aligned |
| **`Vehicles`** | 3NF normalized table | *Table Missing* (Ignored) | 3NF normalized table | **Restored** to `TenantCrmDbContext` |
| **`Estimates`** | 3NF normalized table | *Table Missing* (Ignored) | 3NF normalized table | **Restored** to `TenantCrmDbContext` |
| **`EstimateLines`** | 3NF normalized line items | *Table Missing* (Ignored) | 3NF normalized line items | **Restored** to `TenantCrmDbContext` |
| **`Appointments`** | Normalized lifecycle table | *Table Missing* (Ignored) | Normalized table with `BranchId` | **Restored** to `TenantCrmDbContext`; added `BranchId` FK |
| **`AppointmentHistories`** | Reschedule/Cancel audit | *Table Missing* (Ignored) | Normalized audit table | **Restored** to `TenantCrmDbContext` |
| **`JobCancellationRequests`**| Review workflow table | *Table Missing* (Ignored) | Normalized review table | **Restored** to `TenantCrmDbContext` |
| **`Refunds`** | Disbursement tracking table | *Table Missing* (Ignored) | Normalized disbursement table | **Restored** to `TenantCrmDbContext` |
| **`JobOrders`** | 220 records; soft archiving | 1 record; un-archived | Soft archiving, 3NF FKs, `BranchId` | **Additive migration** to `MasterCrm`; aligned `TenantCrm` |
| **`JobTasks`** | 229 records; soft archiving | 0 records; TaskWork model | Soft archiving, agreed pricing | **Aligned** `TenantCrm` with archiving columns |
| **`Payments`** | 223 records; void audit | 1 record; void audit | Includes `BranchId`, `Purpose`, `Payer` | **Added** `BranchId` to both contexts |
| **`Branches`** | *Table Missing* | Present (`MAIN`) | Standardized `Branches` table | **Added** to `MasterCrmDbContext` with migration |
| **`TenantDatabaseIdentity`** | *Table Missing* | Present (`tenant-a`) | Singleton check constraint `[Id]=1` | **Added** to `MasterCrmDbContext` with migration |

---

## 4. Concrete Alignment Actions Taken

### 4.1 Additive EF Core Migration for `MasterCrmDbContext`
Generated migration: `20261124131444_AddTenantIdentityAndBranchesToMasterCrm`  
- Added `TenantDatabaseIdentity` table with `CK_TenantIdentity_Singleton` constraint enforcing `[Id] = 1`.
- Added `Branches` table with default `MAIN` branch seeding (`BranchCode = 'MAIN'`).
- Added nullable `BranchId` column to `JobOrders`, `Appointments`, and `Payments` with foreign keys referencing `Branches(BranchId)`.
- Implemented backward-compatible SQL script backfilling existing historical rows to `BranchId = 1` (`MAIN`).

### 4.2 Alignment EF Core Migration for `TenantCrmDbContext`
Generated migration: `20261124131459_AlignTenantCrmWithOperationalSchema`  
- Un-ignored 3NF normalized entities: `Vehicles`, `Estimates`, `EstimateLines`, `Appointments`, `AppointmentHistories`, `JobCancellationRequests`, `Refunds`.
- Added soft-archiving metadata columns to `JobOrders` and `JobTasks` (`IsArchived`, `ArchivedAtUtc`, `ArchivedBy`, `ArchiveReason`).
- Added pricing and audit snapshots to `JobTasks` (`AgreedPrice`, `ServiceNameSnapshot`).
- Added relational foreign keys linking `JobOrders` to `Vehicles(VehicleId)` and `Estimates(ApprovedEstimateId)`.

### 4.3 Non-Reentrant Model Cache Key Factories (`IModelCacheKeyFactory`)
To protect active operational databases that have not yet undergone physical migration cutover while allowing disposable database testing:
1. **`MasterCrmModelCacheKeyFactory`:** Extracts `InitialCatalog` from relational connection string extensions. Detects `DB_MasterCrm` in memory without triggering reentrant `DbContext` model creation. Dynamically ignores `BranchId`, `Branches`, and `TenantDatabaseIdentity` when connected to physical `DB_MasterCrm`, but compiles full unified schema for disposable test databases and EF design-time tooling.
2. **`TenantCrmModelCacheKeyFactory`:** Detects prototype database `RevCare_TenantA` and isolates its model cache key, safely ignoring unmigrated columns while serving full 3NF schemas to Company B and Company C.

---

## 5. Shared Entities and Shared Services Architecture

The solution preserves clean architectural separation across four solution projects:
- **`CRM.domain`:** Enterprise entities (`Customer`, `Vehicle`, `JobOrder`, `JobTask`, `Payment`, `Appointment`, `Estimate`, `JobCancellationRequest`, `Refund`, `TenantBranch`, `TenantDatabaseIdentity`).
- **`CRM.infrastructure`:** Data persistence (`MasterCrmDbContext`, `TenantCrmDbContext`), business rules (`WorkshopRules`), reporting templates (`ReportTemplateBuilder`), grid standardizers (`CrmGridStandardizer`).
- **`CRM.api`:** ASP.NET Core Web API multi-tenant routing, JWT authentication, subscription middleware, BI KPI endpoints, branch endpoints.
- **`CRM_Revcare`:** WinForms desktop clients (CRM Legacy / Company A Workspace, Multi-Tenant Workspace, Customer 360 Dialogs, Archive Dialogs).

---

## 6. Company A Authoritative Operational Database Configuration (`DB_MasterCrm`)

- **Role:** Production operational database for Company A / RevCare Auto Hub.
- **Plan:** Plan A (Starter / Core Workshop Operations).
- **Physical Name:** `DB_MasterCrm`.
- **Identity Marker:** `tenant-a`.
- **Authoritative Counts:** 209 Customers, 220 Job Orders, 202 Vehicles, 217 Estimates, 229 Job Tasks, 223 Payments, 207 Inquiries, 204 Feedbacks, 203 Complaints, 15 Staff.
- **Runtime Handling:** Handled via `MasterCrmDbContext` with dynamic model caching ensuring legacy table stability prior to cutover.

---

## 7. Tenant B Schema Configuration (`RevCare_TenantB`)

- **Role:** Independent automotive company.
- **Plan:** Plan B (Professional with Business Intelligence & Analytics).
- **Physical Name:** `RevCare_TenantB`.
- **Identity Marker:** `tenant-b`.
- **Schema Features:** Complete 3NF normalized schema (Vehicles, Estimates, Appointments, Refunds), soft-archiving metadata, default branch `MAIN`.
- **Entitlements:** 6 KPI analytical graphs, customer retention metrics, service turnaround analytics. Branch management restricted.

---

## 8. Tenant C Multi-Branch Schema Configuration (`RevCare_TenantC`)

- **Role:** Enterprise automotive company with multiple physical workshops.
- **Plan:** Plan C (Enterprise Multi-Branch & Analytics).
- **Physical Name:** `RevCare_TenantC`.
- **Identity Marker:** `tenant-c`.
- **Multi-Branching Topology:** Single database containing 3 relational branches:
  - `MAIN`: Main Service Center (`BranchId = 1`)
  - `NORTH`: North Workshop Branch (`BranchId = 2`)
  - `SOUTH`: South Express Service (`BranchId = 3`)
- **Isolation Mechanism:** All transactions (`JobOrders`, `Appointments`, `Payments`) are partitioned by foreign key `BranchId`. Cross-branch data leakage within Company C is prohibited except under authorized multi-branch aggregation roles.

---

## 9. Prototype Database Safeguard Strategy

- **Database:** `RevCare_TenantA` (Initial prototype).
- **Cold Backup Artifact:** `$env:TEMP\RevCare_TenantA_Baseline.bak` (Verified 1 Customer, 1 JobOrder, 1 Payment, 1 Staff).
- **Safeguard Protocol:**
  - `RevCare_TenantA` is marked READ-ONLY / FROZEN.
  - Zero schema alter statements, migrations, or data purges are permitted against `RevCare_TenantA`.
  - Verification was completed restoring `RevCare_TenantA_Baseline.bak` to disposable test databases with byte-for-byte checksum validation.

---

## 10. Migration Execution Plan: Disposable vs Production Cutover

```mermaid
flowchart TD
    subgraph Phase 2: Verified Local Implementation
        M1["MasterCrm Additive Migration<br/>(20261124131444)"] --> D1["Disposable_CompanyA_MigrationTest"]
        M2["TenantCrm Alignment Migration<br/>(20261124131459)"] --> D2["Disposable_CompanyB_MigrationTest"]
        M2 --> D3["Disposable_CompanyC_MigrationTest"]
        D1 & D2 & D3 --> T1["BusinessRules.Smoke (160+ checks PASS)"]
        D1 & D2 & D3 --> T2["Verification.Smoke (100% PASS)"]
        D1 & D2 & D3 --> T3["Backend.Smoke (140 checks PASS)"]
    end

    subgraph Phase 3: Deferred Production Cutover (Pending User Approval)
        B1["Backup DB_MasterCrm<br/>(Full Copy-Only Backup)"]
        B1 --> C1["Apply 20261124131444 to DB_MasterCrm"]
        C1 --> C2["Register tenant-a in RevCare_PlatformMaster"]
        C2 --> C3["Verify Live Routing to DB_MasterCrm"]
        C3 --> C4["Retire Prototype RevCare_TenantA"]
    end
```

---

## 11. Complete DDL Schema Specification for the Unified Model

### 11.1 Tenant Database Identity Singleton
```sql
CREATE TABLE [TenantDatabaseIdentity] (
    [Id] INT NOT NULL,
    [TenantId] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_TenantDatabaseIdentity] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_TenantIdentity_Singleton] CHECK ([Id] = 1)
);
```

### 11.2 Branches
```sql
CREATE TABLE [Branches] (
    [BranchId] INT IDENTITY(1,1) NOT NULL,
    [BranchCode] NVARCHAR(50) NOT NULL,
    [BranchName] NVARCHAR(200) NOT NULL,
    [Address] NVARCHAR(500) NULL,
    [ContactNumber] NVARCHAR(50) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAtUtc] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Branches] PRIMARY KEY ([BranchId]),
    CONSTRAINT [UQ_Branches_BranchCode] UNIQUE ([BranchCode])
);
```

### 11.3 Vehicles (3NF)
```sql
CREATE TABLE [Vehicles] (
    [VehicleId] INT IDENTITY(1,1) NOT NULL,
    [CustomerId] INT NOT NULL,
    [PlateNumber] NVARCHAR(50) NULL,
    [Make] NVARCHAR(100) NOT NULL,
    [Model] NVARCHAR(100) NOT NULL,
    [Year] INT NULL,
    [Color] NVARCHAR(50) NULL,
    [Vin] NVARCHAR(50) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Vehicles] PRIMARY KEY ([VehicleId]),
    CONSTRAINT [FK_Vehicles_Customers_CustomerId] FOREIGN KEY ([CustomerId]) 
        REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION
);
CREATE INDEX [IX_Vehicles_CustomerId] ON [Vehicles] ([CustomerId]);
CREATE INDEX [IX_Vehicles_PlateNumber] ON [Vehicles] ([PlateNumber]);
```

### 11.4 Estimates & EstimateLines (3NF)
```sql
CREATE TABLE [Estimates] (
    [EstimateId] INT IDENTITY(1,1) NOT NULL,
    [EstimateNumber] NVARCHAR(50) NOT NULL,
    [CustomerId] INT NOT NULL,
    [VehicleId] INT NULL,
    [Status] NVARCHAR(30) NOT NULL DEFAULT 'Draft',
    [TotalAmount] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    [Notes] NVARCHAR(1000) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Estimates] PRIMARY KEY ([EstimateId]),
    CONSTRAINT [UQ_Estimates_EstimateNumber] UNIQUE ([EstimateNumber]),
    CONSTRAINT [FK_Estimates_Customers_CustomerId] FOREIGN KEY ([CustomerId]) 
        REFERENCES [Customers] ([CustomerId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Estimates_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) 
        REFERENCES [Vehicles] ([VehicleId]) ON DELETE NO ACTION
);

CREATE TABLE [EstimateLines] (
    [EstimateLineId] INT IDENTITY(1,1) NOT NULL,
    [EstimateId] INT NOT NULL,
    [ServiceCatalogId] INT NULL,
    [Description] NVARCHAR(250) NOT NULL,
    [Quantity] DECIMAL(18,2) NOT NULL DEFAULT 1.00,
    [UnitPrice] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    [LineTotal] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT [PK_EstimateLines] PRIMARY KEY ([EstimateLineId]),
    CONSTRAINT [FK_EstimateLines_Estimates_EstimateId] FOREIGN KEY ([EstimateId]) 
        REFERENCES [Estimates] ([EstimateId]) ON DELETE CASCADE
);
```

### 11.5 Appointments & Histories
```sql
CREATE TABLE [Appointments] (
    [AppointmentId] INT IDENTITY(1,1) NOT NULL,
    [CustomerId] INT NOT NULL,
    [VehicleId] INT NULL,
    [InquiryId] INT NULL,
    [LinkedJobOrderId] INT NULL,
    [BranchId] INT NULL,
    [ScheduledStart] DATETIME2 NOT NULL,
    [ScheduledEnd] DATETIME2 NULL,
    [Status] NVARCHAR(30) NOT NULL DEFAULT 'Scheduled',
    [RequestedServices] NVARCHAR(1000) NULL,
    [ServiceAdvisorUserId] NVARCHAR(450) NULL,
    [CancellationReason] NVARCHAR(500) NULL,
    [CancelledBy] NVARCHAR(100) NULL,
    [Notes] NVARCHAR(2000) NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([AppointmentId]),
    CONSTRAINT [FK_Appointments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([CustomerId]),
    CONSTRAINT [FK_Appointments_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([VehicleId]),
    CONSTRAINT [FK_Appointments_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([BranchId])
);
CREATE UNIQUE INDEX [IX_Appointments_LinkedJobOrderId] ON [Appointments] ([LinkedJobOrderId]) WHERE [LinkedJobOrderId] IS NOT NULL;

CREATE TABLE [AppointmentHistories] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [AppointmentId] INT NOT NULL,
    [Action] NVARCHAR(50) NOT NULL,
    [Reason] NVARCHAR(1000) NOT NULL,
    [Actor] NVARCHAR(150) NOT NULL,
    [TimestampUtc] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_AppointmentHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppointmentHistories_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) 
        REFERENCES [Appointments] ([AppointmentId]) ON DELETE CASCADE
);
```

### 11.6 JobCancellationRequests & Refunds
```sql
CREATE TABLE [JobCancellationRequests] (
    [RequestId] INT IDENTITY(1,1) NOT NULL,
    [JobOrderId] INT NOT NULL,
    [ReasonCategory] NVARCHAR(100) NOT NULL,
    [ReasonExplanation] NVARCHAR(2000) NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending Review',
    [RequestedBy] NVARCHAR(150) NOT NULL,
    [RequestedAtUtc] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    [TotalPaidAtRequest] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    [IncurredCharges] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    [ProposedRefund] DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    [ApprovedRefund] DECIMAL(18,2) NULL,
    [ReviewJustification] NVARCHAR(2000) NULL,
    [ReviewedBy] NVARCHAR(150) NULL,
    [ReviewedAtUtc] DATETIME2 NULL,
    CONSTRAINT [PK_JobCancellationRequests] PRIMARY KEY ([RequestId]),
    CONSTRAINT [FK_JobCancellationRequests_JobOrders_JobOrderId] FOREIGN KEY ([JobOrderId]) REFERENCES [JobOrders] ([JobOrderId])
);

CREATE TABLE [Refunds] (
    [RefundId] INT IDENTITY(1,1) NOT NULL,
    [JobOrderId] INT NOT NULL,
    [PaymentId] INT NULL,
    [CancellationRequestId] INT NULL,
    [SubmissionId] UNIQUEIDENTIFIER NOT NULL,
    [Amount] DECIMAL(18,2) NOT NULL,
    [RefundMethod] NVARCHAR(50) NOT NULL,
    [RecipientName] NVARCHAR(200) NOT NULL,
    [Reason] NVARCHAR(1000) NOT NULL,
    [AuthorizedBy] NVARCHAR(150) NOT NULL,
    [ProcessedAtUtc] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Refunds] PRIMARY KEY ([RefundId]),
    CONSTRAINT [UQ_Refunds_SubmissionId] UNIQUE ([SubmissionId]),
    CONSTRAINT [FK_Refunds_JobOrders_JobOrderId] FOREIGN KEY ([JobOrderId]) REFERENCES [JobOrders] ([JobOrderId]),
    CONSTRAINT [FK_Refunds_JobCancellationRequests_CancellationRequestId] FOREIGN KEY ([CancellationRequestId]) REFERENCES [JobCancellationRequests] ([RequestId])
);
```

---

## 12. Check Constraints, Indexes, and Integrity Safeguards

1. **Singleton Identity Guard:** `[CK_TenantIdentity_Singleton]` enforces `[Id] = 1`, ensuring no database can accidentally hold multiple tenant identity tags.
2. **One-to-One Appointment to Job Link:** `[IX_Appointments_LinkedJobOrderId]` with filtered index `WHERE [LinkedJobOrderId] IS NOT NULL` prevents concurrent or duplicate job order linking to a single appointment.
3. **Refund Submission Idempotency:** `[UQ_Refunds_SubmissionId]` prevents double-disbursement or duplicate refund processing on network retry.
4. **Foreign Key Deletion Safeguards:** All customer, vehicle, and financial parent foreign keys are configured with `ON DELETE NO ACTION` / `ON DELETE RESTRICT` to prevent cascading physical deletions of operational records.

---

## 13. Data Seeding & Backfill Strategy

- **Company A Backfill:** When `20261124131444` is applied during cutover, it executes:
  ```sql
  IF NOT EXISTS (SELECT 1 FROM [Branches] WHERE [BranchCode] = 'MAIN')
      INSERT INTO [Branches] ([BranchCode], [BranchName], [IsActive], [CreatedAtUtc])
      VALUES ('MAIN', 'Main Workshop Branch', 1, SYSUTCDATETIME());

  IF NOT EXISTS (SELECT 1 FROM [TenantDatabaseIdentity] WHERE [Id] = 1)
      INSERT INTO [TenantDatabaseIdentity] ([Id], [TenantId])
      VALUES (1, 'tenant-a');

  UPDATE [JobOrders] SET [BranchId] = 1 WHERE [BranchId] IS NULL;
  UPDATE [Appointments] SET [BranchId] = 1 WHERE [BranchId] IS NULL;
  UPDATE [Payments] SET [BranchId] = 1 WHERE [BranchId] IS NULL;
  ```
- **Company B Onboarding:** Initialized with `TenantDatabaseIdentity` (`tenant-b`) and default branch `MAIN` (`BranchId = 1`).
- **Company C Onboarding:** Initialized with `TenantDatabaseIdentity` (`tenant-c`) and 3 configured branches (`MAIN`, `NORTH`, `SOUTH`).

---

## 14. Multi-Company Data Isolation Verification

The multi-company isolation test suite (`tests/BusinessRules.Smoke/MultiCompanyIsolationTests.cs`) verified:
- **Overlapping Primary Keys:** Customer `1` and Job Order `1` exist concurrently across Company A, Company B, and Company C.
- **Zero Cross-Query Leakage:** Queries against Company A returned Company A records only. Queries against Company B returned Company B records only. Queries against Company C returned Company C records only.
- **Vehicle Link Integrity:** Job Order `1` in each database linked exclusively to its own database's Vehicle entity.
- **Relational Branch Partitioning in Company C:** Branch `NORTH` (`BranchId = 2`) and Branch `SOUTH` (`BranchId = 3`) queries strictly isolated their respective orders.

---

## 15. Cross-Company Security & Tenant Identification Strategy

- **Connection Resolution:** The ASP.NET Core multi-tenant middleware resolves the tenant from the authenticated claims (`TenantId`), looks up the tenant's connection descriptor in `RevCare_PlatformMaster.dbo.Tenants`, and binds the request-scoped `TenantCrmDbContext`.
- **Database Self-Validation:** On connection establishment, `TenantCrmDbContext` verifies that `DatabaseIdentity.First().TenantId == currentTenantId`. If a database identity mismatch occurs (e.g., misconfigured connection string), the context aborts immediately with a `SecurityException`, preventing unauthorized access.
- **Physical Boundary:** No cross-database joins or cross-database transactions are permitted.

---

## 16. Subscription Entitlements & Feature Decoupling

| Feature / Capability | Plan A (Starter) | Plan B (Professional) | Plan C (Enterprise) | Technical Enforcement |
| :--- | :---: | :---: | :---: | :--- |
| **Workshop Operational Core** | Allowed | Allowed | Allowed | Shared `WorkshopRules` |
| **Customer & Vehicle Management**| Allowed | Allowed | Allowed | 3NF domain entities |
| **Estimates & Invoicing** | Allowed | Allowed | Allowed | `WorkshopRules.ValidateDeposit` |
| **Business Intelligence Analytics**| **Forbidden (403)** | **Allowed (200)** | **Allowed (200)** | Policy `SubscriptionEntitlement` |
| **Six KPI BI Visualizations** | **Forbidden (403)** | **Allowed (200)** | **Allowed (200)** | Module Claim `BusinessIntelligence` |
| **Branch Management (`/branches`)**| **Forbidden (403)** | **Forbidden (403)** | **Allowed (200)** | Policy `BranchingEntitlement` |
| **Multi-Branching in Single DB** | 1 Branch (`MAIN`) | 1 Branch (`MAIN`) | Up to 999 Branches | `SubscriptionPlans.MaxBranches` |

Upgrading a company from Plan A to Plan B or Plan C updates `CurrentPlanId` in `RevCare_PlatformMaster.dbo.Tenants` with zero physical database relocation or schema changes.

---

## 17. Risk Analysis & Mitigation Plan

| Risk | Impact | Likelihood | Mitigation Strategy |
| :--- | :--- | :--- | :--- |
| Schema drift between `MasterCrm` and `TenantCrm` | High | Low | Unified EF Core models and automated smoke test `HasPendingModelChanges()` checks. |
| Accidental migration against unmigrated production DB | Critical | Low | Hardcoded safety boundary, model cache key factory isolation, pre-cutover gate. |
| In-memory model caching conflicts | High | Med | Custom `IModelCacheKeyFactory` incorporating connection initial catalog into cache key. |
| Identity singleton violation | High | Low | Database check constraint `[CK_TenantIdentity_Singleton]` rejects multiple identities. |
| Foreign key constraint violations during backfill | High | Low | Migration explicitly seeds default `MAIN` branch before executing FK column additions. |

---

## 18. Pre-Cutover Verification Checklist

Before approving final cutover of `DB_MasterCrm` to Tenant A active routing:
- [x] Additive migration script generated and syntax validated.
- [x] Alignment migration script generated and syntax validated.
- [x] Clean migration application verified on disposable test databases.
- [x] Multi-company data isolation verified with overlapping PKs.
- [x] Single-database multi-branching verified for Tenant C.
- [x] 100% of 160+ checks in `BusinessRules.Smoke` passed.
- [x] 100% of 140 checks in `Backend.Smoke` passed.
- [x] 100% of CRM Legacy and Tenant A checks in `Verification.Smoke` passed.
- [x] Solution builds with zero errors across all 4 projects.
- [ ] Explicit user authorization received for production cutover.

---

## 19. Rollback Plan

In the event of an unforeseen issue during production cutover:
1. **Immediate Database Restore:** Restore `DB_MasterCrm` from its pre-cutover backup copy.
2. **Revert Routing:** Revert `RevCare_PlatformMaster.dbo.Tenants` `ConnectionKey` / `DatabaseName` to baseline.
3. **No Prototype Impact:** Prototype `RevCare_TenantA` remains in its original untouched state, allowing instant fallback.
4. **Zero Data Loss:** All operational transactions are preserved in the pre-cutover snapshot.

---

## 20. Traceability Matrix to 20 Original Use Cases

| UC # | Original Use Case | Unified Schema Support | Entity / Table Implementation |
| :---: | :--- | :--- | :--- |
| **UC01** | Manage Leads | Fully Supported | `Inquiries` table with customer and vehicle linkages |
| **UC02** | Manage Customers & Vehicles | Fully Supported | `Customers` and normalized 3NF `Vehicles` tables |
| **UC03** | Manage Appointments | Fully Supported | `Appointments` and `AppointmentHistories` tables |
| **UC04** | Manage Estimates & Approvals | Fully Supported | `Estimates` and `EstimateLines` 3NF tables |
| **UC05** | Insurance Management | Fully Supported | `Inquiries`, `Estimates`, and `JobOrders` insurance fields |
| **UC06** | Manage Services & Prices | Fully Supported | `Services` / `ServiceOfferings` with agreed price snapshots |
| **UC07** | Manage Job Orders | Fully Supported | `JobOrders` with 5-stage lifecycle and soft archiving |
| **UC08** | Manage Job Tasks | Fully Supported | `JobTasks` with atomic task generation and progress tracking |
| **UC09** | Assign Mechanics | Fully Supported | Whole-job mechanic assignment (`AssignedMechanicUserId`) |
| **UC10** | Track Job Progress | Fully Supported | Real-time task progress calculation and status transitions |
| **UC11** | Quality Assurance (Final Inspection)| Fully Supported | Integrated inspection confirmation before completion |
| **UC12** | Invoicing & Payments | Fully Supported | `Payments` with 50% deposit enforcement and void audit |
| **UC13** | Cancellation & Refunds | Fully Supported | `JobCancellationRequests` and `Refunds` review lifecycle |
| **UC14** | Vehicle Release | Fully Supported | Full financial settlement check before vehicle release |
| **UC15** | Customer Feedback & Complaints | Fully Supported | `Feedbacks` and `Complaints` tables with escalation links |
| **UC16** | Customer 360 Profile | Fully Supported | Unified 5-tab profile dialog aggregating customer history |
| **UC17** | Record Archiving & Retention | Fully Supported | Soft archiving (`IsArchived`, `ArchivedAtUtc`, `ArchivedBy`) |
| **UC18** | Staff & Role Authorization | Fully Supported | Role-based assignment eligibility and permission matrix |
| **UC19** | Multi-Branch Operations | Fully Supported | `Branches` table with relational partitioning (Plan C) |
| **UC20** | Business Intelligence & Analytics | Fully Supported | Subscription-gated BI endpoints and 6 KPI graphs (Plan B/C) |

---

## 21. Phase 3 Security & Company Routing Status

Phase 3 has extended the aligned operational schema with centralized identity and secure company routing:
1. **Dynamic Routing Factory:** `TenantDbContextFactory` validates tenant database identities against `DatabaseIdentity` table and uses `TenantCrmModelCacheKeyFactory`.
2. **Branch Authorization:** Endpoints and persistence rules validate branch ownership and parent-child consistency across `JobOrders`, `JobTasks`, `Payments`, and `Appointments`.
3. **Role Matrix Verification:** Complete matrix of operational roles (`OwnerManager`, `BranchManager`, `ServiceAdvisor`, `LeadMechanic`, `AutomotiveMechanic`, `DetailingTechnician`) verified via automated test suites.
4. **Reference Specification:** Full architecture and test logs are detailed in [docs/AUTHENTICATION_AND_TENANT_SECURITY.md](file:///c:/Users/alexi/source/repos/CRM_Revcare/docs/AUTHENTICATION_AND_TENANT_SECURITY.md).

