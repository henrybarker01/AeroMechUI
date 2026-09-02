using AeroMech.Data.Enums;
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
        private readonly AuditService _auditService;

        public EmployeeService(IDbContextFactory<AeroMechDBContext> contextFactory, IMapper mapper, AuditService auditService)
        {
            _contextFactory = contextFactory;
            _mapper = mapper;
            _auditService = auditService;
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
                var user = await _auditService.ResolveUser();

                employee.IsDeleted = true;

                _auditService.Record(
                    _aeroMechDBContext,
                    user,
                    AuditArea.Employees,
                    AuditAction.Deleted,
                    nameof(Data.Models.Employee),
                    employee.Id,
                    $"{employee.FirstName} {employee.LastName}",
                    $"Employee {employee.FirstName} {employee.LastName} removed.");

                await _aeroMechDBContext.SaveChangesAsync();
            }
        }

        public async Task<int> AddNewEmployee(EmployeeModel employee)
        {
            using var _aeroMechDBContext = await _contextFactory.CreateDbContextAsync();
            
            if (employee.Id == 0)
            {
                Data.Models.Employee emp = _mapper.Map<Data.Models.Employee>(employee);

                // Saved first so the entry can name the id the save produced, and inside one
                // transaction so a person cannot be taken on without a record of it.
                using var transaction = await _aeroMechDBContext.Database.BeginTransactionAsync();

                _aeroMechDBContext.Employees.Add(emp);
                await _aeroMechDBContext.SaveChangesAsync();

                var user = await _auditService.ResolveUser();

                _auditService.Record(
                    _aeroMechDBContext,
                    user,
                    AuditArea.Employees,
                    AuditAction.Created,
                    nameof(Data.Models.Employee),
                    emp.Id,
                    $"{emp.FirstName} {emp.LastName}",
                    $"Employee {emp.FirstName} {emp.LastName} added.");

                await _aeroMechDBContext.SaveChangesAsync();
                await transaction.CommitAsync();

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
                employeeToEdit.ExcludeFromTimesheets = employee.ExcludeFromTimesheets;

                if (employeeToEdit.Address == null)
                {
                    employeeToEdit.Address = new Address();
                }

                employeeToEdit.Address.AddressLine1 = employee.AddressLine1 ?? "";
                employeeToEdit.Address.AddressLine2 = employee.AddressLine2 ?? "";
                employeeToEdit.Address.City = employee.City ?? "";
                employeeToEdit.Address.PostalCode = employee.PostalCode ?? "";

                var user = await _auditService.ResolveUser();

                _auditService.Record(
                    _aeroMechDBContext,
                    user,
                    AuditArea.Employees,
                    AuditAction.Updated,
                    nameof(Data.Models.Employee),
                    employeeToEdit.Id,
                    $"{employeeToEdit.FirstName} {employeeToEdit.LastName}",
                    $"Employee {employeeToEdit.FirstName} {employeeToEdit.LastName} updated.");

                await _aeroMechDBContext.SaveChangesAsync();
                return employeeToEdit.Id;
            }
        }
    }
}
