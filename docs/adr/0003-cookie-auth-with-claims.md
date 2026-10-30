# 0003: Cookie auth with claims

- Status: Accepted
- Date: 2026-10-24

## Context
Staff operations require branch-aware and role-aware authentication.

## Decision
Use ASP.NET Core cookie auth and include role and branch claims in the authenticated principal.

## Consequences
- Clear role-based and branch-bound authorization
- Reduced need for ad hoc request context checks
- Better compatibility with MVC policy enforcement
