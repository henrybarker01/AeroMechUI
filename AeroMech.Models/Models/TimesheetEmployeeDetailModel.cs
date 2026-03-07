using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models.Models
{
    public class TimesheetEmployeeDetailModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public double Hours { get; set; }

        public DateOnly Date { get; set; }
    }
}
