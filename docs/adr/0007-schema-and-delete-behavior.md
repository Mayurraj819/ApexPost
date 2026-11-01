# ADR 0007 — Schema and delete behavior

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Use normalized reference/header/line/history tables, UTC audit columns, unique business keys and rowversion. Historical FKs use NO ACTION; lines cascade with their aggregate. A composite counter/branch FK protects transaction ownership.

## Consequences and validation

Preserves history and avoids SQL Server multiple cascade paths; command services reject product deletion with history or stock. A full per-column schema is generated in sql/schema.sql.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.
