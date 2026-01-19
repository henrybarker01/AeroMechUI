using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    public class ClientService
    {
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly IMapper _mapper;

        public ClientService(IDbContextFactory<AeroMechDBContext> contextFactory, IMapper mapper)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
        }

        public async Task<List<ClientModel>> GetClients()
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            var clients = await _aeroMechDBContext.Clients.AsNoTracking()
                .Include(a => a.Address)
                .Include(r => r.Rates)
                .Where(x => x.IsDeleted == false)
                .ToListAsync();

            return _mapper.Map<List<ClientModel>>(clients);
        }

        public async Task<int> AddClient(ClientModel client)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
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
                ContactPersonBirthDate = client.ContactPersonBirthDate,
                Rates = new List<ClientRate>()
            };

            client.Rates.ForEach(rate =>
            {
                newClient.Rates.Add(new ClientRate()
                {
                    Rate = Convert.ToDecimal(rate.Rate),
                    EffectiveDate = DateTime.Now,
                    ClientId = client.Id,
                    RateType = rate.RateType,
                    IsActive = true,
                });
            });

            _aeroMechDBContext.Clients.Add(newClient);
            await _aeroMechDBContext.SaveChangesAsync();
            return newClient.Id;
        }

        public async Task<int> EditClient(ClientModel client)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            var clientToEdit = await _aeroMechDBContext.Clients
                .Include(x => x.Address)
                .Include(r => r.Rates)
                .SingleAsync(x => x.Id == client.Id);

            clientToEdit.ContactPersonBirthDate = client.ContactPersonBirthDate;
            clientToEdit.ContactPersonEmail = client.ContactPersonEmail;
            clientToEdit.ContactPersonName = client.ContactPersonName;
            clientToEdit.ContactPersonNumber = client.ContactPersonNumber;
            clientToEdit.Name = client.Name;

            if (clientToEdit.Address == null)
            {
                clientToEdit.Address = new Address();
            }

            clientToEdit.Address.AddressLine1 = client.AddressLine1 ?? "";
            clientToEdit.Address.AddressLine2 = client.AddressLine2 ?? "";
            clientToEdit.Address.City = client.City ?? "";
            clientToEdit.Address.PostalCode = client.PostalCode ?? "";

            if (clientToEdit?.Rates?.Count == 0)
            {
                client.Rates.ForEach(rate =>
                {
                    clientToEdit.Rates.Add(new ClientRate()
                    {
                        Rate = Convert.ToDecimal(rate.Rate),
                        EffectiveDate = DateTime.Now,
                        ClientId = client.Id,
                        RateType = rate.RateType,
                        IsActive = true,
                    });
                });
            }
            else
            {
                clientToEdit.Rates.ForEach(rate =>
                {
                    rate.Rate = Convert.ToDecimal(client.Rates.FirstOrDefault(x => x.RateType == rate.RateType).Rate);
                    rate.EffectiveDate = DateTime.Now;
                    rate.IsActive = true;
                });
            }

            await _aeroMechDBContext.SaveChangesAsync();
            return clientToEdit.Id;
        }

        public async Task Delete(int id)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            var client = await _aeroMechDBContext.Clients.FindAsync(id);
            if (client != null)
            {
                client.IsDeleted = true;
                await _aeroMechDBContext.SaveChangesAsync();
            }
        }
    }
}
