# RevCare Auto Hub CRM — Tenant A Readiness Status

**Updated:** 2026-11-25  
**Local Tenant A Target:** `DB_MasterCrm` (`RevCare_PlatformMaster` master)  
**Cloud Tenant A Target:** `db80735` (`db81188` master)  
**Prototype Catalogs (Frozen / Never Silent):** `RevCare_TenantA`, `db81189`

## Verified Capabilities
- **Database Routing & Safety Guards:** `CRM.api` (`appsettings.Development.json`, UserSecrets, `Program.cs`) routes Tenant A to `DB_MasterCrm` locally and `db80735` in cloud mode, rejecting prototype catalogs and protecting operational/cloud DBs from `--initialize` / `--provision-*` mutations.
- **OwnerManager-Only Reporting:** `Reports.Read` is granted solely to `OwnerManager` and enforced across both `TenantWorkspaceForm` navigation and `/crm/reports/*` + `/crm/analytics` API endpoints (`403 Forbidden` for `ServiceAdvisor`, `LeadMechanic`, `AutomotiveMechanic`, `DetailingTechnician`, `BranchManager`, `SuperAdmin`; `401 Unauthorized` for anonymous).
- **EF Core Migration Compatibility & Legacy Bridge:** Zero `PendingModelChangesWarning` suppressions remain. `MasterCrmDbContext` (`20261124193856_AddTenantWorkforceTablesToMasterCrm`), `TenantCrmDbContext`, and `PlatformMasterDbContext` all report zero pending model changes, and `UseLegacyCatalogTables()` non-destructively bridges `TenantStaff` and `TenantServices` to aligned Legacy databases without altering existing `Services` or FKs.
- **Preservation & Acceptance:** Native `COPY_ONLY` backup + disposable restore of `DB_MasterCrm` verified bit-for-bit PK-linked preservation across all 27 tables (`PHP 747,786.00` valid payments, zero fabricated defaults), full materialization of all 23 `TenantCrmDbContext` entity sets, and end-to-end WinForms + live `CRM.api` acceptance.
- **Test Suites (`Release`):** `Backend.Smoke` (302 checks), `BusinessRules.Smoke`, `Verification.Smoke`, and `Tenant.Ui` all passed after final edits.

## Pending External Prerequisites (No Operational Cutover Performed)
- Dedicated `REVCARE_CLOUD_PLATFORM_CONNECTION` for read-only verification of `db81188`.
- Authoritative native provider backups of `db80735` and `db81188` and decision on local `DB_MasterCrm` (`226` jobs) vs cloud `db80735` (`400` jobs) baseline prior to cutover.
