using AeroMech.API.Reports;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AeroMech.UI.Web.Services
{
    /// <summary>
    /// Reporting on stock, as opposed to moving it. Nothing here writes.
    ///
    /// The whole of this service rests on one property of the system: every path that changes
    /// <see cref="Part.QtyOnHand"/> also writes a <see cref="StockAdjustment"/>. Because the ledger
    /// is complete, a level on any past date is simply today's level with the movements since then
    /// unwound - so a period that closed last month can still be reported with a real opening and
    /// a real closing quantity, not merely a list of what moved.
    ///
    ///     opening at F = QtyOnHand today - sum of movements dated on or after F
    ///     closing at T = QtyOnHand today - sum of movements dated after T
    ///
    /// which leaves opening + movements in the period == closing, by construction rather than by
    /// a separate calculation that could drift.
    /// </summary>
    public class StockReportService
    {
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly PartsService _partsService;
        private readonly StockMovementReport _movementReport;
        private readonly StockValuationReport _valuationReport;

        public StockReportService(
            IDbContextFactory<AeroMechDBContext> contextFactory,
            PartsService partsService,
            StockMovementReport movementReport,
            StockValuationReport valuationReport)
        {
            _contextFactory = contextFactory;
            _partsService = partsService;
            _movementReport = movementReport;
            _valuationReport = valuationReport;
        }

        public Task<List<SupplierOptionModel>> GetSuppliers() => _partsService.GetSupplierOptions();

        /// <summary>
        /// The parts screen reads a part's cost from its first price row, so that is the row a
        /// valuation prices stock at. Kept in step with <c>StockTakeService.CurrentCostPrice</c>
        /// deliberately: a count sheet and a valuation disagreeing about what a part costs would
        /// be worse than either being wrong on its own.
        /// </summary>
        private static double CurrentCostPrice(Part part)
            => part.Prices?.OrderBy(x => x.Id).FirstOrDefault()?.CostPrice ?? 0;

        /// <summary>
        /// Dates come off a date picker with no time on them. A period is read as whole calendar
        /// days in UTC, matching how movements are stamped, so a movement made late in the day on
        /// the closing date still falls inside the period.
        /// </summary>
        private static (DateTimeOffset FromStart, DateTimeOffset ToEndExclusive) PeriodBounds(DateOnly from, DateOnly to)
            => (new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

        private static string DescribeSuppliers(IReadOnlyCollection<string> supplierCodes)
            => supplierCodes.Count == 0 ? "All suppliers"
                : supplierCodes.Count <= 6 ? string.Join(", ", supplierCodes)
                : $"{supplierCodes.Count} suppliers";

        private static List<string> NormalizeSuppliers(IEnumerable<string> supplierCodes)
            => supplierCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>
        /// How a movement reads on paper. The enum names the code path that caused it; these name
        /// what actually happened to the stock, which is what somebody reading a ledger wants.
        /// </summary>
        private static string DescribeMovement(StockAdjustmentType type) => type switch
        {
            StockAdjustmentType.ServiceReport => "Service report - issued",
            StockAdjustmentType.ServiceReportReversal => "Service report - reversed",
            StockAdjustmentType.ServiceReportEdit => "Service report - edited",
            StockAdjustmentType.StockTake => "Stock take",
            StockAdjustmentType.StockAdjustment => "Manual adjustment",
            StockAdjustmentType.StockReceipt => "Stock receipt",
            _ => "Adjustment"
        };

        /// <summary>
        /// A part's stock statement over a period: the level it opened at, every movement that
        /// touched it with the balance carried down, and the level it closed at.
        /// </summary>
        public async Task<byte[]> GenerateStockMovementReport(StockMovementReportRequestModel request)
        {
            if (request.ToDate < request.FromDate)
                throw new InvalidOperationException("The end of the period cannot fall before its start.");

            var supplierCodes = NormalizeSuppliers(request.SupplierCodes);
            var (fromStart, toEndExclusive) = PeriodBounds(request.FromDate, request.ToDate);

            using var context = await _contextFactory.CreateDbContextAsync();

            var partsQuery = context.Parts
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            // No supplier chosen means everything, and that has to include parts carrying no
            // supplier code at all - they move the same way as the rest of the shelf.
            if (supplierCodes.Count > 0)
                partsQuery = partsQuery.Where(x => x.SupplierCode != null && supplierCodes.Contains(x.SupplierCode));

            var parts = await partsQuery
                .Select(x => new
                {
                    x.Id,
                    x.PartCode,
                    x.PartDescription,
                    x.Bin,
                    x.SupplierCode,
                    x.QtyOnHand
                })
                .ToListAsync();

            if (parts.Count == 0)
                throw new InvalidOperationException("No parts match that selection, so there is nothing to report on.");

            // Everything from the start of the period onwards. That single sweep answers both
            // ends: the movements at or after the opening date give the opening level, and the
            // ones after the closing date give the closing level.
            var ledgerQuery = context.StockAdjustment
                .AsNoTracking()
                .Where(x => x.AdjustementDate >= fromStart)
                .Where(x => !x.Part.IsDeleted);

            if (supplierCodes.Count > 0)
                ledgerQuery = ledgerQuery.Where(x => x.Part.SupplierCode != null && supplierCodes.Contains(x.Part.SupplierCode));

            var ledger = await ledgerQuery
                .OrderBy(x => x.AdjustementDate)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.PartId,
                    x.AdjustementDate,
                    x.QTY,
                    x.StockAdjustmentType,
                    InvoiceNumber = x.StockReceipt == null ? null : x.StockReceipt.InvoiceNumber,
                    StockTakeNumber = x.StockTake == null ? (int?)null : x.StockTake.StockTakeNumber
                })
                .ToListAsync();

            var ledgerByPart = ledger.GroupBy(x => x.PartId).ToDictionary(x => x.Key, x => x.ToList());

            var reportParts = new List<StockMovementReportPart>();

            foreach (var part in parts.OrderBy(x => x.PartCode, StringComparer.OrdinalIgnoreCase))
            {
                var rows = ledgerByPart.TryGetValue(part.Id, out var found) ? found : new();

                var inPeriod = rows.Where(x => x.AdjustementDate < toEndExclusive).ToList();

                if (inPeriod.Count == 0 && !request.IncludePartsWithNoMovement)
                    continue;

                // Unwind from today back to each end of the period. Both ends come off the same
                // list, so the closing level can never disagree with the movements printed above it.
                var opening = part.QtyOnHand - rows.Sum(x => x.QTY);
                var closing = part.QtyOnHand - rows.Where(x => x.AdjustementDate >= toEndExclusive).Sum(x => x.QTY);

                var reportPart = new StockMovementReportPart
                {
                    PartCode = part.PartCode,
                    PartDescription = part.PartDescription,
                    Bin = part.Bin,
                    SupplierCode = part.SupplierCode,
                    OpeningQuantity = opening,
                    ClosingQuantity = closing
                };

                // Ordered by date then by insertion. The tiebreak matters: editing a service report
                // writes a reversal and its replacement in one save, and read out of order the
                // running balance would swing the wrong way before coming back.
                var balance = opening;

                foreach (var row in inPeriod)
                {
                    balance += row.QTY;

                    reportPart.Movements.Add(new StockMovementReportLine
                    {
                        MovementDate = row.AdjustementDate,
                        MovementType = DescribeMovement(row.StockAdjustmentType),
                        Reference = row.InvoiceNumber
                            ?? (row.StockTakeNumber is int number ? $"ST-{number:0000}" : null),
                        Quantity = row.QTY,
                        Balance = balance
                    });
                }

                reportParts.Add(reportPart);
            }

            _movementReport.Data = new StockMovementReportData
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                GeneratedOn = DateOnly.FromDateTime(DateTime.UtcNow),
                SupplierLabel = DescribeSuppliers(supplierCodes),
                Parts = reportParts
            };

            return Document.Create(_movementReport.Compose).GeneratePdf();
        }

        /// <summary>
        /// What is on the shelf now and what it is worth, grouped by the supplier it is bought
        /// from. Priced at the part's current cost rather than at what each unit was bought for:
        /// there is one cost price per part in this system, so that is the only figure available
        /// and the only one the rest of the app values stock at.
        /// </summary>
        public async Task<byte[]> GenerateStockValuationReport(StockValuationReportRequestModel request)
        {
            var supplierCodes = NormalizeSuppliers(request.SupplierCodes);

            using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.Parts
                .AsNoTracking()
                .Include(x => x.Prices)
                .Include(x => x.Warehouse)
                .Where(x => !x.IsDeleted);

            if (supplierCodes.Count > 0)
                query = query.Where(x => x.SupplierCode != null && supplierCodes.Contains(x.SupplierCode));

            if (request.ExcludeZeroQtyParts)
                query = query.Where(x => x.QtyOnHand != 0);

            var parts = await query.ToListAsync();

            if (parts.Count == 0)
                throw new InvalidOperationException("No parts match that selection, so there is nothing to value.");

            // Parts carrying no supplier code still hold value and still have to be accounted for,
            // so they group under their own heading rather than being dropped. Sorted last: a
            // valuation reads supplier by supplier, and the unattributed tail belongs at the end.
            var suppliers = parts
                .GroupBy(x => string.IsNullOrWhiteSpace(x.SupplierCode) ? null : x.SupplierCode!.Trim())
                .OrderBy(x => x.Key is null)
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new StockValuationReportSupplier
                {
                    SupplierCode = group.Key,
                    Lines = group
                        .OrderBy(x => x.PartCode, StringComparer.OrdinalIgnoreCase)
                        .Select(x => new StockValuationReportLine
                        {
                            PartCode = x.PartCode,
                            PartDescription = x.PartDescription,
                            Bin = x.Bin,
                            WarehouseCode = x.Warehouse?.WarehouseCode,
                            QuantityOnHand = x.QtyOnHand,
                            UnitCost = CurrentCostPrice(x)
                        })
                        .ToList()
                })
                .ToList();

            _valuationReport.Data = new StockValuationReportData
            {
                GeneratedAt = DateTimeOffset.Now,
                SupplierLabel = DescribeSuppliers(supplierCodes),
                SummaryOnly = request.SummaryOnly,
                Suppliers = suppliers
            };

            return Document.Create(_valuationReport.Compose).GeneratePdf();
        }
    }
}
