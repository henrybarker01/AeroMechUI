using AeroMech.Models;
using AeroMech.UI.Serices;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace AeroMech.Web.Components.Pages.Employee
{
	public partial class Employee
	{
		[Inject]
		HttpClient httpClient { get; set; }

		[Inject]
		IConfiguration configuration { get; set; }

		[Inject]
		EmployeeService employeeService { get; set; }

		private string title = "";

		private Modal modal = default!;
		private Modal rateModal = default!;

		private EmployeeModel employee = new EmployeeModel();
		private List<EmployeeModel>? employees;
		private List<EmployeeRateModel>? employeeRates;

		Grid<EmployeeRateModel> employeeRatesGrid = default!;

		protected override void OnInitialized()
		{
			if (employeeRates == null)
			{
				employeeRates = new List<EmployeeRateModel>();
			}
			employeeRates.Add(new EmployeeRateModel()
			{
				EffectiveDate = DateTime.Now,
				EmployeeId = 4,
				IsActive = true,
				Rate = 650.00
			});
			employeeRates.Add(new EmployeeRateModel()
			{
				EffectiveDate = DateTime.Now,
				EmployeeId = 4,
				IsActive = true,
				Rate = 680.00
			});
			employeeRates.Add(new EmployeeRateModel()
			{
				EffectiveDate = DateTime.Now,
				EmployeeId = 4,
				IsActive = false,
				Rate = 690.00
			});
		}

		private async Task OnShowModalClick()
		{
			title = "Add Employee";
			employee = new EmployeeModel();
			await modal.ShowAsync();
		}

		private async Task OnEditEmployeeClick(EmployeeModel emp)
		{
			title = "Edit Employee";
			employee = emp;
			await modal.ShowAsync();
		}

		private async Task OnHideModalClick()
		{
			await GetEmployees();
			StateHasChanged();
			await modal.HideAsync();
		}

		private async void AddNewEmployee()
		{
			var result = await employeeService.AddNewEmployee(employee);
			if (result != null)
			{
				employee = new EmployeeModel();
				await OnHideModalClick();
			}
		}

		private async void EditEmployeeRate(EmployeeModel employee)
		{

			await rateModal.ShowAsync();
			await employeeRatesGrid.RefreshDataAsync();

		}

		protected override async Task OnInitializedAsync()
		{
			await GetEmployees();
		}

		private async Task GetEmployees()
		{
			employees = await employeeService.GetEmployees();
		}

		private async Task DeleteEmployee(EmployeeModel emp)
		{
			await employeeService.DeleteEmployee(emp); 
			employees?.Remove(emp);
		}
	}
}