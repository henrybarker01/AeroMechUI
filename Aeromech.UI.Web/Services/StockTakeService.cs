using AeroMech.API.Reports;
using AeroMech.Data.Enums;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using StockAdjustmentType = AeroMech.Models.Enums.StockAdjustmentType;

namespace AeroMech.UI.Web.Services
{
    /// <summary>
    /// Counting the shelves and settling what the count found.
    ///
    /// The shape of this feature comes from one decision: a sheet freezes the stock levels it will
    /// be judged against at the moment it is raised, and posting applies the difference the count
    /// found rather than writing the counted figure over whatever is there. A count that runs for
    /// two days therefore survives parts being issued while it runs - the count corrects what it
    /// measured, and the movements that happened alongside it stay.
    ///
    /// <see cref="PostStockTake"/> is the only method here that writes stock, so there is one path
    /// to audit and one place the ledger is kept in step.
    /// </summary>
    public class StockTakeService
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly PartsService _partsService;
        private readonly StockCountSheet _countSheet;

        public StockTakeService(
            IDbContextFactory<AeroMechDBContext> contextFactory,
            IMapper mapper,
            PartsService partsService,
            StockCountSheet countSheet)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
            _partsService = partsService;
            _countSheet = countSheet;
        }

        /// <summary>
        /// Sorts blank supplier codes and bins last rather than first. Parts with no supplier still
        /// have to be counted in a full take, but they are the tail of the sheet, not its opening
        /// page.
        /// </summary>
        private const string UnsortedLast = "￿";

        public Task<List<SupplierOptionModel>> GetSuppliers() => _partsService.GetSupplierOptions();

        // Date pickers are date-only. Persist as UTC midnight for the selected calendar date so the
        // stored day never shifts with the timezone.
        private static DateTimeOffset NormalizeDateOnlyToUtc(DateTimeOffset value)
            => new DateTimeOffset(value.Date, TimeSpan.Zero);

        /// <summary>
        /// The parts screen reads a part's cost from its first price row, so that is the row a
        /// count sheet values its differences against.
        /// </summary>
        private static double CurrentCostPrice(Part part)
            => part.Prices?.OrderBy(x => x.Id).FirstOrDefault()?.CostPrice ?? 0;

        /// <summary>
        /// Raises a sheet and, in the same breath, freezes a line per part in scope. The freeze is
        /// the point: every quantity the count is later judged against is the level as it stood
        /// now, not as it stands whenever somebody gets round to posting.
        /// </summary>
        public async Task<int> CreateStockTake(StockTakeRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request.StockTakeDescription))
                throw new InvalidOperationException("A description is required so the sheet can be told apart later.");

            using var context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            var supplierCodes = request.SupplierCodes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var query = context.Parts
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            // No supplier chosen means the whole warehouse, and that has to include parts carrying
            // no supplier code at all - they sit on the same shelves and go missing the same way.
            if (supplierCodes.Count > 0)
                query = query.Where(x => x.SupplierCode != null && supplierCodes.Contains(x.SupplierCode));

            if (request.ExcludeZeroQtyParts)
                query = query.Where(x => x.QtyOnHand != 0);

            var parts = await query
                .Include(x => x.Warehouse)
                .Include(x => x.Prices)
                .ToListAsync();

            if (parts.Count == 0)
                throw new InvalidOperationException("No parts match that selection, so there is nothing to count.");

            var now = DateTimeOffset.UtcNow;
            var raisedBy = request.StockTakeBy ?? string.Empty;

            // Sequential rather than random so the number on the paper reads plainly. Two sheets
            // raised at the same instant would collide here; the unique index refuses the second,
            // which is the right outcome - a duplicated sheet number is exactly what makes a sheet
            // handed back on paper impossible to place.
            var nextNumber = (await context.StockTakes.MaxAsync(x => (int?)x.StockTakeNumber) ?? 0) + 1;

            var stockTake = new StockTake
            {
                StockTakeNumber = nextNumber,
                StockTakeDate = NormalizeDateOnlyToUtc(request.StockTakeDate),
                StockTakeDescription = request.StockTakeDescription.Trim(),
                Remarks = request.Remarks,
                Status = StockTakeStatus.Pending,
                StockTakeBy = raisedBy,
                BlindCount = request.BlindCount,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = raisedBy,
                UpdatedBy = raisedBy,
                IsDeleted = false
            };

            foreach (var part in parts)
            {
                stockTake.StockTakeParts.Add(new StockTakeParts
                {
                    PartId = part.Id,
                    SupplierCode = part.SupplierCode,
                    Bin = part.Bin,
                    WarehouseId = part.WarehouseId == 0 ? null : part.WarehouseId,
                    QuantityOnHand = part.QtyOnHand,
                    Quantity = null,
                    FinalQuantity = null,
                    UnitCost = CurrentCostPrice(part),
                    Status = StockTakeLineStatus.NotCounted,
                    RecountCount = 0,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = raisedBy,
                    UpdatedBy = raisedBy,
                    IsDeleted = false
                });
            }

            context.StockTakes.Add(stockTake);

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return stockTake.Id;
        }

        /// <summary>
        /// Every sheet, newest first, carrying just enough of each line to work out its progress
        /// and what it is worth. The lines are read in a narrow projection rather than in full:
        /// the list needs the counts derived from them, not the part detail behind them.
        /// </summary>
        public async Task<List<StockTakeModel>> GetStockTakes()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var takes = await context.StockTakes
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderByDescending(x => x.StockTakeNumber)
                .ToListAsync();

            if (takes.Count == 0)
                return new List<StockTakeModel>();

            var ids = takes.Select(x => x.Id).ToList();

            var lines = await context.StockTakeParts
                .AsNoTracking()
                .Where(x => ids.Contains(x.StockTakeId) && !x.IsDeleted)
                .Select(x => new
                {
                    x.StockTakeId,
                    x.SupplierCode,
                    x.QuantityOnHand,
                    x.Quantity,
                    x.FinalQuantity,
                    x.UnitCost,
                    x.Status
                })
                .ToListAsync();

            var linesByTake = lines
                .GroupBy(x => x.StockTakeId)
                .ToDictionary(g => g.Key, g => g.Select(x => new StockTakeLineModel
                {
                    SupplierCode = x.SupplierCode,
                    QuantityOnHand = x.QuantityOnHand,
                    Quantity = x.Quantity,
                    FinalQuantity = x.FinalQuantity,
                    UnitCost = x.UnitCost,
                    Status = x.Status
                }).ToList());

            var models = _mapper.Map<List<StockTakeModel>>(takes);

            foreach (var model in models)
                model.Lines = linesByTake.TryGetValue(model.Id, out var takeLines) ? takeLines : new List<StockTakeLineModel>();

            return models;
        }

        /// <summary>
        /// One sheet in full, ordered the way it is to be worked. The part's live stock level comes
        /// back alongside the frozen one so review can point out anything that moved while the
        /// count was running.
        /// </summary>
        public async Task<StockTakeModel?> GetStockTake(int stockTakeId, StockTakeSheetOrder order = StockTakeSheetOrder.SupplierThenPart)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var stockTake = await context.StockTakes
                .AsNoTracking()
                .Where(x => x.Id == stockTakeId && !x.IsDeleted)
                .Include(x => x.StockTakeParts.Where(l => !l.IsDeleted))
                    .ThenInclude(l => l.Part)
                .Include(x => x.StockTakeParts.Where(l => !l.IsDeleted))
                    .ThenInclude(l => l.Warehouse)
                .SingleOrDefaultAsync();

            if (stockTake == null)
                return null;

            var model = _mapper.Map<StockTakeModel>(stockTake);

            // The mapper cannot reach the part's live level through the line, so it is carried over
            // here from the same entities that were just loaded.
            var liveLevels = stockTake.StockTakeParts.ToDictionary(x => x.Id, x => x.Part?.QtyOnHand ?? 0);

            foreach (var line in model.Lines)
            {
                if (liveLevels.TryGetValue(line.Id, out var live))
                    line.CurrentQtyOnHand = live;
            }

            model.Lines = SortLines(model.Lines, order);

            return model;
        }

        /// <summary>
        /// Puts the lines in the order the sheet is read in. Sorted here rather than in the query
        /// because blank supplier codes and blank bins have to fall to the end, and that is not
        /// something the database's own collation will agree to do.
        /// </summary>
        public static List<StockTakeLineModel> SortLines(IEnumerable<StockTakeLineModel> lines, StockTakeSheetOrder order)
            => order == StockTakeSheetOrder.BinThenPart
                ? lines
                    .OrderBy(x => string.IsNullOrWhiteSpace(x.Bin) ? UnsortedLast : x.Bin, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.PartCode, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                : lines
                    .OrderBy(x => string.IsNullOrWhiteSpace(x.SupplierCode) ? UnsortedLast : x.SupplierCode, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.PartCode, StringComparer.OrdinalIgnoreCase)
                    .ToList();

        /// <summary>
        /// Writes captured quantities back. Takes a batch rather than one line so the capture
        /// screen can save as the counter works without a round trip per keystroke, and so a
        /// dropped connection costs at most the handful of lines since the last save.
        /// </summary>
        public async Task SaveCounts(int stockTakeId, IEnumerable<StockTakeCountEntryModel> entries, string countedBy)
        {
            var batch = entries?.ToList() ?? new List<StockTakeCountEntryModel>();
            if (batch.Count == 0)
                return;

            if (batch.Any(x => x.Quantity.HasValue && x.Quantity.Value < 0))
                throw new InvalidOperationException("A counted quantity cannot be negative.");

            using var context = await _contextFactory.CreateDbContextAsync();

            var stockTake = await context.StockTakes
                .Include(x => x.StockTakeParts)
                .SingleOrDefaultAsync(x => x.Id == stockTakeId && !x.IsDeleted)
                ?? throw new InvalidOperationException("That stock take no longer exists.");

            EnsureOpen(stockTake);

            var now = DateTimeOffset.UtcNow;
            var by = countedBy ?? string.Empty;
            var lineIds = batch.Select(x => x.LineId).ToHashSet();
            var lines = stockTake.StockTakeParts.Where(x => lineIds.Contains(x.Id)).ToDictionary(x => x.Id);

            foreach (var entry in batch)
            {
                if (!lines.TryGetValue(entry.LineId, out var line))
                    continue;

                // A line already settled in review is not quietly re-opened by a stray save; the
                // reviewer's decision stands until it is explicitly sent back for a recount.
                if (line.Status == StockTakeLineStatus.Accepted)
                    continue;

                line.Quantity = entry.Quantity;
                line.Remarks = entry.Remarks;
                line.UpdatedAt = now;
                line.UpdatedBy = by;

                if (entry.Quantity.HasValue)
                {
                    line.Status = StockTakeLineStatus.Counted;
                    line.CountedBy = by;
                    line.CountedAt = now;
                }
                else
                {
                    // Clearing a figure puts the line back to uncounted rather than to zero. The
                    // two mean different things and only one of them moves stock.
                    line.Status = StockTakeLineStatus.NotCounted;
                    line.CountedBy = null;
                    line.CountedAt = null;
                }
            }

            // Reached by counting rather than by a button: a sheet somebody has started entering
            // figures on is being counted, whatever anyone remembered to click.
            if (stockTake.Status == StockTakeStatus.Pending && stockTake.StockTakeParts.Any(x => x.Quantity.HasValue))
                stockTake.Status = StockTakeStatus.Counting;

            stockTake.UpdatedAt = now;
            stockTake.UpdatedBy = by;

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Closes counting and opens review. Refused while nothing has been counted, since there
        /// would be nothing to review.
        /// </summary>
        public async Task StartReview(int stockTakeId, string by)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var stockTake = await context.StockTakes
                .Include(x => x.StockTakeParts)
                .SingleOrDefaultAsync(x => x.Id == stockTakeId && !x.IsDeleted)
                ?? throw new InvalidOperationException("That stock take no longer exists.");

            EnsureOpen(stockTake);

            if (!stockTake.StockTakeParts.Any(x => x.Quantity.HasValue))
                throw new InvalidOperationException("Nothing has been counted yet, so there is nothing to review.");

            stockTake.Status = StockTakeStatus.Review;
            stockTake.UpdatedAt = DateTimeOffset.UtcNow;
            stockTake.UpdatedBy = by ?? string.Empty;

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Sends the sheet back to counting, which is what a reviewer does after asking for
        /// recounts or on finding the count was stopped too early.
        /// </summary>
        public async Task ReopenCounting(int stockTakeId, string by)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var stockTake = await context.StockTakes
                .SingleOrDefaultAsync(x => x.Id == stockTakeId && !x.IsDeleted)
                ?? throw new InvalidOperationException("That stock take no longer exists.");

            EnsureOpen(stockTake);

            stockTake.Status = StockTakeStatus.Counting;
            stockTake.UpdatedAt = DateTimeOffset.UtcNow;
            stockTake.UpdatedBy = by ?? string.Empty;

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Takes the counted figures as correct for the given lines. Accepting is what fixes the
        /// quantity posting will write, and it is recorded per line so a sheet settled by several
        /// people still shows who agreed to what.
        /// </summary>
        public async Task AcceptLines(int stockTakeId, IEnumerable<int> lineIds, string by)
        {
            var ids = lineIds?.ToHashSet() ?? new HashSet<int>();
            if (ids.Count == 0)
                return;

            using var context = await _contextFactory.CreateDbContextAsync();

            var stockTake = await context.StockTakes
                .Include(x => x.StockTakeParts)
                .SingleOrDefaultAsync(x => x.Id == stockTakeId && !x.IsDeleted)
                ?? throw new InvalidOperationException("That stock take no longer exists.");

            EnsureOpen(stockTake);

            var now = DateTimeOffset.UtcNow;
            var user = by ?? string.Empty;

            foreach (var line in stockTake.StockTakeParts.Where(x => ids.Contains(x.Id)))
            {
                // Only a line with a figure on it can be accepted. Accepting an uncounted line
                // would be agreeing to a number nobody produced.
                if (!line.Quantity.HasValue)
                    continue;

                line.FinalQuantity = line.Quantity;
                line.Status = StockTakeLineStatus.Accepted;
                line.UpdatedAt = now;
                line.UpdatedBy = user;
            }

            stockTake.UpdatedAt = now;
            stockTake.UpdatedBy = user;

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Sends lines back to be counted again. The rejected figure moves to
        /// <see cref="StockTakeParts.PreviousQuantity"/> instead of being thrown away, so the
        /// second count can be read against the first - a part that lands somewhere different
        /// again is saying something neither figure says on its own.
        /// </summary>
        public async Task RequestRecount(int stockTakeId, IEnumerable<int> lineIds, string by)
        {
            var ids = lineIds?.ToHashSet() ?? new HashSet<int>();
            if (ids.Count == 0)
                return;

            using var context = await _contextFactory.CreateDbContextAsync();

            var stockTake = await context.StockTakes
                .Include(x => x.StockTakeParts)
                .SingleOrDefaultAsync(x => x.Id == stockTakeId && !x.IsDeleted)
                ?? throw new InvalidOperationException("That stock take no longer exists.");

            EnsureOpen(stockTake);

            var now = DateTimeOffset.UtcNow;
            var user = by ?? string.Empty;

            foreach (var line in stockTake.StockTakeParts.Where(x => ids.Contains(x.Id)))
            {
                line.PreviousQuantity = line.Quantity ?? line.PreviousQuantity;
                line.Quantity = null;
                line.FinalQuantity = null;
                line.Status = StockTakeLineStatus.RecountRequested;
                line.RecountCount++;
                line.CountedBy = null;
                line.CountedAt = null;
                line.UpdatedAt = now;
                line.UpdatedBy = user;
            }

            // There is counting to do again, so the sheet says so.
            stockTake.Status = StockTakeStatus.Counting;
            stockTake.UpdatedAt = now;
            stockTake.UpdatedBy = user;

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Abandons a sheet without touching stock. Kept rather than deleted, because a count that
        /// was started and dropped is itself worth being able to see.
        /// </summary>
        public async Task CancelStockTake(int stockTakeId, string by)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var stockTake = await context.StockTakes
                .SingleOrDefaultAsync(x => x.Id == stockTakeId && !x.IsDeleted)
                ?? throw new InvalidOperationException("That stock take no longer exists.");

            if (stockTake.Status == StockTakeStatus.Completed)
                throw new InvalidOperationException("That stock take has already been posted and cannot be cancelled.");

            stockTake.Status = StockTakeStatus.Cancelled;
            stockTake.UpdatedAt = DateTimeOffset.UtcNow;
            stockTake.UpdatedBy = by ?? string.Empty;

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Settles the sheet against stock: corrects each part by the difference its count found,
        /// writes a ledger row for every correction, and closes the sheet. All in one transaction,
        /// because a stock take that moved some stock but recorded none would be untraceable.
        ///
        /// The correction applied is the difference against the frozen level, not the counted
        /// figure written over the current one. Where a part moved while the count ran, that
        /// movement is preserved and the count still corrects what it measured.
        /// </summary>
        public async Task<StockTakePostResultModel> PostStockTake(int stockTakeId, string postedBy)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            var stockTake = await context.StockTakes
                .Include(x => x.StockTakeParts.Where(l => !l.IsDeleted))
                .SingleOrDefaultAsync(x => x.Id == stockTakeId && !x.IsDeleted)
                ?? throw new InvalidOperationException("That stock take no longer exists.");

            EnsureOpen(stockTake);

            var lines = stockTake.StockTakeParts.ToList();

            if (lines.Count == 0)
                throw new InvalidOperationException("That stock take has no lines.");

            var undecided = lines.Count(x => x.Status == StockTakeLineStatus.Counted
                                          && x.Quantity.HasValue
                                          && x.Quantity.Value != x.QuantityOnHand);

            if (undecided > 0)
                throw new InvalidOperationException($"{undecided} difference(s) have not been settled. Accept them or send them back for a recount first.");

            var awaitingRecount = lines.Count(x => x.Status == StockTakeLineStatus.RecountRequested);

            if (awaitingRecount > 0)
                throw new InvalidOperationException($"{awaitingRecount} line(s) are waiting to be counted again.");

            // A line is settled when its count agreed with the system, or when its difference was
            // accepted. Anything else - never counted, sent back - is left alone by posting.
            var settled = lines
                .Where(x => x.Status == StockTakeLineStatus.Accepted
                         || (x.Status == StockTakeLineStatus.Counted && x.Quantity.HasValue && x.Quantity.Value == x.QuantityOnHand))
                .ToList();

            if (settled.Count == 0)
                throw new InvalidOperationException("Nothing on this stock take has been settled, so there is nothing to post.");

            var now = DateTimeOffset.UtcNow;
            var user = postedBy ?? string.Empty;
            var partIds = settled.Select(x => x.PartId).ToList();

            // Read the parts inside the transaction and correct whatever is found there. The
            // browser's copy of a stock level can be hours old on a sheet that took a day to count.
            var parts = await context.Parts
                .Where(x => partIds.Contains(x.Id))
                .ToListAsync();

            var result = new StockTakePostResultModel
            {
                StockTakeId = stockTake.Id,
                Reference = $"ST-{stockTake.StockTakeNumber:0000}",
                LinesNotCounted = lines.Count - settled.Count
            };

            foreach (var line in settled)
            {
                var part = parts.SingleOrDefault(x => x.Id == line.PartId);

                if (part == null || part.IsDeleted)
                    throw new InvalidOperationException("A part on this stock take no longer exists, so the sheet cannot be posted.");

                var finalQuantity = line.FinalQuantity ?? line.Quantity ?? line.QuantityOnHand;
                var delta = finalQuantity - line.QuantityOnHand;

                line.FinalQuantity = finalQuantity;
                line.QtyOnHandAtPost = part.QtyOnHand;
                line.AppliedDelta = delta;
                line.UpdatedAt = now;
                line.UpdatedBy = user;

                if (part.QtyOnHand != line.QuantityOnHand)
                    result.LinesMovedDuringCount++;

                // A count that agreed with the system corrects nothing, so it moves no stock and
                // writes no ledger row. Recording a zero movement would only bury the real ones.
                if (delta == 0)
                    continue;

                part.QtyOnHand += delta;

                result.LinesAdjusted++;
                result.ValueAdjustment += delta * line.UnitCost;

                if (delta > 0)
                    result.UnitsAdded += delta;
                else
                    result.UnitsRemoved += -delta;

                // The same ledger the service reports and receipts write to, so a part's whole
                // movement history reads from one table however the stock came or went.
                context.StockAdjustment.Add(new StockAdjustment
                {
                    PartId = part.Id,
                    WarehouseId = part.WarehouseId,
                    QTY = delta,
                    AdjustementDate = now,
                    AdjustedById = new Guid(),
                    StockAdjustmentType = StockAdjustmentType.StockTake,
                    StockTake = stockTake
                });
            }

            stockTake.Status = StockTakeStatus.Completed;
            stockTake.CompletedDate = now;
            stockTake.CompletedBy = user;
            stockTake.UpdatedAt = now;
            stockTake.UpdatedBy = user;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return result;
        }

        /// <summary>
        /// The printable sheet, in the order asked for. Generated from the stored lines rather than
        /// from the parts table, so a sheet re-printed halfway through a count still lists exactly
        /// the parts the count was raised against and in the same order as the first printing.
        /// </summary>
        public async Task<byte[]> GenerateCountSheet(int stockTakeId, StockTakeSheetOrder order = StockTakeSheetOrder.SupplierThenPart)
        {
            var stockTake = await GetStockTake(stockTakeId, order)
                ?? throw new InvalidOperationException("That stock take no longer exists.");

            var suppliers = stockTake.SupplierCodes;

            _countSheet.Data = new StockCountSheetData
            {
                Reference = stockTake.Reference,
                Description = stockTake.StockTakeDescription ?? string.Empty,
                StockTakeDate = stockTake.StockTakeDate,

                // Named while the list is short enough to read on a page header, counted otherwise.
                SupplierLabel = suppliers.Count == 0 ? "-"
                    : suppliers.Count <= 6 ? string.Join(", ", suppliers)
                    : $"{suppliers.Count} suppliers",

                Order = order,
                ShowExpectedQuantity = !stockTake.BlindCount,
                Lines = stockTake.Lines.Select(x => new StockCountSheetLine
                {
                    PartCode = x.PartCode,
                    PartDescription = x.PartDescription,
                    Bin = x.Bin,
                    SupplierCode = x.SupplierCode,
                    WarehouseCode = x.WarehouseCode,
                    QuantityOnHand = x.QuantityOnHand
                }).ToList()
            };

            return Document.Create(_countSheet.Compose).GeneratePdf();
        }

        private static void EnsureOpen(StockTake stockTake)
        {
            if (stockTake.Status == StockTakeStatus.Completed)
                throw new InvalidOperationException("That stock take has already been posted.");

            if (stockTake.Status == StockTakeStatus.Cancelled)
                throw new InvalidOperationException("That stock take was cancelled.");
        }
    }
}
