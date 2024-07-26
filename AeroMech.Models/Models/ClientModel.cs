using System.ComponentModel.DataAnnotations;

namespace AeroMech.Models
{
    public class ClientModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }

        public int AddressId { get; set; }

        [Required(ErrorMessage = "Address Line 1 is required")]
        public string AddressLine1 { get; set; }

        [Required(ErrorMessage = "Address Line 2 is required")]
        public string AddressLine2 { get; set; }

        [Required(ErrorMessage = "City is required")]
        public string City { get; set; }

        [Required(ErrorMessage = "Postal Code is required")]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "Contact Person Name is required")]
        public string ContactPersonName { get; set; }

        [Required(ErrorMessage = "Contact Person Number is required")]
        [RegularExpression("^(?!0+$)(\\+\\d{1,3}[- ]?)?(?!0+$)\\d{10,15}$", ErrorMessage = "Please enter valid phone no.")]
        public string ContactPersonNumber { get; set; }

        [Required(ErrorMessage = "Contact Person Email is required")]
        public const string Email = @"^[a-zA-Z0-9._\\-]+@[a-zA-Z0-9]+(([\\-]*[a-zA-Z0-9]+)*[.][a-zA-Z0-9]+)+(;[ ]*[a-zA-Z0-9._\\-]+@[a-zA-Z0-9]+(([\\-]*[a-zA-Z0-9]+)*[.][a-zA-Z0-9]+)+)*$";
        [RegularExpression(Email, ErrorMessage = "Please enter valid email address.")]
        public string ContactPersonEmail { get; set; }

        public List<VehicleModel>? Vehicles { get; set; }
    }
}
