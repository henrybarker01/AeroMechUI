using AeroMech.Models;
using VMI.UI.Serices;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Net.Http.Json;


namespace VMI.UI.Pages.Part
{
	public partial class Part
	{
		[Inject]
		IConfiguration configuration { get; set; }

		[Inject]
		HttpClient httpClient { get; set; }

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
