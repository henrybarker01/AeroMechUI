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
	public class PartController : Controller
	{
		private readonly IMapper _mapper;
		private readonly AeroMechDBContext _aeroMechDBContext;
		public PartController(AeroMechDBContext context, IMapper mapper)
		{
			_aeroMechDBContext = context;
			_mapper = mapper;
		}

		[HttpGet("parts")]
		public async Task<ActionResult<IEnumerable<PartModel>>> GetAll()
		{
			List<Part> parts = await _aeroMechDBContext.Parts.Where(x => x.IsDeleted == false)
				.Include(a => a.Warehouse)
				.Include(p => p.Prices)
				.ToListAsync();
			return Ok(_mapper.Map<IEnumerable<PartModel>>(parts));
		}

		[Route("add")]
		[HttpPost(Name = "Add part")]
		public async Task<ActionResult<int>> Add(PartModel part)
		{
			Part prt = _mapper.Map<Part>(part);
			prt.Prices = new List<PartPrice>() { new PartPrice() {
				CostPrice = Convert.ToDouble(part.CostPrice),
				EffectiveDate = DateTime.Now,
				IsDeleted = false,
				SellingPrice = 0
			}};

			_aeroMechDBContext.Parts.Add(prt);
			await _aeroMechDBContext.SaveChangesAsync();
			return Ok(prt.Id);
		}

		[Route("edit")]
		[HttpPost(Name = "Edit part")]
		public async Task<IActionResult> Edit(PartModel part)
		{
			Part partToEdit = _aeroMechDBContext.Parts
				.Include(x => x.Prices)
				.Single(x => x.Id == part.Id);

			partToEdit.PartCode = part.PartCode;
			partToEdit.PartDescription = part.PartDescription;
			partToEdit.Bin = part.Bin;
			partToEdit.CycleCount = part.CycleCount;
			partToEdit.Warehouse = _mapper.Map<Warehouse>(part.Warehouse);
			partToEdit.SupplierCode = part.SupplierCode;
			partToEdit.QtyOnHand = part.QtyOnHand;
			partToEdit.ProductClass = part.ProductClass;

			if (partToEdit.Prices == null || partToEdit.Prices.Count == 0)
			{
				partToEdit.Prices = new List<PartPrice>() { new PartPrice() {
					CostPrice = Convert.ToDouble(part.CostPrice),
					EffectiveDate = DateTime.Now,
					IsDeleted = false,
					SellingPrice = 0
					}};
			}
			else
			{
				partToEdit.Prices.First().CostPrice = Convert.ToDouble(part.CostPrice);
			}

			await _aeroMechDBContext.SaveChangesAsync();
			return Ok();
		}

		[Route("delete/{id}")]
		[HttpPost(Name = "Delete part")]
		public async Task<IActionResult> Delete(int id)
		{
			var part = await _aeroMechDBContext.Parts.FindAsync(id);
			part.IsDeleted = true;
			await _aeroMechDBContext.SaveChangesAsync();
			return new OkResult();
		}
	}
}
