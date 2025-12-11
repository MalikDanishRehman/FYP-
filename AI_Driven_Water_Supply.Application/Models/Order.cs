using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace AI_Driven_Water_Supply.Presentation
{
    [Table("orders")]
    public class Order : BaseModel
    {
        // 👇 Change This Line (false zaroori hai)
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("consumer_name")] public string ConsumerName { get; set; } = string.Empty;
        [Column("address")] public string Address { get; set; } = string.Empty;
        [Column("phone")] public string Phone { get; set; } = string.Empty;
        [Column("quantity")] public int Quantity { get; set; }
        [Column("item_name")] public string ItemName { get; set; } = string.Empty;
        [Column("total_price")] public int TotalPrice { get; set; } 
        [Column("supplier_name")] public string SupplierName { get; set; } = string.Empty;
        [Column("status")] public string Status { get; set; } = "Pending";
        [Column("created_at")] public DateTime CreatedAt { get; set; }
    }
}