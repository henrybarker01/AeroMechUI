using AeroMech.Data.Models;
using AeroMech.Models.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
    public class TimesheetEmployeeDetailProfile : Profile
    {
        public TimesheetEmployeeDetailProfile()
        {
            CreateMap<TimesheetEmployeeDetail, TimesheetEmployeeDetailModel>()
                .ReverseMap();
        }
    }
}
