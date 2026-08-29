using AeroMech.Data.Enums;

namespace AeroMech.Models.Models
{
    /// <summary>
    /// A count sheet as the screens work with it. The scope is described by the lines themselves
    /// rather than held separately: the parts on the sheet are what was counted, so a supplier
    /// list derived from them can never disagree with what the count actually covered.
    /// </summary>
    public class StockTakeModel
    {
        public int Id { get; set; }

        public int StockTakeNumber { get; set; }

        public DateTimeOffset StockTakeDate { get; set; } = DateTimeOffset.UtcNow;

        public string? StockTakeDescription { get; set; }

        public string? Remarks { get; set; }

        public StockTakeStatus Status { get; set; }

        public string? StockTakeBy { get; set; }

        public DateTimeOffset? CompletedDate { get; set; }

        public string? CompletedBy { get; set; }

        public bool BlindCount { get; set; } = true;

        public List<StockTakeLineModel> Lines { get; set; } = new();

        /// <summary>
        /// How the sheet is referred to on paper and on screen.
        /// </summary>
        public string Reference => $"ST-{StockTakeNumber:0000}";

        /// <summary>
        /// The suppliers the sheet covers, read back off the lines.
        /// </summary>
        public List<string> SupplierCodes => Lines
            .Select(x => x.SupplierCode)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        public string SupplierLabel => SupplierCodes.Count switch
        {
            0 => "No suppliers",
            1 => SupplierCodes[0],
            _ => $"{SupplierCodes.Count} suppliers"
        };

        public int LineCount => Lines.Count;

        public int CountedCount => Lines.Count(x => x.IsCounted);

        public int NotCountedCount => Lines.Count(x => !x.IsCounted);

        public int RecountCount => Lines.Count(x => x.Status == StockTakeLineStatus.RecountRequested);

        public int VarianceCount => Lines.Count(x => x.HasVariance);

        /// <summary>
        /// Counted differences still waiting on a decision. The number review works down.
        /// </summary>
        public int NeedsDecisionCount => Lines.Count(x => x.NeedsDecision);

        public int AcceptedCount => Lines.Count(x => x.Status == StockTakeLineStatus.Accepted);

        public double TotalVarianceValue => Lines.Sum(x => x.VarianceValue);

        /// <summary>
        /// What the count found, in units gained less units lost, across every difference whether
        /// or not it has been settled yet. Pairs with <see cref="TotalVarianceValue"/>, which is
        /// the same set of lines measured in money - the two are shown side by side during review
        /// and would mislead if one counted settled lines only.
        /// </summary>
        public int NetUnitVariance => Lines.Sum(x => x.Variance);

        /// <summary>
        /// What the sheet would do to stock if posted now, which counts only the lines actually
        /// settled. Deliberately not the same as <see cref="NetUnitVariance"/>: this is the figure
        /// posting is confirmed against, so it has to reflect the decisions taken rather than the
        /// differences found.
        /// </summary>
        public int NetUnitAdjustment => Lines.Sum(x => x.PendingDelta);

        public int ProgressPercent => LineCount == 0 ? 0 : (int)Math.Round(CountedCount * 100.0 / LineCount);

        /// <summary>
        /// A sheet can only be posted once every difference has been settled and no recount is
        /// outstanding. Parts nobody counted do not block it - they are simply left alone - but
        /// posting says so plainly rather than letting them pass unmentioned.
        /// </summary>
        public bool IsReadyToPost
            => Status != StockTakeStatus.Completed
            && Status != StockTakeStatus.Cancelled
            && LineCount > 0
            && NeedsDecisionCount == 0
            && RecountCount == 0
            && Lines.Any(x => x.IsResolved);

        public bool IsOpen => Status != StockTakeStatus.Completed && Status != StockTakeStatus.Cancelled;

        public string StatusLabel => Status switch
        {
            StockTakeStatus.Pending => "Pending",
            StockTakeStatus.Counting => "Counting",
            StockTakeStatus.Review => "Review",
            StockTakeStatus.Completed => "Completed",
            StockTakeStatus.Cancelled => "Cancelled",
            _ => Status.ToString()
        };
    }
}
