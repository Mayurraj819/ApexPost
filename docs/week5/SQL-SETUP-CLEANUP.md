# Week 5: automatic legacy SQL setup and per-test isolation

## Problem and resulting behavior

The previous full Windows run passed 197 cases with no skips, but only after setting
APEXPOST_TEST_SQLSERVER. Legacy Fact attributes otherwise silently skipped SQL tests.
The legacy collection also shared a seeded mutable database across independent cases.

The legacy projects now source-link the existing `SqlTestDatabase.cs` helper. They
automatically use installed `(localdb)\MSSQLLocalDB` on Windows when no override is
provided, just like the new API integration and BDD tests. A missing/unavailable SQL
engine is an actionable setup failure, not a skipped test or passing result.
No process-wide environment mutation or manual database/seed setup is introduced.

The helper accepts an explicit private test-server override with Database=master,
rejects AttachDBFilename, generates a new database name, migrates it and drops only
the database it owns. Never point the override at a production server. See
`API-DATABASE-TESTS.md` for the shared helper's lifecycle and safety boundary.

## Per-test lifetime

`SqlServerFixture` remains a seed/context wrapper, but it is no longer a shared
ICollectionFixture. Each of the six legacy SQL classes inherits `SqlServerTestBase`.
xUnit creates one class instance per test case; inherited IAsyncLifetime initializes
and disposes that instance's private fixture. Each case therefore gets its own
migrated/seeded database. Multiple contexts inside one concurrency test intentionally
share that one test's connection.

The SqlServer collection remains serialized to bound LocalDB resource usage. It
provides scheduling, not shared mutable storage. The migration-upgrade test still
creates a separate database for downgrade/upgrade, preserving its seeded fixture.

The existing API SQL workflow now owns the same disposable helper directly. It uses
a fixed fictional scan time and has the SqlServer category. Its CRUD, deduplication,
collision and concurrent-upload assertions remain intact.

## Existing-test review update

| Class | Decision implemented in this stage |
| --- | --- |
| MigrationUpgradeTests | Retained all assertions; per-case fixture; separate upgrade database retained. |
| QueryCountTests | Retained command-count assertions; each case receives fresh seeded data. |
| QueryShapeTests | Retained single/split equality and command counts; inject only the output helper, inherit per-case SQL setup. |
| RetrySafetyTests | Retained commit-acknowledgement and rollback fault injection; no shared database between cases. |
| SqlPersistenceTests | Retained persistence, concurrency, procedure, privacy and schema assertions; per-case data isolation. |
| SqlWebTests | Retained server-side MVC/security/audit assertions; per-case data isolation; no browser test added. |
| SqlDeliveryTests | Retained workflow/concurrent-scan assertions; automatic helper, category, fixed scan timestamp. |

No test is removed or renamed, no new package is required, and no production file
is modified. The two test csproj files compile linked helper source; they do not
reference another test assembly. Original domain/API/outbox TDD commits are unchanged.

## Verification on the LocalDB machine

Build first; if it fails, stop rather than testing stale binaries. To demonstrate
the default LocalDB path, remove the override only from this PowerShell process:

```powershell
Remove-Item Env:\APEXPOST_TEST_SQLSERVER -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force evidence/week5 | Out-Null
dotnet build ApexPost.slnx -m:1 -warnaserror 2>&1 | Tee-Object evidence/week5/build-after-sql-cleanup.txt
dotnet test ApexPost.slnx --no-build -m:1 --logger "console;verbosity=minimal" 2>&1 | Tee-Object evidence/week5/tests-after-sql-cleanup.txt
```

Expected after real execution: 197 passed, 0 failed, 0 skipped, and zero build
warnings/errors. This expectation is not a supplied passing result. If your machine
uses a different private SQL Server, keep its existing override rather than clearing
it; that proves the explicit override path instead of the default LocalDB path.
Do not print or commit credentials.

This stage may take longer because databases are seeded per test instead of once
per collection. That is the deliberate isolation tradeoff. Installed LocalDB is
still an infrastructure prerequisite. Run the fast UnitTests project when SQL is
unavailable; do not describe that targeted run as full integration verification.

Both changed projects were compiled and their test discovery was checked on Linux.
SQL Server was unavailable there, so new fixture execution must be verified on
Windows. The earlier 197-pass log remains historical evidence from before this change.

## Remaining limitations

This patch resolves SQL silent skips and shared database state. It does not claim to
have removed all older wall-clock inputs, JWT lifetime dependencies, process-wide
configuration changes or timing-based audit/circuit polling. Those are separate
determinism work. README/strategy/TDD/Copilot notes and the final reflection still
need reconciliation with the final implementation and new actual run evidence.
