using AeroMech.Models;
using AeroMech.UI.Serices;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace AeroMech.Web.Components.Pages.Client
{
    public partial class Clients
    {
        [Inject]
        HttpClient httpClient { get; set; }

        [Inject]
        IConfiguration configuration { get; set; }

        [Inject] ClientService ClientService { get; set; }

        private string title = "";

        private Modal modal = default!;
        private ClientModel client = new ClientModel();
        private List<ClientModel>? clients;
       
        protected override async Task OnInitializedAsync()
        {
            await GetClients();
        }

        private async Task OnShowModalClick()
        {
            title = "Add Client";
            client = new ClientModel();
            await modal.ShowAsync();
        }

        private async Task OnEditClientClick(ClientModel clnt)
        {
            title = "Edit Client";
            client = clnt;
            await modal.ShowAsync();
        }

        private async Task OnHideModalClick()
        {
            await GetClients();
            StateHasChanged();
            await modal.HideAsync();
        }
 
        private async void AddNewClient()
        {
           


                if (client.Id == 0)
                {
                    var result = await httpClient.PostAsJsonAsync<ClientModel>($"{configuration.GetValue<string>("ApiUrl")}Client/add", client);
                    if (result != null)
                    {
                        await OnHideModalClick();
                    }
                }
                else
                {
                    var result = await httpClient.PostAsJsonAsync<ClientModel>($"{configuration.GetValue<string>("ApiUrl")}Client/edit", client);
                    if (result != null)
                    {
                        var kak = await result.Content.ReadAsStringAsync();
                        var morekak = JsonConvert.DeserializeObject(kak);

                        client = new ClientModel();
                        await OnHideModalClick();
                    }
                }
        
        }

        private async Task GetClients()
        {
            clients = await ClientService.GetClients();
        }

        private async Task DeleteClient(AeroMech.Models.ClientModel client)
        {
            await httpClient.DeleteAsync($"{configuration.GetValue<string>("ApiUrl")}Client/Delete/{client.Id}");
            clients?.Remove(client);
        }
    }
}