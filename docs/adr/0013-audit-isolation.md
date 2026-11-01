# ADR 0013 — Audit isolation and final response status

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

AuditActionFilter collects safe metadata and schedules final-status persistence after response completion. EfAuditStore owns an independent context/transaction using the same database. Failures are sanitized and logged.

## Consequences and validation

A business transaction is not rolled back by audit storage failure or flushed after an earlier failed command. Audit delivery is best effort; durable outbox/reconciliation is a separate requirement. Raw document fields, passwords and keys are excluded.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.
