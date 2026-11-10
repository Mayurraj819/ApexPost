# Replacement Part D package verification — 2026-11-10

Reconstructed from GitHub week4-partD baseline 1f84abf after the previous temporary ZIP became unavailable. These are fresh checks for this replacement, not a claim that the missing ZIP was recovered byte-for-byte.

.NET SDK 10.0.100 / Linux main solution build: 0 warnings, 0 errors.

| Suite | Passed | Failed | SQL skipped |
|---|---:|---:|---:|
| ApexPost.Tests | 77 | 0 | 22 |
| ApexPost.Api.Tests | 29 | 0 | 1 |
| Courier.Integration.Tests | 19 | 0 | 0 |
| Total | 125 | 0 | 23 |

The old transport tests use a test authentication handler; the new SecurityTests disable it and validate real signed JWTs. SQL skips require your LocalDB machine and are not verified persistence behavior. Contracts were exported from the running API and NSwag 14.7.1 regenerated the SDK; contract tests pass.

MAUI common C# sources compiled against Controls 10.0.110 reference assemblies with zero warnings/errors. This temporary net10.0 check excluded Platforms and is NOT a native Windows/Android build or UI run. Native packaging/startup, Couchbase, SecureStorage, connectivity events, emulator certificates and end-to-end UI sync remain outstanding. PowerShell scripts were reviewed but not executed on Linux. Record your real demo video; no runtime evidence is fabricated.

No remote files, commits, pushes or merges were changed. No database model/migration changes. Follow PARTD-START-HERE.md on the remote desktop.
