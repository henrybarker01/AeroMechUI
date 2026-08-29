using AeroMech.Data.Models;
using AeroMech.Models.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
    public class StockTakeProfile : Profile
    {
        public StockTakeProfile()
        {
            // Only the read direction is mapped. Raising a sheet freezes stock levels and posting
            // one moves stock and writes the ledger, so both are done by hand in the service
            // where the transaction is.
            CreateMap<StockTake, StockTakeModel>()
                .ForMember(x => x.Lines, opt => opt.MapFrom(x => x.StockTakeParts));

            CreateMap<StockTakeParts, StockTakeLineModel>()
                .ForMember(x => x.PartCode, opt => opt.MapFrom(x => x.Part == null ? string.Empty : x.Part.PartCode))
                .ForMember(x => x.PartDescription, opt => opt.MapFrom(x => x.Part == null ? string.Empty : x.Part.PartDescription))
                .ForMember(x => x.ProductClass, opt => opt.MapFrom(x => x.Part == null ? null : x.Part.ProductClass))
                .ForMember(x => x.WarehouseCode, opt => opt.MapFrom(x => x.Warehouse == null ? null : x.Warehouse.WarehouseCode));
        }
    }
}
