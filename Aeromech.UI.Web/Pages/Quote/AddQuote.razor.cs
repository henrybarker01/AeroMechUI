using AeroMech.Models;
using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AeroMech.UI.Web.Pages.Quote
{
    public partial class AddQuote
    {
        [Inject] ClientService ClientService { get; set; }
        [Inject] PartsService PartsService { get; set; }
        [Inject] VehicleService VehicleService { get; set; }
        [Inject] QuoteService QuoteService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }
        [Inject] protected BlazorBootstrap.ToastService ToastService { get; set; }

        [Parameter] public int quoteId { get; set; }

        private EditContext? editContext;

        private QuoteModel _quote = new();
        List<ClientModel> clients = new();
        List<VehicleModel> vehicles = new();
        List<PartModel> parts = new();

        private RateType selectedRateType = RateType.None;

        private IEnumerable<RateType> RateTypes => Enum.GetValues<RateType>();

        protected override void OnInitialized()
        {
            editContext = new(_quote);

            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            _loaderService.ShowLoader();

            clients = await ClientService.GetClients();
            parts = await PartsService.GetParts();

            if (quoteId == 0)
                InitQuote();
            else
                await LoadQuote(quoteId);

            await InvokeAsync(StateHasChanged);
            _loaderService.HideLoader();
        }

        private void InitQuote()
        {
            _quote.QuoteDate = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

            // Nearly every quote has weekday labour on it, so the row is there from the start.
            // Its rate fills in as soon as a client is picked.
            AddLabour(RateType.Weekdays);
        }

        private async Task LoadQuote(int id)
        {
            _quote = await QuoteService.GetQuote(id);

            if (_quote.ClientId != 0)
            {
                _quote.Client = clients.SingleOrDefault(x => x.Id == _quote.ClientId) ?? new ClientModel();

                // Deliberately not HandleOnChangeClient: a saved quote keeps the rates it was
                // quoted at, even if the client's rates have moved since.
                vehicles = await VehicleService.GetVehicles(_quote.ClientId);
            }

            editContext = new(_quote);

            // _quote is a new instance, so the grids must rebind to it.
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleOnChangeClient(int clientId)
        {
            _quote.ClientId = clientId;
            _quote.Client = clients.SingleOrDefault(x => x.Id == clientId) ?? new ClientModel();
            _quote.VehicleId = 0;
            vehicles = await VehicleService.GetVehicles(clientId);

            // Labour is charged at the client's rates, so every line re-prices against whoever
            // the quote is now for.
            foreach (var labour in _quote.Labour)
                labour.Rate = ClientRateFor(labour.RateType);

            StateHasChanged();
        }

        private void HandleOnChangeVehicle(int vehicleId)
        {
            _quote.VehicleId = vehicleId;
            _quote.Vehicle = vehicles.FirstOrDefault(x => x.Id == vehicleId);
        }

        private void HandleOnServiceTypeChange(ServiceType serviceType) => _quote.ServiceType = serviceType;

        private void AddLabour(RateType rateType)
        {
            selectedRateType = RateType.None;

            if (rateType == RateType.None) return;

            _quote.Labour.Add(new QuoteLabourModel
            {
                RateType = rateType,
                Rate = ClientRateFor(rateType)
            });
        }

        // The rate always comes from the client's rate card, so picking a different rate type
        // simply re-prices the line.
        private void HandleOnChangeRateType(QuoteLabourModel labour, RateType rateType)
        {
            labour.RateType = rateType;
            labour.Rate = ClientRateFor(rateType);
        }

        private double ClientRateFor(RateType rateType)
            => _quote.Client?.Rates?.FirstOrDefault(x => x.RateType == rateType)?.Rate ?? 0;

        // A line the user never saved can simply go; a saved one is flagged so the service knows
        // to strike it off.
        private void RemoveLabour(QuoteLabourModel labour)
        {
            if (labour.Id == 0)
                _quote.Labour.Remove(labour);
            else
                labour.IsDeleted = true;
        }

        private void RemovePart(QuotePartModel part)
        {
            if (part.Id == 0)
                _quote.Parts.Remove(part);
            else
                part.IsDeleted = true;
        }

        private static double LabourTotal(QuoteLabourModel labour)
            => labour.Rate * labour.Hours - labour.Discount / 100 * (labour.Rate * labour.Hours);

        private static double PartTotal(QuotePartModel part)
            => part.CostPrice * part.QTY - part.Discount / 100 * (part.CostPrice * part.QTY);

        private string SearchTerm { get; set; } = string.Empty;

        private IEnumerable<PartModel> FilteredParts =>
            parts.Where(p => string.IsNullOrEmpty(SearchTerm) ||
                             p.PartCode.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                             p.PartDescription.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

        void HandleOnChangePart(int partId)
        {
            SearchTerm = "";

            if (partId == 999999)
            {
                _quote.Parts.Add(new QuotePartModel
                {
                    PartCode = "AdHock",
                    PartDescription = "",
                    IsAdHockPart = true
                });

                return;
            }

            // A part struck off earlier is still on the quote as a deleted line, so putting it
            // back means reviving that line rather than adding a second one for the same part.
            var existing = _quote.Parts.FirstOrDefault(x => !x.IsAdHockPart && x.PartId == partId);
            if (existing != null)
            {
                existing.IsDeleted = false;
                return;
            }

            var prt = parts.FirstOrDefault(x => x.Id == partId);
            if (prt == null) return;

            _quote.Parts.Add(new QuotePartModel
            {
                PartId = prt.Id,
                PartCode = prt.PartCode,
                PartDescription = prt.PartDescription,
                CostPrice = prt.CostPrice,
                SellingPrice = Convert.ToDouble(prt.CostPrice),
                ProductClass = prt.ProductClass,
                Bin = prt.Bin,
                CycleCount = prt.CycleCount,
                QtyOnHand = prt.QtyOnHand,
                Warehouse = prt.Warehouse,
                SupplierCode = prt.SupplierCode,
                IsAdHockPart = false
            });
        }

        private async Task Save()
        {
            var savedId = await SaveQuote();

            if (savedId != 0)
                ToastService.Notify(new(ToastType.Success, $"Quote AEM {_quote.QuoteNumber} saved successfully."));
        }

        private async Task SaveAndPrint()
        {
            var savedId = await SaveQuote();

            if (savedId != 0)
                NavigationManager.NavigateTo($"/ShowQuote/{savedId}");
        }

        private async Task SaveAndNew()
        {
            var savedId = await SaveQuote();

            if (savedId == 0) return;

            ToastService.Notify(new(ToastType.Success, $"Quote AEM {_quote.QuoteNumber} saved successfully."));

            _quote = new QuoteModel();
            editContext = new(_quote);
            InitQuote();

            await InvokeAsync(StateHasChanged);
        }

        private async Task ConvertToServiceReport()
        {
            var savedId = await SaveQuote();

            if (savedId == 0) return;

            // The service report form takes it from here: it opens with the quoted parts already
            // on it and an empty labour grid, so the user has to enter what was actually worked.
            NavigationManager.NavigateTo($"/add-service-report/from-quote/{savedId}");
        }

        private void PrintQuote() => NavigationManager.NavigateTo($"/ShowQuote/{_quote.Id}");

        private void OpenServiceReport() => NavigationManager.NavigateTo($"/add-service-report/{_quote.ServiceReportId}");

        private async Task<int> SaveQuote()
        {
            if (!editContext.Validate())
            {
                ToastService.Notify(new(ToastType.Danger, "Please correct the highlighted fields before saving."));
                return 0;
            }

            _loaderService.ShowLoader();

            try
            {
                var savedId = _quote.Id == 0
                    ? await QuoteService.AddQuote(_quote)
                    : await QuoteService.EditQuote(_quote);

                // Reload so labour and parts carry their database ids. Without them the next save
                // cannot tell saved rows apart from new ones and inserts duplicates.
                await LoadQuote(savedId);

                return savedId;
            }
            catch (Exception ex)
            {
                ToastService.Notify(new(ToastType.Danger, $"Quote could not be saved. {ex.Message}"));
                return 0;
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }
    }
}
