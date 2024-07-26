using AeroMech.Data.Persistence;
using AeroMech.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ClientController : Controller
	{
		private readonly AeroMechDBContext _aeroMechDBContext;
		public ClientController(AeroMechDBContext context)
		{
			_aeroMechDBContext = context;
		}

		[HttpGet(Name = "GetAll")]
		public IActionResult GetAll()
		{
			List<Data.Models.Client> clients = _aeroMechDBContext.Clients.Include(a=>a.Address).ToList();
			List<ClientModel> mappedClients = new List<ClientModel>();

			clients.ForEach(x =>
			{
				mappedClients.Add(new ClientModel
				{
					Id = x.Id,
					Name = x.Name,
					ContactPersonName = x.ContactPersonName,
					ContactPersonEmail = x.ContactPersonEmail,
					ContactPersonNumber = x.ContactPersonNumber,
					AddressLine1 = x.Address.AddressLine1,
					AddressLine2 = x.Address.AddressLine2,
					City = x.Address.City,
					PostalCode = x.Address.PostalCode,
				});
			});

			return new OkObjectResult(mappedClients);
		}

		[Route("add")]
		[HttpPost(Name = "Add client")]
		public async Task<IActionResult> Add(ClientModel client)
		{
			Data.Models.Client newClient = new Data.Models.Client
			{
				Id = client.Id,
				Name = client.Name,
				Address = new Data.Models.Address
				{
					AddressLine1 = client.AddressLine1,
					AddressLine2 = client.AddressLine2,
					City = client.City,
					PostalCode = client.PostalCode,
				},
				ContactPersonName = client.ContactPersonName,
				ContactPersonNumber = client.ContactPersonNumber,
				ContactPersonEmail = client.ContactPersonEmail,
			};
			_aeroMechDBContext.Clients.Add(newClient);
			await _aeroMechDBContext.SaveChangesAsync();
			return new OkObjectResult(newClient.Id);
		}

		[Route("delete/{id}")]
		[HttpPost(Name = "Delete client")]
		public async Task<IActionResult> Delete(int id)
		{
			var client = await _aeroMechDBContext.Clients.FindAsync(id);
			if (client != null)
			{
				_aeroMechDBContext.Clients.Remove(client);
				await _aeroMechDBContext.SaveChangesAsync();
			}

			return new OkResult();
		}
	}
}
