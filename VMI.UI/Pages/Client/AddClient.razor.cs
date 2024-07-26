using AeroMech.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace VMI.UI.Pages.Client
{
	public partial class AddClient
	{
		[Inject]
		HttpClient httpClient { get; set; }

		[Inject]
		IConfiguration configuration { get; set; }

		[Parameter]
		public EventCallback OnCloseClick { get; set; }

		private ClientModel client = new ClientModel();

		private async void AddNewClient()
		{			 
				var result = await httpClient.PostAsJsonAsync<ClientModel>($"{configuration.GetValue<string>("ApiUrl")}Client/add", client);
				if (result != null)
				{
					OnCloseClick.InvokeAsync();
				}
		}
		 
	}
}