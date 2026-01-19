using AeroMech.Data.Persistence;
using AeroMech.Models.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    public class UserService
    {
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;

        public UserService(
            IDbContextFactory<AeroMechDBContext> contextFactory,
            IUserStore<IdentityUser> userStore,
            UserManager<IdentityUser> userManager)
        {
            _contextFactory = contextFactory;
            _userStore = userStore;
            _userManager = userManager;
        }

        public async Task<IdentityResult> CreateUser(IdentityUser user)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            if (_aeroMechDBContext.Users.Any(x => x.Email == user.Email))
            {
                return IdentityResult.Failed(
                    new IdentityError()
                    {
                        Code = "1",
                        Description = "User already registered"
                    });
            }
            else
            {
                var result = await _userStore.CreateAsync(user, CancellationToken.None);
                if (result.Succeeded)
                {
                    var addedUser = _aeroMechDBContext.Users.Single(x => x.Email == user.Email);
                    await _userManager.AddPasswordAsync(addedUser, "P@ssw0rd");
                }
                return result;
            }
        }

        public async Task<IdentityResult> DeleteUser(IdentityUser user)
        {
            return await _userStore.DeleteAsync(user, CancellationToken.None);
        }

        public async Task<List<IdentityUser>> GetUsers()
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            return await _aeroMechDBContext.Users.ToListAsync();
        }
    }
}
