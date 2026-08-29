using AeroMech.Data.Enums;

namespace AeroMech.Data.Models
{
    /// <summary>
    /// One part on a count sheet: what the system believed when the sheet was drawn up, what was
    /// found on the shelf, and what was finally agreed. All three are held rather than one
    /// overwriting the next, because the difference between them is the entire point of counting.
    /// </summary>
    public class StockTakeParts : BaseModel
    {
        public int StockTakeId { get; set; }

        public virtual StockTake? StockTake { get; set; }

        public int PartId { get; set; }

        public virtual Part? Part { get; set; }

        /// <summary>
        /// The part's supplier code as it stood when the sheet was drawn up. Copied rather than
        /// read through the part, so re-printing a sheet months later still lists the parts in
        /// the order they were counted in even if a part has since changed supplier.
        /// </summary>
        public string? SupplierCode { get; set; }

        /// <summary>
        /// Where the part sat when the sheet was drawn up. Snapshot for the same reason as
        /// <see cref="SupplierCode"/>: it is the route the counter physically walked.
        /// </summary>
        public string? Bin { get; set; }

        public int? WarehouseId { get; set; }

        public virtual Warehouse? Warehouse { get; set; }

        /// <summary>
        /// The stock level at the moment the sheet was created, and the figure every variance is
        /// measured against. Frozen here rather than read from the part at posting time, so a
        /// count that takes two days is not thrown out by parts issued while it ran.
        /// </summary>
        public int QuantityOnHand { get; set; }

        /// <summary>
        /// What was counted on the shelf. Null means nobody has counted it yet, which is a
        /// different fact from a count of zero and is never posted as one.
        /// </summary>
        public int? Quantity { get; set; }

        /// <summary>
        /// The figure agreed after review, and the only one posting writes to stock. Set from the
        /// count once a line needs no decision or its difference has been accepted.
        /// </summary>
        public int? FinalQuantity { get; set; }

        /// <summary>
        /// The part's cost when the sheet was drawn up, so a difference can be valued. A single
        /// unit out on a cheap washer and on an expensive part are not the same problem, and
        /// without a value there is no way to sort the review by which matters.
        /// </summary>
        public double UnitCost { get; set; }

        public StockTakeLineStatus Status { get; set; }

        public string? CountedBy { get; set; }

        public DateTimeOffset? CountedAt { get; set; }

        /// <summary>
        /// The count that was rejected when a recount was asked for. Kept so the second count can
        /// be read against the first: a line that lands somewhere different again says something
        /// the second figure alone does not.
        /// </summary>
        public int? PreviousQuantity { get; set; }

        public int RecountCount { get; set; }

        public string? Remarks { get; set; }

        /// <summary>
        /// The level the part was actually found at when the sheet was posted. Where this differs
        /// from <see cref="QuantityOnHand"/> the part moved while the count was running, which
        /// posting records rather than silently absorbs.
        /// </summary>
        public int QtyOnHandAtPost { get; set; }

        /// <summary>
        /// What posting actually added to or took off the part, matching the ledger row it wrote.
        /// Signed: negative where the shelf held less than the system thought.
        /// </summary>
        public int AppliedDelta { get; set; }
    }
}
