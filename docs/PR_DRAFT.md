# Proposed title

Complete Week 3 persistence readiness and submission cleanup

# Proposed description

The Week 3 review found obsolete JSON/backup files, inactive transient retries and numeric
transaction codes. This change removes unused files while preserving historical ADRs/tests,
enables retries with atomic commit-verification receipts for SQL-only commands, and adds a
data-preserving varchar-code migration. It also updates reporting/procedure scripts, explains
runtime DELETE/RECOMPILE decisions and makes the sale demonstration resolve seed IDs itself.

Validation: solution builds with zero warnings/errors; 77 tests passed, 22 SQL-dependent tests
skipped because no SQL Server test connection was configured. EF model and snapshot agree;
the idempotent schema script was regenerated. Migration/retry fault tests, split-query
measurements and refreshed performance evidence must pass on disposable SQL Server before merge.

Review the new table/runtime permissions and migration downgrade restriction for ParcelShipping.
No claim of complete SQL validation is made until those checks are recorded.
