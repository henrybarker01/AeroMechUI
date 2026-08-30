using AeroMech.Data.Enums;
using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;
using System.ComponentModel;
using System.Reflection;
using Modal = BlazorBootstrap.Modal;

namespace AeroMech.UI.Web.Pages.Timesheets
{
    public partial class Timesheets
    {
        [Inject] private TimesheetService _timesheetService { get; set; } = default!;
        [Inject] private LoaderService _loaderService { get; set; } = default!;
        [Inject] private NavigationManager _navigationManager { get; set; } = default!;

        [Parameter] public string? SelectedDate { get; set; }

        private const string EditTitle = "Edit timesheet detail";
        private const string AddTitle = "Add timesheet detail";

        /// <summary>The lengths that get typed most, offered as taps so a phone need not.</summary>
        private static readonly double[] HourPresets = { 0.5, 1, 2, 4, 8, 9 };

        private List<TimesheetDateModel> _timesheets = new();
        private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
        private List<TimesheetEmployeeHoursModel> _timesheetEmployeeHours = new();
        private TimesheetDateModel? _timesheetDateModel;
        private List<TimesheetEmployeeLineModel> _timesheetEmployeeDetails = new();
        private TimesheetEmployeeDetailModel _timesheetEmployeeDetail = new();
        private string _title = string.Empty;
        private Modal _modal = default!;

        private DateOnly? _selectedTimesheetDate;
        private int? _selectedEmployeeId;

        /// <summary>
        /// How far into the day - employee - lines drill-down the user is. On a wide screen all
        /// three panels are on show regardless; on a phone this is what picks the one to display.
        /// </summary>
        private string Step => _selectedTimesheetDate is null
            ? "days"
            : _selectedEmployeeId is null ? "employees" : "lines";

        private string SelectedDayLabel => _timesheetDateModel is null
            ? "No day chosen"
            : _timesheetDateModel.Date.ToString("ddd d MMM yyyy");

        private string SelectedDayLongLabel => _timesheetDateModel is null
            ? string.Empty
            : _timesheetDateModel.Date.ToString("dddd d MMMM yyyy");

        private string SelectedEmployeeName
            => _timesheetEmployeeHours.FirstOrDefault(x => x.EmployeeId == _selectedEmployeeId)?.EmployeeName
               ?? "No employee chosen";

        private double DayTotalHours
            => _timesheetEmployeeHours.Sum(x => x.ServiceReportHours + x.TimesheetHours);

        private int EmployeesWithHours
            => _timesheetEmployeeHours.Count(x => x.ServiceReportHours + x.TimesheetHours > 0);

        private int EmployeesWithoutHours
            => _timesheetEmployeeHours.Count - EmployeesWithHours;

        /// <summary>Trims trailing zeros so a grid of whole hours does not read as "8.00".</summary>
        private static string FormatHours(double hours) => hours.ToString("0.##");

        private static bool MatchesEmployee(TimesheetEmployeeHoursModel employee, string term)
            => employee.EmployeeName.Contains(term, StringComparison.OrdinalIgnoreCase)
               || employee.EmployeeNumber.Contains(term, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gap types carry a <see cref="DescriptionAttribute"/> where their name runs two words
        /// together, so the screen says "Public Holiday" rather than "PublicHoliday".
        /// </summary>
        private static string GapTypeLabel(TimesheetGapTypes gapType)
            => typeof(TimesheetGapTypes).GetField(gapType.ToString())?
                   .GetCustomAttribute<DescriptionAttribute>()?.Description
               ?? gapType.ToString();

        private static string LineDescription(TimesheetEmployeeLineModel line)
            => line.GapType is null ? line.Description : GapTypeLabel(line.GapType.Value);

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _loaderService.ShowLoader();
                await LoadTimesheetsAsync();
                await AutoSelectDateIfProvided();
                _loaderService.HideLoader();
            }
        }

        private async Task LoadTimesheetsAsync()
        {
            ClearSelection();
            _timesheets = await _timesheetService.GetTimesheetDatesFrom(_startDate);
            await InvokeAsync(StateHasChanged);
        }

