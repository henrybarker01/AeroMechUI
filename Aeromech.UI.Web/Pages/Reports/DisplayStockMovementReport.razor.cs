using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Reports
{
    /// <summary>
    /// The stock ledger as a report: what a part opened a period at, what moved it, and what it
    /// closed at. The period can end in the past, because the ledger records every movement and
    /// the level on a past date is today's level with the movements since then unwound.
    /// </summary>
    public partial class DisplayStockMovementReport
    {
        [Inject] private StockReportService StockReportService { get; set; } = default!;
        [Inject] private LoaderService LoaderService { get; set; } = default!;
        [Inject] private ToastService ToastService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private readonly StockMovementReportRequestModel _request = new();

        private List<SupplierOptionModel> _suppliers = new();
        private readonly HashSet<string> _selectedSuppliers = new(StringComparer.OrdinalIgnoreCase);

        private string? _pdfBase64String;
        private byte[]? _pdfBytes;
        private int _reportVersion;

        private string eventLog { get; set; } = $"Last event: ..., CurrentPage: 0, TotalPages: 0";

        private string SelectedSuppliersLabel => _selectedSuppliers.Count switch
        {
            0 => "All suppliers",
            1 => _selectedSuppliers.First(),
            _ => $"{_selectedSuppliers.Count} suppliers selected"
        };

        protected override async Task OnInitializedAsync()
        {
            _suppliers = await StockReportService.GetSuppliers();
        }

        /// <summary>
        /// The periods actually asked for, rather than making somebody pick two dates to answer
        /// "what happened last month". Clearing the report with them, because the dates on screen
        /// would otherwise describe a document that was built for a different period.
        /// </summary>
        private void SetPeriod(int period)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            switch (period)
            {
                case 0:
                    _request.FromDate = new DateOnly(today.Year, today.Month, 1);
                    _request.ToDate = today;
                    break;

                case 1:
                    var lastMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
                    _request.FromDate = lastMonth;
                    _request.ToDate = lastMonth.AddMonths(1).AddDays(-1);
                    break;

                case 2:
                    _request.FromDate = today.AddMonths(-3);
                    _request.ToDate = today;
                    break;

                default:
                    _request.FromDate = new DateOnly(today.Year, 1, 1);
                    _request.ToDate = today;
                    break;
            }

            ClearReport();
        }

        private void OnFromDateChanged(DateOnly value)
        {
            _request.FromDate = value;
            ClearReport();
        }

        private void OnToDateChanged(DateOnly value)
        {
            _request.ToDate = value;
            ClearReport();
        }

        private void ToggleSupplier(string supplierCode, bool isSelected)
        {
            if (isSelected)
                _selectedSuppliers.Add(supplierCode);
            else
                _selectedSuppliers.Remove(supplierCode);
        }

        private void ClearSupplierSelection() => _selectedSuppliers.Clear();

        private void ClearReport()
        {
            _pdfBytes = null;
            _pdfBase64String = null;
        }

        private async Task ViewReport()
        {
            LoaderService.ShowLoader();
            try
            {
                _request.SupplierCodes = _selectedSuppliers.ToList();

                _pdfBytes = await StockReportService.GenerateStockMovementReport(_request);
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
                ToastService.Notify(new(ToastType.Danger, "The stock movement report could not be generated."));
            }
            finally
            {
                LoaderService.HideLoader();
            }

            await InvokeAsync(StateHasChanged);
        }

        private void OnDocumentLoaded(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnDocumentLoaded, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private void OnPageChanged(PdfViewerEventArgs args)
            => eventLog = $"Last event: OnPageChanged, CurrentPage: {args.CurrentPage}, TotalPages: {args.TotalPages}";

        private async Task DownloadPdf()
        {
            if (_pdfBytes is null)
                return;

            var fileName = $"StockMovement_{_request.FromDate:yyyyMMdd}_{_request.ToDate:yyyyMMdd}.pdf";
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
