> **Legacy CRM is temporarily the default desktop mode.** Tenant mode remains available through `./Start-TenantA.ps1` (sign in as `owner`) or the `--tenant` argument. See [Tenant A setup and roles](TENANT_A.md). Legacy and tenant workspaces use separate data stores.

# RevCare Auto Hub CRM â€” Main Transactions

The startup window uses standard WinForms controls and provides seven sections:

| Section | Purpose |
| --- | --- |
| Customers | Customer directory, contact numbers, email, search, and active/inactive status |
| Inquiry | Customer contact, inquiry message, platform, and response status |
| Complaints | Complaint details, optional customer/job references, resolution, and action taken |
| Feedback | Customer/job references, 1â€“5 rating, positive remarks, and improvements |
| Job Orders | Estimate/customer reference, job status, and start date/time |
| Job Tasks (Services) | Existing job order, service reference/name, mechanic notes, and task status |
| Payments (Billing) | Existing job order, payment type, amount, and method |

The first three transaction sections satisfy the original CRM scope. Job Orders, Tasks, and Payments handle business transactions, with Customers acting as a supporting master section.

## Run

Requires a .NET 10-compatible SDK, .NET 10 Windows Desktop runtime, and SQL Server LocalDB (or a configured SQL Server).

```powershell
dotnet run --project CRM_Revcare/CRM_Revcare.csproj
```

In Visual Studio, set `CRM_Revcare` as the startup project and press F5. Stop an existing debugging session before rebuilding. To use the verified Release configuration:

```powershell
dotnet run --project CRM_Revcare/CRM_Revcare.csproj -c Release
```

The default connection is `(localdb)\MSSQLLocalDB`, database `DB_MasterCrm`, using Windows authentication. Override it with `REVCARE_MASTER_CONNECTION_STRING`. The desktop connects directly to `MasterCrmDbContext`; the API does not need to run.

## CRUD workflow

- Click **New / Clear**, fill the fields, and save to create a record.
- Select a grid row to edit its values, then save to update it.
- Delete requires confirmation and permanently removes the selected record. Payment void/delete is permanent deletion, not a soft-void ledger entry.
- Each successful write refreshes that collection's grid and clears its editor. **Refresh List** reads external changes. Switching tabs preserves unfinished edits.
- Required fields use `*`; validation and operation results appear below the grid.
- Tasks and payments must reference an existing job order. Delete related tasks/payments before deleting their job order.
- Amounts must be positive, have at most two decimal places, and fit `decimal(18,2)`.
- Customers: auto-generated CustomerId, FullName, ContactNumber (7-15 digits), optional Email, and active status. Deactivating a customer preserves historical links. Duplicate warnings appear when saving matching contact numbers or names.
- Job Orders require selecting an existing active customer.
- Inquiries can link to a registered customer (details display read-only) or allow unregistered customer entry.
- Job Tasks, Payments, and Feedbacks derive their customer from the selected Job Order.
- Complaints derive their customer from the selected Job Order, or allow selecting a registered customer for general complaints.
- Historical unverified customer references in Complaints and Feedbacks are preserved in LegacyCustomerId columns and are never automatically matched.
- Estimate and service fields accept references or names because no catalog module exists yet.
- CRM creation timestamps use UTC and are preserved on edits. Both active and inactive CRM records are listed.

## Structure

- `CRM.domain`: entities.
- `CRM.infrastructure`: EF contexts, migrations, and tenant infrastructure from the professor's setup.
- `CRM.api`: existing API and migration startup project.
- `CRM_Revcare/CrmTransactionsForm.cs`: main window and CRM editors.
- `CRM_Revcare/CrmTransactionsForm.Operations.cs`: job orders, tasks, and payment editors.
- `CRM_Revcare/CrmCollectionTab.cs`: shared standard-control layout, selection, validation reporting, and EF CRUD.
- `CRM_Revcare/CrmDatabase.cs`: desktop database configuration.

The superseded dashboard, custom drawing controls, old dialogs/API client, legacy NuGet package files, sample weather endpoint, and separate operations form have been removed. Database models, migrations, and existing records are retained. Multi-role staff permissions, tenant-aware desktop routing, balance calculations, and other full-system use cases remain outside this implementation.

## Database setup

For a fresh database:

```powershell
dotnet ef database update --project CRM.infrastructure --startup-project CRM.api --context MasterCrmDbContext
```

EF tooling uses API configuration. For a different database, set `ConnectionStrings__MasterCrm` to the same connection used by the desktop app. The existing operations migration can create the transaction tables or adopt the known legacy schema without deleting rows. Its automatic downgrade is disabled to preserve adopted business data.

## Verification

