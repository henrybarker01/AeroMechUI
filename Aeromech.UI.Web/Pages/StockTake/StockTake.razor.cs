using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.StockTake
{
    public partial class StockTake
    {

        [Inject] PartsService partsService { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }
        [Inject] protected ConfirmationService _confirmationService { get; set; }

        private string title = "";
        private string SearchTerm = string.Empty;

       // private Modal modal = default!;
       // private Modal rateModal = default!;

        private PartModel part = new PartModel();
        private List<PartModel>? parts = new List<PartModel>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                //await GetParts();
                await InvokeAsync(StateHasChanged);
            }
        }

        private void OnAddStockTakeClick()
        {

        }
    }
}
