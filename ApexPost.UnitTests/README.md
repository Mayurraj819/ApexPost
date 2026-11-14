# ApexPost.UnitTests

Isolated unit tests: domain rules, validation, application/outbox orchestration with Moq, and no-network typed-client behavior. FakeTimeProvider is available for deterministic timing work. No tests have been added at the foundation stage.

See [TESTING-STRATEGY.md](../TESTING-STRATEGY.md) in the solution root. Use explicit Arrange / Act / Assert sections and `Method_Scenario_ExpectedResult` names. Never use production credentials/data or external HTTP.
