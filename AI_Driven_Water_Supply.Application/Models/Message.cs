using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Presentation
{
    // Ye class ab alag file mein hai, to Razor compiler confuse nahi hoga
    [Table("messages")]
    public class Message : BaseModel
    {
        // 👇 Change This Line Here Too
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("order_id")] public long OrderId { get; set; }
        [Column("sender_name")] public string SenderName { get; set; } = string.Empty;
        [Column("receiver_name")] public string ReceiverName { get; set; } = string.Empty;
        [Column("content")] public string Content { get; set; } = string.Empty;
        [Column("created_at")] public DateTime CreatedAt { get; set; } 
    }
}