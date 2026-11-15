# Week 5 requirement-to-evidence map

Reviewed code revision: adc53976c978ff1a2f185b1eef0f88284fde9dac.
Verified Windows execution: 197 passed, 0 failed, 0 skipped; zero build warnings/errors.

| Assignment requirement | Implementation / evidence | Status and limit |
| --- | --- | --- |
| Strategy written first | Initial strategy commit 8d142b3 precedes foundation and TDD commits | Implemented; final strategy revision records actual decisions without rewriting history. |
| At least five risks, pyramid, framework choice | TESTING-STRATEGY.md sections 2–3 | Implemented; original target and actual new-work proportions are both disclosed. |
| Row per existing test class | Section 5, 27 baseline classes | Implemented; retained versus actually modified classes distinguished; no deleted class. |
| Database strategy and limitations | Sections 6–8 | Real SQL/native integration; EF query logic not mocked. SQLite is optional and not falsely claimed. |
| Organized test projects | UnitTests, IntegrationTests, Specs plus documented retained equivalents in ApexPost.slnx | Implemented; legacy projects contain mixed test levels. |
| AAA, names, two parameterized rules, boundaries/exceptions | ShipmentBoundaryTests, DeliveredShipmentTests, OutboxServiceTests, ApexApiClientContractTests | New tests implement these; old class naming/style was retained rather than fully rewritten. |
| Test Explorer and dotnet test, automatic data setup | Existing runners, Reqnroll generation, per-case SQL/native lifecycle | Implemented on installed Windows LocalDB; engines/SDK remain prerequisites. |
| Moq Setup/ReturnsAsync/matching/Verify Times | OutboxServiceTests and reviewed output | Implemented; one redundant post-action Setup remains a readability issue. |
| Typed client 200 JSON, 404, 500, cancel/timeout, ProblemDetails | New contract tests plus retained CourierTests resilience checks | Implemented with fake HTTP only; old real timer waits remain a determinism limit. |
| Repository integration; one real API WAF/database/test-auth case | CouchbaseRepositoryTests (15), ShipmentApiDatabaseTests (4) | Implemented; test authentication is not evidence for JWT cryptography. |
| New TDD business rule, separate RED/GREEN/REFACTOR | Delivered details rule; bb70931, d11b3d8, 2262c0b; TDD-LOG.md | Implemented; preserve these commits on merge. |
| Two Reqnroll features; Background/Outline/Examples; injected state/hooks | ShipmentCreation (5), ScanUpload (3), state and tagged async hooks | Implemented at API/SQL level; no claim of full native-outbox-to-server BDD. |
| Actual Copilot service test and feature/binding draft; review | COPILOT-LOG.md, original draft/review, assisted comments | Implemented; tool-specific attribution is recorded in COPILOT-LOG.md. |
| Two generated tests shown failing after mutations and restored | evidence/week5/copilot/mutation-01/02 failed and restored logs | Implemented; no mutation remains in production code. |
| README commands and full-suite summary/evidence | README Week 5 section and post-cleanup logs | Implemented; historical logs are not confused with this final code run. |
| Reflection and five-minute viva | Strategy section 13 and VIVA-NOTES.md | Reflection based on observed results; speaking outline covers the required viva topics. |
| No MAUI/browser/external HTTP/production database | Backend-only solution and fake HTTP/TestServer/private SQL/native fixtures | No new work in excluded scope; retained server-side MVC tests do not automate a browser. |
| Deterministic tests, no clock/order/global-state reliance | New fixed-input tests and per-case SQL/native state | **Partial:** legacy clock/timer/audit-polling/global configuration dependencies remain. See section 7. Passing results do not close this constraint. |
| week5-testing merged with TDD history | Pushed feature branch; default repository target is master, equivalent to the brief's main | **Merge pending:** perform a normal merge only when ready; not yet completed. |

Code coverage was not measured. Outstanding technical constraints are recorded above.
