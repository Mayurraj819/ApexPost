DECLARE @branch uniqueIdentifier = '4d1e86ae-8b8b-4f1d-9d6f-4d4d7817d0d6';
DECLARE @from datetime2 = '2026-10-03T18:37:23.0719712Z';
DECLARE @today datetime2 = '2026-11-02T18:37:23.0719712Z';

SELECT [c].[Id], [c].[OccurredAt], [c].[Status]
FROM [CounterTransactions] AS [c]
WHERE [c].[BranchId] = @branch AND [c].[OccurredAt] >= @from AND [c].[OccurredAt] < @today