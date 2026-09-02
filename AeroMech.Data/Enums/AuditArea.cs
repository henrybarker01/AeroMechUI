namespace AeroMech.Data.Enums
{
    /// <summary>
    /// The part of the business an audit entry belongs to. Named after what a reader is asking
    /// about rather than after the table that was written, because somebody chasing a stock
    /// figure wants every movement whichever screen caused it, and somebody chasing a price
    /// wants every price whether it was typed on the part or came in on an invoice.
    /// </summary>
    public enum AuditArea
    {
        None = 0,

        /// <summary>Anything that changed a quantity on hand.</summary>
        Stock = 1,

        /// <summary>Anything that changed what a part costs or what a client is charged.</summary>
        Pricing = 2,

        /// <summary>The part record itself - added, described differently, removed.</summary>
        Parts = 3,

        StockReceiving = 4,
        StockTake = 5,
        ServiceReport = 6,
        Clients = 7,

        /// <summary>Who has access to the system.</summary>
        Users = 8,

        /// <summary>The machines on a client's fleet.</summary>
        Vehicles = 9,

        /// <summary>The people who work the jobs.</summary>
        Employees = 10,

        /// <summary>Work priced up before any of it is done.</summary>
        Quotes = 11,

        /// <summary>Hours booked against a person's day that no job accounts for.</summary>
        Timesheets = 12
    }
}
