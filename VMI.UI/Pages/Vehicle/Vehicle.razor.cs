using AeroMech.Models;
using VMI.UI.Serices;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;


namespace VMI.UI.Pages.Vehicle
{
    public partial class Vehicle
    {
        [Inject]
        IConfiguration configuration { get; set; }

        [Inject]
        HttpClient httpClient { get; set; }

        [Inject]
        VehicleService vehicleService { get; set; }

        [Inject]

        ClientService clientService { get; set; }

        private string title = "";

        private int selectedClientId = 0;
        private bool pleaseSelectClient;

        private Modal modal = default!;

        private List<ClientModel> clients = new List<ClientModel>();
        private VehicleModel vehicle = new VehicleModel();
        private List<VehicleModel>? vehicles;

        protected override void OnInitialized()
        {
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
            vehicle = vehicle;
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

        protected override async Task OnInitializedAsync()
        {
            clients = await clientService.GetClients();

            if (clients != null && clients.Count() == 1)
            {
                ClientSelected(clients.First().Id);
            }
        }

        private async Task GetVehicles(int clientId)
        {
            vehicles = await vehicleService.GetVehicles(clientId);
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
            await vehicleService.DeleteVehicle(vehicle);
            vehicles?.Remove(vehicle);
        }
    }
}
