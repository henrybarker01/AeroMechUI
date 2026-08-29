using AeroMech.Data.Enums;

namespace AeroMech.Data.Models
{
    /// <summary>
    /// One physical count of the shelves. The sheet is drawn up against a chosen set of supplier
    /// codes, and every part in scope gets a line the moment it is created - which is what fixes
    /// the stock levels the count will be judged against. Nothing here moves stock; that happens
    /// once, when the sheet is posted.
    /// </summary>
    public class StockTake : BaseModel
    {
        /// <summary>
        /// The number written at the top of the printed sheet. Paper comes back days later with
        /// no other way to tell which count it belongs to, so the sheet needs a name a person can
        /// read and quote.
        /// </summary>
        public int StockTakeNumber { get; set; }

        public DateTimeOffset StockTakeDate { get; set; }

        public string? StockTakeDescription { get; set; }

        public string? Remarks { get; set; }

        public StockTakeStatus Status { get; set; }

        public string? StockTakeBy { get; set; }

        public DateTimeOffset? CompletedDate { get; set; }

        public string? CompletedBy { get; set; }

        /// <summary>
        /// Whether the counter is kept from seeing the system quantity while capturing. On by
        /// default: shown the expected figure, a counter tends to confirm it rather than count,
        /// and a stock take that only ever agrees with the system has proved nothing.
        /// </summary>
        public bool BlindCount { get; set; } = true;

        public virtual List<StockTakeParts> StockTakeParts { get; set; } = new();
    }
}
