using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Widgets.ServiceReport
{
    public partial class ServiceReportWidget
    {
        [Inject] ServiceReportService ServiceReportService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        List<ServiceReportModel> serviceReports { get; set; }

        protected override async Task OnInitializedAsync()
        {
            serviceReports = await ServiceReportService.GetRecentServiceReports(DateTime.Now.AddMonths(-1));

            // return base.OnInitializedAsync();
        }

        private void LoadServiceReport(int Id)
        {
            NavigationManager.NavigateTo($"/ShowPDF/{Id}");
        }
    }
}