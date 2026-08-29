using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.StockReceiving
{
    public partial class StockReceipts
    {
        [Inject] private StockReceivingService _stockReceivingService { get; set; } = default!;
        [Inject] private LoaderService _loaderService { get; set; } = default!;
        [Inject] private NavigationManager _navigationManager { get; set; } = default!;

        private List<StockReceiptModel> _receipts = new();

        /// <summary>
        /// Lines are fetched only for the receipts actually opened, and kept once fetched: a posted
        /// receipt never changes, so there is nothing to re-read.
        /// </summary>
        private readonly Dictionary<int, List<StockReceivingLineModel>> _receiptLines = new();

        private readonly HashSet<int> _expanded = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            await LoadReceipts();
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadReceipts()
        {
            _loaderService.ShowLoader();
            try
            {
                _receipts = await _stockReceivingService.GetReceipts();
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        private bool MatchesSearch(StockReceiptModel receipt, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var t = term.Trim();

            return
                (receipt.InvoiceNumber ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (receipt.SupplierCode ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (receipt.ReceivedBy ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (receipt.Notes ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsExpanded(StockReceiptModel receipt) => _expanded.Contains(receipt.Id);

        private async Task ToggleReceipt(StockReceiptModel receipt)
        {
            if (!_expanded.Add(receipt.Id))
            {
                _expanded.Remove(receipt.Id);
                return;
            }

            if (_receiptLines.ContainsKey(receipt.Id)) return;

            var lines = await _stockReceivingService.GetReceiptLines(receipt.Id);
            _receiptLines[receipt.Id] = lines;
        }

        private void NavigateToReceiveStock() => _navigationManager.NavigateTo("receive-stock");
    }
}
