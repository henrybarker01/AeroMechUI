namespace AeroMech.Data.Enums
{
    /// <summary>
    /// What was done. Deliberately a short list: an audit log is read by somebody scanning for
    /// the one entry that explains a figure, and a long list of near-synonyms would make that
    /// scan harder rather than easier. What makes an entry specific is its description and the
    /// values either side of it, not a finer-grained verb.
    /// </summary>
    public enum AuditAction
    {
        None = 0,
        Created = 1,
        Updated = 2,
        Deleted = 3,

        /// <summary>A quantity on hand moved.</summary>
        StockAdjusted = 4,

        /// <summary>A cost price, selling price or client rate moved.</summary>
        PriceChanged = 5,

        /// <summary>A document was settled against the system - a receipt taken in, a sheet posted.</summary>
        Posted = 6,

        Cancelled = 7
    }
}
