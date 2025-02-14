using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Widgets.ServiceReport
{
    public partial class ServiceReportWidget
    {
        [Inject] ServiceReportService ServiceReportService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        List<ServiceReportModel> serviceReports = new List<ServiceReportModel>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                serviceReports = await ServiceReportService.GetRecentServiceReports(DateTime.Now.AddMonths(-1));
                await InvokeAsync(StateHasChanged);
            }
        }

        private void PrintServiceReport(ServiceReportModel serviceReport)
        {
            if (serviceReport.IsComplete)
                NavigationManager.NavigateTo($"/ShowPDF/{serviceReport.Id}");


        }

        private void EditServiceReport(int serviceReportId)
        {
            NavigationManager.NavigateTo($"/add-service-report/{serviceReportId}");
        }
    }
}