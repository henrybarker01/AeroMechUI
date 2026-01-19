using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Extentions
{
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// Create a scope, resolve an IDbContextFactory for TContext, create the context and apply pending migrations.
        /// Throws on error after logging.
        /// </summary>
        public static WebApplication MigrateDatabase<TContext>(this WebApplication app) where TContext : DbContext
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetService<ILogger<TContext>>() ?? services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(TContext));

            try
            {
                var dbFactory = services.GetRequiredService<IDbContextFactory<TContext>>();
                using var context = dbFactory.CreateDbContext();
                context.Database.Migrate();
                logger.LogInformation("Database migrations applied for {Context}.", typeof(TContext).Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database for {Context}.", typeof(TContext).Name);
                throw;
            }

            return app;
        }
    }
}
