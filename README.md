# ApexPost Counter Platform

ApexPost is a .NET 10 MVC counter platform for retail, service, shipping, and money-order workflows. The application uses a layered architecture with JSON-backed repositories, secure ASP.NET Core authentication, and audit-friendly middleware and filters.

## Prerequisites

- .NET 10 SDK
- Visual Studio 2026 or newer, or a compatible .NET 10 CLI environment
- Optional: `dotnet user-secrets` for local configuration values

## Solution layout

- `ApexPost.Domain` – domain entities, aggregates, value objects, and validation rules
- `ApexPost.Application` – application services, repository contracts, and report definitions
- `ApexPost.Infrastructure` – JSON persistence and infrastructure wiring
- `ApexPost.Web` – MVC UI, authentication, middleware, and web pipeline
- `ApexPost.Tests` – xUnit unit and WebApplicationFactory integration tests

## Build and run

```bash
dotnet restore ApexPost.slnx
dotnet build ApexPost.slnx -nologo -v minimal
dotnet run --project ApexPost.Web
dotnet test ApexPost.slnx --logger "console;verbosity=minimal"
```

## Demo accounts

The app accepts either a direct password in configuration or an environment variable reference. The defaults in `ApexPost.Web/appsettings.Development.json` point to the following variable names:

- `APEXPOST_CLERK_PASSWORD`
- `APEXPOST_MANAGER_PASSWORD`
- `APEXPOST_AUDITOR_PASSWORD`
- `APEXPOST_ADMIN_PASSWORD`

Example local setup using config values:

```json
"ApexPost": {
  "DemoAccounts": {
    "Accounts": [
      {
        "Username": "clerk",
        "Password": "LocalClerk123!",
        "PasswordEnvironmentVariable": "APEXPOST_CLERK_PASSWORD"
      }
    ]
  }
}
```

Or use environment variables/user secrets instead of committing secrets to source control:

```bash
dotnet user-secrets set "ApexPost:DemoAccounts:Accounts:0:PasswordEnvironmentVariable" "APEXPOST_CLERK_PASSWORD"
dotnet user-secrets set "ApexPost:DemoAccounts:Accounts:1:PasswordEnvironmentVariable" "APEXPOST_MANAGER_PASSWORD"
```

Then export the actual values in your local shell or define them in the OS environment before launching the app.

## Security configuration

- `ApexPost:Security:IntegrationApiKeys` contains valid API keys for `/integration/*` endpoints.
- `ApexPost:Security:IntegrationAllowedIpAddresses` controls the IP allowlist for integration traffic.
- `ApexPost:Security:LoginRateLimitPermitLimit` and `LoginRateLimitWindowSeconds` configure the login throttling window.
- `ApexPost:Security:MaintenanceMode` enables safe maintenance mode with `503` responses for non-health endpoints.

Use environment variables or user secrets rather than committing secrets to source control.

## Features implemented

- JSON-based branch, product, inventory, and transaction persistence.
- Domain rules for branch availability, staff authorization, stock limits, payments, and manager-only correction workflows.
- Mixed retail/service transaction logic and tax-aware totals.
- Shipping and parcel weighting rules with pricing and status handling.
- Money-order validation with identity verification and masked document handling.
- MVC login, dashboard, catalogue, transaction, shipping, money order, and report pages.
- MVC filters for validation, performance, and audit recording.
- Correlation IDs, request logging, structured exception handling, and secure headers.
- xUnit and WebApplicationFactory coverage for web and domain behavior.

## Example requests

Health check:

```bash
curl -i https://localhost:5001/health
```

Integration endpoint with API key:

```bash
curl -i -H "X-API-Key: phase6-test-key" https://localhost:5001/integration/ping
```

Login page:

```bash
curl -i https://localhost:5001/account/login
```

## Working data files

Seed and runtime data live under `ApexPost.Web/data` and include the JSON-backed branch, product, transaction, zone, and audit data used by the application.

## Screenshots and sample artifacts

This repository includes functional sample artifacts and output data in the application data directory rather than static mock-only screens. Add screenshots to the project documentation as needed for local demos.
