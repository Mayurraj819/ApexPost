# Resilience — Week 4 Part D

CourierRegistration adds typed ApexApiClient via builder.Services/IHttpClientFactory and Microsoft.Extensions.Http.Resilience (Polly). AuthHeaderHandler attaches the SecureStorage bearer. No per-call HttpClient is constructed; the factory pools handlers. Every retry logs attempt/status/delay, and Open/HalfOpen/Closed callbacks log/update the UI without credentials/bodies.

| Setting | Value | Reason |
|---|---|---|
| Attempts | Initial + 2 retries = 3 total | Bound repeated work/battery |
| Delay | 1 second base; exponential with jitter | Spread retry bursts |
| Retry outcomes | Network faults, attempt timeout, HTTP 408/5xx | Transient failures |
| Exclusions | All other 4xx, including 429; caller cancellation | Require user/domain action |
| Attempt timeout | 5 seconds | Bound stalled send |
| Pipeline overall | 30 seconds | Bound sends and backoff |
| Body-inclusive cancellation | 30 seconds | Also bound streaming/deserialization outside handler pipeline |
| Circuit | 50% qualifying failures, minimum 3 outcomes in 30 seconds | Stop repeated outage calls |
| Break | 10 seconds | Permit quick recovery probe |
| Wake | Connectivity Internet event, Sync now, or 15-second interval | Reconnect and probe recovered server |

The brief both permits HTTP 408 and says never retry 4xx: this implementation follows its explicit 408 exception; all other 4xx are excluded. Three attempts means two retries, not four attempts. The circuit counts individual attempt outcomes, so one logical call can open it during its retries. A half-open successful probe closes it. Connectivity is a hint, not proof the API is reachable.

Only GET/HEAD and the specifically marked scan-upload POST retry. General POST/PUT/PATCH/DELETE do not. Scan IDs are generated once locally and reused unchanged; Part B's unique scan IDs, transactions and duplicate responses make replay safe after lost acknowledgements. Group Pending scans by shipment route; upload at most 20 sequentially. A gate prevents overlapping sync runs. Accepted and duplicate IDs mark Synced; explicit rejected IDs mark Failed. Every submitted ID must appear exactly once with consistent counts and no extra IDs. Malformed/null/incomplete/oversized JSON retains Pending. Unknown properties are ignored; the shared sourcegen context uses camelCase, case-insensitive reads and string enums. ProblemDetails/validation errors become bounded friendly messages.

401 clears credentials and retains Pending. Explicit 400/403/404/415 batch errors mark Failed and display a reason; Retry failed requeues after correction. Network/timeout/server/circuit failures retain Pending and show “Server unavailable – working offline”. Local scans are never deleted by sync. MAUI saves through the Part A Couchbase repository first; collection listener updates pending count. ConnectivityChanged signals the worker; on destruction it cancels/awaits the worker before disposing database/listener. Windows Work offline simulates disconnection without cutting RDP; literal airplane mode requires an emulator/device. The Part A console retains its standalone fake teaching uploader; MAUI DI uses the real implementation.

Tests use the real resilience pipeline with controlled handlers for attempts, excluded errors, unsafe writes, header attachment, timeouts/cancellation, circuit recovery, batches, ownership, malformed results and per-ID outcomes. They do not replace native SecureStorage, MAUI network-event, Couchbase and end-to-end UI execution. See the verification checklist.
