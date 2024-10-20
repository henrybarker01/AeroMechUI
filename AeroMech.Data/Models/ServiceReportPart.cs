using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
    public class ServiceReportPart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public virtual ServiceReport? ServiceReport { get; set; }

        [Required]
        public int ServiceReportId { get; set; }

        public virtual Part? Part { get; set; }

        [Required]
        public int PartId { get; set; }

        public double CostPrice { get; set; }
        public double Discount { get; set; }
        public int Qty { get; set; }
        public bool IsDeleted { get; set; }

    }
}
