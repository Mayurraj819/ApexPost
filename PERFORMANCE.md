# ApexPost performance evidence

Measured on a disposable SQL Server database. These are actual observations, not predicted improvements. All raw IO/TIME output, actual .sqlplan files, index-storage snapshots and three EF-generated SQL samples are committed under evidence/.

## Environment

```text
UTC run: 2026-10-30T12:53:30.5646537Z
Branches: 1000
Stock: 30000
Transactions: 100200
Shipments: 33433
MoneyOrders: 10000
SQL Server: 17.0.5005.3
Warm-cache single run; no production claims.
```

Data is synthetic and skewed; see sql/performance/SeedScale.sql. Runtime and benchmark data are separate. This run uses warm caches, one sample per query, millisecond CPU granularity and the provider-created default RCSI setting. Zero CPU milliseconds means below measurement granularity, not zero work. No production latency/throughput claim is made.

## Four selected workloads

| Workload | Rows | Reads before → after | CPU ms before → after | Main plan change |
|---|---:|---:|---:|---|
| Branch stock at/below 3 | 0 | 106 → 5 | 1 → 0 | 	Existing branch/product index seek with clustered-key lookups → covering seek on IX_Stock_Branch_Quantity; clustered-key lookups removed. |
| Branch/date sale-header search | 74 | 443 → 5 | 1 → 0 | 	Branch-only index seek with clustered-key lookups → covering branch/date seek on IX_Transactions_Branch_Date. |
| Zone/service shipment search | 3353 | 1366 → 36 | 6 → 1 | Clustered index scan → covering index seek on IX_Shipment_Zone_Level. |
| Branch/date auditor search | 10 | 24 → 4 | 1 → 0 | Branch-only index seek with clustered-key lookups → covering branch/date seek on IX_MoneyOrder_Branch_Date. |

These descriptions refer to the main SELECT statement in each captured
actual plan. Branch/zone parameter-selection statements are separate and
may still contain scans or seeks. A Clustered Index Seek under a nested-loop
lookup can represent a Key Lookup in SSMS; it does not mean the original
query was already covered by its nonclustered index.
The daily-sales workload measures the branch/date transaction-header access used before aggregating report lines; it does not claim to benchmark an entire dashboard/report request. Logical reads include fixed variable-selection statements in the saved batch. Each batch may have multiple plan result sets; the JSON operator list therefore includes setup operators too. Compare the data-access statements, not merely whether “Index Seek” appears anywhere.

## Index choices and storage

| Index | Reason for key order / INCLUDE | Used KB after |
|---|---|---:|
| IX_Stock_Branch_Quantity | Branch equality then quantity range; INCLUDE ProductId covers warning rows | 1760 |
| IX_Transactions_Branch_Date | Branch equality then UTC date range; INCLUDE Status avoids header lookups | 5176 |
| IX_Shipment_Zone_Level | Zone then level equality; INCLUDE TransactionId and Weight covers shipment search | 2360 |
| IX_MoneyOrder_Branch_Date | Branch equality then UTC date range; INCLUDE OrderAmount and IdentityVerificationId covers auditor search | 720 |

These four indexes consume 10016 KB of used pages in this fixture. Baseline/indexed full storage snapshots include pre-existing PK/FK/unique indexes.

The same rolled-back stock-update batch required **195 → 368 logical reads**, CPU **0 → 0 ms**. Quantity is an index key, so changes must maintain the additional index; this is a real write penalty. The sample does not quantify every insert/update/delete workload, and millisecond timing cannot rank tiny operations reliably. Recheck mixed write throughput before production.

The measured indexes are installed by AddMeasuredWorkloadIndexes. They are explicit SQL-managed indexes in a migration, not inferred from the EF snapshot. The evidence tool drops only those four indexes in its disposable database to capture a genuine baseline, then recreates them. CandidateIndexes.sql must never be applied blindly to the normal development database.

## SARGability

`YEAR(OccurredAt)=2026` was rewritten to `OccurredAt >= 2026-01-01 AND OccurredAt < 2027-01-01`. Actual reads: **638 → 638**; CPU: **21 → 18 ms**. Both returned 100200 rows and used an index scan. The interval matches almost every fixture row and the date is not the leading key of the branch/date index, so a seek is not promised. The predicate rewrite enables a range access path when selectivity and a suitable leading key justify it; this run does not establish a meaningful CPU improvement.

See evidence/plans/non-sargable-0.sqlplan and sargable-0.sqlplan.

## Estimated versus actual rows

