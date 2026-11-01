# ADR 0011 — Domain versus persistence models

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Map rich core aggregates directly with Fluent configuration, private construction and backing fields. Use a separate IdentityVerificationRecord and explicit mapper for masked storage. Use owned Address and complex Money; scalar converters only for scalar representations.

## Consequences and validation

Avoids duplicating every domain model while making privacy/storage concerns explicit for identity. Multi-column Money is not serialized into a single JSON/varchar column. A PHP tariff converter rejects non-PHP currency.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.
