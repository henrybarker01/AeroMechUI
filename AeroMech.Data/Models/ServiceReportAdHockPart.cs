using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	public class ServiceReportAdHockPart
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public virtual ServiceReport? ServiceReport { get; set; }

		[Required]
		public int ServiceReportId { get; set; }
		public string PartCode { get; set; }
		public string PartDescription { get; set; } = "";
		public double CostPrice { get; set; }
		public double Discount { get; set; }
		public int Qty { get; set; }
		public bool IsDeleted { get; set; }
	}
}
