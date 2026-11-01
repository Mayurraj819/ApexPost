# ADR 0009 — Stored procedure boundaries

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

DailyBranchSales and LowStockReport provide stable SQL report contracts; AdjustStock centralizes guarded stock mutation plus history. Parameterized EF APIs call them; CREATE OR ALTER scripts are migration versioned.

## Consequences and validation

RECOMPILE on optional low-stock parameters favors per-call selectivity at compilation cost. Alternatives: cached optional predicate, parameter-sensitive plans, or separate query variants. Benchmark before expanding use. Procedures are not assumed faster; adjustment history/atomicity is the main reason for that write procedure.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.
## Why each operation uses a stored procedure

### usp_DailyBranchSales

This procedure provides a reusable reporting contract with branch and date
parameters. The application and SQL-based reporting tools can use the same
definition of paid retail and service sales.

Alternative: an EF Core LINQ projection could calculate these totals and
would also be valid. We chose a procedure to share the reporting definition
across application and SQL consumers, not because procedures are always faster.

Consequence: changing the report requires a reviewed database migration.
EF calls must remain parameterized, and integration tests must verify totals.

### usp_LowStockReport

This procedure supports both a selected branch and all branches, with a
configurable stock threshold.

Alternative: LINQ could build separate queries for branch-specific and
all-branch requests. A cached optional-parameter SQL query is another option.

We chose OPTION (RECOMPILE) so SQL Server can optimize for the branch and
threshold supplied on each execution. The trade-off is compilation work on
each call. This is a deliberate starting choice, not a measured claim that
RECOMPILE is always faster.

Consequence: if report frequency or compilation cost becomes significant,
compare cached plans, separate query variants and parameter-sensitive plan
behavior using actual execution evidence.

### usp_AdjustStock

This procedure applies a guarded stock adjustment and inserts its history
record in the same transaction. It rejects invalid parameters and adjustments
that would produce an invalid quantity.

Alternative: an application command using tracked EF entities and one
SaveChangesAsync could provide atomic stock and history updates.

We chose a procedure for this specific adjustment operation so SQL callers
can reuse the same guarded update and history-writing contract. It does not
replace the EF-based checkout workflow.

Consequence: the application login needs EXECUTE permission. Calls must be
parameterized, and failures must roll back both the stock update and history
insert. Integration tests exercise the procedure and its history output.
