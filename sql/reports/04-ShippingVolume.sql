-- Include shipments on paid transactions; retain currency in revenue grouping.
SELECT z.Code, s.ServiceLevel, s.ChargeCurrency,
    COUNT_BIG(*) AS Shipments, SUM(s.ChargeAmount) AS Revenue
FROM dbo.Shipments AS s
INNER JOIN dbo.ShippingZones AS z ON z.Id = s.ZoneId
INNER JOIN dbo.CounterTransactions AS t ON t.Id = s.TransactionId AND t.Status = 'Paid'
GROUP BY z.Code, s.ServiceLevel, s.ChargeCurrency;
