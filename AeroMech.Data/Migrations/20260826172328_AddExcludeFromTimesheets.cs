using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroMech.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExcludeFromTimesheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExcludeFromTimesheets",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // The owners are not tracked on timesheets.
            migrationBuilder.Sql(@"
                UPDATE ""Employees""
                SET ""ExcludeFromTimesheets"" = true
                WHERE lower(regexp_replace(trim(coalesce(""FirstName"", '') || ' ' || coalesce(""LastName"", '')), '\s+', ' ', 'g'))
                      IN ('henry barker', 'johann petrus jonker', 'hanno jonker');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExcludeFromTimesheets",
                table: "Employees");
        }
    }
}
