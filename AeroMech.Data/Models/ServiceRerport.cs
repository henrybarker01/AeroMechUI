using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	public class ServiceReport
	{
		[Key]
		public int Id { get; set; }

		public DateTime ReportDate { get; set; }

		public string? SalesOrderNumber { get; set; }

		public string? JobNumber { get; set; }

		//[Required]
		//public string ReportNumber { get; set; }

		public string? Description { get; set; }

		public virtual Client? Client { get; set; }
		public int? ClientId { get; set; }

		public Vehicle? Vehicle { get; set; }
		public int? VehicleId { get; set; }

		public virtual List<ServiceReportEmployee> Employees { get; set; }

		public virtual List<ServiceReportPart> Parts { get; set; }

		public string? Instruction { get; set; }

		public string? DetailedServiceReport { get; set; }

		public bool IsDeleted { get; set; }

		public string? ServiceType { get; set; }

		public int? VehicleHours { get; set; }

		public int? QuoteNumber { get; set; }

		public bool IsComplete { get; set; }
	}
}
