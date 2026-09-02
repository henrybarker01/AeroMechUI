using AeroMech.Data.Enums;
using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    public class VehicleService
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        private readonly AuditService _auditService;

        public VehicleService(IDbContextFactory<AeroMechDBContext> contextFactory, IMapper mapper, AuditService auditService)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
            _auditService = auditService;
        }

        public async Task<List<VehicleModel>> GetVehicles(int clientId)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            List<Vehicle> vehicles = await _aeroMechDBContext.Vehicles.AsNoTracking()
                .Where(x => x.IsDeleted == false && x.ClientId == clientId)
                .ToListAsync();
            return _mapper.Map<List<VehicleModel>>(vehicles);
        }

        public async Task DeleteVehicle(VehicleModel vehicle)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            var part = await _aeroMechDBContext.Vehicles.FindAsync(vehicle.Id);
            if (part != null)
            {
                var user = await _auditService.ResolveUser();

                part.IsDeleted = true;

                _auditService.Record(
                    _aeroMechDBContext,
                    user,
                    AuditArea.Vehicles,
                    AuditAction.Deleted,
                    nameof(Vehicle),
                    part.Id,
                    part.SerialNumber,
                    $"Vehicle {part.SerialNumber} removed.");

                await _aeroMechDBContext.SaveChangesAsync();
            }
        }

        public async Task<int> AddNewVehicle(VehicleModel vehicle)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            if (vehicle.Id == 0)
            {
                Vehicle vhl = _mapper.Map<Vehicle>(vehicle);

                // Saved first so the entry can name the id the save produced, and inside one
                // transaction so a machine cannot be added to a fleet with nothing recording it.
                using var transaction = await _aeroMechDBContext.Database.BeginTransactionAsync();

                _aeroMechDBContext.Vehicles.Add(vhl);
                await _aeroMechDBContext.SaveChangesAsync();

                var user = await _auditService.ResolveUser();

                _auditService.Record(
                    _aeroMechDBContext,
                    user,
                    AuditArea.Vehicles,
                    AuditAction.Created,
                    nameof(Vehicle),
                    vhl.Id,
                    vhl.SerialNumber,
                    $"Vehicle {vhl.SerialNumber} added.");

                await _aeroMechDBContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return vhl.Id;
            }
            else
            {
                Vehicle vehicleToEdit = await _aeroMechDBContext.Vehicles
                    .SingleAsync(x => x.Id == vehicle.Id);
                    
                vehicleToEdit.SerialNumber = vehicle.SerialNumber;
                vehicleToEdit.ChassisNumber = vehicle.ChassisNumber;
                vehicleToEdit.JobNumber = vehicle.JobNumber;
                vehicleToEdit.PurchasePrice = vehicle.PurchasePrice;
                vehicleToEdit.EngineHours = vehicle.EngineHours;
                vehicleToEdit.ManufactureDate = vehicle.ManufactureDate;
                vehicleToEdit.DateInService = vehicle.DateInService;
                vehicleToEdit.Description = vehicle.Description;
                vehicleToEdit.MachineType = vehicle.MachineType;

                var user = await _auditService.ResolveUser();

                _auditService.Record(
                    _aeroMechDBContext,
                    user,
                    AuditArea.Vehicles,
                    AuditAction.Updated,
                    nameof(Vehicle),
                    vehicleToEdit.Id,
                    vehicleToEdit.SerialNumber,
                    $"Vehicle {vehicleToEdit.SerialNumber} updated.");

                await _aeroMechDBContext.SaveChangesAsync();
                return vehicle.Id;
            }
        }
    }
}
