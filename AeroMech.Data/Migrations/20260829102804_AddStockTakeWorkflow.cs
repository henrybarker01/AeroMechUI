using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroMech.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTakeWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTakes_Warehouse_WarehouseId",
                table: "StockTakes");

            migrationBuilder.DropIndex(
                name: "IX_StockTakes_WarehouseId",
                table: "StockTakes");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "StockTakes");

            // EF scaffolded this pair as a rename because both columns are integers. They are
            // unrelated: dropping the warehouse and adding the sheet number is what is meant, and
            // a rename would have carried warehouse ids across as sheet numbers.
            migrationBuilder.DropColumn(
                name: "WarehouseId",
                table: "StockTakes");

            migrationBuilder.AddColumn<int>(
                name: "StockTakeNumber",
                table: "StockTakes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Any sheet already on file gets a number in id order, so the unique index below has
            // something distinct to bite on. A no-op on an empty table.
            migrationBuilder.Sql(@"
                UPDATE ""StockTakes"" st
                SET ""StockTakeNumber"" = sub.rn
                FROM (SELECT ""Id"", ROW_NUMBER() OVER (ORDER BY ""Id"") AS rn FROM ""StockTakes"") sub
                WHERE st.""Id"" = sub.""Id"";");

            migrationBuilder.AlterColumn<string>(
                name: "StockTakeDescription",
                table: "StockTakes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "StockTakeBy",
                table: "StockTakes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "StockTakes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CompletedDate",
                table: "StockTakes",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<bool>(
                name: "BlindCount",
                table: "StockTakes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CompletedBy",
                table: "StockTakes",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "StockTakeParts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "StockTakeParts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "FinalQuantity",
                table: "StockTakeParts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "AppliedDelta",
                table: "StockTakeParts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Bin",
                table: "StockTakeParts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CountedAt",
                table: "StockTakeParts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountedBy",
                table: "StockTakeParts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreviousQuantity",
                table: "StockTakeParts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QtyOnHandAtPost",
                table: "StockTakeParts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RecountCount",
                table: "StockTakeParts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "StockTakeParts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SupplierCode",
                table: "StockTakeParts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "UnitCost",
                table: "StockTakeParts",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "StockTakeId",
                table: "StockAdjustment",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTakes_StockTakeNumber",
                table: "StockTakes",
                column: "StockTakeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockAdjustment_StockTakeId",
                table: "StockAdjustment",
                column: "StockTakeId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockAdjustment_StockTakes_StockTakeId",
                table: "StockAdjustment",
                column: "StockTakeId",
                principalTable: "StockTakes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockAdjustment_StockTakes_StockTakeId",
                table: "StockAdjustment");

            migrationBuilder.DropIndex(
                name: "IX_StockTakes_StockTakeNumber",
                table: "StockTakes");

            migrationBuilder.DropIndex(
                name: "IX_StockAdjustment_StockTakeId",
                table: "StockAdjustment");

            migrationBuilder.DropColumn(
                name: "BlindCount",
                table: "StockTakes");

            migrationBuilder.DropColumn(
                name: "CompletedBy",
                table: "StockTakes");

            migrationBuilder.DropColumn(
                name: "AppliedDelta",
                table: "StockTakeParts");

            migrationBuilder.DropColumn(
                name: "Bin",
                table: "StockTakeParts");

            migrationBuilder.DropColumn(
                name: "CountedAt",
                table: "StockTakeParts");

            migrationBuilder.DropColumn(
                name: "CountedBy",
                table: "StockTakeParts");

            migrationBuilder.DropColumn(
                name: "PreviousQuantity",
                table: "StockTakeParts");

            migrationBuilder.DropColumn(
                name: "QtyOnHandAtPost",
                table: "StockTakeParts");

            migrationBuilder.DropColumn(
                name: "RecountCount",
                table: "StockTakeParts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StockTakeParts");

            migrationBuilder.DropColumn(
                name: "SupplierCode",
                table: "StockTakeParts");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "StockTakeParts");

            migrationBuilder.DropColumn(
                name: "StockTakeId",
                table: "StockAdjustment");

            migrationBuilder.DropColumn(
                name: "StockTakeNumber",
                table: "StockTakes");

            migrationBuilder.AddColumn<int>(
                name: "WarehouseId",
                table: "StockTakes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "StockTakeDescription",
                table: "StockTakes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StockTakeBy",
                table: "StockTakes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "StockTakes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CompletedDate",
                table: "StockTakes",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "StockTakes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "StockTakeParts",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "StockTakeParts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FinalQuantity",
                table: "StockTakeParts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTakes_WarehouseId",
                table: "StockTakes",
                column: "WarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTakes_Warehouse_WarehouseId",
                table: "StockTakes",
                column: "WarehouseId",
                principalTable: "Warehouse",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
