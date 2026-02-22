using System.ComponentModel.DataAnnotations;

namespace AeroMech.Data.Models
{
    public class BaseModel
    {
        [Key]
        public int Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsDeleted { get; set; }
    }
}
