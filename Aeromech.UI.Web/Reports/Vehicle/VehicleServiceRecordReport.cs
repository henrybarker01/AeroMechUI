using AeroMech.Models.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IDocument = AeroMech.UI.Web.Reports.IDocument;

namespace AeroMech.API.Reports
{
    /// <summary>
    /// Everything that has ever been done to a machine, in the order it happened.
    ///
    /// The same document answers two questions depending on how it is grouped. Under a machine it
    /// is the record that goes with the machine - the thing asked for at a handover, a warranty
    /// claim or a sale. Under a machine type it is the same history read across a fleet, which is
    /// where a fault that keeps coming back on one model becomes visible.
    ///
    /// A machine with nothing against it prints its heading and says so. Silence would read as
    /// "no record kept" rather than "nothing done", and those are not the same answer.
    /// </summary>
    public class VehicleServiceRecordReport : IDocument
    {
        public VehicleServiceRecordReportData Data { get; set; } = new();

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public DocumentSettings GetSettings() => DocumentSettings.Default;

        /// <summary>
        /// Grouping by type has to name the machine on every line, because the heading no longer
        /// does. Grouping by machine already named it, so that column would repeat one value all
        /// the way down the page.
        /// </summary>
        private bool ShowMachineColumn => Data.Grouping == VehicleServiceRecordGrouping.ByMachineType;

