# Tenant A cloud integration and cutover evidence

Status: NOT READY FOR CUTOVER. Updated November 24, 2026 (Asia/Manila).
This replaces the earlier readiness claims; synthetic simulation is not historical migration evidence.

## Observed destinations and connectivity

| Environment | Platform master | Tenant A destination | Current evidence |
|---|---|---|---|
| Local | `(localdb)\MSSQLLocalDB / RevCare_PlatformMaster` | Intended `DB_MasterCrm` | Read-only access succeeded. Registry still names `RevCare_TenantA`, key `TenantA`. |
| Cloud | `db81188.public.databaseasp.net,1433 / db81188` | `db80735.public.databaseasp.net,1433 / db80735` | Dedicated credentials attempted; neither SQL connection succeeded. Tenant error 10060. |

Internal `*.databaseasp.net` names supplied by the provider are for provider-hosted websites; desktop inspection uses public names. TLS certificate validation remains enabled. No credential guessing or fallback to local is allowed. Credentials are not in this report. Process variables are not encrypted storage; Windows User variables are also plaintext, despite a masked input prompt.

Evidence: `artifacts/tenant-a-inspection.log` (exit 2), `artifacts/tenant-a-local-readiness.log` (exit 0). Cloud Identity users, active memberships, roles, plans, tenant registration and authentication remain UNVERIFIED. TCP reachability or Legacy SQL access would not establish authenticated routing.

Local master has four applied migrations, four users, three memberships, three plans. Tenant A has active OwnerManager membership, active PlanA subscription, and Ready registration. These are local metadata observations, not proof of an authenticated operational Legacy workspace. No current cloud count or overlap claim can be made.

## Local baseline and report correction

Local inspection: 209 customers, 202 vehicles, 226 jobs, 229 tasks, 217 estimates / 219 lines, 232 payments, 3,067 audit entries; valid payments PHP 747,786.00. Inventory table counts use SQL partition metadata; job/payment totals are direct aggregate queries. Local job range is 1001?8304, and direct `WHERE JobOrderId = 1` count is zero. The previous statement that Job #1 exists in both databases is false. Actual cross-database ID intersections remain blocked by cloud connectivity; ranges are not an intersection calculation and IDs need not be contiguous.

Seventeen local jobs match reference strings used by the old verification suite (`EST-TEST-001`, `EST-DEP-TEST`, `EST-OWNER-TEST`, `EST-LEG-01`, `EST-CRUD-%`). Source inspection confirms that assignment/billing and CRUD tests called the active database factory and saved records; another path targeted protected `RevCare_TenantA`. This supports test contamination as a cause, but does not identify every historical run or attribute the full count increase. No records were deleted. Those operational test paths and test seeding switches now fail closed.

The earlier cloud baseline (401 customers, 399 vehicles, 400 jobs, 603 tasks, 154 estimates, 400 payments / PHP 938,050, 4,217 audit entries) is prior reported evidence only, not reverified this session.

## Native backups and migration rehearsal

No matching provider backup was found in `backups/` or `C:/Users/alexi/Downloads`. Expected paths are not evidence that files exist. No actual cloud backup was restored; no real-data migration success is claimed.

