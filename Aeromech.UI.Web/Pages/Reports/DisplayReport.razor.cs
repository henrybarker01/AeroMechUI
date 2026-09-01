using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Reports
{
    /// <summary>
    /// The field service report for a single job.
    ///
    /// Arriving with a report named - from the service report list, or straight after saving one -
    /// prints it immediately, which is the path this page was built for. Arriving without one, as
    /// the reports view does, offers the list to pick from instead: the report is the same, but
    /// nobody coming from Reports has a report id in their hand.
    /// </summary>
    public partial class DisplayReport
    {
        [Parameter]
        public int? reportId { get; set; }

        [Inject] private ServiceReportService ServiceReportService { get; set; } = default!;
        [Inject] private LoaderService LoaderService { get; set; } = default!;
        [Inject] private ToastService ToastService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private List<ServiceReportOptionModel> _serviceReports = new();
        private int? _selectedId;

        private string? _pdfBase64String;
        private byte[]? _pdfBytes;
        private int _reportVersion;

        private string eventLog { get; set; } = $"Last event: ..., CurrentPage: 0, TotalPages: 0";

        protected override async Task OnInitializedAsync()
        {
            _serviceReports = await ServiceReportService.GetServiceReportOptions();

            // Newest first out of the service, so the head of the list is the report most likely
            // to be wanted when the page is opened without one being named.
            _selectedId = reportId is int requested && _serviceReports.Any(x => x.Id == requested)
                ? requested
                : _serviceReports.FirstOrDefault()?.Id;

            // A named report is what was asked for, so it is drawn straight away. Opening the page
            // cold only offers the list: printing whichever report happens to be newest is not
            // what anybody came for.
            if (reportId is not null && _selectedId is not null)
                await LoadReportAsync();
        }

        private async Task OnServiceReportChanged(ChangeEventArgs args)
        {
            _selectedId = int.TryParse(args.Value?.ToString(), out var id) ? id : null;
            await LoadReportAsync();
        }

        private void ClearReport()
        {
            _pdfBytes = null;
            _pdfBase64String = null;
        }

        private async Task LoadReportAsync()
        {
            if (_selectedId is null)
            {
                ClearReport();
                return;
            }

            LoaderService.ShowLoader();
            try
            {
                _pdfBytes = await ServiceReportService.DownloadServiceReport(_selectedId.Value);
                _pdfBase64String = Convert.ToBase64String(_pdfBytes);
                _reportVersion++;
            }
            catch (InvalidOperationException ex)
            {
                ClearReport();
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
            catch (Exception)
            {
                ClearReport();
                ToastService.Notify(new(ToastType.Danger, "The service report could not be generated."));
            }
            finally
            {
                LoaderService.HideLoader();
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task ViewReport() => await LoadReportAsync();

        private void OnDocumentLoaded(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnDocumentLoaded, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private void OnPageChanged(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnPageChanged, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private async Task DownloadPdf()
        {
            if (_pdfBytes is null || _selectedId is null)
                return;

            var number = _serviceReports.FirstOrDefault(x => x.Id == _selectedId)?.ServiceReportNumber;
            var fileName = number is null ? $"{_selectedId}.pdf" : $"AEM{number}.pdf";

            await DownloadFileFromStream(_pdfBytes, fileName);
        }

        private async Task DownloadFileFromStream(byte[] fileBytes, string fileName)
        {
            var fileStream = new MemoryStream(fileBytes);
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}
