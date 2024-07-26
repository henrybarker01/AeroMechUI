using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace AeroMech.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]

	public class ServiceReportController : Controller
	{
		private readonly IMapper _mapper;
		private readonly AeroMechDBContext _aeroMechDBContext;
		public ServiceReportController(AeroMechDBContext context, IMapper mapper)
		{
			_aeroMechDBContext = context;
			_mapper = mapper;
		}

		[HttpGet("servicereports")]
		public async Task<ActionResult<IEnumerable<ServiceReportModel>>> GetAll()
		{
			List<ServiceReport> serviceReports = await _aeroMechDBContext.ServiceReports.Where(x => x.IsDeleted == false)
				.Include(a => a.Parts)
				.Include(r => r.Employees)
				.ToListAsync();
			return Ok(_mapper.Map<IEnumerable<ServiceReportModel>>(serviceReports));
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<ServiceReportModel>> GetServiceReport(int Id)
		{
			var serviceReport = await _aeroMechDBContext.ServiceReports
				.Include(a => a.Parts)
				.Include(r => r.Employees)
				.Include(c => c.Client)
				.Include(v => v.Vehicle)
				.SingleAsync(x => x.Id == Id);

			return Ok(_mapper.Map<ServiceReportModel>(serviceReport));
		}

		[Route("add")]
		[HttpPost(Name = "Add service report")]
		public async Task<ActionResult<int>> Add(ServiceReportModel serviceReport)
		{
			ServiceReport sr = _mapper.Map<ServiceReport>(serviceReport);
			sr.Client = null;
			sr.Vehicle = null;
			sr.Employees.ForEach(x => x.Id = 0);
			sr.Parts.ForEach(x => x.Id = 0);
			_aeroMechDBContext.ServiceReports.Add(sr);
			await _aeroMechDBContext.SaveChangesAsync();
			return Ok(sr.Id);
		}

		[Route("edit")]
		[HttpPost(Name = "Edit service report")]
		public async Task<IActionResult> Edit(ServiceReport serviceReport)
		{
			ServiceReport serviceReportToEdit = _aeroMechDBContext.ServiceReports
				.Include(x => x.Parts)
				.Include(r => r.Employees)
				.Single(x => x.Id == serviceReport.Id);

			serviceReportToEdit.ReportDate = serviceReportToEdit.ReportDate;
			//employeeToEdit.IDNumber = employee.IDNumber;
			//employeeToEdit.FirstName = employee.FirstName;
			//employeeToEdit.LastName = employee.LastName;
			//employeeToEdit.Email = employee.Email;
			//employeeToEdit.Title = employee.Title;
			//employeeToEdit.BirthDate = employee.BirthDate;

			if (serviceReportToEdit.Parts == null)
			{
				//serviceReportToEdit.Parts = new List<Part>();
			}

			//employeeToEdit.Address.AddressLine1 = employee.AddressLine1 ?? "";
			//employeeToEdit.Address.AddressLine2 = employee.AddressLine2 ?? "";
			//employeeToEdit.Address.City = employee.City ?? "";
			//employeeToEdit.Address.PostalCode = employee.PostalCode ?? "";

			await _aeroMechDBContext.SaveChangesAsync();
			return Ok();
		}

		[Route("delete/{id}")]
		[HttpPost(Name = "Delete servicereport")]
		public async Task<IActionResult> Delete(int id)
		{
			var serviceReport = await _aeroMechDBContext.ServiceReports.FindAsync(id);
			serviceReport.IsDeleted = true;
			await _aeroMechDBContext.SaveChangesAsync();
			return new OkResult();
		}

		[Route("mostrecent/{fromDate}")]
		[HttpGet]
		public async Task<ActionResult<IEnumerable<ServiceReportModel>>> GetMostRecent(DateTime fromDate)
		{
			var serviceReports = await _aeroMechDBContext.ServiceReports
				.Include(x => x.Parts)
				.Include(r => r.Employees)
				.Include(x => x.Client)
				.ThenInclude(x => x.Vehicles)
				.Where(x => x.ReportDate >= fromDate).ToListAsync();
			return Ok(_mapper.Map<IEnumerable<ServiceReportModel>>(serviceReports));
		}

	}
}
