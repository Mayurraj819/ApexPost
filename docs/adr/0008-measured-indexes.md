# ADR 0008 — Measured workload indexes

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Use four composite/covering indexes measured in evidence/ and PERFORMANCE.md. Branch/zone equality precedes range/level predicates; narrow INCLUDE columns remove lookups.

## Consequences and validation

Read IO improves on the measured scale, but stock updates require more IO and indexes consume storage. Small/unselective queries may still scan. Raw SQL migration is deliberate for INCLUDE columns of flattened complex values; the model snapshot alone does not describe these SQL-managed indexes.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.