        private int ColumnCount => ShowMachineColumn ? 8 : 7;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4.Landscape());

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text($"{Data.Title}  {Data.GeneratedAt:dd/MM/yyyy HH:mm}")
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
                        right.Item().AlignRight().Text(Data.Title).Style(titleStyle);
                        right.Item().AlignRight().Text(Data.PeriodLabel).FontSize(12).SemiBold();
                    });
                });

                column.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text($"Scope: {Data.ScopeLabel}").FontSize(9);
                        left.Item().Text($"Machines: {Data.TotalVehicles}").FontSize(9);
                    });

                    row.ConstantItem(220).AlignRight().Column(right =>
                    {
                        right.Item().Text("Services recorded").FontSize(9).FontColor(Colors.Grey.Darken2);
                        right.Item().Text(Data.TotalServices.ToString()).FontSize(16).Bold();
                        right.Item().Text($"Labour hours: {Data.TotalLabourHours:0.##}").FontSize(9);
                    });
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            if (Data.Groups.Count == 0)
            {
                container.PaddingTop(20).AlignCenter()
                    .Text("No machines match that selection, so there is no service record to print.")
                    .FontSize(11).Italic().FontColor(Colors.Grey.Darken2);

                return;
            }

            container.PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(60);   // Date
                    columns.ConstantColumn(55);   // Report number

                    if (ShowMachineColumn)
                        columns.ConstantColumn(120);  // Machine

                    columns.ConstantColumn(65);   // Job number
                    columns.ConstantColumn(65);   // Service type
                    columns.ConstantColumn(45);   // Machine hours
                    columns.RelativeColumn();     // Work done
                    columns.ConstantColumn(45);   // Labour hours
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellStyle).Text("Date");
                    header.Cell().Element(HeaderCellStyle).Text("Report");

                    if (ShowMachineColumn)
                        header.Cell().Element(HeaderCellStyle).Text("Machine");

                    header.Cell().Element(HeaderCellStyle).Text("Job No");
                    header.Cell().Element(HeaderCellStyle).Text("Type");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Hours");
                    header.Cell().Element(HeaderCellStyle).Text("Work Done");
                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Labour");
                });

                foreach (var group in Data.Groups)
                {
                    table.Cell().ColumnSpan((uint)ColumnCount)
                        .Element(GroupCellStyle)
                        .Text(group.Heading);

                    if (group.Lines.Count == 0)
                    {
                        table.Cell().ColumnSpan((uint)ColumnCount)
                            .Element(BodyCellStyle)
                            .Text(ShowMachineColumn
                                ? "No services recorded against any machine of this type."
                                : "No services recorded against this machine.")
                            .Italic().FontColor(Colors.Grey.Darken1);

                        continue;
                    }

                    foreach (var line in group.Lines)
                    {
                        table.Cell().Element(BodyCellStyle).Text($"{line.ReportDate:dd/MM/yyyy}");
                        table.Cell().Element(BodyCellStyle).Text($"AEM{line.ServiceReportNumber}");

                        if (ShowMachineColumn)
                            table.Cell().Element(BodyCellStyle).Text(line.MachineLabel);

                        table.Cell().Element(BodyCellStyle).Text(line.JobNumber ?? string.Empty);
                        table.Cell().Element(BodyCellStyle).Text(line.ServiceType ?? string.Empty);
                        table.Cell().Element(BodyCellStyle).AlignRight()
                            .Text(line.MachineHours?.ToString() ?? string.Empty);
                        table.Cell().Element(BodyCellStyle).Text(line.WorkDone);
                        table.Cell().Element(BodyCellStyle).AlignRight().Text($"{line.LabourHours:0.##}");

                        if (Data.IncludeParts && line.Parts.Count > 0)
                        {
                            table.Cell().ColumnSpan((uint)ColumnCount)
                                .Element(PartsCellStyle)
                                .Text($"Parts fitted: {string.Join(", ", line.Parts.Select(x => x.Label))}");
                        }
                    }

                    table.Cell().ColumnSpan((uint)(ColumnCount - 1)).Element(SubTotalCellStyle)
                        .Text($"{group.Heading} - {group.Lines.Count} service(s)").SemiBold();
                    table.Cell().Element(SubTotalCellStyle).AlignRight()
                        .Text($"{group.TotalLabourHours:0.##}").SemiBold();
                }

                table.Cell().ColumnSpan((uint)(ColumnCount - 1)).Element(GrandTotalCellStyle)
                    .Text($"{Data.TotalServices} service(s) across {Data.TotalVehicles} machine(s)").Bold();
                table.Cell().Element(GrandTotalCellStyle).AlignRight()
                    .Text($"{Data.TotalLabourHours:0.##}").Bold();
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

            static IContainer PartsCellStyle(IContainer container)
                => container
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken2))
                    .BorderBottom(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .PaddingBottom(3)
                    .PaddingLeft(12)
                    .PaddingRight(3);

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
    /// Everything the service record prints, flattened so the document has no opinion about where
    /// it came from and can be composed without touching the database.
    /// </summary>
    public class VehicleServiceRecordReportData
    {
        public DateTimeOffset GeneratedAt { get; set; }

        public VehicleServiceRecordGrouping Grouping { get; set; }

        public string Title => Grouping == VehicleServiceRecordGrouping.ByMachineType
            ? "Service Record by Machine Type"
            : "Vehicle Service Record";

        /// <summary>
        /// The period covered, written the way it was asked for. An open-ended request says so
        /// rather than printing a made-up boundary date.
        /// </summary>
        public string PeriodLabel { get; set; } = string.Empty;

        public string ScopeLabel { get; set; } = string.Empty;

        public bool IncludeParts { get; set; }

        public List<VehicleServiceRecordGroup> Groups { get; set; } = new();

        /// <summary>
        /// Counted by the service rather than off the groups, because a machine type heading
        /// covers many machines and a machine heading covers one.
        /// </summary>
        public int TotalVehicles { get; set; }

        public int TotalServices => Groups.Sum(x => x.Lines.Count);

        public double TotalLabourHours => Groups.Sum(x => x.TotalLabourHours);
    }

    public class VehicleServiceRecordGroup
    {
        public string Heading { get; set; } = string.Empty;

        public List<VehicleServiceRecordLine> Lines { get; set; } = new();

        public double TotalLabourHours => Lines.Sum(x => x.LabourHours);
    }

    public class VehicleServiceRecordLine
    {
        public DateTimeOffset ReportDate { get; set; }
        public int ServiceReportNumber { get; set; }
        public string MachineLabel { get; set; } = string.Empty;
        public string? JobNumber { get; set; }
        public string? ServiceType { get; set; }
        public int? MachineHours { get; set; }
        public string WorkDone { get; set; } = string.Empty;
        public double LabourHours { get; set; }

        public List<VehicleServiceRecordPart> Parts { get; set; } = new();
    }

    public class VehicleServiceRecordPart
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }

        public string Label => string.IsNullOrWhiteSpace(PartDescription)
            ? $"{PartCode} x{Quantity}"
            : $"{PartCode} {PartDescription} x{Quantity}";
    }
}
