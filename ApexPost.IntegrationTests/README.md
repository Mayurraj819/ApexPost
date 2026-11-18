# ApexPost.IntegrationTests

Real repository and API/database integration tests. SQL Server and Couchbase lifecycle fixtures will be added in the infrastructure stage. A fake DbContext or MemoryScans is not persistence proof. No tests or automatic database setup are implemented at the foundation stage.

See [TESTING-STRATEGY.md](../TESTING-STRATEGY.md) in the solution root. Use explicit Arrange / Act / Assert sections and `Method_Scenario_ExpectedResult` names. Never use production credentials/data or external HTTP.
