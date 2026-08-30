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
        
        public PartsService(IDbContextFactory<AeroMechDBContext> contextFactory, IMapper mapper)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
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
                part.IsDeleted = true;
                await _aeroMechDBContext.SaveChangesAsync();
            }
        }

        public async Task<int> AddNewPart(PartModel part)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            if (part.Id == 0)
            {
                AeroMech.Data.Models.Part prt = _mapper.Map<AeroMech.Data.Models.Part>(part);
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
                        WarehouseId = prt.WarehouseId,
                        QTY = prt.QtyOnHand,
                        AdjustementDate = DateTimeOffset.UtcNow,
                        AdjustedById = new Guid(),
                        StockAdjustmentType = StockAdjustmentType.StockAdjustment
                    });
                }

                await _aeroMechDBContext.SaveChangesAsync();
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
                }

                if (partToEdit.Prices == null || partToEdit.Prices.Count == 0)
                {
                    partToEdit.Prices = new List<PartPrice>() { new PartPrice() {
                        CostPrice = Convert.ToDouble(part.CostPrice),
                        EffectiveDate = DateTimeOffset.UtcNow,
                        IsDeleted = false,
                        SellingPrice = 0
                    }};
                }
                else
                {
                    partToEdit.Prices.First().CostPrice = Convert.ToDouble(part.CostPrice);
                }

                await _aeroMechDBContext.SaveChangesAsync();
                return partToEdit.Id;
            }
        }
    }
}
