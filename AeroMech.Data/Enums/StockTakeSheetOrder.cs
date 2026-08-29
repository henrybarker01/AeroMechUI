namespace AeroMech.Data.Enums
{
    /// <summary>
    /// How a count sheet is ordered. Both orders list the same parts; the choice is about which
    /// walk the counter is actually doing.
    /// </summary>
    public enum StockTakeSheetOrder
    {
        /// <summary>
        /// Supplier code, then part number. How the stock is organised on paper and in the system.
        /// </summary>
        SupplierThenPart = 0,

        /// <summary>
        /// Bin, then part number. How the stock is organised on the shelves, so the sheet reads in
        /// the order the counter physically walks rather than sending them back and forth.
        /// </summary>
        BinThenPart = 1
    }
}
