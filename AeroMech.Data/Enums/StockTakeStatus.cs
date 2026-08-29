namespace AeroMech.Data.Enums
{
    /// <summary>
    /// Where a stock take has got to. The numbers are fixed rather than positional because the
    /// status is stored as an integer, so a stage inserted later must not renumber the rest.
    /// </summary>
    public enum StockTakeStatus
    {
        /// <summary>
        /// Raised and its count sheet ready, but nothing counted yet.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// At least one quantity has been captured. Reached on the first save rather than by a
        /// button, so a sheet left half done still reads as being worked on.
        /// </summary>
        Counting = 1,

        /// <summary>
        /// Posted. The counted quantities are the stock levels and the ledger has been written.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Counting is finished and the differences are being settled - each one either accepted
        /// or sent back to be counted again.
        /// </summary>
        Review = 3,

        /// <summary>
        /// Abandoned without posting. Kept rather than deleted so a count that was started and
        /// dropped still shows why the stock was never adjusted.
        /// </summary>
        Cancelled = 4
    }
}
