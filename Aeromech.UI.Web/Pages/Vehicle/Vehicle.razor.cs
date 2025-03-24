using AeroMech.Models;
using AeroMech.UI.Web.Pages.Users;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;


namespace AeroMech.UI.Web.Pages.Vehicle
{
    public partial class Vehicle
    {

        [Inject] VehicleService vehicleService { get; set; }
        [Inject] ClientService clientService { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }
        [Inject] protected ConfirmationService _confirmationService { get; set; }

        private string title = "";

        private int selectedClientId = 0;
        private bool pleaseSelectClient;

        private Modal modal = default!;

        private List<ClientModel> clients = new List<ClientModel>();
        private VehicleModel vehicle = new VehicleModel();
        private List<VehicleModel>? vehicles = new List<VehicleModel>();

        private string SearchTerm { get; set; } = string.Empty;
        private IEnumerable<VehicleModel> FilteredVehicles =>
        vehicles.Where(vehicle =>
            string.IsNullOrEmpty(SearchTerm) ||
            vehicle.Description.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            vehicle.ChassisNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            vehicle.SerialNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
            vehicle.JobNumber.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)
        );

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _loaderService.ShowLoader();
                clients = await clientService.GetClients();

                if (clients != null && clients.Count() == 1)
                {
                    ClientSelected(clients.First().Id);
                }
                await InvokeAsync(StateHasChanged);
                _loaderService.HideLoader();
            }
        }

        private async Task OnAddVehicleClick()
        {
            if (selectedClientId != 0)
            {
                title = "Add Vehicle";
                vehicle = new VehicleModel();
                vehicle.ClientId = selectedClientId;
                await modal.ShowAsync();
            }
            else
            {
                pleaseSelectClient = true;
            }

        }

        private async Task OnEditVehicleClick(VehicleModel vehicle)
        {
            title = "Edit Vehicle";
            this.vehicle = vehicle;
            await modal.ShowAsync();
        }

        private async Task OnHideModalClick()
        {
            await GetVehicles(selectedClientId);
            StateHasChanged();
            await modal.HideAsync();
        }

        private async void AddNewVehicle()
        {
            var result = await vehicleService.AddNewVehicle(vehicle);
            if (result != null)
            {
                vehicle = new VehicleModel();
                await OnHideModalClick();
            }
        }

        private async Task GetVehicles(int clientId)
        {
            _loaderService.ShowLoader();
            vehicles = await vehicleService.GetVehicles(clientId);
            await InvokeAsync(StateHasChanged);
            _loaderService.HideLoader();
        }

        private void HandleOnChangeClient(ChangeEventArgs args)
        {
            int clientId = int.Parse(args.Value.ToString());
            ClientSelected(clientId);
        }

        private async void ClientSelected(int clientId)
        {
            selectedClientId = clientId;
            await GetVehicles(clientId);
            StateHasChanged();
        }

        private async Task DeleteVehicle(VehicleModel vehicle)
        {
            bool confirmed = await _confirmationService.ConfirmAsync("Are you sure?");
            if (confirmed)
            {
                _loaderService.ShowLoader();
                await vehicleService.DeleteVehicle(vehicle);
                vehicles?.Remove(vehicle);
                _loaderService.HideLoader();
            }
        }
    }
}
