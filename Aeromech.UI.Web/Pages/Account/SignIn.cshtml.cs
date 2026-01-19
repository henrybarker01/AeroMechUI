using AeroMech.Data.Persistence;
using AeroMech.Models.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AeroMech.UI.Web.Pages.Account
{
    [IgnoreAntiforgeryToken]
    public class SignInModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly AeroMechDBContext _context;
        private readonly ILogger<SignInModel> _logger;

        public SignInModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            AeroMechDBContext context,
            ILogger<SignInModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public string UserName { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                Credential credential;
                
                // Try to read from JSON body first (for AJAX calls)
                if (Request.ContentType?.Contains("application/json") == true)
                {
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    _logger.LogInformation("SignIn request received (JSON). Body length: {Length}", body.Length);
                    credential = JsonSerializer.Deserialize<Credential>(body, new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    });
                }
                else
                {
                    // Form POST
                    _logger.LogInformation("SignIn request received (Form). UserName: {UserName}", UserName);
                    credential = new Credential { UserName = UserName, Password = Password };
                }

                if (credential == null || string.IsNullOrWhiteSpace(credential.UserName) || string.IsNullOrWhiteSpace(credential.Password))
                {
                    _logger.LogWarning("Invalid credential data received");
                    if (Request.ContentType?.Contains("application/json") == true)
                    {
                        return new JsonResult(new { success = false, message = "Username and password are required." }) 
                        { 
                            StatusCode = 400 
                        };
                    }
                    return RedirectToPage("/Login");
                }

                _logger.LogInformation("Attempting login for user: {UserName}", credential.UserName);

                // Check if any users exist, if not create default admin
                var hasUsers = await _context.Users.AnyAsync();
                if (!hasUsers)
                {
                    _logger.LogInformation("No users found, creating default administrator");
                    
                    var defaultUser = new IdentityUser()
                    {
                        NormalizedEmail = "AEMADMINISTRATOR@VMI.COM",
                        NormalizedUserName = "AEMADMINISTRATOR",
                        PhoneNumber = "1234567890",
                        UserName = "AEMAdministrator",
                        TwoFactorEnabled = false,
                        Email = "AEMAdministrator@vmi.com",
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                        LockoutEnabled = false,
                    };
                    
                    var createResult = await _userStore.CreateAsync(defaultUser, CancellationToken.None);
                    if (createResult.Succeeded)
                    {
                        var addedUser = await _context.Users.SingleAsync(x => x.Email == defaultUser.Email);
                        await _userManager.AddPasswordAsync(addedUser, "P@ssw0rd");
                        _logger.LogInformation("Default administrator created successfully");
                    }
                    else
                    {
                        _logger.LogError("Failed to create default administrator: {Errors}", 
                            string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    }
                    
                    credential.UserName = "AEMAdministrator";
                    credential.Password = "P@ssw0rd";
                }

                var user = await _userManager.FindByNameAsync(credential.UserName);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserName}", credential.UserName);
                    if (Request.ContentType?.Contains("application/json") == true)
                    {
                        return new JsonResult(new { success = false, message = "Invalid username or password." }) 
                        { 
                            StatusCode = 401 
                        };
                    }
                    return RedirectToPage("/Login");
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, credential.Password, lockoutOnFailure: false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Password check failed for user: {UserName}", credential.UserName);
                    if (Request.ContentType?.Contains("application/json") == true)
                    {
                        return new JsonResult(new { success = false, message = "Invalid username or password." }) 
                        { 
                            StatusCode = 401 
                        };
                    }
                    return RedirectToPage("/Login");
                }

                // Sign in the user with a persistent cookie
                _logger.LogInformation("Signing in user: {UserName}", credential.UserName);
                await _signInManager.SignInAsync(user, isPersistent: true);
                _logger.LogInformation("User signed in successfully: {UserName}. Redirecting to home.", credential.UserName);

                // Always redirect for proper cookie handling
                if (Request.ContentType?.Contains("application/json") == true)
                {
                    return new JsonResult(new { success = true, message = "Login successful.", redirectUrl = "/" });
                }
                
                return Redirect("/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during sign-in");
                if (Request.ContentType?.Contains("application/json") == true)
                {
                    return new JsonResult(new { success = false, message = "An error occurred during login." }) 
                    { 
                        StatusCode = 500 
                    };
                }
                return RedirectToPage("/Login");
            }
        }
    }
}
