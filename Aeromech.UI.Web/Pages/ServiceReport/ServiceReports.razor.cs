using AeroMech.Models;
using AeroMech.UI.Web.Pages.Quote;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.ServiceReport
{
    public partial class ServiceReports
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ServiceReportService ServiceReportService { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }

        private List<ServiceReportModel>? serviceReports = new List<ServiceReportModel>();

        private string SearchTerm { get; set; } = string.Empty;
        private IEnumerable<ServiceReportModel> FilteredServiceReports =>
       serviceReports.Where(serviceReport =>
           string.IsNullOrEmpty(SearchTerm) ||
           (serviceReport.Description ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
           (serviceReport.DetailedServiceReport ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
           (serviceReport.Instruction ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
           (serviceReport.QuoteNumber.ToString() ?? "").ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
           (serviceReport.SalesOrderNumber ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
           (serviceReport.JobNumber ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
           serviceReport.Id.ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
       );
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await GetServiceReports();
            }
        }

        private async Task GetServiceReports()
        {
            _loaderService.ShowLoader();
            var fromDate = DateTime.Now.AddMonths(-2);
            serviceReports = await ServiceReportService.GetRecentServiceReports(fromDate);
            await InvokeAsync(StateHasChanged);
            _loaderService.HideLoader();
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