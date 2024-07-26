﻿using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace AeroMech.UI.Web.Pages.Users
{
    public partial class Users
    {

        [Inject] UserService userService { get; set; }

        private string title = "";
        private Modal modal = default!;
        private IdentityUser user = new IdentityUser();
        private List<IdentityUser>? users;

        protected override async Task OnInitializedAsync()
        {
            await GetUsers();
        }

        private async Task GetUsers()
        {
            users = await userService.GetUsers();
        }

        private async Task AddUserClick()
        {
            title = "Add User";
            //client = new UserModel();
            await modal.ShowAsync();
        }

        private async void AddUser()
        {
            user.EmailConfirmed = true;
            user.LockoutEnabled = true;
            user.PhoneNumberConfirmed = true;
            user.TwoFactorEnabled = false;

            var result = await userService.CreateUser(user);
            if (result.Succeeded)
            {
                await OnHideModalClick();
            }
        }

        private async Task DeleteUser(IdentityUser user)
        {
            await userService.DeleteUser(user);
            users?.Remove(user);
        }

        private async Task EditUser(IdentityUser user)
        {
            //await clientService.Delete(client.Id);
            //clients?.Remove(client);
        }

        private async Task OnHideModalClick()
        {
            await GetUsers();
            StateHasChanged();
            await modal.HideAsync();
        }
    }
}


//namespace AeroMech.UI.Web.Pages.Client
//{
//    public partial class Clients
//    {

//        [Inject]
//        IConfiguration configuration { get; set; }

//        [Inject] ClientService clientService { get; set; }

//        private string title = "";

//        private Modal modal = default!;
//        private ClientModel client = new ClientModel();
//        private List<ClientModel>? clients;

//        protected override async Task OnInitializedAsync()
//        {
//            await GetClients();
//        }

//        private async Task OnShowModalClick()
//        {
//            title = "Add Client";
//            client = new ClientModel();
//            await modal.ShowAsync();
//        }

//        private async Task OnEditClientClick(ClientModel clnt)
//        {
//            title = "Edit Client";
//            client = clnt;
//            await modal.ShowAsync();
//        }

//        private async Task OnHideModalClick()
//        {
//            await GetClients();
//            StateHasChanged();
//            await modal.HideAsync();
//        }

//        private async void AddNewClient()
//        {
//            if (client.Id == 0)
//            {
//                var result = await clientService.AddClient(client);
//                if (result != 0)
//                {
//                    await OnHideModalClick();
//                }
//            }
//            else
//            {
//                throw new NotImplementedException();

//                //var result = await httpClient.PostAsJsonAsync<ClientModel>($"{configuration.GetValue<string>("ApiUrl")}Client/edit", client);
//                //if (result != null)
//                //{
//                //    var kak = await result.Content.ReadAsStringAsync();
//                //    var morekak = JsonConvert.DeserializeObject(kak);

//                //    client = new ClientModel();
//                //    await OnHideModalClick();
//                //}
//            }

//        }

//        private async Task GetClients()
//        {
//            clients = await clientService.GetClients();
//        }

//        private async Task DeleteClient(AeroMech.Models.ClientModel client)
//        {
//            await clientService.Delete(client.Id);
//            clients?.Remove(client);
//        }
//    }
//}