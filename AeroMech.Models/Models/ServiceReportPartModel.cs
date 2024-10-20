namespace AeroMech.Models.Models
{
    public class ServiceReportPartModel : PartModel
    {
        public int Id { get; set; }
        public int QTY { get; set; }
        public double Discount { get; set; }
        public bool IsAdHockPart { get; set; }
    }
}
