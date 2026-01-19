using AeroMech.Backup;
using Microsoft.Extensions.Hosting;


var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService();

builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection("Backup"));
builder.Services.AddSingleton<IDatabaseBackup, DatabaseBackup>();

builder.Services.AddHostedService<Worker>(); 

var host = builder.Build();
host.Run();
