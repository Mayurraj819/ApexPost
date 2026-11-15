# Week 5 — real Couchbase repository integration

The `CouchbaseRepositoryTests` class uses Couchbase Lite 4.1.2 through the actual CourierDatabase, CouchbaseScanRepository and ShipmentScanMapper. No mock repository, external service or SQL Server is involved. Each test owns a uniquely named temporary directory; the database is disposed before that directory is removed. Required native binaries come from the existing Couchbase NuGet dependencies. There are no skip attributes or manual seed/configuration steps. A missing native dependency must fail with its real error, not produce a false passing result.

## Coverage: 15 executed cases

- Creation persists all fields, string enums, UTC time and Pending even when input says Synced.
- Null notes are normalized to an empty string.
- Duplicate create does not replace the original stored scan.
- Update persists acknowledged state and removes the scan from Pending queries.
- Delete succeeds once, unknown/deleted lookup returns null and repeat delete returns false.
- Close/reopen retains contents and sync state.
- Tracking queries filter unrelated data, order UTC/tied IDs deterministically and treat SQL-like input as a parameter value.
- Pending query excludes Synced and Failed records.
- Courier grouping returns correct counts and zero entries for missing types/owners (two Theory cases).
- Both required indexes are present.
- Invalid stored scan type/sync status is rejected rather than silently accepted (two Theory cases).
- A change notification contains the changed ID and current Pending count.
- Dispose is idempotent, a disposed wrapper rejects listener registration, and reopening supports a new listener and retained data.

Listener tests await TaskCompletionSource with a 15-second hang guard. They do not sleep or rely on a wall-clock business timestamp. Input IDs/timestamps are fixed; GUIDs only isolate filesystem paths. The lifecycle test verifies observable reuse/notification behavior; it is not a native heap/leak measurement or a direct assertion on Couchbase's internal listener token.

## Run from the solution root

```powershell
New-Item -ItemType Directory -Force evidence/week5 | Out-Null
dotnet test ApexPost.IntegrationTests -m:1 --filter "FullyQualifiedName~CouchbaseRepositoryTests" --logger "console;verbosity=normal" 2>&1 | Tee-Object evidence/week5/couchbase-integration.txt
```

These cases require the stage-01 IntegrationTests project with its existing Courier.Offline project reference. No package/project/solution change is required by this test patch. The package's validation log is a targeted Linux native-engine run; run the command on your Windows machine and retain that result separately. This does not claim the full Week 5 suite is complete. Automatic SQL infrastructure, real API/database E2E, additional unit validation/client coverage, BDD and final Copilot feature evidence remain separate stages.

If a test uncovers a production defect, preserve the failure and diagnose it. Do not weaken an assertion solely to obtain green. No production code is intentionally changed by this package.
