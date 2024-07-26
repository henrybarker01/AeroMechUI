using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	public class ServiceReportEmployee
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public virtual ServiceReport? ServiceReport { get; set; }

		[Required]
		public int? ServiceReportId { get; set; }
		 
		public virtual Employee? Employee { get; set; }

		[Required]
		public int EmployeeId { get; set; }

		public double Hours { get; set; }
		public double Rate { get; set; }

		public double Discount { get; set; }

	}
}
