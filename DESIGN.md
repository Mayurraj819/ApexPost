# ApexPost — Week 3 design

The application retains five primary projects. Domain references no application/framework project. Application owns contracts and use-case rules. Infrastructure implements SQL mapping/repositories/query services; Web is the composition root. An optional evidence console references Infrastructure to reproduce measurements.

```mermaid
flowchart TD
 Web["Web: MVC and composition"] --> App["Application: commands, queries, contracts"]
 Web --> Infra["Infrastructure: EF Core SQL Server"]
 Infra --> App
 App --> Domain["Domain: entities and invariants"]
 Infra --> SQL["SQL Server: ApexPostDb"]
 Tests["Tests: rules, SQL and MVC"] --> Web
 Tests --> Infra
```

## Relational model

There are 19 tables: the 17 named in Parts A–H, Regions for region revenue reporting, and OperationReceipts for uncertain-commit verification. Owned Address, OpeningHours and optional staff IdentityDocument share their owner's table; Money amount/currency values are flattened without JSON serialization.

```mermaid
erDiagram
 Regions {
     uniqueidentifier Id PK
     varchar Code UK
     nvarchar Name
 }
 Branches {
     uniqueidentifier Id PK
     uniqueidentifier RegionId FK
     varchar Code UK
     nvarchar Name
 }
 Counters {
     uniqueidentifier Id PK
     uniqueidentifier BranchId FK
     int Number
 }
 Roles {
     int Id PK
     nvarchar Name UK
 }
 Staff {
     uniqueidentifier Id PK
     uniqueidentifier BranchId FK
     int Role FK
     varchar Email UK
     bit IsActive
 }
 ProductCategories {
     int Id PK
     nvarchar Name UK
 }
 Products {
     uniqueidentifier Id PK
     varchar Sku UK
     int Category FK
     decimal UnitPriceAmount
     varchar UnitPriceCurrency
 }
 BranchStock {
     uniqueidentifier Id PK
     uniqueidentifier BranchId FK
     uniqueidentifier ProductId FK
     int Quantity
     rowversion RowVersion
 }
 StockAdjustments {
     uniqueidentifier Id PK
     uniqueidentifier BranchId FK
     uniqueidentifier ProductId FK
     int Delta
     nvarchar Reason
 }
 ShippingZones {
     uniqueidentifier Id PK
     varchar Code UK
     nvarchar Name
 }
 ShippingRates {
     uniqueidentifier Id PK
     uniqueidentifier ZoneId FK
     int ServiceLevel
     decimal BaseRate
     decimal PerKilogram
 }
 Shipments {
     uniqueidentifier Id PK
     uniqueidentifier TransactionId FK
     uniqueidentifier ZoneId FK
     int ServiceLevel
     decimal Weight
 }
 IdentityVerifications {
     uniqueidentifier Id PK
     varchar MaskedNumber
     varchar Fingerprint
     bit IsVerified
 }
 MoneyOrders {
     uniqueidentifier Id PK
     uniqueidentifier BranchId FK
     uniqueidentifier StaffId FK
     uniqueidentifier IdentityVerificationId FK
     decimal OrderAmount
 }
 CounterTransactions {
     uniqueidentifier Id PK
     uniqueidentifier BranchId FK
     uniqueidentifier CounterId FK
     uniqueidentifier StaffId FK
     datetime2 OccurredAt
     varchar Status
     rowversion RowVersion
 }
 TransactionLines {
     uniqueidentifier Id PK
     uniqueidentifier TransactionId FK
     uniqueidentifier ProductId FK
     varchar Type
     int Quantity
     decimal UnitPriceAmount
 }
 Payments {
     uniqueidentifier Id PK
     uniqueidentifier TransactionId FK
     decimal PaidAmount
     varchar PaidCurrency
     datetime2 PaidAtUtc
 }
 AuditEntries {
     uniqueidentifier Id PK
     nvarchar StaffId
     nvarchar BranchId
     int StatusCode
     datetime2 OccurredAtUtc
 }
 OperationReceipts {
     uniqueidentifier Id PK
     datetime2 CreatedAtUtc
     nvarchar CreatedBy
     datetime2 ModifiedAtUtc
 }
 Regions ||--o{ Branches : contains
 Branches ||--o{ Counters : has
 Branches ||--o{ Staff : employs
 Roles ||--o{ Staff : assigns
 ProductCategories ||--o{ Products : groups
 Branches ||--o{ BranchStock : holds
 Products ||--o{ BranchStock : stocked
 Branches ||--o{ StockAdjustments : records
 Products ||--o{ StockAdjustments : adjusted
 Branches ||--o{ CounterTransactions : records
 Counters ||--o{ CounterTransactions : handles
 Staff ||--o{ CounterTransactions : processes
 CounterTransactions ||--o{ TransactionLines : contains
 Products o|--o{ TransactionLines : references
 CounterTransactions ||--o| Payments : paid
 CounterTransactions ||--o{ Shipments : ships
 ShippingZones ||--o{ Shipments : destination
 ShippingZones ||--o{ ShippingRates : prices
 Branches ||--o{ MoneyOrders : issues
 Staff ||--o{ MoneyOrders : processes
 IdentityVerifications ||--o{ MoneyOrders : supports
```

