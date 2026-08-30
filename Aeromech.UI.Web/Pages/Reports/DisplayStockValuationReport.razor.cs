using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Reports
{
    /// <summary>
    /// What the stock on the shelf is worth right now, grouped by the supplier it is bought from.
    /// No date on it: this is the holding as it stands, which is what an insurance figure, a
    /// year-end number or a conversation with a supplier all want.
    /// </summary>
    public partial class DisplayStockValuationReport
    {
        [Inject] private StockReportService StockReportService { get; set; } = default!;
        [Inject] private LoaderService LoaderService { get; set; } = default!;
        [Inject] private ToastService ToastService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        private readonly StockValuationReportRequestModel _request = new();

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

                _pdfBytes = await StockReportService.GenerateStockValuationReport(_request);
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
                ToastService.Notify(new(ToastType.Danger, "The stock valuation could not be generated."));
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

            await DownloadFileFromStream(_pdfBytes, $"StockValuation_{DateTime.Now:yyyyMMdd}.pdf");
        }

        private async Task DownloadFileFromStream(byte[] fileBytes, string fileName)
        {
            var fileStream = new MemoryStream(fileBytes);
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}
