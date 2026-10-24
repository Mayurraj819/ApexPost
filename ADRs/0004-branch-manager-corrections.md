# 0004: Restrict voids and refunds to branch managers

- Status: Accepted
- Date: 2026-10-24

## Context

The Week 2 assignment requires manager-only void/refund actions and branch-level enforcement for correction workflows.

## Decision

Only active branch managers working at the transaction's branch can void or refund a transaction.

## Consequences

- Clerks cannot execute manager-only corrections.
- Audit records contain a clear reason for the action and the acting role.
- Non-manager attempts fail before changing transaction state.
