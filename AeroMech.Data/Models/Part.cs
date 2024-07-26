using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	public class Part
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string PartCode { get; set; } = "";

		[Required]
		public string PartDescription { get; set; } = "";

		public bool IsDeleted { get; set; }

		public virtual List<PartPrice>? Prices { get; set; }

		public string? Bin { get; set; }

		public string? SupplierCode { get; set; }
		public string? ProductClass { get; set; }
		public int CycleCount { get; set; }
		
		public virtual Warehouse? Warehouse { get; set; }
		public int WarehouseId { get; set; }
		public int QtyOnHand { get; set; }
	}
}
