namespace AeroMech.Models
{
	public class EmployeeRateModel
	{
		public int Id { get; set; }

		public DateTime EffectiveDate { get; set; }

		public double Rate { get; set; }

		public int EmployeeId { get; set; }

		public bool IsActive { get; set; }
	}
}
