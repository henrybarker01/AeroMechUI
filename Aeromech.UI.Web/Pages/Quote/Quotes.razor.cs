using AeroMech.API.Reports;
using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Quote
{
    public partial class Quotes
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ServiceReportService ServiceReportService { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }

        private List<ServiceReportModel>? quotes = new List<ServiceReportModel>();

        private string SearchTerm { get; set; } = string.Empty;
        private IEnumerable<ServiceReportModel> FilteredQuotes =>
        quotes.Where(quote =>
            string.IsNullOrEmpty(SearchTerm) ||
            quote.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            quote.DetailedServiceReport.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            quote.Instruction.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            quote.QuoteNumber.ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            quote.SalesOrderNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            (quote.JobNumber ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            quote.Id.ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
        );

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await GetQuotes();
            }
        }

        private async Task GetQuotes()
        {
            _loaderService.ShowLoader();
            var fromDate = DateTime.Now.AddMonths(-2);
            quotes = await ServiceReportService.GetRecentQuotes(fromDate);
            await InvokeAsync(StateHasChanged);
            _loaderService.HideLoader();
        }

        private void NavigateToAddQuote()
        {
            NavigationManager.NavigateTo($"/add-service-report");
        }

        private void EditQuote(int Id)
        {
            NavigationManager.NavigateTo($"/add-service-report/{Id}");
        }

        private void PrintQuote(int Id)
        {
            NavigationManager.NavigateTo($"/ShowQuote/{Id}");
        }

        private double CalcuateTotalReportValue(ServiceReportModel quote)
        {
            return ServiceReportService.CalculateServiceReportTotal(quote);
        }
    }
}