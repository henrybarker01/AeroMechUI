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

        /// <summary>
        /// Set when the print was asked for from a service report instead of a quote: the same
        /// quote document, composed from the work captured on that report.
        /// </summary>
        [Parameter]
        public int serviceReportId { get; set; }

        [Inject] QuoteService QuoteService { get; set; }
        [Inject] ServiceReportService ServiceReportService { get; set; }
        [Inject] IJSRuntime JS { get; set; }

        private string pdfBase64String;
        private byte[] pdfBytes;

        protected override async void OnInitialized()
        {
            pdfBase64String = await GetPDF();
            StateHasChanged();
        }
        private string eventLog { get; set; } = $"Last event: ..., CurrentPage: 0, TotalPages: 0";

        private void OnDocumentLoaded(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnDocumentLoaded, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private void OnPageChanged(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnPageChanged, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        /// <summary>
        /// The quote print is reached two ways: from a quote, and from a service report the client
        /// wants to sign off as a quote. Both render the same document.
        /// </summary>
        public async Task<string> GetPDF()
        {
            pdfBytes = serviceReportId != 0
                ? QuoteService.DownloadQuoteForServiceReport(await ServiceReportService.GetServiceReport(serviceReportId))
                : await QuoteService.DownloadQuote(quoteId);

            return Convert.ToBase64String(pdfBytes);
        }

        private async Task DownloadFileFromStream()
        {
            var fileStream = new MemoryStream(pdfBytes);
            var fileName = serviceReportId != 0 ? $"Quote-FSR-{serviceReportId}.pdf" : $"Quote-{quoteId}.pdf";
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}