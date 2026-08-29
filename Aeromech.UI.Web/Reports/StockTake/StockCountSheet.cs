using AeroMech.Data.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IDocument = AeroMech.UI.Web.Reports.IDocument;

namespace AeroMech.API.Reports
{
    /// <summary>
    /// The sheet that goes out to the shelves. It is a working document rather than a report: the
    /// quantity column is deliberately empty, ruled, and wide enough to write a figure into by
    /// hand, because the whole purpose of printing it is to come back with numbers on it.
    ///
    /// Where the sheet is blind the expected quantity is not printed at all. A counter who can see
    /// what the system expects tends to confirm it rather than count, and a stock take that only
    /// ever agrees with the system has proved nothing.
    /// </summary>
    public class StockCountSheet : IDocument
    {
        public StockCountSheetData Data { get; set; } = new();

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        /// <summary>
        /// How many columns the table has, which the group bands span. Varies because the expected
        /// quantity column is dropped entirely on a blind sheet.
        /// </summary>
        private int ColumnCount => Data.ShowExpectedQuantity ? 6 : 5;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);

                // Portrait: a count sheet wants as many rows per page as it can get, and it has few
                // enough columns that the extra width of landscape would only pad them out.
                page.Size(PageSizes.A4);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text(Data.Reference).FontSize(8).FontColor(Colors.Grey.Darken1);

                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Grey.Darken1));
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
        }

        private void ComposeHeader(IContainer container)
        {
            var titleStyle = TextStyle.Default.FontSize(18).Bold().FontColor(Colors.Black);

            container.PaddingBottom(8).Column(column =>
            {
                column.Item().Row(row =>
                {
                    var path = Path.Combine(AppContext.BaseDirectory, "Reports", "Images", "AreoMechSmall.png");
                    row.ConstantItem(150).Image(path);

                    row.RelativeItem().ContentFromRightToLeft().Column(right =>
                    {
                        right.Item().Text("Stock Count Sheet").Style(titleStyle);
                        right.Item().Text(Data.Reference).FontSize(12).SemiBold();
                    });
                });

                column.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text($"Description: {Data.Description}").FontSize(9).SemiBold();
                        left.Item().Text($"Date: {Data.StockTakeDate:dd/MM/yyyy}").FontSize(9);
                        left.Item().Text($"Suppliers: {Data.SupplierLabel}").FontSize(9);
                        left.Item().Text($"Order: {(Data.Order == StockTakeSheetOrder.BinThenPart ? "Bin, then part number" : "Supplier code, then part number")}").FontSize(9);
                    });

                    // Signed and dated on the paper itself. A sheet that comes back without a name
                    // on it cannot be questioned later, and questioning a count is the point.
                    row.ConstantItem(200).Column(right =>
                    {
                        right.Item().Text("Counted by: ______________________").FontSize(9);
                        right.Item().PaddingTop(6).Text("Date: ___________________________").FontSize(9);
                        right.Item().PaddingTop(6).Text($"Parts to count: {Data.Lines.Count}").FontSize(9).SemiBold();
                    });
                });

                if (!Data.ShowExpectedQuantity)
                {
                    column.Item().PaddingTop(6).Text("Blind count - the expected quantity is deliberately not shown. Count what is on the shelf.")
                        .FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
                }
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(95);   // Part code
                    columns.RelativeColumn();     // Description
                    columns.ConstantColumn(55);   // Bin
                    columns.ConstantColumn(45);   // Warehouse

                    if (Data.ShowExpectedQuantity)
                        columns.ConstantColumn(50);

                    columns.ConstantColumn(70);   // The box the count is written into
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Part Code");
                    header.Cell().Element(HeaderCellStyle).Text("Description");
                    header.Cell().Element(HeaderCellStyle).Text("Bin");
                    header.Cell().Element(HeaderCellStyle).Text("W/H");

                    if (Data.ShowExpectedQuantity)
                        header.Cell().Element(HeaderCellStyle).AlignRight().Text("On Hand");

                    header.Cell().Element(HeaderCellStyle).AlignCenter().Text("Count");
                });

                string? currentGroup = null;

                foreach (var line in Data.Lines)
                {
                    var group = Data.Order == StockTakeSheetOrder.BinThenPart
                        ? (string.IsNullOrWhiteSpace(line.Bin) ? "(No bin)" : line.Bin!)
                        : (string.IsNullOrWhiteSpace(line.SupplierCode) ? "(No supplier)" : line.SupplierCode!);

                    // A band whenever the grouping changes, so a counter who picks the sheet back up
                    // mid-page can see which supplier or bin they are standing in.
                    if (!string.Equals(currentGroup, group, StringComparison.OrdinalIgnoreCase))
                    {
                        currentGroup = group;

                        var label = Data.Order == StockTakeSheetOrder.BinThenPart ? "Bin" : "Supplier";

                        table.Cell().ColumnSpan((uint)ColumnCount)
                            .Element(GroupCellStyle)
                            .Text($"{label}: {group}");
                    }

                    table.Cell().Element(BodyCellStyle).Text(line.PartCode);
                    table.Cell().Element(BodyCellStyle).Text(line.PartDescription);
                    table.Cell().Element(BodyCellStyle).Text(line.Bin ?? string.Empty);
                    table.Cell().Element(BodyCellStyle).Text(line.WarehouseCode ?? string.Empty);

                    if (Data.ShowExpectedQuantity)
                        table.Cell().Element(BodyCellStyle).AlignRight().Text(line.QuantityOnHand.ToString());

                    // Left empty on purpose. The border makes it obvious where to write, and the
                    // height gives a pen somewhere to land.
                    table.Cell().Element(CountBoxStyle).Text(string.Empty);
                }
            });

            static IContainer HeaderCellStyle(IContainer container)
                => container
                    .DefaultTextStyle(x => x.FontSize(9).Bold())
                    .Background(Colors.Grey.Lighten3)
                    .BorderBottom(1)
                    .BorderColor(Colors.Black)
                    .PaddingVertical(4)
                    .PaddingHorizontal(3);

            static IContainer GroupCellStyle(IContainer container)
                => container
                    .DefaultTextStyle(x => x.FontSize(9).Bold())
                    .Background(Colors.Grey.Lighten2)
                    .PaddingVertical(4)
                    .PaddingHorizontal(3);

            static IContainer BodyCellStyle(IContainer container)
                => container
                    .DefaultTextStyle(x => x.FontSize(9))
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten1)
                    .PaddingVertical(5)
                    .PaddingHorizontal(3)
                    .AlignMiddle();

            static IContainer CountBoxStyle(IContainer container)
                => container
                    .Border(1)
                    .BorderColor(Colors.Grey.Darken1)
                    .MinHeight(22)
                    .PaddingVertical(5)
                    .PaddingHorizontal(3);
        }
    }

    /// <summary>
    /// Everything the sheet prints, flattened so the document has no opinion about where it came
    /// from and can be composed without touching the database.
    /// </summary>
    public class StockCountSheetData
    {
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset StockTakeDate { get; set; }
        public string SupplierLabel { get; set; } = string.Empty;
        public StockTakeSheetOrder Order { get; set; }

        /// <summary>
        /// Whether the expected quantity column is printed. Off for a blind count.
        /// </summary>
        public bool ShowExpectedQuantity { get; set; }

        public List<StockCountSheetLine> Lines { get; set; } = new();
    }

    public class StockCountSheetLine
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartDescription { get; set; } = string.Empty;
        public string? Bin { get; set; }
        public string? SupplierCode { get; set; }
        public string? WarehouseCode { get; set; }
        public int QuantityOnHand { get; set; }
    }
}
