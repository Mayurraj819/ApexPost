# ApexPost API versioning

URL-segment versions use Asp.Versioning.Mvc and its API Explorer groups v1/v2.
DefaultApiVersion is 1.0 for the retained `/api/shipments` alias. Explicit clients should use
`/api/v1/shipments` or `/api/v2/shipments`. Both call IShipmentApiService.

| Change | Classification | Reason |
|---|---|---|
| Add explicit v1/v2 URLs while keeping original URLs | Non-breaking | Existing Part B requests continue to work |
| Group recipientName/recipientPhone/address/city into recipient.name/phone/address/city in v2 responses | Breaking | Response property paths change |
| Return status as a string enum in v2 instead of the v1 numeric enum | Breaking | JSON token type and client deserialization change |
| Add nullable latestScan with scanType/location/scannedAtUtc in v2 | Additive, normally non-breaking | Tolerant clients ignore unknown properties; null means no scans yet |
| Keep create/update/status/scan request contracts unchanged | Non-breaking | Only response DTO mapping differs between versions |
| Report api-supported-versions and api-deprecated-versions | Non-breaking | Additional informational response headers |

v1 is explicitly Deprecated=true. Its OpenAPI operations are marked deprecated, and the
Swagger dropdown labels it v1 (deprecated). v2 is supported. The legacy alias has distinct
route names and is omitted from duplicate documentation only; that is not security.

## Deprecation and sunset plan

Announce deprecation with this release and publish v2 migration examples and generated SDK.
Proposed sunset: 1 February 2027, subject to trainer/customer agreement and confirmation that
clients have migrated. This is a documented plan, not a claim that v1 has already been removed.
Keep v1 functional until that date is approved. Monitor usage without logging request bodies;
notify consumers, then remove v1 in a separately reviewed breaking release.

## Contracts and regeneration

`contracts/v1.json` and `contracts/v2.json` are exports from the actual built-in generator.
OpenAPI 3.0 output is selected for the pinned NSwag tool; no second generator is used.
Review their diff whenever DTOs/routes/statuses change. Stable route names become operationIds
and generated SDK methods (for example GetShipmentV2Async).

Run the API in Development, then `tools/Refresh-ApiContracts.ps1` to export both documents
and run the pinned NSwag configuration. `nswag.json` and the local tool manifest are committed.
Generated C# files are never edited. The JsonStringEnumConverter extension is in a separate
partial class, because the generator does not automatically attach it to all string-enum responses.
A generated-client integration test protects this serialization boundary.

Tests cover version-specific JSON, stable operations, deprecation headers and production docs
being unavailable. Live Swagger Try it out and screenshots still require the user's SQL-backed
Development run; HTTP TestServer checks are not claimed as manual Swagger screenshots.
