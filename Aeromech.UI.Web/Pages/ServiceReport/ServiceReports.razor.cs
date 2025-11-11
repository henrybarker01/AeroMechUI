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

        private List<ServiceReportModel> serviceReports = new();
        private bool isLoading;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            await GetServiceReports();
        }

        private async Task GetServiceReports()
        {
            isLoading = true;
            _loaderService.ShowLoader();
            serviceReports = await _serviceReportService.GetRecentServiceReports();
            _loaderService.HideLoader();
            isLoading = false;
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
        // [Inject] private NavigationManager _navigationManager { get; set; }
        // [Inject] private ServiceReportService _serviceReportService { get; set; }
        // [Inject] private LoaderService _loaderService { get; set; }

        // private int CurrentPage = 1;
        // private int PageSize = 15;
        // private int TotalPages => (int)Math.Ceiling((double)(serviceReports?.Count ?? 1) / PageSize);
        // private bool IsFirstPage => CurrentPage == 1;
        // private bool IsLastPage => CurrentPage >= TotalPages;

        // private void NextPage()
        // {
        //     if (IsLastPage) return;
        //     CurrentPage++;
        //     StateHasChanged();
        // }

        // private void PreviousPage()
        // {
        //     if (IsFirstPage) return;
        //     CurrentPage--;
        //     StateHasChanged();
        // }

        // private List<ServiceReportModel>? serviceReports = new List<ServiceReportModel>();

        // private string SearchTerm { get; set; } = string.Empty;
        // private IEnumerable<ServiceReportModel> FilteredServiceReports =>
        //serviceReports.Where(serviceReport =>
        //            string.IsNullOrEmpty(SearchTerm) ||
        //            (serviceReport.Description ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            (serviceReport.DetailedServiceReport ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            (serviceReport.Instruction ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            (serviceReport.QuoteNumber.ToString() ?? "").ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            (serviceReport.SalesOrderNumber ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            (serviceReport.JobNumber ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            serviceReport.Id.ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
        //     ).Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        // protected override async Task OnAfterRenderAsync(bool firstRender)
        // {
        //     if (firstRender)
        //     {
        //         await GetServiceReports();
        //     }
        // }

        // private async Task GetServiceReports()
        // {
        //     _loaderService.ShowLoader();
        //     serviceReports = await _serviceReportService.GetRecentServiceReports();
        //     await InvokeAsync(StateHasChanged);
        //     _loaderService.HideLoader();
        // }

        // private void NavigateToAddServiceReport()
        // {
        //     _navigationManager.NavigateTo($"/add-service-report");
        // }

        // private void Search(ChangeEventArgs e)
        // {
        //     SearchTerm = e.Value.ToString();
        //     CurrentPage = 1;
        //     StateHasChanged();
        // }

        // private void EditServiceReport(int Id)
        // {
        //     _navigationManager.NavigateTo($"/add-service-report/{Id}");
        // }

        // private void PrintServiceReport(int Id)
        // {
        //     _navigationManager.NavigateTo($"/ShowPDF/{Id}");
        // }

        // private double CalcuateTotalReportValue(ServiceReportModel serviceReport)
        // {
        //     return _serviceReportService.CalculateServiceReportTotal(serviceReport);
        // }
    }
}