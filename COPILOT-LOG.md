# Week 5 GitHub Copilot Log

## 1. Outbox service test draft

Tool: GitHub Copilot Chat in Visual Studio.

Context supplied:
- OutboxService.cs
- IScanRepository.cs
- IShipmentUploader.cs
- ShipmentScan.cs
- ApexPost.UnitTests.csproj

### Prompt

Create ApexPost.UnitTests/OutboxServiceTests.cs using xUnit and Moq.

Read the attached production code first. Do not change production code.

Use explicit Arrange / Act / Assert sections, descriptive
Method_Scenario_ExpectedResult names, fixed UTC timestamps and fictional
scan data. No database, network, DateTime.Now or sleep calls.

Cover:
1. A Theory for uploader returning true/false:
   verify Synced/Failed state, summary counts and exactly one upload/update.
2. Uploader throws HttpRequestException:
   mark the scan Failed and report the correct summary.
3. Cancellation before processing:
   propagate cancellation; never upload or update.
4. Cancellation during upload:
   propagate cancellation; never mark the scan Failed or update it.
5. No pending scans:
   return zero counts without upload/update calls.

Demonstrate Moq Setup, ReturnsAsync, It.Is, It.IsAny and Verify with Times.
Verify that updates preserve the original scan fields except SyncStatus.
Use the actual interface signatures.

Add this comment above the test class:
“GitHub Copilot-assisted draft; human review recorded in COPILOT-LOG.md.”

### Generated output

Copilot produced OutboxServiceTests:
- One Theory with two upload-result cases.
- Four Facts covering upload exceptions, cancellation before/during
  processing, and an empty pending queue.
- Six executable cases in total.

The reviewed implementation is committed at:
ApexPost.UnitTests/OutboxServiceTests.cs

### Human review

| Decision | Finding and reason |
|---|---|
| Kept | Success/failure Theory verifies summary counts and persisted sync status. |
| Kept | HTTP failure, cancellation and empty-queue cases exercise different behavior. |
| Kept | Fixed timestamps and mocked dependencies avoid real network/database access. |
| Changed | Replaced invalid scan IDs with scan::{Guid} values and tracking numbers with APX-eight-digit values. |
| Changed | Added the ApexPost.UnitTests namespace. |
| Changed | Disposed CancellationTokenSource instances with using. |
| Changed | Added verification that Update is called exactly once overall, alongside matching-field verification. |
| Changed | Used ThrowsAnyAsync<OperationCanceledException> and checked the propagated cancellation token. |
| Changed | Simplified during-upload cancellation using Task.FromCanceled<bool>. |
| Rejected | Redundant Assert.NotNull after an exception assertion. |
| Corrected during editing | Moved Verify calls after Act. One redundant Setup remains after the cancellation action in the committed test; it cannot affect the completed action and is a readability cleanup item. Placement errors introduced while applying review changes are not attributed to the original Copilot draft. |

Reviewed test result: 6 passed.

Evidence: evidence/week5/copilot/outbox-reviewed.txt

### Limits of these tests

Mocks prove outbox decisions and interface interactions. They do not
prove Couchbase persistence, SQL++ queries, HTTP delivery or native
database lifecycle behavior.

## 2. Mutation demonstration: incorrect successful-upload status

Temporary defect:
Always persist SyncStatus.Failed, including successful uploads.

Target:
SyncPendingAsync_WhenUploaderReturnsResult_ReportsCorrectSummaryAndPersistsSyncState

Observed result:
1 failed, 1 passed. The successful-upload case detected an Update carrying
Failed instead of Synced.

Restored the original conditional status assignment.
Restored result: all 6 outbox cases passed.

Evidence:
- evidence/week5/copilot/mutation-01-failed.txt
- evidence/week5/copilot/mutation-01-restored.txt

## 3. Mutation demonstration: swallowed cancellation

Temporary defect:
Replace the OperationCanceledException rethrow with uploaded = false.

Target:
SyncPendingAsync_WhenCancellationOccursDuringUpload_ThrowsAndDoesNotFailOrUpdateScan

Observed result:
1 failed: the test detected that cancellation was not propagated.

Restored the original rethrow.
Restored result: all 6 outbox cases passed.

Evidence:
- evidence/week5/copilot/mutation-02-failed.txt
- evidence/week5/copilot/mutation-02-restored.txt

Neither temporary production defect was committed. A clean diff for
OutboxService.cs was checked after restoration.

## 4. Gherkin feature and bindings

The Copilot creation draft and recorded review are preserved in docs/week5/COPILOT-BDD-DRAFT.md and docs/week5/COPILOT-BDD-REVIEW.md. Shipment creation is Copilot-assisted. Subsequent work is attributed separately below.

## Week 5 BDD review — shipment creation draft

# Copilot BDD draft review

Source: `docs/week5/COPILOT-BDD-DRAFT.md`, titled
"Week 5 BDD Draft for Shipment Creation". Original SHA-256:
`6c79998e39d613840b6e328b2137a6795d955b949eb79e144dae064cf3131993`.
The original draft is preserved separately from the executable bindings.

## Kept

- Shipment creation, duplicate tracking and invalid-weight examples 0, 0.09 and 70.01.
- Background, Scenario Outline, constructor-injected state and scenario hooks.
- Real API/SQL level, a fresh verification context, working Location assertion,
  field-by-field persistence assertions and no external HTTP calls.
