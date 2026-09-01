using AeroMech.Data.Enums;

namespace AeroMech.Models.Models
{
    /// <summary>
    /// The period and scope an audit log report covers.
    ///
    /// Every filter is a narrowing of one list held in date order, because that is how the log is
    /// actually read: somebody starts from "what happened last week", then cuts it down to the
    /// person, the subject or the part they are asking about.
    /// </summary>
    public class AuditLogReportRequestModel
    {
        public DateOnly FromDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);

        public DateOnly ToDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        /// <summary>
        /// The users to report on. Empty means everybody, which is the usual case - the log is
        /// most often read before anyone knows whose entry they are looking for.
        /// </summary>
        public List<string> UserNames { get; set; } = new();

        /// <summary>
        /// The subjects to report on - stock, pricing, users. Empty means all of them.
        /// </summary>
        public List<AuditArea> Areas { get; set; } = new();

        /// <summary>
        /// Matched against the reference and the description, so a part code, an invoice number or
        /// a sheet number all find the entries about them without needing a separate filter each.
        /// </summary>
        public string? SearchTerm { get; set; }

        public bool IsAllUsers => UserNames.Count == 0;

        public bool IsAllAreas => Areas.Count == 0;
    }
}
