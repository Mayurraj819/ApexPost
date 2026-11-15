# Week 5 BDD implementation and execution

## Feature levels and purpose

| Feature | Executable cases | Level | Why |
| --- | --- | --- | --- |
| ShipmentCreation | 5: valid creation, duplicate, three invalid weights | API + real SQL Server | Proves HTTP validation, unique tracking, persistence and readable business outcomes together. |
| ScanUpload | 3: new scans, replay, changed content under an existing ID | API + real SQL Server | Proves persisted history remains idempotent and immutable under replay/conflict. |

Gherkin Given/When/Then corresponds to Arrange/Act/Assert. Background expresses a
fresh database and authenticated courier. The invalid-weight Scenario Outline runs
once per example. Bindings share constructor-injected `ShipmentScenarioState`, not
static mutable state. `BeforeScenario("sql")` creates resources and
`AfterScenario("sql")` disposes responses, clients and host before dropping the database.
Setup failure also attempts cleanup. Random names isolate databases; business inputs
and scan timestamps are fixed. There is no dependence on scenario ordering.

These are acceptance tests at API level, not unit tests. Overlap with selected API
integration checks is intentional for the release's high-risk business workflows;
BDD does not repeat every low-level boundary. New unit boundary coverage remains a
separate concern. No old tests are removed or replaced by this stage.

## Reuse and limits

The Specs project source-links `SqlTestDatabase.cs` and `SqlApiFactory.cs` from the
integration test infrastructure. It does not reference another test assembly or
duplicate these implementations. The real application service, unit of work, EF
mapping, SQL provider and migrations execute. Test authentication replaces credential
verification while real authorization/filter behavior remains enabled.

This feature pair does not exercise the actual JWT validator, MAUI UI, device network,
typed client's retry/circuit behavior, or the native offline outbox. ScanUpload is
deliberately named for the server-side behavior it proves. Native Couchbase and outbox
unit/integration tests remain separate evidence; a server upload is not described as
a complete offline-to-server workflow.

The fixtures use installed Windows LocalDB automatically, or the existing private
test-server override `APEXPOST_TEST_SQLSERVER` with `Database=master` and create/drop
rights. They never need application seed data, a manually running API or pasted JWT.
Each scenario creates and migrates its own database and deletes only that database.
SQL Server availability is an infrastructure prerequisite, never a silently skipped test.

The scan event time is fixed to January 2026 for this November 2026 assignment.
The production validator still checks its real clock. This patch does not claim to
have replaced all earlier wall-clock dependencies with TimeProvider.

## Run on Windows / actual SQL Server

```powershell
New-Item -ItemType Directory -Force evidence/week5 | Out-Null
dotnet test ApexPost.Specs -m:1 --logger "console;verbosity=normal" 2>&1 | Tee-Object evidence/week5/bdd.txt
```

Expected after successful execution: 8 passed, 0 failed, 0 skipped. Check the actual
summary before recording it as a result. Test Explorer discovers the same cases.
Your Reqnroll Visual Studio extension supplies editing/navigation support; the
existing Reqnroll.xUnit NuGet package supplies generation and runtime behavior.
Generated feature code stays under obj and must not be committed.

## Binding-only check

```powershell
dotnet test ApexPost.Specs -m:1 -e REQNROLL_DRY_RUN=true --logger "console;verbosity=normal"
```

Dry-run mode checks binding resolution without executing hooks/steps or database
assertions. `reqnroll.json` treats missing/pending steps as errors. Never use the
dry-run output as the full BDD evidence or leave REQNROLL_DRY_RUN enabled in your
environment for the real run. The package's Linux validation is explicitly a dry run;
real database execution there was unavailable.

The installer appends a short feature-level update to TESTING-STRATEGY.md and the
review/prompt to COPILOT-LOG.md, preserving their existing content. This is an
implementation update, not the final results/reflection for the full assignment.

References:
- https://docs.reqnroll.net/latest/automation/context-injection.html
- https://docs.reqnroll.net/latest/automation/hooks.html
- https://docs.reqnroll.net/latest/execution/dry-run.html
