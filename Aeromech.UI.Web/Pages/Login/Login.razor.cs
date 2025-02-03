using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Login
{
    public partial class Login
    {
        [Inject] UserService _userService { get; set; }
        [Inject] NavigationManager _navigationManager { get; set; }


        private Credential _credential = new Credential();

        private async void Authenticate()
        {
            await _userService.LoginAsync(_credential);
            //_navigationManager.NavigateTo("/", forceLoad: true);
        }

    }
}