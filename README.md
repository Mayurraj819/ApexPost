# ApexPost Counter Platform — Week 3

.NET 10 / C# 14 MVC application, SQL Server + EF Core 10, layered Domain/Application/Infrastructure/Web/Tests. JSON runtime persistence has been removed. No database migration runs automatically on application startup.

## Windows setup (solution root, PowerShell or CMD)

Install a .NET 10 SDK and a private SQL Server Developer/Express/LocalDB instance. The example creates **ApexPostDb**, never a shared training database.

```powershell
dotnet tool restore
dotnet restore ApexPost.slnx
dotnet build ApexPost.slnx --no-restore -warnaserror
dotnet user-secrets set "ConnectionStrings:ApexPost" "Server=(localdb)\MSSQLLocalDB;Database=ApexPostDb;Trusted_Connection=True;TrustServerCertificate=True" --project ApexPost.Web
dotnet user-secrets set "ApexPost:Security:IntegrationApiKeys:0" "REPLACE-WITH-YOUR-LOCAL-KEY" --project ApexPost.Web
dotnet ef migrations script --idempotent --project ApexPost.Infrastructure --startup-project ApexPost.Web --output sql/schema.sql
```

Review **InitialCreate**, **AddStoredProcedures**, **AddMeasuredWorkloadIndexes**, **ReadableCodesAndRetryReceipts** and sql/schema.sql before applying:

```powershell
dotnet ef database update --project ApexPost.Infrastructure --startup-project ApexPost.Web
dotnet run --project ApexPost.Web -- --seed
dotnet run --project ApexPost.Web
```

The seed command is explicit and transactional: 5 branches, 20 staff, 30 positively priced products, 10 zones, 30 rates, 200 historical paid transactions plus postal lines/shipments and payments. Existing configured fictional staff/branch IDs are preserved. Do not import the invalid Week 2 JSON data. Demo opening hours are interpreted in UTC, preserving existing domain semantics. A branch-local timezone policy requires a separate business decision.

## Demo login

Development account names are **clerk**, **manager**, **auditor**, **admin**. Supply passwords through environment variables before starting Visual Studio/the shell, or set their Password keys in user-secrets. No working password is committed.

| Account | Environment variable | Optional user-secret key |
|---|---|---|
| clerk | APEXPOST_CLERK_PASSWORD | ApexPost:DemoAccounts:Accounts:0:Password |
| manager | APEXPOST_MANAGER_PASSWORD | ApexPost:DemoAccounts:Accounts:1:Password |
| auditor | APEXPOST_AUDITOR_PASSWORD | ApexPost:DemoAccounts:Accounts:2:Password |
| admin | APEXPOST_ADMIN_PASSWORD | ApexPost:DemoAccounts:Accounts:3:Password |

Counter clerks can create/pay sales. Corrections require an active Branch Manager at the sale branch (the domain rule is retained). A claimed Administrator role does not bypass the domain's manager check. Stock/product changes require manager/admin policies. Price revisions require Administrator.

## Tests

```powershell
dotnet test ApexPost.slnx
```

SQL tests explicitly skip unless APEXPOST_TEST_SQLSERVER is set. Give it a connection to **master** on a disposable/local test server with create/drop-database permission. The fixture creates ApexPost_Test_<random>, migrates/seeds it, and drops only that database. It refuses a base connection naming ApexPostDb or another database. Never use a shared server.

```powershell
$env:APEXPOST_TEST_SQLSERVER = 'Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True'
dotnet test ApexPost.slnx --filter "Category=SqlServer|Category=WebSql"
```

The environment assignment is PowerShell syntax; in CMD use `set APEXPOST_TEST_SQLSERVER=...`. The SQL tests include real mapping/state round trips, idempotent schema replay, atomic failure rollback, conflicting last-unit checkouts returning HTTP 409, procedures, masked identities, shipping, fixed report query counts, role checks and an audit failure during a successful checkout. Original business-rule tests are preserved.

## Reproduce performance evidence

From the solution root with the same dedicated master test connection:

```powershell
dotnet run --project tools/ApexPost.Evidence
```

This tool creates and deletes **ApexPost_Perf_<random>**, generates fictional scale data, captures actual plans and IO/CPU, tests candidate indexes, saves three generated EF SQL samples, records storage/write costs and demonstrates blocking with two SQL sessions. It never targets ApexPostDb. See PERFORMANCE.md and evidence/. The SSMS companion scripts are in sql/evidence/; the recorded run uses ADO.NET sessions, not screenshots of SSMS.

