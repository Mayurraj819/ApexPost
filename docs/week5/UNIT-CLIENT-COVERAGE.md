# Week 5: focused unit and client coverage

This stage adds 15 cases in two new classes. No existing tests or production code
are removed, and no project/package changes are required.

| Class | Cases | Behavior and level |
| --- | --- | --- |
| ShipmentBoundaryTests | 11 | Domain unit tests: valid tracking digit boundaries, null/case/length/newline/non-ASCII rejection, inclusive weight limits and preservation after a rejected detail replacement. |
| ApexApiClientContractTests | 4 | Typed-client unit tests: 200 acknowledgement and exact outgoing fields, 404/500 ProblemDetails status/title/detail/field-error mapping, deterministic in-flight caller cancellation. |

The two parameterized business rules are tracking format and weight bounds. Valid
weight limits are exactly 0.1 and 70 kg; invalid values are 0.099999 and 70.000001.
The rejected-update test checks all editable details and status remain unchanged.
AAA comments distinguish setup, action and assertions. Inputs are fictional and
fixed; no sleeps, shared mutable state, database or external HTTP is involved.

## Existing test review update

| Existing class | Decision | Reason |
| --- | --- | --- |
| ApexPost.Api.Tests.WorkflowTests | Kept | Existing transition rules and coarse invalid inputs remain useful regressions. New unit cases add precise boundary/format and no-partial-update evidence. No duplicate copy of the transition matrix is added. |
| ApexPost.Courier.Integration.Tests.CourierTests | Kept for now; determinism work still pending | Existing tests exercise registered authentication/resilience/sync behavior and retry counts. The new direct-client tests isolate protocol and cancellation behavior. Existing wall-clock circuit/timeout tests still need review before claiming fully deterministic coverage. |

This supplements, rather than replaces, the strategy's per-class baseline review.
The new tests were prepared in ChatGPT, not drafted by GitHub Copilot. Keep the actual
Copilot outbox and feature-generation evidence separate and accurately attributed.

## Fake HTTP boundary

The fake HttpMessageHandler intercepts every send. `apexpost.invalid` is an inert
fictional base URI; no network handler is used. The real typed client serializes
requests and reads the fake JSON response. A stable scan ID and timestamp prove
the request's idempotency marker and payload survive serialization. ProblemDetails
assertions check individual expected fields, not only the exception type.

Cancellation occurs inside the fake send, with Task.FromCanceled using the actual
linked token received from the client. This avoids real timers. The client owns a
linked overall cancellation token, so asserting equality with the caller's token
would be incorrect; the test checks cancellation is propagated.

These tests do not exercise the registered Polly retry/breaker pipeline, bearer
token validation, real server responses or persistence. Those remain the concern
of existing resilience/security and real API/SQL integration tests. The client test
must not be used to claim SQL, native Couchbase or MAUI coverage.

## Run the targeted cases

```powershell
New-Item -ItemType Directory -Force evidence/week5 | Out-Null
dotnet test ApexPost.UnitTests -m:1 --filter "FullyQualifiedName~ShipmentBoundaryTests|FullyQualifiedName~ApexApiClientContractTests" --logger "console;verbosity=normal" 2>&1 | Tee-Object evidence/week5/unit-client.txt
```

Expected: 15 passed, 0 failed, 0 skipped. An actual targeted Linux run passed all
15 cases; run on Windows and save your own evidence before recording its result.
This count excludes the earlier Delivered and outbox tests and is not the full suite.

Remaining work includes the full current-branch audit, older fixture/time-dependency
decisions, zero-warning full build, actual full-suite totals, final README/strategy
reflection, and preserving TDD commit history during the PR/merge.
