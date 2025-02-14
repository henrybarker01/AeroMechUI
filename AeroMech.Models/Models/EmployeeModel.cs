using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models
{
    public class EmployeeModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "ID Number is required")]
        [MinLength(13, ErrorMessage = "Please enter a valid ID number.")]
        public string? IDNumber { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Surname is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [RegularExpression("^(?!0+$)(\\+\\d{1,3}[- ]?)?(?!0+$)\\d{10,15}$", ErrorMessage = "Please enter valid phone no.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email is required")]
        public const string EmailR = @"^[a-zA-Z0-9._\\-]+@[a-zA-Z0-9]+(([\\-]*[a-zA-Z0-9]+)*[.][a-zA-Z0-9]+)+(;[ ]*[a-zA-Z0-9._\\-]+@[a-zA-Z0-9]+(([\\-]*[a-zA-Z0-9]+)*[.][a-zA-Z0-9]+)+)*$";
        [RegularExpression(EmailR, ErrorMessage = "Please enter valid email address.")]
        public string? Email { get; set; }

        public bool IsDeleted { get; set; }

        public int? AddressId { get; set; }

        [Required(ErrorMessage = "Address Line 1 is required")]
        public string? AddressLine1 { get; set; }

        [Required(ErrorMessage = "Address Line 2 is required")]
        public string? AddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string? City { get; set; }

        [Required(ErrorMessage = "PostalCode is required")]
        public string? PostalCode { get; set; }

        public DateOnly? BirthDate { get; set; }
    }
}
