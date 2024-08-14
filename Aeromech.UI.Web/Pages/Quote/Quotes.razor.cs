using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Quote
{
    public partial class Quotes
    {
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] ServiceReportService ServiceReportService { get; set; }

        private List<ServiceReportModel>? quotes;

        protected override async Task OnInitializedAsync()
        {
            await GetQuotes();
        }

        private async Task GetQuotes()
        {
            var fromDate = DateTime.Now.AddMonths(-2);
            quotes = await ServiceReportService.GetRecentQuotes(fromDate);
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