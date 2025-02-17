using AeroMech.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AeroMech.Data.Models
{
	public class ClientRate
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public DateTime EffectiveDate { get; set; }

		[Required]
		[Column(TypeName = "decimal(8, 2)")]
		public decimal Rate { get; set; }

		public virtual Client Client { get; set; }

		[Required]
		public int ClientId { get; set; }

		[Required]
		public RateType RateType { get; set; }

        public bool IsActive { get; set; }
	}
}
