# RevCare CRM — Local Multi-Tenant Setup & Run Guide

**Date:** November 2026  
**Solution:** RevCare Auto Hub CRM (.NET 10, Windows Forms, ASP.NET Core Web API, EF Core)  
**Workspace:** `C:\Users\alexi\source\repos\CRM_Revcare`  

---

## 1. Prerequisites

- **.NET 10 SDK** (verified with `dotnet --version`)
- **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`)
- **SQL Server Command Line Utilities** (`sqlcmd`)
- Windows 10/11 x64 environment

---

## 2. Sanitized Local Configuration

The local multi-tenant configuration is stored in `CRM.api\appsettings.Development.json` (and `dotnet user-secrets` for local developer overrides).

### `CRM.api\appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "ConnectionStrings": {
    "PlatformMaster": "Server=(localdb)\\MSSQLLocalDB;Database=RevCare_PlatformMaster;Trusted_Connection=True;TrustServerCertificate=True",
    "TenantA": "Server=(localdb)\\MSSQLLocalDB;Database=RevCare_TenantA;Trusted_Connection=True;TrustServerCertificate=True",
    "TenantB": "Server=(localdb)\\MSSQLLocalDB;Database=RevCare_TenantB;Trusted_Connection=True;TrustServerCertificate=True",
    "TenantC": "Server=(localdb)\\MSSQLLocalDB;Database=RevCare_TenantC;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Bootstrap": {
    "SuperAdminPassword": "<DEVELOPMENT_SUPERADMIN_PASSWORD>",
    "OwnerPassword": "<DEVELOPMENT_OWNER_PASSWORD>",
    "OwnerBPassword": "<DEVELOPMENT_OWNER_B_PASSWORD>",
    "OwnerCPassword": "<DEVELOPMENT_OWNER_C_PASSWORD>"
  },
  "Tenants": {
    "tenant-a": {
      "Name": "RevCare Auto Hub",
      "Address": "123 Main Workshop St."
    },
    "tenant-b": {
      "Name": "RevCare Tenant B",
      "Address": "456 Enterprise Ave."
    },
    "tenant-c": {
      "Name": "RevCare Tenant C",
      "Address": "789 Multi-Branch Blvd."
    }
  }
}
```

> [!NOTE]
> Database credentials are never passed to the Windows Forms client. Authentication occurs strictly via Master endpoints issuing short-lived JWT bearer tokens.

---

## 3. Provisioning Commands

The provisioning process is completely automated, idempotent, and non-destructive.

### Command: Provision All Local Databases

From the solution root:

```powershell
dotnet run --project CRM.api -c Release -- --provision-all-tenants
```

### What This Command Executes:
1. **Master Database Setup (`RevCare_PlatformMaster`):**
   - Applies all EF Core migrations (`PlatformMasterDbContext`).
   - Seeds roles (`SuperAdmin`, `Owner`, `ServiceAdvisor`, `LeadMechanic`, `AutomotiveMechanic`, `DetailingTechnician`).
   - Seeds module catalog (`Customers`, `Inquiries`, `Complaints`, `Feedbacks`, `JobOrders`, `JobTasks`, `Payments`, `Workshop`, `Audit`, `BusinessIntelligence`, `Branching`).
   - Seeds subscription plans (`PlanA`, `PlanB`, `PlanC`).
   - Seeds Super Admin account (`superadmin`).
2. **Tenant A (`RevCare_TenantA`):**
   - Preserves existing operational data (Customer, JobOrder, Payment).
   - Applies pending shared tenant migrations (`TenantCrmDbContext`).
   - Sets singleton identity marker `tenant-a`.
   - Seeds default branch (`MAIN`).
   - Registers tenant with company code `REVC-A` and activates `PlanA`.
   - Seeds Tenant A owner account (`owner`).
3. **Tenant B (`RevCare_TenantB`):**
   - Creates database if missing and applies shared tenant migrations.
   - Sets singleton identity marker `tenant-b`.
   - Seeds default branch (`MAIN`).
   - Registers tenant with company code `REVC-B` and activates `PlanB` (enabling BI entitlements).
   - Seeds Tenant B owner account (`owner_b`).
4. **Tenant C (`RevCare_TenantC`):**
   - Creates database if missing and applies shared tenant migrations.
   - Sets singleton identity marker `tenant-c`.
   - Provisions 3 enterprise branches (`MAIN` - Main Branch, `NORTH` - North Branch, `SOUTH` - South Branch).
   - Registers tenant with company code `REVC-C` and activates `PlanC` (enabling BI and multi-branch entitlements).
   - Seeds Tenant C owner account (`owner_c`).

### Idempotency Guarantee
Running the command multiple times produces identical outcomes: existing databases, tables, records, hashed passwords, and foreign keys are preserved without duplicate key errors or sequence disruptions.

---

## 4. Development Accounts

| Username | Initial Password (Dev) | Scope / Tenant | Role | Assigned Subscription | Key Privileges |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **`superadmin`** | `Test-only!Master2026` | Platform Master | `SuperAdmin` | Platform Admin | Tenant provisioning, module licensing, subscription plan assignments, audit logs |
| **`owner`** | `Test-only!Owner2026` | Tenant A (`tenant-a`) | `Owner` | Plan A | Complete workshop operations, staff management, job orders, payments, voiding |
| **`owner_b`** | `Test-only!OwnerB2026` | Tenant B (`tenant-b`) | `Owner` | Plan B | All Plan A operations + Business Intelligence KPIs, 6 charts, reports, customer retention |
| **`owner_c`** | `Test-only!OwnerC2026` | Tenant C (`tenant-c`) | `Owner` | Plan C | All Plan B operations + Multi-branch management (`/crm/branches`), multi-branch operations |

---

## 5. Running the Solution

### 5.1 Starting the Web API

```powershell
dotnet run --project CRM.api -c Release
```
API endpoints listen on `https://localhost:7088` (or port configured in `launchSettings.json`).

