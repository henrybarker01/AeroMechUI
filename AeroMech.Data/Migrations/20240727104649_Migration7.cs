using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroMech.Data.Migrations
{
    /// <inheritdoc />
    public partial class Migration7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "JobNumber",
                table: "ServiceReports",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "QuoteNumber",
                table: "ServiceReports",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuoteNumber",
                table: "ServiceReports");

            migrationBuilder.AlterColumn<int>(
                name: "JobNumber",
                table: "ServiceReports",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
