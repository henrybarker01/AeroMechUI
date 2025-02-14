using AeroMech.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Modal = BlazorBootstrap.Modal;

namespace AeroMech.UI.Web.Pages.Employee
{
    public partial class Employee
    {

        [Inject] EmployeeService employeeService { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }
        [Inject] protected ConfirmationService _confirmationService { get; set; }

        private string title = "";

        private Modal modal = default!;
        private Modal rateModal = default!;

        private EmployeeModel employee = new EmployeeModel();
        private List<EmployeeModel>? employees = new List<EmployeeModel>();
        private List<ClientRateModel>? employeeRates;

        Grid<ClientRateModel> employeeRatesGrid = default!;


        private string SearchTerm { get; set; } = string.Empty;
        private IEnumerable<EmployeeModel> FilteredEmpoyees =>
        employees.Where(employee =>
            string.IsNullOrEmpty(SearchTerm) ||
            employee.FirstName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            employee.LastName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
        );

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await GetEmployees();
            }
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
            _loaderService.ShowLoader();
            var result = await employeeService.AddNewEmployee(employee);
            if (result != null)
            {
                employee = new EmployeeModel();
                await OnHideModalClick();
            }
            _loaderService.HideLoader();
        }

        private async void EditEmployeeRate(EmployeeModel employee)
        {

            await rateModal.ShowAsync();
            await employeeRatesGrid.RefreshDataAsync();

        }

        private async Task GetEmployees()
        {
            _loaderService.ShowLoader();
            employees = await employeeService.GetEmployees();
            await InvokeAsync(StateHasChanged);
            _loaderService.HideLoader();
        }

        private async Task DeleteEmployee(EmployeeModel emp)
        {
            bool confirmed = await _confirmationService.ConfirmAsync("Are you sure?");
            if (confirmed)
            {
                _loaderService.ShowLoader();
                await employeeService.DeleteEmployee(emp);
                employees?.Remove(emp);
                _loaderService.HideLoader();
            }
        }
    }
}