using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexPost.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryTrackingAndCourierScans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrackedShipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TrackingNumber = table.Column<string>(type: "varchar(12)", unicode: false, maxLength: 12, nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    RecipientPhone = table.Column<string>(type: "varchar(25)", unicode: false, maxLength: 25, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackedShipments", x => x.Id);
                    table.CheckConstraint("CK_TrackedShipment_Status", "[Status] IN ('Created','PickedUp','InTransit','OutForDelivery','Delivered','Failed')");
                    table.CheckConstraint("CK_TrackedShipment_Weight", "[WeightKg] BETWEEN 0.1 AND 70");
                });

            migrationBuilder.CreateTable(
                name: "CourierScans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(42)", unicode: false, maxLength: 42, nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScanType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CourierId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScannedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierScans", x => x.Id);
                    table.CheckConstraint("CK_CourierScan_Type", "[ScanType] IN ('Pickup','InTransit','Delivered','Failed')");
                    table.ForeignKey(
                        name: "FK_CourierScans_TrackedShipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "TrackedShipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourierScans_ShipmentId_ScannedAtUtc",
                table: "CourierScans",
                columns: new[] { "ShipmentId", "ScannedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackedShipments_Status_City",
                table: "TrackedShipments",
                columns: new[] { "Status", "City" });

            migrationBuilder.CreateIndex(
                name: "IX_TrackedShipments_TrackingNumber",
                table: "TrackedShipments",
                column: "TrackingNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourierScans");

            migrationBuilder.DropTable(
                name: "TrackedShipments");
        }
    }
}
