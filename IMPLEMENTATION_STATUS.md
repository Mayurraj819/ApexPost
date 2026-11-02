# Implementation status

Source branch: `Week3Lab/FeatureApexPost` in `Mayurraj819/ApexPost`.
Review baseline: `fdcc5519dea9628cb758823e3f0d36ae2af9f49b`.
The feature branch and meaningful commits already exist. This follow-up is prepared locally;
it has not been pushed and no Week 3 PR was found at the baseline review.

## Changes prepared after the 85/100 assessment

- Removed unused Web backup, JSON store interface, JSON data and placeholder class.
- Consolidated ADRs under `docs/adr`; retained Week 2 decisions in its historical archive.
- Renamed patch-labelled test folders into Domain, preserving their test bodies.
- Enabled SQL Server transient retries; added atomic operation receipts for raw SQL transactions.
- Preserved one-SaveChanges checkout and business rules; protected the seeder's explicit transaction.
- Added a data-preserving migration to varchar transaction status and line type codes, including ParcelShipping.
- Updated procedures, reports and performance seed scripts to use the new codes.
- Added a single/split aggregate comparison and fault-injection regression tests.
- Explained product/empty-stock DELETE permissions and RECOMPILE directly in SQL.
- Made RecordSale resolve seeded IDs automatically, rolling back by default.

## Verification and submission

See `docs/READINESS_VERIFICATION.md` for commands and actual verification limits.
Historical `evidence/build.txt`, `evidence/tests.txt`, and existing performance plans describe
the earlier schema; they are not proof that this follow-up was run. Keep them as historical
measurements and regenerate performance evidence after the varchar migration.

Before publication: review the diff, apply/test migrations on a disposable SQL Server database,
re-run performance evidence, then approve a push to the existing feature branch and creation
of a PR. Never claim a PR exists until its URL is recorded here.
