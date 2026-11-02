DECLARE @Today date=CAST(SYSUTCDATETIME() AS date);
SELECT b.Code,b.Name FROM dbo.Branches b
LEFT JOIN dbo.CounterTransactions t ON t.BranchId=b.Id AND t.Status=1 AND t.OccurredAt>=@Today AND t.OccurredAt<DATEADD(day,1,@Today)
WHERE t.Id IS NULL;
