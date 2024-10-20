using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IDocument = AeroMech.UI.Web.Reports.IDocument;
using System.Globalization;
using AeroMech.Models;

namespace AeroMech.API.Reports
{
    public class FieldServiceReport : IDocument
    {
        public ServiceReportModel serviceReport { get; set; }

        public FieldServiceReport()
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
                row.ConstantItem(200).Image(path); 

                row.RelativeItem().ContentFromRightToLeft().Column(column =>
                {

                    column.Item().Text($"Field Service Report").Style(titleStyle);
                    column.Item().Text(text =>
                    {
                        text.Span($"Report No:        AEM{serviceReport.Id}").SemiBold().FontSize(12);
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
                    row.RelativeItem().PaddingBottom(20).Component(new ServiceReportOrderInfoLeft(new OrderInfo()
                    {
                        Date = serviceReport.ReportDate.ToShortDateString(),
                        Client = serviceReport.Client.Name,
                        EngineHours = serviceReport.Vehicle.EngineHours.ToString(),
                        MachineType = serviceReport.Vehicle.MachineType,
                        SerialNumber = serviceReport.Vehicle.SerialNumber,
                        Instructions = serviceReport.Instruction
                    }));

                    row.ConstantItem(5);
                    row.ConstantItem(55, Unit.Millimetre).Component(new ServiceReportOrderInfoMiddle(serviceReport.SalesOrderNumber));
                    row.ConstantItem(5);
                    row.ConstantItem(36, Unit.Millimetre).Component(new ServiceReportOrderInfoRight(serviceReport.JobNumber.ToString()));
                });

                column.Item().Text("Labour").SemiBold().FontSize(14);
                column.Item().Element(ComposeLabourTable);
                column.Item().Text("Parts").SemiBold().FontSize(14);
                column.Item().Element(ComposePartsTable);
                column.Item().PaddingBottom(10).Text("Detailed Service Report").FontSize(12).SemiBold().Underline();

                var serviceDescription = serviceReport.DetailedServiceReport;
                column.Item().Text(serviceDescription).FontSize(10);

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
                    row.RelativeItem().Text("Employees Signature");
                    row.ConstantItem(0);
                    row.RelativeItem().Text("Customers Signature");
                });


                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text("We accept that the work detailed above is satisfactory, and the\r\nlabour and materials reflected are correct.").FontSize(10);
                    row.ConstantItem(50);


                    row.RelativeItem().Column(col =>
                    {
                        var totalLabour = serviceReport.Employees.Where(x => !x.IsDeleted).Sum(x => CalulatePercentageOf(x.Rate, x.Hours, x.Discount));
                        var totalParts = serviceReport.Parts.Where(x => !x.IsDeleted).Sum(x => CalulatePercentageOf(x.CostPrice, x.QTY, x.Discount));

                        col.Item().Row(r =>
                        {
                            r.RelativeColumn().PaddingTop(10).Text("Labour :");
                            r.RelativeColumn().AlignRight().PaddingTop(10).Text(totalLabour.ToString("C", CultureInfo.CurrentCulture));
                        });
                        col.Item().Row(r =>
                        {
                            r.RelativeColumn().PaddingTop(10).Text("Parts:");
                            r.RelativeColumn().AlignRight().PaddingTop(10).Text(totalParts.ToString("C", CultureInfo.CurrentCulture));
                        });
                        col.Item().Row(r =>
                        {
                            r.RelativeColumn(4).PaddingTop(12).Text("Total Cost :").Bold();
                            r.RelativeColumn(4).AlignRight().PaddingTop(10).BorderTop(1).BorderBottom(1).Text((totalLabour + totalParts).ToString("C", CultureInfo.CurrentCulture)).LineHeight(2).Bold();
                        });
                    });
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

        public double CalulatePercentageOf(double? value = 0, double? multiplier = 0, double? discount = 0)
        {
            return (double)((multiplier * value) - ((multiplier * value) * (discount / 100)));
        }

