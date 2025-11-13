using AeroMech.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;


namespace AeroMech.UI.Web.Pages.Part
{
    public partial class Part
    {
        [Inject] private PartsService _partsService { get; set; }
        [Inject] private LoaderService _loaderService { get; set; }
        [Inject] private ConfirmationService _confirmationService { get; set; }

        private string _title = "";
        private Modal _modal = default!;      
        private PartModel _part = new PartModel();
        private List<PartModel>? _parts = new List<PartModel>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await GetParts();
                await InvokeAsync(StateHasChanged);
            }
        }

        private bool MatchesSearch(PartModel part, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var t = term.Trim();

            return
                (part.PartCode ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (part.PartDescription ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase);
        }

        private async Task OnAddPartClick()
        {
            _title = "Add Part";
            _part = new PartModel();
            _part.Warehouse = new WarehouseModel()
            {
                Id = 0,
                WarehouseCode = "JH"
            };
            await _modal.ShowAsync();
        }

        private async Task OnEditPartClick(PartModel prt)
        {
            _title = "Edit Part";
            _part = prt;
            await _modal.ShowAsync();
        }

        private async Task OnHideModalClick()
        {
            await GetParts();
            await _modal.HideAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async void AddNewPart()
        {
            _loaderService.ShowLoader();
            var result = await _partsService.AddNewPart(_part);
            if (result != null)
            {
                _part = new PartModel();
                await OnHideModalClick();
            }
            _loaderService.HideLoader();
        }

        private async Task GetParts()
        {
            _loaderService.ShowLoader();
            _parts = await _partsService.GetParts();
            _loaderService.HideLoader();
        }

        private async Task DeletePart(PartModel prt)
        {
            bool confirmed = await _confirmationService.ConfirmAsync("Are you sure?");
            if (confirmed)
            {
                _loaderService.ShowLoader();
                await _partsService.DeletePart(_part);
                _parts?.Remove(prt);
                _loaderService.HideLoader();
            }
        }
    }
}
