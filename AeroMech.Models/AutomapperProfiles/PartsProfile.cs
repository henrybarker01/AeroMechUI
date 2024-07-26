using AeroMech.Data.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class PartsProfile : Profile
	{
		public PartsProfile()
		{
			CreateMap<Part, PartModel>()
				.ForMember(x => x.Warehouse, opt => opt.MapFrom(x => x.Warehouse))
				.ForMember(x => x.CostPrice, opt => opt.MapFrom(x => x.Prices == null ? 0 : x.Prices.First().CostPrice));
			CreateMap<PartModel, Part>()
				.ForMember(x => x.WarehouseId, opt => opt.MapFrom(prt => prt.Warehouse!.Id));
		}
	}
}
