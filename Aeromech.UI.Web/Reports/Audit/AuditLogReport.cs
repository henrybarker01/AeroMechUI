using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IDocument = AeroMech.UI.Web.Reports.IDocument;

namespace AeroMech.API.Reports
{
    /// <summary>
    /// Who did what, in the order it happened.
    ///
    /// Grouped by day and newest first, because the log is nearly always opened with a recent
    /// event in mind - a level that looks wrong, a price nobody recognises - and the reader works
    /// backwards from it. The value before a change and the value after it are printed side by
    /// side rather than folded into the description, so a column of them can be scanned down.
    /// </summary>
    public class AuditLogReport : IDocument
    {
        public AuditLogReportData Data { get; set; } = new();

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        private const int ColumnCount = 7;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);

                // Landscape, because an entry only reads as a sentence when the description, the
                // old value and the new value all fit on one line.
                page.Size(PageSizes.A4.Landscape());

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"Audit Log  {Data.GeneratedAt:dd/MM/yyyy HH:mm}")
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
                        right.Item().AlignRight().Text("Audit Log").Style(titleStyle);
                        right.Item().AlignRight()
                            .Text($"{Data.FromDate:dd/MM/yyyy} to {Data.ToDate:dd/MM/yyyy}").FontSize(12).SemiBold();
                    });
                });

                column.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text($"Users: {Data.UserLabel}").FontSize(9);
                        left.Item().Text($"Activity: {Data.AreaLabel}").FontSize(9);

                        if (!string.IsNullOrWhiteSpace(Data.SearchTerm))
                            left.Item().Text($"Matching: {Data.SearchTerm}").FontSize(9);
                    });

                    row.ConstantItem(230).AlignRight().Column(right =>
                    {
                        right.Item().Text("Entries").FontSize(9).FontColor(Colors.Grey.Darken2);
                        right.Item().Text(Data.TotalEntries.ToString()).FontSize(16).Bold();

                        // Said on the page rather than left to be inferred from the page count: a
                        // report that quietly stopped short would be read as a complete answer.
                        if (Data.Truncated)
                        {
                            right.Item().AlignRight()
                                .Text($"Showing the {Data.PrintedEntries} most recent - narrow the period or the filters to see the rest.")
                                .FontSize(8).Italic().FontColor(Colors.Red.Darken1);
                        }
                    });
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            if (Data.Days.Count == 0)
            {
                container.PaddingTop(20).AlignCenter()
                    .Text("Nothing was recorded against that selection.")
                    .FontSize(11).Italic().FontColor(Colors.Grey.Darken2);

                return;
            }

            container.PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(38);   // Time
                    columns.ConstantColumn(95);   // User
                    columns.ConstantColumn(80);   // Activity
                    columns.ConstantColumn(70);   // Action
                    columns.ConstantColumn(80);   // Reference
                    columns.RelativeColumn();     // What happened
                    columns.ConstantColumn(130);  // Was / now
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Time");
                    header.Cell().Element(HeaderCellStyle).Text("User");
                    header.Cell().Element(HeaderCellStyle).Text("Activity");
                    header.Cell().Element(HeaderCellStyle).Text("Action");
                    header.Cell().Element(HeaderCellStyle).Text("Reference");
                    header.Cell().Element(HeaderCellStyle).Text("What happened");
                    header.Cell().Element(HeaderCellStyle).Text("Was / now");
                });

                foreach (var day in Data.Days)
                {
                    table.Cell().ColumnSpan(ColumnCount)
                        .Element(GroupCellStyle)
                        .Text($"{day.Date:dddd dd MMMM yyyy}   ({day.Lines.Count} entries)");

                    foreach (var line in day.Lines)
                    {
                        table.Cell().Element(BodyCellStyle).Text($"{line.OccurredAt:HH:mm}");
                        table.Cell().Element(BodyCellStyle).Text(line.UserName);
                        table.Cell().Element(BodyCellStyle).Text(line.Area);
                        table.Cell().Element(BodyCellStyle).Text(line.Action);
                        table.Cell().Element(BodyCellStyle).Text(line.Reference ?? string.Empty);
                        table.Cell().Element(BodyCellStyle).Text(line.Description);
                        table.Cell().Element(BodyCellStyle).Text(line.ValueChange);
                    }
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
                    .DefaultTextStyle(x => x.FontSize(8))
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingVertical(3)
                    .PaddingHorizontal(3);
        }
    }

    /// <summary>
    /// Everything the audit log report prints, flattened so the document has no opinion about
    /// where it came from and can be composed without touching the database.
    /// </summary>
    public class AuditLogReportData
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public string UserLabel { get; set; } = string.Empty;
        public string AreaLabel { get; set; } = string.Empty;
        public string? SearchTerm { get; set; }

        /// <summary>How many entries matched, which is not always how many are printed.</summary>
        public int TotalEntries { get; set; }

        /// <summary>Whether more matched than the report will print.</summary>
        public bool Truncated { get; set; }

        public List<AuditLogReportDay> Days { get; set; } = new();

        public int PrintedEntries => Days.Sum(x => x.Lines.Count);
    }

    public class AuditLogReportDay
    {
        public DateOnly Date { get; set; }
        public List<AuditLogReportLine> Lines { get; set; } = new();
    }

    public class AuditLogReportLine
    {
        public DateTimeOffset OccurredAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Field { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        /// <summary>
        /// The two halves of a change in one cell. An entry that moved no single value - a receipt
        /// posted, a sheet cancelled - has nothing to print here, and prints nothing rather than an
        /// arrow between two blanks.
        /// </summary>
        public string ValueChange
        {
            get
            {
                if (string.IsNullOrWhiteSpace(OldValue) && string.IsNullOrWhiteSpace(NewValue))
                    return string.Empty;

                var was = string.IsNullOrWhiteSpace(OldValue) ? "-" : OldValue;
                var now = string.IsNullOrWhiteSpace(NewValue) ? "-" : NewValue;

                return $"{was} \u2192 {now}";
            }
        }
    }
}
