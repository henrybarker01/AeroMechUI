using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AeroMech.Data.Models
{
	public class PartPrice
	{
		[Key]
		public int Id { get; set; }

		public virtual Part? Part { get; set; }

		[Required]
		public int PartId { get; set; }

		[Required]
		[Column(TypeName = "decimal(8, 2)")]
		public double CostPrice { get; set; }

		[Required]
		[Column(TypeName = "decimal(8, 2)")]
		public double SellingPrice { get; set; }

		[Required]
		public DateTime EffectiveDate { get; set; }

		public bool IsDeleted { get; set; }
	}
}
