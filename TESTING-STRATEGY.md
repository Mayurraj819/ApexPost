# ApexPost Week 5 testing strategy

Status: strategy-first plan; implementation and final reflection are pending.

Reviewed baseline: `master` commit `5610e6a97c70d2bd337dbce772f028f72a9c7793` (Week 4 Part D merge). Working branch: `week5-testing`. This document must be committed before new Week 5 test or production changes. The previously supplied grouping image is a planning aid; this document defines the implementation decisions.

## 1. Goal and scope

Build confidence that ApexPost can change without losing scans, corrupting transactions, weakening authorization or breaking clients. Prefer assertions about observable business behavior over a target test count. Preserve functioning Week 3/4 rules and migrations. Change production code only for the chosen new business rule or a justified testability seam, recorded in TDD-LOG.md/change notes.

In scope: domain/application services, validation, SQL and Couchbase repositories at integration level, Web API endpoints, the typed HTTP client, and outbox/sync logic. No MAUI UI/pages/navigation, browser/frontend automation, real external HTTP, email, or production databases. Existing server-side MVC HTTP regressions remain compatibility tests; they are not a new UI-testing workstream or evidence for the Shipment API database path.

## 2. Risk map

| Priority | Risk and consequence | Main evidence |
|---|---|---|
| Critical | Duplicate scan replay or changed-content reuse corrupts delivery history | Pure sync acknowledgement unit tests, real SQL uniqueness/transaction/concurrency integration, scan-sync acceptance |
| Critical | Stock, sale/payment or receipt writes partially commit | Retained commit-boundary unit checks plus SQL rollback/lost-acknowledgement integration |
| Critical | JWT roles or courierId ownership permit unauthorized writes | Real signed-JWT HTTP tests with controlled time; explicit 401/403 and no-write assertions |
| High | Illegal shipment transitions or edits of completed delivery data damage history | Transition/boundary unit matrices; new Delivered-details rule via genuine TDD; HTTP 409 check |
| High | Duplicate tracking numbers or invalid weight/phone enter storage | DTO/domain unit boundaries, real SQL unique constraint, creation acceptance scenarios |
| High | HTTP retries repeat unsafe writes or lose pending scans during an outage | Fake-handler typed-client tests, exact attempt counts, cancellation/circuit tests, unchanged local Pending assertions |
| High | Couchbase serialization, filters or indexes return incorrect local state after restart | Real temporary Couchbase CRUD, reopen/round-trip, SQL++ ordering/filter/group counts, listener lifecycle checks |
| High | Malformed/null JSON or inconsistent per-ID acknowledgements mark unaccepted scans Synced | Source-generated JSON client unit tests and explicit complete-ID validation |
| Medium | API version/SDK drift silently breaks consumers | Retain runtime-vs-committed OpenAPI comparisons and generated-client contract tests |
| Medium | Nondeterministic clocks, shared fixtures or process configuration hide failures | Fixed UTC fixtures, injected time, per-test databases/hosts and no global mutable test state |

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
- Add `ApexPost.UnitTests`: focused new rule/validation/outbox/service/Moq tests, with Domain, Application, Validation and Courier folders.
- Add `ApexPost.IntegrationTests`: actual Couchbase and SQL/API integration, isolated fixture infrastructure and lifecycle tests.
- Add `ApexPost.Specs`: Reqnroll feature files, scenario context, bindings and hooks.

Add the new projects to `ApexPost.slnx`, which excludes the MAUI app; leave `ApexPost.Mobile.slnx` outside this backend test workflow. Do not copy the same test into two projects. Move tests only with a documented source-to-destination mapping and remove the original in the same reviewed change. Keep generated client files untouched.

Every new/refined test uses Arrange, Act, Assert sections and `Method_Scenario_ExpectedResult` naming. For exception tests, the captured action is the Act and Assert.Throws/ThrowsAsync verifies the outcome; also verify unchanged state/no persistence where relevant. Use Theory/InlineData for at least tracking formats and decimal weight boundaries (0.09, 0.1, 70, 70.01), plus transition combinations. Assert types, contents and status/headers, not merely non-null results or absence of an exception.

## 5. Existing-test review (one row per test class)

