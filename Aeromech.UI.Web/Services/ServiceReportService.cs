using AeroMech.API.Reports;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AeroMech.Models.Enums;
using AeroMech.UI.Web.Pages.Employee;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Fluent;

namespace AeroMech.UI.Web.Services
{
    public class ServiceReportService
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly FieldServiceReport _fieldServiceReport;
        private readonly Quote _quote;
        private readonly IMemoryCache _memoryCache;

        public ServiceReportService(IDbContextFactory<AeroMechDBContext> contextFactory, IMapper mapper, FieldServiceReport fieldServiceReport, Quote quote, IMemoryCache memoryCache)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
            _fieldServiceReport = fieldServiceReport;
            _quote = quote;
            _memoryCache = memoryCache;
        }

        public async Task<int> AddServiceReport(ServiceReportModel serviceReport, bool isQuote)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            ServiceReport sr = _mapper.Map<ServiceReport>(serviceReport);

            if (serviceReport.VehicleId > 0)
            {
                var vehicle = await _aeroMechDBContext.Vehicles.SingleAsync(x => x.Id == serviceReport.VehicleId);
                vehicle.EngineHours = serviceReport.VehicleHours;
                sr.JobNumber = vehicle.JobNumber;
            }

            sr.Client = null;
            sr.Vehicle = null;
            sr.Employees.ForEach(x => x.Id = 0);

            sr.AdHockParts = new List<ServiceReportAdHockPart>();

            foreach (var part in serviceReport.Parts.ToList())
            {
                if (part.IsAdHockPart)
                {
                    sr.AdHockParts.Add(new ServiceReportAdHockPart()
                    {
                        Id = 0,
                        CostPrice = Convert.ToDouble(part.CostPrice),
                        Discount = part.Discount,
                        IsDeleted = false,
                        PartDescription = part.PartDescription,
                        PartCode = part.PartCode,
                        Qty = part.QTY
                    });

                    sr.Parts.Remove(sr.Parts.First(x => x.Id == part.Id));
                }
                else
                {
                    var adjustment = new StockAdjustment()
                    {
                        PartId = part.Id,
                        AdjustementDate = DateTime.Now,
                        WarehouseId = 1,
                        QTY = part.QTY * -1,
                        AdjustedById = new Guid(),
                        StockAdjustmentType = StockAdjustmentType.ServiceReport
                    };
                    _aeroMechDBContext.StockAdjustment.Add(adjustment);

                    var partToUpdate = await _aeroMechDBContext.Parts.SingleAsync(x => x.Id == part.Id);
                    partToUpdate.QtyOnHand = partToUpdate.QtyOnHand - part.QTY;
                }
            }

            sr.Parts.ForEach(x => x.Id = 0);

            sr.ServiceReportNumber = (await _aeroMechDBContext.ServiceReports.MaxAsync(x => (int?)x.ServiceReportNumber) ?? 0) + 1;

            if (isQuote && sr.QuoteNumber == 0)
            {
                sr.QuoteNumber = (await _aeroMechDBContext.ServiceReports.MaxAsync(x => (int?)x.QuoteNumber) ?? 0) + 1;
            }

            if (serviceReport.VehicleId == 0)
            {
                sr.VehicleId = null;
            }

            if (serviceReport.ClientId == 0)
            {
                sr.ClientId = null;
            }

            _aeroMechDBContext.ServiceReports.Add(sr);
            await _aeroMechDBContext.SaveChangesAsync();

            foreach (var employee in sr.Employees)
            {
                var actualEmployee = await _aeroMechDBContext.Employees.AsNoTracking().SingleAsync(x => x.Id == employee.EmployeeId);
                employee.Employee = actualEmployee;
            }

            _memoryCache.Set(sr.Id, _mapper.Map<ServiceReportModel>(sr), TimeSpan.FromMinutes(30));

            return sr.Id;
        }

        public async Task<int> EditServiceReport(ServiceReportModel serviceReport, bool isQuote)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            ServiceReport serviceReportToEdit = await _aeroMechDBContext.ServiceReports
                .Include(x => x.Vehicle)
                .Include(x => x.Client)
                .Include(x => x.Parts)
                .Include(x => x.AdHockParts)
                .Include(r => r.Employees)
                .SingleAsync(x => x.Id == serviceReport.Id);

            serviceReportToEdit.ReportDate = serviceReport.ReportDate;
            serviceReportToEdit.DetailedServiceReport = serviceReport.DetailedServiceReport;
            serviceReportToEdit.VehicleHours = serviceReport.VehicleHours;
            serviceReportToEdit.VehicleId = serviceReport.VehicleId;
            serviceReportToEdit.ClientId = serviceReport.ClientId;
            serviceReportToEdit.Description = serviceReport.Description;
            serviceReportToEdit.Instruction = serviceReport.Instruction;
            serviceReportToEdit.IsComplete = serviceReport.IsComplete;
            serviceReportToEdit.JobNumber = serviceReport.Vehicle.JobNumber;
            serviceReportToEdit.QuoteNumber = serviceReport.QuoteNumber;
            serviceReportToEdit.SalesOrderNumber = serviceReport.SalesOrderNumber;
            serviceReportToEdit.ServiceType = serviceReport.ServiceType.ToString();

            if (serviceReportToEdit.Parts == null)
            {
                serviceReportToEdit.Parts = new List<ServiceReportPart>();
            }

            foreach (var part in serviceReport.Parts)
            {
                if (part.IsAdHockPart)
                {
                    if (serviceReportToEdit.AdHockParts.Any(x => x.Id == part.Id))
                    {
                        var p = serviceReportToEdit.AdHockParts.Single(x => x.Id == part.Id);
                        p.Qty = part.QTY;
                        p.PartCode = part.PartCode;
                        p.PartDescription = part.PartDescription;
                        p.CostPrice = Convert.ToDouble(part.CostPrice);
                        p.Discount = part.Discount;
                        p.Id = part.Id;
                        p.IsDeleted = part.IsDeleted;
                        p.ServiceReportId = serviceReportToEdit.Id;
                    }
                    else
                    {
                        serviceReportToEdit.AdHockParts.Add(new ServiceReportAdHockPart()
                        {
                            Qty = part.QTY,
                            PartDescription = part.PartDescription,
                            PartCode = part.PartCode,
                            CostPrice = Convert.ToDouble(part.CostPrice),
                            Discount = part.Discount,
                            IsDeleted = false,
                            ServiceReportId = serviceReportToEdit.Id,
                        });
                    }
                }
                else
                {
                    if (serviceReportToEdit.Parts.Any(x => x.PartId == part.PartId))
                    {
                        var p = serviceReportToEdit.Parts.Single(x => x.PartId == part.PartId);

                        if (p.Qty != part.QTY)
                        {
                            _aeroMechDBContext.StockAdjustment.Add(new StockAdjustment()
                            {
                                PartId = p.PartId,
                                AdjustementDate = DateTime.Now,
                                WarehouseId = 1,
                                QTY = p.Qty,
                                AdjustedById = new Guid(),
                                StockAdjustmentType = StockAdjustmentType.ServiceReportReversal
                            });

                            var partToUpdate = await _aeroMechDBContext.Parts.SingleAsync(x => x.Id == p.PartId);
                            partToUpdate.QtyOnHand = partToUpdate.QtyOnHand + p.Qty;

                            _aeroMechDBContext.StockAdjustment.Add(new StockAdjustment()
                            {
                                PartId = p.PartId,
                                AdjustementDate = DateTime.Now,
                                WarehouseId = 1,
                                QTY = part.QTY * -1,
                                AdjustedById = new Guid(),
                                StockAdjustmentType = StockAdjustmentType.ServiceReportEdit
                            });

                            partToUpdate.QtyOnHand = partToUpdate.QtyOnHand - part.QTY;
                        }
                        else if (part.IsDeleted && !p.IsDeleted)
                        {
                            _aeroMechDBContext.StockAdjustment.Add(new StockAdjustment()
                            {
                                PartId = p.PartId,
                                AdjustementDate = DateTime.Now,
                                WarehouseId = 1,
                                QTY = part.QTY,
                                AdjustedById = new Guid(),
                                StockAdjustmentType = StockAdjustmentType.ServiceReportReversal
                            });
                            var partToUpdate = await _aeroMechDBContext.Parts.SingleAsync(x => x.Id == p.PartId);
                            partToUpdate.QtyOnHand = partToUpdate.QtyOnHand + part.QTY;
                        }

                        p.Qty = part.QTY;
                        p.Discount = part.Discount;
                        p.CostPrice = Convert.ToDouble(part.CostPrice);
                        p.IsDeleted = part.IsDeleted;
                    }
                    else
                    {
                        serviceReportToEdit.Parts.Add(new ServiceReportPart()
                        {
                            Qty = part.QTY,
                            PartId = part.Id,
                            CostPrice = Convert.ToDouble(part.CostPrice),
                            Discount = part.Discount,
                            IsDeleted = false,
                            ServiceReportId = serviceReportToEdit.Id,
                        });

                        _aeroMechDBContext.StockAdjustment.Add(new StockAdjustment()
                        {
                            PartId = part.Id,
                            AdjustementDate = DateTime.Now,
                            WarehouseId = 1,
                            QTY = part.QTY * -1,
                            AdjustedById = new Guid(),
                            StockAdjustmentType = StockAdjustmentType.ServiceReport
                        });

                        var partToUpdate = await _aeroMechDBContext.Parts.SingleAsync(x => x.Id == part.Id);
                        partToUpdate.QtyOnHand = partToUpdate.QtyOnHand - part.QTY;
                    }
                }
            }

            if (serviceReportToEdit.Employees == null)
            {
                serviceReportToEdit.Employees = new List<ServiceReportEmployee>();
            }

            foreach (var employee in serviceReport.Employees)
            {
                if (employee.Id != 0 && serviceReportToEdit.Employees.Any(x => x.Id == employee.Id))
                {
                    var ee = serviceReportToEdit.Employees.Single(x => x.Id == employee.Id);
                    ee.Rate = employee.Rate;
                    ee.RateType = employee.RateType;
                    ee.Hours = employee.Hours ?? 0;
                    ee.Discount = employee.Discount ?? 0;
                    ee.DutyDate = employee.DutyDate;
                    ee.IsDeleted = employee.IsDeleted;
                }
                else
                {
                    var employeeToAdd = _mapper.Map<ServiceReportEmployeeModel, ServiceReportEmployee>(employee);
                    employeeToAdd.Id = 0;
                    employeeToAdd.EmployeeId = employee.EmployeeId;
                    employeeToAdd.ServiceReportId = serviceReportToEdit.Id;

                    serviceReportToEdit.Employees.Add(employeeToAdd);
                }
            }

            if (isQuote && serviceReportToEdit.QuoteNumber == 0)
            {
                serviceReportToEdit.QuoteNumber = (await _aeroMechDBContext.ServiceReports.MaxAsync(x => (int?)x.QuoteNumber) ?? 0) + 1;
            }

            await _aeroMechDBContext.SaveChangesAsync();

            foreach (var employee in serviceReportToEdit.Employees)
            {
                var actualEmployee = await _aeroMechDBContext.Employees.AsNoTracking().SingleAsync(x => x.Id == employee.EmployeeId);
                employee.Employee = actualEmployee;
            }
            
            foreach (var part in serviceReportToEdit.Parts)
            {
                var actualPart = await _aeroMechDBContext.Parts.AsNoTracking().SingleAsync(x => x.Id == part.PartId);
                part.Part = actualPart;
            }
            
            _memoryCache.Set(serviceReport.Id, _mapper.Map<ServiceReportModel>(serviceReportToEdit), TimeSpan.FromMinutes(30));

            return serviceReportToEdit.Id;
        }

        public async Task<ServiceReportModel> GetServiceReport(int Id)
        {
            if (!_memoryCache.TryGetValue(Id, out ServiceReportModel serviceReportModel))
            {
                using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
                
                var serviceReport = await _aeroMechDBContext.ServiceReports
                    .AsNoTracking()
                    .Include(a => a.Parts)
                        .ThenInclude(p => p.Part)
                            .ThenInclude(pp => pp.Prices)
                    .Include(a => a.AdHockParts)
                    .Include(r => r.Employees)
                        .ThenInclude(e => e.Employee)
                    .Include(c => c.Client)
                    .Include(v => v.Vehicle)
                    .SingleAsync(x => x.Id == Id);

                serviceReportModel = _mapper.Map<ServiceReportModel>(serviceReport);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                _memoryCache.Set(Id, serviceReportModel, cacheEntryOptions);
            }

            return serviceReportModel;
        }
        
        public async Task<byte[]> DownloadServiceReport(int serviceReportId)
        {
            if (!_memoryCache.TryGetValue(serviceReportId, out ServiceReportModel serviceReportModel))
            {
                using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
                
                var serviceResport = await _aeroMechDBContext.ServiceReports
                .AsNoTracking()
               .Include(x => x.Vehicle)
               .Include(x => x.Parts)
                   .ThenInclude(x => x.Part)
                .Include(x => x.AdHockParts)
               .Include(x => x.Employees)
                   .ThenInclude(x => x.Employee)
               .Include(x => x.Client)
               .FirstAsync(x => x.Id == serviceReportId);

                serviceReportModel = _mapper.Map<ServiceReportModel>(serviceResport);

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                _memoryCache.Set(serviceReportId, serviceReportModel, cacheEntryOptions);
            }

            _fieldServiceReport.serviceReport = serviceReportModel;

            return Document.Create(_fieldServiceReport.Compose).GeneratePdf();
        }

        public async Task<byte[]> DownloadQuote(int serviceReportId)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            var serviceResport = await _aeroMechDBContext.ServiceReports
                 .AsNoTracking()
           .Include(x => x.Vehicle)
           .Include(x => x.Parts)
               .ThenInclude(x => x.Part)
               .Include(x => x.AdHockParts)
           .Include(x => x.Employees)
               .ThenInclude(x => x.Employee)
           .Include(x => x.Client)
           .FirstAsync(x => x.Id == serviceReportId);

            _quote.serviceReport = _mapper.Map<ServiceReportModel>(serviceResport);

            return Document.Create(_quote.Compose).GeneratePdf();
        }

        public async Task<List<ServiceReportModel>> GetRecentServiceReports(DateTime fromDate = default)
        {
            if (fromDate == default)
                fromDate = DateTime.MinValue;

            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            var serviceReports = await _aeroMechDBContext.ServiceReports
                 .AsNoTracking()
                 .Include(x => x.Vehicle)
                 .Include(x => x.Parts)
                 .Include(x => x.AdHockParts)
                 .Include(r => r.Employees)
                 .Include(x => x.Client)
                 .ThenInclude(x => x.Vehicles)
                 .Where(x => x.ReportDate >= fromDate && x.Client.IsDeleted == false)
                 .OrderByDescending(x => x.ReportDate)
                 .ToListAsync();
            return _mapper.Map<IEnumerable<ServiceReportModel>>(serviceReports).ToList();
        }

        public async Task<List<ServiceReportModel>> GetRecentQuotes(DateTime fromDate = default)
        {
            if (fromDate == default)
                fromDate = DateTime.MinValue;

            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            var serviceReports = await _aeroMechDBContext.ServiceReports
                 .AsNoTracking()
               .Include(x => x.Vehicle)
               .Include(x => x.Parts)
               .Include(x => x.AdHockParts)
               .Include(r => r.Employees)
               .Include(x => x.Client)
               .ThenInclude(x => x.Vehicles)
               .Where(x => x.QuoteNumber > 0 && x.ReportDate >= fromDate)
               .OrderByDescending(x => x.ReportDate)
               .ToListAsync();
            return _mapper.Map<IEnumerable<ServiceReportModel>>(serviceReports).ToList();
        }

        public double CalculateServiceReportTotal(ServiceReportModel model)
        {
            var totalEmployee = model.Employees.Where(x => !x.IsDeleted).Sum(x => x.Rate * x.Hours - x.Discount / 100 * x.Rate * x.Hours);
            var totalParts = model.Parts.Where(x => !x.IsDeleted).Sum(x => Convert.ToDouble(x.CostPrice) * x.QTY - x.Discount / 100 * (Convert.ToDouble(x.CostPrice) * x.QTY));
            return (totalEmployee ?? 0) + totalParts;
        }
    }
}
