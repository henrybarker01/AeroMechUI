using System.ComponentModel.DataAnnotations;
using AeroMech.Data.Enums;

namespace AeroMech.Models.Models
{
    public class TimesheetEmployeeDetailModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Required]
        public TimesheetGapTypes Description { get; set; }

        [Required]
        public double Hours { get; set; }

        public DateOnly Date { get; set; }
    }
}
