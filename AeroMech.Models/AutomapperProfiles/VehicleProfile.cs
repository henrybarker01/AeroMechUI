using AeroMech.Data.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class VehicleProfile : Profile
	{
		public VehicleProfile()
		{
			CreateMap<Vehicle, VehicleModel>();
			CreateMap<VehicleModel, Vehicle>();
		}
	}
}
