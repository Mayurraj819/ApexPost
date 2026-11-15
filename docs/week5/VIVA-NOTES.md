# Week 5 viva: five-minute preparation

Speaking outline: strategy, test boundaries, TDD evidence, review decisions and results.

## 0:00–0:50 — Strategy and pyramid

The highest risks were scan replay/collision, partial transaction writes, illegal
shipment changes, authorization, client retry behavior and offline persistence.
I kept xUnit because the existing tests already used it, Theory supports rule
matrices, per-test instances support isolation, and Reqnroll integrates with it.
Changing frameworks would add work without reducing those risks.

My planned new-work pyramid was 70% unit, 25% integration, 5% BDD. The actual 49 new
cases are 22 unit, 19 native/API integration and 8 BDD. I prioritized the missing real
Couchbase/API persistence evidence instead of adding cheap tests to hit a ratio.
The 148 retained cases are mixed levels; project names do not define the boundary.

## 0:50–1:40 — Unit testing, AAA and a mock

In OutboxServiceTests, Arrange sets up IScanRepository.ListPending and
IShipmentUploader.UploadAsync with Moq. Act calls SyncPendingAsync. Assert checks
the returned summary and verifies exactly one Update carrying the expected SyncStatus
and unchanged original fields. Cancellation must propagate with zero updates.

The mock proves the service's decisions and interface interactions. It does not prove
Couchbase serialization, queries or persistence after restart. Real native tests do.
The fake HttpMessageHandler intercepts every client request; no real external HTTP
is used. Client tests inspect exact outgoing fields, acknowledgement IDs, 404/500
ProblemDetails messages and explicit cancellation.

## 1:40–2:30 — Genuine TDD and mutation evidence

The new rule is that Delivered shipment details cannot be replaced. This rule did
not exist at the baseline; backward status transitions already did, so those would
not qualify as a new TDD feature.

RED bb70931 failed because no exception was thrown. GREEN d11b3d8 added the minimal
Delivered guard before changes. REFACTOR 2262c0b extracted setup without changing
assertions. A later API/SQL case proves PUT returns 409 and saved details stay intact.

For Copilot-test mutation evidence, always marking an upload Failed broke the success
Theory; swallowing cancellation broke the cancellation case. Restoring each defect
returned the six outbox cases to green. These were deliberate temporary faults, both restored before the final run.

## 2:30–3:25 — Database integration and BDD

I do not mock DbContext/DbSet query behavior: arranged LINQ-to-Objects can hide SQL
translation, constraints, rowversion and transaction failures. Real SQL tests create,
migrate, seed where needed and delete one private database per case. LocalDB is chosen
automatically on Windows; a missing engine fails rather than silently skipping.

The BDD features are ShipmentCreation and ScanUpload, at API/SQL level. Background
sets up an authenticated courier and fresh data; the weight Scenario Outline runs
three examples. Reqnroll matches steps to bindings, injects one scenario state object,
and executes setup/cleanup hooks. State is not static. Fake credentials allow the
scenario identity while real authorization and service/persistence behavior remain.
Separate signed-JWT tests retain the actual token-validation coverage.

## 3:25–4:15 — Copilot review and a rejected suggestion

Copilot drafted outbox tests and a creation feature with bindings. I kept meaningful
success/failure assertions, fixed invalid sample identifiers and reviewed cancellation
and call verification. I rejected a hardcoded CourierId comparison that could pass
without exercising authentication, and redundant non-null assertions after exception
assertions. The BDD draft also duplicated SQL/auth helpers and missed imports/cleanup.

COPILOT-LOG.md distinguishes the original Copilot drafts from subsequent assisted
work. Prompts, review decisions and mutation logs are committed.
One redundant Setup still occurs after the cancellation action; it cannot change
that already completed action and is a disclosed readability cleanup item.

## 4:15–5:00 — Results and honest limits

The post-SQL-cleanup Windows run reports 197 passed, zero failed and zero skipped.
The build reports zero warnings/errors. Native and SQL tests gave confidence that
the lightweight mocks could not, at a higher setup cost.

The first BDD run also caught my wrong content-type expectation: MVC validation
returned JSON with correct weight errors. Correcting the assertion preserved the
400/error-field/no-write checks rather than changing working production validation.

I would next replace older timer waits and audit polling with controlled time or
completion signals and isolate process-wide test configuration. Those legacy
determinism gaps remain. I have not measured code coverage, and 197 passes do not
establish complete risk coverage. The final PR must preserve RED/GREEN/REFACTOR history.

## Files to keep ready

- TESTING-STRATEGY.md: risk map, 27-class review, boundaries, results and reflection.
- TDD-LOG.md plus the three commits and evidence/week5/tdd.
- COPILOT-LOG.md plus evidence/week5/copilot mutation failures/restorations.
- ShipmentApiDatabaseTests.cs: actual service and fresh-context persisted assertions.
- Both feature files, ShipmentScenarioState and ShipmentScenarioHooks.
- evidence/week5/build-after-sql-cleanup.txt and tests-after-sql-cleanup.txt.
