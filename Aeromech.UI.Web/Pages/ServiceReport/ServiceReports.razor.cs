using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.ServiceReport
{
    public partial class ServiceReports
    {
        [Inject] private NavigationManager _navigationManager { get; set; }
        [Inject] private ServiceReportService _serviceReportService { get; set; }
        [Inject] private LoaderService _loaderService { get; set; }

        private List<ServiceReportModel> _serviceReports = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            await GetServiceReports();
        }

        private async Task GetServiceReports()
        {
            _loaderService.ShowLoader();
            _serviceReports = await _serviceReportService.GetRecentServiceReports();
            _loaderService.HideLoader();
            await InvokeAsync(StateHasChanged);
        }

        private bool MatchesSearch(ServiceReportModel sr, string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return true;
            var term = q.Trim();
            return (sr.Description ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
            || (sr.DetailedServiceReport ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
            || (sr.Instruction ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
            || sr.QuoteNumber.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
            || (sr.SalesOrderNumber ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
            || (sr.JobNumber ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
            || sr.Id.ToString().Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private void NavigateToAdd() => _navigationManager.NavigateTo("/add-service-report");
        private void Edit(int id) => _navigationManager.NavigateTo($"/add-service-report/{id}");
        private void Print(int id) => _navigationManager.NavigateTo($"/ShowPDF/{id}");
        private double CalculateTotal(ServiceReportModel sr) => _serviceReportService.CalculateServiceReportTotal(sr);
    }
}