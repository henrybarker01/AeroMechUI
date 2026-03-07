namespace AeroMech.Models.Models
{
    public class TimesheetDateModel
    {
        public DateOnly Date { get; set; }
        public string DayOfWeek { get; set; }
        public double TotalWorked { get; set; }
    }
}
