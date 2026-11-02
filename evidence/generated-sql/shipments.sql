DECLARE @zone uniqueIdentifier = '0e44ea65-fffe-4ebf-97f1-ee6b833f0995';

SELECT [s].[TransactionId], [s].[Weight]
FROM [Shipments] AS [s]
WHERE [s].[ZoneId] = @zone AND [s].[ServiceLevel] = 0