using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AeroMech.UI.Web.Pages.Login
{
    /// <summary>
    /// The far end of the emailed reset link. The email address and token ride in on the query
    /// string exactly as <see cref="UserService.SendPasswordResetLink"/> wrote them, and the token
    /// does the proving - this page never asks who somebody is, only what they want their new
    /// password to be.
    /// </summary>
    public partial class ResetPassword
    {
        [Inject] private UserService _userService { get; set; } = default!;
        [Inject] private LoaderService _loaderService { get; set; } = default!;
        [Inject] private ILogger<ResetPassword> _logger { get; set; } = default!;

        [SupplyParameterFromQuery(Name = "email")]
        public string? Email { get; set; }

        [SupplyParameterFromQuery(Name = "token")]
        public string? Token { get; set; }

        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _resetDone;
        private bool _linkInvalid;

        protected override void OnParametersSet()
        {
            _linkInvalid = string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Token);
        }

        private async Task ResetPasswordAsync()
        {
            _errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(_newPassword))
            {
                _errorMessage = L["Enter a new password."];
                return;
            }

            if (!string.Equals(_newPassword, _confirmPassword, StringComparison.Ordinal))
            {
                _errorMessage = L["The passwords do not match."];
                return;
            }

            _loaderService.ShowLoader();

            try
            {
                var result = await _userService.ResetPassword(Email!, Token!, _newPassword);

                if (result.Succeeded)
                {
                    _resetDone = true;
                    return;
                }

                // An expired or reused token comes back as "Invalid token", which means nothing
                // to somebody who just clicked a link in their inbox.
                if (result.Errors.Any(e => e.Code == "InvalidToken"))
                {
                    _linkInvalid = true;
                    return;
                }

                _errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not reset a password.");
                _errorMessage = L["The password could not be reset. Try again, or request a new link."];
            }
            finally
            {
                _loaderService.HideLoader();
            }
        }
    }
}
