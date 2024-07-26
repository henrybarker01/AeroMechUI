using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.ServiceReport
{
    public partial class ServiceReports
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ServiceReportService ServiceReportService { get; set; }

        private List<ServiceReportModel>? serviceReports;

        protected override async Task OnInitializedAsync()
        {
            await GetServiceReports();
        }

        private async Task GetServiceReports()
        {
            var fromDate = DateTime.Now.AddMonths(-2);
            serviceReports = await ServiceReportService.GetRecentServiceReports(fromDate);
        }

        private void NavigateToAddServiceReport()
        {
            NavigationManager.NavigateTo($"/add-service-report");
        }

        private void EditServiceReport(int Id)
        {
            NavigationManager.NavigateTo($"/add-service-report/{Id}");
        }

        private void PrintServiceReport(int Id)
        {
            NavigationManager.NavigateTo($"/ShowPDF/{Id}");
        }

        private double CalcuateTotalReportValue(ServiceReportModel serviceReport)
        {
            return ServiceReportService.CalculateServiceReportTotal(serviceReport);
        }
    }
}