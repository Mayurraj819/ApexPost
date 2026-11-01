DECLARE @branch uniqueIdentifier = '4d1e86ae-8b8b-4f1d-9d6f-4d4d7817d0d6';

SELECT [b].[ProductId], [b].[Quantity]
FROM [BranchStock] AS [b]
WHERE [b].[BranchId] = @branch AND [b].[Quantity] <= 3