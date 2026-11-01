# ADR 0014 — Existing dependency security pin and package review

Status: implemented; trainer review pending where explicitly noted.

## Context

Week 3 continues the Week 2 application and requires SQL Server/EF Core without changing its business rules.

## Decision

Pin existing SqlClient transitive System.Security.Cryptography.Pkcs to 9.0.21. Restore identified NU1902/NU1903 for its older resolved version. Keep vulnerability audit and warnings-as-errors enabled; remove unused coverlet collector.

## Consequences and validation

This is a direct pin of an existing required runtime dependency, not an additional feature framework. The training direct-package allowlist does not explicitly list it: obtain trainer approval for this exception rather than claiming approval. Necessary test SDK/runner tooling is retained; no AutoMapper/MediatR/Testcontainers/Bogus is added. Advisories: https://github.com/advisories/GHSA-2fmv-rv2c-59x7 and https://github.com/advisories/GHSA-846f-f764-76mx.

See DESIGN.md, PERFORMANCE.md and the SQL/unit/Web tests for verification.
