using AeroMech.Data.Persistence;
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
    }
}
