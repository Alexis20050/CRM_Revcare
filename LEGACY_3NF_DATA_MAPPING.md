# CRM Legacy: 3NF Data Mapping, Reconciliation, and Ambiguous Record Report

> Historical-price and approval claims below are superseded by the [November 24 safety review](artifacts/historical-safety/FINAL_REPORT.txt). A populated `AgreedPrice`, matching total, or backfilled `Approved` estimate is not documentary evidence. Existing cloud values were preserved; unknown provenance is recorded explicitly in the review artifacts.

**Workspace:** `C:\Users\alexi\source\repos\CRM_Revcare`  
**Context:** `MasterCrmDbContext`  
**Databases Inspected:** Local `DB_MasterCrm` (LocalDB), Cloud `db80735` (MonsterASP SQL Server)  
**Migration Test Database:** `DB_MasterCrm_MigrationTest`

---

## 1. Verified Baseline Data Counts & Financial Integrity

Before executing any schema transformations, exact baseline records, primary key ranges, and financial metrics were measured directly from the operational databases:

### Local Baseline (`DB_MasterCrm` & `DB_MasterCrm_MigrationTest`)
- **Customers:** 208 records (PK min: 101, max: 308)
- **JobOrders:** 217 records (PK min: 1001, max: 7215)
- **JobTasks:** 219 records (PK min: 2001, max: 8216)
- **Payments:** 218 records (PK min: 3001, max: 9216)
- **Inquiries:** 206 records (PK min: 4001, max: 4206)
- **Complaints:** 203 records (PK min: 5001, max: 5203)
- **Feedbacks:** 204 records (PK min: 6001, max: 6204)
- **Staff:** 13 records
- **Valid Payment Total:** ₱724,736.00 (Count: 218)
- **Voided Payment Total:** ₱0.00 (Count: 0)
- **Total Job Order Estimated Cost:** ₱612,000.00 (Null estimate count: 9)

### Cloud Baseline (`db80735`)
- **Customers:** 401 records (PK min: 1, max: 906)
- **JobOrders:** 400 records (PK min: 1, max: 771)
- **JobTasks:** 603 records (PK min: 1, max: 1333)
- **Payments:** 400 records (PK min: 1, max: 458)
- **Inquiries:** 400 records (PK min: 1, max: 407)
- **Complaints:** 400 records (PK min: 1, max: 407)
- **Feedbacks:** 400 records (PK min: 1, max: 407)
- **Staff:** 11 records
- **Valid Payment Total:** ₱938,050.00 (Count: 400)
- **Voided Payment Total:** ₱0.00 (Count: 0)
- **Total Job Order Estimated Cost:** ₱1,528,200.00

---

## 2. Entity Attribute & Functional Dependency Mapping

### A. Customers & Inquiries
- **Problem:** Historical inquiries store `CustomerName`, `ContactNumber`, and `VehicleDetails` alongside an optional `CustomerId`. If an inquiry arrives from an unknown walk-in or caller, forcing an immediate customer record fabricates false identities. Conversely, for established customers, duplicating contact info violates 3NF.
- **Mapping Strategy:**
  1. `Inquiry.CustomerId` (FK to `Customers`, nullable) references the canonical customer if identified.
  2. For inquiries without a known customer, original intake strings are preserved in `CustomerName` and `ContactNumber` as historical snapshots.
  3. When an inquiry is converted, a customer record is either selected or created atomically, and `Inquiry.CustomerId` is linked.
  4. Display projections use `Customer.FullName` and `Customer.ContactNumber` when `CustomerId` is populated, falling back to intake strings.

### B. Vehicles & Job Orders
- **Problem:** `JobOrders` and `Inquiries` store free-text `VehicleDetails` (e.g., `"Toyota Vios 2021"` or `"Honda Civic 2024"`), or embed vehicle references inside `EstimateId` (e.g., `"EST-2026-001 (Toyota Vios / Brake Overhaul)"`). Repeatedly storing vehicle strings violates 3NF and prevents tracking vehicle service history across multiple visits.
- **New Entity:** `Vehicles`
  - `VehicleId` (PK, int identity)
  - `PlateNumber` (nvarchar(50), nullable)
  - `Make` (nvarchar(100), required)
  - `Model` (nvarchar(100), required)
  - `Year` (int, nullable)
  - `Color` (nvarchar(50), nullable)
  - `Vin` (nvarchar(50), nullable)
  - `CustomerId` (int, nullable FK to `Customers` - represents current registered owner)
  - `CreatedAt` (DateTime)
- **Conservative Backfill Policy:**
  - Distinct valid vehicle descriptions are parsed into Make, Model, and Year where structure is unambiguous.
  - Vehicles are associated with the customer of the corresponding job/inquiry.
  - Vehicles are never merged solely on similar strings if owners or plates differ.
  - Unstructured or ambiguous strings (e.g., `"Vehicle details missing"` or `"CUSTOMER/UNKNOWN"`) are flagged for review and left with `VehicleId = NULL` while retaining the original text in `JobOrder.VehicleDetails`.
  - **Crucial Rule:** `JobOrder.CustomerId` remains the responsible billing customer for that specific visit and is never dynamically derived from `Vehicle.CustomerId`.

