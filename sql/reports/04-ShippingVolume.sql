SELECT z.Code,s.ServiceLevel,s.ChargeCurrency,COUNT_BIG(*) Shipments,SUM(s.ChargeAmount) Revenue
FROM dbo.Shipments s INNER JOIN dbo.ShippingZones z ON z.Id=s.ZoneId
INNER JOIN dbo.CounterTransactions t ON t.Id=s.TransactionId AND t.Status=1
GROUP BY z.Code,s.ServiceLevel,s.ChargeCurrency;
