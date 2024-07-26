
using AeroMech.Data.Persistence;
using AeroMech.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace AeroMech.UI.Serices
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
    }
}
