using AeroMech.Models;
using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using AeroMech.UI.Serices;
using Azure;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AeroMech.UI.Pages.ServiceReport
{
	public partial class AddServiceReport
	{
		[Inject] ClientService ClientService { get; set; }
		[Inject] EmployeeService EmployeeService { get; set; }
		[Inject] PartsService PartsService { get; set; }
		[Inject] VehicleService VehicleService { get; set; }
		[Inject] ServiceReportService ServiceReportService { get; set; }
		[Inject] NavigationManager NavigationManager { get; set; }

		[Parameter] public int servceReportId { get; set; }

		ServiceReportModel serviceReport;
		List<EmployeeModel> employees = new List<EmployeeModel>();
		List<ClientModel> clients = new List<ClientModel>();
		List<VehicleModel> vehicles = new List<VehicleModel>();
		List<PartModel> parts = new List<PartModel>();
		ServiceReportPartModel selectedPart = new ServiceReportPartModel();
		ServiceReportEmployeeModel selectedEmployee = new ServiceReportEmployeeModel();

		protected override async Task OnInitializedAsync()
		{
			serviceReport = new ServiceReportModel();
			employees = await EmployeeService.GetEmployees();
			clients = await ClientService.GetClients();
			parts = await PartsService.GetParts();

			if (servceReportId == 0)
			{
				InitServiceReport();
			}
			else
			{
				serviceReport = await ServiceReportService.GetServiceReport(servceReportId);
				//serviceReport?.Employees?.Add(new ServiceReportEmployeeModel() { IsAdding = true });
				//serviceReport?.Parts?.Add(new ServiceReportPartModel() { IsAdding = true });
			}
		}

		private void InitServiceReport()
		{


			serviceReport.Employees = new List<ServiceReportEmployeeModel>();
			//serviceReport.Employees?.Add(new ServiceReportEmployeeModel() { IsAdding = true });

			serviceReport.Parts = new List<ServiceReportPartModel>();
			//serviceReport.Parts?.Add(new ServiceReportPartModel() { IsAdding = true });

			serviceReport.ReportDate = DateTime.Now;

			StateHasChanged();
		}

		//private void AddLabour()
		//{
		//	if (!serviceReport.Employees.Any(x => x.IsAdding))
		//	{
		//		serviceReport.Employees.Add(new ServiceReportEmployeeModel() { IsAdding = true });
		//	}
		//}

		//private void AddParts()
		//{
		//	//if (!serviceReport.Parts.Any(x => x.IsAdding))
		//	//{
		//	//	serviceReport.Parts.Add(new ServiceReportPartModel() { IsAdding = true });
		//	//}
		//}

		private void RemoveLabour(int Id)
		{
			serviceReport.Employees.Remove(serviceReport.Employees.Single(x => x.Id == Id));
		}

		private void RemovePart(int Id)
		{
			serviceReport.Parts.Remove(serviceReport.Parts.Single(x => x.Id == Id));
		}

		void HandleOnChangeUnbound(int employeeId)
		{
			if (serviceReport.Employees.Any(x => x.Id == employeeId))
			{
				return;
			}

			var employee = new ServiceReportEmployeeModel();
			var emp = employees.First(x => x.Id == employeeId);
			employee.FirstName = emp.FirstName;
			employee.LastName = emp.LastName;
			employee.Id = emp.Id;
			employee.Rate = emp.Rate;
			//employee.ServceType = employee.ServceType;
			employee.BirthDate = emp.BirthDate;
			employee.Email = emp.Email;	
			employee.City = emp.City;
			employee.AddressId = emp.AddressId;
			employee.AddressLine1 = emp.AddressLine1;
			employee.AddressLine2 = emp.AddressLine2;
			employee.IDNumber = emp.IDNumber;
			employee.PhoneNumber = emp.PhoneNumber;
			employee.PostalCode = emp.PostalCode;
			employee.Title	= emp.Title;


			serviceReport.Employees.Add(employee);
			selectedEmployee = new ServiceReportEmployeeModel();
		}

		private async Task HandleOnChangeClient(int clientId)
		{
			serviceReport.ClientId = clientId;
			serviceReport.Client = clients.Single(x => x.Id == clientId);

			vehicles = await VehicleService.GetVehicles(clientId);
			if (serviceReport.Client == null)
			{
				serviceReport.Client = new ClientModel();
			}

			StateHasChanged();
		}

		private async void SaveAndPrint()
		{
			var result = await SaveServiceReport(serviceReport);

			if (result != null)
			{
				string apiResponse = await result.Content.ReadAsStringAsync();
				NavigationManager.NavigateTo($"/ShowPDF/{int.Parse(apiResponse)}");
			}
		}

		private async void SaveAndNew(ServiceReportModel serviceReportToAdd)
		{
			var result = await SaveServiceReport(serviceReportToAdd);
			if (result != null)
			{
				InitServiceReport();
			}
		}

		private async Task<HttpResponseMessage> SaveServiceReport(ServiceReportModel serviceReportToAdd)
		{
			serviceReportToAdd.Description = "Description";
			serviceReportToAdd.SalesOrderNumber = "SAN";

			return await ServiceReportService.AddServiceReport(serviceReportToAdd);
		}

		private void HandleOnServiceTypeChange(ServiceType serviceType)
		{
			serviceReport.ServiceType = serviceType;
					}

		void HandleOnChangePart(int partId)
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
			
			serviceReport.Parts.Add(part);
			selectedPart = new ServiceReportPartModel();
		}

		void HandleOnChangeVehicle(int vehicleId)
		{
			serviceReport.VehicleId = vehicleId;
			serviceReport.Vehicle = vehicles.First(x => x.Id == vehicleId);
		}
	}
}