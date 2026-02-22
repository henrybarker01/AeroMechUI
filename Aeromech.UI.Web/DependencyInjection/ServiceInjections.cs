using AeroMech.API.Reports;
using AeroMech.UI.Web.Services;

namespace AeroMech.UI.Web.DependencyInjection
{
    public static class ServiceInjections
    {
        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<ClientService, ClientService>();
            services.AddScoped<EmployeeService, EmployeeService>();
            services.AddScoped<PartsService, PartsService>();
            services.AddScoped<VehicleService, VehicleService>();
            services.AddScoped<ServiceReportService, ServiceReportService>();
            services.AddScoped<UserService, UserService>();
            services.AddScoped<LoaderService, LoaderService>();
            services.AddScoped<ConfirmationService, ConfirmationService>();
            services.AddScoped<FieldServiceReport, FieldServiceReport>();
            services.AddScoped<Quote, Quote>();
        }
    }
}