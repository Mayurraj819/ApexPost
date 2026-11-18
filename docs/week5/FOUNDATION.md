# Week 5 test foundation

This stage follows the separately committed TESTING-STRATEGY.md. It adds three net10.0 test projects and solution membership only. No new behavioral tests, BDD scenarios, production implementation, clock changes or database fixtures are delivered here. Existing tests remain untouched.

## Packages

All three projects use the same xUnit 2.9.3, runner 3.1.4 and Microsoft.NET.Test.Sdk 17.14.1 already used by the earlier suites. UnitTests adds Moq 4.20.72 and Microsoft.Extensions.TimeProvider.Testing 10.1.0. Specs adds Reqnroll.xUnit 3.3.4; it brings its MSBuild generator transitively. The generated feature code will live under obj. API integration projects use Microsoft.AspNetCore.Mvc.Testing 10.0.13, aligned with the API. No NUnit, test UI or browser project is added.

## Verify this stage

```powershell
dotnet build ApexPost.slnx -m:1
```

Visual Studio: reopen ApexPost.slnx and confirm the three projects are visible. Test Explorer will not yet show tests for their empty assemblies; this is intentional. The existing 148-case Week 4 total is historical and does not increase merely by adding projects. This stage does not claim a fresh Windows/SQL full-suite result.

## Next stages

1. Commit this foundation separately from the strategy.
2. Add the compiling new Delivered-details rule test, run and record its real assertion failure, commit RED:.
3. Add minimal implementation and record GREEN:, then refactor and record REFACTOR:.
4. Implement deterministic Moq/client coverage and automatic isolated SQL/Couchbase infrastructure; then Reqnroll features/hooks and real Copilot drafting/review/mutation evidence.

Do not squash the TDD evidence commits. No push or merge is performed by the foundation installer.
