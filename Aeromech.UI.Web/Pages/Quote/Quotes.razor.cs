using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Quote
{
    public partial class Quotes
    {
        [Inject] private NavigationManager _navigationManager { get; set; }
        [Inject] private ServiceReportService _serviceReportService { get; set; }
        [Inject] private LoaderService _loaderService { get; set; }

        private List<ServiceReportModel> _quotes = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;
            await GetQuotes();
        }

        private async Task GetQuotes()
        {
            _loaderService.ShowLoader();
            _quotes = await _serviceReportService.GetRecentQuotes();
            _loaderService.HideLoader();
            await InvokeAsync(StateHasChanged);
        }

        private bool MatchesSearch(ServiceReportModel quote, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var t = term.Trim();
            return (quote.Description ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase)
            || (quote.DetailedServiceReport ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase)
            || (quote.Instruction ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase)
            || quote.QuoteNumber.ToString().Contains(t, StringComparison.OrdinalIgnoreCase)
            || (quote.SalesOrderNumber ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase)
            || (quote.JobNumber ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase)
            || quote.Id.ToString().Contains(t, StringComparison.OrdinalIgnoreCase);
        }

        private void NavigateToAddQuote() => _navigationManager.NavigateTo("/add-service-report");
        private void EditQuote(int id) => _navigationManager.NavigateTo($"/add-service-report/{id}");
        private void PrintQuote(int id) => _navigationManager.NavigateTo($"/ShowQuote/{id}");
        private double CalculateTotal(ServiceReportModel q) => _serviceReportService.CalculateServiceReportTotal(q);
    }
}