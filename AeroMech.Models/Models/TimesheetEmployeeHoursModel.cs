namespace AeroMech.Models.Models
{
    public class TimesheetEmployeeHoursModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeNumber { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public double ServiceReportHours { get; set; }
        public double TimesheetHours { get; set; }
        public double TotalHours { get; set; }
    }
}
