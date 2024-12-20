﻿using AeroMech.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;


namespace AeroMech.UI.Web.Pages.Part
{
    public partial class Part
    {

        [Inject]
        PartsService partsService { get; set; }

        private string title = "";

        private Modal modal = default!;
        private Modal rateModal = default!;

        private PartModel part = new PartModel();
        private List<PartModel>? parts;
       

        protected override void OnInitialized()
        {
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
            StateHasChanged();
            await modal.HideAsync();
        }

        private async void AddNewPart()
        {
            var result = await partsService.AddNewPart(part);
            if (result != null)
            {
                part = new PartModel();
                await OnHideModalClick();
            }
        }

        // private async void EditPartRate(PartModel part)
        // {
        // 	await rateModal.ShowAsync();
        // 	await employeeRatesGrid.RefreshDataAsync();

        // }

        protected override async Task OnInitializedAsync()
        {
            await GetParts();
        }

        private async Task GetParts()
        {
            parts = await partsService.GetParts();
        }

        private async Task DeletePart(PartModel prt)
        {
            await partsService.DeletePart(part);
            parts?.Remove(prt);
        }
    }
}
