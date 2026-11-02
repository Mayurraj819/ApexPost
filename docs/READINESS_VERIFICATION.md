# Readiness follow-up: review and verification

Baseline: `Mayurraj819/ApexPost`, branch `Week3Lab/FeatureApexPost`, commit `fdcc5519`.
No remote changes or PR have been made by this follow-up.

## Apply to the same repository

Use the supplied patch from the solution root, rather than copying a ZIP over the repository:
a file overlay will not remove obsolete tracked files. Save/commit your own pending work first.

```powershell
git branch --show-current
git status --short
git apply --check "C:\Downloads\ApexPost-Week3-Readiness.patch"
git apply "C:\Downloads\ApexPost-Week3-Readiness.patch"
git diff --stat
```

The expected branch is `Week3Lab/FeatureApexPost`. If `git apply --check` reports a conflict,
stop and review it; do not force application or reset your repository.

## Build and unit/MVC verification

Run these one-line commands in Visual Studio Developer PowerShell at the solution root.

```powershell
dotnet tool restore
dotnet restore ApexPost.slnx
dotnet build ApexPost.slnx --no-restore -warnaserror
dotnet test ApexPost.slnx --no-build --logger "console;verbosity=normal"
```

Without the SQL test connection, SQL tests explicitly SKIP. That is not a full integration pass.
The follow-up test/build logs under `evidence/readiness-followup` record what ran here.

## Disposable SQL Server tests (before updating ApexPostDb)

Use a local/private SQL Server login with database creation rights. The fixtures create and
drop only GUID-named `ApexPost_Test_*` databases; the base connection must target master.
For Windows LocalDB:

```powershell
$env:APEXPOST_TEST_SQLSERVER = "Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True"
dotnet test ApexPost.slnx --no-build --logger "console;verbosity=detailed" --logger "trx;LogFileName=readiness.trx"
```

Confirm zero skipped/failed tests. Important new checks:

- MigrationUpgradeTests: upgrades actual ordinal rows, checks sales totals, downgrades and upgrades again.
- RetrySafetyTests: simulated lost commit acknowledgement must not duplicate stock/history; a failed
  precommit price revision must roll back before replay, leaving prices multiplied exactly once.
- QueryShapeTests: records actual one/split query command counts and row shape, with identical aggregates.
- Model tests: active retry registration, all code converters and migration snapshot agreement.
- Existing unchanged business-rule tests, checkout, concurrency 409, report and audit tests.

Review `OperationReceipts` permissions using `sql/security/RuntimePermissions.sql`. This role is
for runtime only; migrations and seeding require the deployment identity. Receipt retention is
privileged maintenance, never a runtime DELETE grant.

## Migration review and application

Back up your private database. Review the follow-up Up/Down migration before application.
The migration preserves existing values, rebuilds the included-column index and refreshes procedures.
Downgrade refuses ParcelShipping rows because the previous constraint excludes that line type.

```powershell
dotnet ef migrations has-pending-model-changes --project ApexPost.Infrastructure --startup-project ApexPost.Web
dotnet ef migrations script --idempotent --project ApexPost.Infrastructure --startup-project ApexPost.Web --output sql/schema.sql
dotnet ef database update --project ApexPost.Infrastructure --startup-project ApexPost.Web
dotnet run --project ApexPost.Web -- --seed
dotnet run --project ApexPost.Web
```

Use the existing user-secret connection/password settings. Never paste credentials into source files.
Apply RuntimePermissions.sql with the deployment identity if using the restricted application role.
Verify login, product create/edit/delete, stock adjustment, checkout, shipping, void/refund and reports.

## Refresh evidence and Git submission

The existing performance files describe the older integer schema. Capture fresh results after the
varchar migration; widening Status changes the covering index's storage and may change its plans.
The evidence tool creates/drops its own `ApexPost_Perf_*` database from the same master connection:

```powershell
dotnet run --project tools/ApexPost.Evidence
```

Review the resulting PERFORMANCE.md/evidence changes; retain honest measurements even if unchanged
or worse. Add the actual QueryShapeTests console output. Keep the original evidence clearly labelled
if comparing schema versions. Repeat the two-session SSMS demo only if its scripts or observed
behaviour change; do not fabricate screenshots.

After reviewing/testing, approve publication separately. Then create meaningful commits and a PR
from the existing Week3Lab/FeatureApexPost branch to the agreed target. Record the actual PR URL in
IMPLEMENTATION_STATUS.md. A GitHub source ZIP normally omits .git; provide the branch/commit/PR
links as history evidence rather than embedding .git in the submission ZIP.
