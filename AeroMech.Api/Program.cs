
using AeroMech.API.Reports;
using AeroMech.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using System.Reflection;

namespace AeroMech.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(AeroMech.Models.AutomapperProfiles.PartsProfile)));

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    policy =>
                    {
                        policy.AllowAnyMethod();
                        policy.AllowAnyHeader();
                        policy.AllowAnyOrigin();
                    });
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<AeroMechDBContext>(options =>
{
options.UseSqlServer(connectionString);
});

            builder.Services.AddScoped<FieldServiceReport, FieldServiceReport>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}

            QuestPDF.Settings.License = LicenseType.Community;

            app.UseCors();

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
