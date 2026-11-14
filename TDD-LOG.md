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

The test uses fictional, fixed inputs and requires no database or network.
It verifies the domain rule; it does not yet prove the API returns 409
or that persisted details remain unchanged.

The full regression suite still needs to be run after this change.