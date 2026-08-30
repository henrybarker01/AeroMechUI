namespace AeroMech.Models.Models
{
    /// <summary>
    /// The period and scope a stock movement report covers.
    ///
    /// Both ends of the period are answerable because every path that changes
    /// <c>Part.QtyOnHand</c> also writes a <c>StockAdjustment</c>: the level on any past date is
    /// today's level with the movements since then unwound. So an end date in the past still
    /// yields a real opening and closing quantity, not just a list of movements.
    /// </summary>
    public class StockMovementReportRequestModel
    {
        public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

        public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        /// <summary>
        /// The suppliers to report on. Empty means every supplier, including parts carrying no
        /// supplier code at all - they move the same way as everything else.
        /// </summary>
        public List<string> SupplierCodes { get; set; } = new();

        /// <summary>
        /// Prints a part that did not move in the period as a single opening-equals-closing line.
        /// Off by default: a movement report answers what moved, and the dormant tail of the
        /// parts list would bury it.
        /// </summary>
        public bool IncludePartsWithNoMovement { get; set; }

        public bool IsAllSuppliers => SupplierCodes.Count == 0;
    }
}
