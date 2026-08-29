using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	/// <summary>
	/// A stocked part priced on a <see cref="Quote"/>. Quoting a part never writes a
	/// <see cref="StockAdjustment"/>; the stock only moves if the quote becomes a service report.
	/// </summary>
	public class QuotePart
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public virtual Quote? Quote { get; set; }

		[Required]
		public int QuoteId { get; set; }

		public virtual Part? Part { get; set; }

		[Required]
		public int PartId { get; set; }

		public double CostPrice { get; set; }
		public double Discount { get; set; }
		public int Qty { get; set; }
		public bool IsDeleted { get; set; }
	}
}