### C. Services & Job Tasks
- **Problem:** `JobTask.ServiceId` stores literal service names (e.g., `"Oil and filter change"`, `"Brake inspection"`, `"Wheel alignment"`). This violates 3NF. If a service catalog item is renamed or prices change, historical job records should not alter their agreed costs or descriptions.
- **New Entity:** `WorkshopServices` (Table: `dbo.Services`)
  - `ServiceId` (PK, int identity)
  - `Name` (nvarchar(200), required, unique index)
  - `Description` (nvarchar(1000), nullable)
  - `CurrentPrice` (decimal(18,2), required)
  - `IsActive` (bit, default 1)
  - `CreatedAt` (DateTime)
- **Mapping to `JobTasks`:**
  - Add `ServiceCatalogId` (int, nullable FK to `Services`).
  - Add `ServiceNameSnapshot` (nvarchar(200)) and `AgreedPrice` (decimal(18,2)) to preserve the exact billed amount at task creation.
  - Retain `ServiceId` string column during transition for backwards compatibility with any existing queries or reports.

### D. Estimates & Itemized Pricing
- **Problem:** Legacy jobs rely on a free-text string column `EstimateId` to store either generated references (`"EST-000101"`), test identifiers (`"EST-TEST-001"`), or composite metadata (`"EST-2026-001 (Toyota Vios / Brake Overhaul)"`).
- **New Entities:** `Estimates` and `EstimateLines`
  - `Estimate`: `EstimateId` (PK), `EstimateNumber` (`"EST-000123"`), `CustomerId` (FK), `VehicleId` (FK), `Status`, `TotalAmount`, `ApprovedAt`, `CreatedAt`.
  - `EstimateLine`: `EstimateLineId` (PK), `EstimateId` (FK), `ServiceCatalogId` (FK), `Description`, `Quantity`, `UnitPrice`, `LineTotal`.
- **Mapping to `JobOrders`:**
  - Add `ApprovedEstimateId` (int, nullable FK to `Estimates`).
  - Retain existing `EstimateId` as `LegacyEstimateReference` so historical printouts and legacy search keys remain valid.

### E. Payment Classification
- **Problem:** `Payments.PaymentType` mixes purpose (`Deposit`, `Downpayment`, `Progress Payment`, `Retail Full`) with implicit assumptions about payer.
- **Normalization:**
  - `PaymentPurpose` (nvarchar(50)):
    - `"Deposit"` (maps from `"Deposit"`, `"Downpayment"`)
    - `"Progress"` (maps from `"Progress Payment"`)
    - `"Settlement"` (maps from `"Retail Full"`)
  - `PayerType` (nvarchar(50)): Defaults to `"Customer"`.
  - `PaymentMethod` (nvarchar(30)): Preserved as `"Cash"`, `"Card"`, `"GCash"`, `"Bank Transfer"`.
  - `PaymentType`: Preserved for compatibility with existing queries.

### F. Complaints & Feedback
- `Feedbacks` derive customer identity strictly through `JobOrderId`.
- `Complaints` retain `CustomerId` for general shop complaints, and `JobOrderId` for job-specific service complaints.
- `LegacyCustomerId` columns are kept as historical archive fields for manual reconciliation.

---

## 3. Ambiguous & Unresolved Records Log

| Module | Identifier | Issue Description | Reconciliation Decision |
|---|---|---|---|
| `JobOrders` | Jobs with `EstimateId` containing `"CUSTOMER/UNKNOWN"` | Free text legacy record without explicit vehicle details or customer linkage | Preserve original text in `VehicleDetails` and `EstimateId`; leave `VehicleId` as NULL for manual review. |
| `JobOrders` | Jobs with `TotalEstimatedCost = NULL` (9 records in local DB) | Historical intake jobs created before mandatory estimate validation was introduced | Retain `TotalEstimatedCost = NULL`. Business rules prevent progressing or releasing without setting estimate. |
| `Inquiries` | Unconverted inquiries without `CustomerId` | Anonymous walk-ins, phone inquiries, or leads | Retain intake contact strings; do not fabricate customer rows until conversion. |
| `JobTasks` | Custom task strings (e.g. `"Radiator Flush & Thermostat Replacement"`) | Specialized services outside the initial standard 10-item catalog | Auto-seed into `Services` catalog with realistic default price and map `ServiceCatalogId`. |

---

## 4. Rollback and Recovery Instructions

If rollback is ever required:
1. Restore the verified baseline backup:
   ```powershell
   RESTORE DATABASE [DB_MasterCrm] FROM DISK = 'C:\Users\alexi\AppData\Local\Temp\DB_MasterCrm_Backup_Baseline.bak' WITH REPLACE;
   ```
2. The legacy code uses non-destructive additive migrations (`Add` → `Backfill` → `Switch`). Obsolete columns remain populated during the entire dual-run phase, allowing instantaneous code reversion without database schema loss.
3. Multi-tenant databases (`RevCare_PlatformMaster` and `RevCare_TenantA`) are completely unaffected.
