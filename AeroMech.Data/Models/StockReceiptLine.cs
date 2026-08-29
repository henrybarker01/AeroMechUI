using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	/// <summary>
	/// One part taken in on a <see cref="StockReceipt"/>. The stock level either side of the
	/// receipt is stored on the line rather than recalculated, so the receipt still shows what it
	/// did to the stock long after later movements have changed the part's current level.
	/// </summary>
	public class StockReceiptLine
	{
		[Key]
		public int Id { get; set; }

		public virtual StockReceipt? StockReceipt { get; set; }

		[Required]
		public int StockReceiptId { get; set; }

		public virtual Part? Part { get; set; }

		[Required]
		public int PartId { get; set; }

		public int QtyReceived { get; set; }

		/// <summary>
		/// What this part cost on this invoice. Held per line because the invoice price is a fact
		/// about the invoice, and stays right even when the part's cost price later moves on.
		/// </summary>
		public double UnitCost { get; set; }

		public int QtyOnHandBefore { get; set; }

		public int QtyOnHandAfter { get; set; }

		/// <summary>
		/// Whether posting this line also pushed <see cref="UnitCost"/> onto the part's cost
		/// price, so a later reader can tell a repriced part from one left alone.
		/// </summary>
		public bool CostPriceUpdated { get; set; }

		public bool IsDeleted { get; set; }
	}
}
