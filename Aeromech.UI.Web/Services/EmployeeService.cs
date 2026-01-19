using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.UI.Web.Services
{
    public class EmployeeService
    {
        private readonly IMapper _mapper;
        private readonly IDbContextFactory<AeroMechDBContext> _contextFactory;
        
        public EmployeeService(IDbContextFactory<AeroMechDBContext> contextFactory, IMapper mapper)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
        }

        public async Task<List<EmployeeModel>> GetEmployees()
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            List<Employee> employees = await _aeroMechDBContext.Employees.AsNoTracking()
                .Where(x => x.IsDeleted == false)
                .Include(a => a.Address)
                .ToListAsync();
            return _mapper.Map<List<EmployeeModel>>(employees);
        }

        public async Task DeleteEmployee(EmployeeModel emp)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            var employee = await _aeroMechDBContext.Employees.FindAsync(emp.Id);
            if (employee != null)
            {
                employee.IsDeleted = true;
                await _aeroMechDBContext.SaveChangesAsync();
            }
        }

        public async Task<int> AddNewEmployee(EmployeeModel employee)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            if (employee.Id == 0)
            {
                Data.Models.Employee emp = _mapper.Map<Data.Models.Employee>(employee);
                _aeroMechDBContext.Employees.Add(emp);
                await _aeroMechDBContext.SaveChangesAsync();
                return emp.Id;

            }
            else
            {
                Data.Models.Employee employeeToEdit = await _aeroMechDBContext.Employees
                    .Include(x => x.Address)
                    .SingleAsync(x => x.Id == employee.Id);

                employeeToEdit.PhoneNumber = employee.PhoneNumber;
                employeeToEdit.IDNumber = employee.IDNumber;
                employeeToEdit.FirstName = employee.FirstName;
                employeeToEdit.LastName = employee.LastName;
                employeeToEdit.Email = employee.Email;
                employeeToEdit.Title = employee.Title;
                employeeToEdit.BirthDate = employee.BirthDate;

                if (employeeToEdit.Address == null)
                {
                    employeeToEdit.Address = new Address();
                }

                employeeToEdit.Address.AddressLine1 = employee.AddressLine1 ?? "";
                employeeToEdit.Address.AddressLine2 = employee.AddressLine2 ?? "";
                employeeToEdit.Address.City = employee.City ?? "";
                employeeToEdit.Address.PostalCode = employee.PostalCode ?? "";
                 
                await _aeroMechDBContext.SaveChangesAsync();
                return employeeToEdit.Id;
            }
        }
    }
}
