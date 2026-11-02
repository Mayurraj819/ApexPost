# 0002: Keep validation in the domain layer

- Status: Accepted
- Date: 2026-10-24

## Context

The business rules for branch hours, stock constraints, and staff permissions must be enforced consistently across controllers, services, and tests.

## Decision

Model validation in domain entities and value objects instead of scattering rule checks throughout MVC code.

## Consequences

- Core invariants are central and easier to test.
- Controllers and services remain thin and delegate to domain operations.
- Invalid state is rejected before persistence or processing proceeds.
