-- Run with Include Actual Execution Plan enabled (Ctrl+M in SSMS). Save actual output, never estimated-only claims.
SET STATISTICS IO ON;SET STATISTICS TIME ON;
DECLARE @BranchId uniqueidentifier=(SELECT TOP(1) Id FROM dbo.Branches ORDER BY Code);
DECLARE @From datetime2='2026-01-01',@To datetime2='2027-01-01';
SELECT ProductId,Quantity FROM dbo.BranchStock WHERE BranchId=@BranchId AND Quantity<=3;
SELECT t.Id,t.OccurredAt,t.Status FROM dbo.CounterTransactions t WHERE t.BranchId=@BranchId AND t.OccurredAt>=@From AND t.OccurredAt<@To;
DECLARE @ZoneId uniqueidentifier=(SELECT TOP(1)Id FROM dbo.ShippingZones ORDER BY Code);
SELECT TransactionId,Weight FROM dbo.Shipments WHERE ZoneId=@ZoneId AND ServiceLevel=0;
SELECT Id,OrderAmount,IdentityVerificationId FROM dbo.MoneyOrders WHERE BranchId=@BranchId AND OccurredAtUtc>=@From AND OccurredAtUtc<@To;
-- Non-SARGable baseline and date-range candidate:
SELECT Id FROM dbo.CounterTransactions WHERE YEAR(OccurredAt)=2026;
SELECT Id FROM dbo.CounterTransactions WHERE OccurredAt>=@From AND OccurredAt<@To;
SET STATISTICS IO OFF;SET STATISTICS TIME OFF;
