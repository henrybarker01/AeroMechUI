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
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {        
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Address> Addresss { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<ClientRate> ClientRate { get; set; }
        public DbSet<PartPrice> PartPrices { get; set; }
        public DbSet<Part> Parts { get; set; }
        public DbSet<ServiceReportEmployee> ServiceReportEmployees { get; set; }
        public DbSet<ServiceReportPart> ServiceReportParts { get; set; }
        public DbSet<ServiceReport> ServiceReports { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Warehouse> Warehouse { get; set; }
        public DbSet<ServiceReportAdHockPart> ServiceReportAdHockPart { get; set; }
        public DbSet<StockAdjustment> StockAdjustment { get; set; }
        public DbSet<StockTake> StockTakes { get; set; }
        public DbSet<StockTakeParts> StockTakeParts { get; set; }
    }
}
