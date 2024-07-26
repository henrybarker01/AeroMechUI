using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AeroMech.Data.Models
{
	public class EmployeeRate
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public DateTime EffectiveDate { get; set; }

		[Required]
		[Column(TypeName = "decimal(5, 2)")]
		public decimal Rate { get; set; }

		public virtual Employee Employee { get; set; }

		[Required]
		public int EmployeeId { get; set; }

		public bool IsActive { get; set; }
	}
}
