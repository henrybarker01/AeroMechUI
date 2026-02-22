using AeroMech.Data.Enums;
using AeroMech.Models.Enums;

namespace AeroMech.Database.DataMigration
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ContactPersonName { get; set; }
        public string? ContactPersonNumber { get; set; }
        public string? ContactPersonEmail { get; set; }
        public DateTime? ContactPersonBirthDate { get; set; }
        public bool IsDeleted { get; set; }
        public int AddressId { get; set; }
    }

    public class EmployeeDto
    {
        public int Id { get; set; }
        public string? IDNumber { get; set; }
        public string? Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTime? BirthDate { get; set; }
        public bool IsDeleted { get; set; }
        public int? AddressId { get; set; }
    }

    public class ClientRateDto
    {
        public int Id { get; set; }
        public DateTime EffectiveDate { get; set; }
        public decimal Rate { get; set; }
        public int ClientId { get; set; }
        public RateType RateType { get; set; }
        public bool IsActive { get; set; }
    }

    public class PartPriceDto
    {
        public int Id { get; set; }
        public int PartId { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public DateTime EffectiveDate { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class ServiceReportDto
    {
        public int Id { get; set; }
        public DateTime ReportDate { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? JobNumber { get; set; }
        public int ServiceReportNumber { get; set; }
        public string? Description { get; set; }
        public int? ClientId { get; set; }
        public int? VehicleId { get; set; }
        public string? Instruction { get; set; }
        public string? DetailedServiceReport { get; set; }
        public bool IsDeleted { get; set; }
        public string? ServiceType { get; set; }
        public int? VehicleHours { get; set; }
        public int? QuoteNumber { get; set; }
        public bool IsComplete { get; set; }
    }

    public class StockTakeDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime StockTakeDate { get; set; }
        public string StockTakeBy { get; set; }
        public string Type { get; set; }
        public StockTakeStatus Status { get; set; }
        public string StockTakeDescription { get; set; }
        public string Remarks { get; set; }
        public int WarehouseId { get; set; }
        public DateTime CompletedDate { get; set; }
    }

    public class StockAdjustmentDto
    {
        public int Id { get; set; }
        public int PartId { get; set; }
        public int WarehouseId { get; set; }
        public int QTY { get; set; }
        public DateTime AdjustementDate { get; set; }
        public Guid AdjustedById { get; set; }
        public StockAdjustmentType StockAdjustmentType { get; set; }
    }

    public class IdentityUserDto
    {
        public string Id { get; set; }
        public string? UserName { get; set; }
        public string? NormalizedUserName { get; set; }
        public string? Email { get; set; }
        public string? NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PasswordHash { get; set; }
        public string? SecurityStamp { get; set; }
        public string? ConcurrencyStamp { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
    }

    public class IdentityRoleDto
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public string? NormalizedName { get; set; }
        public string? ConcurrencyStamp { get; set; }
    }

    public class IdentityUserRoleDto
    {
        public string UserId { get; set; }
        public string RoleId { get; set; }
    }

    public class IdentityUserClaimDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string? ClaimType { get; set; }
        public string? ClaimValue { get; set; }
    }

    public class IdentityRoleClaimDto
    {
        public int Id { get; set; }
        public string RoleId { get; set; }
        public string? ClaimType { get; set; }
        public string? ClaimValue { get; set; }
    }

    public class IdentityUserLoginDto
    {
        public string LoginProvider { get; set; }
        public string ProviderKey { get; set; }
        public string? ProviderDisplayName { get; set; }
        public string UserId { get; set; }
    }

    public class IdentityUserTokenDto
    {
        public string UserId { get; set; }
        public string LoginProvider { get; set; }
        public string Name { get; set; }
        public string? Value { get; set; }
    }
}
