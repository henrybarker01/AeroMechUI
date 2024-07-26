using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	public class ServiceReport
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public DateTime ReportDate { get; set; }

		[Required]
		public string SalesOrderNumber { get; set; }

		[Required]
		public int JobNumber { get; set; }

		//[Required]
		//public string ReportNumber { get; set; }

		[Required]
		public string Description { get; set; }

		[Required]
		public virtual Client Client { get; set; }
		public int ClientId { get; set; }

		[Required]
		public virtual Vehicle Vehicle { get; set; }
		public int VehicleId { get; set; }

		[Required]
		public virtual List<ServiceReportEmployee> Employees { get; set; }

		[Required]
		public virtual List<ServiceReportPart> Parts { get; set; }

		public string Instruction { get; set; }

		public string DetailedServiceReport { get; set; }

		public bool IsDeleted { get; set; }

        public string ServiceType { get; set; }
    }
}
