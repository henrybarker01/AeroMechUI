using AeroMech.Data.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Extentions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication MigrateDatabase(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AeroMechDBContext>();
                db.Database.Migrate();
            }

            return app;
        }

        /// <summary>
        /// Seeds an initial user from the "SeedUser" configuration section (SeedUser__UserName,
        /// SeedUser__Email, SeedUser__Password). Only runs when all three values are set and the
        /// database contains no users, so it is a no-op in environments without this configuration.
        /// </summary>
        public static WebApplication SeedInitialUser(this WebApplication app)
        {
            var userName = app.Configuration["SeedUser:UserName"];
            var email = app.Configuration["SeedUser:Email"];
            var password = app.Configuration["SeedUser:Password"];

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return app;
            }

            using (var scope = app.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                var logger = app.Logger;

                if (userManager.Users.Any())
                {
                    logger.LogInformation("SeedUser: users already exist, skipping initial user seeding.");
                    return app;
                }

                var user = new IdentityUser
                {
                    UserName = userName.Trim(),
                    Email = email.Trim(),
                    EmailConfirmed = true
                };

                var result = userManager.CreateAsync(user, password).GetAwaiter().GetResult();
                if (result.Succeeded)
                {
                    logger.LogInformation("SeedUser: created initial user {UserName}.", user.UserName);
                }
                else
                {
                    logger.LogError("SeedUser: failed to create initial user {UserName}: {Errors}",
                        user.UserName, string.Join("; ", result.Errors.Select(e => e.Description)));
                }
            }

            return app;
        }
    }
}
