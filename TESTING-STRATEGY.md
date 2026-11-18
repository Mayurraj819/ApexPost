# ApexPost Week 5 testing strategy

Status: implementation and recorded Windows execution reviewed at `adc53976`; 197 passed, 0 failed, 0 skipped, zero build warnings/errors. Legacy determinism limitations and the uncompleted merge are explicitly recorded below.

Reviewed baseline: `master` commit `5610e6a97c70d2bd337dbce772f028f72a9c7793` (Week 4 Part D merge). Working branch: `week5-testing`. The initial strategy was committed first at `8d142b3`, before foundation, RED, GREEN and REFACTOR changes. This revision reconciles that plan with the implemented work; the original plan remains in Git history. The previously supplied grouping image is a planning aid; this document defines the implementation decisions.

## 1. Goal and scope

Build confidence that ApexPost can change without losing scans, corrupting transactions, weakening authorization or breaking clients. Prefer assertions about observable business behavior over a target test count. Preserve functioning Week 3/4 rules and migrations. Change production code only for the chosen new business rule or a justified testability seam, recorded in TDD-LOG.md/change notes.

In scope: domain/application services, validation, SQL and Couchbase repositories at integration level, Web API endpoints, the typed HTTP client, and outbox/sync logic. No MAUI UI/pages/navigation, browser/frontend automation, real external HTTP, email, or production databases. Existing server-side MVC HTTP regressions remain compatibility tests; they are not a new UI-testing workstream or evidence for the Shipment API database path.

## 2. Risk map

| Priority | Risk and consequence | Main evidence |
|---|---|---|
| Critical | Duplicate scan replay or changed-content reuse corrupts delivery history | Pure sync acknowledgement unit tests, real SQL uniqueness/transaction/concurrency integration, scan-sync acceptance |
| Critical | Stock, sale/payment or receipt writes partially commit | Retained commit-boundary unit checks plus SQL rollback/lost-acknowledgement integration |
| Critical | JWT roles or courierId ownership permit unauthorized writes | Real signed-JWT HTTP tests; explicit 401/403 and no-write assertions. Legacy JWT time control remains a limitation in section 7. |
| High | Illegal shipment transitions or edits of completed delivery data damage history | Transition/boundary unit matrices; new Delivered-details rule via genuine TDD; HTTP 409 check |
| High | Duplicate tracking numbers or invalid weight/phone enter storage | DTO/domain unit boundaries, real SQL unique constraint, creation acceptance scenarios |
| High | HTTP retries repeat unsafe writes or lose pending scans during an outage | Fake-handler typed-client tests, exact attempt counts, cancellation/circuit tests, unchanged local Pending assertions |
| High | Couchbase serialization, filters or indexes return incorrect local state after restart | Real temporary Couchbase CRUD, reopen/round-trip, SQL++ ordering/filter/group counts, listener lifecycle checks |
| High | Malformed/null JSON or inconsistent per-ID acknowledgements mark unaccepted scans Synced | Source-generated JSON client unit tests and explicit complete-ID validation |
| Medium | API version/SDK drift silently breaks consumers | Retain runtime-vs-committed OpenAPI comparisons and generated-client contract tests |
| Medium | Nondeterministic clocks, shared fixtures or process configuration hide failures | Fixed inputs in new tests and per-case SQL/native state; remaining legacy clock/configuration coupling is disclosed in section 7. |

## 3. Framework and test pyramid

Keep **xUnit** across the solution, including Reqnroll's xUnit integration. The existing three projects already use it, so changing to NUnit would add migration work without improving the behavioral strategy. Per-test class instances support isolation; IDisposable/IAsyncLifetime provide cleanup; Theory/InlineData express rule matrices; existing runners integrate with Test Explorer and dotnet test. Use fixtures only for immutable resources or factories, never shared mutable business data. Do not introduce a second framework just because a separate training lab used NUnit.