        private async Task AutoSelectDateIfProvided()
        {
            if (!string.IsNullOrWhiteSpace(SelectedDate) && DateOnly.TryParse(SelectedDate, out var parsedDate))
            {
                var timesheet = _timesheets.FirstOrDefault(t => t.Date == parsedDate);
                if (timesheet != null)
                {
                    await ViewDayAsync(timesheet);
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private void ClearSelection()
        {
            _selectedTimesheetDate = null;
            _selectedEmployeeId = null;
            _timesheetDateModel = null;
            _timesheetEmployeeHours = new();
            _timesheetEmployeeDetails = new();
        }

        // Stepping back out of the drill-down. Only reachable on a narrow screen, where one panel
        // is on show at a time; the wide layout keeps all three and has nothing to go back to.
        private void BackToDays() => ClearSelection();

        private void BackToEmployees()
        {
            _selectedEmployeeId = null;
            _timesheetEmployeeDetails = new();
        }

        private async Task ViewDayAsync(TimesheetDateModel timesheet)
        {
            _loaderService.ShowLoader();
            _selectedTimesheetDate = timesheet.Date;
            _selectedEmployeeId = null;
            _timesheetEmployeeDetails = new();
            _timesheetDateModel = timesheet;
            _timesheetEmployeeHours = await _timesheetService.GetTimesheetEmployeeDetailAsync(timesheet.Date);
            await InvokeAsync(StateHasChanged);
            _loaderService.HideLoader();
        }

        private async Task ViewEmplyeeTimesheetDetail(int employeeId)
        {
            if (_timesheetDateModel is null) return;

            _loaderService.ShowLoader();
            _selectedEmployeeId = employeeId;
            _timesheetEmployeeDetails = await _timesheetService.GetEmployeeTimesheetDataAsync(employeeId, _timesheetDateModel.Date);
            _loaderService.HideLoader();
        }

        private async Task AddToEmployeeTimesheetAsync(TimesheetEmployeeHoursModel? timesheetEmployeeHours = null)
        {
            if (_timesheetDateModel is null) return;

            // Adding from an employee's row also makes that row the selection, so the panel below
            // fills in with the lines the new one is joining.
            if (timesheetEmployeeHours is not null)
                await ViewEmplyeeTimesheetDetail(timesheetEmployeeHours.EmployeeId);

            if (_selectedEmployeeId is null) return;

            _title = AddTitle;

            // A fresh model every time. Reusing the last one carried the Id of a line that had
            // just been edited into the next add, which is an insert wearing an existing row's
            // identity - harmless only for as long as the key stays database-generated.
            _timesheetEmployeeDetail = new()
            {
                EmployeeId = _selectedEmployeeId.Value,
                Date = _timesheetDateModel.Date,
                Description = TimesheetGapTypes.General,
                Hours = 0
            };

            await _modal.ShowAsync();
        }

        private async Task AddLineToEmployeeTimesheetDetailAsync()
        {
            _loaderService.ShowLoader();
            if (_title == EditTitle)
            {
                await _timesheetService.EditEmployeeTimesheetDetailAsync(_timesheetEmployeeDetail);
            }
            else
            {
                await _timesheetService.AddLineToEmployeeTimesheetDetailAsync(_timesheetEmployeeDetail);
            }

            await ViewEmplyeeTimesheetDetail(_timesheetEmployeeDetail.EmployeeId);
            RefreshTotalsFromLoadedLines();
            _title = string.Empty;
            await InvokeAsync(StateHasChanged);
            _loaderService.HideLoader();
            await _modal.HideAsync();
        }

        private async Task OnHideModalClick()
        {
            _timesheetEmployeeDetail = new()
            {
                EmployeeId = _selectedEmployeeId ?? 0,
                Date = _timesheetDateModel?.Date ?? default
            };
            _title = string.Empty;
            await _modal.HideAsync();
        }

        private async Task EditEmployeeTimesheetAsync(TimesheetEmployeeLineModel timesheetLine)
        {
            if (_timesheetDateModel is null) return;

            _title = EditTitle;
            _timesheetEmployeeDetail = new()
            {
                Id = timesheetLine.Id,
                EmployeeId = timesheetLine.EmployeeId,
                Date = _timesheetDateModel.Date,
                Description = timesheetLine.GapType ?? TimesheetGapTypes.General,
                Hours = timesheetLine.Hours
            };

            await _modal.ShowAsync();
        }

        private async Task DeleteEmployeeTimesheetDetailAsync(TimesheetEmployeeLineModel timesheetLine)
        {
            _loaderService.ShowLoader();
            await _timesheetService.DeleteEmployeeTimesheetDetailAsync(timesheetLine.Id);
            await ViewEmplyeeTimesheetDetail(timesheetLine.EmployeeId);
            RefreshTotalsFromLoadedLines();
            _loaderService.HideLoader();
        }

        /// <summary>
        /// Re-adds the selected employee's hours from the lines that were just reloaded, and the
        /// day's total from every employee on it.
        ///
        /// The totals used to be nudged by hand either side of the modal - the edit path took the
        /// old hours off before opening it and the save path put the new ones back - so cancelling
        /// an edit left both figures short until the page was reloaded. Counting what is actually
        /// on screen cannot drift.
        /// </summary>
        private void RefreshTotalsFromLoadedLines()
        {
            var employee = _timesheetEmployeeHours.FirstOrDefault(x => x.EmployeeId == _selectedEmployeeId);
            if (employee is not null)
            {
                employee.ServiceReportHours = _timesheetEmployeeDetails.Where(l => l.IsServiceReport).Sum(l => l.Hours);
                employee.TimesheetHours = _timesheetEmployeeDetails.Where(l => !l.IsServiceReport).Sum(l => l.Hours);
                employee.TotalHours = employee.ServiceReportHours + employee.TimesheetHours;
            }

            if (_timesheetDateModel is not null)
                _timesheetDateModel.TotalWorked = DayTotalHours;
        }

        private void ViewServiceReport(int serviceReportId)
            => _navigationManager.NavigateTo($"/add-service-report/{serviceReportId}");

        private void PrintDay(TimesheetDateModel timesheet)
            => _navigationManager.NavigateTo($"/ShowTimesheetReport/{timesheet.Date:yyyy-MM-dd}");
    }
}
