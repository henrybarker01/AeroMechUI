using AeroMech.Data.Models;
using AeroMech.Models;
using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using AeroMech.UI.Web.Pages.Employee;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;

namespace AeroMech.UI.Web.Pages.ServiceReport
{
    public partial class AddServiceReport
    {
        [Inject] ClientService ClientService { get; set; }
        [Inject] EmployeeService EmployeeService { get; set; }
        [Inject] PartsService PartsService { get; set; }
        [Inject] VehicleService VehicleService { get; set; }
        [Inject] ServiceReportService ServiceReportService { get; set; }
        [Inject] NavigationManager NavigationManager { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }
        [Inject] protected BlazorBootstrap.ToastService ToastService { get; set; }

        [Parameter] public int serviceReportId { get; set; }

        private EditContext? editContext;
        private BlazorBootstrap.Modal salesOrderNumberModal = default!;

        private ServiceReportModel _serviceReport;
        List<EmployeeModel> employees = new List<EmployeeModel>();
        List<ClientModel> clients = new List<ClientModel>();
        List<VehicleModel> vehicles = new List<VehicleModel>();
        List<PartModel> parts = new List<PartModel>();
        ServiceReportPartModel selectedPart = new ServiceReportPartModel();
        ServiceReportEmployeeModel selectedEmployee = new ServiceReportEmployeeModel();

        private IEnumerable<RateType> RateTypes => Enum.GetValues<RateType>();

        protected override void OnInitialized()
        {
            _serviceReport = new ServiceReportModel();
            editContext = new(_serviceReport);

            base.OnInitialized();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _loaderService.ShowLoader();

                employees = await EmployeeService.GetEmployees();
                clients = await ClientService.GetClients();
                parts = await PartsService.GetParts();

                if (serviceReportId == 0)
                {
                    // pageTitle = "New Field Service Report";
                    InitServiceReport();
                }
                else
                {
                    // pageTitle = "Edit Field Service Report";
                    _serviceReport = await ServiceReportService.GetServiceReport(serviceReportId);

                    if (_serviceReport.ClientId != 0)
                    {
                        var vehicleId = _serviceReport.VehicleId;
                        await HandleOnChangeClient(_serviceReport.ClientId);

                        if (vehicleId != 0)
                        {
                            HandleOnChangeVehicle(vehicleId);
                        }
                    }

                    editContext = new(_serviceReport);
                }
                await InvokeAsync(StateHasChanged);
                _loaderService.HideLoader();
            }
        }

        private void InitServiceReport()
        {
            _serviceReport.Employees = new List<ServiceReportEmployeeModel>();

            _serviceReport.Parts = new List<ServiceReportPartModel>();

            _serviceReport.ReportDate = DateTime.Now;

            StateHasChanged();
        }

        private void RemoveLabour(int Id)
        {
            var employee = _serviceReport.Employees.SingleOrDefault(x => x.Id == Id);
            if (employee != null)
                employee.IsDeleted = true;
        }

        private void RemovePart(int Id)
        {
            var part = _serviceReport.Parts.SingleOrDefault(x => x.Id == Id);
            if (part != null)
                part.IsDeleted = true;
        }

        private double getTotal(ServiceReportEmployeeModel employee)
        {
            var t = ((employee.Rate * employee.Hours) - ((employee.Discount ?? 0 / 100) * (employee.Rate * employee.Hours))) ?? 0;
            return t;
        }

        private double getRate(ServiceReportEmployeeModel employee)
        {
            if (employee.RateType == RateType.None || _serviceReport.ClientId == 0) return 0;

            var clientRate = _serviceReport.Client?.Rates?.SingleOrDefault(x => x.RateType == employee.RateType);
            employee.RateType = clientRate?.RateType ?? employee.RateType;
            employee.Rate = clientRate?.Rate ?? 0;
            return employee.Rate;
        }

        //void HandleOnRateChangeUnbound(int rateType, int employeeId)
        //{
        //    var employee = serviceReport.Employees.Single(x => x.Id == employeeId);
        //    employee.RateType = (RateType)rateType;
        //}
        void HandleOnChangeUnbound(int employeeId)
        {
            var emp = employees.FirstOrDefault(x => x.Id == employeeId);
            if (emp == null) return;

            var employee = new ServiceReportEmployeeModel
            {
                FirstName = emp.FirstName,
                LastName = emp.LastName,
                EmployeeId = emp.Id,
                BirthDate = emp.BirthDate,
                Email = emp.Email,
                City = emp.City,
                AddressId = emp.AddressId,
                AddressLine1 = emp.AddressLine1,
                AddressLine2 = emp.AddressLine2,
                IDNumber = emp.IDNumber,
                PhoneNumber = emp.PhoneNumber,
                PostalCode = emp.PostalCode,
                Title = emp.Title,
                DutyDate = DateOnly.FromDateTime(DateTime.Now)
            };

            _serviceReport.Employees.Add(employee);
            selectedEmployee = new ServiceReportEmployeeModel();
        }

        private async Task HandleOnChangeClient(int clientId)
        {
            _serviceReport.ClientId = clientId;
            _serviceReport.Client = clients.SingleOrDefault(x => x.Id == clientId);
            _serviceReport.VehicleId = 0;
            vehicles = await VehicleService.GetVehicles(clientId);
            
            if (_serviceReport.Client == null)
            {
                _serviceReport.Client = new ClientModel();
            }

            StateHasChanged();
        }

