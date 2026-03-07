using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Reports
{
    public partial class DisplayTimesheetReport
    {
        [Inject] TimesheetService TimesheetService { get; set; } = default!;
        [Inject] IJSRuntime JS { get; set; } = default!;

        private DateOnly _weekStart = DateOnly.FromDateTime(DateTime.UtcNow);
        private string? _pdfBase64String;
        private byte[]? _pdfBytes;
        private int _reportVersion;

        private string eventLog { get; set; } = $"Last event: ..., CurrentPage: 0, TotalPages: 0";

        protected override async Task OnInitializedAsync()
        {
            await LoadReportAsync();
        }

        private async Task OnWeekStartChanged(DateOnly value)
        {
            _weekStart = value;
            await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            _weekStart = GetWeekStart(_weekStart);
            _pdfBytes = await TimesheetService.DownloadTimesheetReportAsync(_weekStart);
            _pdfBase64String = Convert.ToBase64String(_pdfBytes);
            _reportVersion++;
            await InvokeAsync(StateHasChanged);
        }

        private static DateOnly GetWeekStart(DateOnly date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            var monday = (int)DayOfWeek.Monday;
            var delta = (7 + (dayOfWeek - monday)) % 7;
            return date.AddDays(-delta);
        }

        private void OnDocumentLoaded(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnDocumentLoaded, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private void OnPageChanged(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnPageChanged, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private async Task DownloadFileFromStream()
        {
            if (_pdfBytes is null || _pdfBytes.Length == 0)
                return;

            var fileStream = new MemoryStream(_pdfBytes);
            var fileName = $"TimesheetReport_{_weekStart:yyyyMMdd}.pdf";

            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}
