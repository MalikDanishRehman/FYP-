using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Domain.Entities
{
    [Table("profiles")]
    public class Profile : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("role")]
        public string Role { get; set; }

        [Column("rating")]
        public float8? Rating { get; set; }

        [Column("services")]
        public string Services { get; set; }

        [Column("location")]
        public string Location { get; set; }

        [Column("latitude")]
        public float8? Latitude { get; set; }

        [Column("longitude")]
        public float8? Longitude { get; set; }

        [Column("profilepic")]
        public string ProfilePic { get; set; }
    }
}