There are **27 test-bearing classes** at the reviewed baseline. Decisions below are prospective: **Modified** means retain the behavior and perform the described improvement; it does not claim the code is already changed. **Kept** still receives naming/AAA consistency review if touched. No existing tests have been removed or replaced in this strategy-only change. Any later deletion/consolidation must update this table with the exact replacement and reason before deletion.

| File | Test class | Decision | Reason / required change |
|---|---|---|---|
| `ApexPost.Tests/Application/ApplicationServiceTests.cs` | `ApplicationServiceTests` | Modified | Keep all service behaviors; explicit AAA and focused names. Retain useful fakes; add separate Moq tests to prove calls, arguments and no-write failure paths. |
| `ApexPost.Tests/Application/CommitBoundaryTests.cs` | `CommitBoundaryTests` | Kept | Exact-one-commit success and zero-commit failure assertions protect transactional orchestration. SpyUnitOfWork is a test double, not proof of SQL rollback. |
| `ApexPost.Tests/Domain/BusinessRuleTests.cs` | `BusinessRuleTests` | Kept | Protects closed/inactive/cross-branch restrictions, manager actions and identity safety, including state after rejected operations. |
| `ApexPost.Tests/Domain/DomainRegressionTests.cs` | `DomainRegressionTests` | Modified | Retain unique counter/hours, paid-line and refund cases. Standardize AAA/names and document overlapping stock/identity cases before any later consolidation. |
| `ApexPost.Tests/Domain/EntityTests.cs` | `EntityTests` | Kept | Branch normalization and invalid counter numbers are cheap domain regressions. |
| `ApexPost.Tests/Domain/FinalMoneyOrderSafetyTests.cs` | `FinalMoneyOrderSafetyTests` | Modified | Identity masking and stock checks overlap other classes. Retain initially; turn these into focused money-order safety cases or consolidate only with a recorded test-to-test mapping. |
| `ApexPost.Tests/Domain/TransactionTests.cs` | `TransactionTests` | Modified | Keep total/currency and zero-quantity exceptions; replace UtcNow fixture value with a fixed UTC instant. |
| `ApexPost.Tests/Domain/ValueObjectRuleTests.cs` | `ValueObjectRuleTests` | Kept | Opening-hours closing boundary and negative weight remain useful isolated rules. |
| `ApexPost.Tests/Domain/ValueObjectTests.cs` | `ValueObjectTests` | Kept | Keep normalized branch codes and invalid Money/Quantity inputs; add new boundary matrices without duplicating these checks. |
| `ApexPost.Tests/DomainApplicationTests.cs` | `DomainApplicationTests` | Modified | Retain fingerprint mismatch and tiered shipping-price behaviors; separate domain/service concerns and identify overlapping stock/overweight checks. |
| `ApexPost.Tests/Persistence/ModelContractTests.cs` | `ModelContractTests` | Modified | Retain metadata/converter and SQL-translation checks; label them component contracts, not database integration. Use fixed report dates; execution still needs SQL tests. |
| `ApexPost.Tests/Persistence/ReadinessContractTests.cs` | `ReadinessContractTests` | Kept | Snapshot parity, configured retries, readable converters and dirty-tracker guard are valuable no-connection component contracts. |
| `ApexPost.Tests/SqlServer/MigrationUpgradeTests.cs` | `MigrationUpgradeTests` | Modified | Keep upgrade/downgrade preservation; fixed dates and a dedicated automatically created database per case. |
| `ApexPost.Tests/SqlServer/QueryCountTests.cs` | `QueryCountTests` | Modified | Keep bounded report command counts; use fixed seeded report windows and isolated data rather than UtcNow/shared mutations. |
| `ApexPost.Tests/SqlServer/QueryShapeTests.cs` | `QueryShapeTests` | Modified | Retain single/split aggregate equivalence and command bounds; isolate each case through the new database lifecycle. |
| `ApexPost.Tests/SqlServer/RetrySafetyTests.cs` | `RetrySafetyTests` | Modified | Retain lost-acknowledgement and precommit rollback checks; automatic isolated database fixture, with deliberately injected failures rather than real outages. |
| `ApexPost.Tests/SqlServer/SqlPersistenceTests.cs` | `SqlPersistenceTests` | Modified | Keep SQL-specific persistence, procedures, stock races, receipts, privacy and schema replay; fixed data/windows, per-test databases and no environment-dependent silent skips. |
| `ApexPost.Tests/SqlServer/SqlWebTests.cs` | `SqlWebTests` | Modified | Retain legacy server-side MVC/SQL regressions separately; no browser automation added. Remove timing-based audit polling where an observable completion seam can be used; isolate data. |
| `ApexPost.Tests/Web/ProblemDetailsTests.cs` | `ProblemDetailsTests` | Kept | Existing middleware exception-to-status/sanitization matrix is useful isolated behavior. |
| `ApexPost.Tests/Web/WebApplicationFactoryTests.cs` | `WebApplicationFactoryTests` | Modified | Keep legacy HTTP middleware/security regressions; replace process-wide password environment mutation with per-host configuration and use an isolated rate-limit clock/config. |
| `ApexPost.Tests/Web/WebSecurityTests.cs` | `WebSecurityTests` | Kept | Direct middleware/security tests exercise meaningful behavior without browser or production dependencies. |
| `ApexPost.Api.Tests/GeneratedClientTests.cs` | `GeneratedClientTests` | Modified | Keep generated-client serialization, duplicate upload and typed 404 checks; use fixed scan timestamps. Existing MemoryService means this is a contract test, not SQL persistence proof. |
| `ApexPost.Api.Tests/SecurityTests.cs` | `SecurityTests` | Modified | Keep real JWT issuer/audience/signature/lifetime and role/ownership tests; control time and isolate token configuration. Do not replace these with the permissive transport test handler. |
| `ApexPost.Api.Tests/TransportTests.cs` | `TransportTests` | Modified | Keep binding, status/Location, ProblemDetails and OpenAPI snapshot checks; fixed times and clearer arrangements. MemoryService/test authentication remain explicit doubles; add a separate real-database API test. |
| `ApexPost.Api.Tests/WorkflowTests.cs` | `WorkflowTests` | Modified | Keep permitted/forbidden transition matrices and Created-only deletion; improve tracking/decimal-weight boundaries and clear AAA. Existing backward-transition behavior is not a new TDD feature. |
| `ApexPost.Api.Tests/SqlDeliveryTests.cs` | `SqlDeliveryTests` | Modified | Retain real SQL service workflow, scan replay/content collision and concurrency; split broad cases, use fixed scan times, automatic database lifecycle and no configuration-based skip. |
| `ApexPost.Courier.Integration.Tests/CourierTests.cs` | `CourierTests` | Modified | Retain real HTTP resilience pipeline with fake handlers, batch ownership and per-ID outcomes. Split client/sync/resilience classes, replace wall-clock circuit waits with controllable time/scheduling, fixed timestamps, exact exception assertions and explicit AAA. |

