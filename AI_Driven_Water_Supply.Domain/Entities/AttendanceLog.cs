using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Domain.Entities
{
    [Table("attendance")]
    public class AttendanceLog : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("worker_id")]
        public long WorkerId { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }

        [Column("status")]
        public string Status { get; set; } = null!;
    }
}
