using AeroMech.API.Reports;
using AeroMech.Data.Enums;
using AeroMech.Data.Persistence;
using AeroMech.Models.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AeroMech.UI.Web.Services
{
    /// <summary>
    /// Reading the audit trail, as opposed to writing it. Nothing here writes - that is
    /// <see cref="AuditService"/> - and nothing here can amend an entry, because a log that can be
    /// tidied up answers nothing.
    /// </summary>
    public class AuditReportService
    {
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly AuditLogReport _auditLogReport;

        public AuditReportService(
            IDbContextFactory<AeroMechDBContext> contextFactory,
            AuditLogReport auditLogReport)
        {
            _contextFactory = contextFactory;
            _auditLogReport = auditLogReport;
        }

        /// <summary>
        /// A year of stock movements is a document nobody can read and a request that would sit
        /// there generating. The report prints the most recent entries up to this many and says on
        /// its face that it did, which is more useful than either refusing or running for minutes.
        /// </summary>
        private const int MaxPrintedEntries = 2000;

        /// <summary>
        /// The people who actually appear in the log, for the filter to offer. Read from the log
        /// itself rather than from the user list so that somebody whose account has since been
        /// removed can still be selected - those are the entries most often being looked for.
        /// </summary>
        public async Task<List<string>> GetUsers()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.AuditLogs
                .AsNoTracking()
                .Select(x => x.UserName)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        /// <summary>
        /// How an area reads on screen and on paper. The enum names the code that wrote the entry;
        /// these name the thing somebody is asking about.
        /// </summary>
        public static string Describe(AuditArea area) => area switch
        {
            AuditArea.Stock => "Stock",
            AuditArea.Pricing => "Pricing",
            AuditArea.Parts => "Parts",
            AuditArea.StockReceiving => "Stock receiving",
            AuditArea.StockTake => "Stock takes",
            AuditArea.ServiceReport => "Service reports",
            AuditArea.Clients => "Clients",
            AuditArea.Users => "Users",
            _ => "Other"
        };

        public static string Describe(AuditAction action) => action switch
        {
            AuditAction.Created => "Created",
            AuditAction.Updated => "Updated",
            AuditAction.Deleted => "Deleted",
            AuditAction.StockAdjusted => "Stock adjusted",
            AuditAction.PriceChanged => "Price changed",
            AuditAction.Posted => "Posted",
            AuditAction.Cancelled => "Cancelled",
            _ => "Other"
        };

        /// <summary>
        /// Dates come off a date picker with no time on them. A period is read as whole calendar
        /// days in UTC, matching how entries are stamped, so something done late in the day on the
        /// closing date still falls inside the period.
        /// </summary>
        private static (DateTimeOffset FromStart, DateTimeOffset ToEndExclusive) PeriodBounds(DateOnly from, DateOnly to)
            => (new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));

        private static string DescribeUsers(IReadOnlyCollection<string> userNames)
            => userNames.Count == 0 ? "All users"
                : userNames.Count <= 6 ? string.Join(", ", userNames)
                : $"{userNames.Count} users";

        private static string DescribeAreas(IReadOnlyCollection<AuditArea> areas)
            => areas.Count == 0 ? "All activity" : string.Join(", ", areas.Select(Describe));

        /// <summary>
        /// The log over a period, filtered to the people and the subjects asked about, newest
        /// first and grouped by the day it happened on.
        /// </summary>
        public async Task<byte[]> GenerateAuditLogReport(AuditLogReportRequestModel request)
        {
            if (request.ToDate < request.FromDate)
                throw new InvalidOperationException("The end of the period cannot fall before its start.");

            var userNames = request.UserNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var areas = request.Areas.Distinct().OrderBy(x => x).ToList();
            var searchTerm = request.SearchTerm?.Trim();

            var (fromStart, toEndExclusive) = PeriodBounds(request.FromDate, request.ToDate);

            using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.AuditLogs
                .AsNoTracking()
                .Where(x => x.OccurredAt >= fromStart && x.OccurredAt < toEndExclusive);

            if (userNames.Count > 0)
                query = query.Where(x => userNames.Contains(x.UserName));

            if (areas.Count > 0)
                query = query.Where(x => areas.Contains(x.Area));

            // One box rather than one per column: a part code, an invoice number and a sheet
            // number are all just "the thing I am asking about", and they are held in different
            // columns depending on what wrote the entry.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();

                query = query.Where(x =>
                    (x.Reference != null && x.Reference.ToLower().Contains(term))
                    || x.Description.ToLower().Contains(term)
                    || x.EntityType.ToLower().Contains(term));
            }

            var totalEntries = await query.CountAsync();

            var entries = await query
                .OrderByDescending(x => x.OccurredAt)
                .ThenByDescending(x => x.Id)
                .Take(MaxPrintedEntries)
                .ToListAsync();

            var days = entries
                .GroupBy(x => DateOnly.FromDateTime(x.OccurredAt.UtcDateTime))
                .OrderByDescending(x => x.Key)
                .Select(group => new AuditLogReportDay
                {
                    Date = group.Key,
                    Lines = group.Select(x => new AuditLogReportLine
                    {
                        OccurredAt = x.OccurredAt.ToUniversalTime(),
                        UserName = x.UserName,
                        Area = Describe(x.Area),
                        Action = Describe(x.Action),
                        Reference = x.Reference,
                        Description = x.Description,
                        Field = x.Field,
                        OldValue = x.OldValue,
                        NewValue = x.NewValue
                    }).ToList()
                })
                .ToList();

            _auditLogReport.Data = new AuditLogReportData
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                UserLabel = DescribeUsers(userNames),
                AreaLabel = DescribeAreas(areas),
                SearchTerm = searchTerm,
                TotalEntries = totalEntries,
                Truncated = totalEntries > entries.Count,
                Days = days
            };

            return Document.Create(_auditLogReport.Compose).GeneratePdf();
        }
    }
}
