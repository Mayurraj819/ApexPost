# ApexPost Counter Platform Design

## Overview

ApexPost is a layered .NET 10 MVC platform for branch operations, retail counter transactions, shipping, and money-order processing. The system keeps all business rules in the domain and application layers, while MVC controllers remain thin and focused on request/response handling.

## Layered architecture

- Domain: value objects, entities, enums, and invariants such as branch availability, stock limits, and identity masking.
- Application: repository contracts, services for reporting, shipping pricing, transaction processing, identity verification, and staff workflow logic.
- Infrastructure: JSON persistence adapters and DI registration for repositories and services.
- Web MVC: controllers, views, security middleware, action filters, and the ASP.NET Core pipeline.
- Tests: xUnit unit coverage and WebApplicationFactory integration coverage for security and workflow behavior.

## Runtime pipeline

```text
HTTP request
  -> CorrelationIdMiddleware
  -> ExceptionHandlingMiddleware
  -> RequestLoggingMiddleware
  -> MaintenanceModeMiddleware
  -> HSTS/HTTPS redirection
  -> SecurityHeadersMiddleware
  -> Routing
  -> ApiKeyMiddleware / IpAllowlistMiddleware
  -> Authentication + Authorization
  -> Rate limiting
  -> MVC controller + filters
  -> Response
```

## Persistence model

The platform intentionally avoids EF Core, Identity, and database storage. JSON files are used behind repository interfaces:

- `ApexPost.Web/data/branches.json`
- `ApexPost.Web/data/products.json`
- `ApexPost.Web/data/transactions.json`
- `ApexPost.Web/data/audit.jsonl`
- `ApexPost.Web/data/shipping-zones.json`

This gives the application a simple storage model while keeping the domain free of framework-specific dependencies and lowering operational complexity.

## Security and authorization

Authentication uses ASP.NET Core cookie auth with role and branch claims. The app validates the user identity before actions that require manager, same-branch, or admin access. The same-branch requirement ensures staff only operate within their assigned branch, and manager-only actions are restricted to active branch managers.

The security pipeline also includes:

- correlation IDs and response headers
- structured request logging and timing
- exception-to-ProblemDetails conversion
- maintenance mode with health endpoint exemption
- security headers for CSP, frame denial, and nosniff
- API-key and IP allowlist protections for integration endpoints
- rate limiting for login-related traffic

## Event and notification model

Business-impacting events such as low-stock conditions, high-value money orders, voids, and refunds are represented through domain/application event patterns and observer-style notifications. This keeps side effects separate from transaction state changes and helps avoid duplicate notifications on failed workflows.

## Audit model

Audit records are written through `IAuditStore` and persisted append-only as JSON Lines. The audit filter captures controller/action metadata, route values, correlation IDs, status codes, and timing while avoiding passwords, API keys, identity document numbers, and other sensitive payloads.

## Design trade-offs

- JSON repositories favor simplicity and easy deployment over high-scale concurrent storage.
- Domain validation remains explicit and straightforward to support testability and predictable error handling.
- Middleware is used for cross-cutting concerns, while filters handle MVC-specific action metadata and auditing.
- The app keeps configuration externalized to environment variables, user secrets, and appsettings so secrets are not committed to source control.