Helpers/fixtures are not test classes: SqlServerFixture/SqlServerCollection and custom Fact attributes govern SQL setup; ApiFactory/MemoryService and TransportAuthentication provide HTTP doubles; SecurityTokens supplies JWT material; MemoryScans/Stub/TestTokens and SpyUnitOfWork/in-memory repositories are isolation helpers. Review these alongside their consuming rows. In particular, silent SQL skips and a shared seeded database must not become the Week 5 full-suite acceptance mechanism.

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

## 7. Determinism and automatic setup

Reference environment: the user's Windows .NET 10 machine with SQL Server LocalDB and the Couchbase Lite native runtime. Installed SDK/database engine are prerequisites; no manual database creation, seed, launched API, Swagger token, user secret or environment-variable ceremony should be needed for each test run.

Planned fixture work: discover/start the local test SQL instance automatically on Windows, create a uniquely named `ApexPost_Test_*` database, apply migrations and fixed seed data, then clean up. A deliberately supplied private test connection can be supported on other operating systems/CI. Never default to ApexPostDb or a production connection. Validate deletion targets before cleanup. Give each independent test/scenario its own mutable database (or a proven reset boundary); a race test intentionally uses multiple contexts in the same one-test database. Do not share mutated seed state across unrelated tests.

If the required engine/native dependency is unavailable, the full integration run must fail with actionable setup information rather than silently claim success with skipped cases. A clearly labeled fast/unit-only run may still work without SQL. Existing skip attributes remain at the baseline and must be changed during fixture implementation; this document does not claim automatic setup is already delivered.

