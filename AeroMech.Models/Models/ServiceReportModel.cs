using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models
{
    public class ServiceReportModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Report Date is required")]
        public DateTime ReportDate { get; set; }

       // [Required(ErrorMessage = "Sales Order Number is required")]
        public string SalesOrderNumber { get; set; }

        //[Required(ErrorMessage = "Job Number is required")]
        public string? JobNumber { get; set; }

        //[Required(ErrorMessage = "Report Number is required")]
        //public string? ReportNumber { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }

        public virtual ClientModel Client { get; set; }

        [Required(ErrorMessage = "Client is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Client is required")]
        public int ClientId { get; set; }

        public virtual VehicleModel Vehicle { get; set; }

        [Required(ErrorMessage = "Vehicle is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Vehicle is required")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Vehicle engine hours is required")]
		[Range(1, int.MaxValue, ErrorMessage = "Vehicle is required")]
		public int VehicleHours { get; set; }

        public virtual List<ServiceReportEmployeeModel> Employees { get; set; }

        public virtual List<ServiceReportPartModel> Parts { get; set; }

        [Required(ErrorMessage = "Instruction is required")]
        public string Instruction { get; set; }

        [Required(ErrorMessage = "Detailed Service Report is required")]
        public string DetailedServiceReport { get; set; }

        [Required(ErrorMessage = "Service Type is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Service Type is required")]
        public ServiceType ServiceType { get; set; }

        public int QuoteNumber { get; set; }

        public bool IsComplete { get; set; }

        public int ServiceReportNumber { get; set; }

    }
}
