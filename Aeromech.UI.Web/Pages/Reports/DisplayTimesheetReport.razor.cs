using AeroMech.Models.Models;
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

        [Parameter] public string? ReportDate { get; set; }

        private ReportType _reportType = ReportType.Weekly;
        private OutputFormat _outputFormat = OutputFormat.Pdf;
        private DateOnly _weekStart = DateOnly.FromDateTime(DateTime.UtcNow);
        private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        private DateOnly _fromDate = DateOnly.FromDateTime(DateTime.UtcNow);
        private DateOnly _toDate = DateOnly.FromDateTime(DateTime.UtcNow);
        private string? _pdfBase64String;
        private byte[]? _pdfBytes;
        private int _reportVersion;

        private List<ClientOptionModel> _clients = new();
        private readonly HashSet<int> _selectedClientIds = new();

        private string eventLog { get; set; } = $"Last event: ..., CurrentPage: 0, TotalPages: 0";

        private string SelectedClientsLabel => _selectedClientIds.Count switch
        {
            0 => "All clients",
            1 => _clients.FirstOrDefault(c => c.Id == _selectedClientIds.First())?.Name ?? "1 client selected",
            _ => $"{_selectedClientIds.Count} clients selected"
        };

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(ReportDate) && DateOnly.TryParse(ReportDate, out var parsedDate))
            {
                _reportType = ReportType.Daily;
                _selectedDate = parsedDate;
            }

            _clients = await TimesheetService.GetTimesheetReportClientsAsync();

            await LoadReportAsync();
        }

        private void ToggleClient(int clientId, bool isSelected)
        {
            if (isSelected)
                _selectedClientIds.Add(clientId);
            else
                _selectedClientIds.Remove(clientId);
        }

        private async Task ClearClientSelection()
        {
            if (_selectedClientIds.Count == 0)
                return;

            _selectedClientIds.Clear();
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
            var clientIds = _selectedClientIds.ToList();

            switch (_reportType)
            {
                case ReportType.Weekly:
                    _weekStart = GetWeekStart(_weekStart);
                    _pdfBytes = await TimesheetService.DownloadTimesheetReportAsync(_weekStart, clientIds);
                    break;
                case ReportType.Daily:
                    _pdfBytes = await TimesheetService.DownloadDailyTimesheetReportAsync(_selectedDate, clientIds);
                    break;
                case ReportType.DateRange:
                    _pdfBytes = await TimesheetService.DownloadDateRangeTimesheetReportAsync(_fromDate, _toDate, clientIds);
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
            var clientIds = _selectedClientIds.ToList();

            switch (_reportType)
            {
                case ReportType.Weekly:
                    _weekStart = GetWeekStart(_weekStart);
                    excelBytes = await TimesheetService.ExportWeeklyTimesheetToExcelAsync(_weekStart, clientIds);
                    break;
                case ReportType.Daily:
                    excelBytes = await TimesheetService.ExportDailyTimesheetToExcelAsync(_selectedDate, clientIds);
                    break;
                case ReportType.DateRange:
                    excelBytes = await TimesheetService.ExportDateRangeTimesheetToExcelAsync(_fromDate, _toDate, clientIds);
                    break;
                default:
                    return;
            }

            await DownloadFileFromStream(excelBytes, GetReportFileName("xlsx"));
        }

        private async Task DownloadPdf()
        {
            if (_pdfBytes is null)
                return;

            await DownloadFileFromStream(_pdfBytes, GetReportFileName("pdf"));
        }

        private string GetReportFileName(string extension) => _reportType switch
        {
            ReportType.Weekly => $"TimesheetReport_Week_{_weekStart:yyyyMMdd}.{extension}",
            ReportType.Daily => $"TimesheetReport_Daily_{_selectedDate:yyyyMMdd}.{extension}",
            ReportType.DateRange => $"TimesheetReport_{_fromDate:yyyyMMdd}_to_{_toDate:yyyyMMdd}.{extension}",
            _ => $"TimesheetReport.{extension}"
        };

        private async Task DownloadFileFromStream(byte[] fileBytes, string fileName)
        {
            var fileStream = new MemoryStream(fileBytes);
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}
