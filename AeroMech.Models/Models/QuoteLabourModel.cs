using AeroMech.Models.Enums;

namespace AeroMech.Models.Models
{
    public class QuoteLabourModel
    {
        public int Id { get; set; }

        public RateType RateType { get; set; }

        public double Rate { get; set; }

        public double Hours { get; set; }

        public double Discount { get; set; }

        public bool IsDeleted { get; set; }
    }
}
