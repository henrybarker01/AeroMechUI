using AeroMech.Data.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class EmployeesProfile : Profile
	{
		public EmployeesProfile()
		{
			CreateMap<Employee, EmployeeModel>()
				.ForMember(x => x.AddressLine1, opt => opt.MapFrom(x => x.Address.AddressLine1))
				.ForMember(x => x.AddressLine2, opt => opt.MapFrom(x => x.Address.AddressLine2))
				.ForMember(x => x.City, opt => opt.MapFrom(x => x.Address.City))
				.ForMember(x => x.PostalCode, opt => opt.MapFrom(x => x.Address.PostalCode))
				.ForMember(x => x.AddressId, opt => opt.MapFrom(x => x.Address.Id))
				.ForMember(x => x.Rate, opt => opt.MapFrom(x => x.Rates.First().Rate));

			CreateMap<EmployeeModel, Employee>()
				.ForMember(x => x.Address, opt => opt.MapFrom(emp => new Address()
				{
					AddressLine1 = emp.AddressLine1,
					AddressLine2 = emp.AddressLine2,
					City = emp.City,
					PostalCode = emp.PostalCode,
					Id = emp.AddressId ?? 0

				}))
				.ForMember(x => x.Rates, opt => opt.MapFrom(e =>

					new List<EmployeeRateModel>
					{
						new EmployeeRateModel()
							{
								Rate = e.Rate ?? 0,
								EffectiveDate = DateTime.Now,
								EmployeeId = e.Id,
								IsActive = true
							}
					}
				));

			CreateMap<EmployeeRateModel, EmployeeRate>();
			CreateMap<EmployeeRate, EmployeeRateModel>();
		}
	}
}
