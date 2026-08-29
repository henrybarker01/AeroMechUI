using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.StockTake
{
    public partial class StockTakes
    {
        [Inject] private StockTakeService _stockTakeService { get; set; } = default!;
        [Inject] private LoaderService _loaderService { get; set; } = default!;
        [Inject] private ConfirmationService _confirmationService { get; set; } = default!;
        [Inject] private NavigationManager _navigationManager { get; set; } = default!;
        [Inject] private IJSRuntime JS { get; set; } = default!;
        [Inject] protected BlazorBootstrap.ToastService ToastService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;

        private Modal _newModal = default!;

        private List<StockTakeModel> _stockTakes = new();
        private List<SupplierOptionModel> _suppliers = new();
        private StockTakeRequestModel _request = new();

        private readonly HashSet<string> _selectedSuppliers = new(StringComparer.OrdinalIgnoreCase);

        private string _currentUser = string.Empty;

        private string SelectedSuppliersLabel => _selectedSuppliers.Count switch
        {
            0 => "All suppliers",
            1 => _selectedSuppliers.First(),
            _ => $"{_selectedSuppliers.Count} suppliers selected"
        };

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            _currentUser = state.User.Identity?.Name ?? string.Empty;

            await LoadStockTakes();
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadStockTakes()
        {
            _loaderService.ShowLoader();
            try
            {
                _stockTakes = await _stockTakeService.GetStockTakes();
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        private bool MatchesSearch(StockTakeModel take, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var t = term.Trim();

            return
                take.Reference.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (take.StockTakeDescription ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (take.Remarks ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (take.StockTakeBy ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                take.StatusLabel.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                take.SupplierCodes.Any(s => s.Contains(t, StringComparison.OrdinalIgnoreCase));
        }

        private async Task OpenNewModal()
        {
            _request = new StockTakeRequestModel { StockTakeBy = _currentUser };
            _selectedSuppliers.Clear();

            // Loaded when the sheet is being raised rather than with the list: the list itself has
            // no use for the supplier codes.
            if (_suppliers.Count == 0)
                _suppliers = await _stockTakeService.GetSuppliers();

            await _newModal.ShowAsync();
        }

        private async Task CloseNewModal() => await _newModal.HideAsync();

        private void ToggleSupplier(string supplierCode, bool isSelected)
        {
            if (isSelected)
                _selectedSuppliers.Add(supplierCode);
            else
                _selectedSuppliers.Remove(supplierCode);
        }

        private void ClearSupplierSelection() => _selectedSuppliers.Clear();

        private async Task CreateStockTake()
        {
            _request.SupplierCodes = _selectedSuppliers.ToList();
            _request.StockTakeBy = _currentUser;

            _loaderService.ShowLoader();
            try
            {
                var id = await _stockTakeService.CreateStockTake(_request);

                await _newModal.HideAsync();

                // Straight onto the sheet: raising one is only ever a step towards counting it.
                _navigationManager.NavigateTo($"stock-take/{id}");
            }
            catch (InvalidOperationException ex)
            {
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
            catch (Exception)
            {
                ToastService.Notify(new(ToastType.Danger, "The stock take could not be created."));
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        private void OpenStockTake(StockTakeModel take) => _navigationManager.NavigateTo($"stock-take/{take.Id}");

        private async Task DownloadCountSheet(StockTakeModel take)
        {
            _loaderService.ShowLoader();
            try
            {
                var pdf = await _stockTakeService.GenerateCountSheet(take.Id);
                await DownloadFileFromStream(pdf, $"CountSheet_{take.Reference}.pdf");
            }
            catch (Exception)
            {
                ToastService.Notify(new(ToastType.Danger, "The count sheet could not be generated."));
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        private async Task CancelStockTake(StockTakeModel take)
        {
            var confirmed = await _confirmationService.ConfirmAsync(
                $"Cancel {take.Reference}? No stock will be changed, and the counts already captured are kept.");

            if (!confirmed) return;

            try
            {
                await _stockTakeService.CancelStockTake(take.Id, _currentUser);
                ToastService.Notify(new(ToastType.Success, $"{take.Reference} cancelled."));
                await LoadStockTakes();
            }
            catch (InvalidOperationException ex)
            {
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
        }

        private async Task DownloadFileFromStream(byte[] fileBytes, string fileName)
        {
            var fileStream = new MemoryStream(fileBytes);
            using var streamRef = new DotNetStreamReference(stream: fileStream);
            await JS.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
        }
    }
}
