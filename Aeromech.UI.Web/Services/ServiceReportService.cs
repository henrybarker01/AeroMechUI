using AeroMech.API.Reports;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AeroMech.UI.Web.Services
{
    public class ServiceReportService
    {
        private readonly IMapper _mapper;
        private readonly AeroMechDBContext _aeroMechDBContext;
        private readonly FieldServiceReport _fieldServiceReport;
        private readonly Quote _quote;

        public ServiceReportService(AeroMechDBContext context, IMapper mapper, FieldServiceReport fieldServiceReport, Quote quote)
        {
            _aeroMechDBContext = context;
            _mapper = mapper;
            _fieldServiceReport = fieldServiceReport;
            _quote = quote;
        }

        public async Task<int> AddServiceReport(ServiceReportModel serviceReport, bool isQuote)
        {
            if (serviceReport.Id == 0)
            {
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

                serviceReport.Parts.ForEach(part =>
                 {
                     if (part.IsAdHockPart)
                     {
                         sr.AdHockParts.Add(new ServiceReportAdHockPart()
                         {
                             Id = 0,
                             CostPrice = part.CostPrice,
                             Discount = part.Discount,
                             IsDeleted = false,
                             PartDescription = part.PartDescription,
                             PartCode = part.PartCode,
                             Qty = part.QTY
                         });

                         sr.Parts.Remove(sr.Parts.First(x => x.Id == part.Id));
                     }
                 });

                sr.Parts.ForEach(x => x.Id = 0);

                if (isQuote)
                {
                    sr.QuoteNumber = (await _aeroMechDBContext.ServiceReports.MaxAsync(x => x.QuoteNumber)) + 1;
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
                return sr.Id;

            }
            else
            {
                ServiceReport serviceReportToEdit = _aeroMechDBContext.ServiceReports
                .Include(x => x.Parts)
                .Include(x => x.AdHockParts)
                .Include(r => r.Employees)
                .Single(x => x.Id == serviceReport.Id);

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

                serviceReport.Parts.ForEach(part =>
                {
                    if (part.IsAdHockPart)
                    {
                        if (serviceReportToEdit.AdHockParts.Any(x => x.Id == part.Id))
                        {
                            var p = serviceReportToEdit.AdHockParts.Single(x => x.Id == part.Id);
                            p.Qty = part.QTY;
                            p.PartCode = part.PartCode;
                            p.PartDescription = part.PartDescription;
                            p.CostPrice = part.CostPrice;
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
                                CostPrice = part.CostPrice,
                                Discount = part.Discount,
                                IsDeleted = false,
                                ServiceReportId = serviceReportToEdit.Id,
                            });
                        }
                    }
                    else
                    {
                        if (serviceReportToEdit.Parts.Any(x => x.Id == part.Id))
                        {
                            var p = serviceReportToEdit.Parts.Single(x => x.Id == part.Id);
                            p.Qty = part.QTY;
                            p.Discount = part.Discount;
                            p.Id = part.Id;
                            p.CostPrice = part.CostPrice;
                            p.IsDeleted = part.IsDeleted;
                        }
                        else
                        {
                            serviceReportToEdit.Parts.Add(new ServiceReportPart()
                            {
                                Qty = part.QTY,
                                PartId = part.Id,
                                CostPrice = part.CostPrice,
                                Discount = part.Discount,
                                IsDeleted = false,
                                ServiceReportId = serviceReportToEdit.Id,
                            });
                        }
                    }
                });


                if (serviceReportToEdit.Employees == null)
                {
                    serviceReportToEdit.Employees = new List<ServiceReportEmployee>();
                }

                //serviceReportToEdit.Employees.Where(e => !serviceReport.Employees.Any(x => x.Id == e.Id)).Select(x=>x.IsDeleted == true);

                serviceReport.Employees.ForEach(employee =>
                {
                    if (serviceReportToEdit.Employees.Any(x => x.Id == employee.Id))
                    {
                        var ee = serviceReportToEdit.Employees.Single(x => x.Id == employee.Id);
                        ee.Rate = employee.Rate ?? 0;
                        ee.Discount = employee.Discount ?? 0;
                        ee.DutyDate = employee.DutyDate;
                        ee.IsDeleted = employee.IsDeleted;
                    }
                    else
                    {
                        var employeeToAdd = _mapper.Map<ServiceReportEmployeeModel, ServiceReportEmployee>(employee);
                        employeeToAdd.Id = 0;
                        employeeToAdd.ServiceReportId = serviceReportToEdit.Id;

                        serviceReportToEdit.Employees.Add(employeeToAdd);
                    }
                });

                if (isQuote)
                {
                    serviceReportToEdit.QuoteNumber = (await _aeroMechDBContext.ServiceReports.MaxAsync(x => x.QuoteNumber)) + 1;
                }

                await _aeroMechDBContext.SaveChangesAsync();
                return serviceReportToEdit.Id;
            }
        }

        public async Task<ServiceReportModel> GetServiceReport(int Id)
        {
            var serviceReport = await _aeroMechDBContext.ServiceReports
                .Include(a => a.Parts)
                .ThenInclude(p => p.Part)
                .ThenInclude(pp => pp.Prices)
                .Include(a => a.AdHockParts)
                .Include(r => r.Employees)
                .ThenInclude(e => e.Employee)
                .Include(c => c.Client)
                .Include(v => v.Vehicle)
                .SingleAsync(x => x.Id == Id);

            return _mapper.Map<ServiceReportModel>(serviceReport);
        }

        public async Task<byte[]> DownloadServiceReport(int serviceReportId)
        {
            var serviceResport = await _aeroMechDBContext.ServiceReports
               .Include(x => x.Vehicle)
               .Include(x => x.Parts)
                   .ThenInclude(x => x.Part)
                .Include(x => x.AdHockParts)
               .Include(x => x.Employees)
                   .ThenInclude(x => x.Employee)
               .Include(x => x.Client)
               .FirstAsync(x => x.Id == serviceReportId);

            _fieldServiceReport.serviceReport =
                _mapper.Map<ServiceReportModel>(serviceResport);

            return Document.Create(_fieldServiceReport.Compose).GeneratePdf();
        }

        public async Task<byte[]> DownloadQuote(int serviceReportId)
        {
            var serviceResport = await _aeroMechDBContext.ServiceReports
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

        public async Task<List<ServiceReportModel>> GetRecentServiceReports(DateTime fromDate)
        {
            var serviceReports = await _aeroMechDBContext.ServiceReports
               .Include(x => x.Parts)
               .Include(x => x.AdHockParts)
               .Include(r => r.Employees)
               .Include(x => x.Client)
               .ThenInclude(x => x.Vehicles)
               .Where(x => x.ReportDate >= fromDate).ToListAsync();
            return _mapper.Map<IEnumerable<ServiceReportModel>>(serviceReports).ToList();
        }

        public async Task<List<ServiceReportModel>> GetRecentQuotes(DateTime fromDate)
        {
            var serviceReports = await _aeroMechDBContext.ServiceReports
               .Include(x => x.Parts)
               .Include(x=>x.AdHockParts)
               .Include(r => r.Employees)
               .Include(x => x.Client)
               .ThenInclude(x => x.Vehicles)
               .Where(x => x.QuoteNumber > 0 && x.ReportDate >= fromDate && x.IsComplete).ToListAsync();
            return _mapper.Map<IEnumerable<ServiceReportModel>>(serviceReports).ToList();
        }

        public double CalculateServiceReportTotal(ServiceReportModel model)
        {
            var totalEmployee = model.Employees.Where(x => !x.IsDeleted).Sum(x => x.Rate * x.Hours - x.Discount / 100 * x.Rate * x.Hours);
            var totalParts = model.Parts.Where(x => !x.IsDeleted).Sum(x => x.CostPrice * x.QTY - x.Discount / 100 * (x.CostPrice * x.QTY)); 
            return (totalEmployee ?? 0) + totalParts;
        }
    }
}