The tool creates a full-scan Quantity statistic with NORECOMPUTE, then deliberately changes the distribution in its isolated database. The scan estimated **576** rows versus **28239** actual rows. After `UPDATE STATISTICS ... WITH FULLSCAN`, it estimated **28239** versus **28239** actual rows. Both plans can legitimately scan: the new distribution is mostly zero. Updated cardinality is the evidence of the fix, not a manufactured seek.

See stale-statistics/fresh-statistics .sqlplan and .txt files.

## Blocking and deadlocks

Two independent SQL Server sessions, READ COMMITTED. Default read: Read completed without blocking (inspect RCSI). Elapsed ms=2. Locking read: READCOMMITTEDLOCK read blocked and timed out with error 1222. Elapsed ms=1506. EF-created SQL databases use RCSI ON; the table hint deliberately requests shared locks for the blocking demonstration. SSMS companion scripts are in sql/evidence/.

This run used two independent ADO.NET SQL Server sessions. To reproduce in **two SSMS windows**, use sql/evidence/Blocking-Session-A.sql and Blocking-Session-B.sql; no SSMS screenshot is claimed here. Default READ COMMITTED under RCSI reads a version rather than waiting on the writer. READCOMMITTEDLOCK deliberately requests locking and demonstrates error 1222 while the other session holds its transaction. These observations are tied to the recorded database settings, not a blanket statement about all READ COMMITTED databases.

Keep transactions short, validate before writes, group/sort stock keys consistently, and keep stock/history and sale/line/payment access order stable. No external calls occur while holding SQL locks. EF batching order is not a public lock-order guarantee; production deadlock graphs remain necessary if contention reveals an inconsistent path.

## Generated SQL / tracking / N+1

The three actual `ToQueryString()` samples are evidence/generated-sql/low-stock.sql, sales.sql and shipments.sql. Read screens use DTO projections and AsNoTracking. QueryCountTests verifies that ReportsAsync executes four commands for either one branch or all branches; no per-branch money-order query loop remains. Development command logging is enabled without sensitive parameter logging.

The tracked transaction aggregate currently loads one Lines collection plus single owned values. It does not have a multi-collection cartesian explosion; AsSplitQuery is intentionally not applied mechanically. If a second collection is added to that load, measure and use a split shape where needed.

## Reproduction and limits

Set APEXPOST_TEST_SQLSERVER to a private master connection and run `dotnet run --project tools/ApexPost.Evidence` from the solution root. It creates/migrates/seeds/scales a random ApexPost_Perf_ database and deletes it afterward. Use actual .sqlplan XML plus raw logs as evidence. Repeat measurements on representative hardware/data before treating these candidates as production tuning.

## Generated SQL captured using ToQueryString()

These SQL samples were captured during the recorded performance run.
The IDs and timestamps identify that fictional test dataset; they are not
values that must exist in every local database.

### 1. Low-stock query by branch

Source: evidence/generated-sql/low-stock.sql

```sql
DECLARE @branch uniqueIdentifier =
    '4d1e86ae-8b8b-4f1d-9d6f-4d4d7817d0d6';

SELECT [b].[ProductId], [b].[Quantity]
FROM [BranchStock] AS [b]
WHERE [b].[BranchId] = @branch
  AND [b].[Quantity] <= 3;
```

This query filters stock by branch and threshold in SQL rather than loading
all stock records into application memory.

### 2. Transaction-header search by branch and date

Source: evidence/generated-sql/sales.sql

```sql
DECLARE @branch uniqueIdentifier =
    '4d1e86ae-8b8b-4f1d-9d6f-4d4d7817d0d6';

DECLARE @from datetime2 = '2026-09-30T12:53:27.4969150Z';
DECLARE @today datetime2 = '2026-10-30T12:53:27.4969150Z';

SELECT [c].[Id], [c].[OccurredAt], [c].[Status]
FROM [CounterTransactions] AS [c]
WHERE [c].[BranchId] = @branch
  AND [c].[OccurredAt] >= @from
  AND [c].[OccurredAt] < @today;
```

The half-open date range includes the start and excludes the end.
It avoids applying YEAR or another function to the date column.
This sample measures header access, not the complete sales aggregation.

### 3. Shipment search by zone and service level

Source: evidence/generated-sql/shipments.sql

```sql
DECLARE @zone uniqueIdentifier =
    '303398eb-fe42-4103-b764-dba2b6e6d453';

SELECT [s].[TransactionId], [s].[Weight]
FROM [Shipments] AS [s]
WHERE [s].[ZoneId] = @zone
  AND [s].[ServiceLevel] = 0;
```

This query filters shipments by zone and service level and projects only
the transaction identifier and weight required by this workload.

These read queries use AsNoTracking projections. ToQueryString shows the
generated SQL; actual execution plans and IO/TIME measurements are separate
evidence stored under evidence/.