@sql @bdd
Feature: Courier scan acceptance
  As a courier
  I want scan uploads to be safe to repeat
  So that delivery history is neither duplicated nor overwritten

  Background:
    Given an authenticated courier
    And the isolated test database is empty
    And a shipment already exists with tracking number "APX-12345678"

  Scenario: Accept two new scans for a shipment
    Given the courier has two valid scans for the shipment
    When the courier uploads the scans
    Then 2 scans are accepted, 0 duplicates are skipped and 0 scans are rejected
    And the shipment history contains the two original scans exactly once

  Scenario: Skip scans that were already accepted
    Given the courier has two valid scans for the shipment
    And those scans were already accepted
    When the courier uploads the scans
    Then 0 scans are accepted, 2 duplicates are skipped and 0 scans are rejected
    And the shipment history contains the two original scans exactly once

  Scenario: Reject changed content under an existing scan identifier
    Given the courier has two valid scans for the shipment
    And those scans were already accepted
    And the courier changes the first scan notes without changing its identifier
    When the courier uploads only the changed scan
    Then 0 scans are accepted, 0 duplicates are skipped and 1 scans are rejected
    And the changed scan identifier is reported as rejected
    And the shipment history contains the two original scans exactly once
