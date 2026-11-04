# Parts B/C verification — 4 November 2026

Source: Mayurraj819/ApexPost, week4-partB, 5ffe9f0524e54daf43b955368240319c982bbdbb.

- Full eleven-project solution builds with zero warnings and zero errors.
- API/domain/generated-SDK tests: 21 passed, 1 SQL Server test skipped, 0 failed.
- Existing Week 3 tests: 77 passed, 22 SQL Server tests skipped, 0 failed.
- Combined: 98 passed, 23 skipped, 0 failed.
- Generated SDK was produced by pinned NSwag 14.7.1 from the live built-in v2 document.
- SDK TestServer test covers AddHttpClient, create/get, two-scan upload, deduplication and typed 404 handling.
- Both committed contracts match the application's generated OpenAPI in automated tests.
- Production tests confirm OpenAPI/Swagger are unavailable and internal 500 detail is hidden.
- EF reports no model changes since the additive tracking migration; idempotent SQL script generated.

No SQL Server test connection was available here. SQL persistence, migration execution,
concurrent database deduplication and restricted-role permissions still require the user's
private SQL Server. A skipped test is not a pass. Real Swagger Try it out/screenshots and
branch PR/merge actions remain user verification/publication steps. No remote push was made.
