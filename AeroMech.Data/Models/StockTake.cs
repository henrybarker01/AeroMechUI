using AeroMech.Data.Enums;

namespace AeroMech.Data.Models
{
    public class StockTake : BaseModel
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime StockTakeDate { get; set; }
        public string StockTakeBy { get; set; }
        public string Type { get; set; }
        public StockTakeStatus Status { get; set; }
        public string StockTakeDescription { get; set; }
        public string Remarks { get; set; }
        public int WarehouseId { get; set; }
        public virtual Warehouse Warehouse { get; set; }
        public DateTime CompletedDate { get; set; }
        public virtual ICollection<StockTakeParts> StockTakeParts { get; set; }
    }
}
