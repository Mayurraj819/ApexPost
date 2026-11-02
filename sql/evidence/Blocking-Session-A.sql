-- Session A, disposable performance database only. Run blocks separately to control the observation.
SELECT name,is_read_committed_snapshot_on FROM sys.databases WHERE database_id=DB_ID();
BEGIN TRAN;
UPDATE dbo.BranchStock SET Quantity=Quantity WHERE Id=(SELECT TOP(1)Id FROM dbo.BranchStock ORDER BY Id);
-- While transaction is open run Session B. Then execute COMMIT below.
-- COMMIT TRAN;