Planned distribution for newly added/refined behavior is approximately **70% isolated unit, 25% integration/component, 5% BDD acceptance scenarios**. This is a guide to feedback cost, not a quota or code-coverage target. Existing 148 cases are not relabeled merely to fit a ratio. BDD cases run at an integration boundary but are counted in their acceptance bucket only, not counted twice. Theory rows and Scenario Outline examples count as separately executed cases when reporting results.

| Level | Behaviors | Boundaries and confidence |
|---|---|---|
| Unit (~70%) | Domain rules, validation, outbox decisions, Moq service orchestration, typed-client JSON/errors/retry safety with fake handlers | No real database or external socket; proves decisions and interactions, not query translation/native persistence |
| Integration/component (~25%) | Real SQL repository/migration/transaction behavior; real Couchbase CRUD/query/restart/listener; WebApplicationFactory + real service/database + test auth; generated contracts | In-process TestServer HTTP is allowed. Native embedded DB/SQL access is integration, not unit. Metadata-only EF checks remain component contracts, not execution proof |
| BDD (~5%) | Small business-facing creation and sync flows through the API | Verifies acceptance outcomes using production services, isolated SQL data and scenario-specific context; avoids duplicating every unit boundary |

## 4. Project organization and naming

Keep the existing projects as documented equivalents rather than rename all historical code and break references:

- `ApexPost.Tests`: existing mixed legacy regressions; add accurate category traits and isolate infrastructure fixtures.
- `ApexPost.Api.Tests`: existing HTTP/security/contract and SQL service regressions.
- `ApexPost.Courier.Integration.Tests`: existing no-network client/pipeline tests. Despite the name, many are unit/component tests using MemoryScans and Stub; the directory name does not prove persistence.
- `ApexPost.UnitTests`: four focused classes for the new rule, shipment boundaries, Moq outbox behavior and typed-client contracts (22 cases).
- `ApexPost.IntegrationTests`: actual Couchbase and SQL/API integration, isolated fixture infrastructure and lifecycle tests.
- `ApexPost.Specs`: Reqnroll feature files, scenario context, bindings and hooks.

All three new projects are included in `ApexPost.slnx`, which excludes the MAUI app; `ApexPost.Mobile.slnx` remains outside this backend test workflow. Do not copy the same test into two projects. Move tests only with a documented source-to-destination mapping and remove the original in the same reviewed change. Keep generated client files untouched.

Every new/refined test uses Arrange, Act, Assert sections and `Method_Scenario_ExpectedResult` naming. For exception tests, the captured action is the Act and Assert.Throws/ThrowsAsync verifies the outcome; also verify unchanged state/no persistence where relevant. Use Theory/InlineData for at least tracking formats and decimal weight boundaries (0.09, 0.1, 70, 70.01), plus transition combinations. Assert types, contents and status/headers, not merely non-null results or absence of an exception.

## 5. Existing-test review (one row per test class)

There were **27 test-bearing classes** at the reviewed baseline. The table now records actual decisions at adc53976. **Kept** means the existing class was retained; an improvement mentioned as a limitation has not been implemented. **Modified** means its test code/lifecycle changed while behavior assertions were retained. No baseline test class was removed or replaced. The strategy-first prospective decisions remain visible in the initial Git revision.

