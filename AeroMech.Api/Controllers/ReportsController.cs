using AeroMech.API.Reports;
using AeroMech.Data.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;


namespace AeroMech.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : Controller
    {
        private readonly FieldServiceReport _fieldServiceReport;
        private readonly AeroMechDBContext _aeroMechDBContext;
        public ReportsController(FieldServiceReport fieldServiceReport, AeroMechDBContext aeroMechDBContext)
        {
            _fieldServiceReport = fieldServiceReport;
            _aeroMechDBContext = aeroMechDBContext;
        }

        [Route("printServiceReport/{serviceReportId}")]
        public async Task<ActionResult> PrintServiceReport(int serviceReportId)
        {
            _fieldServiceReport.serviceReport = await _aeroMechDBContext.ServiceReports
                .Include(x => x.Vehicle)
                .Include(x => x.Parts)
                    .ThenInclude(x => x.Part)
                .Include(x => x.Employees)
                    .ThenInclude(x => x.Employee)
                .Include(x => x.Client)
                .FirstAsync(x => x.Id == serviceReportId);

            var pdf = Document.Create(_fieldServiceReport.Compose).GeneratePdf();

          //  var bytes = await System.IO.File.ReadAllBytesAsync($"FieldServiceReport{_fieldServiceReport.serviceReport.ReportNumber}.pdf");
            return File(pdf, "application/pdf", $"FieldServiceReport-AEM{_fieldServiceReport.serviceReport.Id}.pdf");
        }
    }
}