AuditEntries is deliberately independent of business foreign keys: staff/branch identifiers are snapshots so failed/unknown requests can be audited without invalid FK dependencies. A separate context commits sanitized audit data after final response status is known. Audit failures are logged and do not reverse a committed sale. This is best-effort audit, not guaranteed durable delivery.

## Normalization and constraints

1. Branch/region data is separate from transactions: changing a branch address/region does not rewrite every sale (update anomaly).
2. Product category/role lookups are separate: a new category/role exists without inserting a fake product/staff row (insertion anomaly).
3. Transaction header, lines and payments are separate: deleting a draft line cannot remove the branch/staff identity of its transaction (deletion anomaly).

PKs identify rows. Branch code, product SKU, staff email, branch+counter number and branch+product stock are unique. Transaction counter+branch uses a composite FK to prevent mismatched counters. Historical references use NO ACTION; transaction lines cascade with their parent aggregate. Product deletion is additionally guarded against stock/history in the command service. CHECK constraints enforce quantities, weights, amounts, line type/product consistency, statuses and masked identity shape. DEFAULT constraints on all principal tables populate UTC creation time and actor, with nullable modification time. SQL Server rowversion protects stock and transactions. Money columns are decimal(18,2); weights decimal(18,3); UTC timestamps datetime2; codes varchar and names nvarchar.

## Command and query boundaries

Repositories stage changes only, return no IQueryable, and share one scoped ApexPostDbContext with scoped EfUnitOfWork. A command commits once. Checkout validates first, then stages stock, transaction changes and one Payment; one SaveChanges transaction rolls back all of them on failure. Parent transaction modification is explicitly touched when a line changes so its rowversion also protects line-vs-payment races. No application-wide payment semaphore remains.

Views read DTOs from AsNoTracking projections and SQL aggregates. Reports execute four queries regardless of branch count; a command-count integration test enforces this. Query services compose/await sequentially on the scoped context. The transaction aggregate has one collection, so Include on its tracked command load does not create multiple-collection cartesian explosion; no unnecessary AsSplitQuery is added. There is **one database**, not separate query/command databases.

IdentityVerification has a separate persistence record and explicit mapper. Its masked identifier/fingerprint can be restored without reconstructing a raw number. Other core aggregates map directly using private constructors/backing fields, complex Money and owned types. Scalar converters handle Sku/BranchCode/Weight; the PHP-only tariff Money converter rejects other currencies rather than silently discarding them.

## Preserved business decisions

- Branch/staff/counter ownership, inactive staff, opening hours, positive quantities and manager correction rules remain domain behavior.
- High-value money orders (>=10000 in platform currency) remain disabled without an independent verifier. Fingerprint recording/confirmation is not proof of independent verification.
- Void/refund eligibility is unchanged. A correction does not automatically restock merchandise: a separate reasoned stock adjustment records returned inventory. This avoids assuming every financial correction returns goods.
- Historical seed data is generated from valid relations; corrupt old JSON is not silently imported.
- Optional raw identity inputs exist only while binding/creating a masked domain object. They are never persisted/logged and are cleared before form redisplay.

## SOLID mapping

| Principle | Implementation |
|---|---|
| SRP | Domain validates; application coordinates use cases; EF maps/stages; query services project; MVC binds/renders. |
| OCP | Application contracts permit a different data adapter without moving business rules into Web. |
| LSP | EF and fake repositories preserve contract semantics; SQL tests verify staged writes and real state round trips. |
| ISP | Specific stock, payment, shipment, audit and query contracts avoid an IQueryable-based universal repository. |
| DIP | Domain/Application do not reference EF or SqlClient; Infrastructure translates persistence conflicts to application exceptions. |

## Security and operations

Cookie authentication and branch/role claims remain; server-authorized route IDs and command lookups choose targets. Request DTOs cannot bind status/audit identity. Framework errors stay 500; domain errors 400, missing resources 404, unique/concurrency conflicts 409, with Problem Details. Local Bootstrap/scripts comply with restrictive CSP. EF command logging uses normal Microsoft.EntityFrameworkCore.Database.Command logging without sensitive data enabled. Configure its Development level in appsettings.Development.json.

