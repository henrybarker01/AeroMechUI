using AeroMech.Data.Models;
using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class ServiceReportProfile : Profile
	{
		public ServiceReportProfile()
		{
			CreateMap<ServiceReport, ServiceReportModel>()
				 .ForMember(x => x.Parts, opt => opt.MapFrom(x => x.Parts))
				 .AfterMap((s, d) =>
				 {
					 if (s.AdHockParts != null)
					 {
						 d.Parts.AddRange(s.AdHockParts.Select(a => new ServiceReportPartModel()
						 {
							 QTY = a.Qty,
							 PartDescription = a.PartDescription,
							 Bin = "",
							 CostPrice = a.CostPrice,
							 CycleCount = 0,
							 Discount = a.Discount,
							 Id = a.Id,
							 IsAdHockPart = true,
							 PartCode = string.IsNullOrEmpty(a.PartCode) ? "AdHock" : a.PartCode,
							 ProductClass = "",
							 QtyOnHand = 0,
							 SellingPrice = a.CostPrice,
							 SupplierCode = "",
							 IsDeleted = a.IsDeleted
							
						 }));
					 }
				 })

				.ForMember(x => x.Employees, opt => opt.MapFrom(x => x.Employees))
				.ForMember(x => x.ServiceType, opt => opt.MapFrom(s => Enum.Parse<ServiceType>(s.ServiceType)))
				.ForMember(x => x.Client, opt => opt.MapFrom(c => c.Client));

			CreateMap<ServiceReportModel, ServiceReport>()
				.ForMember(x => x.Client, opt => opt.Ignore())
				.ForMember(x => x.Parts, opt => opt.MapFrom(x => x.Parts))
				.ForMember(x => x.Employees, opt => opt.MapFrom(x => x.Employees));
			// .ForMember(x => x.ServiceType, opt => opt.MapFrom(s => s.ServiceType.ToString()));

		}
	}
}
