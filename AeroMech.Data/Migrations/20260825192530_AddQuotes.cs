using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AeroMech.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuoteId",
                table: "ServiceReports",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuoteNumber = table.Column<int>(type: "integer", nullable: false),
                    QuoteDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ClientId = table.Column<int>(type: "integer", nullable: true),
                    VehicleId = table.Column<int>(type: "integer", nullable: true),
                    Instruction = table.Column<string>(type: "text", nullable: true),
                    DetailedServiceReport = table.Column<string>(type: "text", nullable: true),
                    ServiceType = table.Column<string>(type: "text", nullable: true),
                    VehicleHours = table.Column<int>(type: "integer", nullable: true),
                    ConvertedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Quotes_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuoteAdHockParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuoteId = table.Column<int>(type: "integer", nullable: false),
                    PartCode = table.Column<string>(type: "text", nullable: false),
                    PartDescription = table.Column<string>(type: "text", nullable: false),
                    CostPrice = table.Column<double>(type: "double precision", nullable: false),
                    Discount = table.Column<double>(type: "double precision", nullable: false),
                    Qty = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteAdHockParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteAdHockParts_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuoteLabour",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuoteId = table.Column<int>(type: "integer", nullable: false),
                    RateType = table.Column<int>(type: "integer", nullable: false),
                    Rate = table.Column<double>(type: "double precision", nullable: false),
                    Hours = table.Column<double>(type: "double precision", nullable: false),
                    Discount = table.Column<double>(type: "double precision", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteLabour", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteLabour_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuoteParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuoteId = table.Column<int>(type: "integer", nullable: false),
                    PartId = table.Column<int>(type: "integer", nullable: false),
                    CostPrice = table.Column<double>(type: "double precision", nullable: false),
                    Discount = table.Column<double>(type: "double precision", nullable: false),
                    Qty = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteParts_Parts_PartId",
                        column: x => x.PartId,
                        principalTable: "Parts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuoteParts_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceReports_QuoteId",
                table: "ServiceReports",
                column: "QuoteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteAdHockParts_QuoteId",
                table: "QuoteAdHockParts",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteLabour_QuoteId",
                table: "QuoteLabour",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteParts_PartId",
                table: "QuoteParts",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteParts_QuoteId",
                table: "QuoteParts",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_ClientId",
                table: "Quotes",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_VehicleId",
                table: "Quotes",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceReports_Quotes_QuoteId",
                table: "ServiceReports",
                column: "QuoteId",
                principalTable: "Quotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            BackfillQuotesFromQuotedServiceReports(migrationBuilder);
        }

        /// <summary>
        /// Before quotes had tables of their own, quoting a job wrote a service report with a
        /// quote number on it. Those reports had already moved stock and logged hours, so they
        /// stay service reports; each one gets a quote alongside it, marked as already converted
        /// and pointing at the report it produced.
        /// </summary>
        private static void BackfillQuotesFromQuotedServiceReports(MigrationBuilder migrationBuilder)
        {
            // The new quote rows have to be matched back to the reports they came from, and the
            // quote number is not reliably unique, so the source id rides along and is dropped
            // again once the links are made.
            migrationBuilder.Sql(@"ALTER TABLE ""Quotes"" ADD COLUMN ""SourceServiceReportId"" integer;");

            migrationBuilder.Sql(@"
                INSERT INTO ""Quotes"" (
                    ""QuoteNumber"", ""QuoteDate"", ""Description"", ""ClientId"", ""VehicleId"",
                    ""Instruction"", ""DetailedServiceReport"", ""ServiceType"", ""VehicleHours"",
                    ""ConvertedDate"", ""IsDeleted"", ""SourceServiceReportId"")
                SELECT
                    sr.""QuoteNumber"", sr.""ReportDate"", sr.""Description"", sr.""ClientId"", sr.""VehicleId"",
                    sr.""Instruction"", sr.""DetailedServiceReport"", sr.""ServiceType"", sr.""VehicleHours"",
                    sr.""ReportDate"", sr.""IsDeleted"", sr.""Id""
                FROM ""ServiceReports"" sr
                WHERE sr.""QuoteNumber"" IS NOT NULL AND sr.""QuoteNumber"" > 0;");

            migrationBuilder.Sql(@"
                UPDATE ""ServiceReports"" sr
                SET ""QuoteId"" = q.""Id""
                FROM ""Quotes"" q
                WHERE q.""SourceServiceReportId"" = sr.""Id"";");

            // Labour was recorded per person; a quote holds it per rate type, so the old rows are
            // summed down to one line per rate.
            migrationBuilder.Sql(@"
                INSERT INTO ""QuoteLabour"" (""QuoteId"", ""RateType"", ""Rate"", ""Hours"", ""Discount"", ""IsDeleted"")
                SELECT q.""Id"", sre.""RateType"", sre.""Rate"", SUM(sre.""Hours""), 0, false
                FROM ""Quotes"" q
                JOIN ""ServiceReportEmployees"" sre ON sre.""ServiceReportId"" = q.""SourceServiceReportId""
                WHERE q.""SourceServiceReportId"" IS NOT NULL AND sre.""IsDeleted"" = false
                GROUP BY q.""Id"", sre.""RateType"", sre.""Rate"";");

            migrationBuilder.Sql(@"
                INSERT INTO ""QuoteParts"" (""QuoteId"", ""PartId"", ""CostPrice"", ""Discount"", ""Qty"", ""IsDeleted"")
                SELECT q.""Id"", srp.""PartId"", srp.""CostPrice"", srp.""Discount"", srp.""Qty"", srp.""IsDeleted""
                FROM ""Quotes"" q
                JOIN ""ServiceReportParts"" srp ON srp.""ServiceReportId"" = q.""SourceServiceReportId""
                WHERE q.""SourceServiceReportId"" IS NOT NULL;");

            migrationBuilder.Sql(@"
                INSERT INTO ""QuoteAdHockParts"" (""QuoteId"", ""PartCode"", ""PartDescription"", ""CostPrice"", ""Discount"", ""Qty"", ""IsDeleted"")
                SELECT q.""Id"", COALESCE(srap.""PartCode"", ''), COALESCE(srap.""PartDescription"", ''),
                       srap.""CostPrice"", srap.""Discount"", srap.""Qty"", srap.""IsDeleted""
                FROM ""Quotes"" q
                JOIN ""ServiceReportAdHockPart"" srap ON srap.""ServiceReportId"" = q.""SourceServiceReportId""
                WHERE q.""SourceServiceReportId"" IS NOT NULL;");

            migrationBuilder.Sql(@"ALTER TABLE ""Quotes"" DROP COLUMN ""SourceServiceReportId"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceReports_Quotes_QuoteId",
                table: "ServiceReports");

            migrationBuilder.DropTable(
                name: "QuoteAdHockParts");

            migrationBuilder.DropTable(
                name: "QuoteLabour");

            migrationBuilder.DropTable(
                name: "QuoteParts");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropIndex(
                name: "IX_ServiceReports_QuoteId",
                table: "ServiceReports");

            migrationBuilder.DropColumn(
                name: "QuoteId",
                table: "ServiceReports");
        }
    }
}
