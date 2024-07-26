﻿//using AeroMech.Data.Models;
//using AeroMech.Data.Persistence;
//using AeroMech.Models;
//using AutoMapper;
//using Microsoft.EntityFrameworkCore;

//namespace AeroMech.UI.Web.Services
//{
//    public class EmployeeService
//    {
//        private readonly IMapper _mapper;
//        private readonly AeroMechDBContext _aeroMechDBContext;
//        public EmployeeService(AeroMechDBContext context, IMapper mapper)
//        {
//            _aeroMechDBContext = context;
//            _mapper = mapper;
//        }

//        public async Task<List<EmployeeModel>> GetEmployees()
//        {
//            List<Employee> employees = await _aeroMechDBContext.Employees.Where(x => x.IsDeleted == false)
//                .Include(a => a.Address)
//                .Include(r => r.Rates)
//                .ToListAsync();
//            return _mapper.Map<List<EmployeeModel>>(employees);
//        }

//        public async Task DeleteEmployee(EmployeeModel emp)
//        {
//            var employee = await _aeroMechDBContext.Employees.FindAsync(emp.Id);
//            employee.IsDeleted = true;
//            await _aeroMechDBContext.SaveChangesAsync();
//        }

//        public async Task<int> AddNewEmployee(EmployeeModel employee)
//        {
//            if (employee.Id == 0)
//            {
//                Employee emp = _mapper.Map<Employee>(employee);
//                _aeroMechDBContext.Employees.Add(emp);
//                await _aeroMechDBContext.SaveChangesAsync();
//                return emp.Id;

//            }
//            else
//            {
//                Employee employeeToEdit = _aeroMechDBContext.Employees
//                .Include(x => x.Address)
//                .Include(r => r.Rates)
//                .Single(x => x.Id == employee.Id);

//                employeeToEdit.PhoneNumber = employee.PhoneNumber;
//                employeeToEdit.IDNumber = employee.IDNumber;
//                employeeToEdit.FirstName = employee.FirstName;
//                employeeToEdit.LastName = employee.LastName;
//                employeeToEdit.Email = employee.Email;
//                employeeToEdit.Title = employee.Title;
//                employeeToEdit.BirthDate = employee.BirthDate;

//                if (employeeToEdit.Address == null)
//                {
//                    employeeToEdit.Address = new Address();
//                }

//                employeeToEdit.Address.AddressLine1 = employee.AddressLine1 ?? "";
//                employeeToEdit.Address.AddressLine2 = employee.AddressLine2 ?? "";
//                employeeToEdit.Address.City = employee.City ?? "";
//                employeeToEdit.Address.PostalCode = employee.PostalCode ?? "";

//                if (employeeToEdit?.Rates?.Count == 0)
//                {
//                    employeeToEdit.Rates.Add(new EmployeeRate()
//                    {
//                        Rate = Convert.ToDecimal(employee.Rate),
//                        EffectiveDate = DateTime.Now,
//                        EmployeeId = employee.Id,
//                        IsActive = true,
//                    });

//                }
//                else
//                {
//                    employeeToEdit.Rates[0] = new EmployeeRate()
//                    {
//                        Rate = Convert.ToDecimal(employee.Rate),
//                        EffectiveDate = DateTime.Now,
//                        EmployeeId = employee.Id,
//                        IsActive = true,
//                    };
//                }

//                await _aeroMechDBContext.SaveChangesAsync();
//                return employeeToEdit.Id;
//            }
//        }
//    }
//}