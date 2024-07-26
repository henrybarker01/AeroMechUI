using VMI.UI;
using VMI.UI.Serices; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:80") }); //builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<ClientService, ClientService>();
builder.Services.AddScoped<EmployeeService, EmployeeService>();
builder.Services.AddScoped<PartsService, PartsService>();
builder.Services.AddScoped<VehicleService, VehicleService>();
builder.Services.AddScoped<ServiceReportService, ServiceReportService>();

builder.Services.AddBlazorBootstrap();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();



app.Run();
