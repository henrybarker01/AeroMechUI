using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Reports
{
    public partial class DisplayReport
    {
        [Parameter]
        public int reportId { get; set; }

        [Inject] ServiceReportService ServiceReportService { get; set; }

        private string pdfBase64String;

        protected override async void OnInitialized()
        {
        
            pdfBase64String = await GetPDF(reportId);
            StateHasChanged();
        }
        private string eventLog { get; set; } = $"Last event: ..., CurrentPage: 0, TotalPages: 0";

        private void OnDocumentLoaded(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnDocumentLoaded, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private void OnPageChanged(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnPageChanged, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        public async Task<string> GetPDF(int ReportId)
        {
            var pdfResult = await ServiceReportService.DownloadServiceReport(ReportId);
            return Convert.ToBase64String(pdfResult);
        }

        public byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}