### 5.2 Starting the Windows Forms Client

```powershell
dotnet run --project CRM_Revcare -c Release
```

---

## 6. Verification and Automated Test Suites

### 6.1 Backend Integration & Security Suite (`Backend.Smoke`)

Validates 140 comprehensive checks including:
- Master vs. Tenant schema isolation (no business tables in Master, no identity tables in Tenant).
- RBAC role barriers (ServiceAdvisor, LeadMechanic, DetailingTechnician, AutomotiveMechanic).
- Tenant database routing and cross-tenant barrier enforcement (Tenant A cannot query Tenant B/C).
- Subscription plan boundaries (Plan A blocked from BI; Plan B blocked from branching; Plan C granted multi-branch).
- Payment idempotency, void accounting, and deposit business rules.
- QA workflow, review approvals, inspection checks, and rework loops.

```powershell
dotnet run --project tests/Backend.Smoke -c Release
```
**Expected Output:** `All 140 backend checks passed.` (Exit code 0).

### 6.2 Domain Business Rules Suite (`BusinessRules.Smoke`)

Validates core automotive workshop business rules, financial validations, 3NF schema abstractions, and telephone number normalizations.

```powershell
dotnet run --project tests/BusinessRules.Smoke -c Release
```
**Expected Output:** `All business-rule checks passed.` (Exit code 0).

### 6.3 Compilation Verification

```powershell
dotnet build -c Debug
dotnet build -c Release
```
**Expected Output:** `0 Error(s)` across all 4 projects (`CRM.domain`, `CRM.infrastructure`, `CRM.api`, `CRM_Revcare`).

---

## 7. Files Changed Summary

1. **`CRM.domain`:**
   - Retained core entities (`Customer`, `JobOrder`, `JobTask`, `Payment`, `ServiceOffering`, `TenantStaff`, `AuditEntry`).
2. **`CRM.infrastructure`:**
   - `data/PlatformMasterDbContext.cs`: Added `SubscriptionPlan`, `TenantSubscription`, `SubscriptionAuditEntry` entities, and tenant metadata properties (`CompanyCode`, `ServerName`, `DatabaseName`, `CurrentPlanId`).
   - `data/TenantCrmDbContext.cs`: Added `TenantBranch` (`Branches` table) with code, name, address, contact, and active status.
   - `data/WorkshopRules.cs`: Fixed refund-payment link checks to safely evaluate both `PaymentId` and `JobOrderId`.
   - Migrations:
     - `Migrations/PlatformMaster/20261124062726_AddSubscriptionPlansAndCompanyMaster.cs`
     - `Migrations/TenantCrm/20261124062717_AddTenantBranchFoundation.cs`
3. **`CRM.api`:**
   - `BackendInitialization.cs`: Implemented `ProvisionAllLocalDatabasesAsync` and `ProvisionTenantDatabaseAsync` orchestrators with idempotent seed logic.
   - `Program.cs`: Added `--provision-all-tenants` and `--provision-local` CLI switches.
   - `TenantPermissions.cs`: Added module mapping for `branches` (`Branching`) and `analytics` / `bi` (`BusinessIntelligence`).
   - `CrmEndpoints.cs`: Added `/crm/branches` and `/crm/analytics` endpoints (returns KPIs, active branch count, and 6 core graphs).
   - `MasterAdminEndpoints.cs`: Added `/master/plans` endpoint, tenant subscription detail endpoint `/master/tenants/{tenantId}/subscription`, and included database metadata in tenant queries.
   - `BackendAccess.cs`: Integrated subscription validation into JWT claim generation (`SubscriptionPlan`), `/auth/login`, and `/auth/me`.
4. **`CRM_Revcare`:**
   - `CrmTransactionsForm.Operations.cs`: Added `CancellationReviewRow` and `RefundHistoryRow` models.
   - `Views/CustomerProfileDialogForm.cs`: Adjusted payment job order reference types and complaint action fields.
5. **`tests/Backend.Smoke`:**
   - `Program.cs`: Added `MultiTenantPlanAndRoutingChecks` and configured test in-memory configuration isolation.
6. **Documentation:**
   - `docs/LOCAL_DATABASE_ARCHITECTURE.md`: Created complete database architecture, ERDs, routing diagrams, and gap analysis.
   - `docs/LOCAL_TENANT_SETUP.md`: Created setup instructions, provisioning commands, credentials, and verification guide.

---

## 8. Safety & Compliance Confirmation

- **CRM Legacy `DB_MasterCrm`:** Confirmed preserved with exactly 219 jobs. No modifications were performed.
- **`RevCare_TenantA`:** Confirmed preserved with 1 Customer, 1 JobOrder, and 1 Payment. Backup preserved at `C:\Users\alexi\AppData\Local\Temp\RevCare_TenantA_Baseline.bak`.
- **Cloud Databases (`db80735`, `db81188`, `db81189`):** Confirmed untouched. No cloud migrations were executed.
- **Identity Seeds:** Passwords hashed with ASP.NET Core `PasswordHasher<IdentityUser>`.
