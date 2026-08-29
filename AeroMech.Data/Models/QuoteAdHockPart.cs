using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	/// <summary>
	/// A part quoted by hand because it is not carried in stock, so it has no
	/// <see cref="Part"/> behind it.
	/// </summary>
	public class QuoteAdHockPart
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public virtual Quote? Quote { get; set; }

		[Required]
		public int QuoteId { get; set; }

		public string PartCode { get; set; } = "";
		public string PartDescription { get; set; } = "";
		public double CostPrice { get; set; }
		public double Discount { get; set; }
		public int Qty { get; set; }
		public bool IsDeleted { get; set; }
	}
}
