WITH Revenue AS (
 SELECT r.Code Region,p.Id ProductId,p.Name,l.UnitPriceCurrency Currency,SUM(l.Quantity*l.UnitPriceAmount) Revenue
 FROM dbo.Regions r INNER JOIN dbo.Branches b ON b.RegionId=r.Id
 INNER JOIN dbo.CounterTransactions t ON t.BranchId=b.Id AND t.Status=1
 INNER JOIN dbo.TransactionLines l ON l.TransactionId=t.Id AND l.Type=0
 INNER JOIN dbo.Products p ON p.Id=l.ProductId
 GROUP BY r.Code,p.Id,p.Name,l.UnitPriceCurrency HAVING SUM(l.Quantity*l.UnitPriceAmount)>0
),Ranked AS (SELECT *,ROW_NUMBER() OVER(PARTITION BY Region,Currency ORDER BY Revenue DESC,ProductId) Position FROM Revenue)
SELECT * FROM Ranked WHERE Position<=10 ORDER BY Region,Currency,Position;
