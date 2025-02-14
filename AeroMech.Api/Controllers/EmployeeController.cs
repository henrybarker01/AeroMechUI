using AeroMech.Data.Models;
using AeroMech.Data.Persistence;
using AeroMech.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AeroMech.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeController : Controller
    {
        private readonly IMapper _mapper;
        private readonly AeroMechDBContext _aeroMechDBContext;
        public EmployeeController(AeroMechDBContext context, IMapper mapper)
        {
            _aeroMechDBContext = context;
            _mapper = mapper;
        }

        [HttpGet("employees")]
        public async Task<ActionResult<IEnumerable<EmployeeModel>>> GetAll()
        {
            List<Employee> employees = await _aeroMechDBContext.Employees.Where(x => x.IsDeleted == false)
      .Include(a => a.Address) 
      .ToListAsync();
            return Ok(_mapper.Map<IEnumerable<EmployeeModel>>(employees));
        }

        [Route("add")]
        [HttpPost(Name = "Add employee")]
        public async Task<ActionResult<int>> Add(EmployeeModel employee)
        {
            Employee emp = _mapper.Map<Employee>(employee);
            _aeroMechDBContext.Employees.Add(emp);
            await _aeroMechDBContext.SaveChangesAsync();
            return Ok(emp.Id);
        }

        [Route("edit")]
        [HttpPost(Name = "Edit employee")]
        public async Task<IActionResult> Edit(EmployeeModel employee)
        {
            Employee employeeToEdit = _aeroMechDBContext.Employees
                .Include(x => x.Address)
                
                .Single(x => x.Id == employee.Id);

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
            return Ok();
        }

        [Route("delete/{id}")]
        [HttpPost(Name = "Delete employee")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _aeroMechDBContext.Employees.FindAsync(id);
            employee.IsDeleted = true;
            await _aeroMechDBContext.SaveChangesAsync();
            return new OkResult();
        }
    }
}
