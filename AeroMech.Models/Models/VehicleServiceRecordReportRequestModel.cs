namespace AeroMech.Models.Models
{
    /// <summary>
    /// How a vehicle service record is gathered: one heading per machine, or one heading per
    /// machine type. The lines underneath are the same service reports either way - what changes
    /// is the question being asked. "What has this machine had done to it" is a handover or a
    /// warranty question; "what do these machines need" is a fleet question.
    /// </summary>
    public enum VehicleServiceRecordGrouping
    {
        ByVehicle,
        ByMachineType
    }

    /// <summary>
    /// The scope of a vehicle service record: which machines, over what period, and how much of
    /// each visit to print.
    /// </summary>
    public class VehicleServiceRecordReportRequestModel
    {
        public VehicleServiceRecordGrouping Grouping { get; set; } = VehicleServiceRecordGrouping.ByVehicle;

        /// <summary>
        /// The vehicles to report on. Empty means every vehicle.
        /// </summary>
        public List<int> VehicleIds { get; set; } = new();

        /// <summary>
        /// The machine types to report on. Empty means every type. Free text on the vehicle, so
        /// matched as it is stored.
        /// </summary>
        public List<string> MachineTypes { get; set; } = new();

        /// <summary>
        /// Both ends are optional and both are inclusive. Left unset, the record runs the whole
        /// life of the machine, which is what a service record is normally wanted for.
        /// </summary>
        public DateOnly? FromDate { get; set; }

        public DateOnly? ToDate { get; set; }

        /// <summary>
        /// Prints the parts fitted at each visit underneath its line. Off by default: the record
        /// answers what was done and when, and the parts detail trebles the length of it.
        /// </summary>
        public bool IncludeParts { get; set; }

        /// <summary>
        /// Keeps a machine that has never been in on the report, under its own heading, saying so.
        /// On by default: "nothing has ever been done to this one" is an answer, and a machine
        /// silently missing from its own service record is not.
        /// </summary>
        public bool IncludeVehiclesWithNoServices { get; set; } = true;
    }
}
