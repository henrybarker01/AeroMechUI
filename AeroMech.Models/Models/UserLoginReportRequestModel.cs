using AeroMech.Data.Enums;

namespace AeroMech.Models.Models
{
    /// <summary>
    /// The period and scope a user login report covers: who was signing in, when, and whether
    /// they managed it. A narrower cousin of <see cref="AuditLogReportRequestModel"/> - same
    /// period-first reading, but over sign-in traffic only.
    /// </summary>
    public class UserLoginReportRequestModel
    {
        public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

        public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        /// <summary>
        /// The users to report on. Empty means everybody, which is the usual case.
        /// </summary>
        public List<string> UserNames { get; set; } = new();

        /// <summary>
        /// The kinds of event to include - sign-ins, refused attempts, sign-outs. Empty means all
        /// of them, which is how the report reads as a story rather than a filtered slice.
        /// </summary>
        public List<AuditAction> Events { get; set; } = new();

        public bool IsAllUsers => UserNames.Count == 0;

        public bool IsAllEvents => Events.Count == 0;
    }
}
