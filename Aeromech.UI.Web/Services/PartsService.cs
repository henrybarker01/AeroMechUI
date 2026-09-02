using AeroMech.Data.Enums;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    public class PartsService
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly AuditService _auditService;

        public PartsService(IDbContextFactory<AeroMechDBContext> contextFactory, IMapper mapper, AuditService auditService)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
            _auditService = auditService;
        }

        public async Task<List<PartModel>> GetParts()
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            List<Part> parts = await _aeroMechDBContext.Parts
                .AsNoTracking()
                .Where(x => x.IsDeleted == false)
                .Include(a => a.Warehouse)
                .Include(p => p.Prices)
                .OrderBy(x => x.PartCode).ThenBy(x => x.PartDescription)
                .ToListAsync();
            return _mapper.Map<List<PartModel>>(parts);
        }

        /// <summary>
        /// The supplier codes actually carried by parts, with how many parts each covers. Built by
        /// grouping the parts themselves because supplier codes live on <c>Part.SupplierCode</c>
        /// and there is no supplier table. Parts with no code are left out: there is no supplier
        /// to receive them against or to count them under.
        /// </summary>
        public async Task<List<SupplierOptionModel>> GetSupplierOptions()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Parts
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.SupplierCode != null && x.SupplierCode != "")
                .GroupBy(x => x.SupplierCode!)
                .Select(g => new SupplierOptionModel
                {
                    SupplierCode = g.Key,
                    PartCount = g.Count()
                })
                .OrderBy(x => x.SupplierCode)
                .ToListAsync();
        }

        public async Task DeletePart(PartModel prt)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            var part = await _aeroMechDBContext.Parts.FindAsync(prt.Id);
            if (part != null)
            {
                var user = await _auditService.ResolveUser();

                part.IsDeleted = true;

                // Recorded with the level the part was carrying when it went. A part removed with
                // stock still against it is the kind of thing a stock figure later has to be
                // explained by, and the quantity is not readable from anywhere else afterwards.
                _auditService.Record(
                    _aeroMechDBContext,
                    user,
                    AuditArea.Parts,
                    AuditAction.Deleted,
                    nameof(Part),
                    part.Id,
                    part.PartCode,
                    $"Part deleted while carrying {part.QtyOnHand} on hand.");

                await _aeroMechDBContext.SaveChangesAsync();
            }
        }

        public async Task<int> AddNewPart(PartModel part)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var user = await _auditService.ResolveUser();

            if (part.Id == 0)
            {
                // The part has no id until it is saved, and an audit entry that cannot name what
                // it is about is worth little. So the save happens inside a transaction and the
                // entries are written against the id it produced: either both land or neither
                // does, and a part cannot come into existence unrecorded.
                using var transaction = await _aeroMechDBContext.Database.BeginTransactionAsync();

                AeroMech.Data.Models.Part prt = _mapper.Map<AeroMech.Data.Models.Part>(part);

                // The Add Part screen carries a placeholder warehouse with id 0 because the shop
                // runs a single site and offers no picker. Left as it is, EF reads the mapped
                // navigation as a brand new warehouse to insert while the opening movement below
                // still holds id 0 - the very key its foreign key rejects. Bind the part and its
                // movement to the warehouse that already exists instead.
                var warehouseId = await ResolveWarehouseId(_aeroMechDBContext, prt);
                prt.Warehouse = null;
                prt.WarehouseId = warehouseId;

                prt.Prices = new List<PartPrice>() { new PartPrice() {
                    CostPrice = Convert.ToDouble(part.CostPrice),
                    EffectiveDate = DateTimeOffset.UtcNow,
                    IsDeleted = false,
                    SellingPrice = 0
                }};

                _aeroMechDBContext.Parts.Add(prt);

                // A part brought onto the system with stock already against it has to enter the
                // ledger as a movement, not appear from nowhere. Without this row the quantity
                // reads as though it had always been there, and a movement report for any period
                // before the part existed would show it opening at today's level.
                if (prt.QtyOnHand != 0)
                {
                    _aeroMechDBContext.StockAdjustment.Add(new StockAdjustment
                    {
                        Part = prt,
                        WarehouseId = warehouseId,
                        QTY = prt.QtyOnHand,
                        AdjustementDate = DateTimeOffset.UtcNow,
                        AdjustedById = new Guid(),
                        StockAdjustmentType = StockAdjustmentType.StockAdjustment
                    });
                }

                await _aeroMechDBContext.SaveChangesAsync();

                _auditService.Record(
                    _aeroMechDBContext,
                    user,
                    AuditArea.Parts,
                    AuditAction.Created,
                    nameof(Part),
                    prt.Id,
                    prt.PartCode,
                    $"Part added at a cost price of {AuditService.FormatMoney(Convert.ToDouble(part.CostPrice))}.");

                // The opening quantity is a stock movement like any other, and the one most worth
                // being able to point at: it is the only figure on the part nobody had to justify.
                if (prt.QtyOnHand != 0)
                {
                    _auditService.RecordStockChange(
                        _aeroMechDBContext,
                        user,
                        prt.Id,
                        prt.PartCode,
                        0,
                        prt.QtyOnHand,
                        "Opening stock recorded when the part was added.");
                }

                await _aeroMechDBContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return prt.Id;
            }
            else
            {
                Part partToEdit = await _aeroMechDBContext.Parts
                    .Include(x => x.Prices)
                    .SingleAsync(x => x.Id == part.Id);

                // Read before it is written over. Editing a part is the one place stock can change
                // without a receipt or a count behind it, so the difference is what has to reach
                // the ledger for the level to stay explainable.
                var previousQtyOnHand = partToEdit.QtyOnHand;

                // Read for the same reason, one step further: what a field was is the half of a
                // change that stops existing the moment it is written over.
                var changedFields = new List<(string Field, string? OldValue, string? NewValue)>();

                void TrackChange(string field, string? oldValue, string? newValue)
                {
                    if (!string.Equals(oldValue ?? string.Empty, newValue ?? string.Empty, StringComparison.Ordinal))
                        changedFields.Add((field, oldValue, newValue));
                }

                TrackChange(nameof(Part.PartCode), partToEdit.PartCode, part.PartCode);
                TrackChange(nameof(Part.PartDescription), partToEdit.PartDescription, part.PartDescription);
                TrackChange(nameof(Part.Bin), partToEdit.Bin, part.Bin);
                TrackChange(nameof(Part.CycleCount), AuditService.FormatQuantity(partToEdit.CycleCount), AuditService.FormatQuantity(part.CycleCount));
                TrackChange(nameof(Part.SupplierCode), partToEdit.SupplierCode, part.SupplierCode);
                TrackChange(nameof(Part.ProductClass), partToEdit.ProductClass, part.ProductClass);

                var previousCostPrice = partToEdit.Prices?.OrderBy(x => x.Id).FirstOrDefault()?.CostPrice;

                partToEdit.PartCode = part.PartCode;
                partToEdit.PartDescription = part.PartDescription;
                partToEdit.Bin = part.Bin;
                partToEdit.CycleCount = part.CycleCount;
                partToEdit.Warehouse = null;
                partToEdit.SupplierCode = part.SupplierCode;
                partToEdit.QtyOnHand = part.QtyOnHand;
                partToEdit.ProductClass = part.ProductClass;

                if (partToEdit.QtyOnHand != previousQtyOnHand)
                {
                    _aeroMechDBContext.StockAdjustment.Add(new StockAdjustment
                    {
                        PartId = partToEdit.Id,
                        WarehouseId = partToEdit.WarehouseId,
                        QTY = partToEdit.QtyOnHand - previousQtyOnHand,
                        AdjustementDate = DateTimeOffset.UtcNow,
                        AdjustedById = new Guid(),
                        StockAdjustmentType = StockAdjustmentType.StockAdjustment
                    });

                    // Typed straight onto the part, with no receipt or count behind it. This is
                    // the adjustment an audit trail exists for, so it is spelled out as such.
                    _auditService.RecordStockChange(
                        _aeroMechDBContext,
                        user,
                        partToEdit.Id,
                        partToEdit.PartCode,
                        previousQtyOnHand,
                        partToEdit.QtyOnHand,
                        $"Stock adjusted by {Describe(partToEdit.QtyOnHand - previousQtyOnHand)} by editing the part.");
                }

                var newCostPrice = Convert.ToDouble(part.CostPrice);

                if (partToEdit.Prices == null || partToEdit.Prices.Count == 0)
                {
                    partToEdit.Prices = new List<PartPrice>() { new PartPrice() {
                        CostPrice = newCostPrice,
                        EffectiveDate = DateTimeOffset.UtcNow,
                        IsDeleted = false,
                        SellingPrice = 0
                    }};
                }
                else
                {
                    partToEdit.Prices.First().CostPrice = newCostPrice;
                }

                if (previousCostPrice is null || Math.Abs(previousCostPrice.Value - newCostPrice) > 0.0001)
                {
                    _auditService.RecordPriceChange(
                        _aeroMechDBContext,
                        user,
                        nameof(Part),
                        partToEdit.Id,
                        partToEdit.PartCode,
                        nameof(PartPrice.CostPrice),
                        previousCostPrice is null ? string.Empty : AuditService.FormatMoney(previousCostPrice.Value),
                        AuditService.FormatMoney(newCostPrice),
                        "Cost price changed by editing the part.");
                }

                foreach (var change in changedFields)
                {
                    _auditService.Record(
                        _aeroMechDBContext,
                        user,
                        AuditArea.Parts,
                        AuditAction.Updated,
                        nameof(Part),
                        partToEdit.Id,
                        partToEdit.PartCode,
                        $"{change.Field} changed by editing the part.",
                        change.Field,
                        change.OldValue,
                        change.NewValue);
                }

                await _aeroMechDBContext.SaveChangesAsync();
                return partToEdit.Id;
            }
        }

        /// <summary>
        /// A movement reads as a signed figure, because "adjusted by 5" and "adjusted by -5" are
        /// the two things a reader most needs to tell apart at a glance.
        /// </summary>
        internal static string Describe(int delta) => delta > 0 ? $"+{delta}" : delta.ToString();

        /// <summary>
        /// The Add Part screen has no warehouse picker - the shop runs a single site - so it sends
        /// a placeholder warehouse with id 0. Turn that into the id of a warehouse that actually
        /// exists: honour a real id if one is already set, otherwise match on the placeholder's
        /// code, and otherwise fall back to the only warehouse there is. A part cannot be stocked
        /// nowhere, so the absence of any warehouse is stated plainly rather than left to surface
        /// as a foreign key violation.
        /// </summary>
        private static async Task<int> ResolveWarehouseId(AeroMechDBContext context, AeroMech.Data.Models.Part part)
        {
            if (part.WarehouseId != 0)
                return part.WarehouseId;

            var code = part.Warehouse?.WarehouseCode;

            if (!string.IsNullOrWhiteSpace(code))
            {
                var byCode = await context.Warehouse
                    .Where(x => x.WarehouseCode == code)
                    .OrderBy(x => x.Id)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync();

                if (byCode is not null)
                    return byCode.Value;
            }

            var firstWarehouseId = await context.Warehouse
                .OrderBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (firstWarehouseId is not null)
                return firstWarehouseId.Value;

            throw new InvalidOperationException("No warehouse is set up to add the part to.");
        }
    }
}