Transient SQL retries are enabled. Replayable SQL transactions use atomic OperationReceipts and commit verification; the wrapper rejects tracked SaveChanges and external effects are forbidden. The seeder stages once and verifies a stable branch identity. Business concurrency conflicts still return 409 without blind replay. Payment identity is unique per transaction. See ADR 0015.

Offline/outbox topics are listed in the assignment taxonomy but have no Parts A–H acceptance criteria. No second/offline database or listener subsystem is added. Trainer clarification is needed before expanding that scope.

See docs/adr/0007–0014 and PERFORMANCE.md for evidence-linked decisions.
## Foreign keys and delete behaviour

| Parent → Child | Delete behaviour | Reason |
|---|---|---|
| Regions → Branches | NO ACTION | A region cannot be deleted while branches reference it. |
| Branches → Counters | NO ACTION | Prevent deleting a branch that still has counters. |
| Branches → Staff | NO ACTION | Preserve staff assignments and prevent orphaned records. |
| Roles → Staff | NO ACTION | A role cannot be deleted while staff use it. |
| ProductCategories → Products | NO ACTION | Preserve category references for existing products. |
| Branches → BranchStock | NO ACTION | Prevent deleting a branch that holds inventory records. |
| Products → BranchStock | NO ACTION | Prevent deleting a product while stock records reference it. |
| Branches → StockAdjustments | NO ACTION | Preserve the branch reference in stock adjustment history. |
| Products → StockAdjustments | NO ACTION | Preserve the product reference in stock adjustment history. |
| Branches → CounterTransactions | NO ACTION | Preserve historical sales for the branch. |
| Counters → CounterTransactions | NO ACTION | Preserve the counter reference in sales history. |
| Staff → CounterTransactions | NO ACTION | Preserve the staff reference in sales history. |
| CounterTransactions → TransactionLines | CASCADE | Lines belong to their parent transaction and are removed if an allowed parent deletion occurs. |
| Products → TransactionLines | NO ACTION | Prevent deleting products referenced by historical transaction lines. |
| CounterTransactions → Payments | NO ACTION | Protect payment history from accidental deletion. |
| CounterTransactions → Shipments | NO ACTION | Preserve the transaction reference for shipments. |
| ShippingZones → Shipments | NO ACTION | A shipping zone cannot be deleted while shipments reference it. |
| ShippingZones → ShippingRates | NO ACTION | Prevent deleting a zone that still has configured rates. |
| Branches → MoneyOrders | NO ACTION | Preserve the issuing branch in money-order history. |
| Staff → MoneyOrders | NO ACTION | Preserve the processing staff reference in money-order history. |
| IdentityVerifications → MoneyOrders | NO ACTION | Preserve the identity-verification record referenced by a money order. |

The counter relationship uses a composite foreign key:
CounterTransactions(CounterId, BranchId) references Counters(Id, BranchId).
This prevents a transaction from referencing a counter belonging to another branch.

AuditEntries intentionally has no foreign keys to business tables.
Staff and branch identifiers are stored as snapshots so failed requests and
requests containing unknown identifiers can still be audited.

Owned Address, OpeningHours and staff IdentityDocument values share their
owner's table. They do not create separate parent-child table relationships.

Database cascade configuration does not give users permission to delete
historical transactions. Application rules and database permissions still
control which deletions are allowed.

## Post-assessment persistence refinements

`CounterTransactions.Status` and `TransactionLines.Type` now use varchar(24) enum names,
not ordinal values. Domain enums remain unchanged. `ParcelShipping` is included in the
allowed line codes. See ADR 0015 for upgrade and rollback considerations.

`OperationReceipts` is an additional infrastructure table, bringing the table count to 19.
It has a client-generated GUID primary key and shadow CreatedAtUtc, CreatedBy and
ModifiedAtUtc fields, with no foreign keys or cascade deletes. It is atomically inserted
with replayable SQL writes to verify uncertain commits; runtime has SELECT/INSERT only.

Products DELETE supports guarded catalogue removal; BranchStock DELETE supports removing
empty stock rows. CatalogCommands blocks historical/referenced products and non-empty stock;
NO ACTION foreign keys remain a final integrity boundary. These permissions do not authorize
history, payment or audit deletion.

The canonical ADR location is `docs/adr`. Its `week2-archive` subfolder preserves superseded
JSON-era decisions for history; those files do not describe current SQL Server persistence.
