using AeroMech.Models.Models;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Login
{
    public partial class Login
    {
        [Inject] UserService _userService { get; set; }
        [Inject] NavigationManager _navigationManager { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }

        private Credential _credential = new Credential();

        protected override void OnInitialized()
        {
            if (_navigationManager.Uri.Contains("login?Return"))
            {
                try
                {
                    _navigationManager.NavigateTo("/", forceLoad: true);
                }
                catch (Exception ex)
                {
                }

            }


            base.OnInitialized();
        }
        private async void Authenticate()
        {
            _loaderService.ShowLoader();
            await _userService.LoginAsync(_credential);
            _loaderService.HideLoader();

        }

    }
}