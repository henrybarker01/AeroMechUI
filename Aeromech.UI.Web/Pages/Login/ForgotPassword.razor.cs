using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Login
{
    /// <summary>
    /// Asks only for an email address and answers the same way whether or not an account holds
    /// it, so the form cannot be used to test which addresses have accounts. The email itself is
    /// sent - or deliberately not - by <see cref="UserService.SendPasswordResetLink"/>.
    /// </summary>
    public partial class ForgotPassword
    {
        [Inject] private UserService _userService { get; set; } = default!;
        [Inject] private NavigationManager _navigationManager { get; set; } = default!;
        [Inject] private LoaderService _loaderService { get; set; } = default!;
        [Inject] private ILogger<ForgotPassword> _logger { get; set; } = default!;

        private string _email = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _linkSent;

        private async Task SendLink()
        {
            _errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(_email))
            {
                _errorMessage = L["Enter the email address your account is registered with."];
                return;
            }

            _loaderService.ShowLoader();

            try
            {
                await _userService.SendPasswordResetLink(_email, _navigationManager.BaseUri);
                _linkSent = true;
            }
            catch (InvalidOperationException ex)
            {
                // The one failure worth naming: the system has no mail server configured, which
                // is an administrator's problem and not something re-typing the address will fix.
                _errorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not send a password reset link.");
                _errorMessage = L["The reset link could not be sent. Try again, or contact your administrator."];
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }
    }
}
