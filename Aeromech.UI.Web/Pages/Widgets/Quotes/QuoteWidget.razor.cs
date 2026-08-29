using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Widgets.Quotes
{
    public partial class QuoteWidget
    {
        [Inject] QuoteService QuoteService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }

        List<QuoteModel> quotes = new List<QuoteModel>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                quotes = await QuoteService.GetQuotes(DateTimeOffset.UtcNow.AddMonths(-1));
                await InvokeAsync(StateHasChanged);
            }
        }

        private void PrintQuote(int Id)
        {
            NavigationManager.NavigateTo($"/ShowQuote/{Id}");
        }

        private void EditQuote(int quoteId)
        {
            NavigationManager.NavigateTo($"/add-quote/{quoteId}");
        }
    }
}
