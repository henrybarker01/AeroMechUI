using AeroMech.Data.Persistence;
using AeroMech.Models.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly AeroMechDBContext _context;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            AeroMechDBContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _context = context;
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] Credential credential)
        {
            if (string.IsNullOrWhiteSpace(credential.UserName) || string.IsNullOrWhiteSpace(credential.Password))
            {
                return BadRequest(new { success = false, message = "Username and password are required." });
            }

            var user = await _userManager.FindByNameAsync(credential.UserName);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password." });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, credential.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { success = false, message = "Invalid username or password." });
            }

            // Sign in the user with a persistent cookie
            await _signInManager.SignInAsync(user, isPersistent: true);

            return Ok(new { success = true, message = "Login successful." });
        }

        [HttpPost("signout")]
        public async Task<IActionResult> SignOut()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { success = true, message = "Logout successful." });
        }
    }
}
