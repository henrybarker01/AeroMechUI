using AeroMech.Models.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using AeroMech.UI.Web.Services;

namespace AeroMech.UI.Web.Pages.Login
{
    public partial class Login
    {
        [Inject] NavigationManager _navigationManager { get; set; }
        [Inject] protected LoaderService _loaderService { get; set; }
        [Inject] IJSRuntime JSRuntime { get; set; }

        private Credential _credential = new Credential();
        private string _errorMessage = string.Empty;

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

        private async Task HandleSubmit()
        {
            _loaderService.ShowLoader();
            _errorMessage = string.Empty;

            try
            {
                var result = await JSRuntime.InvokeAsync<SignInResult>(
                    "aeroMechAuth.signIn",
                    _credential.UserName,
                    _credential.Password);

                if (result?.Success == true)
                {
                    _navigationManager.NavigateTo(result.RedirectUrl ?? "/", forceLoad: true);
                    return;
                }

                _errorMessage = string.IsNullOrWhiteSpace(result?.Message)
                    ? "Invalid username or password."
                    : result!.Message;
            }
            catch (Exception ex)
            {
                _errorMessage = "An error occurred during login.";
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        private sealed class SignInResult
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
            public string? RedirectUrl { get; set; }
        }
    }
}
