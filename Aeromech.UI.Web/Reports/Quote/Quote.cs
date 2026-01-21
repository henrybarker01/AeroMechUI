using AeroMech.Data.Models;
using AeroMech.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using IDocument = AeroMech.UI.Web.Reports.IDocument;

namespace AeroMech.API.Reports
{
    public class Quote : IDocument
    {
        public ServiceReportModel serviceReport { get; set; }

        public Quote()
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

                var path = Path.Combine(Environment.CurrentDirectory, @"Reports\Images\", "AreoMechSmall.png");
                //var path = Path.Combine(@"C:\Projects\VMI\Aeromech.UI.Web\", @"Reports\Images\", "AreoMechSmall.png");

                row.ConstantItem(200).Image(path);

                row.RelativeItem().ContentFromRightToLeft().Column(column =>
                {

                    column.Item().Text($"Service Quote").Style(titleStyle);

                    column.Item().Text(text =>
                    {
                        text.Span($"Quote No:  AEM{serviceReport.QuoteNumber}").SemiBold().FontSize(12);
                    });
                    column.Item().Text(text =>
                    {
                        text.Span($"Field Service Report No:  AEM{serviceReport.ServiceReportNumber}").SemiBold().FontSize(12);
                    });
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
                        Date = serviceReport.ReportDate.ToString("dd/MM/yyyy"),
                        Client = serviceReport.Client.Name,
                        EngineHours = serviceReport.VehicleHours.ToString() ?? "0", //serviceReport.Vehicle.EngineHours.ToString(),
                        MachineType = serviceReport.Vehicle.MachineType,
                        SerialNumber = serviceReport.Vehicle.SerialNumber,
                        Instructions = serviceReport.Instruction
                    }));

                    row.ConstantItem(5);
                });

                column.Item().Element(ComposeLabourTable);

                column.Item().PaddingBottom(10).Text("Detailed Service Report").FontSize(12).SemiBold().Underline();

                var serviceDescription = serviceReport.DetailedServiceReport;
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

            static IContainer Block(IContainer container)
            {
                return container
                    .Border(1)
                    .Background(Colors.Grey.Lighten3)
                    .ShowOnce()
                    .MinWidth(50)
                    .MinHeight(50)
                    .AlignCenter()
                    .AlignMiddle();
            }
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

                serviceReport.Employees.Where(x => !x.IsDeleted).GroupBy(e => e.Rate).Select(g => new
                {
                    Rate = g.Key,
                    Hours = g.Sum(e => e.Hours),

                }).ToList().ForEach(x =>
                {
                    table.Cell().Element(CellStyle).Text(x.Hours.ToString());
                    table.Cell().Element(CellStyle).Text("Labour");
                    table.Cell().Element(CellStyle).Text("EA");
                    table.Cell().Element(CellStyle).AlignRight().Text(string.Empty);// serviceReport.ServiceType
                    table.Cell().Element(CellStyle).AlignRight().Text(x.Rate.ToString("C", CultureInfo.CurrentCulture));
                    table.Cell().Element(CellStyle).AlignRight().Text((x.Hours * x.Rate)?.ToString("C", CultureInfo.CurrentCulture));
                });

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

                foreach (var part in serviceReport.Parts.Where(x => x.IsDeleted == false))
                {
                    table.Cell().Element(CellStyle).Text(part.QTY.ToString());
                    table.Cell().Element(CellStyle).Text(part?.PartCode);
                    table.Cell().Element(CellStyle).Text("EA");
                    table.Cell().Element(CellStyle).AlignRight().Text(part?.PartDescription);
                    table.Cell().Element(CellStyle).AlignRight().Text(part?.CostPrice.ToString("C", CultureInfo.CurrentCulture));
                    table.Cell().Element(CellStyle).AlignRight().Text((part.CostPrice * part.QTY).ToString("C", CultureInfo.CurrentCulture));
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
                table.Cell().Element(CellTotalsStyle).AlignRight().Text(serviceReport.Parts.Where(x => !x.IsDeleted).Sum(x => x.CostPrice * x.QTY).ToString("C", CultureInfo.CurrentCulture));
                table.Cell().Border(0).Text("");
                table.Cell().Border(0).Text("");
                table.Cell().Border(0).Text("");
                table.Cell().Border(0).Text("");
                table.Cell().Element(CellTotalsStyle).AlignRight().Text("Total Excl VAT:");
                table.Cell().Element(CellTotalsStyle).AlignRight().Text(
                    (
                        serviceReport.Parts.Where(x => x.IsDeleted == false).Sum(x => Convert.ToDouble(x.CostPrice) * x.QTY) +
                        serviceReport.Employees.Where(x => !x.IsDeleted).Sum(x => x.Hours * x.Rate))?
                        .ToString("C", CultureInfo.CurrentCulture)
                    );

            });
        }
    }
}
