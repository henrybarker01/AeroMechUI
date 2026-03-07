using System.ComponentModel.DataAnnotations;
using AeroMech.Data.Enums;

namespace AeroMech.Data.Models
{
    public class TimesheetEmployeeDetail : BaseModel
    {
        public virtual Employee? Employee { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        public TimesheetGapTypes Description { get; set; }

        public double Hours { get; set; }

        public DateOnly Date { get; set; }
    }
}
