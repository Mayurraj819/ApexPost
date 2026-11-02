-- Rank retail revenue per region/currency; ProductId breaks equal-revenue ties.
WITH Revenue AS
(
    SELECT r.Code AS Region, p.Id AS ProductId, p.Name,
        l.UnitPriceCurrency AS Currency, SUM(l.Quantity * l.UnitPriceAmount) AS Revenue
    FROM dbo.Regions AS r
    INNER JOIN dbo.Branches AS b ON b.RegionId = r.Id
    INNER JOIN dbo.CounterTransactions AS t ON t.BranchId = b.Id AND t.Status = 'Paid'
    INNER JOIN dbo.TransactionLines AS l ON l.TransactionId = t.Id AND l.Type = 'RetailProduct'
    INNER JOIN dbo.Products AS p ON p.Id = l.ProductId
    GROUP BY r.Code, p.Id, p.Name, l.UnitPriceCurrency
    HAVING SUM(l.Quantity * l.UnitPriceAmount) > 0
), Ranked AS
(
    SELECT *, ROW_NUMBER() OVER
        (PARTITION BY Region, Currency ORDER BY Revenue DESC, ProductId) AS Position
    FROM Revenue
)
SELECT Region, ProductId, Name, Currency, Revenue, Position
FROM Ranked
WHERE Position <= 10
ORDER BY Region, Currency, Position;
