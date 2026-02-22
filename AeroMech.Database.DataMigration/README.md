# AeroMech Database Migration Tool

This console application migrates data from MS SQL Server to PostgreSQL.

## Prerequisites

1. **Source Database**: MS SQL Server with existing AeroMech data
2. **Target Database**: PostgreSQL with schema already created (run EF migrations first)
3. Connection access to both databases

## Setup

### 1. Configure Connection Strings

Edit `appsettings.json` and update the connection strings:

```json
{
  "ConnectionStrings": {
    "SourceConnection": "Server=YOUR_MSSQL_SERVER;Database=YOUR_SOURCE_DB;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;",
    "TargetConnection": "Host=localhost;Port=5432;Database=aeromechdb;Username=postgres;Password=P@ssw0rd;Include Error Detail=true"
  }
}
```

### 2. Prepare Target Database

**IMPORTANT**: Run EF migrations on the PostgreSQL database FIRST to create the schema:

```bash
cd ../Aeromech.UI.Web
dotnet ef database update
```

This creates all tables with the correct structure in PostgreSQL.

### 3. Clear Target Data (Optional)

If you need a clean migration, clear existing data from PostgreSQL first:

```sql
-- Connect to PostgreSQL and run:
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

1. ? Test connections to both databases
2. ?? Display current record counts
3. ?? Ask for confirmation before proceeding
4. ?? Migrate data in batches (respecting foreign key dependencies)
5. ? Verify migration results

## Migration Order

Data is migrated in this order to respect foreign key relationships:

1. Addresses
2. Warehouses
3. Clients
4. Employees
5. Vehicles
6. Parts
7. Client Rates
8. Part Prices
9. Service Reports
10. Service Report Employees
11. Service Report Parts
12. Service Report Ad-Hock Parts
13. Stock Takes
14. Stock Take Parts
15. Stock Adjustments

## Options

Edit `appsettings.json` to customize:

```json
{
  "Migration": {
    "BatchSize": 1000,           // Records per batch
    "SkipIdentityTables": true   // Skip ASP.NET Identity tables
  }
}
```

## Identity Tables

The migration **does not** migrate ASP.NET Identity tables (users, roles, etc.) by default. 

If you need to migrate users:
- Set `SkipIdentityTables` to `false`
- Manually add migration logic for Identity tables in `DataMigrationService.cs`

## Troubleshooting

### Connection Issues

- Verify connection strings are correct
- Ensure SQL Server allows remote connections
- Check PostgreSQL is running and accessible
- Verify firewall rules

### Foreign Key Violations

- Make sure target database schema is up-to-date
- Ensure migration order respects dependencies
- Check for orphaned records in source database

### Performance

- Adjust `BatchSize` for better performance (default: 1000)
- Consider temporarily disabling indexes on target database
- Run during off-peak hours for large datasets

### Record Count Mismatches

- Check source database for soft-deleted records (IsDeleted = true)
- Verify no records were created during migration
- Look for constraint violations in logs

## Post-Migration

1. ? Verify record counts match
2. ?? Spot-check critical data
3. ?? Run application tests
4. ?? Update application connection strings to PostgreSQL
5. ??? Backup MS SQL database before decommissioning

## Rollback

If migration fails or has issues:

1. Clear PostgreSQL data (see step 3 above)
2. Fix any issues
3. Re-run the migration tool

## Support

For issues or questions, check the logs output by the console application.
