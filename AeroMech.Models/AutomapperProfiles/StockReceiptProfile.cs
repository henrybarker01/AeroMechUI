using AeroMech.Data.Models;
using AeroMech.Models.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class StockReceiptProfile : Profile
	{
		public StockReceiptProfile()
		{
			// Posting is done by hand in the service rather than mapped, because a receipt also
			// has to move stock and write the ledger; only the read direction is mapped here.
			CreateMap<StockReceipt, StockReceiptModel>()
				.ForMember(x => x.Lines, opt => opt.MapFrom(x => x.Lines))
				.ForMember(x => x.UpdateCostPrices, opt => opt.Ignore());

			CreateMap<StockReceiptLine, StockReceivingLineModel>()
				.ForMember(x => x.PartCode, opt => opt.MapFrom(x => x.Part == null ? string.Empty : x.Part.PartCode))
				.ForMember(x => x.PartDescription, opt => opt.MapFrom(x => x.Part == null ? string.Empty : x.Part.PartDescription))
				.ForMember(x => x.Bin, opt => opt.MapFrom(x => x.Part == null ? null : x.Part.Bin))
				.ForMember(x => x.ProductClass, opt => opt.MapFrom(x => x.Part == null ? null : x.Part.ProductClass))
				.ForMember(x => x.WarehouseId, opt => opt.MapFrom(x => x.Part == null ? 0 : x.Part.WarehouseId))
				.ForMember(x => x.WarehouseCode, opt => opt.MapFrom(x => x.Part == null || x.Part.Warehouse == null ? null : x.Part.Warehouse.WarehouseCode))
				// A posted line shows the levels it moved between, not the part's level today.
				.ForMember(x => x.QtyOnHand, opt => opt.MapFrom(x => x.QtyOnHandBefore))
				.ForMember(x => x.CurrentCostPrice, opt => opt.MapFrom(x => x.UnitCost));
		}
	}
}
