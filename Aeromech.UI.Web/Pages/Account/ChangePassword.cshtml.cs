using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace AeroMech.UI.Web.Pages.Account
{
    /// <summary>
    /// Changes the signed-in user's password over HTTP rather than inside the Blazor circuit,
    /// for the same reason sign-in does: the change moves the security stamp, and only an HTTP
    /// response can reissue the cookie to match. Without that, the next revalidation would sign
    /// the user out of the session in which they just proved who they are.
    /// </summary>
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class ChangePasswordModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly UserService _userService;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            UserService userService,
            ILogger<ChangePasswordModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userService = userService;
            _logger = logger;
        }

        private sealed class ChangePasswordRequest
        {
            public string? CurrentPassword { get; set; }
            public string? NewPassword { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                var request = JsonSerializer.Deserialize<ChangePasswordRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request is null
                    || string.IsNullOrWhiteSpace(request.CurrentPassword)
                    || string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return new JsonResult(new { success = false, message = "Both the current and the new password are required." })
                    {
                        StatusCode = 400
                    };
                }

                var user = await _userManager.GetUserAsync(User);
                if (user is null)
                {
                    return new JsonResult(new { success = false, message = "You are no longer signed in." })
                    {
                        StatusCode = 401
                    };
                }

                var result = await _userService.ChangeOwnPassword(user, request.CurrentPassword, request.NewPassword);

                if (!result.Succeeded)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = string.Join(" ", result.Errors.Select(e => e.Description))
                    })
                    {
                        StatusCode = 400
                    };
                }

                // Reissues the cookie against the new security stamp, and without the
                // must-change claim the old principal may still carry.
                await _signInManager.RefreshSignInAsync(user);

                return new JsonResult(new { success = true, message = "Password changed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while changing a password.");

                return new JsonResult(new { success = false, message = "An error occurred while changing the password." })
                {
                    StatusCode = 500
                };
            }
        }
    }
}
