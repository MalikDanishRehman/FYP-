using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;


namespace AI_Driven_Water_Supply.Application.Models
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
        public string Status { get; set; }
    }
}