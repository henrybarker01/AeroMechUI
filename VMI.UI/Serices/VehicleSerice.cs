using AeroMech.Models;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace VMI.UI.Serices
{
	public class VehicleService
	{
		private readonly IConfiguration _configuration;
		private readonly HttpClient _httpClient;

		public VehicleService(IConfiguration configuration, HttpClient httpClient)
		{
			_configuration = configuration;
			_httpClient = httpClient;
		}

		public async Task<List<VehicleModel>> GetVehicles(int clientId)
		{
			var response = await _httpClient.GetAsync($"{_configuration.GetValue<string>("ApiUrl")}vehicle/vehicles/{clientId}");
			string apiResponse = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<List<VehicleModel>>(apiResponse);
		}

		public async Task DeleteVehicle(VehicleModel vehicle)
		{
			await _httpClient.DeleteAsync($"{_configuration.GetValue<string>("ApiUrl")}vehicle/delete/{vehicle.Id}");
		}

		public async Task<HttpResponseMessage> AddNewVehicle(VehicleModel vehicle)
		{
			if (vehicle.Id == 0)
			{
				return await _httpClient.PostAsJsonAsync<VehicleModel>($"{_configuration.GetValue<string>("ApiUrl")}vehicle/add", vehicle);			 
			}
			else
			{
				return await _httpClient.PostAsJsonAsync<VehicleModel>($"{_configuration.GetValue<string>("ApiUrl")}vehicle/edit", vehicle);				
			}
		}
	}
}
