using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models.Enums
{
    public enum RateType
    {
        None = 0,
        Overtime = 1,
        [Display(Name = "Sundays & Public Holidays")]
        SundaysAndPublicHolidays = 2,
        Weekdays = 3,
        [Display(Name = "Weekdays Overtime")]
        WeekdaysOvertime = 4
    }
}
