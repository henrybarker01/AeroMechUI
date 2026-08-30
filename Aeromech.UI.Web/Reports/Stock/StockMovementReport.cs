using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IDocument = AeroMech.UI.Web.Reports.IDocument;

namespace AeroMech.API.Reports
{
    /// <summary>
    /// Where a part's stock went over a period. Every part is printed as a small statement: the
    /// level it opened the period at, each movement that touched it in date order with the balance
    /// carried down the page, and the level it closed at.
    ///
    /// Opening and closing are real figures rather than estimates. The ledger records every
    /// movement, so the level on any past date is today's level with the movements since then
    /// unwound - which is what lets a period ending last month still be reported in full.
    ///
    /// Portrait rather than landscape: the detail columns are narrow, and a report that lists
    /// movements one to a line wants rows on the page more than it wants width.
    /// </summary>
    public class StockMovementReport : IDocument
    {
        public StockMovementReportData Data { get; set; } = new();

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        private const int ColumnCount = 6;

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
                    row.RelativeItem().Text($"Stock Movement  {Data.FromDate:dd/MM/yyyy} - {Data.ToDate:dd/MM/yyyy}")
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

                    // Aligned right rather than laid out right-to-left. The two read the same for
                    // a single value, but a right-to-left run reorders "1 July to 31 July" into
                    // nonsense - the direction of the text is not what wants changing here.
                    row.RelativeItem().Column(right =>
                    {
                        right.Item().AlignRight().Text("Stock Movement Report").Style(titleStyle);
                        right.Item().AlignRight()
                            .Text($"{Data.FromDate:dd/MM/yyyy} to {Data.ToDate:dd/MM/yyyy}").FontSize(12).SemiBold();
                    });
                });

                column.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text($"Suppliers: {Data.SupplierLabel}").FontSize(9);
                        left.Item().Text($"Parts listed: {Data.Parts.Count}").FontSize(9);
                        left.Item().Text($"Movements: {Data.TotalMovements}").FontSize(9);
                    });

                    row.ConstantItem(200).Column(right =>
                    {
                        right.Item().Text($"Units in: {Data.TotalUnitsIn}").FontSize(9);
                        right.Item().Text($"Units out: {Data.TotalUnitsOut}").FontSize(9);
                        right.Item().Text($"Net movement: {Data.TotalNetMovement:+#;-#;0}").FontSize(9).SemiBold();
                    });
                });

                if (Data.ToDate < Data.GeneratedOn)
                {
                    column.Item().PaddingTop(6)
                        .Text("The period ends in the past. Opening and closing are the levels as they stood on those dates, worked back from the movements recorded since.")
                        .FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
                }
            });
        }

        private void ComposeContent(IContainer container)
        {
            if (Data.Parts.Count == 0)
            {
                container.PaddingTop(20).AlignCenter()
                    .Text("Nothing moved in this period for the selection made.")
                    .FontSize(11).Italic().FontColor(Colors.Grey.Darken2);

                return;
            }

            container.PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(65);   // Date
                    columns.RelativeColumn();     // Movement
                    columns.ConstantColumn(95);   // Reference
                    columns.ConstantColumn(45);   // In
                    columns.ConstantColumn(45);   // Out
                    columns.ConstantColumn(60);   // Balance
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Date");
                    header.Cell().Element(HeaderCellStyle).Text("Movement");
                    header.Cell().Element(HeaderCellStyle).Text("Reference");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("In");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Out");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Balance");
                });

                foreach (var part in Data.Parts)
                {
                    // The part this statement belongs to, banded so a page picked up in the middle
                    // still says which part the balances beneath it are running for.
                    table.Cell().ColumnSpan(ColumnCount)
                        .Element(GroupCellStyle)
                        .Text(text =>
                        {
                            text.Span($"{part.PartCode}  ").FontSize(9).Bold();
                            text.Span(part.PartDescription).FontSize(9);
                            text.Span($"   [{part.SupplierLabel}{(string.IsNullOrWhiteSpace(part.Bin) ? string.Empty : $" / bin {part.Bin}")}]")
                                .FontSize(8).FontColor(Colors.Grey.Darken2);
                        });

                    table.Cell().Element(BoundaryCellStyle).Text($"{Data.FromDate:dd/MM/yyyy}").FontSize(8);
                    table.Cell().Element(BoundaryCellStyle).Text("Opening balance").SemiBold();
                    table.Cell().Element(BoundaryCellStyle).Text(string.Empty);
                    table.Cell().Element(BoundaryCellStyle).Text(string.Empty);
                    table.Cell().Element(BoundaryCellStyle).Text(string.Empty);
                    table.Cell().Element(BoundaryCellStyle).AlignRight().Text(part.OpeningQuantity.ToString()).SemiBold();

                    if (part.Movements.Count == 0)
                    {
                        table.Cell().ColumnSpan(ColumnCount).Element(BodyCellStyle)
                            .Text("No movement in this period.")
                            .FontSize(8).Italic().FontColor(Colors.Grey.Darken2);
                    }

                    foreach (var movement in part.Movements)
                    {
                        table.Cell().Element(BodyCellStyle).Text($"{movement.MovementDate:dd/MM/yyyy}");
                        table.Cell().Element(BodyCellStyle).Text(movement.MovementType);
                        table.Cell().Element(BodyCellStyle).Text(movement.Reference ?? string.Empty);

                        // Split rather than signed: a receipt and an issue read as different
                        // columns on paper, which is how a stock ledger is read.
                        table.Cell().Element(BodyCellStyle).AlignRight()
                            .Text(movement.Quantity > 0 ? movement.Quantity.ToString() : string.Empty);
                        table.Cell().Element(BodyCellStyle).AlignRight()
                            .Text(movement.Quantity < 0 ? (-movement.Quantity).ToString() : string.Empty);
                        table.Cell().Element(BodyCellStyle).AlignRight().Text(movement.Balance.ToString());
                    }

                    table.Cell().Element(ClosingCellStyle).Text($"{Data.ToDate:dd/MM/yyyy}").FontSize(8);
                    table.Cell().Element(ClosingCellStyle).Text("Closing balance").SemiBold();
                    table.Cell().Element(ClosingCellStyle).Text(string.Empty);
                    table.Cell().Element(ClosingCellStyle).AlignRight()
                        .Text(part.UnitsIn > 0 ? part.UnitsIn.ToString() : string.Empty).SemiBold();
                    table.Cell().Element(ClosingCellStyle).AlignRight()
                        .Text(part.UnitsOut > 0 ? part.UnitsOut.ToString() : string.Empty).SemiBold();
                    table.Cell().Element(ClosingCellStyle).AlignRight()
                        .Text(part.ClosingQuantity.ToString()).SemiBold();
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
                    .Background(Colors.Grey.Lighten2)
                    .PaddingTop(6)
                    .PaddingBottom(4)
                    .PaddingHorizontal(3);

            static IContainer BoundaryCellStyle(IContainer container)
                => container
                    .DefaultTextStyle(x => x.FontSize(9))
                    .Background(Colors.Grey.Lighten4)
                    .PaddingVertical(3)
                    .PaddingHorizontal(3);

            static IContainer BodyCellStyle(IContainer container)
                => container
                    .DefaultTextStyle(x => x.FontSize(9))
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(3)
                    .PaddingHorizontal(3);

            static IContainer ClosingCellStyle(IContainer container)
                => container
                    .DefaultTextStyle(x => x.FontSize(9))
                    .Background(Colors.Grey.Lighten4)
                    .BorderTop(1)
                    .BorderColor(Colors.Grey.Darken1)
                    .PaddingVertical(3)
                    .PaddingHorizontal(3);
        }
    }

    /// <summary>
    /// Everything the movement report prints, flattened so the document has no opinion about where
    /// it came from and can be composed without touching the database.
    /// </summary>
    public class StockMovementReportData
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }

        /// <summary>
        /// Today, so the report can tell a period that has already closed from one still running
        /// and say so on the page.
        /// </summary>
        public DateOnly GeneratedOn { get; set; }

        public string SupplierLabel { get; set; } = string.Empty;

        public List<StockMovementReportPart> Parts { get; set; } = new();

        public int TotalMovements => Parts.Sum(x => x.Movements.Count);
        public int TotalUnitsIn => Parts.Sum(x => x.UnitsIn);
        public int TotalUnitsOut => Parts.Sum(x => x.UnitsOut);
        public int TotalNetMovement => TotalUnitsIn - TotalUnitsOut;
    }

    public class StockMovementReportPart
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartDescription { get; set; } = string.Empty;
        public string? Bin { get; set; }
        public string? SupplierCode { get; set; }

        public string SupplierLabel => string.IsNullOrWhiteSpace(SupplierCode) ? "No supplier" : SupplierCode!;

        public int OpeningQuantity { get; set; }
        public int ClosingQuantity { get; set; }

        public List<StockMovementReportLine> Movements { get; set; } = new();

        public int UnitsIn => Movements.Where(x => x.Quantity > 0).Sum(x => x.Quantity);
        public int UnitsOut => Movements.Where(x => x.Quantity < 0).Sum(x => -x.Quantity);
    }

    public class StockMovementReportLine
    {
        public DateTimeOffset MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;

        /// <summary>
        /// The document the movement can be walked back to - a supplier invoice or a stock take
        /// sheet. Blank where the ledger holds no pointer, which is the case for the movements a
        /// service report causes.
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// Signed: positive brought stock in, negative took it out.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// The level after this movement, carried down from the opening balance.
        /// </summary>
        public int Balance { get; set; }
    }
}