        private async void Save()
        {
            var isValid = editContext.Validate();
            if (isValid)
            {
                _serviceReport.IsComplete = true;
            }
            var serviceReportId = await SaveServiceReport(_serviceReport, false);

            if (serviceReportId != 0)
            {
                _serviceReport.Id = serviceReportId;
                ToastService.Notify(new(ToastType.Success, $"Service report saved successfully."));
            }
            else
            {
                ToastService.Notify(new(ToastType.Danger, $"Service report could not be saved."));
            }
        }

        private async void SaveAndGenerateQuote()
        {
            var isValid = editContext.Validate();
            if (isValid)
            {
                _serviceReport.IsComplete = true;
                var serviceReportId = await SaveServiceReport(_serviceReport, true);

                if (serviceReportId != 0)
                {
                    _serviceReport.Id = serviceReportId;
                    ToastService.Notify(new(ToastType.Success, $"Service report saved successfully."));
                    NavigationManager.NavigateTo($"/ShowQuote/{serviceReportId}");
                }
                else
                {
                    ToastService.Notify(new(ToastType.Danger, $"Service report could not be saved."));
                }
            }
        }

        private async void SaveAndNew()
        {
            var isValid = editContext.Validate();
            if (isValid)
            {
                _serviceReport.IsComplete = true;
            }
            var result = await SaveServiceReport(_serviceReport, false);
            if (result != 0)
            {
                _serviceReport = new ServiceReportModel();
                InitServiceReport();
                ToastService.Notify(new(ToastType.Success, $"Service report saved successfully."));
            }
            else
            {
                ToastService.Notify(new(ToastType.Danger, $"Service report could not be saved."));
            }
        }

        private async void SaveAndGenerateServiceReport()
        {
            //if (string.IsNullOrEmpty(serviceReport.SalesOrderNumber))
            //{
            //	await salesOrderNumberModal.ShowAsync();
            //}
            //else
            //{
            await salesOrderNumberModal.HideAsync();
            var isValid = editContext.Validate();
            if (isValid)
            {
                _serviceReport.IsComplete = true;
                var serviceReportId = await SaveServiceReport(_serviceReport, false);

                if (serviceReportId != 0)
                {
                    _serviceReport.Id = serviceReportId;
                    ToastService.Notify(new(ToastType.Success, $"Service report saved successfully."));
                    NavigationManager.NavigateTo($"/ShowPDF/{serviceReportId}");
                }
                else
                {
                    ToastService.Notify(new(ToastType.Danger, $"Service report could not be saved."));
                }
            }
            //}
        }

        private string SearchTerm { get; set; } = string.Empty;
        private IEnumerable<PartModel> FilteredParts =>
            parts.Where(p => string.IsNullOrEmpty(SearchTerm) ||
                             p.PartCode.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                             p.PartDescription.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase));

        private async Task<int> SaveServiceReport(ServiceReportModel serviceReport, bool isQuote)
        {
            var isValid = editContext.Validate();
            if (isValid)
            {
                _serviceReport.IsComplete = _serviceReport.SalesOrderNumber != null;
            }

            serviceReport.Description = "Description";

            if (serviceReport.Id == 0)
            {
                return await ServiceReportService.AddServiceReport(serviceReport, isQuote);
            }
            else
            {
                return await ServiceReportService.EditServiceReport(serviceReport, isQuote);
            }
        }

        private void HandleOnServiceTypeChange(ServiceType serviceType)
        {
            _serviceReport.ServiceType = serviceType;
        }

        void HandleOnChangePart(int partId)
        {
            SearchTerm = "";
            if (partId == 999999)
            {
                var part = new ServiceReportPartModel
                {
                    Id = partId,
                    PartCode = "AdHock",
                    PartDescription = "",
                    SellingPrice = 0,
                    CostPrice = 0,
                    ProductClass = "",
                    Bin = "",
                    CycleCount = 0,
                    QtyOnHand = 0,
                    SupplierCode = "",
                    IsAdHockPart = true
                };

                _serviceReport.Parts.Add(part);
                selectedPart = new ServiceReportPartModel();
            }
            else
            {
                if (_serviceReport.Parts.Any(x => x.Id == partId))
                {
                    return;
                }

                var prt = parts.FirstOrDefault(x => x.Id == partId);
                if (prt == null) return;

                var part = new ServiceReportPartModel
                {
                    Id = prt.Id,
                    PartId = partId,
                    PartCode = prt.PartCode,
                    PartDescription = prt.PartDescription,
                    SellingPrice = Convert.ToDouble(prt.CostPrice),
                    CostPrice = prt.CostPrice,
                    ProductClass = prt.ProductClass,
                    Bin = prt.Bin,
                    CycleCount = prt.CycleCount,
                    QtyOnHand = prt.QtyOnHand,
                    Warehouse = prt.Warehouse,
                    SupplierCode = prt.SupplierCode,
                    IsAdHockPart = false
                };

                _serviceReport.Parts.Add(part);
                selectedPart = new ServiceReportPartModel();
            }
        }

        void HandleOnChangeVehicle(int vehicleId)
        {
            _serviceReport.VehicleId = vehicleId;
            _serviceReport.Vehicle = vehicles.FirstOrDefault(x => x.Id == vehicleId);
        }

        private async Task OnHideModalClick()
        {
            await salesOrderNumberModal.HideAsync();
        }
    }
}