Use fixed UTC instants for input/seed/report windows. Inject TimeProvider or another explicit controllable seam where production behavior depends on current time; control the JWT lifetime-validation clock without bypassing the validation being tested. Replace wall-clock circuit-recovery waits and sleep-based assertions with controllable pipeline time/scheduling or observable completion signals. Cancellation tests use explicit signals/tokens rather than depending on the host reaching a duration first. Real database races use barriers, not an assumed execution order. Bounded timeout guards may prevent hangs but are not the business assertion. Generate unique storage names for isolation; do not make expected business results random.

Couchbase tests use a fresh temporary directory, deterministic document IDs/timestamps, disposal before deletion and actual reopen checks. Listener tests signal completion through TaskCompletionSource and a bounded guard, not Thread.Sleep. No singleton/shared static scenario state. Remove process-wide environment mutation from legacy tests. Fake/injected JWT keys are test-only data, not real secrets; never use or print production credentials.

## 8. New coverage and the real API database boundary

Add real Couchbase tests for create/read/update/delete, Pending-at-create, mapper enums/UTC/null notes, malformed stored values, query filtering/order/group counts, indexes, persistence after reopen and listener cleanup. This addresses the Week 4 Part A automated-coverage gap; 19 MemoryScans client tests do not cover these behaviors.

Add an API integration test using WebApplicationFactory<ApiProgram>, test authentication with an explicit Courier role/courierId, the actual ShipmentApiService and a disposable SQL database. Exercise create (201 + Location), fetch, scan upload/replay, then assert persisted rows with a separate DbContext. Do not replace the service with MemoryService in this test. Retain separate actual signed-JWT tests so fake authentication cannot conceal role/lifetime defects. Cover a forbidden request with no database mutation.

## 9. TDD feature and commit evidence

New rule: **a Delivered shipment's editable details cannot be replaced**. `TrackedShipment.ReplaceDetails` at the baseline validates input values but has no Delivered-state guard. Its existing ChangeStatus restrictions already reject Delivered-to-InTransit; therefore that example is NOT eligible as a new feature.

1. First commit this strategy. Establish a separately committed test-project/fixture foundation if required.
2. **RED:** add a compiling test using the existing TrackedShipment API. Advance a shipment through the valid statuses to Delivered, call ReplaceDetails with otherwise valid new details, and expect DeliveryConflictException plus unchanged stored details. Run it and record the actual assertion failure before implementation; commit with a `RED:` prefix.
3. **GREEN:** add only the minimal Delivered-state business guard. Ensure the constructor/Created edits still work and the existing lifecycle remains intact. Run the test and regressions; record the actual result; commit `GREEN:`.
4. **REFACTOR:** improve testability/readability or separate the lifecycle guard from value validation without changing behavior. Run the same suite and commit `REFACTOR:`. Explain the concrete benefit, not a cosmetic claim.
5. Add focused API PUT/409 coverage for the rule. Document requirement, failing test, minimal change, refactor and actual SHAs/output in TDD-LOG.md. Do not manufacture earlier failures, backdate commits, or squash the three evidence commits on merge.

No TDD tests or production implementation are included with this initial document.

## 10. BDD with Reqnroll

Both planned features run at the **API acceptance level**, using production ShipmentApiService, in-process TestServer, scenario-specific disposable SQL data and controlled test authentication. This checks the business-to-HTTP boundary while remaining backend-only. Low-level failure matrices belong in unit/integration tests, not dozens of repeated Gherkin scenarios.

- `ShipmentCreation.feature`: valid fictional creation; duplicate tracking rejected; invalid weight. A Background establishes an authenticated courier/test context. A Scenario Outline with Examples covers business-visible valid/invalid weights.
- `ScanSync.feature`: pending local scans acknowledged by the API; replay skipped; same scan ID with changed content rejected. Use the real temporary Couchbase repository where the scenario claims durable offline state; do not equate an in-memory list with offline persistence. Unit tests cover detailed batching and transport failure branches.

