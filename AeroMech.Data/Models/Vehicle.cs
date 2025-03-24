using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AeroMech.Data.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string SerialNumber { get; set; }

        public string? Description { get; set; }

        public DateOnly ManufactureDate { get; set; }

        public DateOnly? DateInService { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal PurchasePrice { get; set; }

        public int EngineHours { get; set; }

        public virtual string MachineType { get; set; }

        public bool IsDeleted { get; set; }

        public virtual Client? Client { get; set; }

        public int? ClientId { get; set; }

        public string JobNumber { get; set; }
        public string ChassisNumber { get; set; }
    }
}
