using AeroMech.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.Database.DataMigration
{
    public class TargetDbContext : AeroMechDBContext
    {
        public TargetDbContext(DbContextOptions<AeroMechDBContext> options) : base(options)
        {
        }
    }
}
