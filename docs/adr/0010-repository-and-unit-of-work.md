# ADR 0010 — Repository and UoW boundaries

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Scoped repositories stage aggregates. EfUnitOfWork commits once per command. Query services in Infrastructure may directly project DbContext AsNoTracking into DTOs. No IQueryable crosses Application contracts.

## Consequences and validation

SQL procedure/ExecuteUpdate commands execute immediately, not through a pretend staged repository. Their explicit transaction delegates run under the provider execution strategy. One SQL database supports both read and write paths.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.
