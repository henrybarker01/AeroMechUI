using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	public class Address
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public string AddressLine1 { get; set; }

		[Required]
		public string AddressLine2 { get; set; }

		[Required]
		public string City { get; set; }

		[Required]
		public string PostalCode { get; set; }
	}
}
