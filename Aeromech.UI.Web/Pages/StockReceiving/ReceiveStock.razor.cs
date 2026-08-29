using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Globalization;

namespace AeroMech.UI.Web.Pages.StockReceiving
{
    public partial class ReceiveStock
    {
        [Inject] private StockReceivingService _stockReceivingService { get; set; } = default!;
        [Inject] private LoaderService _loaderService { get; set; } = default!;
        [Inject] private ConfirmationService _confirmationService { get; set; } = default!;
        [Inject] protected BlazorBootstrap.ToastService ToastService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider _authenticationStateProvider { get; set; } = default!;

        private StockReceiptModel _receipt = new();
        private List<SupplierOptionModel> _suppliers = new();

        /// <summary>
        /// Set once so the posted receipt records who took the stock in.
        /// </summary>
        private string _receivedBy = string.Empty;

        /// <summary>
        /// Narrows the grid to the rows that actually carry a quantity, which is how a long
        /// supplier list is checked over before posting.
        /// </summary>
        private bool _onlyInvoiceLines;

        /// <summary>
        /// The rows the narrowed grid shows, fixed at the moment it was narrowed. Filtering live
        /// would drop a row the instant its quantity was cleared - so correcting a figure by
        /// typing over it would pull the row out from under the cursor mid-edit.
        /// </summary>
        private readonly HashSet<int> _narrowedToPartIds = new();

        private IEnumerable<StockReceivingLineModel> VisibleLines
            => _onlyInvoiceLines
                ? _receipt.Lines.Where(x => x.IsOnInvoice || _narrowedToPartIds.Contains(x.PartId))
                : _receipt.Lines;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            var state = await _authenticationStateProvider.GetAuthenticationStateAsync();
            _receivedBy = state.User.Identity?.Name ?? string.Empty;

            await LoadSuppliers();
            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadSuppliers()
        {
            _loaderService.ShowLoader();
            try
            {
                _suppliers = await _stockReceivingService.GetSuppliers();
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        private bool MatchesSearch(StockReceivingLineModel line, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var t = term.Trim();

            return
                (line.PartCode ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (line.PartDescription ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (line.Bin ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Changing supplier replaces the grid, and with it anything already captured, since the
        /// lines belong to the invoice of the supplier that was selected.
        /// </summary>
        private async Task OnSupplierChanged(string supplierCode)
        {
            if (string.Equals(_receipt.SupplierCode, supplierCode, StringComparison.Ordinal))
                return;

            if (_receipt.LineCount > 0)
            {
                var discard = await _confirmationService.ConfirmAsync(
                    "Changing supplier will clear the quantities you have captured. Continue?");

                if (!discard) return;
            }

            _receipt.SupplierCode = supplierCode ?? string.Empty;
            _onlyInvoiceLines = false;
            await LoadLinesForSupplier();
        }

        private async Task LoadLinesForSupplier()
        {
            if (string.IsNullOrWhiteSpace(_receipt.SupplierCode))
            {
                _receipt.Lines = new List<StockReceivingLineModel>();
                return;
            }

            _loaderService.ShowLoader();
            try
            {
                _receipt.Lines = await _stockReceivingService.GetPartsForSupplier(_receipt.SupplierCode);
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        // The grid inputs are parsed by hand rather than bound, so a half-typed or cleared box
        // reads as nothing received instead of raising a binding error mid-capture.
        private void OnQtyInput(StockReceivingLineModel line, string? raw)
        {
            line.QtyReceived = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qty) && qty > 0
                ? qty
                : 0;
        }

        private void OnUnitCostInput(StockReceivingLineModel line, string? raw)
        {
            line.UnitCost = double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var cost) && cost > 0
                ? cost
                : 0;
        }

        private void ToggleOnlyInvoiceLines()
        {
            _onlyInvoiceLines = !_onlyInvoiceLines;

            _narrowedToPartIds.Clear();

            if (_onlyInvoiceLines)
            {
                foreach (var line in _receipt.Lines.Where(x => x.IsOnInvoice))
                    _narrowedToPartIds.Add(line.PartId);
            }
        }

        private async Task OnClearClick()
        {
            if (_receipt.LineCount == 0 && string.IsNullOrWhiteSpace(_receipt.InvoiceNumber))
                return;

            var confirmed = await _confirmationService.ConfirmAsync("Clear this invoice and start again?");
            if (!confirmed) return;

            var supplierCode = _receipt.SupplierCode;
            _receipt = new StockReceiptModel { SupplierCode = supplierCode };
            _onlyInvoiceLines = false;

            await LoadLinesForSupplier();
        }

        private async Task OnPostReceiptClick()
        {
            if (_receipt.LineCount == 0)
            {
                ToastService.Notify(new(ToastType.Danger, "Enter a received quantity against at least one part."));
                return;
            }

            // The same invoice number twice from one supplier is the mistake that quietly doubles
            // stock, so it is worth stopping to ask about even where nothing else is enforced.
            if (await _stockReceivingService.InvoiceAlreadyReceived(_receipt.SupplierCode, _receipt.InvoiceNumber))
            {
                var postAnyway = await _confirmationService.ConfirmAsync(
                    $"Invoice {_receipt.InvoiceNumber} has already been received from {_receipt.SupplierCode}. " +
                    "Posting it again will add the stock a second time. Continue?");

                if (!postAnyway) return;
            }

            if (!_receipt.IsReconciled)
            {
                var variance = _receipt.SubTotalVariance.ToString("C", CultureInfo.CurrentCulture);
                var postAnyway = await _confirmationService.ConfirmAsync(
                    $"The lines you captured differ from the invoice sub total by {variance}. Post anyway?");

                if (!postAnyway) return;
            }

            _loaderService.ShowLoader();
            try
            {
                _receipt.ReceivedBy = _receivedBy;

                var lineCount = _receipt.LineCount;
                var qtyReceived = _receipt.TotalQtyReceived;
                var invoiceNumber = _receipt.InvoiceNumber;

                await _stockReceivingService.PostReceipt(_receipt);

                ToastService.Notify(new(ToastType.Success,
                    $"Invoice {invoiceNumber} received: {qtyReceived} units across {lineCount} parts."));

                // Reload from the database so the grid shows the levels that were actually written,
                // including anything else that moved while the invoice was being captured.
                var supplierCode = _receipt.SupplierCode;
                _receipt = new StockReceiptModel { SupplierCode = supplierCode };
                _onlyInvoiceLines = false;

                await LoadLinesForSupplier();
            }
            catch (InvalidOperationException ex)
            {
                ToastService.Notify(new(ToastType.Danger, ex.Message));
            }
            catch (Exception)
            {
                ToastService.Notify(new(ToastType.Danger, "The receipt could not be posted. No stock was changed."));
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }
    }
}
