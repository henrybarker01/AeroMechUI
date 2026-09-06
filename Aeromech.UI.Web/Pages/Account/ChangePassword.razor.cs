using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AeroMech.UI.Web.Pages.Account
{
    /// <summary>
    /// Where anybody signed in changes their own password, and where an account with an assigned
    /// password is sent before it may go anywhere else. The change itself travels over HTTP (see
    /// <see cref="ChangePasswordModel"/>) so the authentication cookie can be reissued; this page
    /// only gathers the two passwords and shows what came back.
    /// </summary>
    public partial class ChangePassword
    {
        [Inject] private NavigationManager _navigationManager { get; set; } = default!;
        [Inject] private LoaderService _loaderService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

        [SupplyParameterFromQuery(Name = "forced")]
        public int? ForcedFromQuery { get; set; }

        protected int Forced => ForcedFromQuery ?? 0;

        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmNewPassword = string.Empty;
        private string _errorMessage = string.Empty;

        private async Task ChangePasswordAsync()
        {
            _errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(_currentPassword) || string.IsNullOrWhiteSpace(_newPassword))
            {
                _errorMessage = "Both the current and the new password are required.";
                return;
            }

            if (!string.Equals(_newPassword, _confirmNewPassword, StringComparison.Ordinal))
            {
                _errorMessage = "The new passwords do not match.";
                return;
            }

            if (string.Equals(_newPassword, _currentPassword, StringComparison.Ordinal))
            {
                _errorMessage = "The new password is the same as the current one.";
                return;
            }

            _loaderService.ShowLoader();

            try
            {
                var result = await JSRuntime.InvokeAsync<ChangePasswordResult>(
                    "aeroMechAuth.changePassword",
                    _currentPassword,
                    _newPassword);

                if (result?.Success == true)
                {
                    // A full load, so the circuit restarts against the reissued cookie and any
                    // forced-change guard re-reads a flag that is now gone.
                    _navigationManager.NavigateTo("/", forceLoad: true);
                    return;
                }

                _errorMessage = string.IsNullOrWhiteSpace(result?.Message)
                    ? "The password could not be changed."
                    : result!.Message!;
            }
            catch (Exception)
            {
                _errorMessage = "An error occurred while changing the password.";
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }

        private sealed class ChangePasswordResult
        {
            public bool Success { get; set; }
            public string? Message { get; set; }
        }
    }
}
