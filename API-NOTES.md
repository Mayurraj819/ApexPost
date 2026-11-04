# ApexPost API notes — Parts B and C

All routes operate on the same SQL Server tracking service. In Part B the prefix is
`/api/shipments`; Part C retains that v1 alias and adds `/api/v1/shipments` and `/api/v2/shipments`.
Use the Development-only Swagger UI at `/swagger`. Authentication is implemented in Part D,
not claimed by this Part B/C package.

| Method and relative route | Success | Failure examples | Description |
|---|---|---|---|
| GET /shipments | 200 | 400 invalid page/filter | page=1, pageSize=20 by default; maximum 50; optional status/city |
| GET /shipments/{trackingNumber} | 200 | 404 unknown | Fetch one shipment |
| POST /shipments | 201 + Location | 400 invalid DTO, 409 duplicate, 415 media | Create with APX- plus eight digits |
| PUT /shipments/{trackingNumber} | 200 | 400, 404, 409 concurrent write, 415 | Replace all editable details; status is never bound |
| PATCH /shipments/{trackingNumber}/status | 200 | 400, 404, 409 invalid transition, 415 | Advance through the domain workflow |
| DELETE /shipments/{trackingNumber} | 204 | 404, 409 non-Created | Delete a Created shipment and any child scans |
| POST /shipments/{trackingNumber}/scans | 200 | 400 invalid envelope/fields/time, 404, 409 database conflict, 415 | Idempotent bulk upload, 1–100 scans |
| GET /shipments/{trackingNumber}/scans | 200 | 404 | UTC-ordered scan history |

Unhandled errors use 500 ProblemDetails. Validation uses 400 ValidationProblemDetails;
other errors use ProblemDetails with a traceId. Non-Development responses hide internal
500 details. Empty router/media errors are filled by status-code pages. No endpoint returns
200 to represent an exception. A processed upload can legitimately return 200 with individual
business rejections in its result.

Workflow: Created → PickedUp → InTransit → OutForDelivery → Delivered or Failed.
Repeating a status or skipping a step returns 409. PUT is a replacement, not a partial update.
Phone format permits an optional plus followed by 7–15 digits, starting with a nonzero digit;
it is format validation, not proof that a phone number exists. Weight is kilograms, 0.1–70.
Numeric JSON properties must be JSON numbers, not quoted numeric strings.

Scans have a globally unique Part A `scan::{Guid}` ID. IDs are normalized to canonical GUID
text. Identical retransmissions count as duplicatesSkipped. Reusing an ID with different
content/shipment is rejected with a reason. A route/body tracking mismatch is rejected per
item; invalid field shapes or future timestamps fail automatic validation for the whole request.
The result includes accepted/duplicate IDs and individual rejection reasons for Part D's sync.
Uploading history does not silently advance shipment status; PATCH controls that workflow.

Same-shipment uploads serialize using a SQL update lock. A receipt is saved atomically with
the batch and verifies an uncertain commit before replay. The tracked state is rebuilt on a
rolled-back retry. The unique scan primary key remains the final deduplication boundary.
Concurrent reuse of an ID across different shipments may surface a 409 and can be retried
for a per-item rejection. No business calls or network effects occur inside the transaction.

## Week 3 compatibility and necessary schema additions

The original Shipments rows require a paid counter transaction, zone, service level and charge.
The assignment's new create payload supplies none of these and introduces a different delivery
lifecycle. Inventing counter sales or overwriting that aggregate would break Week 3 rules.
Instead, new TrackedShipments/CourierScans tables model the operational API, reuse the existing
ApexPostDbContext, audit conventions, SQL Server connection, execution strategy, unit of work
and DI registration. Existing Week 3 entity/configuration/repository/service files are unchanged.
There is one database; this is not an in-memory API or a replacement SQL implementation.

The additive EF configuration and migration are necessary new storage for the new service.
The existing model snapshot and one hard-coded migration-count test are updated accordingly;
Week 3 business-rule tests are unchanged. If the trainer interprets 'do not modify the data layer'
as forbidding even additive schema, clarify that conflict before submission. A tracking/scan
API cannot persist its new fields using only the original required counter-shipment columns.

## Checks and evidence

`ApexPost.Api.http` contains success and failure cases for every endpoint in both versions.
`ApexPost.Api.Tests` tests domain transitions, real HTTP binding/serialization/error middleware,
OpenAPI/deprecation, generated SDK interoperability and SQL persistence/deduplication.
HTTP tests replace only persistence with a test double; SQL tests are separate and explicitly
skip without APEXPOST_TEST_SQLSERVER. Skipped SQL tests are not a persistence pass.

Capture screenshots yourself: Swagger's two versions, a 400 validation body, 409 conflict,
and 201 with Location. No screenshots or SQL run results are fabricated by this package.
