-- Disposable performance DB only. Deterministic fictional skew: 1000 branches, 30k stock rows, 100k additional paid transactions.
SET NOCOUNT ON; SET XACT_ABORT ON;
;WITH N AS (SELECT TOP(995)ROW_NUMBER()OVER(ORDER BY a.object_id,b.object_id)+5 n FROM sys.all_objects a CROSS JOIN sys.all_objects b)
INSERT dbo.Branches(Id,Code,Name,Address,IsActive,OpeningHours_OpensAt,OpeningHours_ClosesAt,RegionId)
SELECT NEWID(),CONCAT('PERF-',n),CONCAT('Performance branch ',n),'Fictional avenue',1,'00:00:00','23:59:59',(SELECT TOP(1)Id FROM dbo.Regions ORDER BY Code) FROM N;
INSERT dbo.Counters(Id,BranchId,Number,Name) SELECT NEWID(),b.Id,1,'Performance counter' FROM dbo.Branches b WHERE NOT EXISTS(SELECT 1 FROM dbo.Counters c WHERE c.BranchId=b.Id);
INSERT dbo.Staff(Id,BranchId,EmployeeNumber,FullName,Email,Role,IsActive)
SELECT NEWID(),b.Id,CONCAT('PERF-',b.Code),CONCAT('Fictional ',b.Code),CONCAT(b.Code,'@example.com'),0,1 FROM dbo.Branches b WHERE NOT EXISTS(SELECT 1 FROM dbo.Staff s WHERE s.BranchId=b.Id);
INSERT dbo.BranchStock(Id,BranchId,ProductId,Quantity)
SELECT NEWID(),b.Id,p.Id,ABS(CHECKSUM(b.Id,p.Id)%51) FROM dbo.Branches b CROSS JOIN dbo.Products p WHERE NOT EXISTS(SELECT 1 FROM dbo.BranchStock s WHERE s.BranchId=b.Id AND s.ProductId=p.Id);
SELECT TOP(100000)ROW_NUMBER()OVER(ORDER BY a.object_id,b.object_id) n INTO #N FROM sys.all_objects a CROSS JOIN sys.all_objects b;
SELECT ROW_NUMBER()OVER(ORDER BY b.Code) rn,b.Id BranchId,c.Id CounterId,s.Id StaffId INTO #Actors FROM dbo.Branches b
CROSS APPLY(SELECT TOP(1)Id FROM dbo.Counters c WHERE c.BranchId=b.Id ORDER BY c.Number)c
CROSS APPLY(SELECT TOP(1)Id FROM dbo.Staff s WHERE s.BranchId=b.Id ORDER BY s.EmployeeNumber)s;
SELECT n.n,NEWID() Id,a.BranchId,a.CounterId,a.StaffId,DATEADD(minute,-CAST(n.n%129600 AS int),SYSUTCDATETIME()) OccurredAt INTO #Sales FROM #N n INNER JOIN #Actors a ON a.rn=((n.n-1)%1000)+1;
INSERT dbo.CounterTransactions(Id,BranchId,CounterId,StaffId,OccurredAt,Status)SELECT Id,BranchId,CounterId,StaffId,OccurredAt,1 FROM #Sales;
DECLARE @ProductId uniqueidentifier=(SELECT TOP(1)Id FROM dbo.Products ORDER BY Sku),@ZoneId uniqueidentifier=(SELECT TOP(1)Id FROM dbo.ShippingZones ORDER BY Code);
INSERT dbo.TransactionLines(Id,TransactionId,Type,ProductId,Code,Quantity,UnitPriceAmount,UnitPriceCurrency)
SELECT NEWID(),Id,0,@ProductId,'PERF-RETAIL',1,20,'PHP' FROM #Sales;
INSERT dbo.TransactionLines(Id,TransactionId,Type,Code,Quantity,UnitPriceAmount,UnitPriceCurrency,ShippingLevel)
SELECT NEWID(),Id,1,'POST-STD',1,70,'PHP',0 FROM #Sales WHERE n%3=0;
INSERT dbo.Payments(Id,TransactionId,PaidAmount,PaidCurrency,PaidAtUtc) SELECT Id,Id,CASE WHEN n%3=0 THEN 90 ELSE 20 END,'PHP',OccurredAt FROM #Sales;
INSERT dbo.Shipments(Id,TransactionId,ZoneId,ServiceLevel,Weight,RecipientName,Address_Street,Address_City,Address_PostalCode,ChargeAmount,ChargeCurrency)
SELECT NEWID(),Id,(SELECT z.Id FROM (SELECT Id,ROW_NUMBER()OVER(ORDER BY Code) rn FROM dbo.ShippingZones)z WHERE z.rn=(n%10)+1),0,1000,'Demo recipient','Fictional street','Demo city','1000',70,'PHP' FROM #Sales WHERE n%3=0;
SELECT TOP(10000)NEWID() Id,BranchId,StaffId,OccurredAt INTO #Orders FROM #Sales ORDER BY n;
INSERT dbo.IdentityVerifications(Id,DocumentType,MaskedNumber,Fingerprint,IsVerified,Method) SELECT Id,'Demo','***1234',REPLICATE('A',64),0,'FingerprintRecorded' FROM #Orders;
INSERT dbo.MoneyOrders(Id,BranchId,StaffId,PayeeName,IdentityVerificationId,OccurredAtUtc,OrderAmount,OrderCurrency)
SELECT Id,BranchId,StaffId,'Fictional payee',Id,OccurredAt,100,'PHP' FROM #Orders;
DROP TABLE #N,#Actors,#Sales,#Orders;
