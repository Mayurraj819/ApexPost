# ADR 0015: Retry SQL operations safely and store readable transaction codes

Status: Accepted for this local follow-up; SQL Server verification is recorded separately.

## Context

The review identified inactive retries and numeric transaction status/line codes. The old
unit-of-work wrapper began an explicit transaction but its execution strategy did not retry.
Simply enabling retries could repeat a non-idempotent stock increment or price multiplier
when COMMIT succeeds but its acknowledgement is lost. Existing migrations must remain
usable by databases already deployed from Week 3.

## Decision

Enable SQL Server retry-on-failure (three retries, maximum five-second delay). Keep ordinary
tracked checkout as one SaveChanges, with stable client-generated GUIDs. A normal request
failure is surfaced to the caller; this does not guarantee idempotency across a new HTTP request.

For the two replayable SQL commands, insert an OperationReceipts GUID inside the same explicit
transaction as the business change. ExecuteInTransactionAsync checks that receipt when a
commit result is uncertain. A rolled-back attempt removes both receipt and business changes;
a committed receipt prevents re-execution after acknowledgement loss. Runtime can read/insert
receipts, but cannot delete them. Privileged retention maintenance must keep receipts longer
than the maximum command/retry lifetime. They contain no identity document data.

The callback supports SQL/ExecuteUpdate only, no external effects or tracked SaveChanges.
Pending tracked changes and tracked saves inside the callback fail explicitly. This narrow
contract matches its actual callers. A future mixed tracked/procedure use case must stage once,
use SaveChanges(false), verify its stable operation identity and accept changes only after
success; do not blindly place domain mutations inside the replay callback. The seeder uses
this tracked pattern and verifies its first fixed branch identity.

Map TransactionStatus and TransactionLineType with string converters into varchar(24).
Preserve CLR enums and all business rules. A follow-up migration converts every existing
numeric value, recreates constraints and the covering index, and updates procedures. Invalid
legacy values cause migration failure rather than silent coercion. Down migration maps codes
back to numbers; it refuses ParcelShipping rows that the old constraint cannot represent.

## Alternatives

- Disable all retries: simpler but leaves transient faults untreated.
- Retry the whole command without verification: unsafe for multipliers and increments.
- Add a fresh context per attempt: useful for complex tracked commands, unnecessary for the
  current SQL-only callbacks and would complicate the shared scoped unit-of-work boundary.
- Remove enums from Domain: unnecessary persistence coupling and a wider rewrite.

## Consequences and checks

An infrastructure-only receipt table adds storage and deployment/permission requirements.
Human-readable codes increase index width; regenerate actual plans/storage/read measurements
rather than treating the old integer-schema evidence as current. Tests cover converter values,
production retry registration, an acknowledgement lost after commit, a precommit failure and
single/split aggregate command counts. SQL fault tests require a disposable SQL Server database.
