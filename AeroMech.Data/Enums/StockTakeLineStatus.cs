namespace AeroMech.Data.Enums
{
    /// <summary>
    /// Where one line of a stock take has got to. Kept apart from the sheet's own status because
    /// a single sheet is normally part counted, part settled and part still to do.
    /// </summary>
    public enum StockTakeLineStatus
    {
        /// <summary>
        /// Nobody has counted this part yet. Deliberately distinct from a count of zero: an empty
        /// shelf is a finding, an uncounted shelf is not, and posting treats them differently.
        /// </summary>
        NotCounted = 0,

        /// <summary>
        /// A quantity has been captured. Where it matches the snapshot there is nothing left to
        /// decide; where it does not, the line waits for review.
        /// </summary>
        Counted = 1,

        /// <summary>
        /// The difference was not believed and the part goes back to be counted again. The
        /// rejected figure is kept on the line so the second count can be read against the first.
        /// </summary>
        RecountRequested = 2,

        /// <summary>
        /// The difference was reviewed and taken as correct. The agreed figure is what posting
        /// writes to stock.
        /// </summary>
        Accepted = 3
    }
}