Inject a scenario-state object into bindings/hooks; no static fields or ScenarioContext.Current. BeforeScenario creates the host/database/context, AfterScenario disposes handlers/hosts/native databases and cleans storage even after failure. Steps describe business intent, delegate operations through shared helpers, and assert status, reason and persisted state. Use Reqnroll xUnit integration; there is no baseline SpecFlow work to migrate.

## 11. GitHub Copilot drafting and review

Use **GitHub Copilot** for at least one service-unit-test draft (e.g. OutboxService with Moq) and one Gherkin feature plus bindings. ChatGPT-generated material is not evidence of a Copilot run. Save exact prompts, relevant generated output and kept/changed/rejected decisions in COPILOT-LOG.md. Mark actually assisted classes with a comment; do not mark handwritten code as Copilot-produced.

Reject weak non-null-only assertions, tests that duplicate implementation logic, incorrect expectations/mock setups, wall-clock sleeps and missing negative boundaries. Strengthen accepted drafts with meaningful state/call verification. For two genuinely generated tests, deliberately introduce a small known production defect, run the targeted tests to observe failure, restore the code and rerun green. Record the actual mutation, command and results. Never submit broken production changes or a fabricated mutation result. Copilot output/review and mutation demonstrations remain pending until performed.

## 12. Execution, results and definition of done

Baseline evidence in `evidence/week4-partD/tests-windows.txt` records 99 + 30 + 19 = **148 passed, 0 failed, 0 skipped**. That is historical Week 4 evidence, not a Week 5 run. No Week 5 test total or coverage percentage is claimed yet.

Planned standard commands from the root after projects/fixtures are implemented:

```powershell
dotnet build ApexPost.slnx -m:1
dotnet test ApexPost.slnx --no-build -m:1 --logger trx --results-directory evidence/week5/test-results
```

Test Explorer should discover the same cases. Document a fast unit-only command and its exact scope after project organization is in place. The full suite must report passes/failures/skips explicitly; any unintended skip blocks final acceptance. Log actual environment/prerequisites and run duration. Optional coverage is diagnostic evidence, not a target; explain uncovered risk rather than chase 100%.

Definition of done: strategy precedes tests; review covers every baseline class with no unexplained deletions; named unit/Moq/boundary tests; real repository and API/database integration; deterministic fixtures/time; real TDD commit trail; two Reqnroll features with hooks/injection/outline/background; actual Copilot review and two mutation demonstrations; README commands and fresh results; final reflection and viva preparation. Preserve RED/GREEN/REFACTOR commits when merging week5-testing through a normal merge rather than squash.

## 13. Reflection (complete after execution)

Pending, intentionally not written as a success claim before tests exist. At completion, replace this section with about half a page based on actual observations: defects or regressions caught and by which level; cost/limitations of mocks versus native/database tests; what changed after failed runs/review; which tests gave the most confidence per minute; remaining risks and the next improvement. Link concrete failing/passing evidence and record one real rejected Copilot test for the five-minute viva. Update this document as decisions change rather than retroactively pretending the initial plan was fully delivered.

## References

- Assignment: Week5(1).txt supplied by the trainer.
- Microsoft EF Core testing strategy: https://learn.microsoft.com/ef/core/testing/choosing-a-testing-strategy
- Reqnroll xUnit: https://docs.reqnroll.net/latest/integrations/xunit.html
- Reqnroll context injection: https://docs.reqnroll.net/latest/automation/context-injection.html
- Reqnroll hooks: https://docs.reqnroll.net/latest/automation/hooks.html


## Week 5 BDD implemented feature levels

| Feature | Level | Reason |
| --- | --- | --- |
| ShipmentCreation (5 cases) | API + real SQL Server | Acceptance of valid details, duplicate tracking and weight validation must agree with persisted state. |
| ScanUpload (3 cases) | API + real SQL Server | New, replayed and conflicting scan IDs must preserve actual database history. |

These features use injected scenario state, tagged async hooks and the existing
SQL/auth helpers as linked source. They do not test native outbox execution, MAUI,
JWT cryptography or the typed client's retries. See docs/week5/BDD-STRATEGY.md for
commands, isolation and limitations. Linux compilation and binding-only dry run
were checked; actual Windows/SQL execution must be recorded in evidence/week5/bdd.txt.
This update does not replace the final full-suite result or reflection.
