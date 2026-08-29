using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using IDocument = AeroMech.UI.Web.Reports.IDocument;

namespace AeroMech.API.Reports
{
    public class TimesheetReport : IDocument
    {
        public TimesheetReportDocumentData Data { get; set; } = new();

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4.Landscape());

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

        private void ComposeHeader(IContainer container)
        {
            var titleStyle = TextStyle.Default.FontSize(22).Bold().FontColor(Colors.Black);

            container.Row(row =>
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Reports", "Images", "AreoMechSmall.png");
                row.ConstantItem(200).Image(path);

                row.RelativeItem().ContentFromRightToLeft().Column(column =>
                {
                    column.Item().Text("Timesheet Report").Style(titleStyle);
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(10).Column(column =>
            {
                column.Spacing(10);

                column.Item().AlignRight().Column(info =>
                {
                    info.Item().Text($"Week No :\t\t\t\t{Data.WeekNumber}").SemiBold().FontSize(10);
                    info.Item().Text($"Date :\t\t\t\t{Data.WeekStartDate:dd/MM/yyyy}").SemiBold().FontSize(10);

                    if (Data.SelectedClientNames.Count > 0)
                        info.Item().Text($"Clients :\t\t\t\t{string.Join(", ", Data.SelectedClientNames)}").SemiBold().FontSize(10);
                });

                column.Item().Element(ComposeMatrixTable);
            });
        }

        private void ComposeMatrixTable(IContainer container)
        {
            var employees = Data.Employees;
            var rows = Data.Rows;

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(140);
                    columns.ConstantColumn(220);

                    foreach (var _ in employees)
                        columns.ConstantColumn(55);

                    columns.ConstantColumn(65); // Total column
                });

                uint currentTableRow = 1;

                table.Cell().Row(currentTableRow).Column(1u).ColumnSpan(2)
                    .Element(HeaderBlankCellStyle)
                    .Text(string.Empty);

                for (var employeeIndex = 0; employeeIndex < employees.Count; employeeIndex++)
                {
                    var employee = employees[employeeIndex];

                    table.Cell().Row(currentTableRow).Column((uint)(3 + employeeIndex))
                        .Element(HeaderEmployeeCellStyle)
                        .AlignCenter()
                        .AlignMiddle()
                        .RotateLeft()
                        .Text(employee.DisplayName)
                        .FontSize(8)
                        .SemiBold();
                }

                table.Cell().Row(currentTableRow).Column((uint)(3 + employees.Count))
                    .Element(HeaderTotalCellStyle)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text("Total")
                    .FontSize(9)
                    .Bold();

                currentTableRow++;

                var rowIndex = 0;
                while (rowIndex < rows.Count)
                {
                    var row = rows[rowIndex];

                    // Grand total row
                    if (row.IsGrandTotalRow)
                    {
                        table.Cell().Row(currentTableRow).Column(1u).ColumnSpan(2)
                            .Element(GrandTotalLeftCellStyle)
                            .Padding(5)
                            .Text("Total");

                        var grandTotal = 0.0;
                        for (var employeeIndex = 0; employeeIndex < employees.Count; employeeIndex++)
                        {
                            var employee = employees[employeeIndex];
                            var value = row.HoursByEmployeeId.TryGetValue(employee.EmployeeId, out var hours) ? hours : 0;
                            grandTotal += value;

                            table.Cell().Row(currentTableRow).Column((uint)(3 + employeeIndex))
                                .Element(GrandTotalValueCellStyle)
                                .AlignCenter()
                                .Padding(5)
                                .Text(value.ToString("0.00", CultureInfo.InvariantCulture));
                        }

                        table.Cell().Row(currentTableRow).Column((uint)(3 + employees.Count))
                            .Element(TotalColumnGrandTotalCellStyle)
                            .AlignCenter()
                            .Padding(5)
                            .Text(grandTotal.ToString("0.00", CultureInfo.InvariantCulture));

                        rowIndex++;
                        currentTableRow++;
                        continue;
                    }

                    // Section start: render the section cell once with RowSpan
                    if (row.ShowSectionTitle)
                    {
                        var sectionTitle = row.SectionTitle;
                        var span = 1;

                        for (var i = rowIndex + 1; i < rows.Count; i++)
                        {
                            if (rows[i].ShowSectionTitle)
                                break;
                            if (!string.Equals(rows[i].SectionTitle, sectionTitle, StringComparison.Ordinal))
                                break;
                            span++;
                        }

                        table.Cell().Row(currentTableRow).Column(1u).RowSpan((uint)span)
                            .Element(SectionCellStyle)
                            .Padding(5)
                            .Text(sectionTitle);
                    }

                    var col2Style = row.IsTotalRow
                        ? (Func<IContainer, IContainer>)TotalRowColumn2CellStyle
                        : RowColumn2CellStyle;

                    var dataStyle = row.IsTotalRow
                        ? (Func<IContainer, IContainer>)TotalRowCellStyle
                        : RowCellStyle;

                    table.Cell().Row(currentTableRow).Column(2u)
                        .Element(col2Style)
                        .Padding(5)
                        .Text(row.RowTitle);

                    var rowTotal = 0.0;
                    for (var employeeIndex = 0; employeeIndex < employees.Count; employeeIndex++)
                    {
                        var employee = employees[employeeIndex];
                        var value = row.HoursByEmployeeId.TryGetValue(employee.EmployeeId, out var hours) ? hours : 0;
                        rowTotal += value;

                        table.Cell().Row(currentTableRow).Column((uint)(3 + employeeIndex))
                            .Element(dataStyle)
                            .AlignCenter()
                            .Padding(5)
                            .Text(value.ToString("0.00", CultureInfo.InvariantCulture));
                    }

                    var totalStyle = row.IsTotalRow
                        ? (Func<IContainer, IContainer>)TotalColumnTotalRowCellStyle
                        : TotalColumnCellStyle;

                    table.Cell().Row(currentTableRow).Column((uint)(3 + employees.Count))
                        .Element(totalStyle)
                        .AlignCenter()
                        .Padding(5)
                        .Text(rowTotal.ToString("0.00", CultureInfo.InvariantCulture));

                    rowIndex++;
                    currentTableRow++;
                }

                static IContainer HeaderBlankCellStyle(IContainer c)
                    => c.ExtendHorizontal().Height(110)
                        .BorderTop(1f)
                        .BorderLeft(1f)
                        .BorderRight(1f)
                        .BorderBottom(1f)
                        .BorderColor(Colors.Black);

                static IContainer HeaderEmployeeCellStyle(IContainer c)
                    => c.ExtendHorizontal().Height(110)
                        .BorderTop(1f)
                        .BorderLeft(1f)
                        .BorderRight(1f)
                        .BorderBottom(1f)
                        .BorderColor(Colors.Black)
                        .PaddingHorizontal(2)
                        .PaddingVertical(4);

                static IContainer HeaderTotalCellStyle(IContainer c)
                    => c.ExtendHorizontal().Height(110)
                        .BorderTop(1f)
                        .BorderLeft(1f)
                        .BorderRight(1f)
                        .BorderBottom(1f)
                        .BorderColor(Colors.Black)
                        .Background(Colors.Grey.Lighten2)
                        .PaddingHorizontal(2)
                        .PaddingVertical(4);

                static IContainer RowCellStyle(IContainer c)
                    => c.ExtendHorizontal().DefaultTextStyle(x => x.FontSize(9))
                        .PaddingVertical(0)
                        .PaddingHorizontal(0)
                        .BorderTop(1f)
                        .BorderLeft(1f)
                        .BorderRight(1f)
                        .BorderBottom(1f)
                        .BorderColor(Colors.Black);

                static IContainer FirstColumnRowCellStyle(IContainer c)
                    => c.ExtendHorizontal().DefaultTextStyle(x => x.FontSize(9))
                        .PaddingVertical(0)
                        .PaddingHorizontal(0)
                        .BorderLeft(1f)
                        .BorderTop(1f)
                        .BorderRight(1f)
                        .BorderBottom(1f)
                        .BorderColor(Colors.Black);

                static IContainer RowColumn2CellStyle(IContainer c)
                    => c.ExtendHorizontal().DefaultTextStyle(x => x.FontSize(9))
                        .PaddingVertical(0)
                        .PaddingHorizontal(0)
                        .BorderTop(1f)
                        .BorderLeft(1f)
                        .BorderRight(1f)
                        .BorderBottom(1f)
                        .BorderColor(Colors.Black);

                static IContainer TotalRowCellStyle(IContainer c)
                    => RowCellStyle(c)
                        .DefaultTextStyle(x => x.FontSize(9).SemiBold())
                        .Background(Colors.Grey.Lighten3);

                static IContainer TotalRowColumn2CellStyle(IContainer c)
                    => RowColumn2CellStyle(c)
                        .DefaultTextStyle(x => x.FontSize(9).SemiBold())
                        .Background(Colors.Grey.Lighten3);

                static IContainer SectionCellStyle(IContainer c)
                    => FirstColumnRowCellStyle(c)
                        .DefaultTextStyle(x => x.FontSize(9).SemiBold())
                        .Background(Colors.Grey.Lighten3)
                        .AlignLeft()
                        .AlignMiddle();

                static IContainer GrandTotalLeftCellStyle(IContainer c)
                    => FirstColumnRowCellStyle(c)
                        .DefaultTextStyle(x => x.FontSize(10).Bold())
                        .Background(Colors.Grey.Lighten2);

                static IContainer GrandTotalValueCellStyle(IContainer c)
                    => RowCellStyle(c)
                        .DefaultTextStyle(x => x.FontSize(10).Bold())
                        .Background(Colors.Grey.Lighten2);

                static IContainer TotalColumnCellStyle(IContainer c)
                    => RowCellStyle(c)
                        .DefaultTextStyle(x => x.FontSize(9).Bold())
                        .Background(Colors.Grey.Lighten2);

                static IContainer TotalColumnTotalRowCellStyle(IContainer c)
                    => RowCellStyle(c)
                        .DefaultTextStyle(x => x.FontSize(9).Bold())
                        .Background(Colors.Grey.Lighten1);

                static IContainer TotalColumnGrandTotalCellStyle(IContainer c)
                    => RowCellStyle(c)
                        .DefaultTextStyle(x => x.FontSize(10).Bold())
                        .Background(Colors.Grey.Lighten1);
            });
        }
    }

    public class TimesheetReportDocumentData
    {
        public int WeekNumber { get; set; }
        public DateOnly WeekStartDate { get; set; }
        public List<string> SelectedClientNames { get; set; } = new();
        public List<TimesheetReportEmployee> Employees { get; set; } = new();
        public List<TimesheetReportRow> Rows { get; set; } = new();
    }

    public class TimesheetReportEmployee
    {
        public int EmployeeId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public class TimesheetReportRow
    {
        public string SectionTitle { get; set; } = string.Empty;
        public bool ShowSectionTitle { get; set; }
        public string RowTitle { get; set; } = string.Empty;
        public bool IsTotalRow { get; set; }
        public bool IsGrandTotalRow { get; set; }
        public Dictionary<int, double> HoursByEmployeeId { get; set; } = new();
    }
}
