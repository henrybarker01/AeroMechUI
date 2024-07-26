using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
    public class Employee
    {
        [Key]
        public int Id { get; set; }
        public string? IDNumber { get; set; }
       // [Required]
        public string? Title { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public virtual List<EmployeeRate>? Rates { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public bool IsDeleted { get; set; }
        public virtual Address Address { get; set; }
        public int? AddressId { get; set; }
		public DateOnly? BirthDate { get; set; }
	}
}