```powershell
dotnet build CRM_Revcare.slnx -c Release --disable-build-servers -m:1
dotnet build tests/CrmCollections.Smoke/CrmCollections.Smoke.csproj -c Release --disable-build-servers -m:1
dotnet build tests/CoreTransactions.Smoke/CoreTransactions.Smoke.csproj -c Release --disable-build-servers -m:1
dotnet tests/CrmCollections.Smoke/bin/Release/net10.0-windows/CrmCollections.Smoke.dll
dotnet tests/CoreTransactions.Smoke/bin/Release/net10.0-windows/CoreTransactions.Smoke.dll
```

Tests create and remove separate temporary LocalDB databases. They cover CRUD, validation, refresh behavior, selection reset, deletion cancellation, missing records, payment precision, and referential integrity. Native confirmation dialogs and visual layout need manual checking.

## MonsterASP cloud database

The six sections are accessed from the left-side menu. Selecting another section preserves unfinished edits.

Cloud connection settings are stored outside this repository at `%LOCALAPPDATA%\RevCareAutoHub\master-connection.bin`, encrypted for the current Windows user. Connection priority is: `REVCARE_MASTER_CONNECTION_STRING`, saved encrypted settings, then the LocalDB fallback. Remove an old environment override if it should no longer take precedence.

After obtaining the database name and username from the MonsterASP panel, build the API in Release and run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Configure-CloudDatabase.ps1 -Server db80735.public.databaseasp.net,1433 -Database db80735 -UserName db80735 -ApplyMigrations
```

The script prompts for the password without displaying it, verifies an encrypted SQL connection with certificate validation, applies migrations, and saves the encrypted desktop configuration. Restart RevCare afterward. It does not copy existing LocalDB records to the cloud. An existing `REVCARE_MASTER_CONNECTION_STRING` environment override takes precedence over saved cloud settings.


Record IDs are SQL Server IDENTITY(1,1) columns. The read-only Record ID field shows 'Assigned on save' for a new record and its generated ID when selected. Reference fields are never auto-incremented. Deleted IDs are not reused, and gaps in numbering are normal.



## Demonstration data

The cloud database was seeded with 200 fictional transactions: 40 inquiries, 20 complaints, 30 feedback entries, 30 job orders, 50 tasks, and 30 payments. Customer names and contact numbers are fictional but realistically formatted, with vehicle descriptions and service notes. Customer references are synthetic, and related job IDs use the actual database-generated identities. Payments and feedback link to completed sample jobs.

`scripts/Seed-DemoTransactions.ps1` uses the saved encrypted cloud connection and executes `scripts/Seed-DemoTransactions.sql` atomically. It seeds empty transaction tables, makes no changes when the combined total is already 200, and refuses other non-empty totals. It never deletes existing data. Existing LocalDB records are not copied.

The seed runner applies `scripts/Realistic-SampleDetails.sql` to replace the original labelled seed text. Use `-RefreshSampleDetailsOnly` to update those original labelled rows without inserting records. The customer details are invented test data and must not be treated as verified contact information.


## Admin login and cloud sync

The desktop now opens an admin login before showing transactions. The requested account is `admin` with password `admin123`. It is stored in the configured database's existing Identity tables with the Admin role and a salted password hash. Login requires the admin username, a valid password, and Admin role membership. There is no registration screen. Logout returns to the login window after confirmation.

The account has been initialized in MonsterASP. For a newly configured database, initialize it explicitly after applying migrations:

```powershell
dotnet build tests/AdminSync.Smoke/AdminSync.Smoke.csproj -c Release --disable-build-servers -m:1
dotnet tests/AdminSync.Smoke/bin/Release/net10.0-windows/AdminSync.Smoke.dll --initialize-cloud-admin
```

Initialization never resets an existing account's password or grants it a role automatically. The command also verifies that the configured database can authenticate the requested account and load all six transaction collections.

**Refresh Cloud** fetches all six collections asynchronously. Refresh runs when the main window opens, then automatically every 60 seconds after each refresh finishes. The button also allows an immediate manual refresh. A status message reports connection success/failure and retains the last successful refresh timestamp. Failed refreshes retry on the next interval. Refreshes never overlap, defer while a modal confirmation is open, and stop when the window closes. Unsaved editor values are preserved, and read failures leave the current grids intact. Every successful Save/Update/Delete already writes directly to the configured database; Sync retrieves external changes and does not upload unfinished drafts. This is an online database application, without an offline queue or LocalDB-to-cloud replication.

The login gate applies to the desktop application. It does not add authentication to the separate existing API endpoints or change SQL Server login permissions.

Run the admin/sync smoke checks against a disposable LocalDB database:

```powershell
dotnet tests/AdminSync.Smoke/bin/Release/net10.0-windows/AdminSync.Smoke.dll
```

