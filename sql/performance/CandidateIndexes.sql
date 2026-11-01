-- Candidate hypotheses only. Capture baseline first. Apply ONLY to a disposable performance database.
CREATE INDEX IX_Stock_Branch_Quantity ON dbo.BranchStock(BranchId,Quantity) INCLUDE(ProductId);
CREATE INDEX IX_Transactions_Branch_Date ON dbo.CounterTransactions(BranchId,OccurredAt) INCLUDE(Status);
CREATE INDEX IX_Shipment_Zone_Level ON dbo.Shipments(ZoneId,ServiceLevel) INCLUDE(TransactionId,Weight);
CREATE INDEX IX_MoneyOrder_Branch_Date ON dbo.MoneyOrders(BranchId,OccurredAtUtc) INCLUDE(OrderAmount,IdentityVerificationId);
-- Record insert/update IO/time before/after; capture storage using sys.dm_db_partition_stats.
SELECT OBJECT_NAME(object_id) TableName,index_id,SUM(used_page_count)*8 UsedKB FROM sys.dm_db_partition_stats GROUP BY object_id,index_id;
