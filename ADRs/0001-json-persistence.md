# 0001: Use JSON files for persistence

- Status: Accepted
- Date: 2026-10-24

## Context

The Week 2 assignment requires a JSON-based platform without introducing a database, ORM, or ASP.NET Core Identity.

## Decision

Use repository interfaces backed by file-based JSON storage for branches, products, and transactions.

## Consequences

- Data is easy to inspect and version in source control.
- The app remains lightweight and testable.
- Operational scale is intentionally limited to the platform's demo and training scope.
