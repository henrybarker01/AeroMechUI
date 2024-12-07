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
        private readonly AeroMechDBContext _aeroMechDBContext;
        public PartsService(AeroMechDBContext context, IMapper mapper)
        {
            _aeroMechDBContext = context;
            _mapper = mapper;
        }

        public async Task<List<PartModel>> GetParts()
        {
            List<AeroMech.Data.Models.Part> parts = await _aeroMechDBContext.Parts.Where(x => x.IsDeleted == false)
                .Include(a => a.Warehouse)
                .Include(p => p.Prices)
                .OrderBy(x=>x.PartCode).ThenBy(x=>x.PartDescription)
                .ToListAsync();
            return _mapper.Map<List<PartModel>>(parts);
        }

        public async Task DeletePart(PartModel prt)
        {
            var part = await _aeroMechDBContext.Parts.FindAsync(prt.Id);
            part.IsDeleted = true;
            await _aeroMechDBContext.SaveChangesAsync();
        }

        public async Task<int> AddNewPart(PartModel part)
        {
            if (part.Id == 0)
            {
                AeroMech.Data.Models.Part prt = _mapper.Map<AeroMech.Data.Models.Part>(part);
                prt.Prices = new List<PartPrice>() { new PartPrice() {
                CostPrice = part.CostPrice,
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
                AeroMech.Data.Models.Part partToEdit = _aeroMechDBContext.Parts
                 .Include(x => x.Prices)
                 .Single(x => x.Id == part.Id);

                partToEdit.PartCode = part.PartCode;
                partToEdit.PartDescription = part.PartDescription;
                partToEdit.Bin = part.Bin;
                partToEdit.CycleCount = part.CycleCount;
                partToEdit.Warehouse = _mapper.Map<Warehouse>(part.Warehouse);
                partToEdit.SupplierCode = part.SupplierCode;
                partToEdit.QtyOnHand = part.QtyOnHand;
                partToEdit.ProductClass = part.ProductClass;

                if (partToEdit.Prices == null || partToEdit.Prices.Count == 0)
                {
                    partToEdit.Prices = new List<PartPrice>() { new PartPrice() {
                    CostPrice = part.CostPrice,
                    EffectiveDate = DateTime.Now,
                    IsDeleted = false,
                    SellingPrice = 0
                    }};
                }
                else
                {
                    partToEdit.Prices.First().CostPrice = part.CostPrice;
                }

                await _aeroMechDBContext.SaveChangesAsync();
                return partToEdit.Id;
            }
        }
    }
}
