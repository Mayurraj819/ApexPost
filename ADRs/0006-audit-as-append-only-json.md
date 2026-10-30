# 0006: Keep audit output append-only and sanitized

- Status: Accepted
- Date: 2026-10-24

## Context

ApexPost needs a durable, reviewable log of administrative actions without exposing sensitive data such as passwords, API keys, full identity numbers, or card inputs.

## Decision

Audit events are written through `IAuditStore` to an append-only JSON Lines file and are sanitized before write.

## Consequences

- Historical audit records remain immutable and easy to inspect.
- Sensitive values are masked before they reach storage.
- Operational teams can correlate request activity without exposing personal identifiers.
