using AeroMech.Data.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class WarehouseProfile : Profile
	{
		public WarehouseProfile()
		{
			CreateMap<Warehouse, WarehouseModel>();
			
			CreateMap<WarehouseModel, Warehouse>();
		}
	}
}
