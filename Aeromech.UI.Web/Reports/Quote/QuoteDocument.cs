using AeroMech.Models.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using IDocument = AeroMech.UI.Web.Reports.IDocument;

namespace AeroMech.API.Reports
{
    public class QuoteDocument : IDocument
    {
        public QuoteModel quote { get; set; }

        public QuoteDocument()
        {

        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(20);
                    page.Size(pageSize: PageSizes.A4);

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);


                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
        }

        void ComposeHeader(IContainer container)
        {
            var titleStyle = TextStyle.Default.FontSize(22).Bold().FontColor(Colors.Black);
            container.Row(row =>
            {

                var path = Path.Combine(AppContext.BaseDirectory, "Reports", "Images", "AreoMechSmall.png");

                row.ConstantItem(200).Image(path);

                row.RelativeItem().ContentFromRightToLeft().Column(column =>
                {

                    column.Item().Text($"Service Quote").Style(titleStyle);

                    // A service report printed as a quote only carries a quote number if it
                    // started life as one, so the line is left off rather than printing AEM0.
                    if (quote.QuoteNumber > 0)
                    {
                        column.Item().Text(text =>
                        {
                            text.Span($"Quote No:  AEM{quote.QuoteNumber}").SemiBold().FontSize(12);
                        });
                    }

                    // A quote only carries a service report number once it has been accepted and
                    // converted, so the line is left off until then.
                    if (quote.ServiceReportNumber.HasValue)
                    {
                        column.Item().Text(text =>
                        {
                            text.Span($"Field Service Report No:  AEM{quote.ServiceReportNumber}").SemiBold().FontSize(12);
                        });
                    }
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(5);

                column.Item().Row(row =>
                {
                    row.RelativeItem().PaddingBottom(20).Component(new QuoteOrderInfoLeft(new OrderInfo()
                    {
                        Date = quote.QuoteDate.ToString("dd/MM/yyyy"),
                        Client = quote.Client?.Name,
                        EngineHours = quote.VehicleHours.ToString(),
                        MachineType = quote.Vehicle?.MachineType,
                        SerialNumber = quote.Vehicle?.SerialNumber,
                        Instructions = quote.Instruction
                    }));

                    row.ConstantItem(5);
                });

                column.Item().Element(ComposeLabourTable);

                column.Item().PaddingBottom(10).Text("Detailed Service Report").FontSize(12).SemiBold().Underline();

                var serviceDescription = quote.DetailedServiceReport;
                column.Item().Text(serviceDescription).FontSize(10);

                column.Item().PaddingBottom(10).PaddingTop(20).Text("Indicate your acceptance by signing below, if you accept this quote.").FontSize(12).SemiBold().Underline();

                column.Item().PaddingTop(60).Row(row =>
                {
                    row.ConstantItem(50);
                    row.ConstantItem(180).LineHorizontal(1);
                    row.ConstantItem(50);
                    row.ConstantItem(180).LineHorizontal(1);
                });
                column.Item().Row(row =>
                {
                    row.ConstantItem(85);
                    row.RelativeItem().Text("Name & Surname");
                    row.ConstantItem(0);
                    row.RelativeItem().Text("Name & Surname");
                });

                column.Item().PaddingTop(60).Row(row =>
                {
                    row.ConstantItem(50);
                    row.ConstantItem(180).LineHorizontal(1);
                    row.ConstantItem(50);
                    row.ConstantItem(180).LineHorizontal(1);
                });
                column.Item().Row(row =>
                {
                    row.ConstantItem(85);
                    row.RelativeItem().Text("Aeromech");
                    row.ConstantItem(0);
                    row.RelativeItem().Text("Customer");
                });
            });
        }

        void ComposeLabourTable(IContainer container)
        {
            container.PaddingBottom(20).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);

                });
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("QTY");
                    header.Cell().Element(CellStyle).Text("P/number");
                    header.Cell().Element(CellStyle).Text("Unit");
                    header.Cell().Element(CellStyle).AlignRight().Text("Parts");
                    header.Cell().Element(CellStyle).AlignRight().Text("Unit Price");
                    header.Cell().Element(CellStyle).AlignRight().Text("Total Value Ex VAT");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold().FontSize(12)).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    }
                });

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.FontSize(10)).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }

                // Quoted labour is already held per rate type, so each line prints as it stands.
                foreach (var labour in quote.Labour.Where(x => !x.IsDeleted))
                {
                    table.Cell().Element(CellStyle).Text(labour.Hours.ToString());
                    table.Cell().Element(CellStyle).Text("Labour");
                    table.Cell().Element(CellStyle).Text("EA");
                    table.Cell().Element(CellStyle).AlignRight().Text(labour.RateType.GetDisplayName());
                    table.Cell().Element(CellStyle).AlignRight().Text(labour.Rate.ToString("C", CultureInfo.CurrentCulture));
                    table.Cell().Element(CellStyle).AlignRight().Text(LineTotal(labour).ToString("C", CultureInfo.CurrentCulture));
                }

                static IContainer CellFlatStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.FontSize(1)).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4);
                }
                table.Cell().Element(CellFlatStyle).Text("");
                table.Cell().Element(CellFlatStyle).Text("");
                table.Cell().Element(CellFlatStyle).Text("");
                table.Cell().Element(CellFlatStyle).AlignRight().Text("");
                table.Cell().Element(CellFlatStyle).AlignRight().Text("");
                table.Cell().Element(CellFlatStyle).AlignRight().Text("");

                foreach (var part in quote.Parts.Where(x => x.IsDeleted == false))
                {
                    table.Cell().Element(CellStyle).Text(part.QTY.ToString());
                    table.Cell().Element(CellStyle).Text(part?.PartCode);
                    table.Cell().Element(CellStyle).Text("EA");
                    table.Cell().Element(CellStyle).AlignRight().Text(part?.PartDescription);
                    table.Cell().Element(CellStyle).AlignRight().Text(part?.CostPrice.ToString("C", CultureInfo.CurrentCulture));
                    table.Cell().Element(CellStyle).AlignRight().Text(LineTotal(part).ToString("C", CultureInfo.CurrentCulture));
                }

                static IContainer CellTotalsStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.FontSize(10).Bold()).BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(4);
                }

                table.Cell().Border(0).Text("");
                table.Cell().Border(0).Text("");
                table.Cell().Border(0).Text("");
                table.Cell().Border(0).Text("");
                table.Cell().Element(CellTotalsStyle).AlignRight().Text("Value of parts user for service:");
                table.Cell().Element(CellTotalsStyle).AlignRight().Text(quote.Parts.Where(x => !x.IsDeleted).Sum(LineTotal).ToString("C", CultureInfo.CurrentCulture));
                table.Cell().Border(0).Text("");
                table.Cell().Border(0).Text("");
                table.Cell().Border(0).Text("");
                table.Cell().Border(0).Text("");
                table.Cell().Element(CellTotalsStyle).AlignRight().Text("Total Excl VAT:");
                table.Cell().Element(CellTotalsStyle).AlignRight().Text(
                    (
                        quote.Parts.Where(x => !x.IsDeleted).Sum(LineTotal) +
                        quote.Labour.Where(x => !x.IsDeleted).Sum(LineTotal))
                        .ToString("C", CultureInfo.CurrentCulture)
                    );

            });
        }

        private static double LineTotal(QuotePartModel part)
            => part.CostPrice * part.QTY - part.Discount / 100 * (part.CostPrice * part.QTY);

        private static double LineTotal(QuoteLabourModel labour)
            => labour.Rate * labour.Hours - labour.Discount / 100 * (labour.Rate * labour.Hours);
    }
}
