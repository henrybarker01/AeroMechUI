using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;
using Modal = BlazorBootstrap.Modal;

namespace AeroMech.UI.Web.Pages.Employee
{
    public partial class Employee
    {
        [Inject] private EmployeeService _employeeService { get; set; }
        [Inject] private LoaderService _loaderService { get; set; }
        [Inject] private ConfirmationService _confirmationService { get; set; }

        private string title = "";

        private Modal modal = default!;
    
        private EmployeeModel _employee = new EmployeeModel();
        private List<EmployeeModel>? _employees = new List<EmployeeModel>();
       
        private bool MatchesSearch(EmployeeModel employee, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var t = term.Trim();

            return
                (employee.FirstName ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (employee.LastName ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase);
        }

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
            _employee = new EmployeeModel();
            await modal.ShowAsync();
        }

        private async Task OnEditEmployeeClick(EmployeeModel emp)
        {
            title = "Edit Employee";
            _employee = emp;
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
            var result = await _employeeService.AddNewEmployee(_employee);
            if (result != null)
            {
                _employee = new EmployeeModel();
                await OnHideModalClick();
            }
            _loaderService.HideLoader();
        }

        private async Task GetEmployees()
        {
            _loaderService.ShowLoader();
            _employees = await _employeeService.GetEmployees();
            await InvokeAsync(StateHasChanged);
            _loaderService.HideLoader();
        }

        private async Task DeleteEmployee(EmployeeModel emp)
        {
            bool confirmed = await _confirmationService.ConfirmAsync("Are you sure?");
            if (confirmed)
            {
                _loaderService.ShowLoader();
                await _employeeService.DeleteEmployee(emp);
                _employees?.Remove(emp);
                _loaderService.HideLoader();
            }
        }
    }
}