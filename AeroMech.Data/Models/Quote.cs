using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	/// <summary>
	/// A priced estimate given to a client before any work is done. Deliberately not a
	/// <see cref="ServiceReport"/>: a quote has no service report number, never moves stock and
	/// never contributes hours to a timesheet. Once the client accepts it, the quote is converted
	/// into a service report, and only then does the work become real. Some clients work the other
	/// way round, and their quote is raised from the service report instead - see
	/// <see cref="CreatedFromServiceReport"/>.
	/// </summary>
	public class Quote
	{
		[Key]
		public int Id { get; set; }

		public int QuoteNumber { get; set; }

		public DateTimeOffset QuoteDate { get; set; }

		public string? Description { get; set; }

		public virtual Client? Client { get; set; }
		public int? ClientId { get; set; }

		public virtual Vehicle? Vehicle { get; set; }
		public int? VehicleId { get; set; }

		/// <summary>
		/// Estimated labour, held per rate type rather than per employee. Who does the work is
		/// not known when the quote is written, and is captured on conversion instead.
		/// </summary>
		public virtual List<QuoteLabour> Labour { get; set; } = new();

		public virtual List<QuotePart> Parts { get; set; } = new();

		public virtual List<QuoteAdHockPart> AdHockParts { get; set; } = new();

		public string? Instruction { get; set; }

		public string? DetailedServiceReport { get; set; }

		public string? ServiceType { get; set; }

		public int? VehicleHours { get; set; }

		/// <summary>
		/// The service report this quote was turned into, once the client accepted it - or, for a
		/// quote <see cref="CreatedFromServiceReport"/>, the report it was raised from. A quote
		/// with a service report is read only from that point on.
		/// </summary>
		public virtual ServiceReport? ServiceReport { get; set; }

		/// <summary>
		/// When the quote was converted into its service report. Left empty on a quote
		/// <see cref="CreatedFromServiceReport"/>, which never was.
		/// </summary>
		public DateTimeOffset? ConvertedDate { get; set; }

		/// <summary>
		/// Set when the quote was raised from a service report rather than written up front, for
		/// clients who only want the numbers once the work is done. Such a quote is a copy of the
		/// report and is rewritten from it each time the report is printed as a quote. A quote the
		/// report was converted from is the record of what the client accepted, and never is.
		/// </summary>
		public bool CreatedFromServiceReport { get; set; }

		public bool IsDeleted { get; set; }
	}
}
