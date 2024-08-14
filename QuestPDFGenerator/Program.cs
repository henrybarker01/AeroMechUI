
using AeroMech.API.Reports;
using AeroMech.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Previewer;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

//Document.Create(container =>
//{
//    container.Page(page =>
//    {
//        page.Size(PageSizes.A4);
//        page.Header().Text("Hello world!!!").Bold().FontSize(32);
//    });
//}).ShowInPreviewer();

var _quote = new Quote();

using (var _aeroMechDBContext = new AeroMechDBContext()) 
{
    _quote.serviceReport = await _aeroMechDBContext.ServiceReports
         .Include(x => x.Vehicle)
         .Include(x => x.Parts)
             .ThenInclude(x => x.Part)
         .Include(x => x.Employees)
             .ThenInclude(x => x.Employee)
         .Include(x => x.Client)
         .FirstAsync(x => x.Id == 1);
}

Document.Create(_quote.Compose).ShowInPreviewer();

//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//builder.Services.AddDbContextFactory<AeroMechDBContext>(options =>
//{
//    options.UseSqlServer(connectionString);
//}, ServiceLifetime.Transient);


//

//return 