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
using System.Globalization;

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
builder.Services.AddAuthentication("Cookies")
    .AddCookie(options =>
    {
        options.LoginPath = "/login";  // Redirect to login page if not authenticated
        options.ExpireTimeSpan = TimeSpan.FromDays(30);  // Cookie expiration time
        options.SlidingExpiration = true;  // Reset expiration on activity
    });


QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}



app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();
app.UseStaticFiles();
//app.UseAuthorization();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Use(async (context, next) =>
{
	var culture = CultureInfo.CurrentCulture.Clone() as CultureInfo;// Set user culture here
    culture.NumberFormat.CurrencySymbol = "R ";
	//culture.DateTimeFormat.ShortDatePattern = "dd-yyyy-m";
	CultureInfo.CurrentCulture = culture;
	CultureInfo.CurrentUICulture = culture;

	// Call the next delegate/middleware in the pipeline
	await next();
});


app.Run();
