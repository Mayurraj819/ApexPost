# ADR 0011 — Domain versus persistence models

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Map rich core aggregates directly with Fluent configuration, private construction and backing fields. Use a separate IdentityVerificationRecord and explicit mapper for masked storage. Use owned Address and complex Money; scalar converters only for scalar representations.

## Consequences and validation

Avoids duplicating every domain model while making privacy/storage concerns explicit for identity. Multi-column Money is not serialized into a single JSON/varchar column. A PHP tariff converter rejects non-PHP currency.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.

## Project-specific context

The existing domain contains validated value objects, private state and
business methods. Replacing every domain entity with a database-oriented
class would duplicate models and risk changing the Week 2 rules.

However, identity verification needs an explicit storage boundary so raw
document numbers cannot accidentally become database columns.

## Alternatives considered

### 1. Separate persistence models for every aggregate

Rejected for this assignment because it would require duplicate entity graphs
and extensive mapping for branches, products, stock and transactions.

### 2. Map every domain model directly

Not selected universally. Direct mapping is suitable for core aggregates,
but identity verification benefits from an explicit masked-storage model.

### 3. Selective separation

Selected. Core aggregates use direct Fluent API mapping. IdentityVerification
uses a separate persistence record and an explicit mapper.

## Directly mapped aggregates

Branch, Counter, Staff, Product, BranchStock, Transaction and Payment retain
their domain behavior.

Infrastructure configurations map private setters and backing fields.
Transaction lines use the private collection, while rowversion and audit
columns use shadow properties.

EF Core configuration remains in Infrastructure. Domain does not reference
EF Core or SQL Server packages.

## Separate identity-verification model

The relevant files are:

- ApexPost.Domain/Entities/IdentityVerification.cs
- ApexPost.Infrastructure/Persistence/Entities/RelationalRecords.cs
- ApexPost.Infrastructure/Persistence/Mapping/IdentityVerificationMapper.cs

IdentityVerificationRecord contains the masked document number, fingerprint,
document type, verification outcome and method.

IdentityVerificationMapper explicitly maps these fields between the domain
and persistence representations. RestoreMasked reconstructs the stored
representation without requiring the original document number.

Recording a fingerprint is not treated as independent identity verification.

## Value-object mapping decisions

Sku and BranchCode use scalar string converters.
Weight uses a scalar decimal converter.

Money generally uses a complex mapping containing separate amount and
currency columns. A single scalar converter cannot preserve two independent
columns.

Shipping tariffs use a PHP-only Money converter. It rejects another currency
instead of silently discarding that currency.

Owned shipment Address and branch OpeningHours values share their owner's
table because they do not have an independent lifecycle.

## Consequences

This approach preserves existing domain behavior without duplicating every
entity. The identity mapper makes the privacy-sensitive storage boundary
explicit.

Changes to identity storage require reviewing the mapper and its tests.
Changes to private domain state require checking the Fluent configuration
and SQL round-trip tests.

## Verification

ModelContractTests checks the relational model and identity mapper.

SQL integration tests verify transaction lines and status, closed branches,
inactive staff, and masked identity values after saving and reloading.
