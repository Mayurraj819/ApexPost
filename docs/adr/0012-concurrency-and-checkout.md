# ADR 0012 — Optimistic concurrency and atomic checkout

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Stock and transaction rowversions detect stale tracked writes. Parent transaction is touched for line edits. Checkout groups stock requirements, validates then stages stock/header/payment and saves once. No automatic retry of a business conflict.

## Consequences and validation

Two HTTP requests racing for the last unit produce one success and one 409 in the SQL Web test. Nonnegative CHECK and unique payment identity provide additional safeguards. Financial corrections do not assume physical returns; reasoned stock adjustment is separate.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.
