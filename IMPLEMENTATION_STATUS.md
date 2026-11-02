# Week 3 implementation status

Baseline: uploaded Week 2 ZIP at 2df57ac; assessment supplied by user: 76/100. Original uploaded ZIP was left unchanged. This is a separate Week 3 source package, not a pushed GitHub branch.

## Verified in this implementation run

- .NET SDK 10.0.100; C# 14, nullable enabled, warnings-as-errors.
- Solution build: **0 warnings, 0 errors**. Raw output: evidence/build.txt.
- Full suite against disposable SQL Server 17.0.5005.3: **84 passed, 0 failed, 0 skipped**. Raw output: evidence/tests.txt.
- Eight original domain/rule test files are byte-for-byte unchanged; hashes: evidence/original-rule-tests.json.
- Three generated migrations and reviewed SQL additions; idempotent schema script replayed twice against the migrated test database.
- Genuine four-workload performance runs on 1000 branches, 30,000 stock rows, 100,200 transactions, 33,433 shipments and 10,000 money orders. 22 actual plan files, IO/CPU logs, storage snapshots, generated EF SQL and blocking observations are included.

## Requirement coverage

| Part | Implemented artifacts / validation |
|---|---|
| A | 18 principal tables including Regions, normalized references, keys/relationships/delete behavior, unique/check/default constraints, audit columns, shadow rowversions, typed money/UTC/codes/names and masked identity storage. DESIGN ER/normalization note and sql/schema.sql. |
| B | Five reporting SQL files; atomic T-SQL sale; two-session READ COMMITTED/RCSI/locking observations and SSMS reproduction scripts; short-transaction/stock-order discussion. |
| C | Four before/after workloads, actual plans, reads/CPU, candidate rationale, write/storage costs, SARGable rewrite with honest scan explanation, stale/fresh cardinality evidence, PERFORMANCE.md. |
| D | Three CREATE OR ALTER procedures with parameter checks/THROW; parameterized EF calls; stock history transaction; least-privilege script/note and procedure ADR. |
| E | EF context, per-entity Fluent configurations, scalar converters/complex Money/owned values, three migrations, explicit seeding, no EnsureCreated in application, DTO reads, tracked command aggregate, atomic checkout, ExecuteUpdate, rowversion 409, transaction/strategy wrapper, three ToQueryString samples. |
| F | Correct dependencies, scoped staged repositories/UoW, no IQueryable contracts, rich domain, separate masked identity record/mapper, request and screen DTOs, explicit mapping and malicious over-posting tests, one database for read/write services, unchanged original rule tests. |
| G | SQL-backed dashboard, catalogue/full stock CRUD, transactions/void/refund, shipping, money orders, reports and alerts; isolated final-status audit; Problem Details mappings; external configuration and local CSP-compatible assets. |
| H | Unit/fake/spy-UoW tests, actual SQL mapping/rollback/concurrency/procedure tests, SQL WebApplicationFactory tests, eight new ADRs, README/DESIGN/PERFORMANCE and evidence. GitHub branch/PR publication is not performed. |

A tracked command aggregate has one collection, so there is no multi-collection cartesian explosion requiring AsSplitQuery. This is documented as an applicability decision, not a claim that split querying was demonstrated on an artificial shape.

## Important source paths

- ApexPost.Infrastructure/Persistence/ApexPostDbContext.cs
- ApexPost.Infrastructure/Persistence/Configurations/
- ApexPost.Infrastructure/Persistence/Repositories/
- ApexPost.Infrastructure/Persistence/EfUnitOfWork.cs
- ApexPost.Infrastructure/Persistence/EfAuditStore.cs
- ApexPost.Infrastructure/Persistence/Mapping/IdentityVerificationMapper.cs
- ApexPost.Infrastructure/Queries/PlatformQueries.cs and SqlOperations.cs
- ApexPost.Infrastructure/Seeding/ApexPostSeeder.cs
- ApexPost.Application/Services/TransactionProcessingService.cs, CatalogCommands.cs, ShippingCommands.cs, MoneyOrderService.cs
- ApexPost.Web/Controllers/ and Models/; Middleware/ExceptionHandlingMiddleware.cs; Filters/AuditActionFilter.cs
- ApexPost.Tests/Application/CommitBoundaryTests.cs, Persistence/ModelContractTests.cs, SqlServer/, Web/ProblemDetailsTests.cs

Draft Start/AddProduct/Pay remains available. Quick retail checkout also creates a new header, lines, payment and stock decrement in a single commit; both paths reuse the same payment staging logic. Empty stock records can be removed, but product/history deletion is guarded. Identity input is cleared on invalid money-order redisplay; high-value issuance remains blocked without independent verification. Audit failure cannot reverse a completed sale.

## Before assignment submission

1. **Trainer package review:** ADR 0014 documents the explicit patched-version pin of an existing SqlClient Pkcs dependency. The assignment's direct-package list does not name it; trainer approval is not claimed. Audit/warnings were not disabled to hide the issue.
2. **SSMS evidence:** recorded blocking uses two actual SQL sessions through ADO.NET. Run the supplied scripts in two SSMS windows if the evaluator requires SSMS screenshots specifically.
3. **Visual review:** Razor and form behavior are HTTP-tested; desktop/mobile browser screenshot approval has not been performed.
4. **Git hygiene:** create/reuse a Week 3 feature branch in your real repository, review the diff and make meaningful commits/PR. No remote write or push was made.
5. **Scope clarification:** offline/listener/outbox topics appear in the assignment taxonomy but not Parts A–H acceptance criteria. No new offline database or unrequested subsystem is added.

These are explicit review/evidence boundaries; 100/100 marks are not promised. Performance is a single synthetic warm-cache sample, not a production throughput guarantee. Audit delivery remains best effort, and automatic transient retries are not enabled by default. The demo interprets opening hours in UTC and uses PHP fixtures.

## Local next step

Extract the source into a fresh folder and follow README.md. Set the Development SQL connection and demo passwords, review the migrations/schema, apply them to your private ApexPostDb, run the explicit --seed command, then start the app. SQL test commands require their own disposable master connection and never reuse ApexPostDb.
