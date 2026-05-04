using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Domain.Entities
{
    [Table("disputes")]
    public class Dispute : BaseModel
    {
        [PrimaryKey("id", false)]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("order_id")]
        public long? OrderId { get; set; }

        [Column("consumer_id")]
        public string? ConsumerId { get; set; }

        [Column("provider_id")]
        public string? ProviderId { get; set; }

        [Column("issue_type")]
        public string IssueType { get; set; } = "";

        [Column("description")]
        public string Description { get; set; } = "";

        [Column("priority")]
        public string Priority { get; set; } = "Med";

        [Column("status")]
        public string Status { get; set; } = "Pending";

        [Column("resolution_notes")]
        public string? ResolutionNotes { get; set; }

        [Column("resolved_by")]
        public string? ResolvedBy { get; set; }

        [Column("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
