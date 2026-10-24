# 0006: Sanitized append-only audit logs

- Status: Accepted
- Date: 2026-10-24

## Context
Operations require a durable audit trail without storing passwords, full identity numbers, or sensitive keys.

## Decision
Use `IAuditStore` to append sanitized JSON audit events to a file-backed log.

## Consequences
- Historical actions remain reviewable
- Sensitive fields are never written fully to storage
- Operational investigation stays safe and compliant
