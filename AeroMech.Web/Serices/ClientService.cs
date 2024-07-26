
using AeroMech.Models;
using Newtonsoft.Json; 

namespace AeroMech.UI.Serices
{
	public class ClientService
	{
		private readonly IConfiguration _configuration;
		private readonly HttpClient _httpClient;
		public ClientService(HttpClient httpClient, IConfiguration configuration)
		{
			_httpClient = httpClient;
			_configuration = configuration;
		}

		public async Task<List<ClientModel>> GetClients()
		{
			var response = await _httpClient.GetAsync($"{_configuration.GetValue<string>("ApiUrl")}Client/");
			string apiResponse = await response.Content.ReadAsStringAsync();
			return  JsonConvert.DeserializeObject<List<ClientModel>>(apiResponse);
		}
	}
}
