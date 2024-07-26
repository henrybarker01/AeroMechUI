using AeroMech.Data.Models;
using AeroMech.Models.Enums;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
    public class ServiceReportProfile : Profile
    {
        public ServiceReportProfile()
        {
            CreateMap<ServiceReport, ServiceReportModel>()
                .ForMember(x => x.Parts, opt => opt.MapFrom(x => x.Parts))
                .ForMember(x => x.Employees, opt => opt.MapFrom(x => x.Employees))
                .ForMember(x => x.ServiceType, opt => opt.MapFrom(s => Enum.Parse<ServiceType>(s.ServiceType)))
                .ForMember(x => x.Client, opt => opt.MapFrom(c => c.Client))
                ;

            CreateMap<ServiceReportModel, ServiceReport>()
                .ForMember(x => x.Client, opt => opt.Ignore())
                .ForMember(x => x.Parts, opt => opt.MapFrom(x => x.Parts))
                .ForMember(x => x.Employees, opt => opt.MapFrom(x => x.Employees));
            // .ForMember(x => x.ServiceType, opt => opt.MapFrom(s => s.ServiceType.ToString()));

        }
    }
}