        void ComposePartsTable(IContainer container)
        {
            container.PaddingBottom(20).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                   // columns.RelativeColumn(1);
                    columns.RelativeColumn(1);

                });
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Part Number");
                    header.Cell().Element(CellStyle).Text("Description");
                    header.Cell().Element(CellStyle).AlignRight().Text("C.P.U.");
                    header.Cell().Element(CellStyle).AlignRight().Text("Qty");
                    header.Cell().Element(CellStyle).AlignRight().Text("Actual");
                   // header.Cell().Element(CellStyle).AlignRight().Text("Disc");
                    header.Cell().Element(CellStyle).AlignRight().Text("Total");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold().FontSize(12)).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    }
                });

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.FontSize(10)).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
				//foreach (var part in serviceReport.AdHockParts.Where(x => !x.IsDeleted))
				//{
				//	table.Cell().Element(CellStyle).Text(part.PartCode);
				//	table.Cell().Element(CellStyle).Text(part.PartDescription);
				//	table.Cell().Element(CellStyle).AlignRight().Text(part.CostPrice.ToString("C", CultureInfo.CurrentCulture));
				//	table.Cell().Element(CellStyle).AlignRight().Text(part.Qty);
				//	table.Cell().Element(CellStyle).AlignRight().Text((part.CostPrice * part.Qty).ToString("C", CultureInfo.CurrentCulture));
				//	table.Cell().Element(CellStyle).AlignRight().Text(part.Discount.ToString("C", CultureInfo.CurrentCulture));
				//	table.Cell().Element(CellStyle).AlignRight().Text(CalulatePercentageOf(part.CostPrice, part.Qty, part.Discount).ToString("C", CultureInfo.CurrentCulture));
				//}
				foreach (var part in serviceReport.Parts.Where(x => !x.IsDeleted))
                {
                    table.Cell().Element(CellStyle).Text(part.PartCode);
                    table.Cell().Element(CellStyle).Text(part.PartDescription);
                    table.Cell().Element(CellStyle).AlignRight().Text(part.CostPrice.ToString("C", CultureInfo.CurrentCulture));
                    table.Cell().Element(CellStyle).AlignRight().Text(part.QTY);
                    table.Cell().Element(CellStyle).AlignRight().Text((part.CostPrice * part.QTY).ToString("C", CultureInfo.CurrentCulture));
                    //table.Cell().Element(CellStyle).AlignRight().Text(part.Discount.ToString("C", CultureInfo.CurrentCulture));
                    table.Cell().Element(CellStyle).AlignRight().Text(CalulatePercentageOf(part.CostPrice, part.QTY, part.Discount).ToString("C", CultureInfo.CurrentCulture));
                }
			});
        }

        void ComposeLabourTable(IContainer container)
        {
            container.PaddingBottom(20).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    //columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                });
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Employee Name");
                    header.Cell().Element(CellStyle).Text("Service Type");
                    header.Cell().Element(CellStyle).Text("Date");
                    header.Cell().Element(CellStyle).AlignRight().Text("Rate");
                    header.Cell().Element(CellStyle).AlignRight().Text("Hours");
                    header.Cell().Element(CellStyle).AlignRight().Text("Actual");
                    //header.Cell().Element(CellStyle).AlignRight().Text("Disc");
                    header.Cell().Element(CellStyle).AlignRight().Text("Total");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold().FontSize(12)).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    }
                });

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.FontSize(10)).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }

                foreach (var employee in serviceReport.Employees.Where(x => !x.IsDeleted))
                {
                    table.Cell().Element(CellStyle).Text($"{employee.FirstName} {employee.LastName}");
                    table.Cell().Element(CellStyle).Text(serviceReport.ServiceType);
                    table.Cell().Element(CellStyle).Text(employee.DutyDate.ToShortDateString());
                    table.Cell().Element(CellStyle).AlignRight().Text(employee.Rate?.ToString("C", CultureInfo.CurrentCulture));
                    table.Cell().Element(CellStyle).AlignRight().Text(employee.Hours?.ToString("C", CultureInfo.CurrentCulture));
                    table.Cell().Element(CellStyle).AlignRight().Text((employee.Rate * employee.Hours)?.ToString("C", CultureInfo.CurrentCulture));
                    //table.Cell().Element(CellStyle).AlignRight().Text(employee.Discount?.ToString("C", CultureInfo.CurrentCulture));
                    table.Cell().Element(CellStyle).AlignRight().Text(CalulatePercentageOf(employee.Rate, employee.Hours, employee.Discount).ToString("C", CultureInfo.CurrentCulture));
                }
            });
        }
    }
}
