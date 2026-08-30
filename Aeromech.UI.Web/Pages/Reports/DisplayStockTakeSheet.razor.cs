using AeroMech.Data.Enums;
using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Reports
{
    /// <summary>
    /// Which sheet is being printed: one drawn up on the spot, or the one belonging to a stock take
    /// that has already been raised.
    /// </summary>
    public enum SheetMode
    {
        Blank,
        StockTake
    }

    /// <summary>
    /// The count sheet as a report rather than as a row action. A blank sheet can be printed
    /// against any slice of the parts list without raising anything, which is what a spot check
    /// wants; a raised stock take can still be reprinted here, which is what a lost sheet wants.
    /// </summary>
    public partial class DisplayStockTakeSheet
    {
        [Inject] private StockTakeService StockTakeService { get; set; } = default!;
        [Inject] private LoaderService LoaderService { get; set; } = default!;
        [Inject] private ToastService ToastService { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;

        [Parameter] public int? StockTakeId { get; set; }

        private SheetMode _mode = SheetMode.Blank;
        private StockTakeSheetOrder _order = StockTakeSheetOrder.SupplierThenPart;

        private readonly StockTakeRequestModel _request = new();
        private List<SupplierOptionModel> _suppliers = new();
        private readonly HashSet<string> _selectedSuppliers = new(StringComparer.OrdinalIgnoreCase);

        private List<StockTakeModel> _stockTakes = new();
        private int? _selectedId;

        private string? _pdfBase64String;
        private byte[]? _pdfBytes;
        private int _reportVersion;

        private string eventLog { get; set; } = $"Last event: ..., CurrentPage: 0, TotalPages: 0";

        private bool CanGenerate => _mode == SheetMode.Blank || _selectedId is not null;

        private string SelectedSuppliersLabel => _selectedSuppliers.Count switch
        {
            0 => "All suppliers",
            1 => _selectedSuppliers.First(),
            _ => $"{_selectedSuppliers.Count} suppliers selected"
        };

        protected override async Task OnInitializedAsync()
        {
            _suppliers = await StockTakeService.GetSuppliers();
            _stockTakes = await StockTakeService.GetStockTakes();

            // Newest first out of the service, so the head of the list is the sheet most likely to
            // be wanted when a stock take is asked for without one being named.
            _selectedId = StockTakeId is int requested && _stockTakes.Any(x => x.Id == requested)
                ? requested
                : _stockTakes.FirstOrDefault()?.Id;

            // Arriving on a named stock take means that sheet is what was asked for, so it is shown
            // straight away. Otherwise the page opens blank: the whole parts list is a big document
            // to build for somebody who has not said yet what they want counted.
            if (StockTakeId is not null && _selectedId is not null)
            {
                _mode = SheetMode.StockTake;
                await LoadReportAsync();
            }
        }

        private async Task SetMode(SheetMode mode)
        {
            if (_mode == mode)
                return;

            _mode = mode;

            // The sheet on screen belongs to the mode that was just left.
            ClearReport();

            if (_mode == SheetMode.StockTake && _selectedId is not null)
                await LoadReportAsync();
        }

        private static string DescribeTake(StockTakeModel take)
        {
            var description = string.IsNullOrWhiteSpace(take.StockTakeDescription)
                ? string.Empty
                : $" - {take.StockTakeDescription}";

            return $"{take.Reference} ({take.StockTakeDate:yyyy-MM-dd}){description} [{take.StatusLabel}]";
        }

        private void ToggleSupplier(string supplierCode, bool isSelected)
        {
            if (isSelected)
                _selectedSuppliers.Add(supplierCode);
            else
                _selectedSuppliers.Remove(supplierCode);
        }

        private void ClearSupplierSelection() => _selectedSuppliers.Clear();

        private void OnDateChanged(ChangeEventArgs args)
        {
            if (DateTime.TryParse(args.Value?.ToString(), out var date))
                _request.StockTakeDate = new DateTimeOffset(date.Date, TimeSpan.Zero);
        }

        private async Task OnStockTakeChanged(ChangeEventArgs args)
        {
            _selectedId = int.TryParse(args.Value?.ToString(), out var id) ? id : null;
            await LoadReportAsync();
        }

        private async Task OnOrderChanged(ChangeEventArgs args)
        {
            if (int.TryParse(args.Value?.ToString(), out var order))
                _order = (StockTakeSheetOrder)order;

            // A blank sheet is only reordered once there is one on screen; asking for bin order is
            // not the same as asking for the whole parts list.
            if (_mode == SheetMode.StockTake || _pdfBytes is not null)
                await LoadReportAsync();
        }

        private void ClearReport()
        {
            _pdfBytes = null;
            _pdfBase64String = null;
        }

        private async Task LoadReportAsync()
        {
            if (_mode == SheetMode.StockTake && _selectedId is null)
            {
                ClearReport();
                return;
            }

            LoaderService.ShowLoader();
            try
            {
                if (_mode == SheetMode.Blank)
                {
                    _request.SupplierCodes = _selectedSuppliers.ToList();
                    _pdfBytes = await StockTakeService.GenerateBlankCountSheet(_request, _order);
                }
                else
                {
                    _pdfBytes = await StockTakeService.GenerateCountSheet(_selectedId!.Value, _order);
                }

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
                ToastService.Notify(new(ToastType.Danger, "The count sheet could not be generated."));
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
            if (_pdfBytes is null)
                return;

            await DownloadFileFromStream(_pdfBytes, BuildFileName());
        }

        private string BuildFileName()
        {
            if (_mode == SheetMode.Blank)
                return $"CountSheet_Blank_{_request.StockTakeDate:yyyyMMdd}.pdf";

            var reference = _stockTakes.FirstOrDefault(x => x.Id == _selectedId)?.Reference ?? "StockTake";
            return $"CountSheet_{reference}.pdf";
        }

        private async Task DownloadFileFromStream(byte[] fileBytes, string fileName)
        {
            var fileStream = new MemoryStream(fileBytes);
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}
