using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models
{
    public class VehicleModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Serial Number is required")]
        public string SerialNumber { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Manufacture Date is required")]
        public DateOnly ManufactureDate { get; set; }

        public DateOnly? DateInService { get; set; }

        [Required(ErrorMessage = "Purchase Price is required")]
        public decimal PurchasePrice { get; set; }

        [Required(ErrorMessage = "Engine Hours is required")]
        public int EngineHours { get; set; }

        [Required(ErrorMessage = "Machine Type is required")]
        public string MachineType { get; set; }//MachineTypeModel

        public int ClientId { get; set; }

		[Required(ErrorMessage = "Job Number is required")]
		public string JobNumber { get; set; }

        public string ChassisNumber { get; set; }
    }
}
