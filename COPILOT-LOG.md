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
| Corrected during editing | Moved Verify calls after Act and removed a duplicate Setup accidentally placed in Assert. These placement errors were introduced while applying the review changes, not attributed to the original Copilot draft. |

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

Pending. A genuine GitHub Copilot feature/bindings draft and its human
review must still be recorded here before Week 5 is complete.