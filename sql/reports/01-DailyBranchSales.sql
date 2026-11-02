-- Paid sales by UTC day and currency; never sum different currencies together.
DECLARE @From datetime2 = DATEADD(day, -7, SYSUTCDATETIME());
DECLARE @To datetime2 = SYSUTCDATETIME();

SELECT b.Code, CAST(t.OccurredAt AS date) AS SalesDay, l.UnitPriceCurrency,
    SUM(CASE WHEN l.Type = 'RetailProduct' THEN l.Quantity * l.UnitPriceAmount ELSE 0 END) AS Retail,
    SUM(CASE WHEN l.Type <> 'RetailProduct' THEN l.Quantity * l.UnitPriceAmount ELSE 0 END) AS Services
FROM dbo.Branches AS b
INNER JOIN dbo.CounterTransactions AS t ON t.BranchId = b.Id
INNER JOIN dbo.TransactionLines AS l ON l.TransactionId = t.Id
WHERE t.Status = 'Paid' AND t.OccurredAt >= @From AND t.OccurredAt < @To
GROUP BY b.Code, CAST(t.OccurredAt AS date), l.UnitPriceCurrency
HAVING SUM(l.Quantity * l.UnitPriceAmount) > 0;
