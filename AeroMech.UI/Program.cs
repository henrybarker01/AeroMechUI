using AeroMech.Data.Persistence; 
using AeroMech.UI.Serices;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using AeroMech.UI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<ClientService, ClientService>();
builder.Services.AddScoped<EmployeeService, EmployeeService>();
builder.Services.AddScoped<PartsService, PartsService>();
builder.Services.AddScoped<VehicleService, VehicleService>();
builder.Services.AddScoped<ServiceReportService, ServiceReportService>();


builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(AeroMech.Models.AutomapperProfiles.PartsProfile)));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AeroMechDBContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(AeroMech.Models.AutomapperProfiles.PartsProfile)));


//QuestPDF.Settings.License = LicenseType.Community;

//builder.Services.AddTransient();

builder.Services.AddBlazorBootstrap();

await builder.Build().RunAsync();