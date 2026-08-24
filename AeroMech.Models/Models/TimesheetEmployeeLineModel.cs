using AeroMech.Data.Enums;

namespace AeroMech.Models.Models
{
    public class TimesheetEmployeeLineModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public string Description { get; set; } = string.Empty;

        public double Hours { get; set; }

        public DateOnly Date { get; set; }

        /// <summary>
        /// Set for manually added timesheet lines, null for service report lines.
        /// </summary>
        public TimesheetGapTypes? GapType { get; set; }

        /// <summary>
        /// Set for service report lines, null for manually added timesheet lines.
        /// </summary>
        public int? ServiceReportId { get; set; }

        public bool IsServiceReport => ServiceReportId is not null;
    }
}
