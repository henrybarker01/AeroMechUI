using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	/// <summary>
	/// A supplier invoice that brought stock into the warehouse. Receiving is captured per
	/// supplier code because that is how the invoice arrives: one invoice covers the parts bought
	/// from one supplier, so the invoice is the document the received quantities are proved
	/// against. Posting a receipt is what moves stock - it raises <see cref="Part.QtyOnHand"/> and
	/// writes a <see cref="StockAdjustment"/> for every line, so the ledger stays the single
	/// record of why stock moved.
	/// </summary>
	public class StockReceipt : BaseModel
	{
		/// <summary>
		/// Copied from <see cref="Part.SupplierCode"/> rather than pointing at a supplier record,
		/// because supplier codes live on the parts themselves and there is no supplier table yet.
		/// </summary>
		[Required]
		public string SupplierCode { get; set; } = "";

		[Required]
		public string InvoiceNumber { get; set; } = "";

		public DateTimeOffset InvoiceDate { get; set; }

		/// <summary>
		/// When the stock was actually taken in, which is not always the day the supplier dated
		/// the invoice.
		/// </summary>
		public DateTimeOffset ReceivedDate { get; set; }

		public string? ReceivedBy { get; set; }

		/// <summary>
		/// The totals as printed on the supplier's invoice, kept exactly as captured. They are
		/// deliberately not derived from the lines: holding both is what lets the captured lines
		/// be reconciled against what the supplier actually billed.
		/// </summary>
		public double InvoiceSubTotal { get; set; }

		public double InvoiceVat { get; set; }

		public double InvoiceTotal { get; set; }

		public string? Notes { get; set; }

		public virtual List<StockReceiptLine> Lines { get; set; } = new();
	}
}