| File | Test class | Decision | Reason / required change |
|---|---|---|---|
| `ApexPost.Tests/Application/ApplicationServiceTests.cs` | `ApplicationServiceTests` | Kept | Retain existing application-service behavior/fakes. New Moq outbox checks are separate; no broad rename or rewrite was performed. |
| `ApexPost.Tests/Application/CommitBoundaryTests.cs` | `CommitBoundaryTests` | Kept | Exact-one-commit success and zero-commit failure assertions protect transactional orchestration. SpyUnitOfWork is a test double, not proof of SQL rollback. |
| `ApexPost.Tests/Domain/BusinessRuleTests.cs` | `BusinessRuleTests` | Kept | Protects closed/inactive/cross-branch restrictions, manager actions and identity safety, including state after rejected operations. |
| `ApexPost.Tests/Domain/DomainRegressionTests.cs` | `DomainRegressionTests` | Kept | Retain domain regressions for branch/counter/staff restrictions, paid lines and refunds; no deletion or cosmetic rewrite. |
| `ApexPost.Tests/Domain/EntityTests.cs` | `EntityTests` | Kept | Branch normalization and invalid counter numbers are cheap domain regressions. |
| `ApexPost.Tests/Domain/FinalMoneyOrderSafetyTests.cs` | `FinalMoneyOrderSafetyTests` | Kept | Retain masking and money-order safety checks despite overlap; no consolidation without a replacement mapping. |
| `ApexPost.Tests/Domain/TransactionTests.cs` | `TransactionTests` | Kept | Retain total/currency and zero-quantity rules; existing UtcNow fixture input is a recorded legacy limitation. |
| `ApexPost.Tests/Domain/ValueObjectRuleTests.cs` | `ValueObjectRuleTests` | Kept | Opening-hours closing boundary and negative weight remain useful isolated rules. |
| `ApexPost.Tests/Domain/ValueObjectTests.cs` | `ValueObjectTests` | Kept | Keep normalized branch codes and invalid Money/Quantity inputs; add new boundary matrices without duplicating these checks. |
| `ApexPost.Tests/DomainApplicationTests.cs` | `DomainApplicationTests` | Kept | Retain stock, identity fingerprint and tiered shipping behavior; do not relabel broad legacy checks as new Week 5 work. |
| `ApexPost.Tests/Persistence/ModelContractTests.cs` | `ModelContractTests` | Kept | Retain model/converter/SQL-translation contracts; no database execution is proven by these no-connection checks. |
| `ApexPost.Tests/Persistence/ReadinessContractTests.cs` | `ReadinessContractTests` | Kept | Snapshot parity, configured retries, readable converters and dirty-tracker guard are valuable no-connection component contracts. |
| `ApexPost.Tests/SqlServer/MigrationUpgradeTests.cs` | `MigrationUpgradeTests` | Modified | Inherits per-case SQL lifecycle; separate downgrade database retained. Existing server/client current-date use remains a limitation. |
| `ApexPost.Tests/SqlServer/QueryCountTests.cs` | `QueryCountTests` | Modified | Each case has fresh seed state; exact bounded report command counts retained. Legacy report windows still depend on current time. |
| `ApexPost.Tests/SqlServer/QueryShapeTests.cs` | `QueryShapeTests` | Modified | Only output helper is constructor-injected; inherits per-case SQL setup. Single/split equality and command-count assertions retained. |
| `ApexPost.Tests/SqlServer/RetrySafetyTests.cs` | `RetrySafetyTests` | Modified | Per-case database replaces shared state; injected lost-acknowledgement/rollback failures and all business assertions retained. |
| `ApexPost.Tests/SqlServer/SqlPersistenceTests.cs` | `SqlPersistenceTests` | Modified | Per-case migrated/seeded database and no silent SQL skip; persistence, procedures, constraints, privacy and schema replay assertions retained. Some date inputs remain current-time based. |
| `ApexPost.Tests/SqlServer/SqlWebTests.cs` | `SqlWebTests` | Modified | Per-case SQL state; server-side MVC/security/audit assertions retained without browser automation. Timing-based audit polling remains a limitation. |
| `ApexPost.Tests/Web/ProblemDetailsTests.cs` | `ProblemDetailsTests` | Kept | Existing middleware exception-to-status/sanitization matrix is useful isolated behavior. |
| `ApexPost.Tests/Web/WebApplicationFactoryTests.cs` | `WebApplicationFactoryTests` | Kept | Keep middleware/security regressions. Process-wide demo-password configuration mutation remains a legacy isolation limitation. |
| `ApexPost.Tests/Web/WebSecurityTests.cs` | `WebSecurityTests` | Kept | Direct middleware/security tests exercise meaningful behavior without browser or production dependencies. |
| `ApexPost.Api.Tests/GeneratedClientTests.cs` | `GeneratedClientTests` | Kept | Keep generated-client wire contracts and typed failures; MemoryService is not SQL evidence. Legacy current-time scan data remains. |
| `ApexPost.Api.Tests/SecurityTests.cs` | `SecurityTests` | Kept | Retain actual signed-JWT issuer/audience/signature/expiry/role tests. Do not replace them with permissive test auth; lifetime clock remains uncontrolled. |
| `ApexPost.Api.Tests/TransportTests.cs` | `TransportTests` | Kept | Retain binding, HTTP status/Location, ProblemDetails and OpenAPI checks with explicit MemoryService/test-auth doubles. New real API/SQL tests are separate. |
| `ApexPost.Api.Tests/WorkflowTests.cs` | `WorkflowTests` | Kept | Keep original permitted/forbidden transitions and Created-only deletion; new precise tracking/weight boundaries live in ShipmentBoundaryTests. |
| `ApexPost.Api.Tests/SqlDeliveryTests.cs` | `SqlDeliveryTests` | Modified | Automatic disposable SQL helper, explicit SQL category and fixed scan timestamp; original CRUD, replay, collision and concurrency assertions retained. |
| `ApexPost.Courier.Integration.Tests/CourierTests.cs` | `CourierTests` | Kept | Keep registered fake-HTTP resilience, batch ownership and per-ID outcomes. New direct-client tests add wire-field and ProblemDetails assertions; legacy real timer waits remain. |

