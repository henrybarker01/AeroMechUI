using AeroMech.Models; 
using Newtonsoft.Json; 
using System.Net.Http.Json;

namespace VMI.UI.Serices
{
	public class EmployeeService
	{
		private readonly IConfiguration _configuration;
		private readonly HttpClient _httpClient;

		public EmployeeService(IConfiguration configuration, HttpClient httpClient)
		{
			_configuration = configuration;
			_httpClient = httpClient;
		}

		public async Task<List<EmployeeModel>> GetEmployees()
		{
			var response = await _httpClient.GetAsync($"{_configuration.GetValue<string>("ApiUrl")}employee/employees");
			string apiResponse = await response.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<List<EmployeeModel>>(apiResponse);
		}

		public async Task DeleteEmployee(EmployeeModel emp)
		{
			await _httpClient.DeleteAsync($"{_configuration.GetValue<string>("ApiUrl")}employee/delete/{emp.Id}");
		}

		public async Task<HttpResponseMessage> AddNewEmployee(EmployeeModel employee)
		{
			if (employee.Id == 0)
			{
				return await _httpClient.PostAsJsonAsync<EmployeeModel>($"{_configuration.GetValue<string>("ApiUrl")}employee/add", employee);
				
			}
			else
			{
				return await _httpClient.PostAsJsonAsync<EmployeeModel>($"{_configuration.GetValue<string>("ApiUrl")}employee/edit", employee);				 
			}
		}
	}
}
