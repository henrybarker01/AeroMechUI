using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	public class Warehouse
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string WarehouseCode { get; set; }

		public string? WarehouseName { get; set; }
	}
}
