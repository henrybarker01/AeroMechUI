using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Quote
{
    public partial class Quotes
    {
        [Inject] private NavigationManager _navigationManager { get; set; }
        [Inject] private QuoteService _quoteService { get; set; }
        [Inject] private LoaderService _loaderService { get; set; }

        private List<QuoteModel> _quotes = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;
            await GetQuotes();
        }

        private async Task GetQuotes()
        {
            _loaderService.ShowLoader();
            _quotes = await _quoteService.GetQuotes();
            _loaderService.HideLoader();
            await InvokeAsync(StateHasChanged);
        }

        private bool MatchesSearch(QuoteModel quote, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var t = term.Trim();
            return (quote.Description ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase)
            || (quote.Vehicle?.SerialNumber ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase)
            || (quote.Client?.Name ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase)
            || (quote.Instruction ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase)
            || quote.QuoteNumber.ToString().Contains(t, StringComparison.OrdinalIgnoreCase)
            || quote.Id.ToString().Contains(t, StringComparison.OrdinalIgnoreCase);
        }

        private void NavigateToAddQuote() => _navigationManager.NavigateTo("/add-quote");
        private void EditQuote(int id) => _navigationManager.NavigateTo($"/add-quote/{id}");
        private void PrintQuote(int id) => _navigationManager.NavigateTo($"/ShowQuote/{id}");
        private void ConvertQuote(int id) => _navigationManager.NavigateTo($"/add-service-report/from-quote/{id}");
        private double CalculateTotal(QuoteModel q) => _quoteService.CalculateQuoteTotal(q);
    }
}
