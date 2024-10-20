namespace AeroMech.Models.Models
{
	public class ServiceReportAdHockPartModel
	{
		public int Id { get; set; }
		public int ServiceReportId { get; set; }
		public string PartDescription { get; set; } = "";
		public string PartNumber { get; set; } = "";
		public double CostPrice { get; set; }
		public double Discount { get; set; }
		public int Qty { get; set; }
		public bool IsDeleted { get; set; }
	}
}
