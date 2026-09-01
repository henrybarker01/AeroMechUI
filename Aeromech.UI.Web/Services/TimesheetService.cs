using AeroMech.Data.Persistence;
using AeroMech.Models.Models;
using AeroMech.API.Reports;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using System.Globalization;
using ClosedXML.Excel;

namespace AeroMech.UI.Web.Services
{
    public class TimesheetService
    {
        private const string NoClientSectionTitle = "No Client";

        private enum TimesheetReportType
        {
            Weekly,
            Daily,
            DateRange
        }

        private sealed record TimesheetReportItem(int EmployeeId, string FirstName, string LastName, string RowKey, double Hours, string? ClientName);

        private sealed record TimesheetReportData(
            List<TimesheetReportEmployee> Employees,
            List<TimesheetReportRow> Rows,
            List<string> ClientNames,
            bool IsClientFiltered);

        private sealed record TimesheetReportParameters(
            string ReportType,
            string PeriodLabel,
            string ClientFilterLabel);

        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly IMapper _mapper;
        private readonly TimesheetReport _timesheetReport;

        public TimesheetService(IDbContextFactory<AeroMechDBContext> contextFactory,
            IMapper mapper,
            TimesheetReport timesheetReport
            )
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
            _timesheetReport = timesheetReport;
        }

        public async Task<List<TimesheetDateModel>> GetTimesheetDatesFrom(DateOnly startDate)
        {
            var timesheetDates = new List<TimesheetDateModel>();

            var lastCompleteDayUtc = DateOnly.FromDateTime(DateTime.UtcNow);

            for (var date = startDate; date <= lastCompleteDayUtc; date = date.AddDays(1))
            {
                timesheetDates.Add(new TimesheetDateModel
                {
                    Date = date,
                    DayOfWeek = date.DayOfWeek.ToString(),
                    TotalWorked = 0
                });
            }

            var endUtc = DateTimeOffset.UtcNow;

            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var serviceReportEmployees = await _aeroMechDBContext.ServiceReportEmployees.AsNoTracking()
                .Where(sr => sr.DutyDate >= startDate && !sr.IsDeleted && !sr.Employee!.IsDeleted && !sr.Employee!.ExcludeFromTimesheets)
                .ToListAsync();

            foreach (var sr in serviceReportEmployees)
            {
                var timesheetDate = timesheetDates.FirstOrDefault(td => td.Date == sr.DutyDate);
                if (timesheetDate != null)
                {
                    timesheetDate.TotalWorked += sr.Hours;
                }
            }

            var timesheetDetailEmployees = await _aeroMechDBContext.TimesheetEmployeeDetails.AsNoTracking()
                .Where(x => x.Date >= startDate && !x.IsDeleted && !x.Employee!.IsDeleted && !x.Employee!.ExcludeFromTimesheets)
                .ToListAsync();

            foreach (var timesheetDetailEmployee in timesheetDetailEmployees)
            {
                var timesheetDate = timesheetDates.FirstOrDefault(td => td.Date == timesheetDetailEmployee.Date);
                if (timesheetDate != null)
                {
                    timesheetDate.TotalWorked += timesheetDetailEmployee.Hours;
                }
            }

            return timesheetDates.OrderByDescending(x => x.Date).ToList();
        }

        public async Task<List<TimesheetEmployeeHoursModel>> GetTimesheetEmployeeDetailAsync(DateOnly date)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            // Aggregate hours per employee from both sources.
            var serviceReportHours = await _aeroMechDBContext.ServiceReportEmployees
                .AsNoTracking()
                .Where(x => x.DutyDate == date && !x.IsDeleted)
                .GroupBy(x => x.EmployeeId)
                .Select(g => new { EmployeeId = g.Key, Hours = g.Sum(x => x.Hours) })
                .ToListAsync();

            var timesheetHours = await _aeroMechDBContext.TimesheetEmployeeDetails
                .AsNoTracking()
                .Where(x => x.Date == date && !x.IsDeleted)
                .GroupBy(x => x.EmployeeId)
                .Select(g => new { EmployeeId = g.Key, Hours = g.Sum(x => x.Hours) })
                .ToListAsync();

