
namespace AeroMech.Models
{
	public class ServiceReportEmployeeModel : EmployeeModel
	{
		
		public double? Hours { get; set; }

		public double? Discount { get; set; }

		public string ServceType { get; set; } = "Service";

		public DateOnly DutyDate { get; set; }
	}
}
