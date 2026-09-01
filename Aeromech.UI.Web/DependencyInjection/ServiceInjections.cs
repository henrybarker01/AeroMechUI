using AeroMech.API.Reports;
using AeroMech.UI.Web.Services;

namespace AeroMech.UI.Web.DependencyInjection
{
    public static class ServiceInjections
    {
        public static void AddServices(this IServiceCollection services)
        {
            // The audit trail names the signed-in user, and outside a Blazor circuit that name is
            // only reachable through the HTTP context.
            services.AddHttpContextAccessor();

            services.AddScoped<CurrentUserService, CurrentUserService>();
            services.AddScoped<AuditService, AuditService>();
            services.AddScoped<AuditReportService, AuditReportService>();
            services.AddScoped<AuditLogReport, AuditLogReport>();
            services.AddScoped<ClientService, ClientService>();
            services.AddScoped<EmployeeService, EmployeeService>();
            services.AddScoped<PartsService, PartsService>();
            services.AddScoped<VehicleService, VehicleService>();
            services.AddScoped<ServiceReportService, ServiceReportService>();
            services.AddScoped<QuoteService, QuoteService>();
            services.AddScoped<UserService, UserService>();
            services.AddScoped<LoaderService, LoaderService>();
            services.AddScoped<ConfirmationService, ConfirmationService>();
            services.AddScoped<FieldServiceReport, FieldServiceReport>();
            services.AddScoped<QuoteDocument, QuoteDocument>();
            services.AddScoped<TimesheetReport, TimesheetReport>();
            services.AddScoped<TimesheetService, TimesheetService>();
            services.AddScoped<StockReceivingService, StockReceivingService>();
            services.AddScoped<StockTakeService, StockTakeService>();
            services.AddScoped<StockCountSheet, StockCountSheet>();
            services.AddScoped<StockReportService, StockReportService>();
            services.AddScoped<StockMovementReport, StockMovementReport>();
            services.AddScoped<StockValuationReport, StockValuationReport>();
            services.AddScoped<VehicleReportService, VehicleReportService>();
            services.AddScoped<VehicleServiceRecordReport, VehicleServiceRecordReport>();
            services.AddScoped<DashboardService, DashboardService>();
        }
    }
}