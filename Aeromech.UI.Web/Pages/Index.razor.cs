using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages
{
    public partial class Index
    {
        [Inject] private QuoteService QuoteService { get; set; } = default!;
        [Inject] private ServiceReportService ServiceReportService { get; set; } = default!;

        private int _openQuotes;
        private int _convertedQuotes;
        private int _openReports;
        private int _completedReports;

        // The same one month window the list widgets below use, so the counts and the
        // rows under them always describe the same period.
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            var since = DateTimeOffset.UtcNow.AddMonths(-1);

            var quotes = await QuoteService.GetQuotes(since);
            var reports = await ServiceReportService.GetRecentServiceReports(since);

            _openQuotes = quotes.Count(q => !q.IsConverted);
            _convertedQuotes = quotes.Count(q => q.IsConverted);
            _openReports = reports.Count(r => !r.IsComplete);
            _completedReports = reports.Count(r => r.IsComplete);

            await InvokeAsync(StateHasChanged);
        }
    }
}
