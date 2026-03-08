using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Domain.Entities
{
    [Table("workers")]
    public class Worker : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("supplier_id")]
        public string SupplierId { get; set; } = null!;

        [Column("name")]
        public string Name { get; set; } = null!;

        [Column("role")]
        public string Role { get; set; } = null!;

        [Column("base_salary")]
        public decimal BaseSalary { get; set; }

        [Column(ignoreOnInsert: true, ignoreOnUpdate: true)]
        public string TodayStatus { get; set; } = "Pending";
    }
}
