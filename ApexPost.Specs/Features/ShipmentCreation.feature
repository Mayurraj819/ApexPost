@sql @bdd
Feature: Shipment creation
  As a courier
  I want to create shipments with valid details
  So that parcels can be tracked without duplicate shipments

  Background:
    Given an authenticated courier
    And the isolated test database is empty

  Scenario: Create a shipment with valid details
    Given the courier has a valid shipment request
    When the courier creates the shipment
    Then the response status should be 201 Created
    And the Location header should point to a working shipment resource
    And the persisted shipment should match the submitted request

  Scenario: Reject duplicate tracking numbers
    Given a shipment already exists with tracking number "APX-12345678"
    And the courier has a valid shipment request with tracking number "APX-12345678"
    When the courier creates the shipment
    Then the response status should be 409 Conflict
    And only one shipment record exists for that tracking number
    And the existing shipment details are unchanged

  Scenario Outline: Reject invalid shipment weights
    Given the courier has a shipment request with weight "<weight>" kilograms
    When the courier creates the shipment
    Then the response status should be 400 Bad Request
    And no shipment is persisted

    Examples:
      | weight |
      | 0      |
      | 0.09   |
      | 70.01  |
