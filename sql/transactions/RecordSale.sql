-- Run against your private, migrated and seeded ApexPostDb.
-- Safe demonstration defaults to ROLLBACK. Set @Commit = 1 to retain the sale.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'ApexPostDb'
    THROW 50019, 'Select your private ApexPostDb before running this demonstration.', 1;
IF @@TRANCOUNT <> 0
    THROW 50018, 'Run this demonstration outside an existing transaction.', 1;

DECLARE @Commit bit = 0;
DECLARE @Quantity int = 1;
DECLARE @BranchId uniqueidentifier, @CounterId uniqueidentifier,
        @StaffId uniqueidentifier, @ProductId uniqueidentifier;
DECLARE @SaleId uniqueidentifier = NEWID(), @Price decimal(18,2),
        @Currency varchar(3), @Sku varchar(40);

-- Resolve fictional seed records automatically; deterministic ordering aids repeatability.
SELECT TOP (1)
    @BranchId = b.Id, @CounterId = c.Id, @StaffId = s.Id, @ProductId = p.Id
FROM dbo.Branches AS b
INNER JOIN dbo.Counters AS c ON c.BranchId = b.Id
INNER JOIN dbo.Staff AS s ON s.BranchId = b.Id AND s.IsActive = 1
INNER JOIN dbo.BranchStock AS bs ON bs.BranchId = b.Id
INNER JOIN dbo.Products AS p ON p.Id = bs.ProductId
WHERE b.IsActive = 1 AND bs.Quantity >= @Quantity AND p.UnitPriceAmount > 0
ORDER BY b.Code, c.Number, s.EmployeeNumber, p.Sku;
IF @BranchId IS NULL
    THROW 50020, 'No eligible seeded branch, counter, staff and stock found. Run --seed first.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE Id = @BranchId AND IsActive = 1)
        THROW 50021, 'Branch is closed.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Counters WHERE Id = @CounterId AND BranchId = @BranchId)
        THROW 50021, 'Counter does not belong to branch.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Staff WHERE Id = @StaffId AND BranchId = @BranchId AND IsActive = 1)
        THROW 50022, 'Staff is not authorized.', 1;

    SELECT @Price = UnitPriceAmount, @Currency = UnitPriceCurrency, @Sku = Sku
    FROM dbo.Products WHERE Id = @ProductId;
    IF @Quantity <= 0 OR @Price IS NULL OR @Price <= 0
        THROW 50023, 'Valid quantity and price required.', 1;

    -- Check inside the UPDATE: competing clerks cannot decrement below zero.
    -- Access order: stock, header, lines, payment; keep this transaction short.
    UPDATE dbo.BranchStock
    SET Quantity = Quantity - @Quantity, ModifiedAtUtc = SYSUTCDATETIME()
    WHERE BranchId = @BranchId AND ProductId = @ProductId AND Quantity >= @Quantity;
    IF @@ROWCOUNT <> 1
        THROW 50024, 'Insufficient stock.', 1;

    INSERT dbo.CounterTransactions (Id, BranchId, CounterId, StaffId, OccurredAt, Status)
    VALUES (@SaleId, @BranchId, @CounterId, @StaffId, SYSUTCDATETIME(), 'Paid');
    INSERT dbo.TransactionLines (Id, TransactionId, Type, ProductId, Code, Quantity, UnitPriceAmount, UnitPriceCurrency)
    VALUES (NEWID(), @SaleId, 'RetailProduct', @ProductId, @Sku, @Quantity, @Price, @Currency);
    INSERT dbo.Payments (Id, TransactionId, PaidAmount, PaidCurrency, PaidAtUtc)
    VALUES (@SaleId, @SaleId, @Quantity * @Price, @Currency, SYSUTCDATETIME());

    IF @Commit = 1
        COMMIT TRANSACTION;
    ELSE
        ROLLBACK TRANSACTION;
    SELECT @SaleId AS DemonstrationSaleId, @Commit AS Persisted;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
