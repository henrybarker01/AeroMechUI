using AeroMech.API.Reports;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using QuestPDF.Fluent;

namespace AeroMech.UI.Web.Services
{
    public class ServiceReportService
    {
        private readonly IMapper _mapper;
        private readonly AeroMechDBContext _aeroMechDBContext;
        private readonly FieldServiceReport _fieldServiceReport;
        public ServiceReportService(AeroMechDBContext context, IMapper mapper, FieldServiceReport fieldServiceReport)
        {
            _aeroMechDBContext = context;
            _mapper = mapper;
            _fieldServiceReport = fieldServiceReport;
        }


        public async Task<int> AddServiceReport(ServiceReportModel serviceReport)
        {
            if (serviceReport.Id == 0)
            {
                Data.Models.ServiceReport sr = _mapper.Map<ServiceReport>(serviceReport);
                sr.Client = null;
                sr.Vehicle = null;
                sr.Employees.ForEach(x => x.Id = 0);
                sr.Parts.ForEach(x => x.Id = 0);
                _aeroMechDBContext.ServiceReports.Add(sr);
                await _aeroMechDBContext.SaveChangesAsync();
                return sr.Id;

            }
            else
            {
                Data.Models.ServiceReport serviceReportToEdit = _aeroMechDBContext.ServiceReports
                .Include(x => x.Parts)
                .Include(r => r.Employees)
                .Single(x => x.Id == serviceReport.Id);

                serviceReportToEdit.ReportDate = serviceReportToEdit.ReportDate;
                //employeeToEdit.IDNumber = employee.IDNumber;
                //employeeToEdit.FirstName = employee.FirstName;
                //employeeToEdit.LastName = employee.LastName;
                //employeeToEdit.Email = employee.Email;
                //employeeToEdit.Title = employee.Title;
                //employeeToEdit.BirthDate = employee.BirthDate;

                if (serviceReportToEdit.Parts == null)
                {
                    //serviceReportToEdit.Parts = new List<Part>();
                }

                //employeeToEdit.Address.AddressLine1 = employee.AddressLine1 ?? "";
                //employeeToEdit.Address.AddressLine2 = employee.AddressLine2 ?? "";
                //employeeToEdit.Address.City = employee.City ?? "";
                //employeeToEdit.Address.PostalCode = employee.PostalCode ?? "";

                await _aeroMechDBContext.SaveChangesAsync();
                return serviceReportToEdit.Id;
            }
        }

        public async Task<ServiceReportModel> GetServiceReport(int Id)
        {
            var serviceReport = await _aeroMechDBContext.ServiceReports
                .Include(a => a.Parts)
                .Include(r => r.Employees)
                .Include(c => c.Client)
                .Include(v => v.Vehicle)
                .SingleAsync(x => x.Id == Id);

            return _mapper.Map<ServiceReportModel>(serviceReport);
        }

        public async Task<byte[]> DownloadServiceReport(int serviceReportId)
        {
            _fieldServiceReport.serviceReport = await _aeroMechDBContext.ServiceReports
               .Include(x => x.Vehicle)
               .Include(x => x.Parts)
                   .ThenInclude(x => x.Part)
               .Include(x => x.Employees)
                   .ThenInclude(x => x.Employee)
               .Include(x => x.Client)
               .FirstAsync(x => x.Id == serviceReportId);

            var pdf = QuestPDF.Fluent.Document.Create(_fieldServiceReport.Compose).GeneratePdf();

            //  var bytes = await System.IO.File.ReadAllBytesAsync($"FieldServiceReport{_fieldServiceReport.serviceReport.ReportNumber}.pdf");
            return pdf;//, "application/pdf", $"FieldServiceReport{_fieldServiceReport.serviceReport.ReportNumber}.pdf");

            // return await _httpClient.GetAsync($"{_configuration.GetValue<string>("ApiUrl")}reports/printServiceReport/{serviceReportId}");
        }

        public async Task<List<ServiceReportModel>> GetRecentServiceReports(DateTime fromDate)
        {
            var serviceReports = await _aeroMechDBContext.ServiceReports
               .Include(x => x.Parts)
               .Include(r => r.Employees)
               .Include(x => x.Client)
               .ThenInclude(x => x.Vehicles)
               .Where(x => x.ReportDate >= fromDate).ToListAsync();
            return _mapper.Map<IEnumerable<ServiceReportModel>>(serviceReports).ToList();
        }

        public double CalculateServiceReportTotal(ServiceReportModel model)
        {
            var totalEmployee = model.Employees.Sum(x => x.Rate * x.Hours - x.Discount / 100 * x.Rate * x.Hours);
            var totalParts = model.Parts.Sum(x => x.CostPrice * x.QTY - x.Discount / 100 * (x.CostPrice * x.QTY));
            return totalEmployee ?? 0 + totalParts;
        }
    }
}
