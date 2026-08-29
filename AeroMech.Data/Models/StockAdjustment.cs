using AeroMech.Models.Enums;
using System.ComponentModel.DataAnnotations; 

namespace AeroMech.Data.Models
{
    public class StockAdjustment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PartId { get; set; }

        public virtual Part Part { get; set; }

        public virtual Warehouse? Warehouse { get; set; }
        public int WarehouseId { get; set; }

        public int QTY {  get; set; }
        public DateTimeOffset AdjustementDate { get; set; }

        public Guid AdjustedById { get; set; }

        public StockAdjustmentType StockAdjustmentType { get; set; }

        /// <summary>
        /// The receipt that caused this movement, when the stock came in on a supplier invoice.
        /// Null for every other kind of adjustment, so the ledger can be walked back to the
        /// invoice that proves an increase.
        /// </summary>
        public int? StockReceiptId { get; set; }

        public virtual StockReceipt? StockReceipt { get; set; }
    }
}
