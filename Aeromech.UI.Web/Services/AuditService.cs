using AeroMech.Data.Enums;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AeroMech.UI.Web.Services
{
    /// <summary>
    /// Writing the audit trail. Nothing here reads it back - that is
    /// <see cref="AuditReportService"/>, kept apart so the path that records what happened cannot
    /// be changed by the needs of the path that prints it.
    ///
    /// Entries are added to the caller's own <see cref="AeroMechDBContext"/> rather than saved on
    /// a context of their own, so they are committed by the same <c>SaveChanges</c> - and inside
    /// the same transaction - as the change they describe. That is the whole point: a stock
    /// adjustment that was rolled back must not leave an entry saying it happened, and one that
    /// stuck must not be able to lack one. <see cref="RecordAsync"/> exists for the few events
    /// that have no transaction of their own to join, such as adding a user through Identity.
    /// </summary>
    public class AuditService
    {
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly CurrentUserService _currentUserService;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            IDbContextFactory<AeroMechDBContext> contextFactory,
            CurrentUserService currentUserService,
            ILogger<AuditService> logger)
        {
            _contextFactory = contextFactory;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// The user to record against the entries a piece of work is about to write. Resolved once
        /// at the top of that work rather than per entry, so every row written by one action names
        /// the same person even if the circuit goes away midway.
        /// </summary>
        public Task<string> ResolveUser(string? preferredUserName = null)
            => _currentUserService.GetUserName(preferredUserName);

        /// <summary>
        /// Numbers are stored as text because one column has to hold quantities, prices and rates
        /// alike. Invariant, so a row written on a machine with one culture still reads the same
        /// on a machine with another.
        /// </summary>
        public static string FormatQuantity(int value)
            => value.ToString(CultureInfo.InvariantCulture);

        public static string FormatMoney(double value)
            => value.ToString("0.00", CultureInfo.InvariantCulture);

        public static string FormatMoney(decimal value)
            => value.ToString("0.00", CultureInfo.InvariantCulture);

        /// <summary>
        /// Hours are a count of time, not money, so they read to two-decimal precision only when
        /// the fraction is there to show - "8" and "7.5", not "8.00". Invariant for the same
        /// reason as the rest: a row must read the same wherever it is later opened.
        /// </summary>
        public static string FormatHours(double value)
            => value.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>
        /// Queues one entry against the caller's context. It is written when the caller saves, so
        /// a caller that never saves records nothing - which is correct, because nothing happened.
        /// </summary>
        public AuditLog Record(
            AeroMechDBContext context,
            string userName,
            AuditArea area,
            AuditAction action,
            string entityType,
            int? entityId,
            string? reference,
            string description,
            string? field = null,
            string? oldValue = null,
            string? newValue = null)
        {
            var entry = Build(userName, area, action, entityType, entityId, reference, description, field, oldValue, newValue);

            context.AuditLogs.Add(entry);

            return entry;
        }

        /// <summary>
        /// A quantity on hand moved, and this is what moved it. Every path that writes a
        /// <see cref="StockAdjustment"/> also writes one of these, so the ledger says what the
        /// level did and the audit trail says who did it and why.
        /// </summary>
        public AuditLog RecordStockChange(
            AeroMechDBContext context,
            string userName,
            int partId,
            string? partCode,
            int quantityBefore,
            int quantityAfter,
            string description)
            => Record(
                context,
                userName,
                AuditArea.Stock,
                AuditAction.StockAdjusted,
                nameof(Part),
                partId,
                partCode,
                description,
                nameof(Part.QtyOnHand),
                FormatQuantity(quantityBefore),
                FormatQuantity(quantityAfter));

        /// <summary>
        /// A price moved. Cost prices are changed from two places - typed on the part, or taken
        /// off a supplier invoice while receiving - and both come through here so a part's price
        /// history reads as one list.
        /// </summary>
        public AuditLog RecordPriceChange(
            AeroMechDBContext context,
            string userName,
            string entityType,
            int? entityId,
            string? reference,
            string field,
            string oldValue,
            string newValue,
            string description)
            => Record(
                context,
                userName,
                AuditArea.Pricing,
                AuditAction.PriceChanged,
                entityType,
                entityId,
                reference,
                description,
                field,
                oldValue,
                newValue);

        /// <summary>
        /// Records an event that has no transaction of its own to join, on a context opened for
        /// the purpose. A failure here is logged and swallowed: the change it describes has
        /// already happened and cannot be undone by refusing to record it, and taking the screen
        /// down after the fact would tell the user the opposite of the truth.
        /// </summary>
        public async Task RecordAsync(
            string userName,
            AuditArea area,
            AuditAction action,
            string entityType,
            int? entityId,
            string? reference,
            string description,
            string? field = null,
            string? oldValue = null,
            string? newValue = null)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                context.AuditLogs.Add(
                    Build(userName, area, action, entityType, entityId, reference, description, field, oldValue, newValue));

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not write an audit entry for {Action} on {EntityType} {Reference}.", action, entityType, reference);
            }
        }

        /// <summary>
        /// Trims each value to what its column will hold. A description that ran long is worth
        /// more shortened than lost to a failed insert, and the length only bites on free text
        /// that is already at the end of its sentence.
        /// </summary>
        private static AuditLog Build(
            string userName,
            AuditArea area,
            AuditAction action,
            string entityType,
            int? entityId,
            string? reference,
            string description,
            string? field,
            string? oldValue,
            string? newValue)
            => new AuditLog
            {
                OccurredAt = DateTimeOffset.UtcNow,
                UserName = Truncate(string.IsNullOrWhiteSpace(userName) ? CurrentUserService.UnknownUser : userName, 256)!,
                Area = area,
                Action = action,
                EntityType = Truncate(entityType, 128)!,
                EntityId = entityId,
                Reference = Truncate(reference, 128),
                Description = Truncate(description, 512)!,
                Field = Truncate(field, 128),
                OldValue = Truncate(oldValue, 256),
                NewValue = Truncate(newValue, 256)
            };

        private static string? Truncate(string? value, int maxLength)
            => value is not null && value.Length > maxLength ? value[..maxLength] : value;
    }
}
