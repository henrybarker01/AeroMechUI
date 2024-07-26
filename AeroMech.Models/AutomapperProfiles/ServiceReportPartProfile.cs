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
				//.ForMember(x=>x.)
				
				;
		 
			CreateMap<ServiceReportPartModel, ServiceReportPart>()
				.ForMember(x=>x.PartId, opt => opt.MapFrom(e=>e.Id))			 
			;
  
		}
	}
}
