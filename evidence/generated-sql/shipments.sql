DECLARE @zone uniqueIdentifier = '303398eb-fe42-4103-b764-dba2b6e6d453';

SELECT [s].[TransactionId], [s].[Weight]
FROM [Shipments] AS [s]
WHERE [s].[ZoneId] = @zone AND [s].[ServiceLevel] = 0