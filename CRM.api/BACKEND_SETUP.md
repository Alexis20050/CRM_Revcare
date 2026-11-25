# Master and Tenant A backend

## Implemented boundary

The API now uses two databases:
- **PlatformMasterDbContext**: ASP.NET Identity users/roles, Tenants, Memberships.
- **TenantCrmDbContext**: Customers, Inquiries, JobOrders, JobTasks, Payments, Complaints, Feedbacks, AuditEntries, TenantDatabaseIdentity.

The existing **MasterCrmDbContext** is deliberately retained as the **legacy desktop context**.
WinForms now defaults to Tenant A API mode. See ../TENANT_A.md and ../Start-TenantA.ps1. The legacy context is used only by the explicit --legacy mode and the one-time import tool.
The reviewed business-data import copied db80735 into db81189 without changing the source. Tenant B and Tenant C are disabled.

Workshop mappings and validation are shared by the legacy and tenant contexts, so the 50% deposit,
task progression, release balance, warranty, audit and payment protections apply to the backend.
Payment aggregate validation also excludes voided receipts.

## Configuration

Development defaults in appsettings.Development.json:
- PlatformMaster -> local SQL database RevCare_PlatformMaster
- TenantA -> local SQL database RevCare_TenantA

Production has no default connection strings. Supply these **server-side** configuration keys:
- ConnectionStrings:PlatformMaster
- ConnectionStrings:TenantA

Environment equivalents:
- ConnectionStrings__PlatformMaster
- ConnectionStrings__TenantA

Use separate named databases and database-scoped SQL logins. Keep passwords out of source files.
Use SQL connection-string builder/secret configuration to handle punctuation in passwords.
Use encrypted connections with valid certificate verification for cloud deployment.
Do not point either new context at db80735 or another existing combined desktop database.

Development secrets use UserSecretsId RevCare-Backend-Master-TenantA.
Set these in Visual Studio's Manage User Secrets for CRM.api (do not paste passwords into chat):
- Bootstrap:SuperAdminUsername (optional, defaults to superadmin)
- Bootstrap:SuperAdminPassword (required for initialization)
- Bootstrap:OwnerUsername (optional, defaults to owner)
- Bootstrap:OwnerPassword (required for initialization)

Passwords must pass Identity policy: at least 12 characters with upper/lowercase, digit and punctuation.
Use distinct accounts. Existing passwords are never reset on repeat initialization.

## Initialize deliberately

From the repository root in PowerShell:

    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet run --project CRM.api --no-launch-profile -- --initialize

This explicit command:
1. Rejects identical database targets and inappropriate existing master/business tables.
2. Applies each context's own migrations.
3. Stamps Tenant A's database with tenant-a.
4. Registers Tenant A in the master database using the server-side connection key TenantA.
5. Creates the Super Admin and Owner accounts if absent, and creates the Owner's tenant membership.

It does not reset or seed business transactions. Normal API startup never provisions, migrates, or reseeds.
Initialization spans two databases and is intentionally rerunnable, not a distributed transaction.
If one stage fails, correct configuration and rerun. Bootstrap is an administrative operation; run only one initializer at a time.
Only initialize fresh targets for this rollout. The old TenantCrm migrations are retained for history;
populated old tenant databases need a separate data-reconciliation plan before the new foreign keys are applied.

For reviewed SQL deployment scripts:

    dotnet ef migrations script --idempotent --context PlatformMasterDbContext --project CRM.infrastructure --startup-project CRM.api --output master.sql
    dotnet ef migrations script --idempotent --context TenantCrmDbContext --project CRM.infrastructure --startup-project CRM.api --output tenant-a.sql

Scripts install schemas only; initialization is still needed for the database marker, registration and accounts.
Cloud deployment awaits the new databases and their secret configuration.

## Run and authenticate

    dotnet run --project CRM.api --launch-profile https

Use CRM.api.http as the request guide. POST /auth/login with username, password and tenantId: tenant-a.
The response contains an ASP.NET Core protected bearer access token. Send Authorization: Bearer <accessToken>.
Access tokens expire after 30 minutes. Log in again to renew; refresh-token redemption is not exposed in this phase.
Persist and protect ASP.NET Data Protection keys on the API host; key loss/replacement invalidates existing tokens.
HTTPS is required for deployment. Do not put SQL credentials in desktop requests.

A tenant token is checked against current membership, role, account security stamp, lockout and tenant activation
on each protected request. The server resolves the database from the tenant registration and secret key.
The target database marker must match tenant-a. Client-supplied company IDs or connection strings do not control routing.

## Access in this backend phase

- SuperAdmin: master tenant metadata only, with a platform token (omit tenantId at login).
- OwnerManager and ServiceAdvisor memberships: existing CRM transaction endpoints.
- Other roles: no transaction access yet. Technician assignment-based permissions and the full permission matrix are a later phase.
- Revoked memberships, inactive tenants, wrong passwords and unconfigured tenants are denied.
- Public /companies, /company-databases and arbitrary tenant-routing endpoints were removed.

This is the database segregation and authenticated backend foundation, not the full six-role/module rollout.

## Endpoint summary

- POST /auth/login; GET /auth/me
- GET /master/tenants (SuperAdmin only; no secrets returned)
- /crm/customers, /crm/inquiries, /crm/complaints, /crm/feedbacks
- POST /crm/inquiries/{id}/convert
- GET/POST /crm/job-orders; GET/PUT/DELETE /crm/job-orders/{id}
- GET /crm/job-tasks?jobOrderId=...; PUT /crm/job-tasks/{id}
- GET/POST /crm/payments; PUT /crm/payments/{id}; POST /crm/payments/{id}/void

Job creation takes requested service names and creates Pending tasks automatically.
Tasks cannot be manually reassigned through task updates.
Job/task/payment updates require the returned base64 RowVersion.
New payments require a client-generated SubmissionId GUID; repeat identical submissions return the existing receipt.
Payments cannot be physically deleted. Voiding requires a reason.
Business validation returns 409, input errors 400, unavailable databases 503, missing identity 401, access denied 403.

## Verification

    dotnet run --project tests/Backend.Smoke -c Release
    dotnet run --project tests/BusinessRules.Smoke -c Release
    dotnet build CRM_Revcare.slnx -c Release

Backend tests create isolated temporary LocalDB databases and remove only those databases afterward.
They verify actual SQL table separation, both migration snapshots, bootstrap reruns, authentication,
cross-scope denial, membership revocation, tenant activation, database marker validation,
customer/job/task/payment writes, payment deduplication, stale updates, audit identity and business rules.

## Next cutover (not executed)

1. Provision the two new cloud databases and configure API secrets.
2. Initialize and test them without redirecting the desktop client.
3. Rehearse an audited legacy-data import, preserving relationships, IDs, totals and history.
4. Add WinForms API authentication and transaction calls, then perform a controlled cutover.
5. Implement remaining roles, per-action permissions and offline synchronization separately.

Do not treat existing cloud refresh as offline synchronization.
