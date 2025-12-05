using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
// using System.ComponentModel.DataAnnotations.Schema; // Agar yeh hai to hata do!

namespace AI_Driven_Water_Supply.Application.Models
{
    // 👇 Yahan poora namespace likho
    [Supabase.Postgrest.Attributes.Table("profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        // 👇 Yahan bhi poora namespace likho
        [Supabase.Postgrest.Attributes.Column("username")]
        public string Username { get; set; }

        [Supabase.Postgrest.Attributes.Column("role")]
        public string Role { get; set; }
    }
}