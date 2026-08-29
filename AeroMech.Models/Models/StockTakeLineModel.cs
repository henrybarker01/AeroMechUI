using AeroMech.Data.Enums;

namespace AeroMech.Models.Models
{
    /// <summary>
    /// One part on a count sheet as the screens work with it: what the system believed, what was
    /// counted, and what that difference is worth. The difference is derived here rather than
    /// stored, so it can never drift out of step with the two figures it comes from.
    /// </summary>
    public class StockTakeLineModel
    {
        public int Id { get; set; }
        public int PartId { get; set; }
        public string PartCode { get; set; } = string.Empty;
        public string PartDescription { get; set; } = string.Empty;
        public string? SupplierCode { get; set; }
        public string? Bin { get; set; }
        public string? ProductClass { get; set; }
        public int? WarehouseId { get; set; }
        public string? WarehouseCode { get; set; }

        /// <summary>
        /// The stock level when the sheet was drawn up. Every variance on this line is measured
        /// against this, not against the part's level today.
        /// </summary>
        public int QuantityOnHand { get; set; }

        /// <summary>
        /// What was found on the shelf, or null while nobody has counted it.
        /// </summary>
        public int? Quantity { get; set; }

        public int? FinalQuantity { get; set; }

        public double UnitCost { get; set; }

        public StockTakeLineStatus Status { get; set; }

        public string? CountedBy { get; set; }
        public DateTimeOffset? CountedAt { get; set; }
        public int? PreviousQuantity { get; set; }
        public int RecountCount { get; set; }
        public string? Remarks { get; set; }

        public int QtyOnHandAtPost { get; set; }
        public int AppliedDelta { get; set; }

        /// <summary>
        /// The part's stock level right now, read live while the sheet is still open. Held next to
        /// the snapshot so a part that moved while the count was running can be pointed out during
        /// review, rather than only being discovered in the ledger afterwards.
        /// </summary>
        public int CurrentQtyOnHand { get; set; }

        /// <summary>
        /// Whether the part moved since the sheet froze its level. Not an error - a service report
        /// issuing parts mid-count is ordinary - but it means the count may already be stale, and
        /// that is worth seeing before the difference is accepted.
        /// </summary>
        public bool HasMovedSinceSnapshot => CurrentQtyOnHand != QuantityOnHand;

        public bool IsCounted => Quantity.HasValue;

        /// <summary>
        /// How far the shelf sits from the system. Positive means more was found than expected.
        /// </summary>
        public int Variance => Quantity.HasValue ? Quantity.Value - QuantityOnHand : 0;

        public bool HasVariance => Quantity.HasValue && Quantity.Value != QuantityOnHand;

        /// <summary>
        /// What the difference is worth, which is how the review decides what to look at first.
        /// </summary>
        public double VarianceValue => Variance * UnitCost;

        public double AbsoluteVarianceValue => Math.Abs(VarianceValue);

        /// <summary>
        /// A counted line that disagrees with the system and has not yet been settled. These are
        /// the lines review exists for, and posting is blocked while any remain.
        /// </summary>
        public bool NeedsDecision => Status == StockTakeLineStatus.Counted && HasVariance;

        /// <summary>
        /// Waiting on a counter: either never counted, or counted and sent back.
        /// </summary>
        public bool IsAwaitingCount
            => Status == StockTakeLineStatus.NotCounted || Status == StockTakeLineStatus.RecountRequested;

        /// <summary>
        /// Settled one way or the other, so posting knows what to do with it: a count that agreed
        /// with the system needs no decision, and an accepted difference has already had one.
        /// </summary>
        public bool IsResolved
            => Status == StockTakeLineStatus.Accepted
            || (Status == StockTakeLineStatus.Counted && !HasVariance);

        /// <summary>
        /// The figure posting would write. An accepted line carries its agreed quantity; a line
        /// that matched the system settles at what was counted.
        /// </summary>
        public int? EffectiveFinalQuantity
            => Status == StockTakeLineStatus.Accepted ? FinalQuantity
             : Status == StockTakeLineStatus.Counted && !HasVariance ? Quantity
             : null;

        /// <summary>
        /// What posting would add to or take off the part. Signed, and zero for a line that
        /// agreed with the system - which is why such lines move no stock and write no ledger row.
        /// </summary>
        public int PendingDelta
            => EffectiveFinalQuantity.HasValue ? EffectiveFinalQuantity.Value - QuantityOnHand : 0;
    }
}
