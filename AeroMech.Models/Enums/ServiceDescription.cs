using System.ComponentModel;

namespace AeroMech.Models.Enums
{
	public enum ServiceDescription
	{
		[Description("Accident Repair")]
		AccidentRepair,
		[Description("Call out")]
		CallOut,
		Commissioning,
		[Description("Mis-use Repair")]
		MisUseRepair,
		Modification,
		[Description("Normal Repair")]
		NormalRepair,
		Rebuild,
		Service,
		[Description("Warranty Repair")]
		WarrantyRepair,
	}
}
