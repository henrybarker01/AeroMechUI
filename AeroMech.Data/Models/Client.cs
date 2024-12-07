using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
	public class Client
	{
		[Key]
		public int Id { get; set; }
		[Required]
		public string Name { get; set; }
		public string ContactPersonName { get; set; }
		public string ContactPersonNumber { get; set; }
		public string ContactPersonEmail { get; set; }
        public DateOnly? ContactPersonBirthDate { get; set; }
        public virtual Address Address { get; set; }
		public int AddressId { get; set; }
		public virtual List<Vehicle>? Vehicles { get; set; }
	}
}
