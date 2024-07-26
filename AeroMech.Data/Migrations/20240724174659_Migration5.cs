using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroMech.Data.Migrations
{
    /// <inheritdoc />
    public partial class Migration5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportNumber",
                table: "ServiceReports");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateInService",
                table: "Vehicles",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateOnly>(
                name: "DateInService",
                table: "Vehicles",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportNumber",
                table: "ServiceReports",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
