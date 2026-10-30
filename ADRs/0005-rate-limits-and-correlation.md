# 0005: Use correlation IDs and fixed-window rate limiting

- Status: Accepted
- Date: 2026-10-24

## Context

The platform must support secure login throttling and allow operations to be traced through request and audit flows.

## Decision

Use a fixed-window login rate limiter with a correlation ID injected through middleware and attached to both the log context and the response headers.

## Consequences

- Repeated failed sign-ins receive an explicit 429 response.
- Security events remain attributable to a single request flow.
- Operators can trace a request across middleware, MVC filters, and audit output.
