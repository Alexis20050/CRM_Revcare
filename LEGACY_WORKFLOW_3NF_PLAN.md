# CRM Legacy: Workflow Simplification & Database Normalization Toward 3NF Plan

**Target Application:** CRM Legacy (`CRM_Revcare.exe`)  
**Target Context:** `MasterCrmDbContext`  
**Databases:** Local Legacy `DB_MasterCrm` (and `DB_MasterCrm_MigrationTest`), Cloud Legacy `db80735`  
**Protected Contexts:** Multi-tenant systems (`PlatformMasterDbContext` / `RevCare_PlatformMaster` / `db81188` and `TenantCrmDbContext` / `RevCare_TenantA` / `db81189`) must remain 100% unaffected.

---

## 1. Executive Overview & Architectural Objectives

This plan coordinates two fundamental improvements to the Legacy CRM:
1. **Workflow & UI Simplification:**
   - Eliminate confusing placeholder ID inputs (e.g., "Assigned on save") on creation forms, replacing them with context-aware titles ("New Job Order", "New Customer").
   - Replace unrestricted status dropdown editing with explicit contextual action buttons: **Start Work**, **Complete Job**, **Release Vehicle**, and **Cancel Job**.
   - Show clear, actionable explanations beside disabled buttons (e.g., *"₱1,500.00 more deposit required before work can start"*).
   - Introduce an Appointment lifecycle embedded within the Inquiry module (`Scheduled` → `Checked In` / `Cancelled` / `No Show`) with reschedule audit tracking and idempotent Job Order check-in.
   - Enforce rigorous Job Order cancellation rules that protect financial history, exclude cancelled work from mechanic queues, and block cancellation when refunds or deposits require review.
   - Improve billing usability with automated customer and balance population, clear payment state indicators, and server-authoritative validations.

2. **Database Normalization Toward Third Normal Form (3NF):**
   - Address functional dependency violations across legacy tables without breaking historical audit trails or multi-tenant code.
   - Model entities for **Vehicles**, **Services**, **Appointments**, **Estimates**, and **EstimateLines**.
   - Normalize payment classification by isolating **Purpose** (Deposit, Progress, Settlement) and **Payer** (Customer, Insurer) from **PaymentMethod** (Cash, Card, GCash, Bank Transfer).
   - Maintain historical intake snapshots (e.g., inquiry message intake strings and original agreed price snapshots on tasks) while pointing current operational references to canonical normalized foreign keys.

---

## 2. Functional Dependency & Normalization Assessment

### Current Legacy Schema Assessment

