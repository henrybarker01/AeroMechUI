using AeroMech.Models.Enums;

namespace AeroMech.Models
{
	public class ClientRateModel
	{
		public int Id { get; set; }

		public DateTime EffectiveDate { get; set; }

		public double Rate { get; set; }
		 
        public int ClientId { get; set; }

        public RateType RateType { get; set; }
        public bool IsActive { get; set; }
	}
}
