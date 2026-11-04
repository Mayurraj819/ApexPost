-- Apply only to your private ApexPostDb using the deployment identity after migrations.
IF DB_NAME() <> N'ApexPostDb' THROW 50100, 'Select your private ApexPostDb.', 1;
IF DATABASE_PRINCIPAL_ID(N'ApexPostRuntime') IS NULL CREATE ROLE ApexPostRuntime;
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.TrackedShipments TO ApexPostRuntime;
GRANT SELECT, INSERT ON dbo.CourierScans TO ApexPostRuntime;
GRANT SELECT, INSERT ON dbo.OperationReceipts TO ApexPostRuntime;
-- Tracked shipment DELETE implements the Created-only use case. Child scans cascade.
-- No direct scan UPDATE/DELETE, DDL, migration or db_owner permissions for runtime.
