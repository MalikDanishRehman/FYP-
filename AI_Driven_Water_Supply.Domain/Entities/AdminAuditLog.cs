using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Domain.Entities
{
    [Table("admin_audit_log")]
    public class AdminAuditLog : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public long? Id { get; set; }

        [Column("admin_id")]
        public string AdminId { get; set; } = "";

        [Column("action")]
        public string Action { get; set; } = "";

        [Column("entity_type")]
        public string? EntityType { get; set; }

        [Column("entity_id")]
        public string? EntityId { get; set; }

        [Column("payload")]
        public string? Payload { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
