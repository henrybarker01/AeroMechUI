namespace AeroMech.Models.Models
{
    /// <summary>
    /// An editable row on the receiving grid: one of the supplier's parts, its stock level now,
    /// and what is being taken in against the invoice. Every part the supplier stocks gets a row,
    /// whether or not it appears on the invoice, so the receiver works down the same list the
    /// invoice was written from instead of searching for parts one at a time.
    /// </summary>
    public class StockReceivingLineModel
    {
        public int PartId { get; set; }
        public string PartCode { get; set; } = string.Empty;
        public string PartDescription { get; set; } = string.Empty;
        public string? Bin { get; set; }
        public string? ProductClass { get; set; }
        public string? WarehouseCode { get; set; }
        public int WarehouseId { get; set; }

        /// <summary>
        /// The stock level as it stood when the grid was loaded.
        /// </summary>
        public int QtyOnHand { get; set; }

        /// <summary>
        /// The part's cost price before this receipt, kept so an invoice price that differs can be
        /// pointed out rather than quietly accepted.
        /// </summary>
        public double CurrentCostPrice { get; set; }

        public int QtyReceived { get; set; }

        /// <summary>
        /// Pre-filled from <see cref="CurrentCostPrice"/> and overridden by the receiver when the
        /// invoice says otherwise, which is the usual reason a part's cost price moves.
        /// </summary>
        public double UnitCost { get; set; }

        /// <summary>
        /// A row only counts as part of the receipt once a quantity is entered, which is how the
        /// invoice lines are picked out of the supplier's full parts list.
        /// </summary>
        public bool IsOnInvoice => QtyReceived > 0;

        public int NewQtyOnHand => QtyOnHand + QtyReceived;

        public double LineTotal => QtyReceived * UnitCost;

        /// <summary>
        /// Whether the invoice price differs from what the part currently costs. Compared to the
        /// cent, because a cost price held as a double will not always match exactly.
        /// </summary>
        public bool CostPriceDiffers => Math.Round(UnitCost, 2) != Math.Round(CurrentCostPrice, 2);
    }
}
