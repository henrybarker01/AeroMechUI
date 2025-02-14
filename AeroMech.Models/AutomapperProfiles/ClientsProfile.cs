using AeroMech.Data.Models;
using AeroMech.Models.Enums;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
    public class ClientsProfile : Profile
    {
        public ClientsProfile()
        {
            CreateMap<Client, ClientModel>()
                .ForMember(x => x.AddressLine1, opt => opt.MapFrom(x => x.Address.AddressLine1))
                .ForMember(x => x.AddressLine2, opt => opt.MapFrom(x => x.Address.AddressLine2))
                .ForMember(x => x.City, opt => opt.MapFrom(x => x.Address.City))
                .ForMember(x => x.PostalCode, opt => opt.MapFrom(x => x.Address.PostalCode))
                    .ForMember(x => x.AddressId, opt => opt.MapFrom(x => x.Address.Id));
                //.ForMember(x => x.Rates, opt => opt.MapFrom(x => x.Rates
                //                            .Where(r => r.EffectiveDate <= DateTime.Now)
                //                            .OrderByDescending(r => r.EffectiveDate)
                //                            .FirstOrDefault().Rate));


            CreateMap<ClientModel, Client>()
                .ForMember(x => x.Address, opt => opt.MapFrom(emp => new Address()
                {
                    AddressLine1 = emp.AddressLine1,
                    AddressLine2 = emp.AddressLine2,
                    City = emp.City,
                    PostalCode = emp.PostalCode,
                    Id = emp.AddressId

                })).ForMember(x => x.Rates, opt => opt.MapFrom(e =>
                    new List<ClientRateModel>
                    {
                        new ClientRateModel()
                            {
                                Rate = e.Rates.FirstOrDefault().Rate,
                                EffectiveDate = DateTime.Now,
                                ClientId = e.Id,
                                IsActive = true,
                                RateType =  e.Rates.FirstOrDefault().RateType,
                            }
                    }
                ));

            CreateMap<ClientRateModel, ClientRate>();
            CreateMap<ClientRate, ClientRateModel>();
        }
    }
}
