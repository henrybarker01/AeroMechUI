using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    public class UserService
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UserService(
            UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IdentityResult> CreateUser(IdentityUser user, string password)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.Email = user.Email?.Trim();
            user.UserName = user.UserName?.Trim();

            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.UserName))
            {
                return IdentityResult.Failed(new IdentityError { Code = "InvalidInput", Description = "Email and UserName are required." });
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return IdentityResult.Failed(new IdentityError { Code = "InvalidPassword", Description = "Password is required." });
            }

            var existing = await _userManager.FindByEmailAsync(user.Email);
            if (existing is not null)
            {
                return IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "User already registered." });
            }

            return await _userManager.CreateAsync(user, password);
        }

        public async Task<IdentityResult> DeleteUser(IdentityUser user)
        {
            return await _userManager.DeleteAsync(user);
        }

        public async Task<List<IdentityUser>> GetUsers()
        {
            return await _userManager.Users.AsNoTracking().ToListAsync();
        }
    }
}
