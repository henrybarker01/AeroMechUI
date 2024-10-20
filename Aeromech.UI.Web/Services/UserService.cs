using AeroMech.Data.Persistence;
using AeroMech.Models.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AeroMech.UI.Web.Services
{
    public class UserService
    {
        private readonly AeroMechDBContext _aeroMechDBContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IHostEnvironmentAuthenticationStateProvider? _hostAuthentication;
       private readonly AuthenticationStateProvider? _authenticationStateProvider;



        public UserService(AeroMechDBContext context,
            IUserStore<IdentityUser> userStore,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IHostEnvironmentAuthenticationStateProvider? hostAuthentication,
            AuthenticationStateProvider? authenticationStateProvider)
        {
            _aeroMechDBContext = context;
            _userStore = userStore;
            _userManager = userManager;
            _signInManager = signInManager;
            _hostAuthentication = hostAuthentication;
            _authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<SignInResult> LoginAsync(Credential credential)
        {

            var hasUsers = await _aeroMechDBContext.Users.AnyAsync();
            if (!hasUsers)
            {
                var usr = new IdentityUser()
                {
                    NormalizedEmail = "AEMAdministrator@vmi.com",
                    NormalizedUserName = "AEMAdministrator",
                    PhoneNumber = "1234567890",
                    UserName = "AEMAdministrator",
                    TwoFactorEnabled = false,
                    Email = "AEMAdministrator@vmi.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    LockoutEnabled = false,

                };
                await CreateUser(usr);
                credential.UserName = "AEMAdministrator";
                credential.Password = "P@ssw0rd";
            }

            var user = await _userStore.FindByNameAsync(credential.UserName, CancellationToken.None);

            if (user == null)
            {
                //await HandleSigningInFailedAsync("Email or Password are not match");
                return new SignInResult();
            }


      
            SignInResult loginResult = await _signInManager!.CheckPasswordSignInAsync(user, credential.Password, false);
            if (loginResult.Succeeded == false)
            {
                //await HandleSigningInFailedAsync("Email or Password are not match");
                return loginResult;
            }
            if (loginResult.Succeeded)
            { 
               // await _signInManager.SignInAsync(user, true);
                 
                 ClaimsPrincipal principal = await _signInManager.CreateUserPrincipalAsync(user);
                _signInManager.Context.User = principal;
                _hostAuthentication!.SetAuthenticationState(
                    Task.FromResult(new AuthenticationState(principal)));
                return loginResult;
                // If you don't need doing anything without moving to next page, you can remove this.
                //AuthenticationState authState = await AuthenticationStateProvider!.GetAuthenticationStateAsync();

                //Navigation!.NavigateTo("/Pages/Edit");
            }
            return new SignInResult();
        }

        public async Task<IdentityResult> CreateUser(IdentityUser user)
        {
            if (_aeroMechDBContext.Users.Any(x => x.Email == user.Email))
            {
                return IdentityResult.Failed(
                    new IdentityError()
                    {
                        Code = "1",
                        Description = "User aleady registered"
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
            return await _aeroMechDBContext.Users.ToListAsync();
        }
    }
}
