using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Widgets.Quotes
{
    public partial class QuoteWidget
    {
        [Inject] ServiceReportService ServiceReportService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        List<ServiceReportModel> quotes { get; set; }

        protected override async Task OnInitializedAsync()
        {
            quotes = await ServiceReportService.GetRecentQuotes(DateTime.Now.AddMonths(-1));
        }

        private void LoadQuote(int Id)
        {
            NavigationManager.NavigateTo($"/ShowQuote/{Id}");
        }
    }
}