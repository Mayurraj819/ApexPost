# 0001: Use JSON files for persistence

- Status: Accepted
- Date: 2026-10-24

## Context
The Week 2 assignment requires JSON persistence and forbids a database or ORM.

## Decision
Use repository interfaces backed by JSON files for core application data.

## Consequences
- Simple storage and easier troubleshooting
- No database licensing or ORM complexity
- Best suited to demo and training workloads
