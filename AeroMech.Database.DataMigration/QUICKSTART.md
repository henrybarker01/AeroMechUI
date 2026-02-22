# Quick Start Guide for Data Migration

## Overview
The migration tool is ready to migrate your data from MS SQL Server to PostgreSQL.

## Files Created
- ? `Program.cs` - Main console application
- ? `DataMigrationService.cs` - Core migration logic
- ? `SourceDbContext.cs` - MS SQL connection
- ? `TargetDbContext.cs` - PostgreSQL connection  
- ? `MigrationOptions.cs` - Configuration settings
- ? `appsettings.json` - Connection strings
- ? `README.md` - Full documentation

## Before You Start

### 1. Update Connection Strings
Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "SourceConnection": "Server=YOUR_SERVER;Database=YOUR_DB;User Id=user;Password=pass;TrustServerCertificate=True;",
    "TargetConnection": "Host=localhost;Port=5432;Database=aeromechdb;Username=postgres;Password=P@ssw0rd;Include Error Detail=true"
  }
}
```

### 2. Prepare PostgreSQL Database
The PostgreSQL database must have the schema created first:

```bash
# Navigate to your web project
cd ../Aeromech.UI.Web

# Apply EF migrations to PostgreSQL
dotnet ef database update
```

This creates all tables, indexes, and constraints in PostgreSQL.

### 3. (Optional) Clear Existing Data
If you need a fresh migration:

```sql
-- In PostgreSQL, run:
TRUNCATE TABLE "StockAdjustment", "StockTakeParts", "StockTakes", 
                "ServiceReportAdHockPart", "ServiceReportParts", "ServiceReportEmployees", 
                "ServiceReports", "PartPrices", "ClientRate", "Parts", "Vehicles", 
                "Employees", "Clients", "Warehouse", "Addresss" CASCADE;
```

## Running the Migration

```bash
cd AeroMech.Database.DataMigration
dotnet run
```

The tool will:
1. Test both database connections
2. Show current record counts
3. Ask for confirmation
4. Migrate all data in batches
5. Verify the results

## What Gets Migrated

- ? Addresses
- ? Warehouses
- ? Clients (with addresses and rates)
- ? Employees
- ? Vehicles
- ? Parts
- ? Client Rates
- ? Part Prices
- ? Service Reports (with employees, parts, and ad-hoc parts)
- ? Stock Takes (with parts)
- ? Stock Adjustments

## What's NOT Migrated

- ? ASP.NET Identity tables (Users, Roles, etc.)

You'll need to recreate users in PostgreSQL or manually migrate them separately.

## Troubleshooting

### "Connection failed"
- Verify SQL Server is accessible
- Check firewall rules
- Ensure PostgreSQL is running

### "Foreign key constraint violation"
- Make sure you ran `dotnet ef database update` on PostgreSQL first
- Check that the migration order in the code is correct

### "Record count mismatch"
- Some records might have IsDeleted = true in source
- Check logs for errors during migration

## Next Steps

After successful migration:
1. ? Verify data in PostgreSQL
2. ?? Update main app connection string to PostgreSQL
3. ?? Test your application thoroughly
4. ?? Backup MS SQL database before decommissioning

For detailed documentation, see `README.md`.
