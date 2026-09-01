namespace AeroMech.Models.Models
{
    /// <summary>
    /// Enough of a service report to recognise it in a picker, and no more. The reports view has
    /// to offer every report ever written, and the full model drags parts, employees and prices
    /// behind it - far too much to load merely to fill a dropdown.
    /// </summary>
    public class ServiceReportOptionModel
    {
        public int Id { get; set; }

        public int ServiceReportNumber { get; set; }

        public DateTimeOffset ReportDate { get; set; }

        public string? ClientName { get; set; }

        public string? MachineType { get; set; }

        public string? SerialNumber { get; set; }

        /// <summary>
        /// How the report reads in a list: its number, the day it was written, and who and what
        /// it was for. Anything missing is simply left out rather than printed as a blank.
        /// </summary>
        public string Label
        {
            get
            {
                var machine = string.Join(" ", new[] { MachineType, SerialNumber }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

                var parts = new[] { ClientName, machine }
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                var trailer = string.Join(" - ", parts);

                var head = $"AEM{ServiceReportNumber} ({ReportDate:yyyy-MM-dd})";

                return string.IsNullOrWhiteSpace(trailer) ? head : $"{head} - {trailer}";
            }
        }
    }
}
