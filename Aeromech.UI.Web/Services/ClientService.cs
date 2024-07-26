using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    public class ClientService
    {
        private readonly AeroMechDBContext _aeroMechDBContext;

        public ClientService(AeroMechDBContext context)
        {
            _aeroMechDBContext = context;
        }

        public async Task<List<ClientModel>> GetClients()
        {

            List<Data.Models.Client> clients = _aeroMechDBContext.Clients.Include(a => a.Address).ToList();
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

            return mappedClients;
        }

        public async Task<int> AddClient(ClientModel client)
        {
            Data.Models.Client newClient = new Data.Models.Client
            {
                Id = client.Id,
                Name = client.Name,
                Address = new Address
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
            return newClient.Id;
        }

        public async Task Delete(int id)
        {
            var client = await _aeroMechDBContext.Clients.FindAsync(id);
            if (client != null)
            {
                _aeroMechDBContext.Clients.Remove(client);
                await _aeroMechDBContext.SaveChangesAsync();
            }
        }
    }
}
