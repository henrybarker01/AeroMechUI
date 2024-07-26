using AeroMech.UI.Serices;
using AeroMech.Web.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace AeroMech.Web
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			////builder.RootComponents.Add<App>("#app");
			builder.RootComponents.Add<HeadOutlet>("head::after");

			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

			builder.Services.AddScoped<ClientService, ClientService>();
			builder.Services.AddScoped<EmployeeService, EmployeeService>();
			builder.Services.AddScoped<PartsService, PartsService>();
			builder.Services.AddScoped<VehicleService, VehicleService>();
			builder.Services.AddScoped<ServiceReportService, ServiceReportService>();

			//builder.Services.AddTransient();

			builder.Services.AddBlazorBootstrap();

			await builder.Build().RunAsync();
			//	var builder = WebApplication.CreateBuilder(args);


			//	builder.Services.AddScoped<ClientService, ClientService>();
			//	builder.Services.AddScoped<EmployeeService, EmployeeService>();
			//	builder.Services.AddScoped<PartsService, PartsService>();
			//	builder.Services.AddScoped<VehicleService, VehicleService>();
			//	builder.Services.AddScoped<ServiceReportService, ServiceReportService>();
			//	builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://84.26.82.48:443/API/") });

			//	// Add services to the container.
			//	builder.Services.AddRazorComponents()
			//		.AddInteractiveServerComponents();

			//	var app = builder.Build();

			//	// Configure the HTTP request pipeline.
			//	if (!app.Environment.IsDevelopment())
			//	{
			//		app.UseExceptionHandler("/Error");
			//		// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			//		app.UseHsts();
			//	}

			//	app.UseHttpsRedirection();

			//	app.UseStaticFiles();
			//	app.UseAntiforgery();

			//	app.MapRazorComponents<App>()
			//		.AddInteractiveServerRenderMode();

			//	app.Run();
			//}
		}
	}
}
