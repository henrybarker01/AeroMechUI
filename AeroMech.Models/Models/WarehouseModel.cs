using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models
{
	public class WarehouseModel
	{ 
		public int Id { get; set; }

		[Required(ErrorMessage = "Warehouse Code is required")]
		public string WarehouseCode { get; set; }

		public string? WarehouseName { get; set; }
	}
}
