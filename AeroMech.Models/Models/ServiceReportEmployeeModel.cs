
using AeroMech.Models.Enums; 

namespace AeroMech.Models
{
	public class ServiceReportEmployeeModel : EmployeeModel
	{
		
		public double? Hours { get; set; }

		public double? Discount { get; set; }

		public string ServceType { get; set; } = "Service";

		public DateOnly DutyDate { get; set; } 
        public double Rate { get; set; }

        public RateType RateType { get; set; }

        public int EmployeeId { get; set; }
    }
}
