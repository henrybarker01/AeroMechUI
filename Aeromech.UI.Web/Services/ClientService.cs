using AeroMech.Data.Enums;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AeroMech.Models.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    public class ClientService
    {
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly IMapper _mapper;
        private readonly AuditService _auditService;

        public ClientService(IDbContextFactory<AeroMechDBContext> contextFactory, IMapper mapper, AuditService auditService)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
            _auditService = auditService;
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
                    EffectiveDate = DateTimeOffset.UtcNow,
                    ClientId = client.Id,
                    RateType = rate.RateType,
                    IsActive = true,
                });
            });

            using var transaction = await _aeroMechDBContext.Database.BeginTransactionAsync();

            _aeroMechDBContext.Clients.Add(newClient);
            await _aeroMechDBContext.SaveChangesAsync();

            // Written against the id the save produced, inside the same transaction, so a client
            // cannot come into existence without an entry that names it - and one that was rolled
            // back leaves none saying it did.
            var user = await _auditService.ResolveUser();

            _auditService.Record(
                _aeroMechDBContext,
                user,
                AuditArea.Clients,
                AuditAction.Created,
                nameof(Data.Models.Client),
                newClient.Id,
                newClient.Name,
                $"Client {newClient.Name} added.");

            // The rates a client is set up with are the figures its first invoices use, recorded
            // the way an edit records a rate that moved - with nothing as the previous value.
            foreach (var rate in newClient.Rates)
            {
                _auditService.RecordPriceChange(
                    _aeroMechDBContext,
                    user,
                    nameof(Data.Models.Client),
                    newClient.Id,
                    newClient.Name,
                    rate.RateType.ToString(),
                    string.Empty,
                    AuditService.FormatMoney(rate.Rate),
                    $"{rate.RateType.GetDisplayName()} rate set for {newClient.Name}.");
            }

            await _aeroMechDBContext.SaveChangesAsync();
            await transaction.CommitAsync();

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

            // Collected inside the loop and written after it, because the loop body cannot await.
            // A rate that had none before is recorded the same way as one that moved, with nothing
            // as its previous value: it is still the figure every invoice raised afterwards uses.
            var rateChanges = new List<(RateType RateType, decimal? Previous, decimal Current)>();

            if (clientToEdit?.Rates?.Count == 0)
            {
                client.Rates.ForEach(rate =>
                {
                    var newRate = Convert.ToDecimal(rate.Rate);

                    clientToEdit.Rates.Add(new ClientRate()
                    {
                        Rate = newRate,
                        EffectiveDate = DateTimeOffset.UtcNow,
                        ClientId = client.Id,
                        RateType = rate.RateType,
                        IsActive = true,
                    });

                    rateChanges.Add((rate.RateType, null, newRate));
                });
            }
            else
            {
                clientToEdit.Rates.ForEach(rate =>
                {
                    var previousRate = rate.Rate;

                    rate.Rate = Convert.ToDecimal(client.Rates.FirstOrDefault(x => x.RateType == rate.RateType).Rate);
                    rate.EffectiveDate = DateTimeOffset.UtcNow;
                    rate.IsActive = true;

                    // What a client is charged is a price like any other, and the one that shows up
                    // on every invoice raised afterwards. Recorded per rate type, because that is
                    // the granularity it is argued about at.
                    if (previousRate != rate.Rate)
                    {
                        rateChanges.Add((rate.RateType, previousRate, rate.Rate));
                    }
                });
            }

            if (rateChanges.Count > 0)
            {
                var user = await _auditService.ResolveUser();

                foreach (var change in rateChanges)
                {
                    _auditService.RecordPriceChange(
                        _aeroMechDBContext,
                        user,
                        nameof(Data.Models.Client),
                        clientToEdit!.Id,
                        clientToEdit.Name,
                        change.RateType.ToString(),
                        change.Previous.HasValue ? AuditService.FormatMoney(change.Previous.Value) : string.Empty,
                        AuditService.FormatMoney(change.Current),
                        change.Previous.HasValue
                            ? $"{change.RateType.GetDisplayName()} rate changed for {clientToEdit.Name}."
                            : $"{change.RateType.GetDisplayName()} rate set for {clientToEdit.Name}.");
                }
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
