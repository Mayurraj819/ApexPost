# Week 5: API and SQL Server integration tests

## Scope and test level

`ShipmentApiDatabaseTests` runs the real API pipeline in `WebApplicationFactory<ApiProgram>`:
routing, binding, authorization, ownership filter, exception mapping, production
`ShipmentApiService`, EF Core mappings, unit of work, migrations and real SQL Server.
An independent context verifies persisted state after each workflow. Neither
`DbContext`/`DbSet` nor the shipment service is mocked. There is no MAUI/browser test
and no HTTP call to a running application or external service.

| Test workflow | HTTP evidence | Database evidence |
| --- | --- | --- |
| Create, upload two scans, replay, change content under an existing ID | 201 plus working Location, 200, exact accepted/duplicate/rejected counts, latest scan | One shipment, two linked scans, original content preserved |
| Advance to Delivered, replace details | Four legal PATCH transitions, PUT 409 ProblemDetails | Delivered status and every original editable field preserved |
| Upload under another courier's ID | 403 ProblemDetails | No scan written; shipment retained |
| Courier deletes, then Admin deletes a Created shipment | 403 ProblemDetails, then 204 | Denied attempt preserves shipment; allowed attempt deletes it |

These are four facts, not four new unit tests. The Delivered test verifies the
existing Week 5 GREEN implementation; it does not add that production guard.

## Authentication boundary

`SqlApiFactory` adds an authentication handler that exists only in this test
assembly. A fictional Courier/Admin role and courier ID enter through test headers;
the application's real authorization policies and ownership checks still execute.
JWT signature, issuer, audience, expiry and partner-key behavior remain the concern
of existing `ApexPost.Api.Tests/SecurityTests.cs`. Passing these tests does not prove
real JWT validation. Never copy this handler into a production project.

## Database lifecycle and prerequisites

On Windows, installed SQL Server Express LocalDB is used automatically at
`(localdb)\MSSQLLocalDB`. The automatic instance starts on connection. No application
database, manual seed, running API or pasted JWT is required.

Optional `APEXPOST_TEST_SQLSERVER` selects a **private test server** instead; it
must specify `Database=master` (or `Initial Catalog=master`) and allow creating and
dropping databases. Do not use a production server. Credentials stay in the local
environment/CI secrets and must not be committed. The test never changes this
process environment variable.

Each fact generates a separate `ApexPost_Test_Week5_<guid>` database, runs the real
migrations, uses only fictional data, stops its API host, and drops its own database.
No shared fixture or test ordering is required. Contexts use no connection pooling
so cleanup does not retain pooled test connections. A failed setup attempts cleanup
without replacing the original error. An abruptly killed process can leave its
uniquely named database behind; inspect ownership before manually removing it.

The initial catalog must be master and AttachDBFilename is rejected. Cleanup also
checks the exact generated name before deleting anything. Application connection
strings are replaced for this test host only; application user-secrets are unchanged.

Missing SQL Server is a visible setup failure, not a skip or a business-rule failure.
Non-Windows runners must provide their own private SQL Server. LocalDB is a
development SQL Server engine, not a simulation of production load/availability.
These tests do not claim coverage of deadlocks, ambiguous commits or real network faults.

## Repeatable inputs

Scan IDs, business values and UTC event times are fixed. Randomness is used only
for database isolation and an ephemeral, unused JWT key. There are no sleeps or
delays in assertions. The fixed January 2026 scan time is safely in the past for
this November 2026 assignment; the production future-time validator still reads
the real clock. This stage does not refactor production time dependencies.

## Run and record actual evidence

From the repository root, with the .NET 10 SDK and LocalDB installed:

```powershell
New-Item -ItemType Directory -Force evidence/week5 | Out-Null
dotnet test ApexPost.IntegrationTests -m:1 --filter "FullyQualifiedName~ShipmentApiDatabaseTests" --logger "console;verbosity=normal" 2>&1 | Tee-Object evidence/week5/api-database.txt
```

Expected after an actual successful run: **4 passed, 0 failed, 0 skipped**.
The expectation is not a recorded result. Check the final summary before committing
the log. Run the same command again to check isolation/repeatability if diagnosing
a state-related failure; do not weaken assertions to make a failure pass.

On this package's Linux validation host, compilation and a temporary host/auth
smoke check were possible, but SQL Server was unavailable. Full execution of these
four database tests therefore remains pending on Windows/CI. The temporary smoke
test was not included in the delivered patch or counted as database coverage.

The old Week 3 `SqlServerFact` tests still have their existing environment-variable
skip behavior. This stage does not claim to have modernized those fixtures, finished
Reqnroll scenarios, or completed the entire Week 5 assignment.

## References

- [Microsoft: ASP.NET Core integration tests and mock authentication](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [Microsoft: SQL Server Express LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)
