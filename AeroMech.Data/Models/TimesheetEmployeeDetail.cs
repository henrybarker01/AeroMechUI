using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
    public class TimesheetEmployeeDetail : BaseModel
    {
        public virtual Employee? Employee { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        public string Description { get; set; } = string.Empty;

        public double Hours { get; set; }

        public DateOnly Date { get; set; }
    }
}
