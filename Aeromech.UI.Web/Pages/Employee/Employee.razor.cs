using AeroMech.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Employee
{
    public partial class Employee
	{

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

        private string SearchTerm { get; set; } = string.Empty;
        private IEnumerable<EmployeeModel> FilteredEmpoyees =>
        employees.Where(employee =>
            string.IsNullOrEmpty(SearchTerm) ||
            employee.FirstName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            employee.LastName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
        );

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