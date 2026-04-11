using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Domain.Entities
{
    [Table("profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = null!;

        [Column("username")]
        public string Username { get; set; } = null!;

        [Column("role")]
        public string Role { get; set; } = null!;

        [Column("rating")]
        public double? Rating { get; set; }

        [Column("trust_score")]
        public double? TrustScore { get; set; }

        [Column("services")]
        public string Services { get; set; } = null!;

        [Column("location")]
        public string Location { get; set; } = null!;

        [Column("latitude")]
        public double? Latitude { get; set; }

        [Column("longitude")]
        public double? Longitude { get; set; }

        [Column("profilepic")]
        public string ProfilePic { get; set; } = null!;
    }
}
