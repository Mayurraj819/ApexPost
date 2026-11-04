# Week 4 Parts B/C implementation coverage

| Requirement | Code/evidence |
|---|---|
| B1 API project, minimal host, DI, thin controllers, route constraint | ApexPost.Api/Program.cs and Controllers; AddInfrastructure plus AddShipmentApiServices |
| B2 eight REST operations, pagination/filtering, workflow | Controllers, Application/Delivery, Domain/Delivery/TrackedShipment.cs, Infrastructure/Delivery |
| B3 HTTP status codes and Location | CreatedAtAction, global handler/status pages; TransportTests |
| B4 four request DTOs, automatic validation, custom future-time rule | Contracts/ShipmentContracts.cs; validation and domain tests |
| B5 ProblemDetails and IExceptionHandler | Errors/ApiExceptionHandler.cs; production test |
| B6 scan IDs, idempotence and count/reason result | ShipmentApiService.UploadAsync, CourierScans PK, SQL receipt; SQL test pending a configured server |
| C1 built-in OpenAPI and document transformer | AddOpenApi/MapOpenApi and OpenApi/LogisticsDocumentTransformer.cs |
| C2 Development Swagger UI without a second generator | SwaggerUI-only package, Program.cs, README; environment test |
| C3 XML comments, response metadata, tags, summaries, route names | API contracts/controllers and exported operationIds; Swagger manual exercise pending |
| C4 v1 deprecated, v2 nested recipient/latestScan/string status | Asp.Versioning explorer groups, response mapping; JSON/header tests |
| C5 separate documents and dropdown | contracts/v1.json and v2.json; OpenAPI tests |
| C6 generated C# SDK and console consumer | ApexPost.Client, ApexPost.Client.Console, nswag.json, tool manifest, GeneratedClientTests |
| C7 saved contracts and migration plan | VERSIONING.md; contract drift assertions |

## Preservation and limits

Part A source is unchanged. Existing Week 3 entities/configurations/repositories/services are
unchanged. New operational tracking storage is additive and reuses the same database context;
API-NOTES.md explains why the old counter-sale shipment entity cannot satisfy the new payload.
The shared model snapshot is necessarily extended. One infrastructure migration-count assertion
now counts available migrations instead of hard-coding four. Business-rule test bodies are unchanged.

Transport tests use a test double; they do not claim real SQL correctness. See
`evidence/week4-partBC/RESULTS.md` for actual results and pending SQL verification. Run the
SQL-backed tests before changing the development database. Capture the four requested
screenshots, exercise every endpoint in Swagger, then review and merge the Part B and Part C PRs.
Security/JWT/partner API keys/MAUI/Polly belong to Part D and are not claimed in this package.
