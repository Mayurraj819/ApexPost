CREATE OR ALTER PROCEDURE dbo.usp_DailyBranchSales @BranchCode varchar(40), @Date date
AS
BEGIN
 SET NOCOUNT ON;
 IF @Date IS NULL OR NULLIF(LTRIM(RTRIM(@BranchCode)), '') IS NULL THROW 50001, 'Branch code and date are required.', 1;
 DECLARE @BranchId uniqueidentifier = (SELECT Id FROM dbo.Branches WHERE Code = @BranchCode);
 IF @BranchId IS NULL THROW 50002, 'Branch was not found.', 1;
 SELECT t.BranchId, CAST(t.OccurredAt AS date) AS [Day],
 SUM(CASE WHEN l.Type = 0 THEN l.Quantity*l.UnitPriceAmount ELSE 0 END) AS Retail,
 SUM(CASE WHEN l.Type <> 0 THEN l.Quantity*l.UnitPriceAmount ELSE 0 END) AS Services,
 l.UnitPriceCurrency AS Currency
 FROM dbo.CounterTransactions t INNER JOIN dbo.TransactionLines l ON l.TransactionId=t.Id
 WHERE t.BranchId=@BranchId AND t.Status=1 AND t.OccurredAt>=@Date AND t.OccurredAt<DATEADD(day,1,@Date)
 GROUP BY t.BranchId, CAST(t.OccurredAt AS date), l.UnitPriceCurrency;
END;
