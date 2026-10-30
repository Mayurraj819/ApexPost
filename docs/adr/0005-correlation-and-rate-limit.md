# 0005: Correlation IDs and login throttling

- Status: Accepted
- Date: 2026-10-24

## Context
Security events and user activity require traceability and attempted login abuse prevention.

## Decision
Apply a fixed-window login limiter and a correlation ID header across the request pipeline.

## Consequences
- Better support for investigation and compliance
- Controlled brute force resistance
- Consistent request tracing from middleware into the audit store
