namespace AeroMech.Models.Models
{
    /// <summary>
    /// Enough of a vehicle to pick it out of a list. Vehicles are recognised by what they are and
    /// the number stamped on them rather than by an id, so both are carried.
    /// </summary>
    public class VehicleOptionModel
    {
        public int Id { get; set; }

        public string? MachineType { get; set; }

        public string? SerialNumber { get; set; }

        public string? ClientName { get; set; }

        /// <summary>
        /// How the vehicle reads in a picker. A machine with no client is still a machine, so a
        /// missing name drops out rather than printing an empty bracket.
        /// </summary>
        public string Label
        {
            get
            {
                var machine = string.Join(" ", new[] { MachineType, SerialNumber }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

                if (string.IsNullOrWhiteSpace(machine))
                    machine = $"Vehicle {Id}";

                return string.IsNullOrWhiteSpace(ClientName) ? machine : $"{machine} - {ClientName}";
            }
        }
    }
}
