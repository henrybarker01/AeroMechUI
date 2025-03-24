namespace AeroMech.Data.Models
{
    public class StockTakeParts : BaseModel
    {
        public int StockTakeId { get; set; }
        public virtual StockTake StockTake { get; set; }
        public int PartId { get; set; }
        public virtual Part Part { get; set; }
        public int Quantity { get; set; }
        public int QuantityOnHand { get; set; }
        public int FinalQuantity { get; set; }
        public int? WarehouseId { get; set; }
        public virtual Warehouse Warehouse { get; set; }
        public string Remarks { get; set; }        
    }
}
