using AeroMech.Data.Enums;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AeroMech.UI.Web.Services
{
    public class StockTakeService
    {
        private IMapper _mapper;
        private AeroMechDBContext _aeroMechDBContext;

        public StockTakeService(IMapper mapper, AeroMechDBContext aeroMechDBContext)
        {
            _aeroMechDBContext = aeroMechDBContext;
            _mapper = mapper;
        }

        public async Task<List<StockTakeModel>> GetStockTakes()
        {
            var stockTakes = await _aeroMechDBContext.StockTakes.AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(x => x.Warehouse)
                .ToListAsync();
            return _mapper.Map<List<StockTakeModel>>(stockTakes);
        }

        public async Task<List<StockTakeParts>> GetStockTakeParts(int stockTakeId)
        {
            var stockTakeParts = await _aeroMechDBContext.StockTakeParts.AsNoTracking()
                .Where(x => x.StockTakeId == stockTakeId)
                .Include(x => x.Part)
                .ToListAsync();
            return _mapper.Map<List<StockTakeParts>>(stockTakeParts);
        }

        public async Task<int> AddStockTake(StockTakeModel stockTake)
        {
            var newStockTake = new StockTake
            {
               // Id = stockTake.Id,
                StockTakeDate = stockTake.StockTakeDate,
                WarehouseId = stockTake.WarehouseId,
                Remarks = stockTake.Remarks,
               // CreatedBy = stockTake.CreatedBy,
                CreatedAt = DateTime.Now,
                Status = StockTakeStatus.Pending,
                Type = "StockTake",
                StockTakeBy = stockTake.StockTakeBy,
                StockTakeDescription = stockTake.StockTakeDescription,                
              //  CreatedDate = DateTime.Now,
                IsDeleted = false
            };
            _aeroMechDBContext.StockTakes.Add(newStockTake);
            await _aeroMechDBContext.SaveChangesAsync();
            return newStockTake.Id;
        }
    }
}
