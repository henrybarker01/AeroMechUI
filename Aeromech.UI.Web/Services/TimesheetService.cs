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
        private sealed record TimesheetReportItem(int EmployeeId, string FirstName, string LastName, string RowKey, double Hours);

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
                .Where(sr => sr.DutyDate >= startDate && !sr.IsDeleted)
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
                .Where(x => x.Date >= startDate && !x.IsDeleted)
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
                .Where(emp => !emp.IsDeleted)
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

        public async Task<byte[]> DownloadTimesheetReportAsync(DateOnly anyDateInWeek)
        {
            var weekStart = GetWeekStart(anyDateInWeek);
            var weekEnd = weekStart.AddDays(7);

            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var serviceReportItems = await _aeroMechDBContext.ServiceReportEmployees
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.DutyDate >= weekStart && x.DutyDate < weekEnd)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    $"{x.ServiceReport!.ServiceReportNumber} - {x.ServiceReport!.JobNumber ?? x.ServiceReport!.SalesOrderNumber}",
                    x.Hours))
                .ToListAsync();

            var timesheetDetailItems = await _aeroMechDBContext.TimesheetEmployeeDetails
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Date >= weekStart && x.Date < weekEnd)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    x.Description.ToString(),
                    x.Hours))
                .ToListAsync();

            var employees = await _aeroMechDBContext.Employees
                .AsNoTracking()
                .Where(emp => !emp.IsDeleted)
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

            var rows = BuildReportRows(timesheetDetailItems, serviceReportItems, employees);

            _timesheetReport.Data = new TimesheetReportDocumentData
            {
                WeekStartDate = weekStart,
                WeekNumber = ISOWeek.GetWeekOfYear(weekStart.ToDateTime(TimeOnly.MinValue)),
                Employees = employees,
                Rows = rows
            };

            return Document.Create(_timesheetReport.Compose).GeneratePdf();
        }

        public async Task<byte[]> DownloadDailyTimesheetReportAsync(DateOnly date)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var serviceReportItems = await _aeroMechDBContext.ServiceReportEmployees
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.DutyDate == date)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    $"{x.ServiceReport!.ServiceReportNumber} - {x.ServiceReport!.JobNumber ?? x.ServiceReport!.SalesOrderNumber}",
                    x.Hours))
                .ToListAsync();

            var timesheetDetailItems = await _aeroMechDBContext.TimesheetEmployeeDetails
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Date == date)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    x.Description.ToString(),
                    x.Hours))
                .ToListAsync();

            var employees = await _aeroMechDBContext.Employees
                .AsNoTracking()
                .Where(emp => !emp.IsDeleted)
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

            var rows = BuildReportRows(timesheetDetailItems, serviceReportItems, employees);

            _timesheetReport.Data = new TimesheetReportDocumentData
            {
                WeekStartDate = date,
                WeekNumber = ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue)),
                Employees = employees,
                Rows = rows
            };

            return Document.Create(_timesheetReport.Compose).GeneratePdf();
        }

        public async Task<byte[]> DownloadDateRangeTimesheetReportAsync(DateOnly fromDate, DateOnly toDate)
        {
            if (toDate < fromDate)
                (fromDate, toDate) = (toDate, fromDate);

            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var serviceReportItems = await _aeroMechDBContext.ServiceReportEmployees
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.DutyDate >= fromDate && x.DutyDate <= toDate)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    $"{x.ServiceReport!.ServiceReportNumber} - {x.ServiceReport!.JobNumber ?? x.ServiceReport!.SalesOrderNumber}",
                    x.Hours))
                .ToListAsync();

            var timesheetDetailItems = await _aeroMechDBContext.TimesheetEmployeeDetails
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Date >= fromDate && x.Date <= toDate)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    x.Description.ToString(),
                    x.Hours))
                .ToListAsync();

            var employees = await _aeroMechDBContext.Employees
                .AsNoTracking()
                .Where(emp => !emp.IsDeleted)
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

            var rows = BuildReportRows(timesheetDetailItems, serviceReportItems, employees);

            _timesheetReport.Data = new TimesheetReportDocumentData
            {
                WeekStartDate = fromDate,
                WeekNumber = ISOWeek.GetWeekOfYear(fromDate.ToDateTime(TimeOnly.MinValue)),
                Employees = employees,
                Rows = rows
            };

            return Document.Create(_timesheetReport.Compose).GeneratePdf();
        }

        private static List<TimesheetReportRow> BuildReportRows(
            List<TimesheetReportItem> timesheetDetailItems,
            List<TimesheetReportItem> serviceReportItems,
            List<TimesheetReportEmployee> employees)
        {
            var rows = new List<TimesheetReportRow>();

            if (timesheetDetailItems.Count == 0)
                timesheetDetailItems.Add(new(0, string.Empty, string.Empty, string.Empty, 0));

            AddSection(
                rows,
                sectionTitle: "Time Sheet Gaps",
                items: timesheetDetailItems,
                employees: employees);

            if (serviceReportItems.Count == 0)
                serviceReportItems.Add(new(0, string.Empty, string.Empty, string.Empty, 0));

            AddSection(
                rows,
                sectionTitle: "Weekdays",
                items: serviceReportItems,
                employees: employees);

            rows.Add(new TimesheetReportRow
            {
                SectionTitle = "Total",
                ShowSectionTitle = true,
                RowTitle = string.Empty,
                IsTotalRow = true,
                HoursByEmployeeId = employees.ToDictionary(
                    e => e.EmployeeId,
                    e => serviceReportItems.Where(x => x.EmployeeId == e.EmployeeId).Sum(x => x.Hours)
                       + timesheetDetailItems.Where(x => x.EmployeeId == e.EmployeeId).Sum(x => x.Hours))
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

        public async Task<byte[]> ExportWeeklyTimesheetToExcelAsync(DateOnly anyDateInWeek)
        {
            var weekStart = GetWeekStart(anyDateInWeek);
            var weekEnd = weekStart.AddDays(7);

            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var serviceReportItems = await _aeroMechDBContext.ServiceReportEmployees
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.DutyDate >= weekStart && x.DutyDate < weekEnd)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    $"{x.ServiceReport!.ServiceReportNumber} - {x.ServiceReport!.JobNumber ?? x.ServiceReport!.SalesOrderNumber}",
                    x.Hours))
                .ToListAsync();

            var timesheetDetailItems = await _aeroMechDBContext.TimesheetEmployeeDetails
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Date >= weekStart && x.Date < weekEnd)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    x.Description.ToString(),
                    x.Hours))
                .ToListAsync();

            var employees = await _aeroMechDBContext.Employees
                .AsNoTracking()
                .Where(emp => !emp.IsDeleted)
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

            var rows = BuildReportRows(timesheetDetailItems, serviceReportItems, employees);

            return GenerateExcelFile(
                employees,
                rows,
                weekStart,
                ISOWeek.GetWeekOfYear(weekStart.ToDateTime(TimeOnly.MinValue)));
        }

        public async Task<byte[]> ExportDailyTimesheetToExcelAsync(DateOnly date)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var serviceReportItems = await _aeroMechDBContext.ServiceReportEmployees
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.DutyDate == date)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    $"{x.ServiceReport!.ServiceReportNumber} - {x.ServiceReport!.JobNumber ?? x.ServiceReport!.SalesOrderNumber}",
                    x.Hours))
                .ToListAsync();

            var timesheetDetailItems = await _aeroMechDBContext.TimesheetEmployeeDetails
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Date == date)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    x.Description.ToString(),
                    x.Hours))
                .ToListAsync();

            var employees = await _aeroMechDBContext.Employees
                .AsNoTracking()
                .Where(emp => !emp.IsDeleted)
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

            var rows = BuildReportRows(timesheetDetailItems, serviceReportItems, employees);

            return GenerateExcelFile(
                employees,
                rows,
                date,
                ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue)));
        }

        public async Task<byte[]> ExportDateRangeTimesheetToExcelAsync(DateOnly fromDate, DateOnly toDate)
        {
            if (toDate < fromDate)
                (fromDate, toDate) = (toDate, fromDate);

            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var serviceReportItems = await _aeroMechDBContext.ServiceReportEmployees
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.DutyDate >= fromDate && x.DutyDate <= toDate)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    $"{x.ServiceReport!.ServiceReportNumber} - {x.ServiceReport!.JobNumber ?? x.ServiceReport!.SalesOrderNumber}",
                    x.Hours))
                .ToListAsync();

            var timesheetDetailItems = await _aeroMechDBContext.TimesheetEmployeeDetails
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Date >= fromDate && x.Date <= toDate)
                .Select(x => new TimesheetReportItem(
                    x.EmployeeId,
                    x.Employee!.FirstName,
                    x.Employee!.LastName,
                    x.Description.ToString(),
                    x.Hours))
                .ToListAsync();

            var employees = await _aeroMechDBContext.Employees
                .AsNoTracking()
                .Where(emp => !emp.IsDeleted)
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

            var rows = BuildReportRows(timesheetDetailItems, serviceReportItems, employees);

            return GenerateExcelFile(
                employees,
                rows,
                fromDate,
                ISOWeek.GetWeekOfYear(fromDate.ToDateTime(TimeOnly.MinValue)));
        }

        private static byte[] GenerateExcelFile(
            List<TimesheetReportEmployee> employees,
            List<TimesheetReportRow> rows,
            DateOnly startDate,
            int weekNumber)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Timesheet Report");

            int currentRow = 1;

            worksheet.Cell(currentRow, 1).Value = "Timesheet Report";
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 16;
            currentRow += 2;

            worksheet.Cell(currentRow, 1).Value = "Week No:";
            worksheet.Cell(currentRow, 2).Value = weekNumber;
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            currentRow++;

            worksheet.Cell(currentRow, 1).Value = "Date:";
            worksheet.Cell(currentRow, 2).Value = startDate.ToString("dd/MM/yyyy");
            worksheet.Cell(currentRow, 1).Style.Font.Bold = true;
            currentRow += 2;

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
