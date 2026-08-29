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
            //|| (sr.DetailedServiceReport ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
            || (sr.Vehicle?.SerialNumber ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
            || (sr.Instruction ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
            || sr.QuoteNumber.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
            || (sr.SalesOrderNumber ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
            || (sr.JobNumber ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase)
            || sr.Id.ToString().Contains(term, StringComparison.OrdinalIgnoreCase);           
        }

        /// <summary>
        /// The stage a report has reached, shown as the colour of its leading cell and as the text
        /// of the Status column. <paramref name="Rank"/> is the order the stages run in, which is
        /// what the Status column sorts on.
        /// </summary>
        private sealed record ReportStatus(string Key, string Label, string Description, string Css, int Rank);

        private static readonly ReportStatus StatusNew = new("new", "New", "New - not yet quoted", "sr-new", 0);
        private static readonly ReportStatus StatusQuoted = new("quoted", "Quoted", "Quoted", "sr-quoted", 1);
        private static readonly ReportStatus StatusComplete = new("complete", "Complete", "Complete - sales order raised", "sr-complete", 2);

        private static readonly ReportStatus[] Statuses = { StatusNew, StatusQuoted, StatusComplete };

        // A report only reaches the next stage once the previous number exists, so the later
        // stage wins.
        private static ReportStatus RowStatus(ServiceReportModel sr)
        {
            if (!string.IsNullOrWhiteSpace(sr.SalesOrderNumber)) return StatusComplete;
            if (sr.QuoteNumber != 0) return StatusQuoted;
            return StatusNew;
        }

        // No chip picked means no filtering at all, so the grid opens on the full list.
        private readonly HashSet<string> _activeStatuses = new(StringComparer.Ordinal);

        private IEnumerable<ServiceReportModel> FilteredServiceReports
            => _activeStatuses.Count == 0
                ? _serviceReports
                : _serviceReports.Where(sr => _activeStatuses.Contains(RowStatus(sr).Key));

        private bool IsStatusActive(ReportStatus status) => _activeStatuses.Contains(status.Key);

        private void ToggleStatus(ReportStatus status)
        {
            if (!_activeStatuses.Add(status.Key))
                _activeStatuses.Remove(status.Key);
        }

        private void ClearStatusFilter() => _activeStatuses.Clear();

        // Counted over every report, not the visible page, so the numbers hold still as chips
        // are picked.
        private int CountFor(ReportStatus status) => _serviceReports.Count(sr => RowStatus(sr) == status);

        private void NavigateToAdd() => _navigationManager.NavigateTo("/add-service-report");
        private void Edit(int id) => _navigationManager.NavigateTo($"/add-service-report/{id}");
        private void Print(int id) => _navigationManager.NavigateTo($"/ShowPDF/{id}");
        private double CalculateTotal(ServiceReportModel sr) => _serviceReportService.CalculateServiceReportTotal(sr);
    }
}