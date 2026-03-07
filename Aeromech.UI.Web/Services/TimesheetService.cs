using AeroMech.Data.Persistence;
using AeroMech.Models.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    public class TimesheetService
    {
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly IMapper _mapper;

        public TimesheetService(IDbContextFactory<AeroMechDBContext> contextFactory,
            IMapper mapper
            )
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
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

        public async Task<List<TimesheetEmployeeDetailModel>> GetEmployeeTimesheetDataAsync(int employeeId, DateOnly date)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var employeeTimesheetDetail = await _aeroMechDBContext.TimesheetEmployeeDetails.AsNoTracking()
                .Where(emp => emp.EmployeeId == employeeId && emp.Date == date && !emp.IsDeleted)
                .ToListAsync();

            return _mapper.Map<List<TimesheetEmployeeDetailModel>>(employeeTimesheetDetail);
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
    }
}
