//using AeroMech.Data.Models;
//using AeroMech.Data.Persistence;
//using AeroMech.Models;
//using AutoMapper;
//using Microsoft.EntityFrameworkCore;

//namespace AeroMech.UI.Web.Services
//{
//    public class VehicleService
//    {
//        private readonly IMapper _mapper;
//        private readonly AeroMechDBContext _aeroMechDBContext;
//        public VehicleService(AeroMechDBContext context, IMapper mapper)
//        {
//            _aeroMechDBContext = context;
//            _mapper = mapper;
//        }

//        public async Task<List<VehicleModel>> GetVehicles(int clientId)
//        {
//            List<Vehicle> vehicles = await _aeroMechDBContext.Vehicles.Where(x => x.IsDeleted == false && x.ClientId == clientId)
//                .ToListAsync();
//            return _mapper.Map<List<VehicleModel>>(vehicles);
//        }

//        public async Task DeleteVehicle(VehicleModel vehicle)
//        {
//            var part = await _aeroMechDBContext.Vehicles.FindAsync(vehicle.Id);
//            part.IsDeleted = true;
//            await _aeroMechDBContext.SaveChangesAsync();
//        }

//        public async Task<int> AddNewVehicle(VehicleModel vehicle)
//        {
//            if (vehicle.Id == 0)
//            {
//                Vehicle vhl = _mapper.Map<Vehicle>(vehicle);
//                _aeroMechDBContext.Vehicles.Add(vhl);
//                await _aeroMechDBContext.SaveChangesAsync();
//                return vhl.Id;
//            }
//            else
//            {
//                Vehicle vehicleToEdit = _aeroMechDBContext.Vehicles
//                //.Include(x => x.Prices)
//                .Single(x => x.Id == vehicle.Id);

//                //partToEdit.PartCode = part.PartCode;
//                //partToEdit.PartDescription = part.PartDescription;
//                //partToEdit.Bin = part.Bin;
//                //partToEdit.CycleCount = part.CycleCount;
//                //partToEdit.Warehouse = _mapper.Map<Warehouse>(part.Warehouse);
//                //partToEdit.SupplierCode = part.SupplierCode;
//                //partToEdit.QtyOnHand = part.QtyOnHand;
//                //partToEdit.ProductClass = part.ProductClass;

//                //if (partToEdit.Prices == null || partToEdit.Prices.Count == 0)
//                //{
//                //	partToEdit.Prices = new List<PartPrice>() { new PartPrice() {
//                //		CostPrice = part.CostPrice,
//                //		EffectiveDate = DateTime.Now,
//                //		IsDeleted = false,
//                //		SellingPrice = 0
//                //		}};
//                //}
//                //else
//                //{
//                //	partToEdit.Prices.First().CostPrice = part.CostPrice;
//                //}

//                await _aeroMechDBContext.SaveChangesAsync();
//                return vehicle.Id;
//            }
//        }
//    }
//}
