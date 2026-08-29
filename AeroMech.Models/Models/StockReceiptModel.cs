using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models.Models
{
    /// <summary>
    /// A supplier invoice being received, or one already posted. Carries both the totals as
    /// printed on the invoice and the lines actually captured, so the two can be held against
    /// each other before anything is written to stock.
    /// </summary>
    public class StockReceiptModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Supplier is required")]
        public string SupplierCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Invoice Number is required")]
        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTimeOffset InvoiceDate { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset ReceivedDate { get; set; } = DateTimeOffset.UtcNow;

        public string? ReceivedBy { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Sub Total cannot be negative")]
        public double InvoiceSubTotal { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "VAT cannot be negative")]
        public double InvoiceVat { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Invoice Total cannot be negative")]
        public double InvoiceTotal { get; set; }

        public string? Notes { get; set; }

        /// <summary>
        /// Whether the invoice prices should become the parts' cost prices. On by default: the
        /// invoice is the most recent word on what a part costs, so receiving is the natural
        /// moment to bring costing up to date.
        /// </summary>
        public bool UpdateCostPrices { get; set; } = true;

        /// <summary>
        /// The captured rows. On the receiving screen this is every part the supplier stocks, and
        /// only those with a quantity are posted; on a posted receipt it is just the lines taken in.
        /// </summary>
        public List<StockReceivingLineModel> Lines { get; set; } = new();

        public IEnumerable<StockReceivingLineModel> InvoicedLines => Lines.Where(x => x.IsOnInvoice);

        public int LineCount => InvoicedLines.Count();

        public int TotalQtyReceived => InvoicedLines.Sum(x => x.QtyReceived);

        /// <summary>
        /// What the captured lines add up to, before VAT, for comparison against
        /// <see cref="InvoiceSubTotal"/>.
        /// </summary>
        public double CapturedLineTotal => InvoicedLines.Sum(x => x.LineTotal);

        /// <summary>
        /// How far the captured lines sit from the invoice sub total. Positive means more was
        /// captured than the supplier billed.
        /// </summary>
        public double SubTotalVariance => Math.Round(CapturedLineTotal - InvoiceSubTotal, 2);

        /// <summary>
        /// Treated as reconciled within a cent, so rounding on the supplier's side does not read
        /// as a discrepancy.
        /// </summary>
        public bool IsReconciled => Math.Abs(SubTotalVariance) < 0.01;

        /// <summary>
        /// Whether the sub total and VAT captured agree with the invoice total. Checked separately
        /// from the lines, because a mistyped total is a different mistake from a mistyped line.
        /// </summary>
        public bool TotalsAddUp => Math.Abs(Math.Round(InvoiceSubTotal + InvoiceVat - InvoiceTotal, 2)) < 0.01;
    }
}