Helpers/fixtures are not test classes: SqlServerFixture/SqlServerCollection and custom Fact attributes govern SQL setup; ApiFactory/MemoryService and TransportAuthentication provide HTTP doubles; SecurityTokens supplies JWT material; MemoryScans/Stub/TestTokens and SpyUnitOfWork/in-memory repositories are isolation helpers. Review these alongside their consuming rows. The previous silent SQL skips and shared seeded collection database were replaced by the per-test lifecycle in adc53976. See docs/week5/SQL-SETUP-CLEANUP.md. Remaining clock/configuration limitations are not described as completed fixes.

## 6. Data access and isolation

| Technique | Use | What it does not prove |
|---|---|---|
| Moq repository/uploader/unit-of-work interfaces | Application/outbox branching, returned values, cancellation, exactly-once calls and no writes after failure | SQL translation, transactions, constraint behavior, Couchbase serialization or native lifetime |
| SQLite in-memory | Optional small relational smoke test only if a provider-neutral model/query is intentionally introduced; hold connection for the test lifetime | SQL Server collation, rowversion, locks, T-SQL, procedures, ExecuteUpdate differences, migrations or retry receipts. Do not replace existing SQL-specific tests with SQLite to make them pass |
| Disposable SQL Server database | Production-provider queries, uniqueness, transaction/locking races, migrations/procedures and the new API E2E test | Production deployment/network capacity or external infrastructure reliability |
| Temporary real Couchbase Lite database directory | Mapper round-trip, reopen durability, CRUD, parameterized SQL++, count/order/filter correctness, indexes and change listener disposal | Server sync/HTTP reliability or multiple devices; those need separate client/API tests |
| EF metadata/ToQueryString component check | Model shape, converters, snapshot parity and translation without connection | Successful SQL execution, actual storage/locking or runtime permissions |

Do **not** mock DbContext/DbSet LINQ queries: LINQ-to-Objects and arranged DbSet results can hide provider translation, collation, constraints and transaction errors. Mock above EF through an existing interface when testing a consumer; use the real provider when testing persistence. No new repository abstraction is required merely to mock an EF query. ShipmentApiService currently depends directly on ApexPostDbContext and IUnitOfWork, so its persistence behavior stays in real-database tests.

Use Moq on real existing interfaces such as IScanRepository/IShipmentUploader for outbox and application repository dependencies for service orchestration. Demonstrate Setup, Returns/ReturnsAsync, It.Is/It.IsAny and Verify(Times.Once/Never/Exactly). Capture arguments to assert the business state sent to persistence. Verify meaningful interactions, not every private implementation call. The existing handwritten fakes need not all be rewritten into mocks.

