using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroMech.Data.Migrations
{
    /// <inheritdoc />
    public partial class Migration19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RateType",
                table: "ServiceReportEmployees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "EmployeeRates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RateType",
                table: "EmployeeRates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRates_ClientId",
                table: "EmployeeRates",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeRates_Clients_ClientId",
                table: "EmployeeRates",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeRates_Clients_ClientId",
                table: "EmployeeRates");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeRates_ClientId",
                table: "EmployeeRates");

            migrationBuilder.DropColumn(
                name: "RateType",
                table: "ServiceReportEmployees");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "EmployeeRates");

            migrationBuilder.DropColumn(
                name: "RateType",
                table: "EmployeeRates");
        }
    }
}
