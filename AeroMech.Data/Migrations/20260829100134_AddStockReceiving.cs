using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AeroMech.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReceiving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StockReceiptId",
                table: "StockAdjustment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockReceipts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierCode = table.Column<string>(type: "text", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    InvoiceDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReceivedBy = table.Column<string>(type: "text", nullable: true),
                    InvoiceSubTotal = table.Column<double>(type: "double precision", nullable: false),
                    InvoiceVat = table.Column<double>(type: "double precision", nullable: false),
                    InvoiceTotal = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReceipts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockReceiptLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockReceiptId = table.Column<int>(type: "integer", nullable: false),
                    PartId = table.Column<int>(type: "integer", nullable: false),
                    QtyReceived = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<double>(type: "double precision", nullable: false),
                    QtyOnHandBefore = table.Column<int>(type: "integer", nullable: false),
                    QtyOnHandAfter = table.Column<int>(type: "integer", nullable: false),
                    CostPriceUpdated = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReceiptLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockReceiptLines_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockReceiptLines_StockReceipts_StockReceiptId",
                        column: x => x.StockReceiptId,
                        principalTable: "StockReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_StockReceiptId",
                table: "StockAdjustment",
                column: "StockReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptLines_PartId",
                table: "StockReceiptLines",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceiptLines_StockReceiptId",
                table: "StockReceiptLines",
                column: "StockReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReceipts_SupplierCode_InvoiceNumber",
                table: "StockReceipts",
                columns: new[] { "SupplierCode", "InvoiceNumber" });

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustment_StockReceipts_StockReceiptId",
                table: "StockAdjustment",
                column: "StockReceiptId",
                principalTable: "StockReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustment_StockReceipts_StockReceiptId",
                table: "StockAdjustment");

            migrationBuilder.DropTable(
                name: "StockReceiptLines");

            migrationBuilder.DropTable(
                name: "StockReceipts");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustment_StockReceiptId",
                table: "StockAdjustment");

            migrationBuilder.DropColumn(
                name: "StockReceiptId",
                table: "StockAdjustment");
        }
    }
}
