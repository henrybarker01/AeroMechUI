using AeroMech.Data.Models;
using AeroMech.Models.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class ServiceReportAdHockPartProfile : Profile
	{
		public ServiceReportAdHockPartProfile()
		{
			CreateMap<ServiceReportAdHockPart, ServiceReportAdHockPartModel>();


			CreateMap<ServiceReportAdHockPartModel, ServiceReportAdHockPart>();

		}
	}
}
