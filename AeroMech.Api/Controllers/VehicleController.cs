using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class VehicleController : Controller
	{
		private readonly IMapper _mapper;
		private readonly AeroMechDBContext _aeroMechDBContext;
		public VehicleController(AeroMechDBContext context, IMapper mapper)
		{
			_aeroMechDBContext = context;
			_mapper = mapper;
		}

		[HttpGet("vehicles/{clientId}")]
		public async Task<ActionResult<IEnumerable<VehicleModel>>> GetAll(int clientId)
		{
			List<Vehicle> vehicles = await _aeroMechDBContext.Vehicles.Where(x => x.IsDeleted == false && x.ClientId == clientId)
				.ToListAsync();
			return Ok(_mapper.Map<IEnumerable<VehicleModel>>(vehicles));
		}

		[Route("add")]
		[HttpPost(Name = "Add Vehicle")]
		public async Task<ActionResult<int>> Add(VehicleModel vehicle)
		{
			Vehicle vhl = _mapper.Map<Vehicle>(vehicle);
			_aeroMechDBContext.Vehicles.Add(vhl);
			await _aeroMechDBContext.SaveChangesAsync();
			return Ok(vhl.Id);
		}

		[Route("edit")]
		[HttpPost(Name = "Edit vehicle")]
		public async Task<IActionResult> Edit(VehicleModel vehicle)
		{
			Vehicle vehicleToEdit = _aeroMechDBContext.Vehicles
				//.Include(x => x.Prices)
				.Single(x => x.Id == vehicle.Id);

			//partToEdit.PartCode = part.PartCode;
			//partToEdit.PartDescription = part.PartDescription;
			//partToEdit.Bin = part.Bin;
			//partToEdit.CycleCount = part.CycleCount;
			//partToEdit.Warehouse = _mapper.Map<Warehouse>(part.Warehouse);
			//partToEdit.SupplierCode = part.SupplierCode;
			//partToEdit.QtyOnHand = part.QtyOnHand;
			//partToEdit.ProductClass = part.ProductClass;

			//if (partToEdit.Prices == null || partToEdit.Prices.Count == 0)
			//{
			//	partToEdit.Prices = new List<PartPrice>() { new PartPrice() {
			//		CostPrice = part.CostPrice,
			//		EffectiveDate = DateTime.Now,
			//		IsDeleted = false,
			//		SellingPrice = 0
			//		}};
			//}
			//else
			//{
			//	partToEdit.Prices.First().CostPrice = part.CostPrice;
			//}

			await _aeroMechDBContext.SaveChangesAsync();
			return Ok();
		}

		[Route("delete/{id}")]
		[HttpPost(Name = "Delete vehicle")]
		public async Task<IActionResult> Delete(int id)
		{
			var part = await _aeroMechDBContext.Vehicles.FindAsync(id);
			part.IsDeleted = true;
			await _aeroMechDBContext.SaveChangesAsync();
			return new OkResult();
		}
	}
}
