using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Reports
{
    public partial class DisplayQuote
    {
        [Parameter]
        public int quoteId { get; set; }

        [Inject] QuoteService QuoteService { get; set; }
        [Inject] IJSRuntime JS { get; set; }

        private string pdfBase64String;
        private byte[] pdfBytes;

        protected override async void OnInitialized()
        {
            pdfBase64String = await GetPDF(quoteId);
            StateHasChanged();
        }
        private string eventLog { get; set; } = $"Last event: ..., CurrentPage: 0, TotalPages: 0";

        private void OnDocumentLoaded(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnDocumentLoaded, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private void OnPageChanged(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnPageChanged, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        public async Task<string> GetPDF(int QuoteId)
        {
            pdfBytes = await QuoteService.DownloadQuote(QuoteId);
            return Convert.ToBase64String(pdfBytes);
        }

        private async Task DownloadFileFromStream()
        {
            var fileStream = new MemoryStream(pdfBytes);
            var fileName = $"Quote-{quoteId}.pdf";
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}