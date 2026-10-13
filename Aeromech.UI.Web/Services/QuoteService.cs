using AeroMech.API.Reports;
using AeroMech.Data.Enums;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AeroMech.Models.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Fluent;

namespace AeroMech.UI.Web.Services
{
    /// <summary>
    /// Quotes are priced before any work happens, so nothing here writes a
    /// <see cref="StockAdjustment"/>, touches <see cref="Part.QtyOnHand"/> or creates a
    /// <see cref="ServiceReportEmployee"/> row that a timesheet would pick up. Stock and hours
    /// only move when <see cref="BuildServiceReportFromQuote"/> hands the quote over to
    /// <see cref="ServiceReportService"/>. A quote raised from a service report
    /// (<see cref="QuoteServiceReport"/>) comes after its report has already moved them, and moves
    /// nothing itself.
    /// </summary>
    public class QuoteService
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly QuoteDocument _quoteDocument;
        private readonly AuditService _auditService;
        private readonly IMemoryCache _memoryCache;

        public QuoteService(IDbContextFactory<AeroMechDBContext> contextFactory, IMapper mapper, QuoteDocument quoteDocument, AuditService auditService, IMemoryCache memoryCache)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
            _quoteDocument = quoteDocument;
            _auditService = auditService;
            _memoryCache = memoryCache;
        }

        // Date picker is date-only. Persist as UTC midnight for the selected calendar date (no timezone day-shift).
        private static DateTimeOffset NormalizeDateOnlyToUtc(DateTimeOffset value)
            => new DateTimeOffset(value.Date, TimeSpan.Zero);

        // One series for every quote, whether it was written up front or raised from a service report.
        private static async Task<int> NextQuoteNumber(AeroMechDBContext context)
            => (await context.Quotes.MaxAsync(x => (int?)x.QuoteNumber) ?? 0) + 1;

        public async Task<int> AddQuote(QuoteModel quote)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var quoteToAdd = _mapper.Map<Quote>(quote);

            quoteToAdd.Id = 0;
            quoteToAdd.QuoteDate = NormalizeDateOnlyToUtc(quote.QuoteDate);
            quoteToAdd.ClientId = quote.ClientId == 0 ? null : quote.ClientId;
            quoteToAdd.VehicleId = quote.VehicleId == 0 ? null : quote.VehicleId;
            quoteToAdd.QuoteNumber = await NextQuoteNumber(_aeroMechDBContext);

            quoteToAdd.Labour.ForEach(x => x.Id = 0);

            quoteToAdd.Parts = new List<QuotePart>();
            quoteToAdd.AdHockParts = new List<QuoteAdHockPart>();

            foreach (var part in quote.Parts.Where(x => !x.IsDeleted))
            {
                if (part.IsAdHockPart)
                    quoteToAdd.AdHockParts.Add(NewAdHockPart(part));
                else
                    quoteToAdd.Parts.Add(NewPart(part));
            }

            _aeroMechDBContext.Quotes.Add(quoteToAdd);

            // Saved first so the entry can name the id the save produced, and inside one
            // transaction so a quote cannot be raised without a record that it was.
            using var transaction = await _aeroMechDBContext.Database.BeginTransactionAsync();

            await _aeroMechDBContext.SaveChangesAsync();

            var reference = $"AEM{quoteToAdd.QuoteNumber}";

            _auditService.Record(
                _aeroMechDBContext,
                await _auditService.ResolveUser(),
                AuditArea.Quotes,
                AuditAction.Created,
                nameof(Quote),
                quoteToAdd.Id,
                reference,
                $"Quote {reference} created.");

            await _aeroMechDBContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return quoteToAdd.Id;
        }

        public async Task<int> EditQuote(QuoteModel quote)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var quoteToEdit = await _aeroMechDBContext.Quotes
                .Include(x => x.Parts)
                .Include(x => x.AdHockParts)
                .Include(x => x.Labour)
                .Include(x => x.ServiceReport)
                .SingleAsync(x => x.Id == quote.Id);

            // A converted quote is the record of what the client accepted, so it stops changing
            // once the service report exists.
            if (quoteToEdit.ServiceReport != null)
                throw new InvalidOperationException($"Quote AEM{quoteToEdit.QuoteNumber} has already been converted to a service report and can no longer be edited.");

            quoteToEdit.QuoteDate = NormalizeDateOnlyToUtc(quote.QuoteDate);
            quoteToEdit.ClientId = quote.ClientId == 0 ? null : quote.ClientId;
            quoteToEdit.VehicleId = quote.VehicleId == 0 ? null : quote.VehicleId;
            quoteToEdit.VehicleHours = quote.VehicleHours;
            quoteToEdit.Description = quote.Description;
            quoteToEdit.Instruction = quote.Instruction;
            quoteToEdit.DetailedServiceReport = quote.DetailedServiceReport;
            quoteToEdit.ServiceType = quote.ServiceType.ToString();

            foreach (var labour in quote.Labour)
            {
                var existing = labour.Id != 0 ? quoteToEdit.Labour.SingleOrDefault(x => x.Id == labour.Id) : null;

                if (existing == null)
                {
                    if (labour.IsDeleted) continue;

                    quoteToEdit.Labour.Add(new QuoteLabour()
                    {
                        QuoteId = quoteToEdit.Id,
                        RateType = labour.RateType,
                        Rate = labour.Rate,
                        Hours = labour.Hours,
                        Discount = labour.Discount
                    });
                }
                else
                {
                    existing.RateType = labour.RateType;
                    existing.Rate = labour.Rate;
                    existing.Hours = labour.Hours;
                    existing.Discount = labour.Discount;
                    existing.IsDeleted = labour.IsDeleted;
                }
            }

            foreach (var part in quote.Parts)
            {
                if (part.IsAdHockPart)
                {
                    var existing = part.Id != 0 ? quoteToEdit.AdHockParts.SingleOrDefault(x => x.Id == part.Id) : null;

                    if (existing == null)
                    {
                        if (part.IsDeleted) continue;

                        var adHock = NewAdHockPart(part);
                        adHock.QuoteId = quoteToEdit.Id;
                        quoteToEdit.AdHockParts.Add(adHock);
                    }
                    else
                    {
                        existing.PartCode = part.PartCode;
                        existing.PartDescription = part.PartDescription;
                        existing.CostPrice = part.CostPrice;
                        existing.Discount = part.Discount;
                        existing.Qty = part.QTY;
                        existing.IsDeleted = part.IsDeleted;
                    }
                }
                else
                {
                    var existing = quoteToEdit.Parts.SingleOrDefault(x => x.PartId == part.PartId);

                    if (existing == null)
                    {
                        if (part.IsDeleted) continue;

                        var quotePart = NewPart(part);
                        quotePart.QuoteId = quoteToEdit.Id;
                        quoteToEdit.Parts.Add(quotePart);
                    }
                    else
                    {
                        existing.CostPrice = part.CostPrice;
                        existing.Discount = part.Discount;
                        existing.Qty = part.QTY;
                        existing.IsDeleted = part.IsDeleted;
                    }
                }
            }

            var reference = $"AEM{quoteToEdit.QuoteNumber}";

            _auditService.Record(
                _aeroMechDBContext,
                await _auditService.ResolveUser(),
                AuditArea.Quotes,
                AuditAction.Updated,
                nameof(Quote),
                quoteToEdit.Id,
                reference,
                $"Quote {reference} updated.");

            await _aeroMechDBContext.SaveChangesAsync();

            return quoteToEdit.Id;
        }

        public async Task<QuoteModel> GetQuote(int id)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var quote = await QuoteWithDetail(_aeroMechDBContext).SingleAsync(x => x.Id == id);

            return _mapper.Map<QuoteModel>(quote);
        }

        public async Task<List<QuoteModel>> GetQuotes(DateTimeOffset fromDate = default)
        {
            if (fromDate == default)
                fromDate = DateTimeOffset.MinValue;
            else
                fromDate = fromDate.ToUniversalTime();

            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var quotes = await QuoteWithDetail(_aeroMechDBContext)
                .Where(x => !x.IsDeleted && x.QuoteDate >= fromDate)
                .OrderByDescending(x => x.QuoteDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<QuoteModel>>(quotes).ToList();
        }

        public async Task DeleteQuote(int id)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var quote = await _aeroMechDBContext.Quotes
                .Include(x => x.ServiceReport)
                .SingleAsync(x => x.Id == id);

            if (quote.ServiceReport != null)
                throw new InvalidOperationException($"Quote AEM{quote.QuoteNumber} has already been converted to a service report and cannot be deleted.");

            quote.IsDeleted = true;

            var reference = $"AEM{quote.QuoteNumber}";

            _auditService.Record(
                _aeroMechDBContext,
                await _auditService.ResolveUser(),
                AuditArea.Quotes,
                AuditAction.Deleted,
                nameof(Quote),
                quote.Id,
                reference,
                $"Quote {reference} deleted.");

            await _aeroMechDBContext.SaveChangesAsync();
        }

        public async Task<byte[]> DownloadQuote(int quoteId)
        {
            _quoteDocument.quote = await GetQuote(quoteId);

            return Document.Create(_quoteDocument.Compose).GeneratePdf();
        }

        /// <summary>
        /// Copies an accepted quote into an unsaved service report, ready for the user to fill in
        /// the labour that was actually worked. Nothing is written here: the quote only becomes a
        /// service report when this model is saved through
        /// <see cref="ServiceReportService.AddServiceReport"/>, which is also where the stock
        /// finally moves. The caller keeps hold of <paramref name="quote"/> so the conversion
        /// screen can show what was quoted next to what is being captured.
        /// </summary>
        public ServiceReportModel BuildServiceReportFromQuote(QuoteModel quote)
        {
            if (quote.IsConverted)
                throw new InvalidOperationException($"Quote AEM{quote.QuoteNumber} has already been converted to service report AEM{quote.ServiceReportNumber}.");

            return new ServiceReportModel
            {
                Id = 0,
                QuoteId = quote.Id,
                QuoteNumber = quote.QuoteNumber,
                ReportDate = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero),
                ClientId = quote.ClientId,
                Client = quote.Client,
                VehicleId = quote.VehicleId,
                Vehicle = quote.Vehicle,
                VehicleHours = quote.VehicleHours,
                ServiceType = quote.ServiceType,
                Instruction = quote.Instruction,
                DetailedServiceReport = quote.DetailedServiceReport,
                Description = quote.Description,
                // The quoted labour was priced per rate type and says nothing about who did the
                // work, so it is left behind for the user to enter per person.
                Employees = new List<ServiceReportEmployeeModel>(),
                Parts = quote.Parts.Where(x => !x.IsDeleted).Select(part => new ServiceReportPartModel
                {
                    Id = part.IsAdHockPart ? 999999 : part.PartId,
                    PartId = part.PartId,
                    PartCode = part.PartCode,
                    PartDescription = part.PartDescription,
                    CostPrice = part.CostPrice,
                    SellingPrice = part.SellingPrice,
                    Discount = part.Discount,
                    QTY = part.QTY,
                    Bin = part.Bin,
                    ProductClass = part.ProductClass,
                    SupplierCode = part.SupplierCode,
                    CycleCount = part.CycleCount,
                    QtyOnHand = part.QtyOnHand,
                    IsAdHockPart = part.IsAdHockPart
                }).ToList()
            };
        }

        /// <summary>
        /// Raises a quote from a service report, the other way round from
        /// <see cref="BuildServiceReportFromQuote"/>. Some clients only want the numbers once the
        /// work is done, so the report is written up first and then printed as a quote for them to
        /// sign. That quote is numbered from the same series as any other, listed with the others,
        /// and linked to the report the way a converted quote is. It is a copy of the report, so
        /// printing the report as a quote again rewrites it under the same number; a plain save of
        /// the report leaves it as it was last printed. A report that was converted from a quote
        /// already answers to that quote - the record of what the client accepted - and it is left
        /// exactly as it is.
        /// </summary>
        /// <param name="serviceReport">The report as it was just saved.</param>
        /// <returns>The quote the report goes out under.</returns>
        public async Task<QuoteModel> QuoteServiceReport(ServiceReportModel serviceReport)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();

            var report = await _aeroMechDBContext.ServiceReports
                .Include(x => x.Quote).ThenInclude(x => x!.Labour)
                .Include(x => x.Quote).ThenInclude(x => x!.Parts)
                .Include(x => x.Quote).ThenInclude(x => x!.AdHockParts)
                .SingleAsync(x => x.Id == serviceReport.Id);

            var quote = report.Quote;

            if (quote != null && !quote.CreatedFromServiceReport)
                return await GetQuote(quote.Id);

            var isNew = quote == null;

            if (quote == null)
            {
                quote = new Quote
                {
                    QuoteNumber = await NextQuoteNumber(_aeroMechDBContext),
                    CreatedFromServiceReport = true
                };

                _aeroMechDBContext.Quotes.Add(quote);
                report.Quote = quote;
            }
            else
            {
                // The lines are a copy of the report's, so they are written afresh rather than
                // matched up against what the report held when it was last printed.
                _aeroMechDBContext.QuoteLabour.RemoveRange(quote.Labour);
                _aeroMechDBContext.QuoteParts.RemoveRange(quote.Parts);
                _aeroMechDBContext.QuoteAdHockParts.RemoveRange(quote.AdHockParts);
            }

            // Read through the same translation that prints a report as a quote, so the stored
            // quote and a printed one cannot come out differently.
            var copy = BuildQuoteFromServiceReport(serviceReport);

            quote.QuoteDate = NormalizeDateOnlyToUtc(copy.QuoteDate);
            quote.ClientId = copy.ClientId == 0 ? null : copy.ClientId;
            quote.VehicleId = copy.VehicleId == 0 ? null : copy.VehicleId;
            quote.VehicleHours = copy.VehicleHours;
            quote.ServiceType = copy.ServiceType.ToString();
            quote.Description = copy.Description;
            quote.Instruction = copy.Instruction;
            quote.DetailedServiceReport = copy.DetailedServiceReport;

            quote.Labour.AddRange(copy.Labour.Select(labour => new QuoteLabour
            {
                RateType = labour.RateType,
                Rate = labour.Rate,
                Hours = labour.Hours,
                Discount = labour.Discount
            }));

            foreach (var part in copy.Parts)
            {
                if (part.IsAdHockPart)
                    quote.AdHockParts.Add(NewAdHockPart(part));
                else
                    quote.Parts.Add(NewPart(part));
            }

            report.QuoteNumber = quote.QuoteNumber;

            // Saved first so the entry can name the id the save produced, and inside one
            // transaction so the quote cannot exist without its report pointing at it.
            using var transaction = await _aeroMechDBContext.Database.BeginTransactionAsync();

            await _aeroMechDBContext.SaveChangesAsync();

            var reference = $"AEM{quote.QuoteNumber}";
            var reportReference = $"AEM{report.ServiceReportNumber}";

            _auditService.Record(
                _aeroMechDBContext,
                await _auditService.ResolveUser(),
                AuditArea.Quotes,
                isNew ? AuditAction.Created : AuditAction.Updated,
                nameof(Quote),
                quote.Id,
                reference,
                isNew
                    ? $"Quote {reference} created from service report {reportReference}."
                    : $"Quote {reference} updated from service report {reportReference}.");

            await _aeroMechDBContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // GetServiceReport keeps reports in the memory cache by id, and this one has just been
            // linked to its quote underneath it.
            _memoryCache.Remove(report.Id);

            return await GetQuote(quote.Id);
        }

        /// <summary>
        /// Renders a service report that was converted from a quote on the quote document: the
        /// work actually captured, under the number of the quote it came from. A report written up
        /// first has a quote of its own instead (<see cref="QuoteServiceReport"/>), which prints
        /// like any other quote.
        /// </summary>
        public byte[] DownloadQuoteForServiceReport(ServiceReportModel serviceReport)
        {
            _quoteDocument.quote = BuildQuoteFromServiceReport(serviceReport);

            return Document.Create(_quoteDocument.Compose).GeneratePdf();
        }

        /// <summary>
        /// Reads a service report back as a quote, the other way round from
        /// <see cref="BuildServiceReportFromQuote"/>. Captured labour is per person and a quote
        /// prices per rate type, so the hours are grouped back up - a quote has nowhere to name
        /// who did the work. Both the quote raised from a report and the print of a converted
        /// report are read through here.
        /// </summary>
        private static QuoteModel BuildQuoteFromServiceReport(ServiceReportModel serviceReport)
        {
            var labour = (serviceReport.Employees ?? new())
                .Where(x => !x.IsDeleted && (x.Hours ?? 0) > 0)
                // Two people on the same rate type can still sit on different rates or discounts,
                // so only lines that price identically are allowed to collapse into one.
                .GroupBy(x => (x.RateType, x.Rate, Discount: x.Discount ?? 0))
                .Select(g => new QuoteLabourModel
                {
                    RateType = g.Key.RateType,
                    Rate = g.Key.Rate,
                    Discount = g.Key.Discount,
                    Hours = g.Sum(x => x.Hours ?? 0)
                })
                .OrderBy(x => x.RateType.ToString(), StringComparer.Ordinal)
                .ToList();

            return new QuoteModel
            {
                Id = 0,
                // A report that started as a quote keeps that quote's number on the print.
                QuoteNumber = serviceReport.QuoteNumber,
                QuoteDate = serviceReport.ReportDate,
                ClientId = serviceReport.ClientId,
                Client = serviceReport.Client,
                VehicleId = serviceReport.VehicleId,
                Vehicle = serviceReport.Vehicle,
                VehicleHours = serviceReport.VehicleHours,
                ServiceType = serviceReport.ServiceType,
                Instruction = serviceReport.Instruction,
                DetailedServiceReport = serviceReport.DetailedServiceReport,
                Description = serviceReport.Description,
                ServiceReportId = serviceReport.Id == 0 ? null : serviceReport.Id,
                ServiceReportNumber = serviceReport.ServiceReportNumber == 0 ? null : serviceReport.ServiceReportNumber,
                Labour = labour,
                Parts = (serviceReport.Parts ?? new())
                    .Where(x => !x.IsDeleted)
                    .Select(part => new QuotePartModel
                    {
                        Id = part.Id,
                        PartId = part.PartId,
                        PartCode = part.PartCode,
                        PartDescription = part.PartDescription,
                        CostPrice = part.CostPrice,
                        SellingPrice = part.SellingPrice,
                        Discount = part.Discount,
                        QTY = part.QTY,
                        Bin = part.Bin,
                        ProductClass = part.ProductClass,
                        SupplierCode = part.SupplierCode,
                        CycleCount = part.CycleCount,
                        QtyOnHand = part.QtyOnHand,
                        IsAdHockPart = part.IsAdHockPart
                    }).ToList()
            };
        }

        public double CalculateQuoteTotal(QuoteModel quote)
        {
            var totalLabour = quote.Labour.Where(x => !x.IsDeleted)
                .Sum(x => x.Rate * x.Hours - x.Discount / 100 * (x.Rate * x.Hours));

            var totalParts = quote.Parts.Where(x => !x.IsDeleted)
                .Sum(x => x.CostPrice * x.QTY - x.Discount / 100 * (x.CostPrice * x.QTY));

            return totalLabour + totalParts;
        }

        private static IQueryable<Quote> QuoteWithDetail(AeroMechDBContext context)
            => context.Quotes
                .AsNoTracking()
                .Include(x => x.Parts)
                    .ThenInclude(x => x.Part)
                        .ThenInclude(x => x.Prices)
                .Include(x => x.AdHockParts)
                .Include(x => x.Labour)
                .Include(x => x.Client)
                .Include(x => x.Vehicle)
                .Include(x => x.ServiceReport);

        private static QuotePart NewPart(QuotePartModel part) => new()
        {
            PartId = part.PartId,
            CostPrice = part.CostPrice,
            Discount = part.Discount,
            Qty = part.QTY,
            IsDeleted = false
        };

        private static QuoteAdHockPart NewAdHockPart(QuotePartModel part) => new()
        {
            PartCode = part.PartCode,
            PartDescription = part.PartDescription,
            CostPrice = part.CostPrice,
            Discount = part.Discount,
            Qty = part.QTY,
            IsDeleted = false
        };
    }
}
