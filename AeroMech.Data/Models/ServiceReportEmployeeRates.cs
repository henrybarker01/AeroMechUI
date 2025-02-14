//using AeroMech.Models.Enums;
//using System.ComponentModel.DataAnnotations;

//namespace AeroMech.Data.Models
//{
//	public class ServiceReportEmployeeRates
//	{
//		[Key]
//		public int Id { get; set; }

//		[Required]
//		public virtual ServiceReport? ServiceReport { get; set; }

//		[Required]
//		public int? ServiceReportId { get; set; }
		 
//		public virtual Employee? Employee { get; set; }

//		[Required]
//		public int EmployeeId { get; set; }

//        public virtual Client Client { get; set; }

//        [Required]
//        public int ClientId { get; set; }

//        public double Hours { get; set; }
//		public double Rate { get; set; }
//		public RateType RateType { get; set; }

//        public double Discount { get; set; } 
//		public bool IsDeleted { get; set; }

//	}
//}
