using AeroMech.API.Reports;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AeroMech.UI.Web.Services
{
    /// <summary>
    /// Reporting on machines rather than on the work booked against them. Nothing here writes.
    ///
    /// A service report is written against a vehicle, so the history of a machine is already in
    /// the database - it has simply never been readable in one place. This service turns that
    /// into the two documents anyone actually asks for: what has been done to this machine, and
    /// what has been done to machines like it.
    /// </summary>
    public class VehicleReportService
    {
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly VehicleServiceRecordReport _serviceRecordReport;

        public VehicleReportService(
            IDbContextFactory<AeroMechDBContext> contextFactory,
            VehicleServiceRecordReport serviceRecordReport)
        {
            _contextFactory = contextFactory;
            _serviceRecordReport = serviceRecordReport;
        }

        private const string NoMachineType = "No machine type";

        /// <summary>
        /// Every machine on the books, for a picker. Ordered the way a machine is spoken about -
        /// what it is, then the number stamped on it.
        /// </summary>
        public async Task<List<VehicleOptionModel>> GetVehicleOptions()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Vehicles
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.MachineType)
                .ThenBy(x => x.SerialNumber)
                .Select(x => new VehicleOptionModel
                {
                    Id = x.Id,
                    MachineType = x.MachineType,
                    SerialNumber = x.SerialNumber,
                    ClientName = x.Client == null ? null : x.Client.Name
                })
                .ToListAsync();
        }

        /// <summary>
        /// The machine types actually in use. Read off the vehicles rather than from a list of
        /// types, because the field is free text and only the values on real machines can be
        /// reported on.
        /// </summary>
        public async Task<List<string>> GetMachineTypes()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var types = await context.Vehicles
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Select(x => x.MachineType)
                .Distinct()
                .ToListAsync();

            return types
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Dates come off a date picker with no time on them, and report dates are stamped at UTC
        /// midnight for the day they were written. Reading the period as whole calendar days in
        /// UTC therefore includes both boundary days in full.
        /// </summary>
        private static DateTimeOffset StartOfDay(DateOnly date)
            => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        private static string DescribePeriod(DateOnly? from, DateOnly? to) => (from, to) switch
        {
            (null, null) => "Full service history",
            (not null, null) => $"From {from:dd/MM/yyyy}",
            (null, not null) => $"Up to {to:dd/MM/yyyy}",
            _ => $"{from:dd/MM/yyyy} to {to:dd/MM/yyyy}"
        };

        private static string DescribeSelection(IReadOnlyCollection<string> names, string allLabel, string noun)
            => names.Count == 0 ? allLabel
                : names.Count <= 4 ? string.Join(", ", names)
                : $"{names.Count} {noun}";

        private static string MachineTypeLabel(string? machineType)
            => string.IsNullOrWhiteSpace(machineType) ? NoMachineType : machineType.Trim();

        /// <summary>
        /// How a machine reads on paper: what it is and the number stamped on it, with the client
        /// after it so a record pulled across the whole fleet still says whose machine it is.
        /// </summary>
        private static string DescribeVehicle(Vehicle vehicle)
        {
            var machine = string.Join(" ", new[] { vehicle.MachineType, vehicle.SerialNumber }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            if (string.IsNullOrWhiteSpace(machine))
                machine = $"Vehicle {vehicle.Id}";

            return string.IsNullOrWhiteSpace(vehicle.Client?.Name)
                ? machine
                : $"{machine} - {vehicle.Client!.Name}";
        }

        /// <summary>
        /// The work done at a visit, in the order the fields are worth reading. The detailed
        /// report is what the technician wrote up; the instruction is what they were sent to do,
        /// which is the next best thing when the write-up was never filled in.
        /// </summary>
        private static string DescribeWork(ServiceReport report)
        {
            foreach (var candidate in new[] { report.DetailedServiceReport, report.Description, report.Instruction })
            {
                if (!string.IsNullOrWhiteSpace(candidate))
                    return candidate.Trim();
            }

            return string.Empty;
        }

        private static List<VehicleServiceRecordPart> DescribeParts(ServiceReport report)
        {
            // Stock parts and ad-hoc parts are held apart because only one of them moves stock,
            // but they were both fitted to the machine, so a service record reads them as one list.
            var parts = (report.Parts ?? new List<ServiceReportPart>())
                .Where(x => !x.IsDeleted)
                .Select(x => new VehicleServiceRecordPart
                {
                    PartCode = x.Part?.PartCode ?? string.Empty,
                    PartDescription = x.Part?.PartDescription ?? string.Empty,
                    Quantity = x.Qty
                });

            var adHoc = (report.AdHockParts ?? new List<ServiceReportAdHockPart>())
                .Where(x => !x.IsDeleted)
                .Select(x => new VehicleServiceRecordPart
                {
                    PartCode = x.PartCode ?? string.Empty,
                    PartDescription = x.PartDescription ?? string.Empty,
                    Quantity = x.Qty
                });

            return parts.Concat(adHoc)
                .OrderBy(x => x.PartCode, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static VehicleServiceRecordLine ToLine(ServiceReport report, Vehicle vehicle, bool includeParts)
            => new()
            {
                ReportDate = report.ReportDate,
                ServiceReportNumber = report.ServiceReportNumber,
                MachineLabel = string.IsNullOrWhiteSpace(vehicle.SerialNumber)
                    ? $"Vehicle {vehicle.Id}"
                    : vehicle.SerialNumber,
                JobNumber = report.JobNumber,
                ServiceType = report.ServiceType,
                MachineHours = report.VehicleHours,
                WorkDone = DescribeWork(report),
                LabourHours = (report.Employees ?? new List<ServiceReportEmployee>())
                    .Where(x => !x.IsDeleted)
                    .Sum(x => x.Hours),
                Parts = includeParts ? DescribeParts(report) : new List<VehicleServiceRecordPart>()
            };

        /// <summary>
        /// The service history of a set of machines, gathered either under each machine or under
        /// the type of machine.
        /// </summary>
        public async Task<byte[]> GenerateVehicleServiceRecordReport(VehicleServiceRecordReportRequestModel request)
        {
            if (request.FromDate is DateOnly from && request.ToDate is DateOnly to && to < from)
                throw new InvalidOperationException("The end of the period cannot fall before its start.");

            var machineTypes = request.MachineTypes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var vehicleIds = request.VehicleIds.Distinct().ToList();

            using var context = await _contextFactory.CreateDbContextAsync();

            var vehicleQuery = context.Vehicles
                .AsNoTracking()
                .Include(x => x.Client)
                .Where(x => !x.IsDeleted);

            if (vehicleIds.Count > 0)
                vehicleQuery = vehicleQuery.Where(x => vehicleIds.Contains(x.Id));

            if (machineTypes.Count > 0)
                vehicleQuery = vehicleQuery.Where(x => machineTypes.Contains(x.MachineType));

            var vehicles = await vehicleQuery.ToListAsync();

            if (vehicles.Count == 0)
                throw new InvalidOperationException("No machines match that selection, so there is nothing to report.");

            // Taken before the machines with no history are dropped: the scope line describes what
            // was asked for, which does not change because one of the machines turned out to be
            // clean.
            var vehicleNames = vehicles.Select(DescribeVehicle).ToList();

            var scopeIds = vehicles.Select(x => x.Id).ToList();

            var reportQuery = context.ServiceReports
                .AsNoTracking()
                .Include(x => x.Employees)
                .Where(x => !x.IsDeleted && x.VehicleId != null && scopeIds.Contains(x.VehicleId!.Value));

            // The part lines are only pulled when they are going to be printed: a full history
            // with every part on every visit is a great deal of reading to fetch and then discard.
            if (request.IncludeParts)
            {
                reportQuery = reportQuery
                    .Include(x => x.AdHockParts)
                    .Include(x => x.Parts).ThenInclude(x => x.Part);
            }

            if (request.FromDate is DateOnly fromDate)
            {
                var fromStart = StartOfDay(fromDate);
                reportQuery = reportQuery.Where(x => x.ReportDate >= fromStart);
            }

            if (request.ToDate is DateOnly toDate)
            {
                var toEndExclusive = StartOfDay(toDate.AddDays(1));
                reportQuery = reportQuery.Where(x => x.ReportDate < toEndExclusive);
            }

            var reports = await reportQuery.ToListAsync();

            var reportsByVehicle = reports
                .GroupBy(x => x.VehicleId!.Value)
                .ToDictionary(x => x.Key, x => x.OrderBy(r => r.ReportDate).ThenBy(r => r.ServiceReportNumber).ToList());

            // Dropped here rather than inside the grouping so that the machine count in the
            // heading counts what is actually on the page, under either grouping.
            if (!request.IncludeVehiclesWithNoServices)
                vehicles = vehicles.Where(x => reportsByVehicle.ContainsKey(x.Id)).ToList();

            var groups = request.Grouping == VehicleServiceRecordGrouping.ByMachineType
                ? BuildTypeGroups(vehicles, reportsByVehicle, request.IncludeParts)
                : BuildVehicleGroups(vehicles, reportsByVehicle, request.IncludeParts);

            if (groups.Count == 0)
                throw new InvalidOperationException("No services were recorded for that selection over that period.");

            _serviceRecordReport.Data = new VehicleServiceRecordReportData
            {
                GeneratedAt = DateTimeOffset.Now,
                Grouping = request.Grouping,
                PeriodLabel = DescribePeriod(request.FromDate, request.ToDate),
                ScopeLabel = request.Grouping == VehicleServiceRecordGrouping.ByMachineType
                    ? DescribeSelection(machineTypes, "All machine types", "machine types")
                    : DescribeSelection(vehicleNames, "All machines", "machines"),
                IncludeParts = request.IncludeParts,
                TotalVehicles = vehicles.Count,
                Groups = groups
            };

            return Document.Create(_serviceRecordReport.Compose).GeneratePdf();
        }

        private static List<VehicleServiceRecordGroup> BuildVehicleGroups(
            List<Vehicle> vehicles,
            IReadOnlyDictionary<int, List<ServiceReport>> reportsByVehicle,
            bool includeParts)
        {
            var groups = new List<VehicleServiceRecordGroup>();

            foreach (var vehicle in vehicles
                .OrderBy(x => MachineTypeLabel(x.MachineType), StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.SerialNumber, StringComparer.OrdinalIgnoreCase))
            {
                var reports = reportsByVehicle.TryGetValue(vehicle.Id, out var found)
                    ? found
                    : new List<ServiceReport>();

                groups.Add(new VehicleServiceRecordGroup
                {
                    Heading = DescribeVehicle(vehicle),
                    Lines = reports.Select(x => ToLine(x, vehicle, includeParts)).ToList()
                });
            }

            return groups;
        }

        private static List<VehicleServiceRecordGroup> BuildTypeGroups(
            List<Vehicle> vehicles,
            IReadOnlyDictionary<int, List<ServiceReport>> reportsByVehicle,
            bool includeParts)
        {
            var groups = new List<VehicleServiceRecordGroup>();

            // Machines carrying no type still have a history, so they group under their own
            // heading rather than being dropped. Sorted last: the unattributed tail belongs at
            // the end, the same way an unattributed supplier does on a valuation.
            foreach (var typeGroup in vehicles
                .GroupBy(x => MachineTypeLabel(x.MachineType), StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x.Key == NoMachineType)
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                // Read as one run of work across the whole type rather than machine by machine:
                // a fault that keeps coming back on a model shows up in the dates, and splitting
                // by machine would bury it.
                var lines = typeGroup
                    .SelectMany(vehicle => (reportsByVehicle.TryGetValue(vehicle.Id, out var found)
                            ? found
                            : new List<ServiceReport>())
                        .Select(report => ToLine(report, vehicle, includeParts)))
                    .OrderBy(x => x.ReportDate)
                    .ThenBy(x => x.ServiceReportNumber)
                    .ToList();

                groups.Add(new VehicleServiceRecordGroup
                {
                    Heading = $"{typeGroup.Key}  ({typeGroup.Count()} machine(s))",
                    Lines = lines
                });
            }

            return groups;
        }
    }
}
