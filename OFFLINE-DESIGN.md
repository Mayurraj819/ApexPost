# ApexPost Courier Offline Design — Part A

## Local-first persistence

Couriers must record shipment events even when network coverage is
unavailable. The console therefore saves every new scan to Couchbase Lite
before attempting an upload. New scans always have syncStatus = Pending.

CourierDatabase owns the local apexpost_courier database and its default
collection. Documents use scan::{Guid} identifiers and contain trackingNumber,
scanType, courierId, location, scannedAtUtc, notes and syncStatus. Timestamps
are normalized to UTC. Enum values are stored as readable strings.

ShipmentScan and the repository/uploader interfaces contain no Couchbase
types. Couchbase-specific persistence and mapping remain separate from the
model. The existing Week 3 SQL Server and EF Core implementation is unchanged.

## Queries and indexes

GetById uses a direct key-value lookup. Parameterized SQL++ queries provide
tracking-number history ordered by scannedAtUtc, pending scans, and grouped
scan-type counts for a courier. Document ID provides a deterministic
tie-break when timestamps match.

Value indexes cover trackingNumber and syncStatus. Query text is fixed;
user input is supplied through parameters rather than concatenation.

## Outbox and upload results

The scan documents also serve as the Part A outbox. OutboxService reads
Pending scans and calls IShipmentUploader. Successful responses change the
local status to Synced; unsuccessful responses or upload exceptions change
it to Failed. Cancellation is propagated without marking the current scan
as failed. Failed documents remain available locally.

FakeShipmentUploader simulates success or failure without network access.
Synced therefore means simulated acceptance in Part A, not delivery to a
real server. Failed scans are not automatically retried by the current
pending-only sync operation.

A future real uploader should use the stable scan ID as an idempotency key.
This prevents duplicate server records if an upload succeeds but its
acknowledgement is lost before the local status is updated.

## Conflict policy

Local saves and deletes use FailOnConflict to detect changes occurring
between loading a document and saving or deleting it. This is not a complete
cross-device conflict-resolution policy.

Last-write-wins is simple, but device clocks may differ and a later edit
could overwrite an important delivery event. For later parts, the server
should validate competing events and decide the authoritative shipment
state, retaining scan history where appropriate. Part A does not implement
server-side conflict resolution.

## Database ownership and listener lifecycle

Each running courier application should own its database directory.
Several processes must not share the same embedded database files in this
design: competing ownership, file locking and shutdown lifecycles make that
an unsuitable communication mechanism. Separate devices/processes should
use separate local stores and exchange data through the later API.

A collection change listener logs changed document IDs and queries the
current Pending count. Notifications may group several changes; the count
represents current database state. CourierDatabase retains the listener
token, removes it before disposing the database, and prevents callbacks
from querying the database during shutdown.

## Verification evidence

Manual checks covered creation, tracking lookup, persistence after restart,
successful and failed simulated uploads, grouped counts and deletion.
Screenshots of query results and listener output are stored under
evidence/week4-partA/.