Provider procedure, for BOTH db80735 and db81188: Control Panel ? Databases ? select exact database ? Backups ? Create BAK file ? wait until complete ? Download. Keep provider timestamp/name and source identity; suggested local names are `backups/db80735_pre_cutover_<timestamp>.bak` and `backups/db81188_pre_cutover_<timestamp>.bak`. Do not use the previous invented `/backups/` server path or `WITH FORMAT` recipe. See [provider backup instructions](https://help.monsterasp.net/databases/mssql/backup).

New rehearsal tool (compiled, NOT exercised with real backups):

```powershell
dotnet run --project tools/Workshop.Database -- --rehearse-native-backup db80735 "C:\path\db80735.bak"
dotnet run --project tools/Workshop.Database -- --rehearse-native-backup db81188 "C:\path\db81188.bak"
```

It checks SHA256 and native header source/full-backup identity, runs VERIFYONLY, restores with all data/log MOVE entries to a unique `RevCare_Restore_<source>_<GUID>` LocalDB database and unique physical paths, without REPLACE or DROP. Only the tenant copy is migrated using existing MasterCrm migrations. Before/after fingerprints compare every original table/column value except rowversion and migration history, without assuming contiguous keys; CHECKCONSTRAINTS checks violations. Master copy is restored without migration. Copies are retained for review. Unsupported file types fail closed. Local SQL must support the backup's SQL version; SQL cannot restore newer-version backups to an older engine ([provider guidance](https://help.monsterasp.net/databases/mssql/export-local-database)).

This tool does not yet establish complete PK/FK definition equivalence, constraint trust state, receipt semantics or authenticated backend compatibility. Those checks and actual restore logs remain mandatory before approval.

## Migration sequence and compatibility

Local has 12 applied MasterCrm migrations through `20261124100231_AddJobOrderAndTaskArchiving`; its confirmed pending migration is `20261124131444_AddTenantIdentityAndBranchesToMasterCrm`.

If cloud still has the previously reported first ten migrations, the pending sequence is:

1. `20261124033501_AddJobCancellationRefundsAndAppointmentHistory`: creates cancellation requests, refunds and appointment history with relationships and indexes. It does not invent historical approval or payment classifications.
2. `20261124100231_AddJobOrderAndTaskArchiving`: archive flags and metadata on jobs/tasks.
3. `20261124131444_AddTenantIdentityAndBranchesToMasterCrm`: identity singleton `tenant-a`, MAIN branch and nullable BranchId columns/indexes/FKs.

Cloud pending sequence is conditional until actual history is read. Do not apply TenantCrm's independent migration chain on top of the Legacy database. The backend expects additional structures (including staff/service representations, TaskWork and TaskReviews); completing the three MasterCrm migrations alone is not proof of backend compatibility.

`db80735` compatibility-model SaveChanges and SaveChangesAsync now reject writes before connecting. Tenant A login validates identity and all mapped table/column names before issuing a token; an incompatible schema returns 503. This checks presence, not complete SQL type/constraint equivalence. Direct operational Legacy launch is blocked; central login is the default. Explicit Legacy mode is restricted to GUID-named disposable LocalDB demos. Database principals must also prevent clients from independently using privileged SQL credentials; application gates cannot revoke existing SQL permissions.

## Separate controlled cutovers ? NOT APPROVED / NOT EXECUTED

For Local: first obtain timestamped native backups of BOTH `DB_MasterCrm` and `RevCare_PlatformMaster`, VERIFYONLY and restore into new disposable catalogs. Rehearse pending migration 13, then resolve and test backend model compatibility. Intended registry change: tenant-a key TenantA remains; ServerName `(localdb)\MSSQLLocalDB`, DatabaseName `DB_MasterCrm`; application `ConnectionStrings:TenantA` must match. Current protected prototype must remain unchanged.

For Cloud: obtain and restore both provider backups, inspect actual master registration and pending histories, rehearse the confirmed migration sequence on the real restored tenant copy, and resolve backend schema incompatibilities. Intended routing: tenant-a key TenantA ? public host `db80735.public.databaseasp.net,1433`, catalog `db80735`; master ? db81188. Exact existing registry before-values and concurrency token are UNKNOWN, so no safe final UPDATE can yet be approved.

Migration command shape for review after destination verification (do not run now):

```powershell
# Set REVCARE_MASTER_CONNECTION_STRING securely to the reviewed destination.
# First use a disposable restore; operational invocation needs explicit approval.
dotnet ef database update 20261124131444_AddTenantIdentityAndBranchesToMasterCrm --context MasterCrmDbContext --project CRM.infrastructure --startup-project CRM.api
```

Before either operation: stop desktop/API write entry points, block writer credentials or enforce a maintenance window, verify no in-flight operations, take the final coordinated backups and record their hashes. Capture current registry/configuration, migration history, every PK/FK definition, all historical row values, amounts, receipt/refund links, audit history and timestamps. Apply only reviewed migrations, validate tenant identity exactly once (never overwrite a mismatch), compare preservation evidence and schema, then test real central login and all seven improved modules on a disposable rehearsal. Reopen writes only after all checks pass.

Recovery: keep writes frozen; restore the verified matching master and tenant backups to the approved original destinations and restore the captured registry/application configuration. Verify identity and data again before reopening. Recovery itself is an operational mutation covered by the cutover approval; do not use destructive Down migrations as historical recovery.

Request ONE approval only after a concrete package has exact backup hashes, source/target catalog names, migration scripts/hashes, registry before/after values, identity initialization, maintenance window, recovery steps and passing real-data/authenticated-UI evidence. That package is not ready, so no cutover approval is requested now.

## Local initialization incident during this session

A concurrently changed API configuration captured the default PlatformMaster connection before WebApplicationFactory applied its disposable override. One backend run invoked initialization twice against local `RevCare_PlatformMaster` and then failed because the intended disposable master did not exist (process exit -532462766). This was outside the intended read-only operational scope. Source permits refresh of module/plan metadata and Ready/PlanA registration fields; exact value-level changes cannot be reconstructed without a pre-run snapshot. Before/after table counts were unchanged, tenant-a still targets RevCare_TenantA, and DB_MasterCrm counts/payment total remained unchanged. Existing passwords are not reset by the initializer. No automatic rollback was attempted.

Evidence retained in `artifacts/tenant-a-backend-isolation-failure.log`. Fixed by deferred IConfiguration resolution, a resolved/configured master comparison inside initialization, and exact intended test destination assertions BEFORE initialization. Subsequent disposable backend tests passed. No cloud initialization, migration or operational writes were performed.
