# ApexPost Counter Platform — Weeks 3–5

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

Week 5 SQL setup now defaults automatically to installed Windows LocalDB. Each SQL
case owns a separate disposable database; migrations and legacy seed data run in the
test lifecycle, never against ApexPostDb. Missing SQL infrastructure fails clearly
instead of silently skipping. An optional APEXPOST_TEST_SQLSERVER override must
name **master** on a private test server with database-create/drop rights. No override
is needed for the standard Windows run. See the Week 5 section below.

Optional override for a different private test environment:

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


## Week 5 — testing strategy, TDD, BDD and evidence

Verified code revision: `adc53976c978ff1a2f185b1eef0f88284fde9dac`
(`week5-testing`, automatic legacy SQL setup and per-test database isolation).
Windows execution on 15 November 2026 recorded **197 passed, 0 failed, 0 skipped**.
The build recorded **0 warnings and 0 errors**. The committed logs record one full-suite run, rather than a sum of targeted runs
or a Reqnroll dry run.

| Test project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| ApexPost.Tests | 99 | 0 | 0 |
| ApexPost.Api.Tests | 30 | 0 | 0 |
| ApexPost.Courier.Integration.Tests | 19 | 0 | 0 |
| ApexPost.IntegrationTests | 19 | 0 | 0 |
| ApexPost.UnitTests | 22 | 0 | 0 |
| ApexPost.Specs | 8 | 0 | 0 |
| **Total** | **197** | **0** | **0** |

Evidence:
- [Full build after SQL cleanup](evidence/week5/build-after-sql-cleanup.txt), 20.15 seconds.
- [Full suite after SQL cleanup](evidence/week5/tests-after-sql-cleanup.txt).
- [Strategy and reflection](TESTING-STRATEGY.md).
- [TDD history and rule](TDD-LOG.md).
- [Actual Copilot prompts, reviews and mutation evidence](COPILOT-LOG.md).
- [Five-minute viva preparation](docs/week5/VIVA-NOTES.md).
- [Requirement-to-evidence map and remaining limits](docs/week5/REQUIREMENT-COVERAGE.md).

### Run all backend tests

Prerequisites: .NET 10 SDK, installed Windows SQL Server Express LocalDB and the
native Couchbase Lite dependency supplied by the existing package. Open the normal
ApexPost.slnx solution, not ApexPost.Mobile.slnx. No MAUI workload, manually running
API, JWT, application user-secret, manual database creation or seed command is
required for tests. Initial restore may need package-feed access.

From the repository root in PowerShell:

```powershell
dotnet restore ApexPost.slnx
dotnet build ApexPost.slnx --no-restore -m:1 -warnaserror
# Run only after the build succeeds:
dotnet test ApexPost.slnx --no-build -m:1 --logger "console;verbosity=minimal"
```

To reproduce the recorded automatic LocalDB path, use a shell without
APEXPOST_TEST_SQLSERVER set. Do not set REQNROLL_DRY_RUN for the real suite. An
explicit private test-server override is supported on other environments; protect
its credentials in local/CI configuration. No production server may be used.
Missing engines are failures, not silently skipped integration coverage.

To record fresh evidence without overwriting the submitted run:

```powershell
New-Item -ItemType Directory -Force evidence/week5 | Out-Null
dotnet build ApexPost.slnx -m:1 -warnaserror 2>&1 | Tee-Object evidence/week5/build-rerun.txt
# Run only after the build succeeds:
dotnet test ApexPost.slnx --no-build -m:1 --logger "console;verbosity=minimal" 2>&1 | Tee-Object evidence/week5/tests-rerun.txt
```

### Fast and focused runs

```powershell
# 22 isolated tests; no SQL server or real HTTP:
dotnet test ApexPost.UnitTests -m:1
# 15 native Couchbase cases, without SQL:
dotnet test ApexPost.IntegrationTests -m:1 --filter "FullyQualifiedName~CouchbaseRepositoryTests"
# Four real API/SQL cases:
dotnet test ApexPost.IntegrationTests -m:1 --filter "FullyQualifiedName~ShipmentApiDatabaseTests"
# Eight real API/SQL BDD scenarios:
dotnet test ApexPost.Specs -m:1
```

Test Explorer discovers the same cases. The Reqnroll extension helps feature
editing/navigation; the existing NuGet integration supplies generation/runtime.
Do not commit generated obj/*.feature.cs files. Dry runs are binding checks only.

### What the evidence proves and what remains

New coverage includes the Delivered-details rule, Moq outbox interaction/cancellation,
tracking and weight boundaries, fake-handler client contracts, real Couchbase durability
and queries, real API/SQL persisted outcomes, and business-readable Reqnroll scenarios.
SQL helpers create/drop a uniquely named database per test/scenario; native tests own
private temporary directories. No test was deleted to obtain the passing count.

Preserve RED `bb70931`, GREEN `d11b3d8` and REFACTOR `2262c0b` in Git history. Merge
week5-testing through a normal merge commit into this repository's default **master**
branch when ready; do not squash or rebase away the evidence. The assignment calls
the target main; master is this repository's established equivalent. The merge has
not been claimed as completed by these documents.

**Known limitation:** some retained legacy tests still use current time, timer delays,
asynchronous audit polling or process-wide test configuration. SQL state isolation
has been corrected, but a passing run does not establish complete determinism. See
TESTING-STRATEGY.md section 7. No measured code-coverage percentage is claimed.
