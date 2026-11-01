# ADR 0007 — Schema and delete behavior

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Use normalized reference/header/line/history tables, UTC audit columns, unique business keys and rowversion. Historical FKs use NO ACTION; lines cascade with their aggregate. A composite counter/branch FK protects transaction ownership.

## Consequences and validation

Preserves history and avoids SQL Server multiple cascade paths; command services reject product deletion with history or stock. A full per-column schema is generated in sql/schema.sql.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.

## Project-specific context

Week 2 JSON files could contain transaction references to missing counters
and could not enforce relationships between stock, products and sales.

Week 3 requires a relational system of record that preserves historical
transactions and rejects invalid relationships and values.

## Alternatives considered

### 1. Store complete transactions as JSON in SQL Server

Rejected because foreign keys, relational reporting and field-level
constraints would be harder to enforce on transaction lines and references.

### 2. Cascade every relationship

Rejected because deleting a branch, product or staff member could remove
historical business records. Multiple cascade paths can also cause SQL Server
schema restrictions.

### 3. Normalize the schema and restrict historical deletion

Selected. Reference data, transaction headers, lines, payments and history
are separate tables with explicit relationships.

## Keys and uniqueness

GUID primary keys identify business records.

Unique constraints/indexes protect branch codes, product SKUs and staff email.
Additional uniqueness rules protect branch/counter numbers, branch/product
stock records and one payment per transaction.

CounterTransactions uses a composite counter/branch foreign key referencing
Counters(Id, BranchId). This prevents a transaction from selecting a counter
from another branch.

## Delete decisions

Historical references use NO ACTION. A parent record cannot be deleted while
dependent records reference it.

TransactionLines uses CASCADE because lines belong to their parent aggregate.
This database configuration does not authorize users to delete historical
transactions: application rules and runtime permissions still apply.

The relationship-by-relationship delete matrix is documented in DESIGN.md.

AuditEntries intentionally has no business foreign keys. Actor and branch
identifiers are snapshots, allowing failed requests and unknown identifiers
to be audited.

## Constraints and data types

CHECK constraints protect nonnegative stock, positive transaction-line
quantities, positive payment and money-order amounts, supported transaction
statuses and shipment weight limits.

Money columns use decimal(18,2). UTC timestamps use datetime2.
Codes use varchar, while names and descriptive text use nvarchar.

Principal tables have creation time, creator and modification-time audit
columns. Creation defaults use SQL Server UTC time and a system actor value.

Stock and transaction rowversions are database-generated concurrency tokens.

## Identity storage

The identity-verification table stores a masked number with a limited column
length, a fingerprint and verification metadata. It has no raw document-number
column.

A fingerprint record is not described as proof of independent verification.

## Consequences and verification

The database can reject invalid references and values even when a caller
bypasses an MVC form.

Restrictive deletion requires explicit cleanup or deactivation policies.
It protects history at the cost of making physical deletion less convenient.

SQL Server tests verify mapping, persisted state and unique constraints.
The generated idempotent schema script is also replay-tested.
