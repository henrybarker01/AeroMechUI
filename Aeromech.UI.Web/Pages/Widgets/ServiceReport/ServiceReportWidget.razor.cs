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
        }

        private void LoadServiceReport(ServiceReportModel serviceReport)
        {
            if (serviceReport.IsComplete)
                NavigationManager.NavigateTo($"/ShowPDF/{serviceReport.Id}");
            else
                NavigationManager.NavigateTo($"/add-service-report/{serviceReport.Id}");
        }
    }
}