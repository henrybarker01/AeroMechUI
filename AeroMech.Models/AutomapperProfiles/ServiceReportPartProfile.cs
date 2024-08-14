using AeroMech.Data.Models;
using AeroMech.Models.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class ServiceReportPartsProfile : Profile
	{
		public ServiceReportPartsProfile()
		{
			CreateMap<ServiceReportPart, ServiceReportPartModel>()
					.ForMember(x => x.CostPrice, opt => opt.MapFrom(e => e.Part.Prices.Single().CostPrice))

					.ForMember(x => x.PartCode, opt => opt.MapFrom(e => e.Part.PartCode))
					.ForMember(x => x.PartDescription, opt => opt.MapFrom(e => e.Part.PartDescription));


			CreateMap<ServiceReportPartModel, ServiceReportPart>()
				.ForMember(x => x.PartId, opt => opt.MapFrom(e => e.Id))
			;

		}
	}
}