            var serviceReportHoursByEmployeeId = serviceReportHours.ToDictionary(x => x.EmployeeId, x => x.Hours);
            var timesheetHoursByEmployeeId = timesheetHours.ToDictionary(x => x.EmployeeId, x => x.Hours);

            // Build final rows from the employee master list to avoid duplicates.
            var employees = await _aeroMechDBContext.Employees
                .AsNoTracking()
                .Where(emp => !emp.IsDeleted && !emp.ExcludeFromTimesheets)
                .OrderBy(emp => emp.FirstName)
                .ThenBy(emp => emp.LastName)
                .ToListAsync();

            return employees
                .Select(emp =>
                {
                    serviceReportHoursByEmployeeId.TryGetValue(emp.Id, out var srHours);
                    timesheetHoursByEmployeeId.TryGetValue(emp.Id, out var tsHours);

                    return new TimesheetEmployeeHoursModel
                    {
                        EmployeeId = emp.Id,
                        EmployeeNumber = emp.IDNumber ?? emp.Id.ToString(),
                        EmployeeName = ((emp.FirstName ?? string.Empty) + " " + (emp.LastName ?? string.Empty)).Trim(),
                        ServiceReportHours = srHours,
                        TimesheetHours = tsHours,
                        TotalHours = srHours + tsHours
                    };
                })
                .OrderBy(x => x.EmployeeName)
                .ToList();
        }

        public async Task<List<TimesheetEmployeeLineModel>> GetEmployeeTimesheetDataAsync(int employeeId, DateOnly date)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var serviceReportEmployees = await _aeroMechDBContext.ServiceReportEmployees.AsNoTracking()
                .Where(sr => sr.EmployeeId == employeeId && sr.DutyDate == date && !sr.IsDeleted)
                .Select(sr => new
                {
                    sr.Id,
                    sr.EmployeeId,
                    sr.Hours,
                    sr.DutyDate,
                    sr.ServiceReportId,
                    sr.ServiceReport!.ServiceReportNumber,
                    sr.ServiceReport!.JobNumber,
                    sr.ServiceReport!.SalesOrderNumber
                })
                .ToListAsync();

            var employeeTimesheetDetail = await _aeroMechDBContext.TimesheetEmployeeDetails.AsNoTracking()
                .Where(emp => emp.EmployeeId == employeeId && emp.Date == date && !emp.IsDeleted)
                .ToListAsync();

            var serviceReportLines = serviceReportEmployees
                .Select(sr => new TimesheetEmployeeLineModel
                {
                    Id = sr.Id,
                    EmployeeId = sr.EmployeeId,
                    Description = BuildServiceReportDescription(sr.ServiceReportNumber, sr.JobNumber, sr.SalesOrderNumber),
                    Hours = sr.Hours,
                    Date = sr.DutyDate,
                    ServiceReportId = sr.ServiceReportId
                })
                .OrderBy(x => x.Description, StringComparer.OrdinalIgnoreCase);

            var timesheetLines = employeeTimesheetDetail
                .Select(detail => new TimesheetEmployeeLineModel
                {
                    Id = detail.Id,
                    EmployeeId = detail.EmployeeId,
                    Description = detail.Description.ToString(),
                    Hours = detail.Hours,
                    Date = detail.Date,
                    GapType = detail.Description
                })
                .OrderBy(x => x.Description, StringComparer.OrdinalIgnoreCase);

            return serviceReportLines.Concat(timesheetLines).ToList();
        }

        private static string BuildServiceReportDescription(int serviceReportNumber, string? jobNumber, string? salesOrderNumber)
        {
            var reference = string.IsNullOrWhiteSpace(jobNumber) ? salesOrderNumber : jobNumber;

            return string.IsNullOrWhiteSpace(reference)
                ? $"AEM {serviceReportNumber}"
                : $"AEM {serviceReportNumber} - {reference}";
        }

        public async Task AddLineToEmployeeTimesheetDetailAsync(TimesheetEmployeeDetailModel timesheetEmployeeDetailModel)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            var timesheetEmployeeDetail = _mapper.Map<Data.Models.TimesheetEmployeeDetail>(timesheetEmployeeDetailModel);

            timesheetEmployeeDetail.IsDeleted = false;
            timesheetEmployeeDetail.CreatedBy = string.Empty;
            timesheetEmployeeDetail.UpdatedBy = string.Empty;

            _aeroMechDBContext.TimesheetEmployeeDetails.Add(timesheetEmployeeDetail);
            await _aeroMechDBContext.SaveChangesAsync();
        }

        public async Task EditEmployeeTimesheetDetailAsync(TimesheetEmployeeDetailModel timesheetEmployeeDetailModel)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            var timesheetEmployeeDetail = await _aeroMechDBContext.TimesheetEmployeeDetails
                .FirstOrDefaultAsync(x => x.Id == timesheetEmployeeDetailModel.Id);

            if (timesheetEmployeeDetail != null)
            {
                timesheetEmployeeDetail.Hours = timesheetEmployeeDetailModel.Hours;
                timesheetEmployeeDetail.Description = timesheetEmployeeDetailModel.Description;
                timesheetEmployeeDetail.UpdatedBy = string.Empty;
                await _aeroMechDBContext.SaveChangesAsync();
            }
        }

        public async Task DeleteEmployeeTimesheetDetailAsync(int id)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            var timesheetEmployeeDetail = await _aeroMechDBContext.TimesheetEmployeeDetails
                .FirstOrDefaultAsync(x => x.Id == id);
            if (timesheetEmployeeDetail != null)
            {
                timesheetEmployeeDetail.IsDeleted = true;
                timesheetEmployeeDetail.UpdatedBy = string.Empty;
                await _aeroMechDBContext.SaveChangesAsync();
            }
        }

        public async Task<List<ClientOptionModel>> GetTimesheetReportClientsAsync()
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            return await _aeroMechDBContext.Clients
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new ClientOptionModel { Id = c.Id, Name = c.Name })
                .ToListAsync();
        }

        public async Task<byte[]> DownloadTimesheetReportAsync(DateOnly anyDateInWeek, IReadOnlyCollection<int>? clientIds = null)
        {
            var weekStart = GetWeekStart(anyDateInWeek);
            var weekEnd = weekStart.AddDays(6);
            var data = await BuildReportDataAsync(weekStart, weekEnd, clientIds);

            return GeneratePdfFile(data, CreateReportParameters(TimesheetReportType.Weekly, weekStart, weekEnd, data));
        }

        public async Task<byte[]> DownloadDailyTimesheetReportAsync(DateOnly date, IReadOnlyCollection<int>? clientIds = null)
        {
            var data = await BuildReportDataAsync(date, date, clientIds);

            return GeneratePdfFile(data, CreateReportParameters(TimesheetReportType.Daily, date, date, data));
        }

        public async Task<byte[]> DownloadDateRangeTimesheetReportAsync(DateOnly fromDate, DateOnly toDate, IReadOnlyCollection<int>? clientIds = null)
        {
            if (toDate < fromDate)
                (fromDate, toDate) = (toDate, fromDate);

            var data = await BuildReportDataAsync(fromDate, toDate, clientIds);

            return GeneratePdfFile(data, CreateReportParameters(TimesheetReportType.DateRange, fromDate, toDate, data));
        }

        public async Task<byte[]> ExportWeeklyTimesheetToExcelAsync(DateOnly anyDateInWeek, IReadOnlyCollection<int>? clientIds = null)
        {
            var weekStart = GetWeekStart(anyDateInWeek);
            var weekEnd = weekStart.AddDays(6);
            var data = await BuildReportDataAsync(weekStart, weekEnd, clientIds);

            return GenerateExcelFile(
                data.Employees,
                data.Rows,
                CreateReportParameters(TimesheetReportType.Weekly, weekStart, weekEnd, data));
        }

        public async Task<byte[]> ExportDailyTimesheetToExcelAsync(DateOnly date, IReadOnlyCollection<int>? clientIds = null)
        {
            var data = await BuildReportDataAsync(date, date, clientIds);

            return GenerateExcelFile(
                data.Employees,
                data.Rows,
                CreateReportParameters(TimesheetReportType.Daily, date, date, data));
        }

        public async Task<byte[]> ExportDateRangeTimesheetToExcelAsync(DateOnly fromDate, DateOnly toDate, IReadOnlyCollection<int>? clientIds = null)
        {
            if (toDate < fromDate)
                (fromDate, toDate) = (toDate, fromDate);

            var data = await BuildReportDataAsync(fromDate, toDate, clientIds);

            return GenerateExcelFile(
                data.Employees,
                data.Rows,
                CreateReportParameters(TimesheetReportType.DateRange, fromDate, toDate, data));
        }

        private byte[] GeneratePdfFile(TimesheetReportData data, TimesheetReportParameters parameters)
        {
            _timesheetReport.Data = new TimesheetReportDocumentData
            {
                ReportType = parameters.ReportType,
                PeriodLabel = parameters.PeriodLabel,
                ClientFilterLabel = parameters.ClientFilterLabel,
                Employees = data.Employees,
                Rows = data.Rows
            };

            return Document.Create(_timesheetReport.Compose).GeneratePdf();
        }

        private static TimesheetReportParameters CreateReportParameters(
            TimesheetReportType reportType,
            DateOnly fromDate,
            DateOnly toDate,
            TimesheetReportData data)
        {
            var periodLabel = fromDate == toDate
                ? fromDate.ToString("d MMMM yyyy", CultureInfo.InvariantCulture)
                : $"{fromDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture)} – {toDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";

            if (reportType == TimesheetReportType.Weekly)
            {
                var weekNumber = ISOWeek.GetWeekOfYear(fromDate.ToDateTime(TimeOnly.MinValue));
                periodLabel = $"Week {weekNumber} · {periodLabel}";
            }

            var clientFilterLabel = !data.IsClientFiltered
                ? "All clients"
                : data.ClientNames.Count > 0
                    ? string.Join(", ", data.ClientNames)
                    : "No matching clients";

            var reportTypeLabel = reportType switch
            {
                TimesheetReportType.Weekly => "Weekly",
                TimesheetReportType.Daily => "Daily",
                TimesheetReportType.DateRange => "Date range",
                _ => throw new ArgumentOutOfRangeException(nameof(reportType))
            };

            return new TimesheetReportParameters(reportTypeLabel, periodLabel, clientFilterLabel);
        }

        /// <summary>
        /// Loads every timesheet line between the two dates (both inclusive), optionally restricted to a set of
        /// clients, and shapes it into the employee columns / section rows the report and the Excel export share.
        /// </summary>
        private async Task<TimesheetReportData> BuildReportDataAsync(
            DateOnly fromDate,
            DateOnly toDate,
            IReadOnlyCollection<int>? clientIds)
        {
            var filterClientIds = clientIds?.Distinct().ToList() ?? new List<int>();
            var isClientFiltered = filterClientIds.Count > 0;

            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            // Only employees who take part in timesheets get a column, and every figure on the report is
            // read off those columns - so anyone else's lines have to be left out here too. Loading them
            // produced rows of zeros and totals that came up short of the hours actually booked.
            var serviceReportQuery = _aeroMechDBContext.ServiceReportEmployees
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.DutyDate >= fromDate && x.DutyDate <= toDate
                    && !x.Employee!.IsDeleted && !x.Employee!.ExcludeFromTimesheets);

            if (isClientFiltered)
            {
                serviceReportQuery = serviceReportQuery
                    .Where(x => x.ServiceReport!.ClientId != null && filterClientIds.Contains(x.ServiceReport!.ClientId!.Value));
            }

            var serviceReportItems = await serviceReportQuery
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    $"{x.ServiceReport!.ServiceReportNumber} - {x.ServiceReport!.JobNumber ?? x.ServiceReport!.SalesOrderNumber}",
                    x.Hours,
                    x.ServiceReport!.Client != null ? x.ServiceReport!.Client!.Name : NoClientSectionTitle))
                .ToListAsync();

            // Timesheet gaps (leave, sick, training, ...) are not tied to a client, so they are only meaningful
            // when the report covers every client.
            var timesheetDetailItems = isClientFiltered
                ? new List<TimesheetReportItem>()
                : await _aeroMechDBContext.TimesheetEmployeeDetails
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.Date >= fromDate && x.Date <= toDate
                        && !x.Employee!.IsDeleted && !x.Employee!.ExcludeFromTimesheets)
                    .Select(x => new TimesheetReportItem(
                        x.EmployeeId,
                        x.Employee!.FirstName,
                        x.Employee!.LastName,
                        x.Description.ToString(),
                        x.Hours,
                        null))
                    .ToListAsync();

            var employeeQuery = _aeroMechDBContext.Employees
                .AsNoTracking()
                .Where(emp => !emp.IsDeleted && !emp.ExcludeFromTimesheets);

            if (isClientFiltered)
            {
                // Only the employees who actually booked time against the selected clients - otherwise the matrix
                // is mostly empty columns.
                var employeeIdsWithHours = serviceReportItems.Select(x => x.EmployeeId).Distinct().ToList();
                employeeQuery = employeeQuery.Where(emp => employeeIdsWithHours.Contains(emp.Id));
            }

            var employees = await employeeQuery
                .OrderBy(emp => emp.LastName)
                .ThenBy(emp => emp.FirstName)
                .Select(emp => new TimesheetReportEmployee
                {
                    EmployeeId = emp.Id,
                    DisplayName = string.IsNullOrWhiteSpace(emp.FirstName)
                        ? emp.LastName
                        : $"{emp.FirstName[0]} {emp.LastName}".Trim()
                })
                .ToListAsync();

            var clientNames = isClientFiltered
                ? await _aeroMechDBContext.Clients
                    .AsNoTracking()
                    .Where(c => filterClientIds.Contains(c.Id))
                    .OrderBy(c => c.Name)
                    .Select(c => c.Name)
                    .ToListAsync()
                : new List<string>();

            var rows = BuildReportRows(timesheetDetailItems, serviceReportItems, employees, includeGapsSection: !isClientFiltered);

            return new TimesheetReportData(employees, rows, clientNames, isClientFiltered);
        }

        private static List<TimesheetReportRow> BuildReportRows(
            List<TimesheetReportItem> timesheetDetailItems,
            List<TimesheetReportItem> serviceReportItems,
            List<TimesheetReportEmployee> employees,
            bool includeGapsSection)
        {
            var rows = new List<TimesheetReportRow>();

            if (includeGapsSection)
            {
                if (timesheetDetailItems.Count == 0)
                    timesheetDetailItems.Add(new(0, string.Empty, string.Empty, string.Empty, 0, null));

                AddSection(
                    rows,
                    sectionTitle: "Time Sheet Gaps",
                    items: timesheetDetailItems,
                    employees: employees);
            }

            if (serviceReportItems.Count == 0)
            {
                AddSection(
                    rows,
                    sectionTitle: "Weekdays",
                    items: new List<TimesheetReportItem> { new(0, string.Empty, string.Empty, string.Empty, 0, null) },
                    employees: employees);
            }
            else
            {
                // One section per client so the hours are grouped - and sub-totalled - by who they were done for.
                var clientGroups = serviceReportItems
                    .GroupBy(x => x.ClientName ?? NoClientSectionTitle)
                    .OrderBy(g => g.Key == NoClientSectionTitle ? 1 : 0)
                    .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var clientGroup in clientGroups)
                {
                    AddSection(
                        rows,
                        sectionTitle: clientGroup.Key,
                        items: clientGroup.ToList(),
                        employees: employees);
                }
            }

            // Built from the section totals rather than the raw items, so the bottom row can never
            // disagree with the subtotals printed above it.
            var sectionTotals = rows.Where(r => r.IsTotalRow).ToList();

            rows.Add(new TimesheetReportRow
            {
                SectionTitle = "Total",
                ShowSectionTitle = true,
                RowTitle = string.Empty,
                IsTotalRow = true,
                IsGrandTotalRow = true,
                HoursByEmployeeId = employees.ToDictionary(
                    e => e.EmployeeId,
                    e => sectionTotals.Sum(r => r.HoursByEmployeeId.TryGetValue(e.EmployeeId, out var hours) ? hours : 0))
            });

            return rows;
        }

        private static DateOnly GetWeekStart(DateOnly date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var monday = (int)DayOfWeek.Monday;
            var delta = (7 + (dayOfWeek - monday)) % 7;
            return date.AddDays(-delta);
        }

        private static void AddSection(
            List<TimesheetReportRow> rows,
            string sectionTitle,
            IEnumerable<TimesheetReportItem> items,
            List<TimesheetReportEmployee> employees)
        {
            var grouped = items
                .GroupBy(x => x.RowKey)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var isFirst = true;
            foreach (var g in grouped)
            {
                var row = new TimesheetReportRow
                {
                    SectionTitle = sectionTitle,
                    ShowSectionTitle = isFirst,
                    RowTitle = g.Key,
                    IsTotalRow = false,
                    HoursByEmployeeId = employees.ToDictionary(
                        e => e.EmployeeId,
                        e => g.Where(x => x.EmployeeId == e.EmployeeId).Sum(x => x.Hours))
                };
                rows.Add(row);
                isFirst = false;
            }

            // Section total
            var totalRow = new TimesheetReportRow
            {
                SectionTitle = sectionTitle,
                ShowSectionTitle = false,
                RowTitle = "Total",
                IsTotalRow = true,
                HoursByEmployeeId = employees.ToDictionary(
                    e => e.EmployeeId,
                    e => grouped.Sum(g => g.Where(x => x.EmployeeId == e.EmployeeId).Sum(x => x.Hours)))
            };
            rows.Add(totalRow);
        }

        private static byte[] GenerateExcelFile(
            List<TimesheetReportEmployee> employees,
            List<TimesheetReportRow> rows,
            TimesheetReportParameters parameters)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Timesheet Report");

            int currentRow = 1;

            worksheet.Cell(currentRow, 1).Value = "Timesheet Report";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
            currentRow += 2;

            worksheet.Cell(currentRow, 1).Value = "Type:";
            worksheet.Cell(currentRow, 2).Value = parameters.ReportType;
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Period:";
            worksheet.Cell(currentRow, 2).Value = parameters.PeriodLabel;
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Clients:";
            worksheet.Cell(currentRow, 2).Value = parameters.ClientFilterLabel;
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            currentRow++;

            currentRow++;

            int currentCol = 3;
            foreach (var employee in employees)
            {
                worksheet.Cell(currentRow, currentCol).Value = employee.DisplayName;
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGray;
                currentCol++;
            }

            worksheet.Cell(currentRow, currentCol).Value = "Total";
            worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
            worksheet.Cell(currentRow, currentCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGray;
            currentRow++;

            foreach (var row in rows)
            {
                if (row.ShowSectionTitle)
                {
                    worksheet.Cell(currentRow, 1).Value = row.SectionTitle;
                    worksheet.Cell(currentRow, 1).Style.Font.Bold = true;

                    if (!row.IsTotalRow)
                    {
                        worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    }
                    else
                    {
                        worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                }
                else if (row.IsTotalRow)
                {
                    worksheet.Cell(currentRow, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                worksheet.Cell(currentRow, 2).Value = row.RowTitle;
                worksheet.Cell(currentRow, 2).Style.Font.Bold = row.IsTotalRow;

                if (row.IsTotalRow)
                {
                    worksheet.Cell(currentRow, 2).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                currentCol = 3;
                double rowTotal = 0;

                foreach (var employee in employees)
                {
                    var hours = row.HoursByEmployeeId.GetValueOrDefault(employee.EmployeeId, 0);
                    if (hours > 0)
                    {
                        worksheet.Cell(currentRow, currentCol).Value = hours;
                        worksheet.Cell(currentRow, currentCol).Style.NumberFormat.Format = "0.00";
                    }
                    rowTotal += hours;

                    if (row.IsTotalRow)
                    {
                        worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;
                        worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGray;
                    }

                    currentCol++;
                }

                worksheet.Cell(currentRow, currentCol).Value = rowTotal;
                worksheet.Cell(currentRow, currentCol).Style.NumberFormat.Format = "0.00";
                worksheet.Cell(currentRow, currentCol).Style.Font.Bold = true;

                if (row.IsTotalRow)
                {
                    worksheet.Cell(currentRow, currentCol).Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                currentRow++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
