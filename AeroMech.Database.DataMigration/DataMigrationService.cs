using AeroMech.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace AeroMech.Database.DataMigration
{
    public class DataMigrationService
    {
        private readonly SourceDbContext _sourceContext;
        private readonly TargetDbContext _targetContext;
        private readonly ILogger<DataMigrationService> _logger;
        private readonly MigrationOptions _options;

        public DataMigrationService(
            SourceDbContext sourceContext,
            TargetDbContext targetContext,
            ILogger<DataMigrationService> logger,
            MigrationOptions options)
        {
            _sourceContext = sourceContext;
            _targetContext = targetContext;
            _logger = logger;
            _options = options;
        }

        public async Task MigrateAllAsync()
        {
            _logger.LogInformation("Starting database migration from MS SQL to PostgreSQL...");

            try
            {
                await MigrateTableAsync<Address>("Addresses");
                await MigrateTableAsync<Warehouse>("Warehouses");
                await MigrateClientsAsync();
                await MigrateEmployeesAsync();
                await MigrateTableAsync<Vehicle>("Vehicles");
                await MigrateTableAsync<Part>("Parts");
                await MigrateClientRatesAsync();
                await MigratePartPricesAsync();
                await MigrateServiceReportsAsync();
                await MigrateTableAsync<ServiceReportEmployee>("Service Report Employees");
                await MigrateTableAsync<ServiceReportPart>("Service Report Parts");
                await MigrateTableAsync<ServiceReportAdHockPart>("Service Report Ad-Hock Parts");
                await MigrateStockTakesAsync();
                await MigrateTableAsync<StockTakeParts>("Stock Take Parts");
                await MigrateStockAdjustmentsAsync();

                if (!_options.SkipIdentityTables)
                {
                    await MigrateIdentityAsync();
                }
                else
                {
                    _logger.LogInformation("Skipping ASP.NET Identity migration (SkipIdentityTables=true). Set Migration:SkipIdentityTables=false in appsettings.json to migrate AspNetUsers and related tables.");
                }

                _logger.LogInformation("Database migration completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration failed with error: {Message}", ex.Message);
                throw;
            }
        }

        private async Task MigrateTableAsync<T>(string tableName) where T : class
        {
            _logger.LogInformation("Migrating {TableName}...", tableName);

            try
            {
                var sourceSet = _sourceContext.Set<T>();
                var targetSet = _targetContext.Set<T>();

                var totalRecords = await sourceSet.CountAsync();
                _logger.LogInformation("{TableName}: {TotalRecords} records found", tableName, totalRecords);

                if (totalRecords == 0)
                {
                    _logger.LogInformation("{TableName}: No records to migrate", tableName);
                    return;
                }

                var batchSize = _options.BatchSize;
                var processedRecords = 0;

                for (int skip = 0; skip < totalRecords; skip += batchSize)
                {
                    var batch = await sourceSet
                        .AsNoTracking()
                        .Skip(skip)
                        .Take(batchSize)
                        .ToListAsync();

                    await targetSet.AddRangeAsync(batch);
                    await _targetContext.SaveChangesAsync();

                    processedRecords += batch.Count;
                    _logger.LogInformation("{TableName}: Migrated {ProcessedRecords}/{TotalRecords} records", 
                        tableName, processedRecords, totalRecords);

                    _targetContext.ChangeTracker.Clear();
                }

                _logger.LogInformation("{TableName}: Migration completed successfully", tableName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate {TableName}: {Message}", tableName, ex.Message);
                throw;
            }
        }

        public async Task<bool> TestConnectionsAsync()
        {
            try
            {
                _logger.LogInformation("Testing source connection...");
                await _sourceContext.Database.CanConnectAsync();
                _logger.LogInformation("Source connection: OK");

                _logger.LogInformation("Testing target connection...");
                await _targetContext.Database.CanConnectAsync();
                _logger.LogInformation("Target connection: OK");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<Dictionary<string, (int Source, int Target)>> GetRecordCountsAsync()
        {
            var counts = new Dictionary<string, (int Source, int Target)>
            {
                ["Addresses"] = (await _sourceContext.Addresss.CountAsync(), await _targetContext.Addresss.CountAsync()),
                ["Warehouses"] = (await _sourceContext.Warehouse.CountAsync(), await _targetContext.Warehouse.CountAsync()),
                ["Clients"] = (await _sourceContext.Clients.CountAsync(), await _targetContext.Clients.CountAsync()),
                ["Employees"] = (await _sourceContext.Employees.CountAsync(), await _targetContext.Employees.CountAsync()),
                ["Vehicles"] = (await _sourceContext.Vehicles.CountAsync(), await _targetContext.Vehicles.CountAsync()),
                ["Parts"] = (await _sourceContext.Parts.CountAsync(), await _targetContext.Parts.CountAsync()),
                ["ClientRates"] = (await _sourceContext.ClientRate.CountAsync(), await _targetContext.ClientRate.CountAsync()),
                ["PartPrices"] = (await _sourceContext.PartPrices.CountAsync(), await _targetContext.PartPrices.CountAsync()),
                ["ServiceReports"] = (await _sourceContext.ServiceReports.CountAsync(), await _targetContext.ServiceReports.CountAsync()),
                ["ServiceReportEmployees"] = (await _sourceContext.ServiceReportEmployees.CountAsync(), await _targetContext.ServiceReportEmployees.CountAsync()),
                ["ServiceReportParts"] = (await _sourceContext.ServiceReportParts.CountAsync(), await _targetContext.ServiceReportParts.CountAsync()),
                ["ServiceReportAdHockParts"] = (await _sourceContext.ServiceReportAdHockPart.CountAsync(), await _targetContext.ServiceReportAdHockPart.CountAsync()),
                ["StockTakes"] = (await _sourceContext.StockTakes.CountAsync(), await _targetContext.StockTakes.CountAsync()),
                ["StockTakeParts"] = (await _sourceContext.StockTakeParts.CountAsync(), await _targetContext.StockTakeParts.CountAsync()),
                ["StockAdjustments"] = (await _sourceContext.StockAdjustment.CountAsync(), await _targetContext.StockAdjustment.CountAsync())
            };

            return counts;
        }

        private async Task MigrateClientsAsync()
        {
            _logger.LogInformation("Migrating Clients...");

            try
            {
                var clients = await _sourceContext.Database
                    .SqlQueryRaw<ClientDto>("SELECT Id, Name, ContactPersonName, ContactPersonNumber, ContactPersonEmail, ContactPersonBirthDate, IsDeleted, AddressId FROM Clients")
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Clients: {TotalRecords} records found", clients.Count);

                foreach (var dto in clients)
                {
                    var client = new Client
                    {
                        Id = dto.Id,
                        Name = dto.Name,
                        ContactPersonName = dto.ContactPersonName,
                        ContactPersonNumber = dto.ContactPersonNumber,
                        ContactPersonEmail = dto.ContactPersonEmail,
                        ContactPersonBirthDate = dto.ContactPersonBirthDate.HasValue ? DateOnly.FromDateTime(dto.ContactPersonBirthDate.Value) : null,
                        IsDeleted = dto.IsDeleted,
                        AddressId = dto.AddressId
                    };

                    _targetContext.Clients.Add(client);
                }

                await _targetContext.SaveChangesAsync();
                _targetContext.ChangeTracker.Clear();

                _logger.LogInformation("Clients: Migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate Clients: {Message}", ex.Message);
                throw;
            }
        }

        private async Task MigrateEmployeesAsync()
        {
            _logger.LogInformation("Migrating Employees...");

            try
            {
                var employees = await _sourceContext.Database
                    .SqlQueryRaw<EmployeeDto>("SELECT Id, IDNumber, Title, FirstName, LastName, PhoneNumber, Email, BirthDate, IsDeleted, AddressId FROM Employees")
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Employees: {TotalRecords} records found", employees.Count);

                foreach (var dto in employees)
                {
                    var employee = new Employee
                    {
                        Id = dto.Id,
                        IDNumber = dto.IDNumber,
                        Title = dto.Title,
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        PhoneNumber = dto.PhoneNumber,
                        Email = dto.Email,
                        BirthDate = dto.BirthDate.HasValue ? DateOnly.FromDateTime(dto.BirthDate.Value) : null,
                        IsDeleted = dto.IsDeleted,
                        AddressId = dto.AddressId
                    };

                    _targetContext.Employees.Add(employee);
                }

                await _targetContext.SaveChangesAsync();
                _targetContext.ChangeTracker.Clear();

                _logger.LogInformation("Employees: Migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate Employees: {Message}", ex.Message);
                throw;
            }
        }

        private async Task MigrateClientRatesAsync()
        {
            _logger.LogInformation("Migrating Client Rates...");

            try
            {
                var rates = await _sourceContext.Database
                    .SqlQueryRaw<ClientRateDto>("SELECT Id, EffectiveDate, Rate, ClientId, RateType, IsActive FROM ClientRate")
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Client Rates: {TotalRecords} records found", rates.Count);

                foreach (var dto in rates)
                {
                    var rate = new ClientRate
                    {
                        Id = dto.Id,
                        EffectiveDate = new DateTimeOffset(dto.EffectiveDate, TimeSpan.Zero),
                        Rate = dto.Rate,
                        ClientId = dto.ClientId,
                        RateType = dto.RateType,
                        IsActive = dto.IsActive
                    };

                    _targetContext.ClientRate.Add(rate);
                }

                await _targetContext.SaveChangesAsync();
                _targetContext.ChangeTracker.Clear();

                _logger.LogInformation("Client Rates: Migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate Client Rates: {Message}", ex.Message);
                throw;
            }
        }

        private async Task MigratePartPricesAsync()
        {
            _logger.LogInformation("Migrating Part Prices...");

            try
            {
                var prices = await _sourceContext.Database
                    .SqlQueryRaw<PartPriceDto>("SELECT Id, PartId, CostPrice, SellingPrice, EffectiveDate, IsDeleted FROM PartPrices")
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Part Prices: {TotalRecords} records found", prices.Count);

                foreach (var dto in prices)
                {
                    var price = new PartPrice
                    {
                        Id = dto.Id,
                        PartId = dto.PartId,
                        CostPrice = (double)dto.CostPrice,
                        SellingPrice = (double)dto.SellingPrice,
                        EffectiveDate = new DateTimeOffset(dto.EffectiveDate, TimeSpan.Zero),
                        IsDeleted = dto.IsDeleted
                    };

                    _targetContext.PartPrices.Add(price);
                }

                await _targetContext.SaveChangesAsync();
                _targetContext.ChangeTracker.Clear();

                _logger.LogInformation("Part Prices: Migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate Part Prices: {Message}", ex.Message);
                throw;
            }
        }

        private async Task MigrateServiceReportsAsync()
        {
            _logger.LogInformation("Migrating Service Reports...");

            try
            {
                var reports = await _sourceContext.Database
                    .SqlQueryRaw<ServiceReportDto>("SELECT Id, ReportDate, SalesOrderNumber, JobNumber, ServiceReportNumber, Description, ClientId, VehicleId, Instruction, DetailedServiceReport, IsDeleted, ServiceType, VehicleHours, QuoteNumber, IsComplete FROM ServiceReports")
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Service Reports: {TotalRecords} records found", reports.Count);

                foreach (var dto in reports)
                {
                    var report = new ServiceReport
                    {
                        Id = dto.Id,
                        ReportDate = new DateTimeOffset(dto.ReportDate, TimeSpan.Zero),
                        SalesOrderNumber = dto.SalesOrderNumber,
                        JobNumber = dto.JobNumber,
                        ServiceReportNumber = dto.ServiceReportNumber,
                        Description = dto.Description,
                        ClientId = dto.ClientId,
                        VehicleId = dto.VehicleId,
                        Instruction = dto.Instruction,
                        DetailedServiceReport = dto.DetailedServiceReport,
                        IsDeleted = dto.IsDeleted,
                        ServiceType = dto.ServiceType,
                        VehicleHours = dto.VehicleHours,
                        QuoteNumber = dto.QuoteNumber,
                        IsComplete = dto.IsComplete
                    };

                    _targetContext.ServiceReports.Add(report);
                }

                await _targetContext.SaveChangesAsync();
                _targetContext.ChangeTracker.Clear();

                _logger.LogInformation("Service Reports: Migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate Service Reports: {Message}", ex.Message);
                throw;
            }
        }

        private async Task MigrateStockTakesAsync()
        {
            _logger.LogInformation("Migrating Stock Takes...");

            try
            {
                var stockTakes = await _sourceContext.Database
                    .SqlQueryRaw<StockTakeDto>("SELECT Id, CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, IsDeleted, StockTakeDate, StockTakeBy, Type, Status, StockTakeDescription, Remarks, WarehouseId, CompletedDate FROM StockTakes")
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Stock Takes: {TotalRecords} records found", stockTakes.Count);

                foreach (var dto in stockTakes)
                {
                    var stockTake = new StockTake
                    {
                        Id = dto.Id,
                        CreatedAt = new DateTimeOffset(dto.CreatedAt, TimeSpan.Zero),
                        UpdatedAt = new DateTimeOffset(dto.UpdatedAt, TimeSpan.Zero),
                        CreatedBy = dto.CreatedBy,
                        UpdatedBy = dto.UpdatedBy,
                        IsDeleted = dto.IsDeleted,
                        StockTakeDate = new DateTimeOffset(dto.StockTakeDate, TimeSpan.Zero),
                        StockTakeBy = dto.StockTakeBy,
                        Type = dto.Type,
                        Status = dto.Status,
                        StockTakeDescription = dto.StockTakeDescription,
                        Remarks = dto.Remarks,
                        WarehouseId = dto.WarehouseId,
                        CompletedDate = new DateTimeOffset(dto.CompletedDate, TimeSpan.Zero)
                    };

                    _targetContext.StockTakes.Add(stockTake);
                }

                await _targetContext.SaveChangesAsync();
                _targetContext.ChangeTracker.Clear();

                _logger.LogInformation("Stock Takes: Migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate Stock Takes: {Message}", ex.Message);
                throw;
            }
        }

        private async Task MigrateStockAdjustmentsAsync()
        {
            _logger.LogInformation("Migrating Stock Adjustments...");

            try
            {
                var adjustments = await _sourceContext.Database
                    .SqlQueryRaw<StockAdjustmentDto>("SELECT Id, PartId, WarehouseId, QTY, AdjustementDate, AdjustedById, StockAdjustmentType FROM StockAdjustment")
                    .AsNoTracking()
                    .ToListAsync();

                _logger.LogInformation("Stock Adjustments: {TotalRecords} records found", adjustments.Count);

                foreach (var dto in adjustments)
                {
                    var adjustment = new StockAdjustment
                    {
                        Id = dto.Id,
                        PartId = dto.PartId,
                        WarehouseId = dto.WarehouseId,
                        QTY = dto.QTY,
                        AdjustementDate = new DateTimeOffset(dto.AdjustementDate, TimeSpan.Zero),
                        AdjustedById = dto.AdjustedById,
                        StockAdjustmentType = dto.StockAdjustmentType
                    };

                    _targetContext.StockAdjustment.Add(adjustment);
                }

                await _targetContext.SaveChangesAsync();
                _targetContext.ChangeTracker.Clear();

                _logger.LogInformation("Stock Adjustments: Migration completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate Stock Adjustments: {Message}", ex.Message);
                throw;
            }
        }

        private async Task MigrateIdentityAsync()
        {
            _logger.LogInformation("Migrating ASP.NET Identity tables...");

            var hasUsers = await _targetContext.Set<IdentityUser>().AsNoTracking().AnyAsync();
            if (hasUsers)
            {
                _logger.LogWarning("Target database already contains AspNetUsers. Skipping Identity inserts to avoid duplicate key conflicts.");
                _logger.LogInformation("Ensuring Identity normalized fields and sequences are correct...");
                await FixIdentityNormalizedFieldsAsync();
                await FixIdentitySequencesAsync();
                return;
            }

            await MigrateIdentityRolesAsync();
            await MigrateIdentityUsersAsync();
            await MigrateIdentityRoleClaimsAsync();
            await MigrateIdentityUserClaimsAsync();
            await MigrateIdentityUserLoginsAsync();
            await MigrateIdentityUserTokensAsync();
            await MigrateIdentityUserRolesAsync();

            await FixIdentityNormalizedFieldsAsync();
            await FixIdentitySequencesAsync();

            _logger.LogInformation("ASP.NET Identity migration completed successfully.");
        }

        private async Task FixIdentityNormalizedFieldsAsync()
        {
            try
            {
                // FindByNameAsync uses NormalizedUserName. If this is null/incorrect after migration,
                // users will appear as "not found" even though AspNetUsers contains rows.
                await _targetContext.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""AspNetUsers"" SET ""UserName"" = btrim(""UserName"") WHERE ""UserName"" IS NOT NULL AND ""UserName"" <> btrim(""UserName"");");

                await _targetContext.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""AspNetUsers"" SET ""NormalizedUserName"" = upper(""UserName"") WHERE ""UserName"" IS NOT NULL AND (""NormalizedUserName"" IS NULL OR ""NormalizedUserName"" = '');");

                await _targetContext.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""AspNetUsers"" SET ""Email"" = btrim(""Email"") WHERE ""Email"" IS NOT NULL AND ""Email"" <> btrim(""Email"");");

                await _targetContext.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""AspNetUsers"" SET ""NormalizedEmail"" = upper(""Email"") WHERE ""Email"" IS NOT NULL AND (""NormalizedEmail"" IS NULL OR ""NormalizedEmail"" = '');");

                await _targetContext.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""AspNetRoles"" SET ""NormalizedName"" = upper(""Name"") WHERE ""Name"" IS NOT NULL AND (""NormalizedName"" IS NULL OR ""NormalizedName"" = '');");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to fix Identity normalized fields. If FindByNameAsync returns null, check AspNetUsers.NormalizedUserName.");
            }
        }

        private async Task MigrateIdentityRolesAsync()
        {
            _logger.LogInformation("Migrating AspNetRoles...");

            var roles = await _sourceContext.Database
                .SqlQueryRaw<IdentityRoleDto>("SELECT Id, Name, NormalizedName, ConcurrencyStamp FROM AspNetRoles")
                .AsNoTracking()
                .ToListAsync();

            foreach (var dto in roles)
            {
                var name = dto.Name?.Trim();
                var normalizedName = (dto.NormalizedName ?? name)?.Trim().ToUpperInvariant();

                _targetContext.Add(new IdentityRole
                {
                    Id = dto.Id,
                    Name = name,
                    NormalizedName = normalizedName,
                    ConcurrencyStamp = dto.ConcurrencyStamp
                });
            }

            await _targetContext.SaveChangesAsync();
            _targetContext.ChangeTracker.Clear();
        }

        private async Task MigrateIdentityUsersAsync()
        {
            _logger.LogInformation("Migrating AspNetUsers...");

            var users = await _sourceContext.Database
                .SqlQueryRaw<IdentityUserDto>(
                    "SELECT Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, CAST(LockoutEnd AS datetimeoffset) AS LockoutEnd, LockoutEnabled, AccessFailedCount FROM AspNetUsers")
                .AsNoTracking()
                .ToListAsync();

            foreach (var dto in users)
            {
                var userName = dto.UserName?.Trim();
                var normalizedUserName = (dto.NormalizedUserName ?? userName)?.Trim().ToUpperInvariant();
                var email = dto.Email?.Trim();
                var normalizedEmail = (dto.NormalizedEmail ?? email)?.Trim().ToUpperInvariant();

                _targetContext.Add(new IdentityUser
                {
                    Id = dto.Id,
                    UserName = userName,
                    NormalizedUserName = normalizedUserName,
                    Email = email,
                    NormalizedEmail = normalizedEmail,
                    EmailConfirmed = dto.EmailConfirmed,
                    PasswordHash = dto.PasswordHash,
                    SecurityStamp = dto.SecurityStamp,
                    ConcurrencyStamp = dto.ConcurrencyStamp,
                    PhoneNumber = dto.PhoneNumber,
                    PhoneNumberConfirmed = dto.PhoneNumberConfirmed,
                    TwoFactorEnabled = dto.TwoFactorEnabled,
                    LockoutEnd = dto.LockoutEnd,
                    LockoutEnabled = dto.LockoutEnabled,
                    AccessFailedCount = dto.AccessFailedCount
                });
            }

            await _targetContext.SaveChangesAsync();
            _targetContext.ChangeTracker.Clear();
        }

        private async Task MigrateIdentityUserRolesAsync()
        {
            _logger.LogInformation("Migrating AspNetUserRoles...");

            var userRoles = await _sourceContext.Database
                .SqlQueryRaw<IdentityUserRoleDto>("SELECT UserId, RoleId FROM AspNetUserRoles")
                .AsNoTracking()
                .ToListAsync();

            foreach (var dto in userRoles)
            {
                _targetContext.Add(new IdentityUserRole<string>
                {
                    UserId = dto.UserId,
                    RoleId = dto.RoleId
                });
            }

            await _targetContext.SaveChangesAsync();
            _targetContext.ChangeTracker.Clear();
        }

        private async Task MigrateIdentityUserClaimsAsync()
        {
            _logger.LogInformation("Migrating AspNetUserClaims...");

            var claims = await _sourceContext.Database
                .SqlQueryRaw<IdentityUserClaimDto>("SELECT Id, UserId, ClaimType, ClaimValue FROM AspNetUserClaims")
                .AsNoTracking()
                .ToListAsync();

            foreach (var dto in claims)
            {
                _targetContext.Add(new IdentityUserClaim<string>
                {
                    Id = dto.Id,
                    UserId = dto.UserId,
                    ClaimType = dto.ClaimType,
                    ClaimValue = dto.ClaimValue
                });
            }

            await _targetContext.SaveChangesAsync();
            _targetContext.ChangeTracker.Clear();
        }

        private async Task MigrateIdentityRoleClaimsAsync()
        {
            _logger.LogInformation("Migrating AspNetRoleClaims...");

            var claims = await _sourceContext.Database
                .SqlQueryRaw<IdentityRoleClaimDto>("SELECT Id, RoleId, ClaimType, ClaimValue FROM AspNetRoleClaims")
                .AsNoTracking()
                .ToListAsync();

            foreach (var dto in claims)
            {
                _targetContext.Add(new IdentityRoleClaim<string>
                {
                    Id = dto.Id,
                    RoleId = dto.RoleId,
                    ClaimType = dto.ClaimType,
                    ClaimValue = dto.ClaimValue
                });
            }

            await _targetContext.SaveChangesAsync();
            _targetContext.ChangeTracker.Clear();
        }

        private async Task MigrateIdentityUserLoginsAsync()
        {
            _logger.LogInformation("Migrating AspNetUserLogins...");

            var logins = await _sourceContext.Database
                .SqlQueryRaw<IdentityUserLoginDto>("SELECT LoginProvider, ProviderKey, ProviderDisplayName, UserId FROM AspNetUserLogins")
                .AsNoTracking()
                .ToListAsync();

            foreach (var dto in logins)
            {
                _targetContext.Add(new IdentityUserLogin<string>
                {
                    LoginProvider = dto.LoginProvider,
                    ProviderKey = dto.ProviderKey,
                    ProviderDisplayName = dto.ProviderDisplayName,
                    UserId = dto.UserId
                });
            }

            await _targetContext.SaveChangesAsync();
            _targetContext.ChangeTracker.Clear();
        }

        private async Task MigrateIdentityUserTokensAsync()
        {
            _logger.LogInformation("Migrating AspNetUserTokens...");

            var tokens = await _sourceContext.Database
                .SqlQueryRaw<IdentityUserTokenDto>("SELECT UserId, LoginProvider, Name, Value FROM AspNetUserTokens")
                .AsNoTracking()
                .ToListAsync();

            foreach (var dto in tokens)
            {
                _targetContext.Add(new IdentityUserToken<string>
                {
                    UserId = dto.UserId,
                    LoginProvider = dto.LoginProvider,
                    Name = dto.Name,
                    Value = dto.Value
                });
            }

            await _targetContext.SaveChangesAsync();
            _targetContext.ChangeTracker.Clear();
        }

        private async Task FixIdentitySequencesAsync()
        {
            try
            {
                await _targetContext.Database.ExecuteSqlRawAsync(
                    @"SELECT setval(pg_get_serial_sequence('""AspNetUserClaims""','""Id""'), COALESCE((SELECT MAX(""Id"") FROM ""AspNetUserClaims""), 1));");
                await _targetContext.Database.ExecuteSqlRawAsync(
                    @"SELECT setval(pg_get_serial_sequence('""AspNetRoleClaims""','""Id""'), COALESCE((SELECT MAX(""Id"") FROM ""AspNetRoleClaims""), 1));");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to update Identity sequences.");
            }
        }
    }
}
