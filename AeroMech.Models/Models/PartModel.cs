using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models
{
	public class PartModel
	{
		public int Id { get; set; }
		
		[Required(ErrorMessage = "Part Code is required")]
		public string PartCode { get; set; }

		[Required(ErrorMessage = "Part Description is required")]
		public string PartDescription { get; set; }

		[Required(ErrorMessage = "Cost Price is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Cost Price is required")]
        public double CostPrice { get; set; }

		[Required(ErrorMessage = "Selling Price is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Selling Price is required")]
        public double SellingPrice { get; set; }
		public string? Bin { get; set; }
		public string? SupplierCode { get; set; }
		public string? ProductClass { get; set; }

		[Required(ErrorMessage = "Cycle Count is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Cycle Count is required")]
        public int CycleCount { get; set; }
		public virtual WarehouseModel? Warehouse { get; set; }

		[Required(ErrorMessage = "Qty On Hand is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Qty On Hand is required")]
        public int QtyOnHand { get; set; }	
	}
}
