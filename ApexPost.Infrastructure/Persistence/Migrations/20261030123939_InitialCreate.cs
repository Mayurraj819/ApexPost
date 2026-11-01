using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace ApexPost.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc/>
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(name: "AuditEntries", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), Controller = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), StaffId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true), Role = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true), BranchId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true), CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), StatusCode = table.Column<int>(type: "int", nullable: false), ElapsedMilliseconds = table.Column<double>(type: "float", nullable: false), SafeRoutes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false), Outcome = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false), ExceptionType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true) }, constraints: table =>
            {
                table.PrimaryKey("PK_AuditEntries", x => x.Id);
                table.CheckConstraint("CK_Audit_Status", "[StatusCode] BETWEEN 100 AND 599");
            });
            migrationBuilder.CreateTable(name: "IdentityVerifications", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), DocumentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false), MaskedNumber = table.Column<string>(type: "varchar(7)", unicode: false, maxLength: 7, nullable: false), Fingerprint = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false), IsVerified = table.Column<bool>(type: "bit", nullable: false), Method = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true) }, constraints: table =>
            {
                table.PrimaryKey("PK_IdentityVerifications", x => x.Id);
                table.CheckConstraint("CK_Identity_Masked", "LEFT([MaskedNumber],3) = '***' AND LEN([MaskedNumber]) BETWEEN 4 AND 7");
            });
            migrationBuilder.CreateTable(name: "ProductCategories", columns: table => new { Id = table.Column<int>(type: "int", nullable: false), Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true) }, constraints: table =>
            {
                table.PrimaryKey("PK_ProductCategories", x => x.Id);
            });
            migrationBuilder.CreateTable(name: "Regions", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false), Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true) }, constraints: table =>
            {
                table.PrimaryKey("PK_Regions", x => x.Id);
            });
            migrationBuilder.CreateTable(name: "Roles", columns: table => new { Id = table.Column<int>(type: "int", nullable: false), Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true) }, constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });
            migrationBuilder.CreateTable(name: "ShippingZones", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false), Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true) }, constraints: table =>
            {
                table.PrimaryKey("PK_ShippingZones", x => x.Id);
            });
            migrationBuilder.CreateTable(name: "Products", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Sku = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false), Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), Category = table.Column<int>(type: "int", nullable: false), Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), UnitPriceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false), UnitPriceCurrency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.CheckConstraint("CK_Product_Price", "[UnitPriceAmount] >= 0");
                table.CheckConstraint("CK_Product_Weight", "[Weight] IS NULL OR [Weight] >= 0");
                table.ForeignKey(name: "FK_Products_ProductCategories_Category", column: x => x.Category, principalTable: "ProductCategories", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "Branches", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false), Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false), OpeningHours_OpensAt = table.Column<TimeOnly>(type: "time", nullable: false), OpeningHours_ClosesAt = table.Column<TimeOnly>(type: "time", nullable: false), IsActive = table.Column<bool>(type: "bit", nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), RegionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Branches", x => x.Id);
                table.ForeignKey(name: "FK_Branches_Regions_RegionId", column: x => x.RegionId, principalTable: "Regions", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "ShippingRates", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), ZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), ServiceLevel = table.Column<int>(type: "int", nullable: false), BaseRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false), PerKilogram = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true) }, constraints: table =>
            {
                table.PrimaryKey("PK_ShippingRates", x => x.Id);
                table.CheckConstraint("CK_Rate_Amounts", "[BaseRate] > 0 AND [PerKilogram] >= 0");
                table.ForeignKey(name: "FK_ShippingRates_ShippingZones_ZoneId", column: x => x.ZoneId, principalTable: "ShippingZones", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "BranchStock", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false), Quantity = table.Column<int>(type: "int", nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_BranchStock", x => x.Id);
                table.CheckConstraint("CK_Stock_Quantity", "[Quantity] >= 0");
                table.ForeignKey(name: "FK_BranchStock_Branches_BranchId", column: x => x.BranchId, principalTable: "Branches", principalColumn: "Id");
                table.ForeignKey(name: "FK_BranchStock_Products_ProductId", column: x => x.ProductId, principalTable: "Products", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "Counters", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Number = table.Column<int>(type: "int", nullable: false), Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true) }, constraints: table =>
            {
                table.PrimaryKey("PK_Counters", x => x.Id);
                table.UniqueConstraint("AK_Counters_Id_BranchId", x => new { x.Id, x.BranchId });
                table.CheckConstraint("CK_Counter_Number", "[Number] > 0");
                table.ForeignKey(name: "FK_Counters_Branches_BranchId", column: x => x.BranchId, principalTable: "Branches", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "Staff", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), EmployeeNumber = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false), FullName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), Email = table.Column<string>(type: "varchar(254)", unicode: false, maxLength: 254, nullable: false), Role = table.Column<int>(type: "int", nullable: false), IdentityDocument_DocumentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true), IdentityDocument_MaskedNumber = table.Column<string>(type: "varchar(7)", unicode: false, maxLength: 7, nullable: true), IdentityDocument_Fingerprint = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true), IsActive = table.Column<bool>(type: "bit", nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true) }, constraints: table =>
            {
                table.PrimaryKey("PK_Staff", x => x.Id);
                table.ForeignKey(name: "FK_Staff_Branches_BranchId", column: x => x.BranchId, principalTable: "Branches", principalColumn: "Id");
                table.ForeignKey(name: "FK_Staff_Roles_Role", column: x => x.Role, principalTable: "Roles", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "StockAdjustments", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Delta = table.Column<int>(type: "int", nullable: false), Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false), ChangedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true) }, constraints: table =>
            {
                table.PrimaryKey("PK_StockAdjustments", x => x.Id);
                table.CheckConstraint("CK_Adjustment_Delta", "[Delta] <> 0");
                table.ForeignKey(name: "FK_StockAdjustments_Branches_BranchId", column: x => x.BranchId, principalTable: "Branches", principalColumn: "Id");
                table.ForeignKey(name: "FK_StockAdjustments_Products_ProductId", column: x => x.ProductId, principalTable: "Products", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "CounterTransactions", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), CounterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false), Status = table.Column<int>(type: "int", nullable: false), StatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_CounterTransactions", x => x.Id);
                table.CheckConstraint("CK_Transaction_Status", "[Status] IN (0,1,2,3)");
                table.ForeignKey(name: "FK_CounterTransactions_Branches_BranchId", column: x => x.BranchId, principalTable: "Branches", principalColumn: "Id");
                table.ForeignKey(name: "FK_CounterTransactions_Counters_CounterId_BranchId", columns: x => new { x.CounterId, x.BranchId }, principalTable: "Counters", principalColumns: new[] { "Id", "BranchId" });
                table.ForeignKey(name: "FK_CounterTransactions_Staff_StaffId", column: x => x.StaffId, principalTable: "Staff", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "MoneyOrders", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), PayeeName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), IdentityVerificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), OrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false), OrderCurrency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_MoneyOrders", x => x.Id);
                table.CheckConstraint("CK_MoneyOrder_Amount", "[OrderAmount] > 0");
                table.ForeignKey(name: "FK_MoneyOrders_Branches_BranchId", column: x => x.BranchId, principalTable: "Branches", principalColumn: "Id");
                table.ForeignKey(name: "FK_MoneyOrders_IdentityVerifications_IdentityVerificationId", column: x => x.IdentityVerificationId, principalTable: "IdentityVerifications", principalColumn: "Id");
                table.ForeignKey(name: "FK_MoneyOrders_Staff_StaffId", column: x => x.StaffId, principalTable: "Staff", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "Payments", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false), PaidCurrency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Payments", x => x.Id);
                table.CheckConstraint("CK_Payment_Amount", "[PaidAmount] > 0");
                table.ForeignKey(name: "FK_Payments_CounterTransactions_TransactionId", column: x => x.TransactionId, principalTable: "CounterTransactions", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "Shipments", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), ZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false), ServiceLevel = table.Column<int>(type: "int", nullable: false), Weight = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false), RecipientName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false), Address_Street = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false), Address_City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), Address_PostalCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), ChargeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false), ChargeCurrency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_Shipments", x => x.Id);
                table.CheckConstraint("CK_Shipment_Weight", "[Weight] > 0 AND [Weight] <= 30000");
                table.ForeignKey(name: "FK_Shipments_CounterTransactions_TransactionId", column: x => x.TransactionId, principalTable: "CounterTransactions", principalColumn: "Id");
                table.ForeignKey(name: "FK_Shipments_ShippingZones_ZoneId", column: x => x.ZoneId, principalTable: "ShippingZones", principalColumn: "Id");
            });
            migrationBuilder.CreateTable(name: "TransactionLines", columns: table => new { Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false), Type = table.Column<int>(type: "int", nullable: false), ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true), Code = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false), ShippingLevel = table.Column<int>(type: "int", nullable: true), CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"), CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"), ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true), TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true), Quantity = table.Column<int>(type: "int", nullable: false), UnitPriceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false), UnitPriceCurrency = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false) }, constraints: table =>
            {
                table.PrimaryKey("PK_TransactionLines", x => x.Id);
                table.CheckConstraint("CK_Line_Price", "[UnitPriceAmount] >= 0");
                table.CheckConstraint("CK_Line_Product", "[Type] <> 0 OR [ProductId] IS NOT NULL");
                table.CheckConstraint("CK_Line_Quantity", "[Quantity] > 0");
                table.CheckConstraint("CK_Line_Type", "[Type] IN (0,1,2)");
                table.ForeignKey(name: "FK_TransactionLines_CounterTransactions_TransactionId", column: x => x.TransactionId, principalTable: "CounterTransactions", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_TransactionLines_Products_ProductId", column: x => x.ProductId, principalTable: "Products", principalColumn: "Id");
            });
            migrationBuilder.CreateIndex(name: "IX_Branches_Code", table: "Branches", column: "Code", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Branches_RegionId", table: "Branches", column: "RegionId");
            migrationBuilder.CreateIndex(name: "IX_BranchStock_BranchId_ProductId", table: "BranchStock", columns: new[] { "BranchId", "ProductId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_BranchStock_ProductId", table: "BranchStock", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_Counters_BranchId_Number", table: "Counters", columns: new[] { "BranchId", "Number" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_CounterTransactions_BranchId", table: "CounterTransactions", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_CounterTransactions_CounterId_BranchId", table: "CounterTransactions", columns: new[] { "CounterId", "BranchId" });
            migrationBuilder.CreateIndex(name: "IX_CounterTransactions_StaffId", table: "CounterTransactions", column: "StaffId");
            migrationBuilder.CreateIndex(name: "IX_MoneyOrders_BranchId", table: "MoneyOrders", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_MoneyOrders_IdentityVerificationId", table: "MoneyOrders", column: "IdentityVerificationId");
            migrationBuilder.CreateIndex(name: "IX_MoneyOrders_StaffId", table: "MoneyOrders", column: "StaffId");
            migrationBuilder.CreateIndex(name: "IX_Payments_TransactionId", table: "Payments", column: "TransactionId", unique: true);
            migrationBuilder.CreateIndex(name: "IX_ProductCategories_Name", table: "ProductCategories", column: "Name", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Products_Category", table: "Products", column: "Category");
            migrationBuilder.CreateIndex(name: "IX_Products_Sku", table: "Products", column: "Sku", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Regions_Code", table: "Regions", column: "Code", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Roles_Name", table: "Roles", column: "Name", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Shipments_TransactionId", table: "Shipments", column: "TransactionId");
            migrationBuilder.CreateIndex(name: "IX_Shipments_ZoneId", table: "Shipments", column: "ZoneId");
            migrationBuilder.CreateIndex(name: "IX_ShippingRates_ZoneId_ServiceLevel", table: "ShippingRates", columns: new[] { "ZoneId", "ServiceLevel" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_ShippingZones_Code", table: "ShippingZones", column: "Code", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Staff_BranchId", table: "Staff", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_Staff_Email", table: "Staff", column: "Email", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Staff_Role", table: "Staff", column: "Role");
            migrationBuilder.CreateIndex(name: "IX_StockAdjustments_BranchId", table: "StockAdjustments", column: "BranchId");
            migrationBuilder.CreateIndex(name: "IX_StockAdjustments_ProductId", table: "StockAdjustments", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_TransactionLines_ProductId", table: "TransactionLines", column: "ProductId");
            migrationBuilder.CreateIndex(name: "IX_TransactionLines_TransactionId", table: "TransactionLines", column: "TransactionId");
        }

        /// <inheritdoc/>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AuditEntries");
            migrationBuilder.DropTable(name: "BranchStock");
            migrationBuilder.DropTable(name: "MoneyOrders");
            migrationBuilder.DropTable(name: "Payments");
            migrationBuilder.DropTable(name: "Shipments");
            migrationBuilder.DropTable(name: "ShippingRates");
            migrationBuilder.DropTable(name: "StockAdjustments");
            migrationBuilder.DropTable(name: "TransactionLines");
            migrationBuilder.DropTable(name: "IdentityVerifications");
            migrationBuilder.DropTable(name: "ShippingZones");
            migrationBuilder.DropTable(name: "CounterTransactions");
            migrationBuilder.DropTable(name: "Products");
            migrationBuilder.DropTable(name: "Counters");
            migrationBuilder.DropTable(name: "Staff");
            migrationBuilder.DropTable(name: "ProductCategories");
            migrationBuilder.DropTable(name: "Branches");
            migrationBuilder.DropTable(name: "Roles");
            migrationBuilder.DropTable(name: "Regions");
        }
    }
}
