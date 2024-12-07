using System.ComponentModel;

namespace AeroMech.Models.Enums
{
	public enum TimeGapType
	{
		Admin,
		[Description("Cleaning/Misc Services")]
		CleaningMiscServices,
		[Description("Family Responsibility Leave")]
		FamilyResponsibilityLeave,
		General,
		[Description("Injured on Duty")]
		InjuredOnDuty,
		Leave,
		[Description("Loading/Offloading")]
		LoadingOffloading,
		Meeting,
		Procurement,
		[Description("Public Holiday")]
		PublicHoliday,
		Safety,
		[Description("Sick Leave")]
		SickLeave,
		Standby,
		[Description("Stock Take")]
		StockTake,
		[Description("Study Leave")]
		StudyLeave,
		Training,
		Traveling
	}
}
