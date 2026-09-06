using AeroMech.Data.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Web;

namespace AeroMech.UI.Web.Services
{
    public class UserService
    {
        /// <summary>
        /// Marks an account whose password was set for its owner rather than by them. Held as an
        /// identity claim rather than a column of our own so no schema change rides on it; the
        /// sign-in path reads it and refuses to go anywhere but the change-password screen.
        /// </summary>
        public const string MustChangePasswordClaimType = "aeromech:must-change-password";

        /// <summary>
        /// Claim value: an administrator typed the account's current password, so its owner does
        /// not yet have one of their own. Older rows carry "true", which meant the same thing.
        /// </summary>
        public const string MustChangeReasonAssigned = "assigned";

        /// <summary>
        /// Claim value: the password was left alone; its owner still knows it, they are just
        /// required to pick a new one before carrying on.
        /// </summary>
        public const string MustChangeReasonForced = "forced";

        private readonly UserManager<IdentityUser> _userManager;
        private readonly AuditService _auditService;
        private readonly EmailService _emailService;

        public UserService(
            UserManager<IdentityUser> userManager,
            AuditService auditService,
            EmailService emailService)
        {
            _userManager = userManager;
            _auditService = auditService;
            _emailService = emailService;
        }

        public async Task<IdentityResult> CreateUser(IdentityUser user, string password, bool mustChangePassword)
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
                if (mustChangePassword)
                {
                    await _userManager.AddClaimAsync(user, new Claim(MustChangePasswordClaimType, MustChangeReasonAssigned));
                }

                await _auditService.RecordAsync(
                    await _auditService.ResolveUser(),
                    AuditArea.Users,
                    AuditAction.Created,
                    nameof(IdentityUser),
                    null,
                    user.UserName,
                    $"User account created for {user.UserName} ({user.Email})."
                        + (mustChangePassword ? " They must choose their own password at first sign-in." : string.Empty));
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

        /// <summary>
        /// Whether an account is flagged to choose its own password before doing anything else.
        /// Read from the store rather than the cookie, so a flag an administrator set five minutes
        /// ago is seen even by a session signed in before it existed.
        /// </summary>
        public async Task<bool> MustChangePassword(IdentityUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);

            return claims.Any(c => c.Type == MustChangePasswordClaimType);
        }

        /// <summary>
        /// Why an account must change its password: <see cref="MustChangeReasonAssigned"/>,
        /// <see cref="MustChangeReasonForced"/>, "true" on rows from before the reasons existed,
        /// or null when no change is required.
        /// </summary>
        public async Task<string?> MustChangePasswordReason(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);

