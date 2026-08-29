namespace AeroMech.Models.Models
{
    /// <summary>
    /// One captured figure on its way back to the database. Carries the line rather than the part
    /// because the same part can sit on more than one open sheet.
    /// </summary>
    public class StockTakeCountEntryModel
    {
        public int LineId { get; set; }

        /// <summary>
        /// What was counted, or null to put the line back to uncounted. Null is a real value here:
        /// it is how a figure entered by mistake is taken back off without becoming a zero.
        /// </summary>
        public int? Quantity { get; set; }

        public string? Remarks { get; set; }
    }
}
