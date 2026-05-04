using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Domain.Entities
{
    [Table("admin_preferences")]
    public class AdminPreferences : BaseModel
    {
        [PrimaryKey("admin_id", false)]
        [Column("admin_id")]
        public string AdminId { get; set; } = "";

        [Column("preferences")]
        public string Preferences { get; set; } = "{}";

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
