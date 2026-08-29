using AeroMech.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	/// <summary>
	/// A line of estimated labour on a <see cref="Quote"/>. Carries no employee: a quote prices
	/// the hours by rate type, and the actual people are only named when the quote is converted
	/// into a service report.
	/// </summary>
	public class QuoteLabour
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public virtual Quote? Quote { get; set; }

		[Required]
		public int QuoteId { get; set; }

		public RateType RateType { get; set; }

		public double Rate { get; set; }

		public double Hours { get; set; }

		public double Discount { get; set; }

		public bool IsDeleted { get; set; }
	}
}