| Table | Primary Key | Current Attributes & Transitive Dependencies | 3NF Status & Assessment |
|---|---|---|---|
| `Customers` | `CustomerId` | `FullName`, `ContactNumber`, `Email`, `IsActive`, `CreatedAt` | **3NF Compliant**. No transitive dependencies. |
| `Inquiries` | `InquiryId` | `CustomerId`, `CustomerName`, `ContactNumber`, `VehicleDetails`, `ConvertedJobOrderId`, `InquiryPlatform`, `MessageDetails`, `Status`, `IsActive`, `CreatedAt` | **Violates 2NF/3NF**. Contains repeated customer name/contact and raw vehicle string when `CustomerId` is known. *Decision:* Maintain original intake attributes as explicit intake snapshots (`IntakeCustomerName`, `IntakeContactNumber`, `IntakeVehicleDetails`) for unverified leads, but reference canonical `CustomerId` and `VehicleId` for current customer/vehicle data. |
| `JobOrders` | `JobOrderId` | `CustomerId`, `VehicleDetails`, `InquiryNotes`, `EstimateId`, `TotalEstimatedCost`, `StartedAt`, `CompletedAt`, `Status`, `AssignedMechanicUserId`, `AssignedMechanic`, `CreatedAt` | **Violates 2NF/3NF**. Vehicle details stored as free text. `EstimateId` frequently concatenates estimate number, customer name, and vehicle details (e.g., `EST-2026-001 (Toyota Vios / Brake Overhaul)`). *Decision:* Introduce `Vehicles` table; link `JobOrder.VehicleId`. Introduce `Estimates` table; link `JobOrder.ApprovedEstimateId`. Retain `VehicleDetails` and `EstimateId` as legacy snapshots. |
| `JobTasks` | `JobTaskId` | `JobOrderId`, `ServiceId`, `Remarks`, `Status`, `CreatedAt` | **Violates 3NF**. `ServiceId` currently stores the literal service name/description (e.g., `"Air-conditioning diagnosis"`). *Decision:* Introduce `Services` catalog with numeric `ServiceId` (`ServiceCatalogId`), name, active status, and default price. Store `ServiceCatalogId` on `JobTask` while preserving historical snapshot `ServiceNameSnapshot` and `AgreedPrice`. |
| `Payments` | `PaymentId` | `JobOrderId`, `PaymentType`, `PaymentMethod`, `AmountPaid`, `AllowCustomerCredit`, `IsVoided`, `VoidReason`, `VoidedAtUtc`, `SubmissionId`, `CreatedAt` | **Mixed Semantic Classification**. `PaymentType` combines purpose (`Deposit`, `Downpayment`, `Progress Payment`, `Retail Full`) with implicit customer payer. *Decision:* Add `PaymentPurpose` and `PayerType` columns while preserving `PaymentType` for backwards compatibility. |
| `Complaints` | `ComplaintId` | `CustomerId`, `JobOrderId`, `SourceFeedbackId`, `LegacyCustomerId`, `ComplaintType`, `Description`, `ResolutionStatus`, `ActionTaken`, `IsUrgent`, `UnderWarrantyService`, `IsActive`, `CreatedAt` | **Partially Normalized**. General complaints link to `CustomerId`; job-linked complaints link to `JobOrderId`. *Decision:* Retain dual-path customer resolution with constraint validation. Retain `LegacyCustomerId` for archival reconciliation. |
| `Feedbacks` | `FeedbackId` | `JobOrderId`, `LegacyCustomerId`, `RatingScore`, `PositiveRemarks`, `AreasForImprovement`, `IsActive`, `CreatedAt` | **Partially Normalized**. Customer is derived from `JobOrderId`. `LegacyCustomerId` preserved for archival reconciliation. |

---

## 3. Entity Relationship Architecture (Before vs. After)

### Before Normalization
```
[Inquiry] (CustomerName, ContactNumber, VehicleDetails as strings)
   │
   ├─► [JobOrder] (VehicleDetails, EstimateId as strings)
          ├─► [JobTask] (ServiceId as string)
          ├─► [Payment] (PaymentType mixes purpose and method)
          ├─► [Feedback] (Rating, Remarks) ──► [Complaint] (Urgent low-rating)
          └─► [Complaint] (Job-linked or general)
```

### After Normalization (3NF Focused)
```
[Customer] 1 ──── ∞ [Vehicle]
   │                      │
   │                      │ 1
   ├──────────┐           │
   │ 1        │ 1         ▼ ∞
[Inquiry]     └────► [JobOrder] ∞ ──── 1 [Staff] (AssignedMechanicUserId)
   │                      │
   ├─► [Appointment]      ├─► ∞ [JobTask] ∞ ──── 1 [Service] (Catalog)
   │                      │        └─► Historical Service & Price Snapshot
   │                      ├─► ∞ [Payment] (Purpose, Payer, Method isolated)
   │                      ├─► 0..1 [Estimate] 1 ──── ∞ [EstimateLine]
   │                      ├─► ∞ [Feedback] ──► 0..1 [Complaint]
   │                      └─► ∞ [Complaint]
```

---

## 4. Multi-Phase Implementation Plan

### Phase 1: Safe Baseline Verification & Isolation Setup
- Run `tests/BusinessRules.Smoke`, `tests/Backend.Smoke`, and `tests/Verification.Smoke` to confirm 100% green status.
- Backup `DB_MasterCrm` to local `.bak` file and restore to `DB_MasterCrm_MigrationTest`.
- Ensure multi-tenant migrations (`PlatformMaster`, `TenantCrmDb`) are locked and untouched.

