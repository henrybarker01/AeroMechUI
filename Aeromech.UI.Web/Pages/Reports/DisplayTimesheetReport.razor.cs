using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Reports
{
    public enum ReportType
    {
        Weekly,
        Daily,
        DateRange
    }

    public enum OutputFormat
    {
        Pdf,
        Excel
    }

    public partial class DisplayTimesheetReport
    {
        [Inject] TimesheetService TimesheetService { get; set; } = default!;
        [Inject] IJSRuntime JS { get; set; } = default!;

        private ReportType _reportType = ReportType.Weekly;
        private OutputFormat _outputFormat = OutputFormat.Pdf;
        private DateOnly _weekStart = DateOnly.FromDateTime(DateTime.UtcNow);
        private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        private DateOnly _fromDate = DateOnly.FromDateTime(DateTime.UtcNow);
        private DateOnly _toDate = DateOnly.FromDateTime(DateTime.UtcNow);
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

        private async Task OnSelectedDateChanged(DateOnly value)
        {
            _selectedDate = value;
            await LoadReportAsync();
        }

        private async Task OnFromDateChanged(DateOnly value)
        {
            _fromDate = value;
            await LoadReportAsync();
        }

        private async Task OnToDateChanged(DateOnly value)
        {
            _toDate = value;
            await LoadReportAsync();
        }

        private async Task LoadReportAsync()
        {
            switch (_reportType)
            {
                case ReportType.Weekly:
                    _weekStart = GetWeekStart(_weekStart);
                    _pdfBytes = await TimesheetService.DownloadTimesheetReportAsync(_weekStart);
                    break;
                case ReportType.Daily:
                    _pdfBytes = await TimesheetService.DownloadDailyTimesheetReportAsync(_selectedDate);
                    break;
                case ReportType.DateRange:
                    _pdfBytes = await TimesheetService.DownloadDateRangeTimesheetReportAsync(_fromDate, _toDate);
                    break;
            }

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

        private async Task ViewReport()
        {
            await LoadReportAsync();
        }

        private async Task ExportToExcel()
        {
            byte[] excelBytes;

            switch (_reportType)
            {
                case ReportType.Weekly:
                    _weekStart = GetWeekStart(_weekStart);
                    excelBytes = await TimesheetService.ExportWeeklyTimesheetToExcelAsync(_weekStart);
                    break;
                case ReportType.Daily:
                    excelBytes = await TimesheetService.ExportDailyTimesheetToExcelAsync(_selectedDate);
                    break;
                case ReportType.DateRange:
                    excelBytes = await TimesheetService.ExportDateRangeTimesheetToExcelAsync(_fromDate, _toDate);
                    break;
                default:
                    return;
            }

            var fileStream = new MemoryStream(excelBytes);
            var fileName = _reportType switch
            {
                ReportType.Weekly => $"TimesheetReport_Week_{_weekStart:yyyyMMdd}.xlsx",
                ReportType.Daily => $"TimesheetReport_Daily_{_selectedDate:yyyyMMdd}.xlsx",
                ReportType.DateRange => $"TimesheetReport_{_fromDate:yyyyMMdd}_to_{_toDate:yyyyMMdd}.xlsx",
                _ => "TimesheetReport.xlsx"
            };

            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }

        private async Task DownloadFileFromStream()
        {
            if (_pdfBytes is null || _pdfBytes.Length == 0)
                return;

            var fileStream = new MemoryStream(_pdfBytes);
            var fileName = _reportType switch
            {
                ReportType.Weekly => $"TimesheetReport_Week_{_weekStart:yyyyMMdd}.pdf",
                ReportType.Daily => $"TimesheetReport_Daily_{_selectedDate:yyyyMMdd}.pdf",
                ReportType.DateRange => $"TimesheetReport_{_fromDate:yyyyMMdd}_to_{_toDate:yyyyMMdd}.pdf",
                _ => "TimesheetReport.pdf"
            };

            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}
