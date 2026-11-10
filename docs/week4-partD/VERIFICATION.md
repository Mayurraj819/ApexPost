# Part D acceptance

Baseline: week4-partD at 1f84abf91f9c5e4ed2c3c1478a3717b3a433c2ec. This is a local patch; no push/merge/migration is performed. Main solution is portable; ApexPost.Mobile.slnx is native.

| Requirement | Implementation | Check |
|---|---|---|
| D1 JWT | Api/Security/ApiSecurity.cs | Missing/expired/bad issuer/audience/signature → 401 |
| D2 roles/ownership | Three shipment controllers and filter | Courier upload/PATCH, Admin DELETE; mismatched courierId → 403 |
| D3 partner key | PartnerShipmentsController/PartnerKeyHandler | Invalid/missing 401; valid read; no ordinary route access |
| D4 OAuth | SECURITY.md | PKCE, Client Credentials, provider/tokens explained |
| D5 TLS/OpenAPI | Program/transformer/contracts | HTTPS, production HSTS, Swagger Authorize |
| D6 factory | Integration/CourierRegistration, Courier/MauiProgram | Typed client, config, auth handler |
| D7 resilience | Registration/state | Attempts, delays, timeouts, circuit, logs |
| D8 JSON | Contracts/ApexApiClient | Sourcegen, ProblemDetails, malformed/ID checks |
| D9 sync | CourierSync/CourierSession | Local first, 20-sized batches, reconnect, per-ID state/listener |
| D10 storage | SecureTokenStore/AuthHeaderHandler/UI | SecureStorage only; 401 navigates Login |
| D11 screens | CourierTabs | Login, New Scan, Pending, Sync Status |

Abbreviated paths Api/Integration/Courier mean ApexPost.Api/ApexPost.Courier.Integration/ApexPost.Courier.

1. Run Verify-PartD.ps1 -WithLocalDb -BuildWindows. Require zero warnings/errors and no skipped SQL tests. The portable MAUI source check does not verify platform startup/native compilation.
2. Run API https, authorize v2 Swagger, create fictional shipment. Capture valid JWT, missing/invalid/expired JWT 401, Courier DELETE 403, Admin PATCH 403, wrong courierId 403, missing/invalid partner key 401, valid partner read 200. Mask credentials. Existing A–C domain behavior must remain.
3. MAUI: login, save offline scans, restart to prove local persistence, reconnect and verify count reaches zero/server stores each ID once. Check duplicate acknowledgements and explicit rejection/Failed handling.
4. Leave a Pending batch, stop API, sync to open circuit and show offline banner; continue saving. Restart API, wait for half-open probe and Closed recovery. Capture retry/state logs without tokens/bodies.
5. Let a 15-minute token expire plus 30-second skew: sync should clear token, select Login, retain Pending. New same-courier login resumes.
6. Record the required 3–5 minute video: masked login, **actual airplane-mode scans on emulator/device**, auto sync after reconnect, 401/403, circuit opening/recovery. Windows Work offline is only a simulation; never disconnect the RDP host. No video/runtime screenshots are fabricated/supplied.
7. Review evidence for secrets, then commit/push and open a PR after validation. Merge only after review/approval. Submit inspectable source/docs/evidence, not only an inaccessible URL.

Reproduce portable checks: `dotnet build ApexPost.slnx -m:1`, `dotnet test ApexPost.slnx --no-build -m:1`; contracts/SDK use the pinned NSwag tool. The supplied logs state what ran. SQL skips, native MAUI, SecureStorage, emulator certificates and UI sync remain unverified until performed on suitable machines. PowerShell scripts are reviewed, not executed on Linux.