### Phase 2: Schema Normalization & Entity Modeling (Add Phase)
- Define new domain entities in `CRM.domain/entities/`:
  - `Vehicle.cs`: Reusable vehicle entity (`PlateNumber`, `Make`, `Model`, `Year`, `Color`, `Vin`, `CustomerId`).
  - `ServiceCatalogItem.cs` (or `WorkshopService.cs`): Service catalog (`ServiceId`, `Name`, `Description`, `CurrentPrice`, `IsActive`).
  - `Appointment.cs`: Appointment entity (`AppointmentId`, `CustomerId`, `VehicleId`, `InquiryId`, `ScheduledStart`, `ScheduledEnd`, `RequestedServices`, `Status`, `CancellationReason`, `CancelledBy`, `CancelledAt`, `LinkedJobOrderId`).
  - `Estimate.cs` & `EstimateLine.cs`: Quotation header and itemized lines.
- Add navigation properties and foreign keys to `JobOrder`, `JobTask`, `Inquiry`, and `Payment` without dropping existing fields.
- Configure `MasterCrmDbContext.cs` exclusively for these new tables and relations (leaving `TenantCrmDbContext` untouched).
- Create EF Core Migration `20261123_LegacyNormalization3NF` for `MasterCrmDbContext`.

### Phase 3: Data Backfill & Reconciliation
- Backfill `Vehicles` from distinct combinations of parsed `VehicleDetails` from `JobOrders` and `Inquiries`. Link existing `JobOrder.VehicleId` and `Inquiry.VehicleId`.
- Backfill `Services` from `JobServices.Catalog` and distinct `JobTask.ServiceId` values. Map `JobTask.ServiceCatalogId` and capture `AgreedPrice` snapshots.
- Backfill `PaymentPurpose` (`Deposit`, `Progress`, `Settlement`) and `PayerType` (`Customer`) on `Payments`.
- Backfill `Estimates` and `EstimateLines` for existing structured `JobOrders`.
- Generate `LEGACY_3NF_DATA_MAPPING.md` documenting all mappings, conservative fallbacks, and unresolved historical references.

### Phase 4: Business Rules & Workflow Enforcement
- Update `WorkshopRules.cs`:
  - Enforce Appointment status transitions (`Scheduled` → `Checked In`, `Cancelled`, `No Show`).
  - Enforce idempotent conversion of Appointments to Job Orders.
  - Enforce Job Order cancellation rules:
    - Block cancellation if payment > 0 (requires financial refund review).
    - Block cancellation for `Completed` or `Released` jobs.
    - Require authorized actor and cancellation reason.
    - Set unfinished tasks (`Pending`, `Ongoing`) to `Cancelled`.
    - Exclude cancelled jobs from active mechanic queues.
- Ensure all business rule validations apply cleanly to `MasterCrmDbContext`.

### Phase 5: UI & Workflow Simplification
- In `CRM_Revcare`:
  - Remove disabled "Assigned on save" input boxes from creation dialogs; use clear headers like "New Job Order" or "New Customer".
  - Display actual database-generated read-only IDs upon saving (`Job #123`, `RCPT-000123`).
  - Replace unrestricted status combobox with contextual action buttons:
    - **Start Work**
    - **Complete Job**
    - **Release Vehicle**
    - **Cancel Job**
  - Implement dynamic prerequisite hints beside disabled buttons (e.g. *"₱1,500.00 more deposit required before work can start"*).
  - Embed Appointment scheduling & conversion directly inside the Inquiries tab.
  - Maintain zero auto-selection on grids and tabs, preserving scroll and pagination.

### Phase 6: Targeted Verification & Acceptance Testing
- Run targeted automated smoke tests covering all 18 acceptance criteria:
  1. Unique database-generated IDs under concurrent saves.
  2. Read-only ID references after save.
  3. Form preservation and friendly messages on missing customer input.
  4. Appointment cancellation with audit history.
  5. Appointment rescheduling with prior schedule tracking.
  6. Idempotent check-in creating exactly one linked job.
  7. Job cancellation refund safeguards.
  8. Exclusion of cancelled jobs from mechanic workload.
  9. Staff rename resilience.
  10. Historical documents and financial totals matching baseline.
  11. Vehicle ownership decoupled from historical job customer.
  12. Service catalog price changes preserving past job costs.
  13. Zero orphaned foreign keys.
  14. Baseline payment totals matching exactly:
      - Local: ₱724,736.00 (218 payments)
      - Cloud: ₱938,050.00 (400 payments)
  15. Multi-tenant Platform Master and Tenant A remaining 100% unaffected.
- Compile `LEGACY_WORKFLOW_3NF_IMPLEMENTATION_REPORT.md`.
