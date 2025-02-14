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
        ServiceReportModel serviceReport;
        List<EmployeeModel> employees = new List<EmployeeModel>();
        List<ClientModel> clients = new List<ClientModel>();
        List<VehicleModel> vehicles = new List<VehicleModel>();
        List<PartModel> parts = new List<PartModel>();
        ServiceReportPartModel selectedPart = new ServiceReportPartModel();
        ServiceReportEmployeeModel selectedEmployee = new ServiceReportEmployeeModel();

        private IEnumerable<RateType> RateTypes => Enum.GetValues<RateType>();

        protected override void OnInitialized()
        {
            serviceReport = new ServiceReportModel();
            editContext = new(serviceReport);            

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
                    serviceReport = await ServiceReportService.GetServiceReport(serviceReportId);

                    if (serviceReport.ClientId != 0)
                    {
                        var vehicleId = serviceReport.VehicleId;
                        await HandleOnChangeClient(serviceReport.ClientId);

                        if (vehicleId != 0)
                        {
                            HandleOnChangeVehicle(vehicleId);
                        }
                    }

                    editContext = new(serviceReport);
                }
                await InvokeAsync(StateHasChanged);
                _loaderService.HideLoader();
            }
        }

        private void InitServiceReport()
        {
            serviceReport.Employees = new List<ServiceReportEmployeeModel>();

            serviceReport.Parts = new List<ServiceReportPartModel>();

            serviceReport.ReportDate = DateTime.Now;

            StateHasChanged();
        }

        private void RemoveLabour(int Id)
        {
            serviceReport.Employees.Single(x => x.Id == Id).IsDeleted = true;
        }

        private void RemovePart(int Id)
        {
            serviceReport.Parts.Single(x => x.Id == Id).IsDeleted = true;
        }

        private double getTotal(ServiceReportEmployeeModel employee)
        {
            var t = ((employee.Rate * employee.Hours) - ((employee.Discount ?? 0 / 100) * (employee.Rate * employee.Hours))) ?? 0;
            return t;
        }

        private double getRate(ServiceReportEmployeeModel employee)
        {
            if (employee.RateType == RateType.None || serviceReport.ClientId == 0) return 0;

            var clientRate = serviceReport.Client.Rates.SingleOrDefault(x => x.RateType == employee.RateType);
            employee.RateType = clientRate.RateType;
            employee.Rate = clientRate.Rate;
            return employee.Rate;
        }

        private void OnRateChanged(object newRate)
        {
            var o = 1;

            //employee.Rate = newRate;
            //Console.WriteLine($"Rate changed to: {newRate}"); // Debugging
        }

        //void HandleOnRateChangeUnbound(int rateType, int employeeId)
        //{
        //    var employee = serviceReport.Employees.Single(x => x.Id == employeeId);
        //    employee.RateType = (RateType)rateType;
        //}
        void HandleOnChangeUnbound(int employeeId)
        {
            var employee = new ServiceReportEmployeeModel();
            var emp = employees.First(x => x.Id == employeeId);
            employee.FirstName = emp.FirstName;
            employee.LastName = emp.LastName;
            employee.Id = emp.Id;
            //employee.Rates = emp.Rate;
            employee.BirthDate = emp.BirthDate;
            employee.Email = emp.Email;
            employee.City = emp.City;
            employee.AddressId = emp.AddressId;
            employee.AddressLine1 = emp.AddressLine1;
            employee.AddressLine2 = emp.AddressLine2;
            employee.IDNumber = emp.IDNumber;
            employee.PhoneNumber = emp.PhoneNumber;
            employee.PostalCode = emp.PostalCode;
            employee.Title = emp.Title;
            employee.DutyDate = DateOnly.FromDateTime(DateTime.Now);


            serviceReport.Employees.Add(employee);
            selectedEmployee = new ServiceReportEmployeeModel();
        }

        private async Task HandleOnChangeClient(int clientId)
        {
            serviceReport.ClientId = clientId;
            serviceReport.Client = clients.Single(x => x.Id == clientId);
            serviceReport.VehicleId = 0;
            vehicles = await VehicleService.GetVehicles(clientId);
            if (serviceReport.Client == null)
            {
                serviceReport.Client = new ClientModel();
            }

            StateHasChanged();
        }

        private async void Save()
        {
            var isValid = editContext.Validate();
            if (isValid)
            {
                serviceReport.IsComplete = true;//serviceReport.SalesOrderNumber != null;
            }
            var serviceReportId = await SaveServiceReport(serviceReport, false);

            if (serviceReportId != 0)
            {
                serviceReport.Id = serviceReportId;
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
                serviceReport.IsComplete = true;// serviceReport.SalesOrderNumber != null;
                var serviceReportId = await SaveServiceReport(serviceReport, true);

                if (serviceReportId != 0)
                {
                    serviceReport.Id = serviceReportId;
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
                serviceReport.IsComplete = true;// serviceReport.SalesOrderNumber != null; ;

            }
            var result = await SaveServiceReport(serviceReport, false);
            if (result != 0)
            {
                serviceReport = new ServiceReportModel();
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
                serviceReport.IsComplete = true;// serviceReport.SalesOrderNumber != null;
                var serviceReportId = await SaveServiceReport(serviceReport, false);

                if (serviceReportId != 0)
                {
                    serviceReport.Id = serviceReportId;
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


        private async Task<int> SaveServiceReport(ServiceReportModel serviceReportToAdd, bool isQuote)
        {
            var isValid = editContext.Validate();
            if (isValid)
            {
                serviceReport.IsComplete = serviceReport.SalesOrderNumber != null;

            }
            serviceReportToAdd.Description = "Description";
            return await ServiceReportService.AddServiceReport(serviceReportToAdd, isQuote);
        }

        private void HandleOnServiceTypeChange(ServiceType serviceType)
        {
            serviceReport.ServiceType = serviceType;
        }

        void HandleOnChangePart(int partId)
        {
            SearchTerm = "";
            if (partId == 999999)
            {
                var part = new ServiceReportPartModel { Id = partId };

                part.PartCode = "AdHock";
                part.PartDescription = "";
                part.SellingPrice = 0;
                part.CostPrice = 0;
                part.ProductClass = "";
                part.Bin = "";
                part.CycleCount = 0;
                part.QtyOnHand = 0;
                //part.Warehouse = ;
                part.SupplierCode = "";
                part.IsAdHockPart = true;

                serviceReport.Parts.Add(part);
                selectedPart = new ServiceReportPartModel();
            }
            else
            {
                if (serviceReport.Parts.Any(x => x.Id == partId))
                {
                    return;
                }

                var part = new ServiceReportPartModel { Id = partId };
                var prt = parts.First(x => x.Id == partId);
                part.Id = prt.Id;
                part.PartId = partId;
                part.PartCode = prt.PartCode;
                part.PartDescription = prt.PartDescription;
                part.SellingPrice = Convert.ToDouble(prt.CostPrice);
                part.CostPrice = prt.CostPrice;
                part.ProductClass = prt.ProductClass;
                part.Bin = prt.Bin;
                part.CycleCount = prt.CycleCount;
                part.QtyOnHand = prt.QtyOnHand;
                part.Warehouse = prt.Warehouse;
                part.SupplierCode = prt.SupplierCode;
                part.IsAdHockPart = false;

                serviceReport.Parts.Add(part);
                selectedPart = new ServiceReportPartModel();
            }

        }

        void HandleOnChangeVehicle(int vehicleId)
        {
            serviceReport.VehicleId = vehicleId;
            serviceReport.Vehicle = vehicles.First(x => x.Id == vehicleId);
        }

        private async Task OnHideModalClick()
        {
            await salesOrderNumberModal.HideAsync();
        }
    }
}