using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models.Models
{
    /// <summary>
    /// What is being asked for when a count sheet is raised. Only the scope needs describing -
    /// the lines and the stock levels they freeze are worked out from it once, at creation.
    /// </summary>
    public class StockTakeRequestModel
    {
        public DateTimeOffset StockTakeDate { get; set; } = DateTimeOffset.UtcNow;

        [Required(ErrorMessage = "A description is required so the sheet can be told apart later")]
        public string StockTakeDescription { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        /// <summary>
        /// The suppliers to count. Empty means every supplier, which is the full stock take.
        /// </summary>
        public List<string> SupplierCodes { get; set; } = new();

        /// <summary>
        /// Whether the counter is kept from seeing the expected quantity. See
        /// <see cref="StockTakeModel.BlindCount"/> for why this defaults on.
        /// </summary>
        public bool BlindCount { get; set; } = true;

        /// <summary>
        /// Leaves out parts the system already believes are empty. Off by default, because a part
        /// recorded as zero that turns out to have three on the shelf is exactly the kind of
        /// error a count is run to find - but on a long tail of dormant parts it makes for a lot
        /// of paper, so it is offered.
        /// </summary>
        public bool ExcludeZeroQtyParts { get; set; }

        public string? StockTakeBy { get; set; }

        public bool IsAllSuppliers => SupplierCodes.Count == 0;
    }
}
