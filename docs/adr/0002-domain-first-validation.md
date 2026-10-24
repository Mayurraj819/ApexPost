# 0002: Domain-first validation

- Status: Accepted
- Date: 2026-10-24

## Context
Branch hours, inventory constraints, and authorization rules must be enforced consistently.

## Decision
Validate rules in the domain entities and value objects instead of in MVC-only code.

## Consequences
- Strong invariants and easier testing
- Thin web controllers
- Consistent enforcement across all entry points
