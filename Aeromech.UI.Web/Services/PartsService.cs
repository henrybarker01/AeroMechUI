using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
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
                    EffectiveDate = DateTime.Now,
                    IsDeleted = false,
                    SellingPrice = 0
                }};

                _aeroMechDBContext.Parts.Add(prt);
                await _aeroMechDBContext.SaveChangesAsync();
                return prt.Id;
            }
            else
            {
                Part partToEdit = await _aeroMechDBContext.Parts
                    .Include(x => x.Prices)
                    .SingleAsync(x => x.Id == part.Id);

                partToEdit.PartCode = part.PartCode;
                partToEdit.PartDescription = part.PartDescription;
                partToEdit.Bin = part.Bin;
                partToEdit.CycleCount = part.CycleCount;
                partToEdit.Warehouse = null;
                partToEdit.SupplierCode = part.SupplierCode;
                partToEdit.QtyOnHand = part.QtyOnHand;
                partToEdit.ProductClass = part.ProductClass;

                if (partToEdit.Prices == null || partToEdit.Prices.Count == 0)
                {
                    partToEdit.Prices = new List<PartPrice>() { new PartPrice() {
                        CostPrice = Convert.ToDouble(part.CostPrice),
                        EffectiveDate = DateTime.Now,
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
