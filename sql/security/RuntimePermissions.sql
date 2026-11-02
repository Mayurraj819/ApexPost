-- Reviewed for the Week 3 runtime use cases; apply with a separate deployment identity.
-- Map the application's login to this database role separately. Never commit login credentials.
IF DB_NAME()<>N'ApexPostDb' THROW 50090,'Select your private ApexPostDb before applying runtime permissions.',1;
IF DATABASE_PRINCIPAL_ID(N'ApexPostRuntime') IS NULL CREATE ROLE ApexPostRuntime;
GRANT SELECT ON dbo.Regions TO ApexPostRuntime;
GRANT SELECT ON dbo.Roles TO ApexPostRuntime;
GRANT SELECT ON dbo.ProductCategories TO ApexPostRuntime;
GRANT SELECT ON dbo.ShippingZones TO ApexPostRuntime;
GRANT SELECT ON dbo.ShippingRates TO ApexPostRuntime;
GRANT SELECT,INSERT,UPDATE ON dbo.Branches TO ApexPostRuntime;
GRANT SELECT,INSERT,UPDATE ON dbo.Counters TO ApexPostRuntime;
GRANT SELECT,INSERT,UPDATE ON dbo.Staff TO ApexPostRuntime;
GRANT SELECT,INSERT,UPDATE ON dbo.CounterTransactions TO ApexPostRuntime;
-- Catalogue deletion is allowed only for products without sales/adjustment history or positive stock.
-- CatalogCommands enforces this rule; NO ACTION foreign keys preserve referenced history.
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.Products TO ApexPostRuntime;
-- DELETE supports removing zero-quantity stock rows during catalogue cleanup.
-- The application rejects non-empty stock removal; historical rows remain protected by FKs.
GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.BranchStock TO ApexPostRuntime;
GRANT SELECT,INSERT ON dbo.StockAdjustments TO ApexPostRuntime;
GRANT SELECT,INSERT ON dbo.TransactionLines TO ApexPostRuntime;
GRANT SELECT,INSERT ON dbo.Payments TO ApexPostRuntime;
GRANT SELECT,INSERT ON dbo.Shipments TO ApexPostRuntime;
GRANT SELECT,INSERT ON dbo.MoneyOrders TO ApexPostRuntime;
GRANT SELECT,INSERT ON dbo.IdentityVerifications TO ApexPostRuntime;
GRANT SELECT,INSERT ON dbo.AuditEntries TO ApexPostRuntime;
GRANT EXECUTE ON dbo.usp_DailyBranchSales TO ApexPostRuntime;
GRANT EXECUTE ON dbo.usp_LowStockReport TO ApexPostRuntime;
GRANT EXECUTE ON dbo.usp_AdjustStock TO ApexPostRuntime;
-- Receipts verify uncertain commits. Runtime may append/read, never erase the evidence.
GRANT SELECT,INSERT ON dbo.OperationReceipts TO ApexPostRuntime;
-- No DDL, db_owner, migrations-history mutation, audit UPDATE/DELETE, or payment deletion.
