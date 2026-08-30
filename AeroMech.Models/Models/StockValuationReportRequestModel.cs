namespace AeroMech.Models.Models
{
    /// <summary>
    /// The scope of a stock valuation. There is no date on it: the report values what is on the
    /// shelf now, at the cost price the part carries now, which is the figure the stock is worth
    /// today rather than the figure it was worth at some point in the past.
    /// </summary>
    public class StockValuationReportRequestModel
    {
        /// <summary>
        /// The suppliers to value. Empty means every supplier.
        /// </summary>
        public List<string> SupplierCodes { get; set; } = new();

        /// <summary>
        /// Leaves out parts with nothing on the shelf. On by default, because a part at zero
        /// contributes nothing to a valuation and only lengthens it.
        /// </summary>
        public bool ExcludeZeroQtyParts { get; set; } = true;

        /// <summary>
        /// Prints only the supplier subtotals and the grand total, leaving the part lines off.
        /// What is wanted when the question is what the stock is worth rather than what it is.
        /// </summary>
        public bool SummaryOnly { get; set; }

        public bool IsAllSuppliers => SupplierCodes.Count == 0;
    }
}
