-- Demonstration: run only against your private training database; fill valid IDs first.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @BranchId uniqueidentifier=NULL,@CounterId uniqueidentifier=NULL,@StaffId uniqueidentifier=NULL,@ProductId uniqueidentifier=NULL;
DECLARE @Quantity int=1,@SaleId uniqueidentifier=NEWID(),@Price decimal(18,2),@Currency varchar(3);
IF @BranchId IS NULL OR @CounterId IS NULL OR @StaffId IS NULL OR @ProductId IS NULL THROW 50020,'Supply valid fictional IDs before executing.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM dbo.Counters WHERE Id=@CounterId AND BranchId=@BranchId) THROW 50021,'Counter does not belong to branch.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Staff WHERE Id=@StaffId AND BranchId=@BranchId AND IsActive=1) THROW 50022,'Staff is not authorized.',1;
 SELECT @Price=UnitPriceAmount,@Currency=UnitPriceCurrency FROM dbo.Products WHERE Id=@ProductId;
 IF @Quantity<=0 OR @Price IS NULL OR @Price<=0 THROW 50023,'Valid quantity and price required.',1;
 UPDATE dbo.BranchStock SET Quantity=Quantity-@Quantity,ModifiedAtUtc=SYSUTCDATETIME()
 WHERE BranchId=@BranchId AND ProductId=@ProductId AND Quantity>=@Quantity;
 IF @@ROWCOUNT<>1 THROW 50024,'Insufficient stock.',1;
 INSERT dbo.CounterTransactions(Id,BranchId,CounterId,StaffId,OccurredAt,Status) VALUES(@SaleId,@BranchId,@CounterId,@StaffId,SYSUTCDATETIME(),1);
 INSERT dbo.TransactionLines(Id,TransactionId,Type,ProductId,Code,Quantity,UnitPriceAmount,UnitPriceCurrency) VALUES(NEWID(),@SaleId,0,@ProductId,'DEMO',@Quantity,@Price,@Currency);
 INSERT dbo.Payments(Id,TransactionId,PaidAmount,PaidCurrency,PaidAtUtc) VALUES(@SaleId,@SaleId,@Quantity*@Price,@Currency,SYSUTCDATETIME());
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
