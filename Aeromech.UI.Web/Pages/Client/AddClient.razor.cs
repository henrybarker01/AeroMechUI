using AeroMech.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Client
{
    public partial class AddClient
    {
        [Inject] ClientService clientService { get; set; }

        [Parameter]
        public EventCallback OnCloseClick { get; set; }

        private ClientModel client = new ClientModel();

        private async void AddNewClient()
        {
            var result = await clientService.AddClient(client);

            if (result != 0)
            {
                OnCloseClick.InvokeAsync();
            }
        }

    }
}