Use fake HttpMessageHandler instances for 200 JSON, 404 ProblemDetails, 500, validation errors, malformed/null/unknown JSON, timeout and cancellation. Assert error details/status and unchanged Pending state; no unit handler may delegate to real network transport. Retain exact retry counts for HTTP 408/5xx/network faults and exclude other 4xx/unsafe writes. In-process TestServer requests are integration tests, not external HTTP.

## 7. Determinism, automatic setup and remaining limitations

The reference Windows environment has the .NET 10 SDK, LocalDB and the Couchbase
native runtime. These installed engines are prerequisites, not manual per-test data
setup. All SQL tests now use the existing automatic helper: default LocalDB, or an
explicit private-server master connection; generated database names, real migrations,
seed where required and validated ownership before deletion. Missing SQL fails with
setup information rather than skipping. The full post-cleanup run has zero skips.

Each legacy SQL case inherits an instance IAsyncLifetime wrapper; collection
serialization limits resource usage but does not share a mutable database. New API
and BDD cases also own one database per case/scenario. A migration test uses a second
disposable database for downgrade. Race tests intentionally use multiple contexts
inside their own test database. Native Couchbase cases own temporary directories
and dispose before deletion. The native listener uses a completion signal with a
bounded guard, not a sleep as its assertion.

New domain/client/outbox tests use fixed fictional inputs, explicit cancellation
signals and no external network. The scan business timestamps are fixed in the past
for the November 2026 assignment. Storage names can be random for isolation without
making expected business outcomes random.

**Strict determinism is not fully implemented across retained legacy tests.** The
following are known limitations, not implied fixes or new feature coverage:

- CourierTests uses a real delay for circuit recovery and real attempt-timeout timers.
- SqlWebTests polls for asynchronous audit completion; elapsed time can affect it.
- Some old domain/SQL/report/transport fixtures and seeding use current UTC time;
  signed-JWT lifetime tests do not inject a controllable validation clock.
- WebApplicationFactoryTests mutates process-wide demo-account environment settings.
- OutboxServiceTests retains one redundant Setup after the cancellation action. It
  cannot affect that already completed action, but should be removed for clarity.

Next improvements are explicit TimeProvider/scheduler seams for timing behavior,
completion signals for auditing, fixed seed/report windows and per-host rather than
process-wide configuration. These need code changes and their own rerun. A 197-pass
run demonstrates observed correctness on this environment; it cannot prove the
absence of timing/order/configuration coupling across all execution environments.

## 8. Implemented coverage and test boundaries

The 148 baseline cases remain. Week 5 adds **49 executable cases**:

| New class / feature | Cases | Actual boundary |
| --- | ---: | --- |
| DeliveredShipmentTests | 1 | Domain unit; new Delivered-details guard |
| OutboxServiceTests | 6 | Moq repository/uploader; summary, persisted fields, failures, cancellation and empty queue |
| ShipmentBoundaryTests | 11 | Tracking digit/format edge cases and inclusive/just-outside weight limits; rejected update preserves details |
| ApexApiClientContractTests | 4 | Fake HTTP; 200 upload wire fields/acknowledgement, 404/500 problem fields, explicit cancellation |
| CouchbaseRepositoryTests | 15 | Real native CRUD, mapper values, reopen, filters/order/group counts, indexes and listener lifecycle |
| ShipmentApiDatabaseTests | 4 | Real API, production service/UoW/EF/SQL with fake credential verification; persisted state and authorization |
| ShipmentCreation.feature | 5 | Real API/SQL; creation/Location, duplicate preservation and three invalid-weight examples |
| ScanUpload.feature | 3 | Real API/SQL; first upload, exact replay and changed-content rejection |
| **Added total** | **49** | **22 unit + 19 native/API integration + 8 acceptance cases** |

The new-work distribution is approximately **45% unit, 39% integration, 16% BDD**,
rather than the original 70/25/5 planning target. The identified native-persistence
and real-API gaps justified more expensive integration coverage; eight focused BDD
cases were retained for readable acceptance criteria. The ratio is disclosed rather
than changing labels or adding low-value tests to hit a quota. Project names do not
establish the level: the three legacy projects remain mixed unit/component/integration
coverage; whole-suite percentages are not inferred from project names.

