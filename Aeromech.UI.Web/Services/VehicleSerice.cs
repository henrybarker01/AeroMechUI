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
        private readonly AeroMechDBContext _aeroMechDBContext;
        public VehicleService(AeroMechDBContext context, IMapper mapper)
        {
            _aeroMechDBContext = context;
            _mapper = mapper;
        }

        public async Task<List<VehicleModel>> GetVehicles(int clientId)
        {
            List<Vehicle> vehicles = await _aeroMechDBContext.Vehicles.AsNoTracking()
                .Where(x => x.IsDeleted == false && x.ClientId == clientId)
                .ToListAsync();
            return _mapper.Map<List<VehicleModel>>(vehicles);
        }

        public async Task DeleteVehicle(VehicleModel vehicle)
        {
            var part = await _aeroMechDBContext.Vehicles.FindAsync(vehicle.Id);
            part.IsDeleted = true;
            await _aeroMechDBContext.SaveChangesAsync();
        }

        public async Task<int> AddNewVehicle(VehicleModel vehicle)
        {
            if (vehicle.Id == 0)
            {
                Vehicle vhl = _mapper.Map<Vehicle>(vehicle);
                _aeroMechDBContext.Vehicles.Add(vhl);
                await _aeroMechDBContext.SaveChangesAsync();
                return vhl.Id;
            }
            else
            {
                Vehicle vehicleToEdit = _aeroMechDBContext.Vehicles
                .Single(x => x.Id == vehicle.Id);
                vehicleToEdit.SerialNumber = vehicle.SerialNumber;
                vehicleToEdit.ChassisNumber = vehicle.ChassisNumber;
                vehicleToEdit.JobNumber = vehicle.JobNumber;
                vehicleToEdit.PurchasePrice = vehicle.PurchasePrice;
                vehicleToEdit.EngineHours = vehicle.EngineHours;
                vehicleToEdit.ManufactureDate = vehicle.ManufactureDate;
                vehicleToEdit.DateInService = vehicle.DateInService;
                vehicleToEdit.Description = vehicle.Description;
                vehicleToEdit.MachineType = vehicle.MachineType;


                await _aeroMechDBContext.SaveChangesAsync();
                return vehicle.Id;
            }
        }
    }
}
