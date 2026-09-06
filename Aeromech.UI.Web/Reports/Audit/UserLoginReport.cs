using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IDocument = AeroMech.UI.Web.Reports.IDocument;

namespace AeroMech.API.Reports
{
    /// <summary>
    /// Who signed in, who signed out, and who was turned away - in the order it happened.
    ///
    /// Grouped by day and newest first, like the audit log it is drawn from, because the report
    /// is nearly always opened with a recent question in mind: who was in the system yesterday,
    /// or whose account somebody has been guessing at. Refused attempts are printed in red so a
    /// run of them stands out of a page of routine sign-ins.
    /// </summary>
    public class UserLoginReport : IDocument
    {
        public UserLoginReportData Data { get; set; } = new();

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        private const int ColumnCount = 4;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);

                // Portrait, unlike the audit log: four columns of short values need no more width.
                page.Size(PageSizes.A4);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"User Logins  {Data.GeneratedAt:dd/MM/yyyy HH:mm}")
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

                    row.RelativeItem().Column(right =>
                    {
                        right.Item().AlignRight().Text("User Logins").Style(titleStyle);
                        right.Item().AlignRight()
                            .Text($"{Data.FromDate:dd/MM/yyyy} to {Data.ToDate:dd/MM/yyyy}").FontSize(12).SemiBold();
                    });
                });

                column.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text($"Users: {Data.UserLabel}").FontSize(9);
                        left.Item().Text($"Events: {Data.EventLabel}").FontSize(9);
                    });

                    row.ConstantItem(230).AlignRight().Column(right =>
                    {
                        right.Item().Text("Entries").FontSize(9).FontColor(Colors.Grey.Darken2);
                        right.Item().Text(Data.TotalEntries.ToString()).FontSize(16).Bold();

                        if (Data.FailedEntries > 0)
                        {
                            right.Item().AlignRight()
                                .Text($"{Data.FailedEntries} refused sign-in attempt{(Data.FailedEntries == 1 ? string.Empty : "s")}")
                                .FontSize(9).SemiBold().FontColor(Colors.Red.Darken1);
                        }

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
                    columns.ConstantColumn(42);   // Time
                    columns.ConstantColumn(120);  // User
                    columns.ConstantColumn(80);   // Event
                    columns.RelativeColumn();     // Detail
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Time");
                    header.Cell().Element(HeaderCellStyle).Text("User");
                    header.Cell().Element(HeaderCellStyle).Text("Event");
                    header.Cell().Element(HeaderCellStyle).Text("Detail");
                });

                foreach (var day in Data.Days)
                {
                    table.Cell().ColumnSpan(ColumnCount)
                        .Element(GroupCellStyle)
                        .Text($"{day.Date:dddd dd MMMM yyyy}   ({day.Lines.Count} entries)");

                    foreach (var line in day.Lines)
                    {
                        var colour = line.Failed ? Colors.Red.Darken1 : Colors.Black;

                        table.Cell().Element(BodyCellStyle).Text($"{line.OccurredAt:HH:mm}").FontColor(colour);
                        table.Cell().Element(BodyCellStyle).Text(line.UserName).FontColor(colour);
                        table.Cell().Element(BodyCellStyle).Text(line.Event).FontColor(colour);
                        table.Cell().Element(BodyCellStyle).Text(line.Description).FontColor(colour);
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
    /// Everything the user login report prints, flattened so the document has no opinion about
    /// where it came from and can be composed without touching the database.
    /// </summary>
    public class UserLoginReportData
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public string UserLabel { get; set; } = string.Empty;
        public string EventLabel { get; set; } = string.Empty;

        /// <summary>How many entries matched, which is not always how many are printed.</summary>
        public int TotalEntries { get; set; }

        /// <summary>How many of the matched entries were refused sign-in attempts.</summary>
        public int FailedEntries { get; set; }

        /// <summary>Whether more matched than the report will print.</summary>
        public bool Truncated { get; set; }

        public List<UserLoginReportDay> Days { get; set; } = new();

        public int PrintedEntries => Days.Sum(x => x.Lines.Count);
    }

    public class UserLoginReportDay
    {
        public DateOnly Date { get; set; }
        public List<UserLoginReportLine> Lines { get; set; } = new();
    }

    public class UserLoginReportLine
    {
        public DateTimeOffset OccurredAt { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Failed { get; set; }
    }
}
