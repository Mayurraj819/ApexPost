# ADR 0010 — Repository and UoW boundaries

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Scoped repositories stage aggregates. EfUnitOfWork commits once per command. Query services in Infrastructure may directly project DbContext AsNoTracking into DTOs. No IQueryable crosses Application contracts.

## Consequences and validation

SQL procedure/ExecuteUpdate commands execute immediately, not through a pretend staged repository. Their explicit transaction delegates run under the provider execution strategy. One SQL database supports both read and write paths.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.

## Project-specific context

Week 2 JSON repositories wrote changes immediately. A payment could therefore
update stock before transaction persistence failed. The application needed
one reliable commit boundary for each SQL-backed business use case.

## Alternatives considered

### 1. SaveChanges inside each repository

Rejected because stock, transaction and payment repositories could commit
independently. A later failure would leave earlier changes persisted.

### 2. Inject DbContext directly into MVC controllers

Rejected because controllers would become responsible for persistence and
transaction coordination. Application use cases would be harder to test
without EF Core.

### 3. Staged repositories with an application-controlled Unit of Work

Selected. Repositories load aggregates and stage changes. The application
service decides when the complete use case is ready to commit.

## Commit ownership

- Starting a draft transaction commits the new draft once.
- Adding a product commits that draft modification once.
- Paying a draft stages stock changes, transaction status and payment,
  then commits once.
- Quick checkout stages a new header, lines, stock changes and payment,
  then persists them through one SaveChangesAsync.
- Validation failures do not call SaveChangesAsync.

DbContext, repositories and Unit of Work are scoped and share one business
context per request.

## Where direct DbContext access is acceptable

Infrastructure query services may use DbContext directly for AsNoTracking
DTO projections and database-side aggregation. Reads do not need to load
tracked domain aggregates merely to display a screen.

Application interfaces return materialized DTOs or aggregates, never
IQueryable. MVC controllers do not compose EF queries.

## Stored procedures and set-based writes

A stored procedure or ExecuteUpdateAsync executes immediately; it is not
a staged repository operation.

When a use case mixes these operations with SaveChangesAsync, the complete
operation must use an explicit transaction under the provider execution
strategy. The implementation provides ExecuteInTransactionAsync for this
boundary.

## Audit exception

Audit persistence uses a separate context and commit boundary. It must not
flush pending business changes or reverse a successful sale if auditing fails.
ADR 0013 explains this deliberate exception.

## Verification

CommitBoundaryTests uses fake repositories and a spy Unit of Work to check
one commit on success and no commit on validation failure.

SQL integration tests verify staged writes, atomic rollback and persisted
checkout results using SQL Server.

Read and write paths use the same database. Separate databases are not required.
