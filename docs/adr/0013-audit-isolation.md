# ADR 0013 — Audit isolation and final response status

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

AuditActionFilter collects safe metadata and schedules final-status persistence after response completion. EfAuditStore owns an independent context/transaction using the same database. Failures are sanitized and logged.

## Consequences and validation

A business transaction is not rolled back by audit storage failure or flushed after an earlier failed command. Audit delivery is best effort; durable outbox/reconciliation is a separate requirement. Raw document fields, passwords and keys are excluded.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.

## Project-specific context

A sale changes stock, transaction state and payment records. Audit logging
records the request outcome, including failed requests.

If auditing shares the business commit boundary, an audit failure could
prevent a valid sale. Reusing a business context after a failed command could
also accidentally save pending business changes.

## Alternatives considered

### 1. Share the business DbContext and Unit of Work

Rejected for request auditing. Audit persistence could affect the business
transaction or accidentally flush changes left by a failed command.

### 2. Keep JSON-file auditing

Rejected for Week 3 because the assignment requires AuditEntries in SQL Server.

### 3. Use an independent audit context and transaction

Selected. EfAuditStore creates its own context using the configured SQL
connection. It writes to the same database but does not share the business
context's tracked changes or transaction.

## Final response status

AuditActionFilter collects sanitized request metadata and registers a response
completion callback.

Persistence reads the final response status after result execution. This avoids
recording a redirect as HTTP 200 simply because the action filter ran before
the redirect result.

The audit entry includes controller/action, actor identifiers, correlation ID,
status, duration and permitted route identifiers.

## Failure handling

Audit persistence has a bounded timeout. An audit-store exception is caught
and logged without replacing the completed business response.

A successfully committed sale therefore remains successful even if the audit
insert fails.

## Privacy boundary

Audit entries exclude request bodies, passwords, API keys and raw identity
document numbers.

Only selected GUID route values are recorded. Exception metadata records
the exception type rather than copying sensitive request values.

## Trade-off: best-effort delivery

The response-completion approach does not guarantee durable audit delivery.
A process crash or database outage can cause an audit entry to be lost.

A durable outbox and retry/reconciliation workflow would strengthen delivery,
but it is not implemented here. We do not claim exactly-once or guaranteed
audit persistence.

## Verification

A SQL-backed MVC test uses a failing audit store during a successful sale
workflow and verifies that the business operation still succeeds.

Another SQL-backed MVC test verifies that a void action's audit entry records
the final HTTP 302 redirect status.