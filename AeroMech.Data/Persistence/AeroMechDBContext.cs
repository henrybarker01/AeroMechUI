using AeroMech.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.Data.Persistence
{
    public class AeroMechDBContext : IdentityDbContext
    {
        public AeroMechDBContext()
        {
        }

        public AeroMechDBContext(DbContextOptions<AeroMechDBContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
#if DEBUG
            var connection = "Server=localhost;Database=AeroMech;User Id=sa;password=P@ssw0rd;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";
            optionsBuilder.UseSqlServer(connection, b => b.MigrationsAssembly("AeroMech.Data"));
#endif


            //optionsBuilder.UseSqlServer("Server=tcp:aeromech-database-server.database.windows.net,1433;Initial Catalog=aeromech;Persist Security Info=False;User ID=aeromech@henrybarker11gmail.onmicrosoft.com;Password=VMIDatabaseP@ssw0rd;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Authentication=\"Active Directory Password\";");
            //optionsBuilder.UseSqlServer();

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Database.Migrate();

            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Address> Addresss { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeRate> EmployeeRates { get; set; }
        public DbSet<PartPrice> PartPrices { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<ServiceReportEmployee> ServiceReportEmployees { get; set; }
        public DbSet<ServiceReportPart> ServiceReportParts { get; set; }
        public DbSet<ServiceReport> ServiceReports { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Warehouse> Warehouse { get; set; }

    }
}
