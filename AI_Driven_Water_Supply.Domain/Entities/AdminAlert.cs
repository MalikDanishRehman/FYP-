using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Domain.Entities
{
    [Table("admin_alerts")]
    public class AdminAlert : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("alert_type")]
        public string AlertType { get; set; } = "";

        [Column("message")]
        public string Message { get; set; } = "";

        [Column("detail")]
        public string Detail { get; set; } = "{}";

        [Column("read")]
        public bool Read { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
