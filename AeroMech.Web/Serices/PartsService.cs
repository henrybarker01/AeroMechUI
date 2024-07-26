using AeroMech.Models;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace AeroMech.UI.Serices
{
	public class PartsService
	{
		private readonly IConfiguration _configuration;
		private readonly HttpClient _httpClient;

		public PartsService(IConfiguration configuration, HttpClient httpClient)
		{
			_configuration = configuration;
			_httpClient = httpClient;
		}

		public async Task<List<PartModel>> GetParts()
		{
			var response = await _httpClient.GetAsync($"{_configuration.GetValue<string>("ApiUrl")}part/parts");
			string apiResponse = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<List<PartModel>>(apiResponse);
		}

		public async Task DeletePart(PartModel prt)
		{
			await _httpClient.DeleteAsync($"{_configuration.GetValue<string>("ApiUrl")}part/delete/{prt.Id}");
		}

		public async Task<HttpResponseMessage> AddNewPart(PartModel part)
		{
			if (part.Id == 0)
			{
				return await _httpClient.PostAsJsonAsync<PartModel>($"{_configuration.GetValue<string>("ApiUrl")}part/add", part);			 
			}
			else
			{
				return await _httpClient.PostAsJsonAsync<PartModel>($"{_configuration.GetValue<string>("ApiUrl")}part/edit", part);				
			}
		}
	}
}
