using Aeromech.UI.Web.Data;
using AeroMech.API.Reports;
using AeroMech.Data.Persistence;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Reflection;
using AeroMech.Areas.Identity;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components.Server;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://84.26.82.48:443/api/") });

builder.Services.AddScoped<ClientService, ClientService>();
builder.Services.AddScoped<EmployeeService, EmployeeService>();
builder.Services.AddScoped<PartsService, PartsService>();
builder.Services.AddScoped<VehicleService, VehicleService>();
builder.Services.AddTransient<ServiceReportService, ServiceReportService>();
builder.Services.AddScoped<UserService, UserService>();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContextFactory<AeroMechDBContext>(options =>
{
    options.UseSqlServer(connectionString);
}, ServiceLifetime.Transient);

//builder.Services.AddDbContext<AuthenticationDbContext>(options =>
//    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<AeroMechDBContext>();

builder.Services.AddScoped<IHostEnvironmentAuthenticationStateProvider>(sp =>
                (ServerAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>()
            );

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
builder.Services.AddScoped<FieldServiceReport, FieldServiceReport>();
builder.Services.AddScoped<Quote, Quote>();

builder.Services.AddBlazorBootstrap();

builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(AeroMech.Models.AutomapperProfiles.PartsProfile)));



QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
