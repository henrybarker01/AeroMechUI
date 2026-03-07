using AeroMech.Data.Models;
using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;
using Modal = BlazorBootstrap.Modal;

namespace AeroMech.UI.Web.Pages.Timesheets
{
    public partial class Timesheets
    {
        [Inject] private TimesheetService _timesheetService { get; set; }
        [Inject] private LoaderService _loaderService { get; set; }

        [Parameter] public string? SelectedDate { get; set; }

        private List<TimesheetDateModel> _timesheets = new();
        private DateOnly _startDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-30));
        private List<TimesheetEmployeeHoursModel> _timesheetEmployeeHours = new();
        private TimesheetDateModel _timesheetDateModel;
        private List<TimesheetEmployeeDetailModel> _timesheetEmployeeDetails = new();
        private TimesheetEmployeeDetailModel _timesheetEmployeeDetail = new();
        private string _title = string.Empty;
        private Modal _modal = default!;

        private DateOnly? _selectedTimesheetDate;
        private int? _selectedEmployeeId;

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
            _selectedTimesheetDate = null;
            _selectedEmployeeId = null;
            _timesheetEmployeeHours = new();
            _timesheetEmployeeDetails = new();
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
            _loaderService.ShowLoader();
            _selectedEmployeeId = employeeId;
            _timesheetEmployeeDetail.EmployeeId = employeeId;
            _timesheetEmployeeDetails = await _timesheetService.GetEmployeeTimesheetDataAsync(employeeId, _timesheetDateModel.Date);
            _loaderService.HideLoader();
        }

        private async Task AddToEmployeeTimesheetAsync(TimesheetEmployeeHoursModel? timesheetEmployeeHours = null)
        {
            _title = "Add timesheet detail";
            if (timesheetEmployeeHours is not null)
            {
                _timesheetEmployeeDetail = new();
                _timesheetEmployeeDetail.EmployeeId = timesheetEmployeeHours.EmployeeId;
            }
            else
            {
                _timesheetEmployeeDetail.Description = AeroMech.Data.Enums.TimesheetGapTypes.General;
                _timesheetEmployeeDetail.Hours = 0;
            }

            _timesheetEmployeeDetail.Date = _timesheetDateModel.Date;
            await _modal.ShowAsync();
        }

        private async Task AddLineToEmployeeTimesheetDetailAsync()
        {
            _loaderService.ShowLoader();
            if (_title == "Edit timesheet detail")
            {
                await _timesheetService.EditEmployeeTimesheetDetailAsync(_timesheetEmployeeDetail);
            }
            else
            {
                await _timesheetService.AddLineToEmployeeTimesheetDetailAsync(_timesheetEmployeeDetail);
            }

            _timesheetEmployeeHours
                    .FirstOrDefault(x => x.EmployeeId == _timesheetEmployeeDetail.EmployeeId)?.TimesheetHours += _timesheetEmployeeDetail.Hours;

            _timesheets.FirstOrDefault(x => x.Date == _timesheetEmployeeDetail.Date)?.TotalWorked += _timesheetEmployeeDetail.Hours;

            await ViewEmplyeeTimesheetDetail(_timesheetEmployeeDetail.EmployeeId);
            await InvokeAsync(StateHasChanged);
            _loaderService.HideLoader();
            await _modal.HideAsync();
        }

        private async Task OnHideModalClick()
        {
            _timesheetEmployeeDetail = new();
            _title = string.Empty;
            await _modal.HideAsync();
        }

        private async Task EditEmployeeTimesheetAsync(TimesheetEmployeeDetailModel timesheetEmployeeHours)
        {
            _title = "Edit timesheet detail";
            _timesheetEmployeeDetail.Id = timesheetEmployeeHours.Id;
            _timesheetEmployeeDetail.EmployeeId = timesheetEmployeeHours.EmployeeId;
            _timesheetEmployeeDetail.Date = _timesheetDateModel.Date;
            _timesheetEmployeeDetail.Description = timesheetEmployeeHours.Description;
            _timesheetEmployeeDetail.Hours = timesheetEmployeeHours.Hours;

            _timesheetEmployeeHours.First(x => x.EmployeeId == _timesheetEmployeeDetail.EmployeeId)?
               .TimesheetHours -= timesheetEmployeeHours?.Hours ?? 0;

            _timesheets.FirstOrDefault(x => x.Date == _timesheetEmployeeDetail.Date)?.TotalWorked -= timesheetEmployeeHours?.Hours ?? 0;

            await _modal.ShowAsync();
        }

        private async Task DeleteEmployeeTimesheetDetailAsync(TimesheetEmployeeDetailModel timesheetEmployeeHours)
        {
            _loaderService.ShowLoader();
            await _timesheetService.DeleteEmployeeTimesheetDetailAsync(timesheetEmployeeHours.Id);
            _timesheetEmployeeHours.First(x => x.EmployeeId == _timesheetEmployeeDetail.EmployeeId)?
              .TimesheetHours -= timesheetEmployeeHours?.Hours ?? 0;
            _timesheets.FirstOrDefault(x => x.Date == _timesheetEmployeeDetail.Date)?.TotalWorked -= timesheetEmployeeHours?.Hours ?? 0;
            await ViewEmplyeeTimesheetDetail(_timesheetEmployeeDetail.EmployeeId);
            _loaderService.HideLoader();
            await _modal.HideAsync();
        }
    }
}
