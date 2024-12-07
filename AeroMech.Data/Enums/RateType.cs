using System.ComponentModel;

namespace AeroMech.Models.Enums
{
	public enum RateType
	{
		Overtime,
		[Description("Sundays & Public Holidays")]
		SundaysAndPublicHolidays,
		Weekdays,
		[Description("Weekdays Overtime")]
		WeekdaysOvertime
	}
}
