# ApexPost Web middleware order

1. `CorrelationIdMiddleware` establishes or propagates `X-Correlation-ID`.
2. `ExceptionHandlingMiddleware` converts unhandled exceptions to safe Problem Details.
3. `MaintenanceModeMiddleware` rejects non-health requests while maintenance mode is enabled.
4. `UseHttpsRedirection` and HSTS enforce transport security.
5. `SecurityHeadersMiddleware` adds browser security headers.
6. Static files are served.
7. Routing selects endpoints and exposes route metadata.
8. `ApiKeyMiddleware` and `IpAllowlistMiddleware` protect `/integration/*`.
9. Authentication establishes the user; authorization evaluates policies.
10. Rate limiting applies endpoint-specific limits.
11. MVC endpoint execution runs audit, validation, and performance filters.

Secrets, API keys, passwords, authorization headers, identity document numbers, and request bodies are excluded from request logs and audit events.

## Configuration and lifetimes

- `WebSecurityOptions` is bound from `ApexPost:Security` using configuration, environment variables, or user-secrets.
- Middleware is registered as singleton-safe components; request state is read from `HttpContext`.
- MVC filters are registered through DI and are transient per MVC request.
- No secret value belongs in source control. Use `dotnet user-secrets` locally or deployment secret stores.
