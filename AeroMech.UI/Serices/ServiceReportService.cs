using AeroMech.Data.Persistence;
using AeroMech.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Json;

namespace AeroMech.UI.Serices
{
    public class ServiceReportService
    {
        HttpClient _httpClient;
        IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly AeroMechDBContext _aeroMechDBContext;

        public ServiceReportService(HttpClient httpClient, IConfiguration configuration, AeroMechDBContext context, IMapper mapper)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _aeroMechDBContext = context;
            _mapper = mapper;
        }

        public async Task<HttpResponseMessage> AddServiceReport(ServiceReportModel serviceReport)
        {
            if (serviceReport.Id == 0)
            {
                return await _httpClient.PostAsJsonAsync<ServiceReportModel>($"{_configuration.GetValue<string>("ApiUrl")}servicereport/add", serviceReport);

            }
            else
            {
                return await _httpClient.PostAsJsonAsync<ServiceReportModel>($"{_configuration.GetValue<string>("ApiUrl")}servicereport/edit", serviceReport);
            }
        }

        public async Task<ServiceReportModel> GetServiceReport(int Id)
        {
            var response = await _httpClient.GetAsync($"{_configuration.GetValue<string>("ApiUrl")}servicereport/{Id}");
            string apiResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ServiceReportModel>(apiResponse);
        }

        public async Task<HttpResponseMessage> DownloadServiceReport(int serviceReportId)
        {
            return await _httpClient.GetAsync($"{_configuration.GetValue<string>("ApiUrl")}reports/printServiceReport/{serviceReportId}");
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
            //var response = await _httpClient.GetAsync($"{_configuration.GetValue<string>("ApiUrl")}servicereport/mostrecent/{fromDate.Date.ToString("dd-MMM-yyyy")}");
            //string apiResponse = await response.Content.ReadAsStringAsync();
            //return JsonConvert.DeserializeObject<List<ServiceReportModel>>(apiResponse);

        }

        public double CalculateServiceReportTotal(ServiceReportModel model)
        {
            var totalEmployee = model.Employees.Sum(x => ((x.Rate * x.Hours) - ((x.Discount / 100) * (x.Rate * x.Hours))));
            var totalParts = model.Parts.Sum(x => (x.CostPrice * x.QTY) - ((x.Discount / 100) * (x.CostPrice * x.QTY)));
            return totalEmployee ?? 0 + totalParts;
        }
    }
}