## Architecture and evidence

- DESIGN.md: dependencies, ER diagram, normalization, SOLID and business decisions.
- docs/adr/0007–0014: eight new decisions, including package security pin.
- PERFORMANCE.md and evidence/: measured results, actual .sqlplan files and raw logs.
- sql/: idempotent schema, three procedures, five reports, atomic sale demonstration and performance scripts.
- IMPLEMENTATION_STATUS.md: tested coverage and remaining review items.

The runtime identity needs narrow table DML and stored procedure EXECUTE, not db_owner. Apply schema changes with a separate deployment identity. See sql/security/RuntimePermissions.sql. The Pkcs package reference pins an existing SqlClient dependency to a patched version; trainer review of this direct package exception is explicitly recorded in ADR 0014. No third-party mapper, mediator, fake-data generator or container package is introduced.

## Post-assessment changes

See [verification instructions](docs/READINESS_VERIFICATION.md) before applying this follow-up.
The varchar-code migration preserves existing numeric data and updates procedures. Back up
ApexPostDb first, and prove the upgrade on a disposable database. Reapply the reviewed runtime
permissions script using the deployment identity so the application can write operation receipts.
The runtime identity must not run migrations or seed data.


## Week 4 Parts B and C — API and generated client

The Week 3 MVC app and Part A offline console still run independently. The new API reuses
Infrastructure registration and its SQL Server DbContext. See API-NOTES.md for the additive
tracking schema and why the counter-sale shipment model is preserved.

Run from the solution root (Visual Studio Developer PowerShell):

```powershell
dotnet tool restore
dotnet restore ApexPost.slnx
dotnet build ApexPost.slnx -warnaserror
dotnet test ApexPost.slnx --no-build
dotnet user-secrets set "ConnectionStrings:ApexPost" "Server=(localdb)\MSSQLLocalDB;Database=ApexPostDb;Trusted_Connection=True;TrustServerCertificate=True" --project ApexPost.Api
dotnet ef database update --project ApexPost.Infrastructure --startup-project ApexPost.Api
dotnet run --project ApexPost.Api --launch-profile https
```

Back up your existing private ApexPostDb before migration; do not run migrations with a runtime
login. Run `sql/security/Week4ApiPermissions.sql` with your deployment identity if using the
restricted ApexPostRuntime role. Connection strings are not committed to appsettings.json.

Open https://localhost:7268/swagger in Development. Try every endpoint with fictional data;
ApexPost.Api.http gives success and failure examples for both versions. Save screenshots under
evidence/week4-partBC. The API does not seed fake counter sales to satisfy tracking POSTs.

**OpenAPI** is the language-neutral API contract (paths, schemas, responses and operation IDs).
**Microsoft.AspNetCore.OpenApi** is .NET's built-in generator, configured with AddOpenApi and
MapOpenApi plus document/operation transformers. **Swagger UI** is the browser interface that
renders that contract and sends Try it out requests. Only the SwaggerUI package is installed;
there is no AddSwaggerGen or second schema generator. Documents/UI are mapped in Development
only; hiding documentation does not authorize requests. Part D adds authentication.

Exports are committed in contracts/v1.json and contracts/v2.json. See VERSIONING.md for breaking
changes, deprecation and the proposed sunset. To regenerate while the API runs:

```powershell
.\tools\Refresh-ApiContracts.ps1
dotnet run --project ApexPost.Client.Console
```

The consumer defaults to https://localhost:7268; override APEXPOST_API_BASE_URL for another
private environment. It registers the generated client through AddHttpClient, creates/fetches a
shipment, uploads two scans, and catches the generated typed exception for a missing shipment.
Keep APX-00000000 absent for that deliberate 404 demonstration. Generated files remain untouched;
custom serialization lives in a partial class. NSwag is pinned in .config/dotnet-tools.json.

SQL tests create/drop separate ApexPost_Test_* databases, never ApexPostDb:

```powershell
$env:APEXPOST_TEST_SQLSERVER = "Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test ApexPost.slnx --no-build
```

Zero skipped SQL tests is required for a complete persistence verification. Inspect the new
migration before applying it. No EnsureCreated or automatic startup migration is introduced.

## Week 4 Part D

See [PARTD-START-HERE.md](PARTD-START-HERE.md) for API security and MAUI setup. Earlier API requests now require a bearer token. Use ApexPost.Mobile.slnx for the native app; ApexPost.slnx is the portable server/test solution.