- Invariant-culture parsing of weight examples.

## Changed or rejected

| Draft issue | Review decision and reason |
| --- | --- |
| Creation bindings omit imports for scenario context and `TrackedShipment` | Add the correct namespaces; the proposed code would not compile as supplied. |
| Copies the SQL database and factory/authentication implementation into Specs | Source-link the two existing helper files. There is one implementation and no test-project reference. Keep their existing namespace and internal visibility. |
| Public scenario properties would expose internal helpers when reused | Use internal helper access in the public scenario state class. No production type visibility change is required. |
| Setup stores the database only after constructing the factory | Store each acquired resource immediately and clean up if setup fails. |
| LastResponse is never disposed; factory disposal is synchronous; one cleanup error can prevent database cleanup | Dispose replaced responses and use nested try/finally with async factory disposal before dropping the database. |
| Authentication step only compares a hardcoded CourierId property with a hardcoded literal | Check the configured client role header. Actual authenticated endpoint calls and existing integration role/ownership tests provide behavioral evidence. Do not claim this header assertion tests JWT cryptography. |
| Invalid request described as a "valid" request with invalid weight | Change the step wording to "a shipment request with weight". |
| Only numeric error status is checked | Also assert ProblemDetails; for invalid weight require the WeightKg validation error, avoiding an unrelated 400 passing the scenario. |
| Duplicate check only counts rows | Check that the original persisted details were not overwritten. |
| Broad capture patterns and repetitive request construction | Use Cucumber `{string}` parameters and a fictional request factory. |
| Only one feature exists | Add `ScanUpload.feature`: first upload, exact replay and conflicting content under an existing ID. This second feature and its bindings were added during subsequent assisted review; see tool attribution below. |
| Draft proposes changing visibility of ApiProgram if necessary | Reject unnecessary production change: ApiProgram is already public. |

The reviewed scan bindings deserialize their response once into scenario state;
later assertions reuse the result rather than rereading an already consumed JSON stream.
Both feature files use one injected state instance per scenario and tagged async hooks.

## Tool attribution and evidence

`ShipmentCreation.feature`, `ShipmentCreationSteps`, scenario state and hooks were
adapted from the original Copilot draft. Assisted C# classes carry comments.
`ScanUpload.feature` and `ScanUploadSteps` were developed with ChatGPT assistance
during subsequent review. Other ChatGPT-assisted additions include boundary/client
tests and documentation. These are separate from the GitHub Copilot drafts.
The existing SQL/auth helpers were reused rather than generated anew.

Compilation and an eight-scenario binding dry run were checked on Linux. SQL Server
was unavailable there. A dry-run pass is not evidence that the real API/SQL behavior
passed. Record the actual Windows run in `evidence/week5/bdd.txt` before claiming it.
The earlier two outbox mutation checks remain the assignment's mutation evidence;
no BDD mutation or additional RED/GREEN cycle is claimed here.

## Original Copilot prompt

For ApexPost Week 5, draft ShipmentCreation.feature and its C# Reqnroll bindings in chat only. Do not edit files yet.

Inspect:
- ApexPost.Specs/ApexPost.Specs.csproj
- ApexPost.Api/Contracts/ShipmentContracts.cs
- ApexPost.IntegrationTests/Infrastructure/SqlTestDatabase.cs
- ApexPost.IntegrationTests/Infrastructure/SqlApiFactory.cs
- ApexPost.IntegrationTests/Api/ShipmentApiDatabaseTests.cs

Requirements:
1. Use business-language Given/When/Then.
2. Background: an authenticated courier and an empty isolated test database.
3. Successful creation: HTTP 201, working Location, and persisted shipment.
4. Duplicate tracking number: HTTP 409 and only one persisted shipment.
5. Scenario Outline for invalid weights: 0, 0.09 and 70.01 kg; expect HTTP 400 and no shipment persisted.
6. Use constructor-injected scenario state, never static mutable fields.
7. BeforeScenario/AfterScenario hooks create and dispose resources.
8. Use WebApplicationFactory with real ShipmentApiService and SQL Server.
9. Reuse the existing database/authentication helpers where practical. Explain any project changes required; do not reference another test project.
10. Use fictional data, xUnit assertions, and no running API or pasted JWT.

Show the feature, bindings, scenario context, hooks, and proposed file paths.

The follow-up prompt saved the draft to `docs/week5/COPILOT-BDD-DRAFT.md`
without creating or modifying implementation files. The preserved draft and this
review provide the provenance record.



## Final execution and review correction

The actual Windows BDD run passed all eight scenarios. The first three invalid-weight
examples failed because the reviewed assertion expected application/problem+json for
MVC validation responses. The API returned HTTP 400 with application/json and the
correct WeightKg validation errors. The assertion now distinguishes MVC 400 JSON
from exception-handler 409 problem+json; body and no-write assertions were retained.
The test-expectation correction was made during subsequent review; production
validation behavior was unchanged.

Final evidence after SQL fixture isolation: 197 passed, 0 failed, 0 skipped in
`evidence/week5/tests-after-sql-cleanup.txt`; zero build warnings/errors in
`evidence/week5/build-after-sql-cleanup.txt`. The prior two genuine outbox mutation
checks remain in evidence/week5/copilot; both production defects were restored.
No coverage percentage, additional mutation or complete legacy determinism is claimed.
