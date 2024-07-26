using AeroMech.Data.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class ServiceReportEmployeesProfile : Profile
	{
		public ServiceReportEmployeesProfile()
		{
			CreateMap<ServiceReportEmployee, ServiceReportEmployeeModel>()
				//.ForMember(x=>x.)
				
				;
		 
			CreateMap<ServiceReportEmployeeModel, ServiceReportEmployee>()
				.ForMember(x=>x.EmployeeId, opt => opt.MapFrom(e=>e.Id))
			 
			;
 
 
		}
	}
}
