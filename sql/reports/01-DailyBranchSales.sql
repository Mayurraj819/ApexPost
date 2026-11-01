DECLARE @From datetime2=DATEADD(day,-7,SYSUTCDATETIME()),@To datetime2=SYSUTCDATETIME();
SELECT b.Code,CAST(t.OccurredAt AS date) AS SalesDay,l.UnitPriceCurrency,
 SUM(CASE WHEN l.Type=0 THEN l.Quantity*l.UnitPriceAmount ELSE 0 END) Retail,
 SUM(CASE WHEN l.Type<>0 THEN l.Quantity*l.UnitPriceAmount ELSE 0 END) Services
FROM dbo.Branches b INNER JOIN dbo.CounterTransactions t ON b.Id=t.BranchId
INNER JOIN dbo.TransactionLines l ON l.TransactionId=t.Id
WHERE t.Status=1 AND t.OccurredAt>=@From AND t.OccurredAt<@To
GROUP BY b.Code,CAST(t.OccurredAt AS date),l.UnitPriceCurrency HAVING SUM(l.Quantity*l.UnitPriceAmount)>0;
