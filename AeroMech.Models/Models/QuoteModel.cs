using AeroMech.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models.Models
{
    public class QuoteModel
    {
        public int Id { get; set; }

        public int QuoteNumber { get; set; }

        [Required(ErrorMessage = "Quote Date is required")]
        public DateTimeOffset QuoteDate { get; set; }

        public string? Description { get; set; }

        public virtual ClientModel Client { get; set; }

        [Required(ErrorMessage = "Client is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Client is required")]
        public int ClientId { get; set; }

        public virtual VehicleModel Vehicle { get; set; }

        [Required(ErrorMessage = "Vehicle is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Vehicle is required")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Vehicle engine hours is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Vehicle engine hours is required")]
        public int VehicleHours { get; set; }

        public virtual List<QuoteLabourModel> Labour { get; set; } = new();

        public virtual List<QuotePartModel> Parts { get; set; } = new();

        [Required(ErrorMessage = "Instruction is required")]
        public string Instruction { get; set; }

        [Required(ErrorMessage = "Detailed Service Report is required")]
        public string DetailedServiceReport { get; set; }

        [Required(ErrorMessage = "Service Type is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Service Type is required")]
        public ServiceType ServiceType { get; set; }

        /// <summary>
        /// Set once the quote has been turned into a service report, which also makes the quote
        /// read only.
        /// </summary>
        public int? ServiceReportId { get; set; }

        public int? ServiceReportNumber { get; set; }

        public DateTimeOffset? ConvertedDate { get; set; }

        public bool IsConverted => ServiceReportId.HasValue;
    }
}
