using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Widgets.Quotes
{
    public partial class QuoteWidget
    {
        [Inject] ServiceReportService ServiceReportService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        List<ServiceReportModel> quotes = new List<ServiceReportModel>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                quotes = await ServiceReportService.GetRecentQuotes(DateTime.Now.AddMonths(-1));
                await InvokeAsync(StateHasChanged);
            }
        }

        private void PrintQuote(int Id)
        {
            NavigationManager.NavigateTo($"/ShowQuote/{Id}");
        }

        private void EditQuote(int serviceReportId)
        {
            NavigationManager.NavigateTo($"/add-service-report/{serviceReportId}");
        }
    }
}