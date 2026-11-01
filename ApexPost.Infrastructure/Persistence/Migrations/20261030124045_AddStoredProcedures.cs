using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace ApexPost.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc/>
    public partial class AddStoredProcedures : Migration
    {
        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_DailyBranchSales @BranchCode varchar(40), @Date date\nAS\nBEGIN\n SET NOCOUNT ON;\n IF @Date IS NULL OR NULLIF(LTRIM(RTRIM(@BranchCode)), '''') IS NULL THROW 50001, ''Branch code and date are required.'', 1;\n DECLARE @BranchId uniqueidentifier = (SELECT Id FROM dbo.Branches WHERE Code = @BranchCode);\n IF @BranchId IS NULL THROW 50002, ''Branch was not found.'', 1;\n SELECT t.BranchId, CAST(t.OccurredAt AS date) AS [Day],\n SUM(CASE WHEN l.Type = 0 THEN l.Quantity*l.UnitPriceAmount ELSE 0 END) AS Retail,\n SUM(CASE WHEN l.Type <> 0 THEN l.Quantity*l.UnitPriceAmount ELSE 0 END) AS Services,\n l.UnitPriceCurrency AS Currency\n FROM dbo.CounterTransactions t INNER JOIN dbo.TransactionLines l ON l.TransactionId=t.Id\n WHERE t.BranchId=@BranchId AND t.Status=1 AND t.OccurredAt>=@Date AND t.OccurredAt<DATEADD(day,1,@Date)\n GROUP BY t.BranchId, CAST(t.OccurredAt AS date), l.UnitPriceCurrency;\nEND;\n');");
            migrationBuilder.Sql("EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_AdjustStock @BranchCode varchar(40), @Sku varchar(40), @Delta int, @Reason nvarchar(500), @ChangedBy nvarchar(100)\nAS\nBEGIN\n SET NOCOUNT ON;\n SET XACT_ABORT ON;\n IF @Delta IS NULL OR @Delta=0 THROW 50004,''Stock adjustment must be nonzero.'',1;\n IF NULLIF(LTRIM(RTRIM(@Reason)),'''') IS NULL OR NULLIF(LTRIM(RTRIM(@ChangedBy)),'''') IS NULL THROW 50005,''Reason and actor are required.'',1;\n DECLARE @BranchId uniqueidentifier=(SELECT Id FROM dbo.Branches WHERE Code=@BranchCode);\n DECLARE @ProductId uniqueidentifier=(SELECT Id FROM dbo.Products WHERE Sku=@Sku);\n IF @BranchId IS NULL OR @ProductId IS NULL THROW 50006,''Branch or SKU was not found.'',1;\n DECLARE @OwnTransaction bit=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;\n BEGIN TRY\n  IF @OwnTransaction=1 BEGIN TRANSACTION; ELSE SAVE TRANSACTION AdjustStockSavepoint;\n  UPDATE dbo.BranchStock SET Quantity=Quantity+@Delta,ModifiedAtUtc=SYSUTCDATETIME()\n   WHERE BranchId=@BranchId AND ProductId=@ProductId AND CAST(Quantity AS bigint)+@Delta BETWEEN 0 AND 1000000;\n  IF @@ROWCOUNT<>1 THROW 50007,''Stock is missing or adjustment exceeds permitted quantity.'',1;\n  INSERT dbo.StockAdjustments(Id,BranchId,ProductId,Delta,Reason,ChangedBy,CreatedAtUtc,CreatedBy)\n   VALUES(NEWID(),@BranchId,@ProductId,@Delta,@Reason,@ChangedBy,SYSUTCDATETIME(),@ChangedBy);\n  IF @OwnTransaction=1 COMMIT TRANSACTION;\n END TRY\n BEGIN CATCH\n  IF @OwnTransaction=1 AND XACT_STATE()<>0 ROLLBACK TRANSACTION;\n  ELSE IF @OwnTransaction=0 AND XACT_STATE()=1 ROLLBACK TRANSACTION AdjustStockSavepoint;\n  THROW;\n END CATCH\nEND;\n');");
            migrationBuilder.Sql("EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_LowStockReport @BranchCode varchar(40)=NULL, @Threshold int=3\nAS\nBEGIN\n SET NOCOUNT ON;\n IF @Threshold IS NULL OR @Threshold<0 THROW 50003, ''Threshold must be nonnegative.'', 1;\n IF @BranchCode IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Branches WHERE Code=@BranchCode) THROW 50002,''Branch was not found.'',1;\n SELECT s.BranchId,s.ProductId,p.Name AS Product,s.Quantity\n FROM dbo.BranchStock s INNER JOIN dbo.Branches b ON b.Id=s.BranchId INNER JOIN dbo.Products p ON p.Id=s.ProductId\n WHERE (@BranchCode IS NULL OR b.Code=@BranchCode) AND s.Quantity<=@Threshold\n ORDER BY s.Quantity,s.BranchId,s.ProductId OPTION(RECOMPILE);\nEND;\n');");
        }

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_DailyBranchSales;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_AdjustStock;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_LowStockReport;");
        }
    }
}
