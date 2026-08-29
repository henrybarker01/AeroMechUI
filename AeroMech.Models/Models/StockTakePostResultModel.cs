namespace AeroMech.Models.Models
{
    /// <summary>
    /// What posting a count sheet actually did. Reported back rather than assumed, because the
    /// figures the screen was showing and the figures that were written can differ - stock moves
    /// while a sheet is open, and posting works against what it finds.
    /// </summary>
    public class StockTakePostResultModel
    {
        public int StockTakeId { get; set; }

        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Lines whose count disagreed with the system and so moved stock. Lines that agreed are
        /// not counted here: they were settled without anything needing to change.
        /// </summary>
        public int LinesAdjusted { get; set; }

        public int UnitsAdded { get; set; }

        public int UnitsRemoved { get; set; }

        public double ValueAdjustment { get; set; }

        /// <summary>
        /// Parts on the sheet nobody counted. Their stock was left exactly as it was.
        /// </summary>
        public int LinesNotCounted { get; set; }

        /// <summary>
        /// Parts that moved between the sheet being raised and being posted. The correction was
        /// still applied as a difference rather than as a replacement level, so the movement
        /// survives - but it is called out because it means the count was taken against a level
        /// that has since changed.
        /// </summary>
        public int LinesMovedDuringCount { get; set; }
    }
}
