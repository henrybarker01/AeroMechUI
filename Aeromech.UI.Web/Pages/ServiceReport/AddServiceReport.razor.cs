using AeroMech.Models;
using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using BootstrapBlazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

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
        private string pageTitle = "";

        protected override async Task OnInitializedAsync()
        {
            serviceReport = new ServiceReportModel();
            editContext = new(serviceReport);

            employees = await EmployeeService.GetEmployees();
            clients = await ClientService.GetClients();
            parts = await PartsService.GetParts();

            if (serviceReportId == 0)
            {
                pageTitle = "New Field Service Report";
                InitServiceReport();
            }
            else
            {
                pageTitle = "Edit Field Service Report";
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

        void HandleOnChangeUnbound(int employeeId)
        {
            //if (serviceReport.Employees.Any(x => x.Id == employeeId))
            //{
            //	return;
            //}

            var employee = new ServiceReportEmployeeModel();
            var emp = employees.First(x => x.Id == employeeId);
            employee.FirstName = emp.FirstName;
            employee.LastName = emp.LastName;
            employee.Id = emp.Id;
            employee.Rate = emp.Rate;
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
                part.PartCode = prt.PartCode;
                part.PartDescription = prt.PartDescription;
                part.SellingPrice = prt.CostPrice;
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