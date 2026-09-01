using AeroMech.Data.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    public class UserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditService _auditService;

        public UserService(
            UserManager<IdentityUser> userManager,
            AuditService auditService)
        {
            _userManager = userManager;
            _auditService = auditService;
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

            var result = await _userManager.CreateAsync(user, password);

            // Identity owns its own save, so there is no transaction here to join and the entry is
            // written on a context of its own once the account exists. Only a successful attempt is
            // recorded: a rejected one changed nothing.
            if (result.Succeeded)
            {
                await _auditService.RecordAsync(
                    await _auditService.ResolveUser(),
                    AuditArea.Users,
                    AuditAction.Created,
                    nameof(IdentityUser),
                    null,
                    user.UserName,
                    $"User account created for {user.UserName} ({user.Email}).");
            }

            return result;
        }

        public async Task<IdentityResult> DeleteUser(IdentityUser user)
        {
            var result = await _userManager.DeleteAsync(user);

            // Who can reach the system is what an audit trail is asked about first when something
            // is found to have gone missing, and a removed account leaves nothing else behind to
            // answer with.
            if (result.Succeeded)
            {
                await _auditService.RecordAsync(
                    await _auditService.ResolveUser(),
                    AuditArea.Users,
                    AuditAction.Deleted,
                    nameof(IdentityUser),
                    null,
                    user?.UserName,
                    $"User account removed for {user?.UserName} ({user?.Email}).");
            }

            return result;
        }

        public async Task<List<IdentityUser>> GetUsers()
        {
            return await _userManager.Users.AsNoTracking().ToListAsync();
        }
    }
}
