# Week 5 TDD Log

## Requirement

A Delivered shipment's editable details cannot be replaced.

Previously, TrackedShipment.ReplaceDetails validated the supplied values
but allowed changes after delivery.

## RED — bb70931

Added ReplaceDetails_WhenDelivered_ThrowsAndPreservesDetails.

The test advances a shipment through the valid lifecycle to Delivered,
then attempts to replace its details with otherwise valid values.

Expected: DeliveryConflictException and all original details preserved.

Actual: 1 failed. Assert.Throws reported that no exception was thrown.

Evidence: evidence/week5/tdd/red.txt

## GREEN — d11b3d8

Added a Delivered-status guard at the start of
TrackedShipment.ReplaceDetails, before validation or field assignments.

The guard throws DeliveryConflictException.

Targeted result: 1 passed, 0 failed.

Evidence: evidence/week5/tdd/green.txt

## REFACTOR — 2262c0b

Extracted CreateDeliveredShipment in the test class to separate lifecycle
setup from the behavior being tested.

Production behavior and all assertions remained unchanged.

Targeted result: 1 passed, 0 failed.

Evidence: evidence/week5/tdd/refactor.txt

## Testability and limitations

The existing public domain API was sufficient. No new interface,
dependency injection or clock abstraction was needed for this rule.

The domain test uses fictional, fixed inputs and requires no database or network.
The later real API/SQL test
`ShipmentApiDatabaseTests.Replace_DeliveredShipment_Returns409AndPreservesAllPersistedDetails`
advances status through the HTTP API, verifies PUT 409, and checks every original
editable field and Delivered status in a fresh database context.

Evidence: evidence/week5/api-database.txt (4 passed), followed by the full post-cleanup
run evidence/week5/tests-after-sql-cleanup.txt (197 passed, 0 failed, 0 skipped).
Build: evidence/week5/build-after-sql-cleanup.txt (0 warnings, 0 errors).

The three TDD commits remain separate in the pushed week5-testing branch. Later API, repository and BDD tests provide additional regression evidence.
Preserve the original RED/GREEN/REFACTOR commits on merge.
