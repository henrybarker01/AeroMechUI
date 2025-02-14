using AeroMech.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;


namespace AeroMech.UI.Web.Pages.Part
{
    public partial class Part
    {

        [Inject] PartsService partsService { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }
        [Inject] protected ConfirmationService _confirmationService { get; set; }

        private string title = "";

        private Modal modal = default!;
        private Modal rateModal = default!;

        private PartModel part = new PartModel();
        private List<PartModel>? parts = new List<PartModel>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await GetParts();
                await InvokeAsync(StateHasChanged);
            }
        }

        private string SearchTerm { get; set; } = string.Empty;
        private IEnumerable<PartModel> FilteredParts =>
        parts.Where(part =>
            string.IsNullOrEmpty(SearchTerm) ||
            part.PartCode.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            part.PartDescription.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
        );

        private async Task OnAddPartClick()
        {
            title = "Add Part";
            part = new PartModel();
            part.Warehouse = new WarehouseModel()
            {
                Id = 0,
                WarehouseCode = "JH"
            };
            await modal.ShowAsync();
        }

        private async Task OnEditPartClick(PartModel prt)
        {
            title = "Edit Part";
            part = prt;
            await modal.ShowAsync();
        }

        private async Task OnHideModalClick()
        {
            await GetParts();
            await modal.HideAsync();
            await InvokeAsync(StateHasChanged);
        }

        private async void AddNewPart()
        {
            _loaderService.ShowLoader();
            var result = await partsService.AddNewPart(part);
            if (result != null)
            {
                part = new PartModel();
                await OnHideModalClick();
            }
            _loaderService.HideLoader();
        }

        private async Task GetParts()
        {
            _loaderService.ShowLoader();
            parts = await partsService.GetParts();
           
               
            _loaderService.HideLoader();
        }

        private async Task DeletePart(PartModel prt)
        {
            bool confirmed = await _confirmationService.ConfirmAsync("Are you sure?");
            if (confirmed)
            {
                _loaderService.ShowLoader();
                await partsService.DeletePart(part);
                parts?.Remove(prt);
                _loaderService.HideLoader();
            }
        }
    }
}
