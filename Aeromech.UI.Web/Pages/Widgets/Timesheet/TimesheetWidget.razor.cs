using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Widgets.Timesheet
{
    public partial class TimesheetWidget
    {
        [Inject] TimesheetService TimesheetService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        List<TimesheetDateModel> weekDays = new List<TimesheetDateModel>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await LoadCurrentWeekTimesheets();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task LoadCurrentWeekTimesheets()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var currentDayOfWeek = (int)today.DayOfWeek;
            var mondayOffset = currentDayOfWeek == 0 ? -6 : -(currentDayOfWeek - 1);
            var monday = today.AddDays(mondayOffset);

            var allTimesheets = await TimesheetService.GetTimesheetDatesFrom(monday);

            weekDays = allTimesheets
                .Where(t => t.Date >= monday && t.Date < monday.AddDays(7))
                .OrderBy(t => t.Date)
                .ToList();
        }

        private string GetStatusClass(double hours)
        {
            if (hours == 0) return "status-missing";
            if (hours < 8) return "status-incomplete";
            return "status-complete";
        }

        private string GetStatusText(double hours)
        {
            if (hours == 0) return "Missing";
            if (hours < 8) return "Incomplete";
            return "Complete";
        }

        private void ViewTimesheet(DateOnly date)
        {
            NavigationManager.NavigateTo($"/timesheets/{date:yyyy-MM-dd}");
        }
    }
}
