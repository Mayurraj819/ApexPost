CREATE OR ALTER PROCEDURE dbo.usp_AdjustStock
    @BranchCode varchar(40),
    @Sku varchar(40),
    @Delta int,
    @Reason nvarchar(500),
    @ChangedBy nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @Delta IS NULL OR @Delta = 0
        THROW 50004, 'Stock adjustment must be nonzero.', 1;
    IF NULLIF(LTRIM(RTRIM(@Reason)), '') IS NULL OR NULLIF(LTRIM(RTRIM(@ChangedBy)), '') IS NULL
        THROW 50005, 'Reason and actor are required.', 1;

    DECLARE @BranchId uniqueidentifier = (SELECT Id FROM dbo.Branches WHERE Code = @BranchCode);
    DECLARE @ProductId uniqueidentifier = (SELECT Id FROM dbo.Products WHERE Sku = @Sku);
    IF @BranchId IS NULL OR @ProductId IS NULL
        THROW 50006, 'Branch or SKU was not found.', 1;

    -- Own a standalone transaction; preserve an existing caller's transaction with a savepoint.
    DECLARE @OwnTransaction bit = CASE WHEN @@TRANCOUNT = 0 THEN 1 ELSE 0 END;
    BEGIN TRY
        IF @OwnTransaction = 1
            BEGIN TRANSACTION;
        ELSE
            SAVE TRANSACTION AdjustStockSavepoint;

        -- Check bounds atomically in the UPDATE, using bigint to avoid intermediate overflow.
        UPDATE dbo.BranchStock
        SET Quantity = Quantity + @Delta, ModifiedAtUtc = SYSUTCDATETIME()
        WHERE BranchId = @BranchId AND ProductId = @ProductId
            AND CAST(Quantity AS bigint) + @Delta BETWEEN 0 AND 1000000;
        IF @@ROWCOUNT <> 1
            THROW 50007, 'Stock is missing or adjustment exceeds permitted quantity.', 1;

        INSERT dbo.StockAdjustments (Id, BranchId, ProductId, Delta, Reason, ChangedBy, CreatedAtUtc, CreatedBy)
        VALUES (NEWID(), @BranchId, @ProductId, @Delta, @Reason, @ChangedBy, SYSUTCDATETIME(), @ChangedBy);
        IF @OwnTransaction = 1
            COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @OwnTransaction = 1 AND XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        ELSE IF @OwnTransaction = 0 AND XACT_STATE() = 1
            ROLLBACK TRANSACTION AdjustStockSavepoint;
        THROW;
    END CATCH;
END;
