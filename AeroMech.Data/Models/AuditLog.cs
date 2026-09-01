using AeroMech.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
    /// <summary>
    /// One thing one person did, kept for as long as the system runs.
    ///
    /// Deliberately not a <see cref="BaseModel"/>: an audit row is written once and never edited,
    /// so it has no updated-by of its own, and it carries no <c>IsDeleted</c> because a record
    /// that can be quietly withdrawn answers nothing. The row is written in the same transaction
    /// as the change it describes wherever the change runs in one, so a change that was rolled
    /// back leaves no entry claiming it happened, and a change that stuck cannot lack one.
    ///
    /// The values either side of a change are stored as text rather than typed columns because
    /// this one table has to hold quantities, prices, rates and descriptions alike, and a reader
    /// wants to see what was there and what replaced it, not to compute with it.
    /// </summary>
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public DateTimeOffset OccurredAt { get; set; }

        /// <summary>
        /// The signed-in user, as they are known to the system. Stored as the name rather than as
        /// a foreign key to the identity user: an audit trail has to keep reading correctly after
        /// an account is removed.
        /// </summary>
        [MaxLength(256)]
        public string UserName { get; set; } = string.Empty;

        public AuditArea Area { get; set; }

        public AuditAction Action { get; set; }

        /// <summary>
        /// What was touched, in the system's own words - "Part", "StockReceipt", "StockTake".
        /// </summary>
        [MaxLength(128)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        /// <summary>
        /// How the thing touched is known to somebody reading the log - a part code, an invoice
        /// number, a sheet number. The id alone would mean nothing on paper.
        /// </summary>
        [MaxLength(128)]
        public string? Reference { get; set; }

        /// <summary>
        /// What happened, in a sentence, including what caused it. This is the column the report
        /// is read from, so it is written to stand on its own.
        /// </summary>
        [MaxLength(512)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The field that moved, where one did. Null for entries that describe a whole document
        /// rather than a single value.
        /// </summary>
        [MaxLength(128)]
        public string? Field { get; set; }

        [MaxLength(256)]
        public string? OldValue { get; set; }

        [MaxLength(256)]
        public string? NewValue { get; set; }
    }
}