            return user is null ? null : await MustChangePasswordReason(user);
        }

        public async Task<string?> MustChangePasswordReason(IdentityUser user)
        {
            var claims = await _userManager.GetClaimsAsync(user);

            return claims.FirstOrDefault(c => c.Type == MustChangePasswordClaimType)?.Value;
        }

        /// <summary>
        /// Where an account carrying the must-change flag is sent, worded for why it carries it:
        /// "own=1" when the password was left alone, absent when one was assigned (or the flag
        /// predates the reasons, which was only ever written for an assigned password).
        /// </summary>
        public static string ChangePasswordRedirect(string? reason)
            => reason == MustChangeReasonForced
                ? "/change-password?forced=1&own=1"
                : "/change-password?forced=1";

        public async Task SetMustChangePassword(IdentityUser user, bool mustChange, string reason = MustChangeReasonAssigned)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var existing = claims.Where(c => c.Type == MustChangePasswordClaimType).ToList();

            if (mustChange && existing.Count == 0)
            {
                await _userManager.AddClaimAsync(user, new Claim(MustChangePasswordClaimType, reason));
            }
            else if (mustChange && existing[0].Value != reason)
            {
                await _userManager.ReplaceClaimAsync(user, existing[0], new Claim(MustChangePasswordClaimType, reason));
            }
            else if (!mustChange && existing.Count > 0)
            {
                await _userManager.RemoveClaimsAsync(user, existing);
            }
        }

        /// <summary>
        /// Updates the account details an administrator can see, and - when a new password was
        /// typed - replaces the password. Works on a fresh copy of the user rather than the row the
        /// grid holds, so a save that fails validation leaves nothing half-applied.
        /// </summary>
        public async Task<IdentityResult> UpdateUser(
            string userId,
            string email,
            string userName,
            string? phoneNumber,
            string? newPassword,
            bool mustChangePassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return IdentityResult.Failed(new IdentityError { Code = "NotFound", Description = "That user no longer exists." });
            }

            email = email?.Trim() ?? string.Empty;
            userName = userName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userName))
            {
                return IdentityResult.Failed(new IdentityError { Code = "InvalidInput", Description = "Email and UserName are required." });
            }

            var duplicate = await _userManager.FindByEmailAsync(email);
            if (duplicate is not null && duplicate.Id != user.Id)
            {
                return IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Another user is registered with that email." });
            }

            var changes = new List<string>();

            if (!string.Equals(user.UserName, userName, StringComparison.Ordinal))
                changes.Add($"user name {user.UserName} -> {userName}");

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                changes.Add($"email {user.Email} -> {email}");

            if (!string.Equals(user.PhoneNumber ?? string.Empty, phoneNumber?.Trim() ?? string.Empty, StringComparison.Ordinal))
                changes.Add("phone number changed");

            user.Email = email;
            user.UserName = userName;
            user.PhoneNumber = phoneNumber?.Trim();

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return result;
            }

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                // Reset through a token rather than remove-and-add: one operation, and the
                // security stamp moves with it, so sessions signed in under the old password
                // are revalidated out.
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, newPassword);
                if (!passwordResult.Succeeded)
                {
                    return passwordResult;
                }

                await _auditService.RecordAsync(
                    await _auditService.ResolveUser(),
                    AuditArea.Users,
                    AuditAction.PasswordChanged,
                    nameof(IdentityUser),
                    null,
                    user.UserName,
                    $"Password assigned to {user.UserName} by an administrator."
                        + (mustChangePassword ? " They must choose their own at next sign-in." : string.Empty));
            }

            var hadFlag = await MustChangePassword(user);

            if (!mustChangePassword)
            {
                await SetMustChangePassword(user, false);
            }
            else if (!hadFlag || !string.IsNullOrWhiteSpace(newPassword))
            {
                // A flag that was already standing keeps its reason unless a password was assigned
                // right now; re-saving the form must not turn "assigned" into "forced".
                await SetMustChangePassword(user, true,
                    string.IsNullOrWhiteSpace(newPassword) ? MustChangeReasonForced : MustChangeReasonAssigned);
            }

            if (changes.Count > 0 || hadFlag != mustChangePassword)
            {
                var flagNote = hadFlag == mustChangePassword
                    ? string.Empty
                    : mustChangePassword
                        ? " Must now change password at next sign-in."
                        : " No longer required to change password at sign-in.";

                await _auditService.RecordAsync(
                    await _auditService.ResolveUser(),
                    AuditArea.Users,
                    AuditAction.Updated,
                    nameof(IdentityUser),
                    null,
                    user.UserName,
                    changes.Count > 0
                        ? $"User account updated: {string.Join(", ", changes)}.{flagNote}"
                        : $"User account updated.{flagNote}");
            }

            return IdentityResult.Success;
        }

        /// <summary>
        /// Changes a password for the person who knows the current one. The must-change flag comes
        /// off here whatever set it: the account now has a password only its owner knows, which is
        /// the state the flag existed to reach.
        /// </summary>
        public async Task<IdentityResult> ChangeOwnPassword(IdentityUser user, string currentPassword, string newPassword)
        {
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (result.Succeeded)
            {
                await SetMustChangePassword(user, false);

                await _auditService.RecordAsync(
                    user.UserName ?? CurrentUserService.UnknownUser,
                    AuditArea.Users,
                    AuditAction.PasswordChanged,
                    nameof(IdentityUser),
                    null,
                    user.UserName,
                    $"{user.UserName} changed their own password.");
            }

            return result;
        }

        /// <summary>
        /// Emails a reset link to an address, if an account answers to it. The caller is told
        /// nothing about whether one did: the screen says the same thing either way, so the form
        /// cannot be used to test which addresses hold accounts.
        /// </summary>
        public async Task SendPasswordResetLink(string email, string baseUri)
        {
            email = email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                // Still worth an audit line: a run of these against unknown addresses is somebody
                // probing, and the log is where that is noticed.
                await _auditService.RecordAsync(
                    CurrentUserService.UnknownUser,
                    AuditArea.Users,
                    AuditAction.Updated,
                    nameof(IdentityUser),
                    null,
                    email,
                    $"A password reset link was requested for {email}, which matches no account. No email was sent.");

                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var link = $"{baseUri.TrimEnd('/')}/reset-password?email={HttpUtility.UrlEncode(user.Email)}&token={HttpUtility.UrlEncode(token)}";

            await _emailService.SendAsync(
                user.Email,
                "AeroMech password reset",
                $"""
                <p>Hello {HttpUtility.HtmlEncode(user.UserName)},</p>
                <p>A password reset was requested for your AeroMech account. Click the link below to choose a new password:</p>
                <p><a href="{link}">Reset your password</a></p>
                <p>If you did not ask for this, you can ignore this email - your password has not changed.</p>
                """);

            await _auditService.RecordAsync(
                user.UserName ?? CurrentUserService.UnknownUser,
                AuditArea.Users,
                AuditAction.Updated,
                nameof(IdentityUser),
                null,
                user.UserName,
                $"A password reset link was emailed to {user.Email}.");
        }

        /// <summary>
        /// Completes a reset begun by <see cref="SendPasswordResetLink"/>. The token proves the
        /// email was received, so a success also clears any must-change flag: the owner has just
        /// chosen their own password.
        /// </summary>
        public async Task<IdentityResult> ResetPassword(string email, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(email?.Trim() ?? string.Empty);
            if (user is null)
            {
                return IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "This reset link is not valid. Request a new one." });
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                await SetMustChangePassword(user, false);

                await _auditService.RecordAsync(
                    user.UserName ?? CurrentUserService.UnknownUser,
                    AuditArea.Users,
                    AuditAction.PasswordChanged,
                    nameof(IdentityUser),
                    null,
                    user.UserName,
                    $"{user.UserName} reset their password through an emailed link.");
            }

            return result;
        }
    }
}
