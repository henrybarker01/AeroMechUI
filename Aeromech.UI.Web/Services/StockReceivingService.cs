using AeroMech.Data.Enums;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    /// <summary>
    /// Receiving stock against supplier invoices. Every increase goes through
    /// <see cref="PostReceipt"/>, which is the only place in this feature that writes stock, so
    /// there is one path to audit and one place the ledger is kept in step.
    /// </summary>
    public class StockReceivingService
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly PartsService _partsService;
        private readonly AuditService _auditService;

        public StockReceivingService(
            IDbContextFactory<AeroMechDBContext> contextFactory,
            IMapper mapper,
            PartsService partsService,
            AuditService auditService)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
            _partsService = partsService;
            _auditService = auditService;
        }

        // Date pickers are date-only. Persist as UTC midnight for the selected calendar date so
        // the stored day never shifts with the timezone.
        private static DateTimeOffset NormalizeDateOnlyToUtc(DateTimeOffset value)
            => new DateTimeOffset(value.Date, TimeSpan.Zero);

        /// <summary>
        /// The supplier codes worth receiving against, which is those actually carried by parts.
        /// Owned by <see cref="PartsService"/> so receiving and stock takes read one definition of
        /// what counts as a supplier rather than each keeping its own.
        /// </summary>
        public Task<List<SupplierOptionModel>> GetSuppliers() => _partsService.GetSupplierOptions();

        /// <summary>
        /// Every part the supplier carries, ready to receive against. Quantities start at zero and
        /// the unit cost starts at what the part currently costs, so a receiver only has to touch
        /// the rows the invoice actually lists.
        /// </summary>
        public async Task<List<StockReceivingLineModel>> GetPartsForSupplier(string supplierCode)
        {
            if (string.IsNullOrWhiteSpace(supplierCode))
                return new List<StockReceivingLineModel>();

            using var context = await _contextFactory.CreateDbContextAsync();

            var parts = await context.Parts
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.SupplierCode == supplierCode)
                .Include(x => x.Warehouse)
                .Include(x => x.Prices)
                .OrderBy(x => x.PartCode).ThenBy(x => x.PartDescription)
                .ToListAsync();

            return parts.Select(part =>
            {
                var costPrice = CurrentCostPrice(part);

                return new StockReceivingLineModel
                {
                    PartId = part.Id,
                    PartCode = part.PartCode,
                    PartDescription = part.PartDescription,
                    Bin = part.Bin,
                    ProductClass = part.ProductClass,
                    WarehouseId = part.WarehouseId,
                    WarehouseCode = part.Warehouse?.WarehouseCode,
                    QtyOnHand = part.QtyOnHand,
                    CurrentCostPrice = costPrice,
                    UnitCost = costPrice,
                    QtyReceived = 0
                };
            }).ToList();
        }

        /// <summary>
        /// Whether this supplier's invoice number has already been received. The same number from
        /// two different suppliers is ordinary; the same number twice from one supplier almost
        /// always means the stock is about to be taken in twice.
        /// </summary>
        public async Task<bool> InvoiceAlreadyReceived(string supplierCode, string invoiceNumber)
        {
            if (string.IsNullOrWhiteSpace(supplierCode) || string.IsNullOrWhiteSpace(invoiceNumber))
                return false;

            using var context = await _contextFactory.CreateDbContextAsync();

            var supplier = supplierCode.Trim();
            var invoice = invoiceNumber.Trim().ToLower();

            return await context.StockReceipts
                .AsNoTracking()
                .AnyAsync(x => !x.IsDeleted
                            && x.SupplierCode == supplier
                            && x.InvoiceNumber.ToLower() == invoice);
        }

        /// <summary>
        /// Takes the invoice into stock: raises each part's quantity, writes a ledger row per line
        /// and stores the invoice with the levels it moved between. All of it in one transaction,
        /// because a receipt that moved some stock but recorded none would be invisible.
        /// </summary>
        public async Task<int> PostReceipt(StockReceiptModel receipt)
        {
            var lines = receipt.Lines.Where(x => x.IsOnInvoice).ToList();

            if (string.IsNullOrWhiteSpace(receipt.SupplierCode))
                throw new InvalidOperationException("A supplier is required to receive stock.");

            if (string.IsNullOrWhiteSpace(receipt.InvoiceNumber))
                throw new InvalidOperationException("An invoice number is required to receive stock.");

            if (lines.Count == 0)
                throw new InvalidOperationException("Enter a received quantity against at least one part.");

            if (receipt.Lines.Any(x => x.QtyReceived < 0))
                throw new InvalidOperationException("Received quantities cannot be negative.");

            using var context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            var receivedAt = DateTimeOffset.UtcNow;
            var receivedBy = string.IsNullOrWhiteSpace(receipt.ReceivedBy) ? string.Empty : receipt.ReceivedBy;

            // The screen already names who is receiving, and that name is what goes on the
            // document. The audit trail keeps the same one so the two cannot disagree, falling
            // back to the signed-in user where the screen supplied nothing.
            var auditUser = await _auditService.ResolveUser(receivedBy);

            var stockReceipt = new StockReceipt
            {
                SupplierCode = receipt.SupplierCode.Trim(),
                InvoiceNumber = receipt.InvoiceNumber.Trim(),
                InvoiceDate = NormalizeDateOnlyToUtc(receipt.InvoiceDate),
                ReceivedDate = receivedAt,
                ReceivedBy = receivedBy,
                InvoiceSubTotal = receipt.InvoiceSubTotal,
                InvoiceVat = receipt.InvoiceVat,
                InvoiceTotal = receipt.InvoiceTotal,
                Notes = receipt.Notes,
                CreatedAt = receivedAt,
                UpdatedAt = receivedAt,
                CreatedBy = receivedBy,
                UpdatedBy = receivedBy,
                IsDeleted = false
            };

            context.StockReceipts.Add(stockReceipt);

            var partIds = lines.Select(x => x.PartId).ToList();

            // Read the parts inside the transaction and add to whatever is found there. The
            // browser's copy of a stock level can be minutes old, so a service report posted while
            // this screen was open would be wiped out by writing back the level the grid showed.
            var parts = await context.Parts
                .Include(x => x.Prices)
                .Where(x => partIds.Contains(x.Id))
                .ToListAsync();

            foreach (var line in lines)
            {
                var part = parts.SingleOrDefault(x => x.Id == line.PartId);

                if (part == null || part.IsDeleted)
                    throw new InvalidOperationException($"Part {line.PartCode} no longer exists and cannot be received.");

                var qtyBefore = part.QtyOnHand;
                part.QtyOnHand = qtyBefore + line.QtyReceived;

                var costPriceUpdated = receipt.UpdateCostPrices && line.CostPriceDiffers;
                var costPriceBefore = CurrentCostPrice(part);

                if (costPriceUpdated)
                    ApplyCostPrice(part, line.UnitCost, receivedAt);

                stockReceipt.Lines.Add(new StockReceiptLine
                {
                    PartId = part.Id,
                    QtyReceived = line.QtyReceived,
                    UnitCost = line.UnitCost,
                    QtyOnHandBefore = qtyBefore,
                    QtyOnHandAfter = part.QtyOnHand,
                    CostPriceUpdated = costPriceUpdated,
                    IsDeleted = false
                });

                // The same ledger the service reports write to, so a part's whole movement history
                // reads from one table whichever direction the stock went.
                context.StockAdjustment.Add(new StockAdjustment
                {
                    PartId = part.Id,
                    WarehouseId = part.WarehouseId,
                    QTY = line.QtyReceived,
                    AdjustementDate = receivedAt,
                    AdjustedById = new Guid(),
                    StockAdjustmentType = StockAdjustmentType.StockReceipt,
                    StockReceipt = stockReceipt
                });

                // A line at a time rather than one entry for the invoice, because the question
                // asked of an audit trail is almost always about one part.
                _auditService.RecordStockChange(
                    context,
                    auditUser,
                    part.Id,
                    part.PartCode,
                    qtyBefore,
                    part.QtyOnHand,
                    $"Stock received on invoice {stockReceipt.InvoiceNumber} from {stockReceipt.SupplierCode}.");

                // Receiving is the one path that changes a price as a side effect of moving stock,
                // which makes it the one most easily missed when a cost is later queried.
                if (costPriceUpdated)
                {
                    _auditService.RecordPriceChange(
                        context,
                        auditUser,
                        nameof(Part),
                        part.Id,
                        part.PartCode,
                        nameof(PartPrice.CostPrice),
                        AuditService.FormatMoney(costPriceBefore),
                        AuditService.FormatMoney(line.UnitCost),
                        $"Cost price updated from invoice {stockReceipt.InvoiceNumber} while receiving stock.");
                }
            }

            await context.SaveChangesAsync();

            // Written after the save because the receipt has no id until then, and an entry that
            // cannot point at the invoice it is about would not be worth keeping. Still inside the
            // transaction, so the receipt and its audit entry stand or fall together.
            _auditService.Record(
                context,
                auditUser,
                AuditArea.StockReceiving,
                AuditAction.Posted,
                nameof(StockReceipt),
                stockReceipt.Id,
                stockReceipt.InvoiceNumber,
                $"Invoice {stockReceipt.InvoiceNumber} from {stockReceipt.SupplierCode} received: "
                    + $"{lines.Count} line(s), {lines.Sum(x => x.QtyReceived)} unit(s), "
                    + $"total {AuditService.FormatMoney(receipt.InvoiceTotal)}.");

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return stockReceipt.Id;
        }

        /// <summary>
        /// Posted receipts, newest first, for looking an invoice up later.
        /// </summary>
        public async Task<List<StockReceiptModel>> GetReceipts()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var receipts = await context.StockReceipts
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(x => x.Lines)
                .OrderByDescending(x => x.ReceivedDate)
                .ThenByDescending(x => x.Id)
                .ToListAsync();

            return _mapper.Map<List<StockReceiptModel>>(receipts);
        }

        /// <summary>
        /// The lines taken in on one receipt, with the part detail needed to read them.
        /// </summary>
        public async Task<List<StockReceivingLineModel>> GetReceiptLines(int receiptId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var lines = await context.StockReceiptLines
                .AsNoTracking()
                .Where(x => x.StockReceiptId == receiptId && !x.IsDeleted)
                .Include(x => x.Part)
                    .ThenInclude(p => p!.Warehouse)
                .OrderBy(x => x.Part!.PartCode)
                .ToListAsync();

            return _mapper.Map<List<StockReceivingLineModel>>(lines);
        }

        /// <summary>
        /// The parts screen reads a part's cost from its first price row, so that is the row a
        /// receipt has to write back to for the new cost to be the one shown.
        /// </summary>
        private static PartPrice? CurrentPriceRow(Part part)
            => part.Prices?.OrderBy(x => x.Id).FirstOrDefault();

        private static double CurrentCostPrice(Part part)
            => CurrentPriceRow(part)?.CostPrice ?? 0;

        private static void ApplyCostPrice(Part part, double unitCost, DateTimeOffset effectiveDate)
        {
            var priceRow = CurrentPriceRow(part);

            if (priceRow == null)
            {
                part.Prices ??= new List<PartPrice>();
                part.Prices.Add(new PartPrice
                {
                    CostPrice = unitCost,
                    SellingPrice = 0,
                    EffectiveDate = effectiveDate,
                    IsDeleted = false
                });
                return;
            }

            priceRow.CostPrice = unitCost;
            priceRow.EffectiveDate = effectiveDate;
        }
    }
}
