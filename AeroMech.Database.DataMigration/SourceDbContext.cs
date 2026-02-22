using AeroMech.Data.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.Database.DataMigration
{
    public class SourceDbContext : AeroMechDBContext
    {
        public SourceDbContext(DbContextOptions<AeroMechDBContext> options) : base(options)
        {
        }
    }
}

