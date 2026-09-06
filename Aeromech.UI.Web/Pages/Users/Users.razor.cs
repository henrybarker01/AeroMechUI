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
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _modalErrorMessage = string.Empty;
        private bool _mustChangePassword;
        private bool _isEdit;
        private string _editingUserId = string.Empty;

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
            _title = L["Add User"];
            _isEdit = false;
            _editingUserId = string.Empty;
            _user = new IdentityUser();
            _password = string.Empty;
            _confirmPassword = string.Empty;
            _modalErrorMessage = string.Empty;

            // A password typed by whoever created the account is not the owner's own; the default
            // is to make them choose one the first time they sign in.
            _mustChangePassword = true;

            await _modal.ShowAsync();
        }

        private async Task SaveUser()
        {
            _modalErrorMessage = string.Empty;

            var passwordProvided = !string.IsNullOrWhiteSpace(_password);

            if (!_isEdit && !passwordProvided)
            {
                _modalErrorMessage = L["Password is required."];
                return;
            }

            if (passwordProvided && !string.Equals(_password, _confirmPassword, StringComparison.Ordinal))
            {
                _modalErrorMessage = L["Passwords do not match."];
                return;
            }

            _loaderService.ShowLoader();

            try
            {
                var result = _isEdit
                    ? await _userService.UpdateUser(
                        _editingUserId,
                        _user.Email ?? string.Empty,
                        _user.UserName ?? string.Empty,
                        _user.PhoneNumber,
                        passwordProvided ? _password : null,
                        _mustChangePassword)
                    : await CreateUser();

                if (result.Succeeded)
                {
                    await OnHideModalClick();
                    return;
                }

                _modalErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        private async Task<IdentityResult> CreateUser()
        {
            _user.EmailConfirmed = true;
            _user.LockoutEnabled = true;
            _user.PhoneNumberConfirmed = true;
            _user.TwoFactorEnabled = false;

            return await _userService.CreateUser(_user, _password, _mustChangePassword);
        }

        private async Task DeleteUser(IdentityUser user)
        {
            bool confirmed = await _confirmationService.ConfirmAsync(L["Are you sure?"]);
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
            _title = L["Edit User"];
            _isEdit = true;
            _editingUserId = user.Id;

            // A copy rather than the row the grid holds, so typing into the form and then
            // closing it changes nothing on screen.
            _user = new IdentityUser
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber
            };

            _password = string.Empty;
            _confirmPassword = string.Empty;
            _modalErrorMessage = string.Empty;
            _mustChangePassword = await _userService.MustChangePassword(user);

            await _modal.ShowAsync();
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