-- Keep status/date predicates in ON, preserving branches with no matching sales.
DECLARE @Today date = CAST(SYSUTCDATETIME() AS date);

SELECT b.Code, b.Name
FROM dbo.Branches AS b
LEFT JOIN dbo.CounterTransactions AS t ON t.BranchId = b.Id
    AND t.Status = 'Paid'
    AND t.OccurredAt >= @Today AND t.OccurredAt < DATEADD(day, 1, @Today)
WHERE t.Id IS NULL;
