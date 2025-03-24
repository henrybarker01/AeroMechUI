using AeroMech.Data.Enums;
using AeroMech.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AeroMech.Models.Models
{
    public class StockTakeModel
    {
        public DateTime StockTakeDate { get; set; }
        public string StockTakeBy { get; set; }
        public string Type { get; set; }
        public StockTakeStatus Status { get; set; }
        public string StockTakeDescription { get; set; }
        public string Remarks { get; set; }

        public int WarehouseId { get; set; }
        public virtual Warehouse Warehouse { get; set; }
        public DateTime CompletedDate { get; set; }

        public virtual ICollection<StockTakeParts> StockTakeParts { get; set; }

    }
}
