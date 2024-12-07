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
        public DateTime AdjustementDate { get; set; }

        public Guid AdjustedById { get; set; }

        public StockAdjustmentType StockAdjustmentType { get; set; }
    }
}
