using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace ApexPost.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc/>
    public partial class AddMeasuredWorkloadIndexes : Migration
    {
        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Based on the captured actual plans and write/storage trade-offs in PERFORMANCE.md.
            migrationBuilder.Sql("CREATE INDEX IX_Stock_Branch_Quantity ON dbo.BranchStock(BranchId,Quantity) INCLUDE(ProductId);");
            migrationBuilder.Sql("CREATE INDEX IX_Transactions_Branch_Date ON dbo.CounterTransactions(BranchId,OccurredAt) INCLUDE(Status);");
            migrationBuilder.Sql("CREATE INDEX IX_Shipment_Zone_Level ON dbo.Shipments(ZoneId,ServiceLevel) INCLUDE(TransactionId,Weight);");
            migrationBuilder.Sql("CREATE INDEX IX_MoneyOrder_Branch_Date ON dbo.MoneyOrders(BranchId,OccurredAtUtc) INCLUDE(OrderAmount,IdentityVerificationId);");
        }

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IX_Stock_Branch_Quantity ON dbo.BranchStock;");
            migrationBuilder.Sql("DROP INDEX IX_Transactions_Branch_Date ON dbo.CounterTransactions;");
            migrationBuilder.Sql("DROP INDEX IX_Shipment_Zone_Level ON dbo.Shipments;");
            migrationBuilder.Sql("DROP INDEX IX_MoneyOrder_Branch_Date ON dbo.MoneyOrders;");
        }
    }
}
