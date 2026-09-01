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

            // Configure TimesheetGapTypes enum to be stored as string in database
            modelBuilder.Entity<TimesheetEmployeeDetail>()
                .Property(e => e.Description)
                .HasConversion<string>();

            // A quote is converted into at most one service report, and the report keeps the
            // pointer back so the quote it came from can always be found.
            modelBuilder.Entity<ServiceReport>()
                .HasOne(x => x.Quote)
                .WithOne(x => x.ServiceReport)
                .HasForeignKey<ServiceReport>(x => x.QuoteId)
                .OnDelete(DeleteBehavior.SetNull);

            // Two suppliers can raise the same invoice number, but one supplier raising it twice
            // means the same stock was received twice - the mistake this feature most needs to
            // make visible.
            modelBuilder.Entity<StockReceipt>()
                .HasIndex(x => new { x.SupplierCode, x.InvoiceNumber });

            // A receipt is the reason its lines exist, so they go when it goes. The ledger rows
            // it wrote are deliberately kept: stock that moved stays on the record even if the
            // paperwork behind it is removed.
            modelBuilder.Entity<StockReceiptLine>()
                .HasOne(x => x.StockReceipt)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.StockReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StockAdjustment>()
                .HasOne(x => x.StockReceipt)
                .WithMany()
                .HasForeignKey(x => x.StockReceiptId)
                .OnDelete(DeleteBehavior.SetNull);

            // The number written on the printed count sheet, and how a sheet handed back on paper
            // is found again. Unique because two sheets answering to one number would make that
            // lookup ambiguous exactly when it matters.
            modelBuilder.Entity<StockTake>()
                .HasIndex(x => x.StockTakeNumber)
                .IsUnique();

            // A count sheet is the reason its lines exist, so they go when it goes.
            modelBuilder.Entity<StockTakeParts>()
                .HasOne(x => x.StockTake)
                .WithMany(x => x.StockTakeParts)
                .HasForeignKey(x => x.StockTakeId)
                .OnDelete(DeleteBehavior.Cascade);

            // As with receipts, the ledger rows a stock take wrote outlive the sheet: stock that
            // moved stays on the record even where the paperwork behind it is removed.
            modelBuilder.Entity<StockAdjustment>()
                .HasOne(x => x.StockTake)
                .WithMany()
                .HasForeignKey(x => x.StockTakeId)
                .OnDelete(DeleteBehavior.SetNull);

            // The audit log is read in three ways and indexed for each: a period ("what happened
            // last month"), a person ("what has this user been doing"), and a subject ("every
            // price change"). Newest first in each, because that is the order it is asked for.
            modelBuilder.Entity<AuditLog>()
                .HasIndex(x => x.OccurredAt)
                .IsDescending();

            modelBuilder.Entity<AuditLog>()
                .HasIndex(x => new { x.UserName, x.OccurredAt })
                .IsDescending(false, true);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(x => new { x.Area, x.OccurredAt })
                .IsDescending(false, true);

            // The entity an entry is about, for pulling the history of one part or one invoice.
            modelBuilder.Entity<AuditLog>()
                .HasIndex(x => new { x.EntityType, x.EntityId });
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
        public DbSet<StockReceipt> StockReceipts { get; set; }
        public DbSet<StockReceiptLine> StockReceiptLines { get; set; }
        public DbSet<StockTake> StockTakes { get; set; }
        public DbSet<StockTakeParts> StockTakeParts { get; set; }
        public DbSet<TimesheetEmployeeDetail> TimesheetEmployeeDetails { get; set; }
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<QuoteLabour> QuoteLabour { get; set; }
        public DbSet<QuotePart> QuoteParts { get; set; }
        public DbSet<QuoteAdHockPart> QuoteAdHockParts { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
    }
}
