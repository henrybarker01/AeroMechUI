using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using IDocument = AeroMech.UI.Web.Reports.IDocument;

namespace AeroMech.API.Reports
{
    /// <summary>
    /// What is on the shelf right now and what it is worth, grouped by the supplier the part is
    /// bought from. Supplier is the grouping because that is how the stock is bought, counted and
    /// received, so it is the figure anyone asking "what are we holding of theirs" wants.
    ///
    /// Every supplier carries its own subtotal and the report ends on a grand total, so the page
    /// answers both the detailed question and the one-number question without a second report.
    /// </summary>
    public class StockValuationReport : IDocument
    {
        public StockValuationReportData Data { get; set; } = new();

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        private const int ColumnCount = 7;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"Stock Valuation  {Data.GeneratedAt:dd/MM/yyyy HH:mm}")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);

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

                    // Aligned right rather than laid out right-to-left: a right-to-left run
                    // reorders text mixed with numbers, which a dated heading always is.
                    row.RelativeItem().Column(right =>
                    {
                        right.Item().AlignRight().Text("Stock Valuation by Supplier").Style(titleStyle);
                        right.Item().AlignRight()
                            .Text($"As at {Data.GeneratedAt:dd/MM/yyyy HH:mm}").FontSize(12).SemiBold();
                    });
                });

                column.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text($"Suppliers: {Data.SupplierLabel}").FontSize(9);
                        left.Item().Text($"Parts: {Data.TotalParts}").FontSize(9);
                        left.Item().Text($"Units on hand: {Data.TotalQuantity}").FontSize(9);
                    });

                    row.ConstantItem(220).AlignRight().Column(right =>
                    {
                        right.Item().Text("Total stock value").FontSize(9).FontColor(Colors.Grey.Darken2);
                        right.Item().Text(Data.TotalValue.ToString("C", CultureInfo.CurrentCulture))
                            .FontSize(16).Bold();
                    });
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            if (Data.Suppliers.Count == 0)
            {
                container.PaddingTop(20).AlignCenter()
                    .Text("No parts match that selection, so there is nothing to value.")
                    .FontSize(11).Italic().FontColor(Colors.Grey.Darken2);

                return;
            }

            container.PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(85);   // Part code
                    columns.RelativeColumn();     // Description
                    columns.ConstantColumn(50);   // Bin
                    columns.ConstantColumn(40);   // Warehouse
                    columns.ConstantColumn(45);   // Qty
                    columns.ConstantColumn(70);   // Unit cost
                    columns.ConstantColumn(80);   // Value
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Part Code");
                    header.Cell().Element(HeaderCellStyle).Text("Description");
                    header.Cell().Element(HeaderCellStyle).Text("Bin");
                    header.Cell().Element(HeaderCellStyle).Text("W/H");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Qty");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Unit Cost");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Value");
                });

                foreach (var supplier in Data.Suppliers)
                {
                    table.Cell().ColumnSpan(ColumnCount)
                        .Element(GroupCellStyle)
                        .Text($"Supplier: {supplier.SupplierLabel}   ({supplier.Lines.Count} parts)");

                    if (!Data.SummaryOnly)
                    {
                        foreach (var line in supplier.Lines)
                        {
                            table.Cell().Element(BodyCellStyle).Text(line.PartCode);
                            table.Cell().Element(BodyCellStyle).Text(line.PartDescription);
                            table.Cell().Element(BodyCellStyle).Text(line.Bin ?? string.Empty);
                            table.Cell().Element(BodyCellStyle).Text(line.WarehouseCode ?? string.Empty);
                            table.Cell().Element(BodyCellStyle).AlignRight().Text(line.QuantityOnHand.ToString());
                            table.Cell().Element(BodyCellStyle).AlignRight()
                                .Text(line.UnitCost.ToString("C", CultureInfo.CurrentCulture));
                            table.Cell().Element(BodyCellStyle).AlignRight()
                                .Text(line.TotalValue.ToString("C", CultureInfo.CurrentCulture));
                        }
                    }

                    table.Cell().ColumnSpan(4).Element(SubTotalCellStyle)
                        .Text($"{supplier.SupplierLabel} total").SemiBold();
                    table.Cell().Element(SubTotalCellStyle).AlignRight()
                        .Text(supplier.TotalQuantity.ToString()).SemiBold();
                    table.Cell().Element(SubTotalCellStyle).Text(string.Empty);
                    table.Cell().Element(SubTotalCellStyle).AlignRight()
                        .Text(supplier.TotalValue.ToString("C", CultureInfo.CurrentCulture)).SemiBold();
                }

                table.Cell().ColumnSpan(4).Element(GrandTotalCellStyle).Text("Total stock value").Bold();
                table.Cell().Element(GrandTotalCellStyle).AlignRight().Text(Data.TotalQuantity.ToString()).Bold();
                table.Cell().Element(GrandTotalCellStyle).Text(string.Empty);
                table.Cell().Element(GrandTotalCellStyle).AlignRight()
                    .Text(Data.TotalValue.ToString("C", CultureInfo.CurrentCulture)).Bold();
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
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(3)
                    .PaddingHorizontal(3);

            static IContainer SubTotalCellStyle(IContainer container)
                => container
                    .DefaultTextStyle(x => x.FontSize(9))
                    .Background(Colors.Grey.Lighten4)
                    .BorderTop(1)
                    .BorderColor(Colors.Grey.Darken1)
                    .PaddingVertical(4)
                    .PaddingHorizontal(3);

            static IContainer GrandTotalCellStyle(IContainer container)
                => container
                    .DefaultTextStyle(x => x.FontSize(10))
                    .Background(Colors.Grey.Lighten2)
                    .BorderTop(2)
                    .BorderColor(Colors.Black)
                    .PaddingVertical(6)
                    .PaddingHorizontal(3);
        }
    }

    /// <summary>
    /// Everything the valuation prints, flattened so the document has no opinion about where it
    /// came from and can be composed without touching the database.
    /// </summary>
    public class StockValuationReportData
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public string SupplierLabel { get; set; } = string.Empty;

        /// <summary>
        /// Whether the part lines are dropped and only the supplier subtotals printed.
        /// </summary>
        public bool SummaryOnly { get; set; }

        public List<StockValuationReportSupplier> Suppliers { get; set; } = new();

        public int TotalParts => Suppliers.Sum(x => x.Lines.Count);
        public int TotalQuantity => Suppliers.Sum(x => x.TotalQuantity);
        public double TotalValue => Suppliers.Sum(x => x.TotalValue);
    }

    public class StockValuationReportSupplier
    {
        public string? SupplierCode { get; set; }

        public string SupplierLabel => string.IsNullOrWhiteSpace(SupplierCode) ? "No supplier" : SupplierCode!;

        public List<StockValuationReportLine> Lines { get; set; } = new();

        public int TotalQuantity => Lines.Sum(x => x.QuantityOnHand);
        public double TotalValue => Lines.Sum(x => x.TotalValue);
    }

    public class StockValuationReportLine
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartDescription { get; set; } = string.Empty;
        public string? Bin { get; set; }
        public string? WarehouseCode { get; set; }
        public int QuantityOnHand { get; set; }
        public double UnitCost { get; set; }

        public double TotalValue => QuantityOnHand * UnitCost;
    }
}
