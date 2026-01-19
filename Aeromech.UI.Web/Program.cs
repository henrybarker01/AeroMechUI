using AeroMech.API.Reports;
using AeroMech.Areas.Identity;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AeroMech.UI.Web.Extentions;
using AeroMech.UI.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Reflection;
using AeroMech.Areas.Identity;
using AeroMech.UI.Web.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ClientService, ClientService>();
builder.Services.AddScoped<EmployeeService, EmployeeService>();
builder.Services.AddScoped<PartsService, PartsService>();
builder.Services.AddScoped<VehicleService, VehicleService>();
builder.Services.AddTransient<ServiceReportService, ServiceReportService>();
builder.Services.AddScoped<UserService, UserService>();
builder.Services.AddScoped<LoaderService, LoaderService>();
builder.Services.AddSingleton<ConfirmationService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add DbContextFactory for Blazor Server scenarios
builder.Services.AddDbContextFactory<AeroMechDBContext>(options =>
{
    options.UseSqlServer(connectionString);
}, ServiceLifetime.Scoped);

// Add DbContext for Identity and other services that require it
builder.Services.AddDbContext<AeroMechDBContext>(options =>
{
    options.UseSqlServer(connectionString);
}, ServiceLifetime.Scoped);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Configure ASP.NET Core Identity with persistent cookies
builder.Services.AddDefaultIdentity<IdentityUser>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AeroMechDBContext>();

// Configure application cookie for persistent authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages(options =>
{
    // Disable antiforgery validation for the SignIn page
    options.Conventions.ConfigureFilter(new Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryTokenAttribute());
});
builder.Services.AddServerSideBlazor();

// Register the authentication state provider
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

builder.Services.AddScoped<FieldServiceReport, FieldServiceReport>();
builder.Services.AddScoped<Quote, Quote>();

builder.Services.AddBlazorBootstrap();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(AeroMech.Models.AutomapperProfiles.PartsProfile)));

QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// Apply EF Core migrations at startup (abstracted)
app.MigrateDatabase<AeroMechDBContext>();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Culture middleware before routing
app.Use(async (context, next) =>
{
    var culture = CultureInfo.CurrentCulture.Clone() as CultureInfo;
    culture.NumberFormat.CurrencySymbol = "R ";
    CultureInfo.CurrentCulture = culture;
    CultureInfo.CurrentUICulture = culture;

    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
