using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPost.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReadableCodesAndRetryReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IX_Transactions_Branch_Date ON dbo.CounterTransactions;");
            migrationBuilder.DropCheckConstraint(
                name: "CK_Line_Product",
                table: "TransactionLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Line_Type",
                table: "TransactionLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transaction_Status",
                table: "CounterTransactions");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "TransactionLines",
                type: "varchar(24)",
                unicode: false,
                maxLength: 24,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "CounterTransactions",
                type: "varchar(24)",
                unicode: false,
                maxLength: 24,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            // ALTER int -> varchar first produces ordinal strings; map every value before adding checks.
            migrationBuilder.Sql("UPDATE dbo.CounterTransactions SET Status = CASE Status WHEN '0' THEN 'Open' WHEN '1' THEN 'Paid' WHEN '2' THEN 'Voided' WHEN '3' THEN 'Refunded' ELSE Status END;");
            migrationBuilder.Sql("UPDATE dbo.TransactionLines SET Type = CASE Type WHEN '0' THEN 'RetailProduct' WHEN '1' THEN 'PostalService' WHEN '2' THEN 'FinancialService' WHEN '3' THEN 'ParcelShipping' ELSE Type END;");
            migrationBuilder.Sql("CREATE INDEX IX_Transactions_Branch_Date ON dbo.CounterTransactions(BranchId,OccurredAt) INCLUDE(Status);");

            migrationBuilder.CreateTable(
                name: "OperationReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationReceipts", x => x.Id);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Line_Product",
                table: "TransactionLines",
                sql: "[Type] <> 'RetailProduct' OR [ProductId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Line_Type",
                table: "TransactionLines",
                sql: "[Type] IN ('RetailProduct','PostalService','FinancialService','ParcelShipping')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transaction_Status",
                table: "CounterTransactions",
                sql: "[Status] IN ('Open','Paid','Voided','Refunded')");
            migrationBuilder.Sql("EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_DailyBranchSales\n    @BranchCode varchar(40),\n    @Date date\nAS\nBEGIN\n    SET NOCOUNT ON;\n    IF @Date IS NULL OR NULLIF(LTRIM(RTRIM(@BranchCode)), '''') IS NULL\n        THROW 50001, ''Branch code and date are required.'', 1;\n\n    DECLARE @BranchId uniqueidentifier = (SELECT Id FROM dbo.Branches WHERE Code = @BranchCode);\n    IF @BranchId IS NULL\n        THROW 50002, ''Branch was not found.'', 1;\n\n    -- Half-open UTC range supports the branch/date index; currency stays in the grouping.\n    SELECT t.BranchId, CAST(t.OccurredAt AS date) AS [Day],\n        SUM(CASE WHEN l.Type = ''RetailProduct'' THEN l.Quantity * l.UnitPriceAmount ELSE 0 END) AS Retail,\n        SUM(CASE WHEN l.Type <> ''RetailProduct'' THEN l.Quantity * l.UnitPriceAmount ELSE 0 END) AS Services,\n        l.UnitPriceCurrency AS Currency\n    FROM dbo.CounterTransactions AS t\n    INNER JOIN dbo.TransactionLines AS l ON l.TransactionId = t.Id\n    WHERE t.BranchId = @BranchId AND t.Status = ''Paid''\n        AND t.OccurredAt >= @Date AND t.OccurredAt < DATEADD(day, 1, @Date)\n    GROUP BY t.BranchId, CAST(t.OccurredAt AS date), l.UnitPriceCurrency;\nEND;\n');");
            migrationBuilder.Sql("EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_AdjustStock\n    @BranchCode varchar(40),\n    @Sku varchar(40),\n    @Delta int,\n    @Reason nvarchar(500),\n    @ChangedBy nvarchar(100)\nAS\nBEGIN\n    SET NOCOUNT ON;\n    SET XACT_ABORT ON;\n    IF @Delta IS NULL OR @Delta = 0\n        THROW 50004, ''Stock adjustment must be nonzero.'', 1;\n    IF NULLIF(LTRIM(RTRIM(@Reason)), '''') IS NULL OR NULLIF(LTRIM(RTRIM(@ChangedBy)), '''') IS NULL\n        THROW 50005, ''Reason and actor are required.'', 1;\n\n    DECLARE @BranchId uniqueidentifier = (SELECT Id FROM dbo.Branches WHERE Code = @BranchCode);\n    DECLARE @ProductId uniqueidentifier = (SELECT Id FROM dbo.Products WHERE Sku = @Sku);\n    IF @BranchId IS NULL OR @ProductId IS NULL\n        THROW 50006, ''Branch or SKU was not found.'', 1;\n\n    -- Own a standalone transaction; preserve an existing caller''s transaction with a savepoint.\n    DECLARE @OwnTransaction bit = CASE WHEN @@TRANCOUNT = 0 THEN 1 ELSE 0 END;\n    BEGIN TRY\n        IF @OwnTransaction = 1\n            BEGIN TRANSACTION;\n        ELSE\n            SAVE TRANSACTION AdjustStockSavepoint;\n\n        -- Check bounds atomically in the UPDATE, using bigint to avoid intermediate overflow.\n        UPDATE dbo.BranchStock\n        SET Quantity = Quantity + @Delta, ModifiedAtUtc = SYSUTCDATETIME()\n        WHERE BranchId = @BranchId AND ProductId = @ProductId\n            AND CAST(Quantity AS bigint) + @Delta BETWEEN 0 AND 1000000;\n        IF @@ROWCOUNT <> 1\n            THROW 50007, ''Stock is missing or adjustment exceeds permitted quantity.'', 1;\n\n        INSERT dbo.StockAdjustments (Id, BranchId, ProductId, Delta, Reason, ChangedBy, CreatedAtUtc, CreatedBy)\n        VALUES (NEWID(), @BranchId, @ProductId, @Delta, @Reason, @ChangedBy, SYSUTCDATETIME(), @ChangedBy);\n        IF @OwnTransaction = 1\n            COMMIT TRANSACTION;\n    END TRY\n    BEGIN CATCH\n        IF @OwnTransaction = 1 AND XACT_STATE() <> 0\n            ROLLBACK TRANSACTION;\n        ELSE IF @OwnTransaction = 0 AND XACT_STATE() = 1\n            ROLLBACK TRANSACTION AdjustStockSavepoint;\n        THROW;\n    END CATCH;\nEND;\n');");
            migrationBuilder.Sql("EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_LowStockReport\n    @BranchCode varchar(40) = NULL,\n    @Threshold int = 3\nAS\nBEGIN\n    SET NOCOUNT ON;\n    IF @Threshold IS NULL OR @Threshold < 0\n        THROW 50003, ''Threshold must be nonnegative.'', 1;\n    IF @BranchCode IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE Code = @BranchCode)\n        THROW 50002, ''Branch was not found.'', 1;\n\n    -- A branch-specific call and an all-branch call have different selectivity.\n    -- RECOMPILE specializes these predicates at the cost of compilation per call.\n    -- Alternatives: cached optional predicates, separate query variants, or PSP where eligible.\n    -- Re-measure before changing this policy; ADR 0009 records the tradeoff.\n    SELECT s.BranchId, s.ProductId, p.Name AS Product, s.Quantity\n    FROM dbo.BranchStock AS s\n    INNER JOIN dbo.Branches AS b ON b.Id = s.BranchId\n    INNER JOIN dbo.Products AS p ON p.Id = s.ProductId\n    WHERE (@BranchCode IS NULL OR b.Code = @BranchCode) AND s.Quantity <= @Threshold\n    ORDER BY s.Quantity, s.BranchId, s.ProductId\n    OPTION (RECOMPILE);\nEND;\n');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The old schema cannot represent ParcelShipping. Refuse a destructive rollback.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM dbo.TransactionLines WHERE Type = 'ParcelShipping') THROW 50091, 'Cannot downgrade while ParcelShipping lines exist.', 1;");
            migrationBuilder.Sql("DROP INDEX IX_Transactions_Branch_Date ON dbo.CounterTransactions;");
            migrationBuilder.DropTable(
                name: "OperationReceipts");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Line_Product",
                table: "TransactionLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Line_Type",
                table: "TransactionLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transaction_Status",
                table: "CounterTransactions");

            migrationBuilder.Sql("UPDATE dbo.CounterTransactions SET Status = CASE Status WHEN 'Open' THEN '0' WHEN 'Paid' THEN '1' WHEN 'Voided' THEN '2' WHEN 'Refunded' THEN '3' ELSE Status END;");
            migrationBuilder.Sql("UPDATE dbo.TransactionLines SET Type = CASE Type WHEN 'RetailProduct' THEN '0' WHEN 'PostalService' THEN '1' WHEN 'FinancialService' THEN '2' ELSE Type END;");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "TransactionLines",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldUnicode: false,
                oldMaxLength: 24);

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "CounterTransactions",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(24)",
                oldUnicode: false,
                oldMaxLength: 24);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Line_Product",
                table: "TransactionLines",
                sql: "[Type] <> 0 OR [ProductId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Line_Type",
                table: "TransactionLines",
                sql: "[Type] IN (0,1,2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transaction_Status",
                table: "CounterTransactions",
                sql: "[Status] IN (0,1,2,3)");
            migrationBuilder.Sql("CREATE INDEX IX_Transactions_Branch_Date ON dbo.CounterTransactions(BranchId,OccurredAt) INCLUDE(Status);");
            migrationBuilder.Sql("EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_DailyBranchSales @BranchCode varchar(40), @Date date\nAS\nBEGIN\n SET NOCOUNT ON;\n IF @Date IS NULL OR NULLIF(LTRIM(RTRIM(@BranchCode)), '''') IS NULL THROW 50001, ''Branch code and date are required.'', 1;\n DECLARE @BranchId uniqueidentifier = (SELECT Id FROM dbo.Branches WHERE Code = @BranchCode);\n IF @BranchId IS NULL THROW 50002, ''Branch was not found.'', 1;\n SELECT t.BranchId, CAST(t.OccurredAt AS date) AS [Day],\n SUM(CASE WHEN l.Type = 0 THEN l.Quantity*l.UnitPriceAmount ELSE 0 END) AS Retail,\n SUM(CASE WHEN l.Type <> 0 THEN l.Quantity*l.UnitPriceAmount ELSE 0 END) AS Services,\n l.UnitPriceCurrency AS Currency\n FROM dbo.CounterTransactions t INNER JOIN dbo.TransactionLines l ON l.TransactionId=t.Id\n WHERE t.BranchId=@BranchId AND t.Status=1 AND t.OccurredAt>=@Date AND t.OccurredAt<DATEADD(day,1,@Date)\n GROUP BY t.BranchId, CAST(t.OccurredAt AS date), l.UnitPriceCurrency;\nEND;\n');");
            migrationBuilder.Sql("EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_LowStockReport @BranchCode varchar(40)=NULL, @Threshold int=3\nAS\nBEGIN\n SET NOCOUNT ON;\n IF @Threshold IS NULL OR @Threshold<0 THROW 50003, ''Threshold must be nonnegative.'', 1;\n IF @BranchCode IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.Branches WHERE Code=@BranchCode) THROW 50002,''Branch was not found.'',1;\n SELECT s.BranchId,s.ProductId,p.Name AS Product,s.Quantity\n FROM dbo.BranchStock s INNER JOIN dbo.Branches b ON b.Id=s.BranchId INNER JOIN dbo.Products p ON p.Id=s.ProductId\n WHERE (@BranchCode IS NULL OR b.Code=@BranchCode) AND s.Quantity<=@Threshold\n ORDER BY s.Quantity,s.BranchId,s.ProductId OPTION(RECOMPILE);\nEND;\n');");
            migrationBuilder.Sql("EXEC(N'CREATE OR ALTER PROCEDURE dbo.usp_AdjustStock @BranchCode varchar(40), @Sku varchar(40), @Delta int, @Reason nvarchar(500), @ChangedBy nvarchar(100)\nAS\nBEGIN\n SET NOCOUNT ON;\n SET XACT_ABORT ON;\n IF @Delta IS NULL OR @Delta=0 THROW 50004,''Stock adjustment must be nonzero.'',1;\n IF NULLIF(LTRIM(RTRIM(@Reason)),'''') IS NULL OR NULLIF(LTRIM(RTRIM(@ChangedBy)),'''') IS NULL THROW 50005,''Reason and actor are required.'',1;\n DECLARE @BranchId uniqueidentifier=(SELECT Id FROM dbo.Branches WHERE Code=@BranchCode);\n DECLARE @ProductId uniqueidentifier=(SELECT Id FROM dbo.Products WHERE Sku=@Sku);\n IF @BranchId IS NULL OR @ProductId IS NULL THROW 50006,''Branch or SKU was not found.'',1;\n DECLARE @OwnTransaction bit=CASE WHEN @@TRANCOUNT=0 THEN 1 ELSE 0 END;\n BEGIN TRY\n  IF @OwnTransaction=1 BEGIN TRANSACTION; ELSE SAVE TRANSACTION AdjustStockSavepoint;\n  UPDATE dbo.BranchStock SET Quantity=Quantity+@Delta,ModifiedAtUtc=SYSUTCDATETIME()\n   WHERE BranchId=@BranchId AND ProductId=@ProductId AND CAST(Quantity AS bigint)+@Delta BETWEEN 0 AND 1000000;\n  IF @@ROWCOUNT<>1 THROW 50007,''Stock is missing or adjustment exceeds permitted quantity.'',1;\n  INSERT dbo.StockAdjustments(Id,BranchId,ProductId,Delta,Reason,ChangedBy,CreatedAtUtc,CreatedBy)\n   VALUES(NEWID(),@BranchId,@ProductId,@Delta,@Reason,@ChangedBy,SYSUTCDATETIME(),@ChangedBy);\n  IF @OwnTransaction=1 COMMIT TRANSACTION;\n END TRY\n BEGIN CATCH\n  IF @OwnTransaction=1 AND XACT_STATE()<>0 ROLLBACK TRANSACTION;\n  ELSE IF @OwnTransaction=0 AND XACT_STATE()=1 ROLLBACK TRANSACTION AdjustStockSavepoint;\n  THROW;\n END CATCH\nEND;\n');");
        }
    }
}
