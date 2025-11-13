using AeroMech.Models;
using AeroMech.UI.Web.Services;
using BlazorBootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace AeroMech.UI.Web.Pages.Users
{
    public partial class Users
    {
        [Inject] private UserService _userService { get; set; }
        [Inject] private LoaderService _loaderService { get; set; }
        [Inject] private ConfirmationService _confirmationService { get; set; }

        private string _title = "";
        private Modal _modal = default!;
        private IdentityUser _user = new IdentityUser();
        private List<IdentityUser>? _users = new List<IdentityUser>();

        private bool MatchesSearch(IdentityUser user, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return true;
            var t = term.Trim();

            return
                (user.Email ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase) ||
                (user.UserName ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
                await GetUsers();
        }

        private async Task GetUsers()
        {
            _loaderService.ShowLoader();
            _users = await _userService.GetUsers();
            await InvokeAsync(StateHasChanged);
            _loaderService.HideLoader();
        }

        private async Task AddUserClick()
        {
            _title = "Add User";
            await _modal.ShowAsync();
        }

        private async void AddUser()
        {
            _user.EmailConfirmed = true;
            _user.LockoutEnabled = true;
            _user.PhoneNumberConfirmed = true;
            _user.TwoFactorEnabled = false;

            _loaderService.ShowLoader();
            var result = await _userService.CreateUser(_user);
            if (result.Succeeded)
            {
                await OnHideModalClick();
            }
            _loaderService.HideLoader();
        }

        private async Task DeleteUser(IdentityUser user)
        {
            bool confirmed = await _confirmationService.ConfirmAsync("Are you sure?");
            if (confirmed)
            {
                _loaderService.ShowLoader();
                await _userService.DeleteUser(user);
                _users?.Remove(user);
                _loaderService.HideLoader();
            }
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
            await _modal.HideAsync();
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