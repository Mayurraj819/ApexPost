# ADR 0009 — Stored procedure boundaries

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

DailyBranchSales and LowStockReport provide stable SQL report contracts; AdjustStock centralizes guarded stock mutation plus history. Parameterized EF APIs call them; CREATE OR ALTER scripts are migration versioned.

## Consequences and validation

RECOMPILE on optional low-stock parameters favors per-call selectivity at compilation cost. Alternatives: cached optional predicate, parameter-sensitive plans, or separate query variants. Benchmark before expanding use. Procedures are not assumed faster; adjustment history/atomicity is the main reason for that write procedure.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.