The API database factory preserves actual ShipmentApiService, unit of work,
configuration, mappings and SQL provider. A fresh DbContext verifies saved rows.
Its test-only auth handler permits a fictional identity, while real authorization
and ownership checks remain. Actual signed-JWT tests are retained separately.
Fake HTTP handlers never delegate to real network transport. Moq does not prove
native persistence, and metadata/ToQueryString checks do not prove SQL execution.

## 9. TDD evidence

The new rule is **a Delivered shipment's editable details cannot be replaced**.
The baseline already rejected backward status transitions, so that was not claimed
as a new feature. Strategy commit 8d142b3 preceded the test work.

| Stage | Commit | Observed result |
| --- | --- | --- |
| RED | bb70931 | One assertion failure: no DeliveryConflictException was thrown |
| GREEN | d11b3d8 | Minimal guard at the start of ReplaceDetails; targeted test passed |
| REFACTOR | 2262c0b | Extracted delivered-shipment setup helper; assertions/behavior preserved; test passed |

The domain API already supported the test without a new interface or production
rewrite. Later API/SQL PUT coverage verifies 409 and unchanged persisted
details. See TDD-LOG.md and evidence/week5/tdd. Separate commits remain in the pushed
branch and must be preserved during merge; documentation does not manufacture a
second TDD cycle or claim new testability work that did not occur.

## 10. Implemented BDD with Reqnroll

Both features run at **API acceptance level**, using production services, in-process
TestServer, scenario-specific SQL data and test authentication. ShipmentCreation
contains a Background and a Scenario Outline with three invalid-weight examples.
ScanUpload covers new uploads, replay and conflicting content under the same ID.
It intentionally describes server acceptance, not execution of a real local outbox.
Native durability is proven in the separate Couchbase integration tests; no memory
list is represented as durable offline storage.

Bindings share constructor-injected ShipmentScenarioState. Tagged async BeforeScenario
and AfterScenario hooks initialize/dispose resources, including setup-failure cleanup.
There is no static mutable scenario state or ScenarioContext.Current. Existing helper
source is linked into Specs rather than copied or referenced through another test
assembly. Generated feature code stays in obj. The extension supports editing;
Reqnroll.xUnit supplies generation/runtime. No SpecFlow migration was needed.

The initial invalid-weight failures caught an incorrect test media-type expectation:
MVC validation used application/json, while the exception handler used problem+json.
The corrected test still verifies 400, a WeightKg error and no persisted shipment.
The real Windows eight-case run passed; binding-only dry-run output is not used as
business evidence. See evidence/week5/bdd.txt and the final full-run log.

## 11. Copilot drafting, human review and mutations

GitHub Copilot drafted OutboxService unit tests and a creation feature with
context, hooks and bindings. COPILOT-LOG.md records prompts and
kept/changed/rejected decisions; docs/week5/COPILOT-BDD-DRAFT.md preserves the draft.
Assisted classes carry attribution comments. COPILOT-LOG.md distinguishes
Copilot drafts from subsequent assisted implementation and review work.

Review rejected invalid sample identifiers, redundant non-null assertions and a
hardcoded identity assertion that could pass without testing authentication. The BDD
draft also missed imports, duplicated infrastructure and lacked complete cleanup.
Those were corrected while preserving meaningful status/field/no-write assertions.
One redundant post-action Setup remains as a documented readability limitation.

Two temporary production mutations proved generated tests can detect defects:

1. Persist Failed even after a successful upload: targeted Theory produced one
   failure and one pass; restoring the conditional returned all six outbox cases green.
2. Swallow upload cancellation: the targeted case failed because no cancellation was
   thrown; restoring the rethrow returned all six cases green.

See evidence/week5/copilot. Neither defect remains in submitted production code.
The evidence covers these two mutations; no code-coverage measurement was performed.

## 12. Actual results and submission status

Verified code: adc53976c978ff1a2f185b1eef0f88284fde9dac, week5-testing.
Windows evidence supplied on 15 November 2026:

