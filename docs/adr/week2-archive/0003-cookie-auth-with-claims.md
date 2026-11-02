# 0003: Use cookie auth with branch and role claims

- Status: Accepted
- Date: 2026-10-24

## Context

The workflow requires secure authentication and branch-aware authorization for staff members across branch-specific operations.

## Decision

Use ASP.NET Core cookie authentication and include staff, branch, and role claims in the authenticated principal.

## Consequences

- Policies can enforce manager-only or same-branch access.
- UI and services have consistent user identity metadata.
- Claims remain portable and easy to audit without storing secrets in the app itself.
