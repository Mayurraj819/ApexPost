# ADR 0008 — Measured workload indexes

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Use four composite/covering indexes measured in evidence/ and PERFORMANCE.md. Branch/zone equality precedes range/level predicates; narrow INCLUDE columns remove lookups.

## Consequences and validation

Read IO improves on the measured scale, but stock updates require more IO and indexes consume storage. Small/unselective queries may still scan. Raw SQL migration is deliberate for INCLUDE columns of flattened complex values; the model snapshot alone does not describe these SQL-managed indexes.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.

## Project-specific context

The baseline database already had primary-key, foreign-key and unique indexes.
These did not cover every selected reporting/search workload.

Actual plans showed clustered-key lookups for several branch-based queries
and a clustered scan for the selected shipment search.

The decision therefore uses measured access paths, not a rule that every
foreign key or WHERE column needs another index.

## Alternatives considered

### 1. Keep only the existing indexes

This avoids additional storage and write maintenance, but retains the
measured lookups and scans for the selected workloads.

### 2. Add composite indexes without INCLUDE columns

These could improve filtering while using less storage than covering indexes.
However, retrieving the selected output columns could still require lookups.

### 3. Use narrow composite covering indexes

Selected for the four measured workloads. Equality/range predicates determine
the keys, and selected output columns determine the INCLUDE list.

## Workload-specific decisions

### Branch low-stock search

Index: IX_Stock_Branch_Quantity

Keys: BranchId, Quantity.
INCLUDE: ProductId.

BranchId supports branch equality, followed by the quantity range.
ProductId covers the selected product reference without a clustered-key lookup.

A filtered index for Quantity <= 3 was considered conceptually but not adopted:
the reporting procedure accepts other thresholds. Such an index would serve
a narrower set of requests and would require its own measurement.

### Branch/date transaction-header search

Index: IX_Transactions_Branch_Date

Keys: BranchId, OccurredAt.
INCLUDE: Status.

The branch equality comes before the UTC date range. Status is included
because it is returned by the measured header query.

This evidence applies to transaction-header access. It does not establish
the performance of every sales aggregation or the complete dashboard.

### Shipment search

Index: IX_Shipment_Zone_Level

Keys: ZoneId, ServiceLevel.
INCLUDE: TransactionId, Weight.

The measured query filters by zone and service level. The included fields
cover its output, replacing the observed clustered scan with a covering seek.

The zone-leading order also supports zone-based searches. It is not designed
to optimize searches using only ServiceLevel.

### Auditor money-order search

Index: IX_MoneyOrder_Branch_Date

Keys: BranchId, OccurredAtUtc.
INCLUDE: OrderAmount, IdentityVerificationId.

Branch equality precedes the date range. The included fields cover the
selected amount and identity-verification reference without extra lookups.

## Measured consequences

PERFORMANCE.md contains the recorded before/after reads, CPU, plan changes
and per-index storage costs. Actual plans and raw output remain under evidence/.

The measured stock-update batch required more logical reads after indexing.
Quantity is an index key, so stock changes require additional index maintenance.

Write overhead has not yet been measured separately for all four workloads.
The stock measurement must not be presented as evidence for shipment,
transaction or money-order write costs.

## Limits of the evidence

These results come from synthetic data and a single warm-cache sample per
query. Millisecond CPU values are too coarse to rank very small differences.

A SARGable date rewrite still scanned because it selected almost every row
and did not constrain the leading branch key. We do not claim every range
predicate must produce a seek.

The stale-statistics investigation demonstrates improved row estimates after
a statistics update; it does not claim that a scan is automatically incorrect.

## Deployment and review

AddMeasuredWorkloadIndexes installs these SQL-managed indexes.
The evidence tool removes them only in its disposable performance database
to reproduce the baseline.

Reassess the indexes if workload selectivity, write frequency or data volume
changes. Do not add duplicate indexes or remove existing uniqueness constraints
merely because another index has similar leading columns.