| Project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| ApexPost.Tests | 99 | 0 | 0 |
| ApexPost.Api.Tests | 30 | 0 | 0 |
| ApexPost.Courier.Integration.Tests | 19 | 0 | 0 |
| ApexPost.IntegrationTests | 19 | 0 | 0 |
| ApexPost.UnitTests | 22 | 0 | 0 |
| ApexPost.Specs | 8 | 0 | 0 |
| **Total** | **197** | **0** | **0** |

The build recorded zero warnings/errors in 20.15 seconds. Test logs list per-project
durations (4s, 4s, 7s, 7s, 36s and 281ms); no precise total wall duration was supplied,
so these rounded values are not presented as a measured overall duration.
Evidence: evidence/week5/build-after-sql-cleanup.txt and tests-after-sql-cleanup.txt.
Earlier full/targeted logs are historical milestones, not substitutes for this run.
No code-coverage measurement was supplied.

From the solution root, run the build first and stop on failure:

```powershell
dotnet build ApexPost.slnx -m:1 -warnaserror
dotnet test ApexPost.slnx --no-build -m:1 --logger "console;verbosity=minimal"
```

Fast isolated run: `dotnet test ApexPost.UnitTests -m:1` (22 cases, no SQL).
See README for prerequisites, other focused commands and recording fresh evidence.
Test Explorer discovers the same cases. Default Windows LocalDB needs no environment
assignment; optional private-server configuration must never name a production DB.

The initial strategy, test projects, genuine TDD commits, Moq/native/API coverage,
BDD features and Copilot evidence are included. The feature branch has not yet
been merged. Use a normal merge commit
from week5-testing into the repository's existing default master branch (the brief's
main equivalent); do not squash/rebase away RED/GREEN/REFACTOR history. Remaining
legacy determinism constraints in section 7 are not marked complete.

## 13. Reflection

The most useful outcome was distinguishing a passing assertion from credible
behavioral evidence. The Delivered test first failed because ReplaceDetails accepted
changes after delivery. A small guard made it pass, and the later real API/SQL test
showed that the client receives 409 while stored details remain unchanged. The TDD
history makes that progression inspectable rather than relying on a final green log.

Moq outbox tests gave quick feedback on summary values, state preservation and
cancellation. Mutating successful uploads to Failed, then swallowing cancellation,
caused the intended tests to fail. These checks were inexpensive and gave high
confidence per minute for orchestration, but they could not establish native document
serialization or restart durability. The fifteen real Couchbase cases filled that
gap. Similarly, the API/SQL tests used real services and independent verification
contexts so a correct HTTP response could not conceal a missing database write.

BDD made creation and scan replay expectations readable. It also exposed an error
in the test itself: all three invalid-weight examples expected the wrong media type.
The API returned valid weight errors as JSON. Correcting that expectation while
retaining the status, error-field and no-write assertions was better than weakening
the tests or changing working production validation to satisfy an assumption.

The initial shared SQL fixture and environment-controlled skips were weaknesses even
though a configured run passed. Automatic LocalDB and a fresh migrated/seeded database
per case removed those dependencies. The resulting legacy project run took 36 seconds
versus the earlier logged 10 seconds; stronger isolation has a visible setup cost.
The full post-cleanup run passed 197 tests with no failures/skips and a clean build.

I would next control legacy clocks and scheduling, replace audit polling with an
observable completion signal, and isolate process-wide test configuration. Those
issues remain disclosed. The 70/25/5 target also shifted because real persistence
gaps deserved priority; I would optimize fixture setup only after measuring it and
preserving isolation. For fast service changes the focused unit/mutation checks gave
the best feedback per minute; for persistence, native and SQL tests supplied confidence
that mocks could not. Passing counts guide review but do not establish coverage of
every risk or establish complete determinism.

## References

- Trainer assignment: Week5(1).txt.
- https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy
- https://docs.reqnroll.net/latest/integrations/xunit.html
- https://docs.reqnroll.net/latest/automation/context-injection.html
- https://docs.reqnroll.net/latest/automation/hooks.html
- https://docs.reqnroll.net/latest/execution/dry-run.html
