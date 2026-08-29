using AeroMech.Data.Models;
using AeroMech.Models.Enums;
using AeroMech.Models.Models;
using AutoMapper;

namespace AeroMech.Models.AutomapperProfiles
{
	public class QuoteProfile : Profile
	{
		public QuoteProfile()
		{
			CreateMap<Quote, QuoteModel>()
				.ForMember(x => x.Parts, opt => opt.MapFrom(x => x.Parts))
				.ForMember(x => x.Labour, opt => opt.MapFrom(x => x.Labour))
				.ForMember(x => x.Client, opt => opt.MapFrom(x => x.Client))
				.ForMember(x => x.ServiceReportId, opt => opt.MapFrom(x => x.ServiceReport != null ? (int?)x.ServiceReport.Id : null))
				.ForMember(x => x.ServiceReportNumber, opt => opt.MapFrom(x => x.ServiceReport != null ? (int?)x.ServiceReport.ServiceReportNumber : null))
				.ForMember(x => x.ServiceType, opt => opt.MapFrom(s => ParseServiceType(s.ServiceType)))
				// Ad hoc parts are a separate table only because they have no Part row behind
				// them. The form shows one parts grid, so they join the stocked parts here.
				.AfterMap((s, d) =>
				{
					if (s.AdHockParts == null) return;

					d.Parts.AddRange(s.AdHockParts.Select(a => new QuotePartModel()
					{
						Id = a.Id,
						QTY = a.Qty,
						PartCode = string.IsNullOrEmpty(a.PartCode) ? "AdHock" : a.PartCode,
						PartDescription = a.PartDescription,
						CostPrice = a.CostPrice,
						SellingPrice = a.CostPrice,
						Discount = a.Discount,
						IsAdHockPart = true,
						IsDeleted = a.IsDeleted,
						Bin = "",
						ProductClass = "",
						SupplierCode = "",
						CycleCount = 0,
						QtyOnHand = 0
					}));
				});

			CreateMap<QuoteModel, Quote>()
				.ForMember(x => x.Client, opt => opt.Ignore())
				.ForMember(x => x.Vehicle, opt => opt.Ignore())
				.ForMember(x => x.ServiceReport, opt => opt.Ignore())
				.ForMember(x => x.Parts, opt => opt.Ignore())
				.ForMember(x => x.AdHockParts, opt => opt.Ignore())
				.ForMember(x => x.Labour, opt => opt.MapFrom(x => x.Labour))
				.ForMember(x => x.ServiceType, opt => opt.MapFrom(s => s.ServiceType.ToString()));

			CreateMap<QuoteLabour, QuoteLabourModel>();
			CreateMap<QuoteLabourModel, QuoteLabour>()
				.ForMember(x => x.Quote, opt => opt.Ignore());

			CreateMap<QuotePart, QuotePartModel>()
					// Older lines were saved without a price of their own and fall back to the
					// part's current one.
					.ForMember(x => x.CostPrice, opt => opt.MapFrom(e => e.CostPrice > 0 || e.Part == null
						? e.CostPrice
						: e.Part.Prices.Select(p => p.CostPrice).FirstOrDefault()))
					.ForMember(x => x.PartCode, opt => opt.MapFrom(e => e.Part.PartCode))
					.ForMember(x => x.PartDescription, opt => opt.MapFrom(e => e.Part.PartDescription));

			CreateMap<QuotePartModel, QuotePart>()
				.ForMember(x => x.Quote, opt => opt.Ignore())
				.ForMember(x => x.Part, opt => opt.Ignore())
				.ForMember(x => x.Qty, opt => opt.MapFrom(e => e.QTY))
				.ForMember(x => x.PartId, opt => opt.MapFrom(e => e.PartId));
		}

		// Older rows can hold an empty service type, and a quote is still worth showing without
		// one.
		private static ServiceType ParseServiceType(string? serviceType)
			=> Enum.TryParse<ServiceType>(serviceType, out var parsed) ? parsed : ServiceType.None;
	}
}
