# ADR 0012 — Optimistic concurrency and atomic checkout

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Stock and transaction rowversions detect stale tracked writes. Parent transaction is touched for line edits. Checkout groups stock requirements, validates then stages stock/header/payment and saves once. No automatic retry of a business conflict.

## Consequences and validation

Two HTTP requests racing for the last unit produce one success and one 409 in the SQL Web test. Nonnegative CHECK and unique payment identity provide additional safeguards. Financial corrections do not assume physical returns; reasoned stock adjustment is separate.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.

## Project-specific context

Two clerks can read the same available stock before either sale commits.
Checking quantity only in application memory is insufficient: both requests
could believe that the last unit is available.

Week 2 used a process-local payment lock. That lock cannot coordinate
multiple application instances.

## Alternatives considered

### 1. Application-wide SemaphoreSlim

Rejected as the SQL concurrency strategy. It serializes requests within
one process but does not protect stock across multiple application instances.

### 2. Pessimistic locking for every checkout

Not selected as the default. Explicit update locks could protect stock,
but would increase lock duration and require careful transaction management.

### 3. Optimistic concurrency with SQL Server rowversion

Selected for tracked stock and transaction updates. The database detects
whether a row changed after the application loaded it.

## Stock protection

BranchStock has a shadow RowVersion concurrency token.

EF includes the original rowversion in the update condition. If another
request has already changed that stock row, the stale update affects no
rows and EF raises DbUpdateConcurrencyException.

The database also has a CHECK constraint preventing negative quantity.
This is an additional integrity safeguard, not a replacement for concurrency
handling.

## Transaction protection

CounterTransactions also has a shadow RowVersion.

The transaction repository touches its modification timestamp when saving
aggregate changes, including line edits. This causes the parent transaction
to participate in concurrency checking rather than protecting only its lines.

## Atomic checkout

Quick checkout stages:

- the new transaction header;
- its transaction lines;
- stock decrements;
- the payment.

One SaveChangesAsync persists these changes atomically.

The existing draft workflow remains available. Paying a draft stages its
stock changes, paid status and payment in one commit.

Payment has a unique transaction reference to prevent multiple payment
records for the same transaction.

## Conflict response

Infrastructure translates a concurrency failure into an application-level
ConcurrencyConflictException.

The web exception middleware returns HTTP 409 with Problem Details and a
message asking the user to refresh.

The application does not blindly retry a stock conflict. Retrying without
reloading and validating the latest stock could repeat an invalid business
decision.

## Transaction duration and access order

Stock requirements are grouped by product and processed in a consistent
product-ID order. Validation occurs before persistence, and no external
service call is performed while holding the checkout write transaction.

EF batching is not a public guarantee of SQL lock order. If production
deadlocks occur, inspect their graphs and correct the conflicting access
paths rather than assuming optimistic concurrency prevents all deadlocks.

## Verification

SQL integration tests load the same last-unit stock row through independent
contexts and verify that a stale update fails.

A WebApplicationFactory test synchronizes two checkout requests after they
have loaded stock. It verifies one successful checkout, one HTTP 409 response,
and only one persisted payment for those competing sales.

Rollback tests verify that a failed sale does not leave stock and transaction
rows partially committed.
