namespace AeroMech.Models.Models
{
    public class QuotePartModel : PartModel
    {
        public int Id { get; set; }
        public int PartId { get; set; }
        public int QTY { get; set; }
        public double Discount { get; set; }
        public bool IsAdHockPart { get; set; }
    }
}
