using AeroMech.Data.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class ServiceReportEmployeesProfile : Profile
	{
		public ServiceReportEmployeesProfile()
		{
			CreateMap<ServiceReportEmployee, ServiceReportEmployeeModel>()
				.ForMember(x => x.FirstName, opt => opt.MapFrom(e => e.Employee.FirstName))
				.ForMember(x => x.LastName, opt => opt.MapFrom(e => e.Employee.LastName));

			CreateMap<ServiceReportEmployeeModel, ServiceReportEmployee>()
				.ForMember(x => x.EmployeeId, opt => opt.MapFrom(e => e.Id));
		}
	}
}
