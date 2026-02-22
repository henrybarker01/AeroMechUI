using AeroMech.Database.DataMigration;
using AeroMech.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var serviceProvider = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    })
    .AddSingleton(configuration.GetSection("Migration").Get<MigrationOptions>() ?? new MigrationOptions())
    .AddSingleton<SourceDbContext>(sp =>
    {
        var optionsBuilder = new DbContextOptionsBuilder<AeroMechDBContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("SourceConnection"));
        return new SourceDbContext(optionsBuilder.Options);
    })
    .AddSingleton<TargetDbContext>(sp =>
    {
        var optionsBuilder = new DbContextOptionsBuilder<AeroMechDBContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("TargetConnection"));
        return new TargetDbContext(optionsBuilder.Options);
    })
    .AddTransient<DataMigrationService>()
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var migrationOptions = serviceProvider.GetRequiredService<MigrationOptions>();

try
{
    logger.LogInformation("=== AeroMech Database Migration Tool ===");
    logger.LogInformation("Source: MS SQL Server");
    logger.LogInformation("Target: PostgreSQL");
    logger.LogInformation("BatchSize: {BatchSize}", migrationOptions.BatchSize);
    logger.LogInformation("Migrate Identity tables: {MigrateIdentity}", !migrationOptions.SkipIdentityTables);
    logger.LogInformation("");

    var migrationService = serviceProvider.GetRequiredService<DataMigrationService>();

    logger.LogInformation("Step 1: Testing database connections...");
    var connectionsOk = await migrationService.TestConnectionsAsync();
    
    if (!connectionsOk)
    {
        logger.LogError("Connection test failed. Please check your connection strings in appsettings.json");
        return;
    }

    logger.LogInformation("");
    logger.LogInformation("Step 2: Checking current record counts...");
    var countsBefore = await migrationService.GetRecordCountsAsync();
    
    Console.WriteLine("");
    Console.WriteLine("Current Record Counts:");
    Console.WriteLine("-----------------------------------------------------");
    Console.WriteLine($"{"Table",-30} {"Source",-10} {"Target",-10}");
    Console.WriteLine("-----------------------------------------------------");
    
    foreach (var (table, (source, target)) in countsBefore)
    {
        Console.WriteLine($"{table,-30} {source,-10} {target,-10}");
    }
    
    Console.WriteLine("-----------------------------------------------------");
    Console.WriteLine("");

    Console.Write("Do you want to proceed with the migration? (yes/no): ");
    var response = Console.ReadLine()?.Trim().ToLower();

    if (response != "yes" && response != "y")
    {
        logger.LogInformation("Migration cancelled by user.");
        return;
    }

    logger.LogInformation("");
    logger.LogInformation("Step 3: Starting migration...");
    await migrationService.MigrateAllAsync();

    logger.LogInformation("");
    logger.LogInformation("Step 4: Verifying migration results...");
    var countsAfter = await migrationService.GetRecordCountsAsync();

    Console.WriteLine("");
    Console.WriteLine("Migration Results:");
    Console.WriteLine("-----------------------------------------------------");
    Console.WriteLine($"{"Table",-30} {"Source",-10} {"Target",-10} {"Status",-10}");
    Console.WriteLine("-----------------------------------------------------");
    
    foreach (var (table, (source, target)) in countsAfter)
    {
        var status = source == target ? "✓ OK" : "⚠ MISMATCH";
        Console.WriteLine($"{table,-30} {source,-10} {target,-10} {status,-10}");
    }
    
    Console.WriteLine("-----------------------------------------------------");
    
    logger.LogInformation("");
    logger.LogInformation("Migration completed! Press any key to exit...");
}
catch (Exception ex)
{
    logger.LogError(ex, "An error occurred during migration: {Message}", ex.Message);
}

Console.ReadKey();
